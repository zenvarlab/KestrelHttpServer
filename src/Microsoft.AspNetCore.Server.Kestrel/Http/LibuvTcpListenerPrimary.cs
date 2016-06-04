// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    /// <summary>
    /// An implementation of <see cref="LibuvListenerPrimary"/> using TCP sockets.
    /// </summary>
    public class LibuvTcpListenerPrimary : LibuvListenerPrimary
    {
        public LibuvTcpListenerPrimary(ServiceContext serviceContext) : base(serviceContext)
        {
        }

        /// <summary>
        /// Creates the socket used to listen for incoming connections
        /// </summary>
        protected override UvStreamHandle CreateListenSocket()
        {
            var socket = new UvTcpHandle(Log);
            socket.Init(LibuvThread.Loop, LibuvThread.QueueCloseHandle);
            socket.NoDelay(ServerOptions.NoDelay);
            socket.Bind(ServerAddress);

            // If requested port was "0", replace with assigned dynamic port.
            ServerAddress.Port = socket.GetSockIPEndPoint().Port;

            socket.Listen(Constants.ListenBacklog, (stream, status, error, state) => ConnectionCallback(stream, status, error, state), this);
            return socket;
        }

        /// <summary>
        /// Handles an incoming connection
        /// </summary>
        /// <param name="listenSocket">Socket being used to listen on</param>
        /// <param name="status">Connection status</param>
        protected override void OnConnection(UvStreamHandle listenSocket, int status)
        {
            var acceptSocket = new UvTcpHandle(Log);

            try
            {
                acceptSocket.Init(LibuvThread.Loop, LibuvThread.QueueCloseHandle);
                acceptSocket.NoDelay(ServerOptions.NoDelay);
                listenSocket.Accept(acceptSocket);
                DispatchConnection(acceptSocket);

            }
            catch (UvException ex)
            {
                Log.LogError(0, ex, "TcpListenerPrimary.OnConnection");
                acceptSocket.Dispose();
                return;
            }
        }
    }
}
