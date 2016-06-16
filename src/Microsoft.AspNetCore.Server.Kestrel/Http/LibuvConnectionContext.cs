// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class LibuvConnectionContext : LibuvListenerContext, IConnectionContext
    {
        public LibuvConnectionContext()
        {
        }

        public LibuvConnectionContext(LibuvListenerContext context) : base(context)
        {
        }

        public LibuvConnectionContext(LibuvConnectionContext context) : base(context)
        {
            ConnectionControl = context.ConnectionControl;
            RemoteEndPoint = context.RemoteEndPoint;
            LocalEndPoint = context.LocalEndPoint;
            ConnectionId = context.ConnectionId;
            FrameInputChannel = context.FrameInputChannel;
            FrameOutputChannel = context.FrameOutputChannel;
            InputChannel = context.InputChannel;
            OutputChannel = context.OutputChannel;
            PrepareRequest = context.PrepareRequest;
        }

        public IConnectionControl ConnectionControl { get; set; }

        public IPEndPoint RemoteEndPoint { get; set; }

        public IPEndPoint LocalEndPoint { get; set; }

        public MemoryPoolChannel FrameInputChannel { get; set; }

        public MemoryPoolChannel FrameOutputChannel { get; set; }

        public MemoryPoolChannel InputChannel { get; set; }

        public MemoryPoolChannel OutputChannel { get; set; }

        public string ConnectionId { get; set; }

        public Action<IFeatureCollection> PrepareRequest { get; set; }
    }
}