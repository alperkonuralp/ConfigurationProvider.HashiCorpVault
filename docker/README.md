# HashiCorp Vault Docker Setup

This directory contains Docker Compose configuration for setting up a HashiCorp Vault instance for development and testing purposes.

## Configuration Details

The `docker-compose.yml` file sets up the following:

- **Image**: `hashicorp/vault:latest` (Community Edition)
- **Container Name**: `hashicorp-vault`
- **API Port**: `8200` (mapped to host port `8200`)
- **Root Token**: `dev-only-token` (for development only)
- **Storage**: Persistent volume (`vault-data`)

## Environment Variables

- `VAULT_DEV_ROOT_TOKEN_ID=dev-only-token` - Sets a predictable root token
- `VAULT_DEV_LISTEN_ADDRESS=0.0.0.0:8200` - Makes Vault listen on all interfaces
- `VAULT_ADDR=http://127.0.0.1:8200` - Sets the Vault API address

## Usage Instructions

### Starting the Vault Server

```bash
# Navigate to the docker directory
cd docker

# Start Vault in detached mode
docker-compose up -d
```

### Verifying Vault is Running

```bash
# Check container status
docker-compose ps

# View logs
docker-compose logs vault
```

### Accessing the Vault UI

The Vault UI will be available at: http://localhost:8200

You can log in using the root token: `dev-only-token`

### Interacting with Vault CLI

```bash
# Exec into the container
docker-compose exec vault sh

# Set the Vault address and token
export VAULT_ADDR='http://127.0.0.1:8200'
export VAULT_TOKEN='dev-only-token'

# Now you can run vault commands
vault status
vault secrets list
```

## Setting Up Vault for Development

### 1. Enable and Configure KV Secrets Engine

The KV (Key-Value) secrets engine is most commonly used for development:

```bash
# Enable KV version 2 secrets engine at path "secret"
vault secrets enable -version=2 -path=secret kv

# Verify it was enabled
vault secrets list
```

### 2. Adding Secrets for Development

```bash
# Add a simple key-value secret
vault kv put secret/myapp/config db.username=devuser db.password=devpassword

# Add nested JSON data
vault kv put secret/myapp/config @- << EOF
{
  "database": {
    "username": "admin",
    "password": "supersecret"
  },
  "api": {
    "key": "api-key-123",
    "endpoint": "https://api.example.com"
  }
}
EOF
```

### 3. Reading Secrets

```bash
# Read a secret
vault kv get secret/myapp/config

# Read specific field
vault kv get -field=database.password secret/myapp/config

# Format as JSON
vault kv get -format=json secret/myapp/config
```

### 4. Using the HTTP API

You can also interact with Vault using the HTTP API:

```bash
# Read a secret using curl
curl --header "X-Vault-Token: dev-only-token" \
     http://localhost:8200/v1/secret/data/myapp/config | jq

# Write a secret using curl
curl --header "X-Vault-Token: dev-only-token" \
     --request POST \
     --data '{"data": {"api_key": "my-api-key"}}' \
     http://localhost:8200/v1/secret/data/myapp/apikeys
```

### 5. Using with the .NET Configuration Provider

To use this Vault instance with the ConfigurationProvider.HashiCorpVault library:

```csharp
// In your Program.cs or Startup.cs
using Microsoft.Extensions.Configuration;

// Add this to your configuration setup
builder.Configuration.AddHashiCorpVault(options => {
    options.VaultUri = "http://localhost:8200";
    options.Token = "dev-only-token";  
    options.SecretsPath = "secret/data/myapp/config";
});
```

Make sure your application has network access to the Vault container.

### Stopping the Services

```bash
# Stop the container
docker-compose down

# To also remove volumes (will delete all data)
docker-compose down -v
```

## Security Notice

This configuration is intended for development and testing only. For production use:

- Do not use the default token
- Implement proper secrets management
- Configure TLS
- Use audit logging
- Follow HashiCorp's security best practices

## Related Documentation

- [HashiCorp Vault Documentation](https://www.vaultproject.io/docs)
- [Docker Hub: hashicorp/vault](https://hub.docker.com/_/vault)