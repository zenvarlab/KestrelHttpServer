using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Filter;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public class ConnectionInitializer<TContext> : IConnectionInitializer, IDisposable
    {
        private readonly IHttpApplication<TContext> _application;

        // Base32 encoding - in ascii sort order for easy text based sorting
        private static readonly string _encode32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

        // Seed the _lastConnectionId for this application instance with
        // the number of 100-nanosecond intervals that have elapsed since 12:00:00 midnight, January 1, 0001
        // for a roughly increasing _requestId over restarts
        private static long _lastConnectionId = DateTime.UtcNow.Ticks;

        private List<Func<Task>> _stopFrame = new List<Func<Task>>();

        public ConnectionInitializer(IHttpApplication<TContext> application)
        {
            _application = application;
        }

        public async Task<IConnectionContext> StartConnectionAync(IConnectionInformation connectionInformation, ServiceContext serviceContext)
        {
            var connectionId = GenerateConnectionId(Interlocked.Increment(ref _lastConnectionId));

            var frame = new Frame<TContext>(_application, connectionInformation, serviceContext);

            var inputChannel = new MemoryPoolChannel(serviceContext.Memory, serviceContext.ThreadPool);
            var outputChannel = new MemoryPoolChannel(serviceContext.Memory, serviceContext.ThreadPool);

            frame.ConnectionId = connectionId;
            frame.InputChannel = inputChannel;
            frame.OutputChannel = outputChannel;

            _stopFrame.Add(() =>
            {
                var task = frame.StopAsync();
                inputChannel.CompleteAwaiting();
                return task;
            });


            if (serviceContext.ServerOptions.ConnectionFilter == null)
            {
                StartRequestProcessing(frame);
                return new ConnectionContext(connectionId, inputChannel, outputChannel);
            }

            var stream = new MemoryPoolChannelStream(inputChannel, outputChannel);

            var connectionFilterContext = new ConnectionFilterContext
            {
                Connection = stream,
                Address = connectionInformation.ServerAddress
            };

            await serviceContext.ServerOptions.ConnectionFilter.OnConnectionAsync(connectionFilterContext);

            frame.PrepareRequest = connectionFilterContext.PrepareRequest;

            if (connectionFilterContext.Connection != stream)
            {
                var streamConnection = new StreamConnection(
                    connectionId,
                    connectionFilterContext.Connection,
                    serviceContext.Memory,
                    serviceContext.Log,
                    serviceContext.ThreadPool);

                frame.OutputChannel = streamConnection.InputChannel;
                frame.InputChannel = streamConnection.OutputChannel;

                streamConnection.Start();
            }

            StartRequestProcessing(frame);
            return new ConnectionContext(connectionId, inputChannel, outputChannel);
        }

        private static async void StartRequestProcessing(Frame frame)
        {
            await frame.StartAsync();
        }

        private static unsafe string GenerateConnectionId(long id)
        {
            // The following routine is ~310% faster than calling long.ToString() on x64
            // and ~600% faster than calling long.ToString() on x86 in tight loops of 1 million+ iterations
            // See: https://github.com/aspnet/Hosting/pull/385

            // stackalloc to allocate array on stack rather than heap
            char* charBuffer = stackalloc char[13];

            charBuffer[0] = _encode32Chars[(int)(id >> 60) & 31];
            charBuffer[1] = _encode32Chars[(int)(id >> 55) & 31];
            charBuffer[2] = _encode32Chars[(int)(id >> 50) & 31];
            charBuffer[3] = _encode32Chars[(int)(id >> 45) & 31];
            charBuffer[4] = _encode32Chars[(int)(id >> 40) & 31];
            charBuffer[5] = _encode32Chars[(int)(id >> 35) & 31];
            charBuffer[6] = _encode32Chars[(int)(id >> 30) & 31];
            charBuffer[7] = _encode32Chars[(int)(id >> 25) & 31];
            charBuffer[8] = _encode32Chars[(int)(id >> 20) & 31];
            charBuffer[9] = _encode32Chars[(int)(id >> 15) & 31];
            charBuffer[10] = _encode32Chars[(int)(id >> 10) & 31];
            charBuffer[11] = _encode32Chars[(int)(id >> 5) & 31];
            charBuffer[12] = _encode32Chars[(int)id & 31];

            // string ctor overload that takes char*
            return new string(charBuffer, 0, 13);
        }

        public void Dispose()
        {
            var tasks = new Task[_stopFrame.Count];

            for (int i = 0; i < tasks.Length; i++)
            {
                var frame = _stopFrame[i];
                tasks[i] = frame();
            }

            Task.WaitAll(tasks);
        }

        private class ConnectionContext : IConnectionContext
        {
            public ConnectionContext(string connectionId, MemoryPoolChannel input, MemoryPoolChannel output)
            {
                ConnectionId = connectionId;
                InputChannel = input;
                OutputChannel = output;
            }

            public string ConnectionId { get; private set; }

            public IWritableChannel InputChannel { get; private set; }

            public IReadableChannel OutputChannel { get; private set; }
        }
    }
}
