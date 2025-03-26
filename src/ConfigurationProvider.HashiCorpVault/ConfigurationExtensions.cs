using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Configuration.HashiCorpVault;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddVaultConfiguration(
         this IConfigurationBuilder builder,
         string key,
         ILogger logger = null)
    {
        return builder.Add(new VaultConfigurationSource(key, logger));
    }
}