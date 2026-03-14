// ============================================================
// OC Pittem — Azure Infrastructure (Bicep) — West Europe
// Deploy: az deployment group create -g rg-ocpittem -f infrastructure/main.bicep -p infrastructure/main.parameters.json
// ============================================================

@description('Location for all resources. Use westeurope to maximize service compatibility.')
param location string = 'westeurope'

@description('Short suffix to make names unique. For Storage Account use only letters/numbers, keep it short (e.g. 2026, ocp26).')
param nameSuffix string = '2026'

// ---- Resource names (you can override any of these in the parameters file) ----
@description('Storage account name (globally unique, lowercase, 3-24 chars, letters/numbers only).')
param storageAccountName string = toLower('stocpittem${nameSuffix}')

@description('Function App name (globally unique).')
param functionAppName string = toLower('func-ocpittem-${nameSuffix}')

@description('Static Web App name (globally unique).')
param staticWebAppName string = toLower('swa-ocpittem-${nameSuffix}')

@description('Application Insights name.')
param appInsightsName string = toLower('appi-ocpittem-${nameSuffix}')

@description('Key Vault name (globally unique, 3-24 chars, lowercase).')
param keyVaultName string = toLower('kv-ocpittem-${nameSuffix}')

@description('Enable purge protection on Key Vault (recommended for production; cannot be disabled later).')
param enablePurgeProtection bool = true

// ---- App settings (non-secret) ----
param appFrontendUrl string = 'https://ocpittem.be'
param appContactEmail string = 'oudercomitepittem@gmail.com'
param sendGridFromEmail string = 'oudercomitepittem@gmail.com'
param sendGridFromName string = 'Oudercomité met Pit'
param stripeTicketPriceId string = 'price_xxx'

// ---- Table names ----
param tableNameOrders string = 'Orders'
param tableNameTickets string = 'Tickets'
param tableNameWebhookEvents string = 'WebhookEvents'
param tableNameSponsors string = 'SponsorRequests'

// ---- Key Vault secret names (values are set AFTER deployment) ----
param kvSecretStripeSecretKeyName string = 'stripe-secret-key'
param kvSecretStripeWebhookSecretName string = 'stripe-webhook-secret'
param kvSecretSendGridApiKeyName string = 'sendgrid-api-key'

// ---- CORS ----
param corsAllowedOrigins array = [
  'https://ocpittem.be'
  'https://www.ocpittem.be'
]

// ============================================================
// Storage Account
// ============================================================
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

var storageKey = storageAccount.listKeys().keys[0].value
var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageKey};EndpointSuffix=${environment().suffixes.storage}'

// Table Storage tables
resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource tableOrders 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  parent: tableService
  name: tableNameOrders
}
resource tableTickets 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  parent: tableService
  name: tableNameTickets
}
resource tableWebhookEvents 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  parent: tableService
  name: tableNameWebhookEvents
}
resource tableSponsors 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  parent: tableService
  name: tableNameSponsors
}

// Content share for Functions (Linux Consumption)
resource fileService 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}
var contentShareName = toLower('content${uniqueString(resourceGroup().id, functionAppName)}')
resource contentShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: fileService
  name: contentShareName
  properties: {}
}

// ============================================================
// Application Insights
// ============================================================
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: 30
  }
}

// ============================================================
// Key Vault (RBAC)
// ============================================================
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: enablePurgeProtection
    publicNetworkAccess: 'Enabled'
  }
}

// Use vaultUri property (avoid hardcoded vault.azure.net)
var kvBaseUri = keyVault.properties.vaultUri
var stripeSecretKeyUri = '${kvBaseUri}secrets/${kvSecretStripeSecretKeyName}'
var stripeWebhookSecretUri = '${kvBaseUri}secrets/${kvSecretStripeWebhookSecretName}'
var sendGridApiKeyUri = '${kvBaseUri}secrets/${kvSecretSendGridApiKeyName}'

// ============================================================
// App Service Plan (Consumption, Linux)
// ============================================================
resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${functionAppName}-plan'
  location: location
  sku: { name: 'Y1', tier: 'Dynamic' }
  properties: {
    reserved: true // Linux
  }
}

// ============================================================
// Function App (.NET 8 isolated)
// ============================================================
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      cors: {
        allowedOrigins: corsAllowedOrigins
      }
      appSettings: [
        // Core runtime
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'WEBSITE_RUN_FROM_PACKAGE', value: '1' }

        // Storage
        { name: 'AzureWebJobsStorage', value: storageConnectionString }
        { name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING', value: storageConnectionString }
        { name: 'WEBSITE_CONTENTSHARE', value: contentShareName }

        // App Insights
        { name: 'APPINSIGHTS_INSTRUMENTATIONKEY', value: appInsights.properties.InstrumentationKey }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }

        // Secrets via Key Vault references
        { name: 'Stripe__SecretKey', value: '@Microsoft.KeyVault(SecretUri=${stripeSecretKeyUri})' }
        { name: 'Stripe__WebhookSecret', value: '@Microsoft.KeyVault(SecretUri=${stripeWebhookSecretUri})' }
        { name: 'SendGrid__ApiKey', value: '@Microsoft.KeyVault(SecretUri=${sendGridApiKeyUri})' }

        // Non-secrets
        { name: 'Stripe__TicketPriceId', value: stripeTicketPriceId }
        { name: 'SendGrid__FromEmail', value: sendGridFromEmail }
        { name: 'SendGrid__FromName', value: sendGridFromName }
        { name: 'App__FrontendUrl', value: appFrontendUrl }
        { name: 'App__ContactEmail', value: appContactEmail }

        // Table names
        { name: 'Storage__TableNameOrders', value: tableNameOrders }
        { name: 'Storage__TableNameTickets', value: tableNameTickets }
        { name: 'Storage__TableNameWebhookEvents', value: tableNameWebhookEvents }
        { name: 'Storage__TableNameSponsors', value: tableNameSponsors }
      ]
    }
  }
}

// ============================================================
// Grant Function App identity access to Key Vault secrets (RBAC)
// ============================================================
var keyVaultSecretsUserRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4633458b-17de-408a-b874-0445c86b69e6' // Key Vault Secrets User
)

resource kvRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  // IMPORTANT: name must be computable at start of deployment (avoid principalId in name)
  name: guid(keyVault.id, functionApp.id, 'kv-secrets-user')
  scope: keyVault
  properties: {
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================
// Static Web App (Free)
// ============================================================
resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: location
  sku: { name: 'Free', tier: 'Free' }
  properties: {}
}

// ============================================================
// Outputs
// ============================================================
output storageAccountId string = storageAccount.id
output keyVaultUri string = keyVault.properties.vaultUri
output functionAppDefaultHostName string = functionApp.properties.defaultHostName
output staticWebAppDefaultHostName string = staticWebApp.properties.defaultHostname
