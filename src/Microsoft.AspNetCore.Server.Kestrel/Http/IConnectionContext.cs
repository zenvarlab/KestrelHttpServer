using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public interface IConnectionContext
    {
        IConnectionControl ConnectionControl { get; set; }

        IPEndPoint RemoteEndPoint { get; set; }

        IPEndPoint LocalEndPoint { get; set; }

        MemoryPoolChannel FrameInputChannel { get; set; }

        MemoryPoolChannel FrameOutputChannel { get; set; }

        // For connection filters
        MemoryPoolChannel ConnectionInputChannel { get; set; }

        MemoryPoolChannel ConnectionOutputChannel { get; set; }

        string ConnectionId { get; set; }

        Action<IFeatureCollection> PrepareRequest { get; set; }
    }
}
