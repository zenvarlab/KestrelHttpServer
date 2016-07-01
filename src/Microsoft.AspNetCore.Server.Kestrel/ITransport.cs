using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public interface ITransport : IDisposable
    {
        void Initialize(ServiceContext serviceContext);
        IDisposable CreateListener(ListenerContext context);
    }

    public class ListenerContext
    {
        public ServerAddress Address { get; set; }
        public ServiceContext ServiceContext { get; set; }
        public MemoryPool Memory { get; set; }

        public IConnectionInitializer ConnectionInitializer { get; set; }
    }
}
