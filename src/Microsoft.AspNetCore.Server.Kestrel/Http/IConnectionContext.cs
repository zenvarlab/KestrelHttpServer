using System;
using System.Net;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public interface IConnectionContext
    {
        ServerAddress ServerAddress { get; set; }

        IConnectionControl ConnectionControl { get; set; }

        IPEndPoint RemoteEndPoint { get; set; }

        IPEndPoint LocalEndPoint { get; set; }

        MemoryPoolChannel FrameInputChannel { get; set; }

        MemoryPoolChannel FrameOutputChannel { get; set; }

        MemoryPoolChannel ConnectionInputChannel { get; set; }

        MemoryPoolChannel ConnectionOutputChannel { get; set; }

        string ConnectionId { get; set; }

        Action<IFeatureCollection> PrepareRequest { get; set; }
    }
}
