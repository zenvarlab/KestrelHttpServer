// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class LibuvConnectionContext : LibuvListenerContext, IConnectionInformation
    {
        public LibuvConnectionContext()
        {
        }

        public LibuvConnectionContext(LibuvListenerContext context) : base(context)
        {
        }

        public LibuvConnectionContext(LibuvConnectionContext context) : base(context)
        {
            RemoteEndPoint = context.RemoteEndPoint;
            LocalEndPoint = context.LocalEndPoint;
        }
        
        public IPEndPoint RemoteEndPoint { get; set; }

        public IPEndPoint LocalEndPoint { get; set; }

        public MemoryPool Pool => LibuvThread.Pool;
    }
}