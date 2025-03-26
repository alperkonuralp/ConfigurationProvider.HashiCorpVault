using Microsoft.Extensions.Configuration.HashiCorpVault;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ConfigurationProvider.HashiCorpVault.Tests
{
    public class DefaultRefreshTimerTests : IDisposable
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly string _testKey = "test-timer";
        private DefaultRefreshTimer _timer;
        private bool _callbackExecuted;
        private readonly SemaphoreSlim _semaphore;

        public DefaultRefreshTimerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _timer = new DefaultRefreshTimer(_testKey, _mockLogger.Object);
            _callbackExecuted = false;
            _semaphore = new SemaphoreSlim(0, 1);
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _semaphore?.Dispose();
        }

        [Fact]
        public async Task Start_WhenStartImmediately_ShouldExecuteCallbackRightAway()
        {
            // Arrange
            async Task Callback(CancellationToken token)
            {
                await Task.Yield(); // Add minimal await to satisfy compiler
                _callbackExecuted = true;
                _semaphore.Release();
            }

            // Act
            _timer.Start(Callback, true, TimeSpan.FromSeconds(10));

            // Assert
            await _semaphore.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.True(_callbackExecuted);
        }

        [Fact]
        public async Task Start_WhenNotStartImmediately_ShouldWaitForInterval()
        {
            // Arrange
            async Task Callback(CancellationToken token)
            {
                await Task.Yield();
                _callbackExecuted = true;
                _semaphore.Release();
            }

            // Act
            _timer.Start(Callback, false, TimeSpan.FromMilliseconds(100));

            // Assert - Should not execute immediately
            Assert.False(_callbackExecuted);
            
            // Wait for the interval and check if callback executed
            await _semaphore.WaitAsync(TimeSpan.FromMilliseconds(200));
            Assert.True(_callbackExecuted);
        }

        [Fact]
        public async Task Stop_ShouldPreventFurtherCallbacks()
        {
            // Arrange
            int callCount = 0;
            async Task Callback(CancellationToken token)
            {
                await Task.Yield();
                Interlocked.Increment(ref callCount);
                _semaphore.Release();
            }

            // Act
            _timer.Start(Callback, true, TimeSpan.FromMilliseconds(50));
            
            // Wait for first execution
            await _semaphore.WaitAsync(TimeSpan.FromMilliseconds(100));
            
            // Stop the timer
            _timer.Stop();
            
            // Wait to ensure no more callbacks
            await Task.Delay(200);

            // Assert
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task Callback_WhenThrowsException_ShouldNotCrashTimer()
        {
            // Arrange
            int callCount = 0;
            async Task Callback(CancellationToken token)
            {
                await Task.Yield();
                Interlocked.Increment(ref callCount);
                _semaphore.Release();
                throw new Exception("Test exception");
            }

            // Act
            _timer.Start(Callback, true, TimeSpan.FromMilliseconds(50));

            // Wait for two intervals
            await _semaphore.WaitAsync(TimeSpan.FromMilliseconds(100));
            await _semaphore.WaitAsync(TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.True(callCount >= 2, "Timer should continue despite exceptions");
        }

        [Fact]
        public async Task Dispose_ShouldPreventFurtherCallbacks()
        {
            // Arrange
            int callCount = 0;
            async Task Callback(CancellationToken token)
            {
                await Task.Yield();
                Interlocked.Increment(ref callCount);
                _semaphore.Release();
            }

            // Act
            _timer.Start(Callback, true, TimeSpan.FromMilliseconds(50));
            
            // Wait for first execution
            await _semaphore.WaitAsync(TimeSpan.FromMilliseconds(100));
            
            // Dispose the timer
            await _timer.DisposeAsync();
            
            // Wait to ensure no more callbacks
            await Task.Delay(200);

            // Assert
            Assert.Equal(1, callCount);
        }
    }
}