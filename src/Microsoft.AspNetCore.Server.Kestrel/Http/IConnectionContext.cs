using System;
using System.Net;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public interface IConnectionContext
    {
        ServerAddress ServerAddress { get; set; }

        IPEndPoint RemoteEndPoint { get; set; }

        IPEndPoint LocalEndPoint { get; set; }

        MemoryPoolChannel InputChannel { get; set; }

        MemoryPoolChannel OutputChannel { get; set; }

        string ConnectionId { get; set; }
    }
}
