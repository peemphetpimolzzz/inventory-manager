// Inventory Manager — Azure infrastructure (deployment-ready).
//
// Provisions the backend API as an Azure Container App backed by an Azure SQL
// Database. Container image is pulled from Azure Container Registry using a
// user-assigned managed identity (no registry passwords), and the database
// connection string is stored in Key Vault and surfaced to the app as a
// Key Vault-referenced secret.
//
// Deploy:
//   az deployment group create -g <rg> -f infra/main.bicep -p infra/main.bicepparam
//
// See docs/deployment.md for the full walkthrough (identity, OIDC, first push).

targetScope = 'resourceGroup'

@description('Base name used to derive resource names. Lowercase letters and numbers.')
@minLength(3)
@maxLength(20)
param appName string = 'inventorymgr'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Container image reference for the API, e.g. <acr>.azurecr.io/inventory-api:<tag>. Leave as the placeholder for the first infra-only deploy; the deploy workflow updates it on each release.')
// Bootstrap note: the placeholder image listens on :80, while ingress and the
// probes below target 8080, so the first (infra-only) revision stays unhealthy
// until the deploy workflow pushes the real image. That is expected — the app
// only serves once its own image is deployed.
param apiImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('Azure SQL administrator login.')
param sqlAdminLogin string = 'sqladmin'

@description('Azure SQL administrator password.')
@secure()
param sqlAdminPassword string

@description('Object ID of the deploying user/service principal, granted Key Vault Secrets Officer so the connection-string secret can be written.')
param deployerObjectId string

var suffix = uniqueString(resourceGroup().id, appName)
var acrName = toLower('${appName}acr${suffix}')
var kvName = take(toLower('${appName}kv${suffix}'), 24)
var sqlServerName = toLower('${appName}-sql-${suffix}')
var databaseName = 'Inventory'
var identityName = '${appName}-id'
var envName = '${appName}-cae'
var logName = '${appName}-logs'
var apiAppName = '${appName}-api'
var connSecretName = 'sql-connection-string'

// Built-in role definition IDs.
var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var kvSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
var kvSecretsOfficerRoleId = 'b86a8fe4-44ce-4948-aff5-f7d1c8c9a5a3'

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logName
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: { name: 'Basic' }
  properties: {
    adminUserEnabled: false
  }
}

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, identity.id, acrPullRoleId)
  scope: acr
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: kvName
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

resource kvSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, identity.id, kvSecretsUserRoleId)
  scope: keyVault
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
  }
}

resource kvSecretsOfficer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, deployerObjectId, kvSecretsOfficerRoleId)
  scope: keyVault
  properties: {
    principalId: deployerObjectId
    principalType: 'User'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsOfficerRoleId)
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Allow other Azure services (Container Apps) to reach the server.
//
// Known limitation (demo simplicity): this is the "Allow Azure services"
// catch-all, so the server is reachable from any Azure tenant — the SQL admin
// password is the only barrier. A production deployment would instead put the
// Container Apps environment on a VNet, expose SQL through a Private Endpoint +
// private DNS zone, drop this rule, and set publicNetworkAccess: 'Disabled'.
// See docs/deployment.md → "Known limitations".
resource sqlAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: sqlServer
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'GP_S_Gen5_1'
    tier: 'GeneralPurpose'
  }
  properties: {
    autoPauseDelay: 60
    minCapacity: json('0.5')
  }
}

// Connection string kept in Key Vault, not in app config or workflow logs.
resource connSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: connSecretName
  properties: {
    // Password is single-quoted so a ';' in it does not terminate the token
    // (ADO.NET connection-string quoting). Avoid a literal single quote in the
    // password, or double it per the connection-string spec.
    value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${databaseName};User ID=${sqlAdminLogin};Password=\'${sqlAdminPassword}\';Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
  }
  dependsOn: [
    kvSecretsOfficer
  ]
}

resource env 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: envName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: acr.properties.loginServer
          identity: identity.id
        }
      ]
      secrets: [
        {
          name: connSecretName
          keyVaultUrl: connSecret.properties.secretUri
          identity: identity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: apiImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              // Do not seed demo data into a real database. The app defaults
              // SEED_DATA to true, which would insert the demo catalog on first run.
              name: 'SEED_DATA'
              value: 'false'
            }
            {
              name: 'ConnectionStrings__Default'
              secretRef: connSecretName
            }
          ]
          probes: [
            {
              // The app applies EF migrations before Kestrel starts listening,
              // and serverless SQL may need to resume from auto-pause on a cold
              // start. The startup probe gives that up to ~5 min before the
              // liveness probe can recycle the container.
              type: 'Startup'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 10
              failureThreshold: 30
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 20
              periodSeconds: 15
            }
          ]
        }
      ]
      // Pinned to a single replica: the app applies EF migrations on startup
      // (MigrateAsync before app.Run), so multiple replicas would run DDL
      // concurrently against Azure SQL with no distributed lock. To scale
      // horizontally, set RUN_MIGRATIONS=false on the API and apply migrations
      // as a separate release step, then raise maxReplicas.
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
  dependsOn: [
    acrPull
    kvSecretsUser
  ]
}

@description('Login server of the container registry (used by the deploy workflow).')
output acrLoginServer string = acr.properties.loginServer

@description('Name of the container registry.')
output acrName string = acr.name

@description('Name of the API container app (used by the deploy workflow).')
output apiAppName string = apiApp.name

@description('Public URL of the API.')
output apiUrl string = 'https://${apiApp.properties.configuration.ingress.fqdn}'
