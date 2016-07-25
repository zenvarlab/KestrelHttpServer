// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Filter.Internal
{
    public class FilteredStreamAdapter
    {
        private readonly string _connectionId;
        private readonly Stream _filteredStream;
        private readonly IKestrelTrace _log;
        private readonly MemoryPool _memory;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public FilteredStreamAdapter(
            string connectionId,
            Stream filteredStream,
            MemoryPool memory,
            IKestrelTrace logger,
            IThreadPool threadPool,
            IBufferSizeControl bufferSizeControl)
        {
            SocketInput = new SocketInput(memory, threadPool, bufferSizeControl);
            SocketOutput = new StreamSocketOutput(connectionId, filteredStream, memory, logger);

            _connectionId = connectionId;
            _log = logger;
            _filteredStream = filteredStream;
            _memory = memory;
        }

        public SocketInput SocketInput { get; }

        public ISocketOutput SocketOutput { get; }

        public async Task ReadInputAsync()
        {
            var block = _memory.Lease();

            try
            {
                // Use pooled block for copy
                int bytesRead;
                while ((bytesRead = await _filteredStream.ReadAsync(block.Array, block.Data.Offset, block.Data.Count, _cts.Token)) != 0)
                {
                    SocketInput.IncomingData(block.Array, block.Data.Offset, bytesRead);
                }

                if (_cts.IsCancellationRequested)
                {
                    SocketInput.AbortAwaiting();
                }

                try
                {
                    SocketInput.IncomingFin();
                }
                catch (Exception ex)
                {
                    _log.LogError(0, ex, "FilteredStreamAdapter.SocketInput.IncomingFin()");
                }
            }
            catch (TaskCanceledException)
            {
                SocketInput.AbortAwaiting();
                _log.LogError("FilteredStreamAdapter.ReadInputAsync canceled.");
            }
            catch (Exception ex)
            {
                SocketInput.AbortAwaiting();
                _log.LogError(0, ex, "FilteredStreamAdapter.ReadInputAsync");
            }
            finally
            {
                _memory.Return(block);
            }
        }

        public void Abort()
        {
            _cts.Cancel();
        }
    }
}
