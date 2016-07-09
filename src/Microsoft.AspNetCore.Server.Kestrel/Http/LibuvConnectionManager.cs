// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class LibuvConnectionManager
    {
        private LibuvThread _thread;
        private List<Task> _connectionStopTasks;
        private TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
        private IThreadPool _threadPool;

        public LibuvConnectionManager(LibuvThread thread, IThreadPool threadPool)
        {
            _thread = thread;
            _threadPool = threadPool;
        }

        // This must be called on the libuv event loop
        public void WalkConnectionsAndClose()
        {
            if (_connectionStopTasks != null)
            {
                throw new InvalidOperationException($"{nameof(WalkConnectionsAndClose)} cannot be called twice.");
            }

            _connectionStopTasks = new List<Task>();

            _thread.Walk(ptr =>
            {
                var handle = UvMemory.FromIntPtr<UvHandle>(ptr);
                var connection = (handle as UvStreamHandle)?.Connection;

                if (connection != null)
                {
                    _connectionStopTasks.Add(connection.StopAsync());
                }
            });

            _threadPool.Complete(_tcs);
        }

        public async Task WaitForConnectionCloseAsync()
        {
            await _tcs.Task.ConfigureAwait(false);

            await Task.WhenAll(_connectionStopTasks).ConfigureAwait(false);
        }
    }
}
