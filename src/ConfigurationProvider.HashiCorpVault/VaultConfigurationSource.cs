using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.Configuration.HashiCorpVault;

public class VaultConfigurationSource(string key, ILogger logger = null) : IConfigurationSource
{
    private readonly string _key = key;
    private readonly ILogger _logger = logger;
    private CachedVaultConfigurationProvider _cachedVaultConfigurationProvider = null;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (_cachedVaultConfigurationProvider != null) return _cachedVaultConfigurationProvider;

        var a = builder.Sources.IndexOf(this);
        var c = new ConfigurationBuilder();
        for (var i = 0; i < a; i++)
        {
            if(builder.Sources[i] is VaultConfigurationSource) continue;
            c.Add(builder.Sources[i]);
        }

        var b = c.Build();
        var vs = b.GetSection(VaultConfigSection.SectionName);
        var v = vs.Get<VaultConfigSection>();

        if (!v.ContainsKey(_key))
        {
            throw new ArgumentException($"Key {_key} not found in configuration");
        }

        _cachedVaultConfigurationProvider = new CachedVaultConfigurationProvider(_key, v[_key], _logger);

        return _cachedVaultConfigurationProvider;
    }
}