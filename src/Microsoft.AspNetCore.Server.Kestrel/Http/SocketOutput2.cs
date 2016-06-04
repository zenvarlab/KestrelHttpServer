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
        private readonly Task _writeToLibuv;
        private readonly UvStreamHandle _socket;

        private Task _backOffTask = TaskUtilities.CompletedTask;
        private readonly KestrelThread _thread;
        private readonly IThreadPool _threadPool;

        public MemoryPoolAwaiter OutputAwaitable { get; }

        public SocketOutput2(KestrelThread thread,
            UvStreamHandle socket,
            MemoryPoolAwaiter outputAwaitable,
            Connection connection,
            IKestrelTrace log,
            IThreadPool threadPool)
        {
            _socket = socket;
            _thread = thread;
            _threadPool = threadPool;
            OutputAwaitable = outputAwaitable;
            _writeToLibuv = ProcessOutput(log, thread, connection, socket);
        }

        public void ProducingComplete(MemoryPoolIterator end)
        {
            OutputAwaitable.EndWrite(end);
        }

        public MemoryPoolIterator ProducingStart()
        {
            return OutputAwaitable.BeginWrite();
        }

        public void Write(ArraySegment<byte> buffer, bool chunk = false)
        {
            throw new NotImplementedException();
        }

        public async Task WriteAsync(ArraySegment<byte> buffer, bool chunk = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_socket.IsClosed)
            {
                return;
            }

            await _backOffTask;

            if (buffer.Count > 0)
            {
                var tail = OutputAwaitable.BeginWrite();
                if (tail.IsDefault)
                {
                    return;
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

                _backOffTask = OutputAwaitable.EndWrite(tail);
            }
        }

        private async Task ProcessOutput(IKestrelTrace log, KestrelThread thread, Connection connection, UvStreamHandle socket)
        {
            // Reuse the awaiter
            var awaitable = new UVAwaitable<UvWriteReq>();

            // Reuse the write request for all writes (is this ok?)
            using (var req = new UvWriteReq(log))
            {
                req.Init(thread.Loop);

                try
                {
                    while (true)
                    {
                        await OutputAwaitable;

                        // Switch to the UV thread
                        await thread;

                        var start = OutputAwaitable.BeginRead();
                        var end = OutputAwaitable.End();

                        int bytes;
                        int buffers;
                        BytesBetween(start, end, out bytes, out buffers);

                        try
                        {
                            req.Write(socket, start, end, buffers, UVAwaitable<UvWriteReq>.Callback, awaitable);
                            int status = await awaitable;
                            log.ConnectionWriteCallback(connection.ConnectionId, status);
                        }
                        catch (Exception ex)
                        {
                            // Abort the connection for any failed write
                            // Queued on threadpool so get it in as first op.
                            connection.Abort();

                            log.ConnectionError(connection.ConnectionId, ex);
                        }
                        finally
                        {
                            OutputAwaitable.EndRead(end);
                        }

                        if (_socket.IsClosed)
                        {
                            break;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    await thread;

                    // Aborted the awaiter
                    var shutdownAwaitable = new UVAwaitable<UvShutdownReq>();
                    var shutdownReq = new UvShutdownReq(log);
                    shutdownReq.Init(thread.Loop);
                    shutdownReq.Shutdown(socket, UVAwaitable<UvShutdownReq>.Callback, shutdownAwaitable);
                    int status = await shutdownAwaitable;

                    log.ConnectionWroteFin(connection.ConnectionId, status);
                }
                finally
                {
                    socket.Dispose();
                    connection.OnSocketClosed();
                    OutputAwaitable.Dispose();

                    log.ConnectionStop(connection.ConnectionId);
                }
            }
        }
        public void End(ProduceEndType endType)
        {
            switch (endType)
            {
                case ProduceEndType.SocketShutdown:
                case ProduceEndType.SocketDisconnect:
                    OutputAwaitable.AbortAwaiting();
                    break;
            }
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
                var exception = _exception;
                var status = _status;

                // Reset the awaitable state
                _exception = null;
                _status = 0;

                if (exception != null)
                {
                    throw exception;
                }

                return status;
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
