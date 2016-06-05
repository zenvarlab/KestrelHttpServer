// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class LibuvConnection : LibuvConnectionContext, IConnectionControl
    {
        // Base32 encoding - in ascii sort order for easy text based sorting
        private static readonly string _encode32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

        // Seed the _lastConnectionId for this application instance with
        // the number of 100-nanosecond intervals that have elapsed since 12:00:00 midnight, January 1, 0001
        // for a roughly increasing _requestId over restarts
        private static long _lastConnectionId = DateTime.UtcNow.Ticks;

        private readonly UvStreamHandle _socket;
        private Frame _frame;

        private LibuvOutput _output;
        private LibuvInput _input;

        private readonly object _stateLock = new object();
        private ConnectionState _connectionState;
        private TaskCompletionSource<object> _socketClosedTcs;

        public LibuvConnection(LibuvListenerContext context, UvStreamHandle socket) : base(context)
        {
            _socket = socket;
            socket.Connection = this;
            ConnectionControl = this;

            ConnectionId = GenerateConnectionId(Interlocked.Increment(ref _lastConnectionId));
        }

        // Internal for testing
        internal LibuvConnection()
        {
        }

        public async void Start()
        {
            Log.ConnectionStart(ConnectionId);

            try
            {
                await InitializeConnection(this, this);
            }
            catch
            {
                ConnectionControl.End(ProduceEndType.SocketDisconnect);
                return;
            }

            _input = new LibuvInput(LibuvThread, _socket, ConnectionInputChannel, this, Log, ThreadPool);
            _output = new LibuvOutput(LibuvThread, _socket, ConnectionOutputChannel, this, Log, ThreadPool);

            _input.Start();
            _output.Start();

            var tcpHandle = _socket as UvTcpHandle;
            if (tcpHandle != null)
            {
                RemoteEndPoint = tcpHandle.GetPeerIPEndPoint();
                LocalEndPoint = tcpHandle.GetSockIPEndPoint();
            }

            lock (_stateLock)
            {
                if (_connectionState != ConnectionState.CreatingFrame)
                {
                    throw new InvalidOperationException("Invalid connection state: " + _connectionState);
                }

                _connectionState = ConnectionState.Open;

                _frame = CreateFrame();
                _frame.Start();
            }
        }

        public Task StopAsync()
        {
            lock (_stateLock)
            {
                switch (_connectionState)
                {
                    case ConnectionState.SocketClosed:
                        return TaskUtilities.CompletedTask;
                    case ConnectionState.CreatingFrame:
                        _connectionState = ConnectionState.ToDisconnect;
                        break;
                    case ConnectionState.Open:
                        _frame.Stop();
                        ConnectionInputChannel.CompleteAwaiting();
                        break;
                }

                _socketClosedTcs = new TaskCompletionSource<object>();
                return _socketClosedTcs.Task;
            }
        }

        public virtual void Abort()
        {
            // Frame.Abort calls user code while this method is always
            // called from a libuv thread.
            ThreadPool.Run(() =>
            {
                var connection = this;

                lock (connection._stateLock)
                {
                    if (connection._connectionState == ConnectionState.CreatingFrame)
                    {
                        connection._connectionState = ConnectionState.ToDisconnect;
                    }
                    else
                    {
                        connection._frame?.Abort();
                    }
                }
            });
        }

        // Called on Libuv thread
        public virtual void OnSocketClosed()
        {
            ConnectionInputChannel.Dispose();

            lock (_stateLock)
            {
                _connectionState = ConnectionState.SocketClosed;

                if (_socketClosedTcs != null)
                {
                    // This is always waited on synchronously, so it's safe to
                    // call on the libuv thread.
                    _socketClosedTcs.TrySetResult(null);
                }
            }
        }

        private Frame CreateFrame()
        {
            return FrameFactory(this, this);
        }

        void IConnectionControl.End(ProduceEndType endType)
        {
            switch (endType)
            {
                case ProduceEndType.ConnectionKeepAlive:
                    if (_connectionState != ConnectionState.Open)
                    {
                        return;
                    }

                    Log.ConnectionKeepAlive(ConnectionId);
                    break;
                case ProduceEndType.SocketShutdown:
                case ProduceEndType.SocketDisconnect:
                    lock (_stateLock)
                    {
                        if (_connectionState == ConnectionState.Disconnecting ||
                            _connectionState == ConnectionState.SocketClosed)
                        {
                            return;
                        }
                        _connectionState = ConnectionState.Disconnecting;

                        Log.ConnectionDisconnect(ConnectionId);
                        _output?.Stop();
                        break;
                    }
            }
        }

        private static unsafe string GenerateConnectionId(long id)
        {
            // The following routine is ~310% faster than calling long.ToString() on x64
            // and ~600% faster than calling long.ToString() on x86 in tight loops of 1 million+ iterations
            // See: https://github.com/aspnet/Hosting/pull/385

            // stackalloc to allocate array on stack rather than heap
            char* charBuffer = stackalloc char[13];

            charBuffer[0] = _encode32Chars[(int)(id >> 60) & 31];
            charBuffer[1] = _encode32Chars[(int)(id >> 55) & 31];
            charBuffer[2] = _encode32Chars[(int)(id >> 50) & 31];
            charBuffer[3] = _encode32Chars[(int)(id >> 45) & 31];
            charBuffer[4] = _encode32Chars[(int)(id >> 40) & 31];
            charBuffer[5] = _encode32Chars[(int)(id >> 35) & 31];
            charBuffer[6] = _encode32Chars[(int)(id >> 30) & 31];
            charBuffer[7] = _encode32Chars[(int)(id >> 25) & 31];
            charBuffer[8] = _encode32Chars[(int)(id >> 20) & 31];
            charBuffer[9] = _encode32Chars[(int)(id >> 15) & 31];
            charBuffer[10] = _encode32Chars[(int)(id >> 10) & 31];
            charBuffer[11] = _encode32Chars[(int)(id >> 5) & 31];
            charBuffer[12] = _encode32Chars[(int)id & 31];

            // string ctor overload that takes char*
            return new string(charBuffer, 0, 13);
        }

        private enum ConnectionState
        {
            CreatingFrame,
            ToDisconnect,
            Open,
            Disconnecting,
            SocketClosed
        }
    }
}
