// ============================================================
// OC Pittem — Azure Infrastructure (Bicep)
// Deploy: az deployment group create -g rg-ocpittem -f main.bicep
// ============================================================

@description('Location for all resources')
param location string = 'westeurope'

@description('Storage account name (globally unique)')
param storageAccountName string = 'stocpittem'

@description('Function App name (globally unique)')
param functionAppName string = 'func-ocpittem'

@description('Static Web App name')
param staticWebAppName string = 'swa-ocpittem'

@description('Application Insights name')
param appInsightsName string = 'appi-ocpittem'

// ---- Storage Account ----
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

// ---- Application Insights ----
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: 30
  }
}

// ---- App Service Plan (Consumption) ----
resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${functionAppName}-plan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true // Linux
  }
}

// ---- Function App ----
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      cors: {
        allowedOrigins: [
          'https://ocpittem.be'
          'https://www.ocpittem.be'
        ]
      }
      appSettings: [
        { name: 'AzureWebJobsStorage', value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=core.windows.net;AccountKey=${storageAccount.listKeys().keys[0].value}' }
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'APPINSIGHTS_INSTRUMENTATIONKEY', value: appInsights.properties.InstrumentationKey }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
      ]
    }
  }
}

// ---- Static Web App (Free) ----
resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: location
  sku: { name: 'Free', tier: 'Free' }
  properties: {}
}

// ---- Outputs ----
output storageAccountId string = storageAccount.id
output functionAppDefaultHostName string = functionApp.properties.defaultHostName
output staticWebAppDefaultHostName string = staticWebApp.properties.defaultHostname
