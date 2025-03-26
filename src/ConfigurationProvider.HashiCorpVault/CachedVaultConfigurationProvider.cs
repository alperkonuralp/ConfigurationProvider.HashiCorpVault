using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Configuration.HashiCorpVault;

internal sealed class CachedVaultConfigurationProvider : ConfigurationProvider, IDisposable, IAsyncDisposable
{
    private readonly IVaultService _vaultService;
    private readonly IRefreshTimer _refreshTimer;
    private readonly string _key;
    private readonly VaultConnectionConfigSection _config;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private ILogger _logger;
    private DateTime _lastRefresh;
    private bool disposedValue;
    private TimeSpan _waitInterval;
    private static readonly TimeSpan MAX_TIMEOUT = TimeSpan.FromSeconds(20);

    private ILogger Logger => _logger ??= LoggerManager.LoggerFactory.CreateLogger("CachedVaultConfigurationProvider-" + _key);

    /// <summary>
    /// Constructor with custom timer implementation (for testing)
    /// </summary>
    internal CachedVaultConfigurationProvider(string key, VaultConnectionConfigSection config, ILogger logger = null, IVaultService vaultService = null, IRefreshTimer refreshTimer = null)
    {
        _key = key;
        _config = config;
        _logger = logger;
        _vaultService = vaultService ?? new VaultService(key, config);
        _refreshTimer = refreshTimer ?? new DefaultRefreshTimer(key, logger);

        _waitInterval = _config.RefreshInterval < MAX_TIMEOUT ? _config.RefreshInterval : MAX_TIMEOUT;

        Logger.LogDebug("'{Key}' Initializing CachedVaultConfigurationProvider with refresh interval: {RefreshInterval}", _key, _config.RefreshInterval);

        _refreshTimer.Start(RefreshIfNeeded, !_config.IsAsync, _config.RefreshInterval);
    }

    public override void Load()
    {
        if (_config.IsAsync)
        {
            Logger.LogInformation("'{Key}' Configuration will be loaded asynchronously from Vault", _key);
            return;
        }

        Logger.LogInformation("'{Key}' Synchronously loading configuration from Vault", _key);
        RefreshIfNeeded(CancellationToken.None).GetAwaiter().GetResult();
    }

    private async Task RefreshIfNeeded(CancellationToken cancellationToken)
    {
        // Skip refresh if already disposed
        if (disposedValue || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            if (!await _semaphore.WaitAsync(_waitInterval, cancellationToken)) return;

            // Check again after acquiring the semaphore in case disposal happened during wait
            if (disposedValue || cancellationToken.IsCancellationRequested)
            {
                _semaphore.Release();
                return;
            }

            Logger.LogDebug("'{Key}' Starting configuration refresh at {Time} [LastRefresh:{LastRefresh}]", _key, DateTime.Now, _lastRefresh);

            await RefreshConfiguration(cancellationToken);

            _lastRefresh = DateTime.Now;
            Logger.LogDebug("'{Key}' Completed configuration refresh at {Time} [LastRefresh:{LastRefresh}]", _key, _lastRefresh, _lastRefresh);
        }
        catch (ObjectDisposedException)
        {
            // Ignore if semaphore has been disposed - we're shutting down
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "'{Key}' Error in RefreshIfNeeded [LastRefresh:{LastRefresh}]", _key, _lastRefresh);
        }
        finally
        {
            if (!disposedValue && _semaphore != null)
            {
                try
                {
                    _semaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                    // Ignore if already disposed
                }
            }
        }
    }

    private async Task RefreshConfiguration(CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogDebug("'{Key}' Refreshing Vault configuration from path: {EngineName}/{Path} [LastRefresh:{LastRefresh}]", _key, _config.EngineName, _config.Path, _lastRefresh);

            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogDebug("'{Key}' Refresh operation cancelled", _key);
                return;
            }
            var secret = await _vaultService.ReadSecretsAsync(_config.Path);

            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogDebug("'{Key}' Refresh operation cancelled after reading secret", _key);
                return;
            }

            Logger.LogDebug("'{Key}' Retrieved {Count} configuration values from Vault", _key, secret.Data.Data.Count);

            if (string.IsNullOrWhiteSpace(_config.Prefix))
            {
                Data = secret.Data.Data.ToDictionary(x => x.Key, x => x.Value.ToString());
            }
            else
            {
                Data = secret.Data.Data.ToDictionary(x => _config.Prefix + x.Key, x => x.Value.ToString());
                Logger.LogDebug("'{Key}' Added prefix '{Prefix}' to configuration keys", _key, _config.Prefix);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogDebug("'{Key}' Refresh operation cancelled before reload", _key);
                return;
            }

            OnReload();
            Logger.LogInformation("'{Key}' Successfully refreshed configuration from Vault path: {EngineName}/{Path} [LastRefresh:{LastRefresh}]", _key, _config.EngineName, _config.Path, _lastRefresh);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "'{Key}' Error refreshing configuration from Vault path: {EngineName}/{Path} [LastRefresh:{LastRefresh}]", _key, _config.EngineName, _config.Path, _lastRefresh);
        }
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Logger.LogDebug("'{Key}' Disposing CachedVaultConfigurationProvider resources, path: {EngineName}/{Path}", _key, _config.EngineName, _config.Path);

                // Stop the timer first to prevent new callbacks
                _refreshTimer?.Stop();

                // Wait briefly to ensure no operations are in progress
                try
                {
                    if (_semaphore.Wait(100))
                    {
                        _semaphore.Release();
                    }
                }
                catch
                {
                    // Ignore any exceptions during disposal
                }

                // Dispose resources
                _refreshTimer?.Dispose();
                _semaphore?.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (!disposedValue)
        {
            Logger.LogDebug("'{Key}' Asynchronously disposing CachedVaultConfigurationProvider resources, path: {EngineName}/{Path}", _key, _config.EngineName, _config.Path);

            // Stop the timer first to prevent new callbacks
            _refreshTimer?.Stop();

            // Wait briefly to ensure no operations are in progress
            try
            {
                if (await _semaphore.WaitAsync(100))
                {
                    _semaphore.Release();
                }
            }
            catch
            {
                // Ignore any exceptions during disposal
            }

            // Dispose resources
            if (_refreshTimer != null)
            {
                await _refreshTimer.DisposeAsync();
            }

            _semaphore?.Dispose();

            disposedValue = true;
        }
    }
}