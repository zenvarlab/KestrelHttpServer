using System;
using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public interface IConnectionContext
    {
        string ConnectionId { get; }

        IWritableChannel InputChannel { get; }

        IReadableChannel OutputChannel { get; }
    }
}
