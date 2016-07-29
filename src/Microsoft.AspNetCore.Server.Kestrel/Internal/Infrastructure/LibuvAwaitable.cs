using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure
{
    public class LibuvAwaitable<TRequest> : ICriticalNotifyCompletion where TRequest : UvRequest
    {
        private static readonly Action CALLBACK_RAN = () => { };

        private Action _callback;

        private Exception _exception;

        private int _status;

        public static Action<TRequest, int, object> Callback = (req, status, state) =>
        {
            var awaitable = (LibuvAwaitable<TRequest>)state;

            awaitable._status = status;

            if (status < 0)
            {
                Exception exception;
                req.Libuv.Check(status, out exception);
                awaitable._exception = exception;
            }

            var continuation = Interlocked.Exchange(ref awaitable._callback, CALLBACK_RAN);

            continuation?.Invoke();
        };

        public LibuvAwaitable<TRequest> GetAwaiter() => this;
        public bool IsCompleted => _callback == CALLBACK_RAN;

        public int GetResult()
        {
            var exception = _exception;
            var status = _status;

            // Reset the awaitable state
            _exception = null;
            _status = 0;
            _callback = null;

            if (exception != null)
            {
                throw exception;
            }

            return status;
        }

        public void OnCompleted(Action continuation)
        {
            if (_callback == CALLBACK_RAN ||
                Interlocked.CompareExchange(ref _callback, continuation, null) == CALLBACK_RAN)
            {
                Task.Run(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }
    }

}
