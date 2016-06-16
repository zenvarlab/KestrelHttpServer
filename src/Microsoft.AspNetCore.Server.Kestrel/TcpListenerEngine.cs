using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Http;

namespace Microsoft.AspNetCore.Server.Kestrel
{
#if NET451
    public class TcpListenerEngine : IDisposable
    {
        private ServiceContext _serviceContext;
        public void Initialize(ServiceContext serviceContext)
        {
            _serviceContext = serviceContext;
        }

        public IDisposable CreateServer(ServerAddress address)
        {
            var listener = new TcpListener(IPAddress.Parse(address.Host), address.Port);
            listener.Start();

            Go(listener, address);

            return new DisposableAction(listener.Stop);
        }

        private async void Go(TcpListener listener, ServerAddress address)
        {
            while (true)
            {
                var socket = await listener.AcceptSocketAsync();

                var connection = new SocketConnection(socket, _serviceContext);
                connection.ServerAddress = address;
                connection.RemoteEndPoint = socket.RemoteEndPoint as IPEndPoint;
                connection.LocalEndPoint = socket.LocalEndPoint as IPEndPoint;
                connection.Start();
            }
        }

        public void Dispose()
        {

        }

        private class SocketConnection : IConnectionContext
        {
            private readonly Socket _socket;
            private readonly ServiceContext _serviceContext;

            public SocketConnection(Socket socket, ServiceContext serviceContext)
            {
                _socket = socket;
                _serviceContext = serviceContext;
            }

            public async void Start()
            {
                using (await _serviceContext.InitializeConnection(this, _serviceContext))
                {
                    var stream = new NetworkStream(_socket);

                    Go(stream);
                }
            }

            private async void Go(NetworkStream stream)
            {
                var t1 = DoReads(stream);
                var t2 = DoWrites(stream);

                await Task.WhenAny(t1, t2);
            }

            private async Task DoReads(NetworkStream stream)
            {
                while (true)
                {
                    var end = InputChannel.BeginWrite();
                    var block = end.Block;

                    try
                    {
                        int bytesRead = await stream.ReadAsync(block.Array, block.End, block.Data.Offset + block.Data.Count - block.End);

                        if (bytesRead == 0)
                        {
                            break;
                        }
                        else
                        {
                            end.UpdateEnd(bytesRead);
                            await InputChannel.EndWrite(end);
                        }
                    }
                    catch (Exception error)
                    {
                        await InputChannel.EndWrite(end, error);
                        break;
                    }
                }
            }

            private async Task DoWrites(NetworkStream stream)
            {
                while (!OutputChannel.Completed)
                {
                    await OutputChannel;

                    var start = OutputChannel.BeginRead();
                    var end = OutputChannel.End();

                    try
                    {
                        var block = start.Block;

                        while (true)
                        {
                            var blockStart = block == start.Block ? start.Index : block.Data.Offset;
                            var blockEnd = block == end.Block ? end.Index : block.Data.Offset + block.Data.Count;
                            var length = blockEnd - blockStart;

                            await stream.WriteAsync(block.Array, blockStart, length);

                            if (block == end.Block)
                            {
                                break;
                            }

                            block = block.Next;
                        }
                    }
                    catch (Exception)
                    {
                        // Handle
                        break;
                    }
                    finally
                    {
                        OutputChannel.EndRead(end);
                    }
                }
            }

            public string ConnectionId { get; set; }

            public MemoryPoolChannel InputChannel { get; set; }

            public MemoryPoolChannel OutputChannel { get; set; }

            public IPEndPoint LocalEndPoint { get; set; }

            public IPEndPoint RemoteEndPoint { get; set; }

            public ServerAddress ServerAddress { get; set; }
        }

        private class DisposableAction : IDisposable
        {
            private readonly Action _action;

            public DisposableAction(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }
    }
#endif

}
