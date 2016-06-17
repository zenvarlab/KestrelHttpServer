using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Http;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public interface IConnectionInitializer
    {
        Task<IConnectionContext> StartConnectionAync(IConnectionInformation connectionInformation, ServiceContext serviceContext);
    }
}
