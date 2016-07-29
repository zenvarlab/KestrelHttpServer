// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public class LibuvOutput
    {
        public LibuvOutput(
            KestrelThread libuvThread,
            UvStreamHandle socket,
            SocketInput channel,
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

        public SocketInput Channel { get; }

        public UvStreamHandle Socket { get; }

        public KestrelThread LibuvThread { get; }

        public string ConnectionId { get; }

        public async Task Start()
        {
            try
            {
                while (!Channel.CheckFinOrThrow())
                {
                    await Channel;

                    // Switch to the UV thread
                    await LibuvThread;

                    if (Socket.IsClosed)
                    {
                        break;
                    }

                    var start = Channel.ConsumingStart();

                    if (start.IsDefault)
                    {
                        Channel.ConsumingComplete(start, start);
                        continue;
                    }

                    int bytes;
                    int buffers;
                    MemoryPoolIterator end;
                    BytesBetween(start, out end, out bytes, out buffers);

                    var req = LibuvThread.WriteReqPool.Allocate();

                    try
                    {
                        if (bytes > 0)
                        {
                            int status = await req.Write(Socket, start, end, buffers);
                            Log.ConnectionWriteCallback(ConnectionId, status);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.ConnectionError(ConnectionId, ex);
                        break;
                    }
                    finally
                    {
                        Channel.ConsumingComplete(end, end);

                        // Return the request to the pool
                        LibuvThread.WriteReqPool.Return(req);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("**************** LibuvOutput ex: {0}", ex);
                throw;
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

                Socket.Dispose();

                Log.ConnectionStop(ConnectionId);
            }
        }

        private static void BytesBetween(MemoryPoolIterator start, out MemoryPoolIterator end, out int bytes, out int buffers)
        {
            bytes = 0;
            buffers = 0;

            var nextBlock = start.Block;

            MemoryPoolBlock block;
            int blockEnd;

            do
            {
                block = nextBlock;
                nextBlock = block.Next;

                blockEnd = block.End;

                bytes += blockEnd - block.Start;
                buffers++;
            } while (nextBlock != null);

            end = new MemoryPoolIterator(block, blockEnd);
        }
    }
}
