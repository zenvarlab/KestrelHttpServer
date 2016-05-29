// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    /// <summary>
    /// Base class for listeners in Kestrel. Listens for incoming connections
    /// </summary>
    public abstract class Listener : ListenerContext, IAsyncDisposable
    {
        private bool _closed;

        protected Listener(ServiceContext serviceContext)
            : base(serviceContext)
        {
        }

        protected UvStreamHandle ListenSocket { get; private set; }

        public async Task StartAsync(
            ServerAddress address,
            KestrelThread thread)
        {
            ServerAddress = address;
            Thread = thread;
            ConnectionManager = new ConnectionManager(thread);

            await Thread;

            ListenSocket = CreateListenSocket();
        }

        /// <summary>
        /// Creates the socket used to listen for incoming connections
        /// </summary>
        protected abstract UvStreamHandle CreateListenSocket();

        protected static void ConnectionCallback(UvStreamHandle stream, int status, Exception error, object state)
        {
            var listener = (Listener)state;

            if (error != null)
            {
                listener.Log.LogError(0, error, "Listener.ConnectionCallback");
            }
            else if (!listener._closed)
            {
                listener.OnConnection(stream, status);
            }
        }

        /// <summary>
        /// Handles an incoming connection
        /// </summary>
        /// <param name="listenSocket">Socket being used to listen on</param>
        /// <param name="status">Connection status</param>
        protected abstract void OnConnection(UvStreamHandle listenSocket, int status);

        protected virtual void DispatchConnection(UvStreamHandle socket)
        {
            var connection = new Connection(this, socket);
            connection.Start();
        }

        public virtual async Task DisposeAsync()
        {
            // Ensure the event loop is still running.
            // If the event loop isn't running and we try to wait on this Post
            // to complete, then KestrelEngine will never be disposed and
            // the exception that stopped the event loop will never be surfaced.
            if (Thread.FatalError == null && ListenSocket != null)
            {
                await Thread;

                ListenSocket.Dispose();

                _closed = true;

                ConnectionManager.WalkConnectionsAndClose();

                await Task.Yield();

                await ConnectionManager.WaitForConnectionCloseAsync().ConfigureAwait(false);

                await Thread;

                while (WriteReqPool.Count > 0)
                {
                    WriteReqPool.Dequeue().Dispose();
                }

                await Task.Yield();
            }

            Memory.Dispose();
            ListenSocket = null;
        }
    }
}
