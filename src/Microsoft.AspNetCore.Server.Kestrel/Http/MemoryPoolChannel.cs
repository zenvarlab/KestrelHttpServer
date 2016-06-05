// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class MemoryPoolChannel : ICriticalNotifyCompletion, IDisposable
    {
        private static readonly Action _awaitableIsCompleted = () => { };
        private static readonly Action _awaitableIsNotCompleted = () => { };

        private readonly MemoryPool _memory;
        private readonly ManualResetEventSlim _manualResetEvent = new ManualResetEventSlim(false, 0);

        private Action _awaitableState;
        private Exception _awaitableError;

        private MemoryPoolBlock _head;
        private MemoryPoolBlock _tail;

        private int _consumingState;
        private object _sync = new object();
        private readonly int _threshold;
        private int _count;
        private LimitState _limitState;
        private readonly IThreadPool _threadPool;


        public MemoryPoolChannel(MemoryPool memory, IThreadPool threadPool, int threshold = 10 * 1024)
        {
            _memory = memory;
            _awaitableState = _awaitableIsNotCompleted;
            _threshold = threshold;
            _threadPool = threadPool;
        }

        public bool RemoteIntakeFin { get; set; }

        public bool IsCompleted => ReferenceEquals(_awaitableState, _awaitableIsCompleted);

        public MemoryPoolIterator BeginWrite()
        {
            const int minimumSize = 2048;

            MemoryPoolBlock block = null;

            if (_tail != null && minimumSize <= _tail.Data.Offset + _tail.Data.Count - _tail.End)
            {
                block = _tail;
            }
            else
            {
                block = _memory.Lease();
            }

            // REVIEW: This should be in the lock
            if (_head == null)
            {
                _head = block;
            }
            else if (block != _tail)
            {
                Volatile.Write(ref _tail.Next, block);
                _tail = block;
            }

            return new MemoryPoolIterator(block, block.End);
        }

        public MemoryPoolIterator End()
        {
            return new MemoryPoolIterator(_tail, _tail.End);
        }

        public Task WriteAsync(byte[] buffer)
        {
            return WriteAsync(buffer, 0, buffer.Length);
        }

        public Task WriteAsync(ArraySegment<byte> buffer)
        {
            return WriteAsync(buffer.Array, buffer.Offset, buffer.Count);
        }

        public Task WriteAsync(byte[] buffer, int offset, int count)
        {
            lock (_sync)
            {
                if (count > 0)
                {
                    var iterator = BeginWrite();
                    iterator.CopyFrom(buffer, offset, count);
                    return EndWrite(iterator);
                }
                else
                {
                    // No more input
                    RemoteIntakeFin = true;
                    Complete();
                    return TaskUtilities.CompletedTask;
                }
            }
        }

        public Task EndWrite(MemoryPoolIterator end)
        {
            return EndWrite(end, error: null);
        }

        public Task EndWrite(MemoryPoolIterator end, Exception error)
        {
            lock (_sync)
            {
                if (!end.IsDefault)
                {
                    _tail = end.Block;
                    _tail.End = end.Index;

                    // REVIEW: This isn't that efficient, (we can do better)
                    var length = new MemoryPoolIterator(_head).GetLength(end);

                    if (length > _threshold)
                    {
                        _limitState = new LimitState();
                        _limitState.Length = length;
                    }
                }

                if (error != null)
                {
                    _awaitableError = error;
                }

                Complete();

                return _limitState?.Tcs.Task ?? TaskUtilities.CompletedTask;
            }
        }

        public void IncomingFin()
        {
            // Force a FIN
            WriteAsync(null, 0, 0);
        }

        private void Complete()
        {
            var awaitableState = Interlocked.Exchange(
                ref _awaitableState,
                _awaitableIsCompleted);

            _manualResetEvent.Set();

            if (!ReferenceEquals(awaitableState, _awaitableIsCompleted) &&
                !ReferenceEquals(awaitableState, _awaitableIsNotCompleted))
            {
                _threadPool.Run(awaitableState);
            }
        }

        public MemoryPoolIterator BeginRead()
        {
            if (Interlocked.CompareExchange(ref _consumingState, 1, 0) != 0)
            {
                throw new InvalidOperationException("Already consuming input.");
            }

            return new MemoryPoolIterator(_head);
        }

        public void EndRead(MemoryPoolIterator end)
        {
            EndRead(end, end);
        }

        public void EndRead(
            MemoryPoolIterator consumed,
            MemoryPoolIterator examined)
        {
            MemoryPoolBlock returnStart = null;
            MemoryPoolBlock returnEnd = null;

            lock (_sync)
            {
                if (!consumed.IsDefault)
                {
                    var lengthConsumed = new MemoryPoolIterator(_head).GetLength(consumed);

                    if (_limitState != null)
                    {
                        _limitState.Length -= lengthConsumed;

                        // Need to drain down to 1/2 to start the pipe again
                        if (_limitState.Length < (_threshold / 2))
                        {
                            _threadPool.Complete(_limitState.Tcs);
                        }
                    }

                    returnStart = _head;
                    returnEnd = consumed.Block;
                    _head = consumed.Block;
                    _head.Start = consumed.Index;
                }

                if (!examined.IsDefault &&
                    examined.IsEnd &&
                    RemoteIntakeFin == false &&
                    _awaitableError == null)
                {
                    _manualResetEvent.Reset();

                    Interlocked.CompareExchange(
                        ref _awaitableState,
                        _awaitableIsNotCompleted,
                        _awaitableIsCompleted);
                }
            }

            while (returnStart != returnEnd)
            {
                var returnBlock = returnStart;
                returnStart = returnStart.Next;
                returnBlock.Pool.Return(returnBlock);
            }

            if (Interlocked.CompareExchange(ref _consumingState, 0, 1) != 1)
            {
                throw new InvalidOperationException("No ongoing consuming operation to complete.");
            }
        }

        public void CompleteAwaiting()
        {
            Complete();
        }

        public void Cancel()
        {
            _awaitableError = new TaskCanceledException("The request was aborted");

            Complete();
        }

        public MemoryPoolChannel GetAwaiter()
        {
            return this;
        }

        public void OnCompleted(Action continuation)
        {
            var awaitableState = Interlocked.CompareExchange(
                ref _awaitableState,
                continuation,
                _awaitableIsNotCompleted);

            if (ReferenceEquals(awaitableState, _awaitableIsNotCompleted))
            {
                return;
            }
            else if (ReferenceEquals(awaitableState, _awaitableIsCompleted))
            {
                // Dispatch here to avoid stack diving
                _threadPool.Run(continuation);
            }
            else
            {
                _awaitableError = new InvalidOperationException("Concurrent reads are not supported.");

                Interlocked.Exchange(
                    ref _awaitableState,
                    _awaitableIsCompleted);

                _manualResetEvent.Set();

                _threadPool.Run(continuation);
                _threadPool.Run(awaitableState);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public void GetResult()
        {
            if (!IsCompleted)
            {
                _manualResetEvent.Wait();
            }
            var error = _awaitableError;
            if (error != null)
            {
                if (error is TaskCanceledException || error is InvalidOperationException)
                {
                    throw error;
                }
                throw new IOException(error.Message, error);
            }
        }

        public void Dispose()
        {
            Cancel();

            // Return all blocks
            var block = _head;
            while (block != null)
            {
                var returnBlock = block;
                block = block.Next;

                returnBlock.Pool.Return(returnBlock);
            }

            _head = null;
            _tail = null;
        }

        private class LimitState
        {
            public int Length { get; set; }

            public TaskCompletionSource<object> Tcs { get; set; } = new TaskCompletionSource<object>();
        }
    }
}
