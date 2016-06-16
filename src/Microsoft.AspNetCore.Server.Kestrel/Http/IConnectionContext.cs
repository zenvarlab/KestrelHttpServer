using System;
using System.Net;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public interface IConnectionInformation
    {
        ServerAddress ServerAddress { get; set; }

        IPEndPoint RemoteEndPoint { get; set; }

        IPEndPoint LocalEndPoint { get; set; }

        string ConnectionId { get; set; }
    }

    public interface IConnectionContext
    {
        MemoryPoolChannel InputChannel { get; }

        MemoryPoolChannel OutputChannel { get; }
    }
}
