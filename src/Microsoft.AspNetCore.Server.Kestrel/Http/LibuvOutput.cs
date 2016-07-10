using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class LibuvOutput
    {
        private UvWriteReq _currentWriteReq;
        private MemoryPoolSpan _currentSpan;

        public LibuvOutput(
            LibuvThread libuvThread,
            UvStreamHandle socket,
            IReadableChannel channel,
            string connectionId,
            IKestrelTrace log)
        {
            LibuvThread = libuvThread;
            Socket = socket;
            Channel = channel;
            ConnectionId = connectionId;
            Log = log;
        }

        public IKestrelTrace Log { get; }

        public IReadableChannel Channel { get; }

        public UvStreamHandle Socket { get; }

        public LibuvThread LibuvThread { get; }

        public string ConnectionId { get; }

        public async Task Start()
        {
            try
            {
                while (!Channel.Completed)
                {
                    await Channel;

                    // Switch to the UV thread
                    await LibuvThread;

                    if (Socket.IsClosed)
                    {
                        break;
                    }

                    var span = Channel.BeginRead();
                    var start = span.Begin;
                    var end = span.End;

                    _currentSpan = span;

                    int bytes;
                    int buffers;
                    BytesBetween(start, end, out bytes, out buffers);

                    if (bytes == 0)
                    {
                        continue;
                    }

                    var req = LibuvThread.AllocateWriteReq();

                    _currentWriteReq = req;

                    try
                    {
                        int status = await req.Write(Socket, start, end, buffers);
                        Log.ConnectionWriteCallback(ConnectionId, status);
                    }
                    catch (Exception ex)
                    {
                        Log.ConnectionError(ConnectionId, ex);
                        break;
                    }
                    finally
                    {
                        Channel.EndRead(end);

                        _currentWriteReq = null;

                        // Return the request to the pool
                        LibuvThread.ReturnWriteRequest(req);
                    }
                }
            }
            catch (TaskCanceledException)
            {

            }
            finally
            {
                await LibuvThread;

                try
                {
                    if (!Socket.IsClosed)
                    {
                        var shutdownAwaitable = new LibuvAwaitable<UvShutdownReq>();
                        using (var shutdownReq = new UvShutdownReq(Log))
                        {
                            Log.ConnectionWriteFin(ConnectionId);
                            shutdownReq.Init(LibuvThread.Loop);
                            shutdownReq.Shutdown(Socket, LibuvAwaitable<UvShutdownReq>.Callback, shutdownAwaitable);
                            int status = await shutdownAwaitable;

                            Log.ConnectionWroteFin(ConnectionId, status);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Log.ConnectionError(ConnectionId, ex);
                }

                Channel.CompleteReading();

                Log.ConnectionStop(ConnectionId);
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
    }
}
