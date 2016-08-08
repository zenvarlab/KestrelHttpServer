// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal
{
    public class KestrelEngine : ServiceContext, IDisposable
    {
        private Thread _postThread;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public KestrelEngine(ServiceContext context)
            : this(new Libuv(), context)
        { }

        // For testing
        internal KestrelEngine(Libuv uv, ServiceContext context)
           : base(context)
        {
            Libuv = uv;
            Threads = new List<KestrelThread>();
        }

        public Libuv Libuv { get; private set; }
        public List<KestrelThread> Threads { get; private set; }

        public void Start(int count)
        {
            for (var index = 0; index < count; index++)
            {
                Threads.Add(new KestrelThread(this));
            }
            _postThread = new Thread(() =>
            {
                var token = _cancellationTokenSource.Token;
                while (!token.IsCancellationRequested)
                {
                    foreach (var kestrelThread in Threads)
                    {
                        kestrelThread.SignalPost();
                    }
                }
            })
            {
                IsBackground = true
            };
            foreach (var thread in Threads)
            {
                thread.StartAsync().Wait();
            }
            _postThread.Start();
        }

        public void Dispose()
        {
            Task.WaitAll(Threads.Select(thread => thread.StopAsync(TimeSpan.FromSeconds(2.5))).ToArray());
            _cancellationTokenSource.Cancel();
            Threads.Clear();
#if DEBUG
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
#endif
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
                            (Listener) new PipeListener(this) :
                            new TcpListener(this);
                        listeners.Add(listener);
                        listener.StartAsync(address, thread).Wait();
                    }
                    else if (first)
                    {
                        var listener = usingPipes
                            ? (ListenerPrimary) new PipeListenerPrimary(this)
                            : new TcpListenerPrimary(this);

                        listeners.Add(listener);
                        listener.StartAsync(pipeName, address, thread).Wait();
                    }
                    else
                    {
                        var listener = usingPipes
                            ? (ListenerSecondary) new PipeListenerSecondary(this)
                            : new TcpListenerSecondary(this);
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
            var disposeTasks = listeners.Select(listener => listener.DisposeAsync()).ToArray();

            if (!Task.WaitAll(disposeTasks, TimeSpan.FromSeconds(2.5)))
            {
                Log.LogError(0, null, "Disposing listeners failed");
            }
        }
    }
}
