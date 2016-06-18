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
    public class TcpListenerTransport : ITransport
    {
        private ServiceContext _serviceContext;
        private IConnectionInitializer _initializer;

        public void Initialize(IConnectionInitializer initializer, ServiceContext serviceContext)
        {
            _initializer = initializer;
            _serviceContext = serviceContext;
        }

        public IDisposable CreateListener(ServerAddress address)
        {
            var listener = new Listener(_initializer, _serviceContext);
            listener.Start(address);

            return listener;
        }

        public void Dispose()
        {

        }

        private class Listener : IDisposable
        {
#if !NET451
            public Listener(IConnectionInitializer initializer, ServiceContext serviceContext)
            {
            }

            public async void Start(ServerAddress address)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
            }
#else
            private readonly ServiceContext _serviceContext;
            private readonly IConnectionInitializer _initializer;
            private TcpListener _listener;
            private CancellationTokenSource _cts = new CancellationTokenSource();

            public Listener(IConnectionInitializer initializer, ServiceContext serviceContext)
            {
                _initializer = initializer;
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
                        StartConnectionAsync(address, socket);
                    }
                    catch (ObjectDisposedException)
                    {
                        // We're done
                        break;
                    }
                }
            }

            private async void StartConnectionAsync(ServerAddress address, Socket socket)
            {
                var connection = new SocketConnection(socket, _cts.Token);
                connection.ServerAddress = address;
                connection.RemoteEndPoint = socket.RemoteEndPoint as IPEndPoint;
                connection.LocalEndPoint = socket.LocalEndPoint as IPEndPoint;
                var connectionContext = await _initializer.StartConnectionAync(connection, _serviceContext);
                connection.Start(connectionContext);
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
            private readonly CancellationToken _token;

            public SocketConnection(Socket socket, CancellationToken token)
            {
                _socket = socket;
                _token = token;
            }

            public async void Start(IConnectionContext connectionContext)
            {
                var stream = new NetworkStream(_socket);

                await Process(connectionContext, stream);
            }

            private async Task Process(IConnectionContext connectionContext, NetworkStream stream)
            {
                await Task.WhenAll(ProcessReads(connectionContext, stream), ProcessWrites(connectionContext, stream));
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

                stream.Dispose();
            }
#endif
            public string ConnectionId { get; set; }

            public IPEndPoint LocalEndPoint { get; set; }

            public IPEndPoint RemoteEndPoint { get; set; }

            public ServerAddress ServerAddress { get; set; }
        }
    }
}
