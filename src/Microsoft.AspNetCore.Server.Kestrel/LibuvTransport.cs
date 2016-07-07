// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public class LibuvTransport : ITransport
    {
        private LibuvEngine _engine;
        private readonly int _threadCount;

        public LibuvTransport(int threadCount)
        {
            _threadCount = threadCount;
        }

        public void Initialize(ServiceContext serviceContext)
        {
            _engine = new LibuvEngine(serviceContext);
            _engine.Start(_threadCount);
        }

        public IDisposable CreateListener(ListenerContext listenerContext)
        {
            return _engine.CreateServer(listenerContext);
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }
    }

    public class LibuvEngine : IDisposable
    {
        private ServiceContext _serviceContext;
        public LibuvEngine(ServiceContext serviceContext)
            : this(new Libuv(), serviceContext)
        { }

        // For testing
        internal LibuvEngine(Libuv uv, ServiceContext serviceContext)
        {
            Libuv = uv;
            _serviceContext = serviceContext;
            Threads = new List<LibuvThread>();
        }

        public Libuv Libuv { get; private set; }
        public List<LibuvThread> Threads { get; private set; }

        public void Start(int count)
        {
            for (var index = 0; index < count; index++)
            {
                Threads.Add(new LibuvThread(this, _serviceContext));
            }

            foreach (var thread in Threads)
            {
                thread.StartAsync().Wait();
            }
        }

        public void Dispose()
        {
            foreach (var thread in Threads)
            {
                thread.Stop(TimeSpan.FromSeconds(2.5));
            }

            Threads.Clear();
        }

        public IDisposable CreateServer(ListenerContext listenerContext)
        {
            var listeners = new List<IAsyncDisposable>();

            var address = listenerContext.Address;
            var usingPipes = address.IsUnixPipe;

            try
            {
                var pipeName = (Libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");

                var single = Threads.Count == 1;
                var first = true;

                for (int i = 0; i < Threads.Count; i++)
                {
                    var thread = Threads[i];

                    if (single)
                    {
                        var listener = usingPipes ?
                            (LibuvListener)new PipeListener(listenerContext.ServiceContext) :
                            new LibuvTcpListener(listenerContext.ServiceContext);
                        listeners.Add(listener);
                        listener.StartAsync(address, thread, listenerContext.ConnectionInitializer).Wait();
                    }
                    else if (first)
                    {
                        var listener = usingPipes
                            ? (LibuvListenerPrimary)new PipeListenerPrimary(listenerContext.ServiceContext)
                            : new LibuvTcpListenerPrimary(listenerContext.ServiceContext);

                        listeners.Add(listener);
                        listener.StartAsync(pipeName, address, thread, listenerContext.ConnectionInitializer).Wait();
                    }
                    else
                    {
                        var listener = usingPipes
                            ? (LibuvListenerSecondary)new PipeListenerSecondary(listenerContext.ServiceContext)
                            : new LibuvTcpListenerSecondary(listenerContext.ServiceContext);
                        listeners.Add(listener);
                        listener.StartAsync(pipeName, address, thread, listenerContext.ConnectionInitializer).Wait();
                    }

                    first = false;
                }

                return new Disposable(() =>
                {
                    DisposeListeners(listeners);
                });
            }
            catch
            {
                DisposeListeners(listeners);

                throw;
            }
        }

        private void DisposeListeners(List<IAsyncDisposable> listeners)
        {
            var disposeTasks = new List<Task>();

            foreach (var listener in listeners)
            {
                disposeTasks.Add(listener.DisposeAsync());
            }

            // TODO: Add timeouts back
            Task.WhenAll(disposeTasks).Wait();
        }
    }
}
