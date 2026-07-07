using './main.bicep'

// Base name used to derive resource names (lowercase letters/numbers).
param appName = 'inventorymgr'

// SQL admin credentials. Supply the password at deploy time — do NOT commit a real value:
//   az deployment group create -g <rg> -f infra/main.bicep -p infra/main.bicepparam \
//     -p sqlAdminPassword=$SQL_ADMIN_PASSWORD -p deployerObjectId=$(az ad signed-in-user show --query id -o tsv)
param sqlAdminLogin = 'sqladmin'
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')

// Object ID granted Key Vault Secrets Officer (the identity running the deploy).
param deployerObjectId = readEnvironmentVariable('DEPLOYER_OBJECT_ID', '')
