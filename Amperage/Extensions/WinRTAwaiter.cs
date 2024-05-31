// Based on https://gist.github.com/devhawk/834c08958b2c21883d66182b183a7ea0

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Amperage.Extensions
{
    static class WinRTAwaiter
    {
        public static TaskAwaiter<T> GetAwaiter<T, P>(this IAsyncOperationWithProgress<T, P> op)
        {
            var tcs = new TaskCompletionSource<T>();

            op.Completed = (IAsyncOperationWithProgress<T, P> asyncStatus, AsyncStatus unused) =>
            {
                switch (asyncStatus.Status)
                {
                    case AsyncStatus.Canceled:
                        tcs.SetCanceled();
                        break;
                    case AsyncStatus.Error:
                        tcs.SetException(asyncStatus.ErrorCode);
                        break;
                    case AsyncStatus.Completed:
                        tcs.SetResult(asyncStatus.GetResults());
                        break;
                }
            };

            return tcs.Task.GetAwaiter();
        }
    }
}