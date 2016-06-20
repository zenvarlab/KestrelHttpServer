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
        private Task _processingTask;

        public LibuvConnection(LibuvListenerContext context, UvStreamHandle socket) : base(context)
        {
            _socket = socket;
            socket.Connection = this;
        }

        // Internal for testing
        internal LibuvConnection()
        {
        }

        public void Start()
        {
            _processingTask = StartProcessing();
        }

        private async Task StartProcessing()
        {
            var tcpHandle = _socket as UvTcpHandle;
            if (tcpHandle != null)
            {
                RemoteEndPoint = tcpHandle.GetPeerIPEndPoint();
                LocalEndPoint = tcpHandle.GetSockIPEndPoint();
            }

            var context = await ConnectionInitializer.StartConnectionAync(this, this);

            var input = new LibuvInput(LibuvThread, _socket, context.InputChannel, context.ConnectionId, Log, ThreadPool);
            var output = new LibuvOutput(LibuvThread, _socket, context.OutputChannel, context.ConnectionId, Log, ThreadPool, WriteReqPool);

            var inputTask = input.Start();
            var outputTask = output.Start();

            await outputTask;
        }

        public Task StopAsync()
        {
            return _processingTask ?? TaskUtilities.CompletedTask;
        }
    }
}
