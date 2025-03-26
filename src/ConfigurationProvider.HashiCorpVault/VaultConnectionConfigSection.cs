using System;

namespace Microsoft.Extensions.Configuration.HashiCorpVault;

public class VaultConnectionConfigSection
{
    public string Address { get; set; }
    public string Token { get; set; }
    public string EngineName { get; set; }
    public string Path { get; set; }
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
    public string Prefix { get; set; } = null;

    public bool IsAsync { get; set; } = true;
}
