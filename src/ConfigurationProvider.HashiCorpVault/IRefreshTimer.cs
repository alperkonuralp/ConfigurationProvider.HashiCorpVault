using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Configuration.HashiCorpVault
{
    /// <summary>
    /// Interface for a timer that triggers refresh operations
    /// </summary>
    public interface IRefreshTimer : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Starts the timer with the specified refresh interval and callback
        /// </summary>
        /// <param name="refreshCallback">The callback to invoke when the timer elapses</param>
        /// <param name="startImmediately">Whether to trigger the callback immediately</param>
        /// <param name="refreshInterval">The interval at which to trigger the callback</param>
        void Start(Func<CancellationToken, Task> refreshCallback, bool startImmediately, TimeSpan refreshInterval);

        /// <summary>
        /// Stops the timer
        /// </summary>
        void Stop(bool cancel = true);
    }
}