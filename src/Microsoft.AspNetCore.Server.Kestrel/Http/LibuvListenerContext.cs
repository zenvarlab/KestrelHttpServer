// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class LibuvListenerContext : ServiceContext
    {
        public LibuvListenerContext()
        {
        }

        public LibuvListenerContext(ServiceContext serviceContext)
            : base(serviceContext)
        {
            WriteReqPool = new Queue<UvWriteReq>(SocketOutput.MaxPooledWriteReqs);
        }

        public LibuvListenerContext(LibuvListenerContext listenerContext)
            : base(listenerContext)
        {
            ServerAddress = listenerContext.ServerAddress;
            LibuvThread = listenerContext.LibuvThread;
            Memory = listenerContext.Memory;
            ConnectionManager = listenerContext.ConnectionManager;
            WriteReqPool = listenerContext.WriteReqPool;
            Log = listenerContext.Log;
        }

        public LibuvThread LibuvThread { get; set; }

        public LibuvConnectionManager ConnectionManager { get; set; }

        public Queue<UvWriteReq> WriteReqPool { get; set; }
    }
}
