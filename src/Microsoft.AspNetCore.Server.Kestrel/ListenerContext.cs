namespace Microsoft.AspNetCore.Server.Kestrel
{
    public class ListenerContext
    {
        public ServerAddress Address { get; set; }

        public IConnectionInitializer ConnectionInitializer { get; set; }
    }
}
