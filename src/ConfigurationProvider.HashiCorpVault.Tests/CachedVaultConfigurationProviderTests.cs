using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.HashiCorpVault;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VaultSharp.V1.Commons;
using Xunit;

namespace ConfigurationProvider.HashiCorpVault.Tests
{
    public class CachedVaultConfigurationProviderTests
    {
        private readonly string _testKey = "test-provider";
        private readonly VaultConnectionConfigSection _validConfig;
        private readonly Mock<IVaultService> _mockVaultService;
        private readonly Mock<IRefreshTimer> _mockRefreshTimer;
        private readonly Mock<ILogger> _mockLogger;

        public CachedVaultConfigurationProviderTests()
        {
            _validConfig = new VaultConnectionConfigSection
            {
                Address = "http://localhost:8200",
                Token = "test-token",
                EngineName = "secret",
                Path = "test/path",
                RefreshInterval = TimeSpan.FromMinutes(5),
                IsAsync = false
            };

            _mockVaultService = new Mock<IVaultService>();
            _mockRefreshTimer = new Mock<IRefreshTimer>();
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var provider = new CachedVaultConfigurationProvider(
                _testKey,
                _validConfig,
                _mockLogger.Object,
                _mockVaultService.Object,
                _mockRefreshTimer.Object
            );

            // Assert
            _mockRefreshTimer.Verify(x => x.Start(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.Is<bool>(sync => !_validConfig.IsAsync),
                It.Is<TimeSpan>(t => t == _validConfig.RefreshInterval)
            ), Times.Once);
        }

        [Fact]
        public void Load_WhenAsync_ShouldNotLoadImmediately()
        {
            // Arrange
            _validConfig.IsAsync = true;
            var provider = new CachedVaultConfigurationProvider(
                _testKey,
                _validConfig,
                _mockLogger.Object,
                _mockVaultService.Object,
                _mockRefreshTimer.Object
            );

            // Act
            provider.Load();

            // Assert
            _mockVaultService.Verify(x => x.ReadSecretsAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Load_WhenSync_ShouldLoadImmediately()
        {
            // Arrange
            _validConfig.IsAsync = false;
            var secretData = new Dictionary<string, object> { { "key1", "value1" } };
            var secret = new Secret<SecretData>
            {
                Data = new SecretData { Data = secretData }
            };

            _mockVaultService
                .Setup(x => x.ReadSecretsAsync(It.IsAny<string>()))
                .ReturnsAsync(secret);

            var provider = new CachedVaultConfigurationProvider(
                _testKey,
                _validConfig,
                _mockLogger.Object,
                _mockVaultService.Object,
                _mockRefreshTimer.Object
            );

            // Act
            provider.Load();

            // Assert
            _mockVaultService.Verify(x => x.ReadSecretsAsync(_validConfig.Path), Times.Once);
            Assert.Equal("value1", ((IConfigurationProvider)provider).TryGet("key1", out var value) ? value : null);
        }

        [Fact]
        public void Load_WithPrefix_ShouldPrependPrefixToKeys()
        {
            // Arrange
            _validConfig.IsAsync = false;
            _validConfig.Prefix = "test:";
            var secretData = new Dictionary<string, object> { { "key1", "value1" } };
            var secret = new Secret<SecretData>
            {
                Data = new SecretData { Data = secretData }
            };

            _mockVaultService
                .Setup(x => x.ReadSecretsAsync(It.IsAny<string>()))
                .ReturnsAsync(secret);

            var provider = new CachedVaultConfigurationProvider(
                _testKey,
                _validConfig,
                _mockLogger.Object,
                _mockVaultService.Object,
                _mockRefreshTimer.Object
            );

            // Act
            provider.Load();

            // Assert
            Assert.Equal("value1", ((IConfigurationProvider)provider).TryGet("test:key1", out var value) ? value : null);
        }

        [Fact]
        public async Task Dispose_ShouldCleanupResources()
        {
            // Arrange
            var stopCalled = false;
            var disposeAsyncCalled = false;

            _mockRefreshTimer
                .Setup(x => x.Stop(true))
                .Callback(() => stopCalled = true);

            _mockRefreshTimer
                .Setup(x => x.DisposeAsync())
                .Callback(() => disposeAsyncCalled = true)
                .Returns(new ValueTask());

            var provider = new CachedVaultConfigurationProvider(
                _testKey,
                _validConfig,
                _mockLogger.Object,
                _mockVaultService.Object,
                _mockRefreshTimer.Object
            );

            // Act
            await provider.DisposeAsync();

            // Assert
            Assert.True(stopCalled, "Stop was not called");
            Assert.True(disposeAsyncCalled, "DisposeAsync was not called");
        }

        [Fact]
        public async Task RefreshIfNeeded_WhenCancelled_ShouldNotRefresh()
        {
            // Arrange
            var provider = new CachedVaultConfigurationProvider(
                _testKey,
                _validConfig,
                _mockLogger.Object,
                _mockVaultService.Object,
                _mockRefreshTimer.Object
            );

            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Get the refresh callback that was registered with the timer
            Func<CancellationToken, Task> refreshCallback = null;
            _mockRefreshTimer.Verify(x => x.Start(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<bool>(),
                It.IsAny<TimeSpan>()
            ), Times.Once);

            _mockRefreshTimer
                .Setup(x => x.Start(
                    It.IsAny<Func<CancellationToken, Task>>(),
                    It.IsAny<bool>(),
                    It.IsAny<TimeSpan>()
                ))
                .Callback<Func<CancellationToken, Task>, bool, TimeSpan>((callback, _, __) => refreshCallback = callback);

            // Act
            if (refreshCallback != null)
            {
                await refreshCallback(cts.Token);
            }

            // Assert
            _mockVaultService.Verify(x => x.ReadSecretsAsync(It.IsAny<string>()), Times.Never);
        }
    }
}