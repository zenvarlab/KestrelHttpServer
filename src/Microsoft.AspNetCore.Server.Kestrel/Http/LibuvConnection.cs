// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class LibuvConnection : LibuvConnectionContext
    {
        private readonly UvStreamHandle _socket;

        public LibuvConnection(LibuvListenerContext context, UvStreamHandle socket) : base(context)
        {
            _socket = socket;
            socket.Connection = this;
        }

        // Internal for testing
        internal LibuvConnection()
        {
        }

        public async void Start()
        {
            Log.ConnectionStart(ConnectionId);

            var tcpHandle = _socket as UvTcpHandle;
            if (tcpHandle != null)
            {
                RemoteEndPoint = tcpHandle.GetPeerIPEndPoint();
                LocalEndPoint = tcpHandle.GetSockIPEndPoint();
            }

            var context = await StartConnectionAsync(this, this);

            var input = new LibuvInput(LibuvThread, _socket, context.InputChannel, this, Log, ThreadPool);
            var output = new LibuvOutput(LibuvThread, _socket, context.OutputChannel, this, Log, ThreadPool, WriteReqPool);

            var inputTask = input.Start();
            var outputTask = output.Start();

            await Task.WhenAny(inputTask, outputTask);
        }

        public Task StopAsync()
        {
            //lock (_stateLock)
            //{
            //    switch (_connectionState)
            //    {
            //        case ConnectionState.SocketClosed:
            //            return TaskUtilities.CompletedTask;
            //        case ConnectionState.CreatingFrame:
            //            _connectionState = ConnectionState.ToDisconnect;
            //            break;
            //        case ConnectionState.Open:
            //            _frame.Stop();
            //            ConnectionInputChannel.CompleteAwaiting();
            //            break;
            //    }

            //    _socketClosedTcs = new TaskCompletionSource<object>();
            //    return _socketClosedTcs.Task;
            //}
            return TaskUtilities.CompletedTask;
        }

        public virtual void Abort()
        {
            // Frame.Abort calls user code while this method is always
            // called from a libuv thread.
            //ThreadPool.Run(() =>
            //{
            //    var connection = this;

            //    lock (connection._stateLock)
            //    {
            //        if (connection._connectionState == ConnectionState.CreatingFrame)
            //        {
            //            connection._connectionState = ConnectionState.ToDisconnect;
            //        }
            //        else
            //        {
            //            connection._frame?.Abort();
            //        }
            //    }
            //});
        }
    }
}
