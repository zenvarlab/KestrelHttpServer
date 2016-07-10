using System;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public interface ITransport : IDisposable
    {
        void Initialize(ServiceContext serviceContext);
        IDisposable CreateListener(ListenerContext context);
    }
}
