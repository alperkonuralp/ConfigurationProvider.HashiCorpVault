using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.HashiCorpVault;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace ConfigurationProvider.HashiCorpVault.Tests
{
    public class VaultConfigurationSourceTests
    {
        private readonly string _testKey = "test-source";
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IConfigurationBuilder> _mockBuilder;

        public VaultConfigurationSourceTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockBuilder = new Mock<IConfigurationBuilder>();
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
        {
            // Act
            var source = new VaultConfigurationSource(_testKey, _mockLogger.Object);

            // Assert
            Assert.NotNull(source);
        }

        [Fact]
        public void Build_WhenCalledMultipleTimes_ShouldReturnSameCachedProvider()
        {
            // Arrange
            var source = new VaultConfigurationSource(_testKey, _mockLogger.Object);
            SetupValidConfiguration();

            // Act
            var provider1 = source.Build(_mockBuilder.Object);
            var provider2 = source.Build(_mockBuilder.Object);

            // Assert
            Assert.Same(provider1, provider2);
        }

        [Fact]
        public void Build_WithMissingKey_ShouldThrowArgumentException()
        {
            // Arrange
            var source = new VaultConfigurationSource(_testKey, _mockLogger.Object);
            SetupConfigurationWithoutKey();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => source.Build(_mockBuilder.Object));
            Assert.Contains(_testKey, exception.Message);
        }

        [Fact]
        public void Build_WithValidConfiguration_ShouldCreateProvider()
        {
            // Arrange
            var source = new VaultConfigurationSource(_testKey, _mockLogger.Object);
            SetupValidConfiguration();

            // Act
            var provider = source.Build(_mockBuilder.Object);

            // Assert
            Assert.NotNull(provider);
            Assert.IsType<CachedVaultConfigurationProvider>(provider);
        }

        [Fact]
        public void Build_ShouldSkipOtherVaultConfigurationSources()
        {
            // Arrange
            var source = new VaultConfigurationSource(_testKey, _mockLogger.Object);
            var sources = new List<IConfigurationSource>
            {
                new VaultConfigurationSource("other-key", _mockLogger.Object),
                new MemoryConfigurationSource(),
                source
            };

            SetupValidConfiguration(sources);

            // Act
            var provider = source.Build(_mockBuilder.Object);

            // Assert
            Assert.NotNull(provider);
        }

        private void SetupValidConfiguration(List<IConfigurationSource> sources = null)
        {
            sources ??= new List<IConfigurationSource>
            {
                new MemoryConfigurationSource(),
                new VaultConfigurationSource(_testKey, _mockLogger.Object)
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { $"Vault:{_testKey}:Address", "http://localhost:8200" },
                    { $"Vault:{_testKey}:Token", "test-token" },
                    { $"Vault:{_testKey}:EngineName", "secret" },
                    { $"Vault:{_testKey}:Path", "test/path" }
                })
                .Build();

            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(x => x.GetSection(VaultConfigSection.SectionName))
                .Returns(configuration.GetSection(VaultConfigSection.SectionName));

            _mockBuilder.Setup(x => x.Sources).Returns(sources);
            _mockBuilder.Setup(x => x.Build()).Returns(configuration);
        }

        private void SetupConfigurationWithoutKey()
        {
            var sources = new List<IConfigurationSource>
            {
                new MemoryConfigurationSource()
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Vault:other-key:Address", "http://localhost:8200" }
                })
                .Build();

            _mockBuilder.Setup(x => x.Sources).Returns(sources);
            _mockBuilder.Setup(x => x.Build()).Returns(configuration);
        }
    }
}