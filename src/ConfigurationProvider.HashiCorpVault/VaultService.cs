using System;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines;

namespace Microsoft.Extensions.Configuration.HashiCorpVault;

internal class VaultService : IVaultService
{
    private readonly IVaultClient _vaultClient;

    public VaultService(string key, VaultConnectionConfigSection config)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Config = config ?? throw new ArgumentNullException(nameof(config));

        var vaultClientSettings = new VaultClientSettings(config.Address, new TokenAuthMethodInfo(config.Token))
        {
            SecretsEngineMountPoints = new SecretsEngineMountPoints
            {
                KeyValueV2 = config.EngineName
            }
        };
        _vaultClient = new VaultClient(vaultClientSettings);
    }

    public string Key { get; }
    public VaultConnectionConfigSection Config { get; }

    public Task<Secret<SecretData>> ReadSecretsAsync(string path)
    {
        return _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path);
    }
}
