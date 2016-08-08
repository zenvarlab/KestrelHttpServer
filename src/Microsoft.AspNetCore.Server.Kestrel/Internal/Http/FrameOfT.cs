// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public class Frame<TContext> : Frame
    {
        private readonly IHttpApplication<TContext> _application;

        public Frame(IHttpApplication<TContext> application,
                     ConnectionContext context)
            : base(context)
        {
            _application = application;
        }

        private static byte[] _request = Encoding.UTF8.GetBytes(@"GET /plaintext HTTP/1.1
Host: 10.0.0.100:5000

");
        private static byte[] _response = Encoding.UTF8.GetBytes(@"HTTP/1.1 200 OK
Date: Mon, 08 Aug 2016 16:15:23 GMT
Content-Length: 13
Content-Type: text/plain
Server: Kestrel

Hello, World!");

        /// <summary>
        /// Primary loop which consumes socket input, parses it for protocol framing, and invokes the
        /// application delegate for as long as the socket is intended to remain open.
        /// The resulting Task from this loop is preserved in a field which is used when the server needs
        /// to drain and close all currently active connections.
        /// </summary>
        public override async Task RequestProcessingAsync()
        {
            var requestSize = _request.Length;
            var requestBytes = new byte[requestSize];

            try
            {
                while (!_requestProcessingStopping)
                {
                    var read = 0;
                    while (!_requestProcessingStopping)
                    {
                        if (!SocketInput.CheckFinOrThrow())
                        {
                            await SocketInput;

                            var scan = SocketInput.ConsumingStart();
                            int actual;
                            scan = scan.CopyTo(requestBytes, read, requestSize - read, out actual);
                            read += actual;
                            SocketInput.ConsumingComplete(scan, scan);
                            if (read == requestSize)
                            {
                                await SocketOutput.WriteAsync(new ArraySegment<byte>(_response));
                                break;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }
            catch (BadHttpRequestException ex)
            {
                if (!_requestRejected)
                {
                    // SetBadRequestState logs the error.
                    SetBadRequestState(ex);
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning(0, ex, "Connection processing ended abnormally");
            }
            finally
            {
                try
                {
                    await TryProduceInvalidRequestResponse();

                    // If _requestAborted is set, the connection has already been closed.
                    if (Volatile.Read(ref _requestAborted) == 0)
                    {
                        ConnectionControl.End(ProduceEndType.SocketShutdown);
                    }
                }
                catch (Exception ex)
                {
                    Log.LogWarning(0, ex, "Connection shutdown abnormally");
                }
            }
        }
    }
}
