// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    /// <summary>
    /// An implementation of <see cref="LibuvListenerSecondary"/> using UNIX sockets.
    /// </summary>
    public class PipeListenerSecondary : LibuvListenerSecondary
    {
        public PipeListenerSecondary(ServiceContext serviceContext) : base(serviceContext)
        {
        }

        /// <summary>
        /// Creates a socket which can be used to accept an incoming connection
        /// </summary>
        protected override UvStreamHandle CreateAcceptSocket()
        {
            var acceptSocket = new UvPipeHandle(Log);
            acceptSocket.Init(LibuvThread.Loop, LibuvThread.QueueCloseHandle, false);
            return acceptSocket;
        }
    }
}
