using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;
using Microsoft.AspNetCore.Server.KestrelTests.TestHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class ConnectionTests
    {
        [Fact]
        public void DoesNotEndConnectionOnZeroRead()
        {
            var mockLibuv = new MockLibuv();
            
            using (var memory = new MemoryPool())
            using (var engine = new LibuvEngine(mockLibuv, new TestServiceContext()))
            {
                engine.Start(count: 1);

                var trace = new TestKestrelTrace();
                var context = new LibuvListenerContext(new TestServiceContext())
                {
                    FrameFactory = (connectionContext, sc) => new Frame<HttpContext>(
                        new DummyApplication(httpContext => TaskUtilities.CompletedTask), connectionContext, sc),
                    Memory = memory,
                    ServerAddress = ServerAddress.FromUrl("http://127.0.0.1:0"),
                    LibuvThread = engine.Threads[0]
                };
                var socket = new MockSocket(mockLibuv, Thread.CurrentThread.ManagedThreadId, trace);
                var connection = new LibuvConnection(context, socket);
                connection.Start();

                Libuv.uv_buf_t ignored;
                mockLibuv.AllocCallback(socket.InternalGetHandle(), 2048, out ignored);
                mockLibuv.ReadCallback(socket.InternalGetHandle(), 0, ref ignored);
                Assert.False(connection.InputChannel.RemoteIntakeFin);

                connection.ConnectionControl.End(ProduceEndType.SocketDisconnect);
            }
        }
    }
}
