using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
            var listener = new Listener(_serviceContext);
            listener.Start(address);

            return listener;
        }

        public void Dispose()
        {

        }

        private class Listener : IDisposable
        {
            private readonly ServiceContext _serviceContext;
            private TcpListener _listener;
            private CancellationTokenSource _cts = new CancellationTokenSource();

            public Listener(ServiceContext serviceContext)
            {
                _serviceContext = serviceContext;
            }

            public async void Start(ServerAddress address)
            {
                _listener = new TcpListener(IPAddress.Parse(address.Host), address.Port);
                _listener.Start();

                while (true)
                {
                    try
                    {
                        var socket = await _listener.AcceptSocketAsync();

                        var connection = new SocketConnection(socket, _serviceContext, _cts.Token);
                        connection.ServerAddress = address;
                        connection.RemoteEndPoint = socket.RemoteEndPoint as IPEndPoint;
                        connection.LocalEndPoint = socket.LocalEndPoint as IPEndPoint;
                        connection.Start();
                    }
                    catch (ObjectDisposedException)
                    {
                        // We're done
                        break;
                    }
                }
            }

            public void Dispose()
            {
                _cts.Cancel();
                _listener.Stop();
            }
        }

        private class SocketConnection : IConnectionInformation
        {
            private readonly Socket _socket;
            private readonly ServiceContext _serviceContext;
            private readonly CancellationToken _token;

            public SocketConnection(Socket socket, ServiceContext serviceContext, CancellationToken token)
            {
                _socket = socket;
                _serviceContext = serviceContext;
                _token = token;
            }

            public async void Start()
            {
                var connectionContext = await _serviceContext.StartConnectionAsync(this, _serviceContext);

                var stream = new NetworkStream(_socket);

                await Process(connectionContext, stream);
            }

            private async Task Process(IConnectionContext connectionContext, NetworkStream stream)
            {
                await Task.WhenAny(ProcessReads(connectionContext, stream), ProcessWrites(connectionContext, stream));
            }

            private async Task ProcessReads(IConnectionContext context, NetworkStream stream)
            {
                while (true)
                {
                    var end = context.InputChannel.BeginWrite();
                    var block = end.Block;

                    try
                    {
                        int bytesRead = await stream.ReadAsync(block.Array, block.End, block.Data.Offset + block.Data.Count - block.End, _token);

                        if (bytesRead == 0)
                        {
                            context.InputChannel.Completed = true;
                            await context.InputChannel.EndWriteAsync(end);
                            break;
                        }
                        else
                        {
                            end.UpdateEnd(bytesRead);
                            await context.InputChannel.EndWriteAsync(end);
                        }
                    }
                    catch (Exception error)
                    {
                        await context.InputChannel.EndWriteAsync(end, error);
                        break;
                    }
                }
            }

            private async Task ProcessWrites(IConnectionContext context, NetworkStream stream)
            {
                try
                {
                    while (!context.OutputChannel.Completed)
                    {
                        await context.OutputChannel;

                        if (context.OutputChannel.Completed)
                        {
                            break;
                        }

                        var start = context.OutputChannel.BeginRead();
                        var end = context.OutputChannel.End();

                        try
                        {
                            var block = start.Block;

                            while (true)
                            {
                                var blockStart = block == start.Block ? start.Index : block.Data.Offset;
                                var blockEnd = block == end.Block ? end.Index : block.Data.Offset + block.Data.Count;
                                var length = blockEnd - blockStart;

                                await stream.WriteAsync(block.Array, blockStart, length, _token);

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
                            context.OutputChannel.EndRead(end);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // Aborted
                }

                _socket.Dispose();
            }

            public string ConnectionId { get; set; }

            public IPEndPoint LocalEndPoint { get; set; }

            public IPEndPoint RemoteEndPoint { get; set; }

            public ServerAddress ServerAddress { get; set; }
        }
    }
#endif

}
