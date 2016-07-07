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

        MemoryPool Pool { get; }
    }

    public interface IConnectionContext
    {
        string ConnectionId { get; }

        IWritableChannel InputChannel { get; }

        IReadableChannel OutputChannel { get; }
    }
}
