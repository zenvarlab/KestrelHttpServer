// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
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

        public IDisposable CreateListener(ServerAddress address)
        {
            return _engine.CreateServer(address);
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }

        public void Initialize(ServiceContext serviceContext)
        {
            _engine = new LibuvEngine(serviceContext);
            _engine.Start(_threadCount);
        }
    }

    public class LibuvEngine : ServiceContext, IDisposable
    {
        public LibuvEngine(ServiceContext context)
            : this(new Libuv(), context)
        { }

        // For testing
        internal LibuvEngine(Libuv uv, ServiceContext context)
           : base(context)
        {
            Libuv = uv;
            Threads = new List<LibuvThread>();
        }

        public Libuv Libuv { get; private set; }
        public List<LibuvThread> Threads { get; private set; }

        public void Start(int count)
        {
            for (var index = 0; index < count; index++)
            {
                Threads.Add(new LibuvThread(this));
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

        public IDisposable CreateServer(ServerAddress address)
        {
            var listeners = new List<IAsyncDisposable>();

            var usingPipes = address.IsUnixPipe;

            try
            {
                var pipeName = (Libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");

                var single = Threads.Count == 1;
                var first = true;

                foreach (var thread in Threads)
                {
                    if (single)
                    {
                        var listener = usingPipes ?
                            (LibuvListener)new PipeListener(this) :
                            new LibuvTcpListener(this);
                        listeners.Add(listener);
                        listener.StartAsync(address, thread).Wait();
                    }
                    else if (first)
                    {
                        var listener = usingPipes
                            ? (LibuvListenerPrimary)new PipeListenerPrimary(this)
                            : new LibuvTcpListenerPrimary(this);

                        listeners.Add(listener);
                        listener.StartAsync(pipeName, address, thread).Wait();
                    }
                    else
                    {
                        var listener = usingPipes
                            ? (LibuvListenerSecondary)new PipeListenerSecondary(this)
                            : new LibuvTcpListenerSecondary(this);
                        listeners.Add(listener);
                        listener.StartAsync(pipeName, address, thread).Wait();
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

            if (!Task.WhenAll(disposeTasks).Wait(ServerOptions.ShutdownTimeout))
            {
                Log.NotAllConnectionsClosedGracefully();
            }
        }
    }
}
