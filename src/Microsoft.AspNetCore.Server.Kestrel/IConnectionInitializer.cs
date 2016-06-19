using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public interface IConnectionInitializer
    {
        Task<IConnectionContext> StartConnectionAync(IConnectionInformation connectionInformation, ServiceContext serviceContext);
    }
}
