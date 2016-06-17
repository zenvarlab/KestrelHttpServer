using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public interface ITransport : IDisposable
    {
        void Initialize(IConnectionInitializer initializer, ServiceContext serviceContext);

        IDisposable CreateListener(ServerAddress address);
    }
}
