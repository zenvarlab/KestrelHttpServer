using System;
using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel
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
