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

        private const int MinimumSize = 2048;

        public LibuvInput(
            LibuvThread libuvThread,
            UvStreamHandle socket,
            IWritableChannel inputChannel,
            string connectionId,
            IKestrelTrace log,
            IThreadPool threadPool)
        {
            LibuvThread = libuvThread;
            Socket = socket;
            InputChannel = inputChannel;
            ConnectionId = connectionId;
            Log = log;
            ThreadPool = threadPool;
        }

        public IThreadPool ThreadPool { get; }

        public IKestrelTrace Log { get; }

        public IWritableChannel InputChannel { get; }

        public UvStreamHandle Socket { get; }

        public LibuvThread LibuvThread { get; }

        public string ConnectionId { get; private set; }

        public void Start()
        {
            Resume();
        }

        private void Resume()
        {
            Socket.ReadStart(_allocCallback, _readCallback, this);
        }

        private void Stop()
        {
            Log.ConnectionPause(ConnectionId);
            Socket.ReadStop();
        }

        private static Libuv.uv_buf_t AllocCallback(UvStreamHandle handle, int suggestedSize, object state)
        {
            return ((LibuvInput)state).OnAlloc(handle, suggestedSize);
        }

        private Libuv.uv_buf_t OnAlloc(UvStreamHandle handle, int suggestedSize)
        {
            _iterator = InputChannel.BeginWrite(MinimumSize);
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
                Log.ConnectionRead(ConnectionId, readCount);
            }
            else
            {
                Socket.ReadStop();

                Log.ConnectionReadFin(ConnectionId);
            }

            Exception error = null;
            if (errorDone)
            {
                handle.Libuv.Check(status, out error);
            }

            _iterator.UpdateEnd(readCount);
            var task = InputChannel.EndWriteAsync(_iterator);

            if (readCount == 0)
            {
                InputChannel.CompleteWriting();
            }

            _iterator = default(MemoryPoolIterator);

            if (errorDone)
            {
                InputChannel.CompleteWriting(error);
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
