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
        private IThreadPool _threadPool;

        public LibuvConnectionManager(LibuvThread thread, IThreadPool threadPool)
        {
            _thread = thread;
            _threadPool = threadPool;
        }

        public async Task WaitForConnectionCloseAsync()
        {
            await _thread;

            var connectionStopTasks = new List<Task>();

            _thread.Walk(ptr =>
            {
                var handle = UvMemory.FromIntPtr<UvHandle>(ptr);
                var connection = (handle as UvStreamHandle)?.Connection;

                if (connection != null)
                {
                    connectionStopTasks.Add(connection.StopAsync());
                }
            });

            await _threadPool;

            await Task.WhenAll(connectionStopTasks).ConfigureAwait(false);
        }
    }
}
