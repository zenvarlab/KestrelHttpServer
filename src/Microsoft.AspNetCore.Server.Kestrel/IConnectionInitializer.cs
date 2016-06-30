using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public interface IConnectionInitializer
    {
        IConnectionContext StartConnection(IConnectionInformation connectionInformation, ServiceContext serviceContext);
    }
}
