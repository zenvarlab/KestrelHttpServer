using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public interface IConnectionInitializer
    {
        /// <summary>
        /// Starts a connection
        /// </summary>
        /// <param name="connectionInformation"></param>
        /// <returns></returns>
        IConnectionContext StartConnection(IConnectionInformation connectionInformation);
    }
}
