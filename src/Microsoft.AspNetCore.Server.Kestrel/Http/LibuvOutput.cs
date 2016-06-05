using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class LibuvOutput
    {
        public LibuvOutput(
            LibuvThread libuvThread,
            UvStreamHandle socket,
            MemoryPoolChannel outputChannel,
            LibuvConnection connection,
            IKestrelTrace log,
            IThreadPool threadPool)
        {
            LibuvThread = libuvThread;
            Socket = socket;
            OutputChannel = outputChannel;
            Connection = connection;
            Log = log;
            ThreadPool = threadPool;
        }

        public IThreadPool ThreadPool { get; }

        public IKestrelTrace Log { get; }

        public MemoryPoolChannel OutputChannel { get; }

        public UvStreamHandle Socket { get; }

        public LibuvThread LibuvThread { get; }

        public LibuvConnection Connection { get; }

        public async void Start()
        {
            // Reuse the awaiter
            var awaitable = new LibuvAwaitable<UvWriteReq>();

            // Reuse the write request for all writes (is this ok?)
            using (var req = new UvWriteReq(Log))
            {
                req.Init(LibuvThread.Loop);

                try
                {
                    while (true)
                    {
                        await OutputChannel;

                        // Switch to the UV thread
                        await LibuvThread;

                        var start = OutputChannel.BeginRead();
                        var end = OutputChannel.End();

                        int bytes;
                        int buffers;
                        BytesBetween(start, end, out bytes, out buffers);

                        try
                        {
                            req.Write(Socket, start, end, buffers, LibuvAwaitable<UvWriteReq>.Callback, awaitable);
                            int status = await awaitable;
                            Log.ConnectionWriteCallback(Connection.ConnectionId, status);
                        }
                        catch (Exception ex)
                        {
                            // Abort the connection for any failed write
                            // Queued on threadpool so get it in as first op.
                            Connection.Abort();

                            Log.ConnectionError(Connection.ConnectionId, ex);
                        }
                        finally
                        {
                            OutputChannel.EndRead(end);
                        }

                        if (Socket.IsClosed)
                        {
                            break;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    await LibuvThread;

                    // Aborted the awaiter
                    var shutdownAwaitable = new LibuvAwaitable<UvShutdownReq>();
                    var shutdownReq = new UvShutdownReq(Log);
                    shutdownReq.Init(LibuvThread.Loop);
                    shutdownReq.Shutdown(Socket, LibuvAwaitable<UvShutdownReq>.Callback, shutdownAwaitable);
                    int status = await shutdownAwaitable;

                    Log.ConnectionWroteFin(Connection.ConnectionId, status);
                }
                finally
                {
                    Socket.Dispose();
                    Connection.OnSocketClosed();
                    OutputChannel.Dispose();

                    Log.ConnectionStop(Connection.ConnectionId);
                }
            }
        }
        public void Stop()
        {
            OutputChannel.Cancel();
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
    }
}
