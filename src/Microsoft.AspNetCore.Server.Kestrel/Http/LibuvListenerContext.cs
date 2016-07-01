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
        }

        public LibuvListenerContext(LibuvListenerContext listenerContext)
            : base(listenerContext)
        {
            ServerAddress = listenerContext.ServerAddress;
            LibuvThread = listenerContext.LibuvThread;
            Log = listenerContext.Log;
            ConnectionInitializer = listenerContext.ConnectionInitializer;
        }

        public ServerAddress ServerAddress { get; set; }

        public LibuvThread LibuvThread { get; set; }

        public IConnectionInitializer ConnectionInitializer { get; set; }
    }
}
