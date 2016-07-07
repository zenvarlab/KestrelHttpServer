using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public class TcpListenerTransport : ITransport
    {
        private readonly List<Listener> _listeners = new List<Listener>();

        public void Initialize(ServiceContext serviceContext)
        {
        }

        public IDisposable CreateListener(ListenerContext listenerContext)
        {
            var listener = new Listener(listenerContext);
            _listeners.Add(listener);
            listener.Start(listenerContext.Address);
            return listener;
        }

        public void Dispose()
        {
            _listeners.ForEach(l => l.ShutdownConnections());
        }

        private class Listener : IDisposable
        {
#if !NET451
            public Listener(ListenerContext context)
            {
            }

            public async void Start(ServerAddress address)
            {
                throw new NotImplementedException();
            }

            public void ShutdownConnections()
            {
            }

            public void Dispose()
            {
            }

            public MemoryPool Pool => null;
#else
            private readonly ListenerContext _context;
            private TcpListener _listener;
            private CancellationTokenSource _cts = new CancellationTokenSource();
            private List<Task> _connections = new List<Task>();
            private readonly MemoryPool _pool;

            public Listener(ListenerContext context)
            {
                _context = context;
                _pool = new MemoryPool();
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

            private void StartConnectionAsync(ServerAddress address, Socket socket)
            {
                var connection = new SocketConnection(_pool, socket, _cts.Token);
                connection.ServerAddress = address;
                connection.RemoteEndPoint = socket.RemoteEndPoint as IPEndPoint;
                connection.LocalEndPoint = socket.LocalEndPoint as IPEndPoint;
                var connectionContext = _context.ConnectionInitializer.StartConnection(connection);
                _connections.Add(connection.Start(connectionContext));
            }

            public void ShutdownConnections()
            {
                _cts.Cancel();
                Task.WaitAll(_connections.ToArray());
                _pool.Dispose();
            }

            public void Dispose()
            {
                _listener.Stop();
            }
        }

        private class SocketConnection : IConnectionInformation
        {
            private readonly Socket _socket;
            private readonly CancellationToken _token;
            private readonly MemoryPool _pool;

            public SocketConnection(MemoryPool pool, Socket socket, CancellationToken token)
            {
                _pool = pool;
                _socket = socket;
                _token = token;
                _token.Register(state => Close(state), this);
            }

            private static void Close(object state)
            {
                ((SocketConnection)state)._socket.Close();
            }

            public async Task Start(IConnectionContext connectionContext)
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
                try
                {
                    while (true)
                    {
                        var end = context.InputChannel.BeginWrite();
                        var block = end.Block;

                        try
                        {
                            int bytesRead = await stream.ReadAsync(block.Array, block.End, block.Data.Offset + block.Data.Count - block.End);

                            if (bytesRead == 0)
                            {
                                await context.InputChannel.EndWriteAsync(end);
                                context.InputChannel.CompleteWriting();
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
                            if (_token.IsCancellationRequested || !_socket.Connected)
                            {
                                break;
                            }

                            context.InputChannel.CompleteWriting(error);
                            break;
                        }
                    }
                }
                catch (Exception)
                {

                }

                context.InputChannel.CompleteWriting();
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

                        var span = context.OutputChannel.BeginRead();
                        var start = span.Begin;
                        var end = span.End;

                        try
                        {
                            var block = start.Block;

                            if (end.IsDefault)
                            {
                                continue;
                            }

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
                            context.OutputChannel.EndRead(end);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // Aborted
                }

                _socket.Close();

                context.OutputChannel.CompleteReading();
            }

            public MemoryPool Pool => _pool;
#endif

            public IPEndPoint LocalEndPoint { get; set; }

            public IPEndPoint RemoteEndPoint { get; set; }

            public ServerAddress ServerAddress { get; set; }
        }
    }
}
