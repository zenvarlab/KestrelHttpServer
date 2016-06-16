using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public class LibuvInput
    {
        private static readonly Action<UvStreamHandle, int, object> _readCallback =
            (handle, status, state) => ReadCallback(handle, status, state);
        private static readonly Func<UvStreamHandle, int, object, Libuv.uv_buf_t> _allocCallback =
            (handle, suggestedsize, state) => AllocCallback(handle, suggestedsize, state);

        private MemoryPoolIterator _iterator;

        private TaskCompletionSource<object> _tcs;

        public LibuvInput(
            LibuvThread libuvThread,
            UvStreamHandle socket,
            MemoryPoolChannel inputChannel,
            LibuvConnection connection,
            IKestrelTrace log,
            IThreadPool threadPool)
        {
            LibuvThread = libuvThread;
            Socket = socket;
            InputChannel = inputChannel;
            Connection = connection;
            Log = log;
            ThreadPool = threadPool;
        }

        public IThreadPool ThreadPool { get; }

        public IKestrelTrace Log { get; }

        public MemoryPoolChannel InputChannel { get; }

        public UvStreamHandle Socket { get; }

        public LibuvThread LibuvThread { get; }

        public LibuvConnection Connection { get; private set; }

        public Task Start()
        {
            if (_tcs != null)
            {
                return _tcs.Task;
            }
            _tcs = new TaskCompletionSource<object>();
            Resume();
            return _tcs.Task;
        }

        private void Resume()
        {
            Socket.ReadStart(_allocCallback, _readCallback, this);
        }

        private void Stop()
        {
            Log.ConnectionPause(Connection.ConnectionId);
            Socket.ReadStop();
        }

        private static Libuv.uv_buf_t AllocCallback(UvStreamHandle handle, int suggestedSize, object state)
        {
            return ((LibuvInput)state).OnAlloc(handle, suggestedSize);
        }

        private Libuv.uv_buf_t OnAlloc(UvStreamHandle handle, int suggestedSize)
        {
            _iterator = InputChannel.BeginWrite();
            var result = _iterator.Block;

            return handle.Libuv.buf_init(
                result.DataArrayPtr + result.End,
                result.Data.Offset + result.Data.Count - result.End);
        }

        private static void ReadCallback(UvStreamHandle handle, int status, object state)
        {
            ((LibuvInput)state).OnRead(handle, status);
        }

        private async void OnRead(UvStreamHandle handle, int status)
        {
            if (status == 0)
            {
                // A zero status does not indicate an error or connection end. It indicates
                // there is no data to be read right now.
                return;
            }

            var normalRead = status > 0;
            var normalDone = status == Constants.ECONNRESET || status == Constants.EOF;
            var errorDone = !(normalDone || normalRead);
            var readCount = normalRead ? status : 0;

            if (normalRead)
            {
                Log.ConnectionRead(Connection.ConnectionId, readCount);
            }
            else
            {
                Socket.ReadStop();

                Log.ConnectionReadFin(Connection.ConnectionId);
            }

            Exception error = null;
            if (errorDone)
            {
                handle.Libuv.Check(status, out error);
            }

            if (readCount == 0)
            {
                InputChannel.Completed = true;
                _tcs.TrySetResult(null);
            }
            else
            {
                _iterator.UpdateEnd(readCount);
            }

            var task = InputChannel.EndWriteAsync(_iterator, error);
            _iterator = default(MemoryPoolIterator);

            if (errorDone)
            {
                _tcs.TrySetException(error);
            }
            else
            {
                if (!task.IsCompleted)
                {
                    Stop();

                    // Wait so we can re-open the flood gates
                    await task;

                    // Get back onto the UV thread
                    await LibuvThread;

                    // Resume pumping data from the socket
                    Resume();
                }

            }
        }

    }

}
