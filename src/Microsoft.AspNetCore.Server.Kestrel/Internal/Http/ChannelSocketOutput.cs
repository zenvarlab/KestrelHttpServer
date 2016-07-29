// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public class ChannelSocketOutput : ISocketOutput
    {
        private readonly SocketInput _channel;
        private readonly Connection _connection;

        private readonly object _closeLock = new object();
        private bool _writing;
        private bool _outputClosed;

        public ChannelSocketOutput(
            KestrelThread libuvThread,
            MemoryPool memory,
            IThreadPool threadPool,
            UvStreamHandle socket,
            Connection connection,
            string connectionId,
            IKestrelTrace log)
        {
            // TODO: Backpressure
            _channel = new SocketInput(memory, threadPool, bufferSizeControl: null);
            _connection = connection;

            var libuvOutput = new LibuvOutput(libuvThread, socket, _channel, connectionId, log);
            libuvOutput.Start().ContinueWith((t, state) =>
            {
                // TODO: Log, wait, ...
                ((ChannelSocketOutput)state).ConsumerStopped();
            }, this);
        }

        public void Write(ArraySegment<byte> buffer, bool chunk = false)
        {
            lock (_closeLock)
            {
                if (_outputClosed)
                {
                    return;
                }

                _writing = true;
            }

            var startBlock = _channel.IncomingStart();
            var tail = new MemoryPoolIterator(startBlock, startBlock.End);

            if (chunk && buffer.Array != null)
            {
                ChunkWriter.WriteBeginChunkBytes(ref tail, buffer.Count);
            }

            tail.CopyFrom(buffer);

            if (chunk && buffer.Array != null)
            {
                ChunkWriter.WriteEndChunkBytes(ref tail);
            }

            _channel.IncomingComplete(tail);

            lock (_closeLock)
            {
                if (_outputClosed)
                {
                    ProducerStopped();
                }

                _writing = false;
            }
        }

        public Task WriteAsync(ArraySegment<byte> buffer, bool chunk = false,
            CancellationToken cancellationToken = new CancellationToken())
        {
            Write(buffer, chunk);
            return TaskUtilities.CompletedTask;
        }

        public MemoryPoolIterator ProducingStart()
        {
            lock (_closeLock)
            {
                if (_outputClosed)
                {
                    return default(MemoryPoolIterator);
                }

                _writing = true;
            }

            var block = _channel.IncomingStart();
            return new MemoryPoolIterator(block, block.End);
        }

        // TODO: Find a way to delay uv_write until Write call
        public void ProducingComplete(MemoryPoolIterator end)
        {
            _channel.IncomingComplete(end);

            lock (_closeLock)
            {
                if (_outputClosed)
                {
                    ProducerStopped();
                }

                _writing = false;
            }
        }

        public void End(ProduceEndType endType)
        {
            _channel.IncomingComplete(0, error: null);
        }

        private void ConsumerStopped()
        {
            lock (_closeLock)
            {
                if (!_writing)
                {
                    ProducerStopped();
                }

                _outputClosed = true;
            }
        }

        private void ProducerStopped()
        {
            _channel.Dispose();
            _connection.OnSocketClosed();
        }
    }
}
