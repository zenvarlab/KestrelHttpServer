using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class SocketOutput2 : ISocketOutput
    {
        private readonly SocketInput _socketInput;
        private readonly Task _writeToLibuv;
        private readonly UvStreamHandle _socket;

        public SocketInput SocketInput => _socketInput;

        public SocketOutput2(KestrelThread thread,
            UvStreamHandle socket,
            MemoryPool memory,
            Connection connection,
            string connectionId,
            IKestrelTrace log,
            IThreadPool threadPool)
        {
            _socket = socket;
            _socketInput = new SocketInput(memory);
            _writeToLibuv = ProcessOutput(log, thread, connection, socket);
        }

        public void ProducingComplete(MemoryPoolIterator end)
        {
            SocketInput.IncomingComplete(end);
        }

        public MemoryPoolIterator ProducingStart()
        {
            return new MemoryPoolIterator(SocketInput.IncomingStart());
        }

        public void Write(ArraySegment<byte> buffer, bool chunk = false)
        {
            throw new NotImplementedException();
        }

        public Task WriteAsync(ArraySegment<byte> buffer, bool chunk = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_socket.IsClosed)
            {
                _socketInput.RemoteIntakeFin = true;
                return TaskUtilities.CompletedTask;
            }

            if (buffer.Count > 0)
            {
                // Use a task queue

                var tail = ProducingStart();
                if (tail.IsDefault)
                {
                    return TaskUtilities.CompletedTask;
                }

                if (chunk)
                {
                    ChunkWriter.WriteBeginChunkBytes(ref tail, buffer.Count);
                }

                tail.CopyFrom(buffer);

                if (chunk)
                {
                    ChunkWriter.WriteEndChunkBytes(ref tail);
                }

                ProducingComplete(tail);

                // Handle back pressure here
            }

            return TaskUtilities.CompletedTask;
        }

        private async Task ProcessOutput(IKestrelTrace log, KestrelThread thread, Connection connection, UvStreamHandle socket)
        {
            // Reuse the awaiter
            var awaitable = new UVAwaitable<UvWriteReq>();

            // Reuse the write request for all writes (is this ok?)
            using (var req = new UvWriteReq(log))
            {
                req.Init(thread.Loop);

                while (true)
                {
                    await thread;

                    var start = SocketInput.ConsumingStart();
                    var end = start.ReadToEnd();
                    int bytes;
                    int buffers;
                    BytesBetween(start, end, out bytes, out buffers);

                    try
                    {
                        req.Write(socket, start, end, buffers, UVAwaitable<UvWriteReq>.Callback, awaitable);
                        await awaitable;
                    }
                    catch
                    {
                        // Abort the connection for any failed write
                        // Queued on threadpool so get it in as first op.
                        connection.Abort();
                    }
                    finally
                    {
                        // REVIEW: Should this happen on the thread pool thread?
                        SocketInput.ConsumingComplete(end, end);
                    }

                    if (_socket.IsClosed)
                    {
                        break;
                    }

                    await SocketInput;
                }
            }

            var shutdownAwaitable = new UVAwaitable<UvShutdownReq>();
            var shutdownReq = new UvShutdownReq(log);
            shutdownReq.Shutdown(socket, UVAwaitable<UvShutdownReq>.Callback, shutdownAwaitable);
            await shutdownAwaitable;

            // REVIEW: Who is responsible for disposing the socket output?
            SocketInput.Dispose();
            socket.Dispose();
            connection.OnSocketClosed();
        }

        private static void BytesBetween(MemoryPoolIterator start, MemoryPoolIterator end, out int bytes, out int buffers)
        {
            if (start.Block == end.Block)
            {
                bytes = end.Index - start.Index;
                buffers = 1;
                return;
            }

            bytes = start.Block.Data.Offset + start.Block.Data.Count - start.Index;
            buffers = 1;

            for (var block = start.Block.Next; block != end.Block; block = block.Next)
            {
                bytes += block.Data.Count;
                buffers++;
            }

            bytes += end.Index - end.Block.Data.Offset;
            buffers++;
        }

        public class UVAwaitable<TRequest> : ICriticalNotifyCompletion where TRequest : UvRequest
        {
            private readonly static Action CALLBACK_RAN = () => { };

            private Action _callback;

            private Exception _exception;

            private int _status;

            public static Action<TRequest, int, object> Callback = (req, status, state) =>
            {
                var awaitable = (UVAwaitable<TRequest>)state;

                Exception exception;
                req.Libuv.Check(status, out exception);
                awaitable._exception = exception;

                awaitable._status = status;

                var continuation = Interlocked.Exchange(ref awaitable._callback, CALLBACK_RAN);

                continuation?.Invoke();
            };

            public UVAwaitable<TRequest> GetAwaiter() => this;
            public bool IsCompleted => _callback == CALLBACK_RAN;

            public int GetResult()
            {
                if (_exception != null)
                {
                    throw _exception;
                }

                return _status;
            }

            public void OnCompleted(Action continuation)
            {
                if (_callback == CALLBACK_RAN ||
                    Interlocked.CompareExchange(ref _callback, continuation, null) == CALLBACK_RAN)
                {
                    Task.Run(continuation);
                }
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                OnCompleted(continuation);
            }
        }

    }
}
