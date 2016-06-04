// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    /// <summary>
    /// A secondary listener is delegated requests from a primary listener via a named pipe or 
    /// UNIX domain socket.
    /// </summary>
    public abstract class LibuvListenerSecondary : LibuvListenerContext, IAsyncDisposable
    {
        private string _pipeName;
        private IntPtr _ptr;
        private Libuv.uv_buf_t _buf;
        private bool _closed;

        protected LibuvListenerSecondary(ServiceContext serviceContext) : base(serviceContext)
        {
            _ptr = Marshal.AllocHGlobal(4);
        }

        UvPipeHandle DispatchPipe { get; set; }

        public Task StartAsync(
            string pipeName,
            ServerAddress address,
            LibuvThread thread)
        {
            _pipeName = pipeName;
            _buf = thread.Loop.Libuv.buf_init(_ptr, 4);

            ServerAddress = address;
            LibuvThread = thread;
            ConnectionManager = new LibuvConnectionManager(thread);

            DispatchPipe = new UvPipeHandle(Log);

            var tcs = new TaskCompletionSource<int>(this);
            LibuvThread.Post(state => StartCallback((TaskCompletionSource<int>)state), tcs);
            return tcs.Task;
        }

        private static void StartCallback(TaskCompletionSource<int> tcs)
        {
            var listener = (LibuvListenerSecondary)tcs.Task.AsyncState;
            listener.StartedCallback(tcs);
        }

        private void StartedCallback(TaskCompletionSource<int> tcs)
        {
            try
            {
                DispatchPipe.Init(LibuvThread.Loop, LibuvThread.QueueCloseHandle, true);
                var connect = new UvConnectRequest(Log);
                connect.Init(LibuvThread.Loop);
                connect.Connect(
                    DispatchPipe,
                    _pipeName,
                    (connect2, status, error, state) => ConnectCallback(connect2, status, error, (TaskCompletionSource<int>)state),
                    tcs);
            }
            catch (Exception ex)
            {
                DispatchPipe.Dispose();
                tcs.SetException(ex);
            }
        }

        private static void ConnectCallback(UvConnectRequest connect, int status, Exception error, TaskCompletionSource<int> tcs)
        {
            var listener = (LibuvListenerSecondary)tcs.Task.AsyncState;
            listener.ConnectedCallback(connect, status, error, tcs);
        }

        private void ConnectedCallback(UvConnectRequest connect, int status, Exception error, TaskCompletionSource<int> tcs)
        {
            connect.Dispose();
            if (error != null)
            {
                tcs.SetException(error);
                return;
            }

            try
            {
                DispatchPipe.ReadStart(
                    (handle, status2, state) => ((LibuvListenerSecondary)state)._buf,
                    (handle, status2, state) => ((LibuvListenerSecondary)state).ReadStartCallback(handle, status2),
                    this);

                tcs.SetResult(0);
            }
            catch (Exception ex)
            {
                DispatchPipe.Dispose();
                tcs.SetException(ex);
            }
        }

        private void ReadStartCallback(UvStreamHandle handle, int status)
        {
            if (status < 0)
            {
                if (status != Constants.EOF)
                {
                    Exception ex;
                    LibuvThread.Loop.Libuv.Check(status, out ex);
                    Log.LogError(0, ex, "DispatchPipe.ReadStart");
                }

                DispatchPipe.Dispose();
                return;
            }

            if (_closed || DispatchPipe.PendingCount() == 0)
            {
                return;
            }

            var acceptSocket = CreateAcceptSocket();

            try
            {
                DispatchPipe.Accept(acceptSocket);
            }
            catch (UvException ex)
            {
                Log.LogError(0, ex, "DispatchPipe.Accept");
                acceptSocket.Dispose();
                return;
            }

            var connection = new LibuvConnection(this, acceptSocket);
            connection.Start();
        }

        /// <summary>
        /// Creates a socket which can be used to accept an incoming connection
        /// </summary>
        protected abstract UvStreamHandle CreateAcceptSocket();

        private void FreeBuffer()
        {
            var ptr = Interlocked.Exchange(ref _ptr, IntPtr.Zero);
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public async Task DisposeAsync()
        {
            // Ensure the event loop is still running.
            // If the event loop isn't running and we try to wait on this Post
            // to complete, then KestrelEngine will never be disposed and
            // the exception that stopped the event loop will never be surfaced.
            if (LibuvThread.FatalError == null)
            {
                await LibuvThread;

                DispatchPipe.Dispose();

                FreeBuffer();

                _closed = true;

                ConnectionManager.WalkConnectionsAndClose();

                await ThreadPool;

                await ConnectionManager.WaitForConnectionCloseAsync().ConfigureAwait(false);

                await LibuvThread;

                while (WriteReqPool.Count > 0)
                {
                    WriteReqPool.Dequeue().Dispose();
                }

                await ThreadPool;
            }
            else
            {
                FreeBuffer();
            }

            Memory.Dispose();
        }
    }
}
