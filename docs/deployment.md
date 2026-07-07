# Deploying to Azure

The backend API runs as an **Azure Container App** backed by an **Azure SQL Database**.
Images are stored in **Azure Container Registry (ACR)** and pulled with a **user-assigned
managed identity** (no registry passwords). The database connection string lives in
**Azure Key Vault** and is surfaced to the app as a Key Vault–referenced secret. CI/CD
authenticates to Azure with **OIDC federated credentials**, so no long-lived cloud secret
is ever stored in the repository.

```
GitHub Actions ──OIDC──▶ Azure
      │
      ├─ az acr build ─────▶ ACR ──(managed identity pull)──▶ Container App (API :8080 /health)
      │                                                             │
      └─ az containerapp update                                     ▼
                                                          Azure SQL Database  ◀── conn string (Key Vault ref)
```

> Everything here is **deployment-ready** infrastructure-as-code. It has not been applied
> to a live subscription in this repository — follow the steps below to provision it in
> your own Azure account.

## Prerequisites

- Docker and git (the Azure CLI runs from a container — no host install needed).
- An Azure subscription and permission to create resource groups and role assignments.

Run the Azure CLI without installing it:

```bash
az() { docker run --rm -it -v "$HOME/.azure:/root/.azure" mcr.microsoft.com/azure-cli az "$@"; }
az login
```

## 1. Provision the infrastructure

```bash
RG=inventory-manager-rg
LOCATION=southeastasia

az group create -n "$RG" -l "$LOCATION"

export SQL_ADMIN_PASSWORD='<a-strong-password>'
export DEPLOYER_OBJECT_ID="$(az ad signed-in-user show --query id -o tsv)"

az deployment group create \
  -g "$RG" \
  -f infra/main.bicep \
  -p infra/main.bicepparam \
  -p sqlAdminPassword="$SQL_ADMIN_PASSWORD" \
  -p deployerObjectId="$DEPLOYER_OBJECT_ID"
```

The deployment outputs `acrName`, `apiAppName`, and `apiUrl`. Note the ACR name and the
container-app name for the next step.

## 2. Wire up OIDC for GitHub Actions

Create an app registration (or user-assigned identity) with a **federated credential**
scoped to this repository, then grant it `Contributor` on the resource group (which also
authorises `az acr build`):

```bash
APP_ID="$(az ad app create --display-name inventory-manager-deploy --query appId -o tsv)"
az ad sp create --id "$APP_ID"

az ad app federated-credential create --id "$APP_ID" --parameters '{
  "name": "github-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:peemphetpimolzzz/inventory-manager:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'

SUB_ID="$(az account show --query id -o tsv)"
az role assignment create --assignee "$APP_ID" --role Contributor \
  --scope "/subscriptions/$SUB_ID/resourceGroups/$RG"
```

## 3. Configure the repository

**Settings → Secrets and variables → Actions**

| Kind | Name | Value |
|------|------|-------|
| Secret | `AZURE_CLIENT_ID` | the app registration's `appId` |
| Secret | `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv` |
| Secret | `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv` |
| Variable | `AZURE_RESOURCE_GROUP` | `inventory-manager-rg` |
| Variable | `ACR_NAME` | ACR name from step 1 |
| Variable | `API_APP_NAME` | container-app name from step 1 |

## 4. Deploy

Run the **Deploy (Azure)** workflow from the Actions tab (it is `workflow_dispatch` only
until the secrets above are set; re-enable the `push` trigger in `deploy.yml` to deploy on
every merge to `main`). It builds the image in ACR, then rolls out a new Container Apps
revision and prints the public URL. Verify:

```bash
curl -f "https://<apiUrl>/health"
```

## Frontend (optional)

The React SPA (`frontend/`) is designed to be served by nginx and reverse-proxy `/api`.
To host it on Azure, publish it to **Azure Static Web Apps** (pointing its API path at the
container app's URL) or add a second container app for the nginx image. The API above is
fully functional on its own via Swagger.

## First-deploy & re-deploy notes

- **Key Vault RBAC propagation.** The template grants the deployer *Key Vault Secrets
  Officer* and writes the connection-string secret in the same deployment. If the first
  `az deployment group create` fails with a Key Vault `403 Forbidden` on the secret write
  (or the app's first revision cannot resolve the secret), the role assignment has not
  reached the data plane yet — wait a minute and re-run the same command; it is idempotent.
- **Re-deploying into the same resource group.** Key Vault names are deterministic and
  soft-delete is on, so after `az group delete` you must purge the vault before
  re-deploying within the retention window: `az keyvault purge --name <kvName>`.

## Known limitations

This is a portfolio template optimised for a one-command deploy, so a few production
concerns are deliberately traded for simplicity:

- **Azure SQL is on the public endpoint** with the "Allow Azure services" firewall rule, so
  the admin password is the only network barrier. In production, put the Container Apps
  environment on a VNet, reach SQL through a **Private Endpoint** + private DNS zone, drop
  the `AllowAllAzureIps` rule, and set `publicNetworkAccess: 'Disabled'`.
- **The API applies EF migrations on startup**, which is why it is pinned to a single
  replica (concurrent replicas would race the DDL). To scale horizontally, set
  `RUN_MIGRATIONS=false` on the API and run migrations as a separate release step.
- **First revision is unhealthy until the real image is pushed** — the placeholder image
  listens on `:80` while the probes target `:8080`. This is expected; the deploy workflow
  replaces the image and the app becomes healthy on the next revision.

## Teardown

```bash
az group delete -n "$RG" --yes --no-wait
```
