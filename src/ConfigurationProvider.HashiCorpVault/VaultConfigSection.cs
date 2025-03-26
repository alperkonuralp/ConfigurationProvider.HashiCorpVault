using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.HashiCorpVault;

public class VaultConfigSection : Dictionary<string, VaultConnectionConfigSection>
{
    public const string SectionName = "Vault";
}
