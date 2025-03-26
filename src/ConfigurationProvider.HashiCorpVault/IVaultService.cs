using System.Threading.Tasks;
using VaultSharp.V1.Commons;

namespace Microsoft.Extensions.Configuration.HashiCorpVault;

public interface IVaultService
{
    Task<Secret<SecretData>> ReadSecretsAsync(string path);
}
