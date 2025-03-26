using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Configuration.HashiCorpVault
{
    /// <summary>
    /// Default implementation of IRefreshTimer using System.Threading.Timer
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the DefaultRefreshTimer class
    /// </remarks>
    /// <param name="key">A key to identify this timer in logs</param>
    /// <param name="logger">Optional logger instance</param>
    internal class DefaultRefreshTimer(string key, ILogger logger = null) : IRefreshTimer
    {
        private readonly string _key = key;
        private ILogger _logger = logger;
        private Timer _timer;
        private CancellationTokenSource _cts = new();
        private bool _disposedValue;

        private ILogger Logger => _logger ??= LoggerManager.LoggerFactory.CreateLogger("DefaultRefreshTimer-" + _key);

        /// <inheritdoc/>
        public void Start(Func<CancellationToken, Task> refreshCallback, bool startImmediately, TimeSpan refreshInterval)
        {
            if (_timer != null)
            {
                Stop(false);
            }

            Logger.LogDebug("'{Key}' Starting refresh timer with interval: {RefreshInterval}", _key, refreshInterval);

            _timer = new Timer(
                async _ => await ExecuteCallbackSafelyAsync(refreshCallback),
                null,
                startImmediately ? 0L : (long)refreshInterval.TotalMilliseconds,
                (long)refreshInterval.TotalMilliseconds
            );
        }

        /// <inheritdoc/>
        public void Stop(bool cancel = true)
        {
            Logger.LogDebug("'{Key}' Stopping refresh timer", _key);
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            // Cancel any ongoing operations
            if (cancel) _cts.Cancel();
        }

        private async Task ExecuteCallbackSafelyAsync(Func<CancellationToken, Task> callback)
        {
            if (_disposedValue || _cts.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await callback(_cts.Token);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "'{Key}' Error executing timer callback", _key);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Logger.LogDebug("'{Key}' Disposing timer resources", _key);

                    // Stop the timer first
                    Stop();

                    if (_cts?.IsCancellationRequested == false)
                    {
                        _cts.Cancel();
                    }

                    // Dispose resources
                    _timer?.Dispose();
                    _cts?.Dispose();

                    _timer = null;
                    _cts = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public ValueTask DisposeAsync()
        {
            if (!_disposedValue)
            {
                Logger.LogDebug("'{Key}' Asynchronously disposing timer resources", _key);

                // Stop the timer first
                Stop();

                if (_cts?.IsCancellationRequested == false)
                {
                    _cts.Cancel();
                }

                // Dispose resources
                _timer?.Dispose();
                _cts?.Dispose();

                _timer = null;
                _cts = null;

                _disposedValue = true;
            }

            return new ValueTask(Task.CompletedTask);
        }
    }
}