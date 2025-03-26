using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines;

namespace ConfigurationProvider.HashiCorpVault.Demo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IConfiguration _configuration;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync()
        {
            var a = await CreateFirst();

            Console.WriteLine(a.WrapInfo);

            a = await CreateSecond();

            Console.WriteLine(a.WrapInfo);

            a = await CreateThird();

            Console.WriteLine(a.WrapInfo);

            return Ok();
        }

        private async Task<VaultSharp.V1.Commons.Secret<VaultSharp.V1.Commons.CurrentSecretMetadata>> CreateFirst()
        {
            var vaultClientSettings = new VaultClientSettings("http://localhost:8200", new TokenAuthMethodInfo("dev-only-token"))
            {
                SecretsEngineMountPoints = new SecretsEngineMountPoints
                {
                    KeyValueV2 = "demo_common"
                }
            };
            var vaultClient = new VaultClient(vaultClientSettings);

            // mount a new v2 kv
            var kv2SecretsEngine = new SecretsEngine
            {
                Type = SecretsEngineType.KeyValueV2,
                Config = new Dictionary<string, object>
                {
                    {  "version", "1" }
                },
                Path = "demo_common"
            };

            try
            {
                await vaultClient.V1.System.MountSecretBackendAsync(kv2SecretsEngine);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while mounting secret backend");
            }

            var a = await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync("default", new Dictionary<string, object>
            {
                { "App:ProjectName", "VaultDemo" },
                { "App:ApplicationName", "ConfigurationProvider.HashiCorpVault.Demo" },
                { "App:ServerName", "None" },
                { "ConnectionStrings:Demo1", "First Connection String" }
            });
            return a;
        }

        private async Task<VaultSharp.V1.Commons.Secret<VaultSharp.V1.Commons.CurrentSecretMetadata>> CreateSecond()
        {
            var vaultClientSettings = new VaultClientSettings("http://localhost:8200", new TokenAuthMethodInfo("dev-only-token"))
            {
                SecretsEngineMountPoints = new SecretsEngineMountPoints
                {
                    KeyValueV2 = "demo_app1"
                }
            };
            var vaultClient = new VaultClient(vaultClientSettings);

            // mount a new v2 kv
            var kv2SecretsEngine = new SecretsEngine
            {
                Type = SecretsEngineType.KeyValueV2,
                Config = new Dictionary<string, object>
                {
                    {  "version", "1" }
                },
                Path = "demo_app1"
            };

            try
            {
                await vaultClient.V1.System.MountSecretBackendAsync(kv2SecretsEngine);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while mounting secret backend");
            }

            var a = await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync("default", new Dictionary<string, object>
            {
                { "App:ApplicationName", "ConfigurationProvider.HashiCorpVault.Demo 1" },
                { "ConnectionStrings:Demo2", "Second Connection String" }
            });
            return a;
        }

        private async Task<VaultSharp.V1.Commons.Secret<VaultSharp.V1.Commons.CurrentSecretMetadata>> CreateThird()
        {
            var vaultClientSettings = new VaultClientSettings("http://localhost:8200", new TokenAuthMethodInfo("dev-only-token"))
            {
                SecretsEngineMountPoints = new SecretsEngineMountPoints
                {
                    KeyValueV2 = "demo_server1"
                }
            };
            var vaultClient = new VaultClient(vaultClientSettings);

            // mount a new v2 kv
            var kv2SecretsEngine = new SecretsEngine
            {
                Type = SecretsEngineType.KeyValueV2,
                Config = new Dictionary<string, object>
                {
                    {  "version", "1" }
                },
                Path = "demo_server1"
            };
            try
            {
                await vaultClient.V1.System.MountSecretBackendAsync(kv2SecretsEngine);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while mounting secret backend");
            }

            var a = await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync("default", new Dictionary<string, object>
            {
                { "App:ServerName", "Server1" },
                { "ConnectionStrings:Demo3", "Third Connection String" }
            });
            return a;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            _logger.LogInformation(_configuration.GetValue<string>("App:ProjectName"));
            _logger.LogInformation(_configuration.GetValue<string>("App:ApplicationName"));
            _logger.LogInformation(_configuration.GetValue<string>("App:ServerName"));
            _logger.LogInformation(_configuration.GetValue<string>("ConnectionStrings:Demo1"));
            _logger.LogInformation(_configuration.GetValue<string>("ConnectionStrings:Demo2"));
            _logger.LogInformation(_configuration.GetValue<string>("ConnectionStrings:Demo3"));

            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("configs")]
        public IActionResult GetConfigs()
        {
            return Ok(new
            {
                ProjectName = _configuration.GetValue<string>("App:ProjectName"),
                ApplicationName = _configuration.GetValue<string>("App:ApplicationName"),
                ServerName = _configuration.GetValue<string>("App:ServerName"),
                ConnectionString1 = _configuration.GetValue<string>("ConnectionStrings:Demo1"),
                ConnectionString2 = _configuration.GetValue<string>("ConnectionStrings:Demo2"),
                ConnectionString3 = _configuration.GetValue<string>("ConnectionStrings:Demo3"),
            });
        }
    }
}