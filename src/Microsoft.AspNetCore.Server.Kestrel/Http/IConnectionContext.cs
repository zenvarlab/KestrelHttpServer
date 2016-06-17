using System;
using System.Net;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public interface IConnectionInformation
    {
        ServerAddress ServerAddress { get; }

        IPEndPoint RemoteEndPoint { get; }

        IPEndPoint LocalEndPoint { get; }
    }

    public interface IConnectionContext
    {
        string ConnectionId { get; }

        MemoryPoolChannel InputChannel { get; }

        MemoryPoolChannel OutputChannel { get; }
    }
}
