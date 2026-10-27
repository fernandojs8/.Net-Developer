targetScope = 'resourceGroup'
@minLength(3)
@maxLength(24)
param appName string
param location string = resourceGroup().location
@secure()
@minLength(16)
param sqlPassword string
param alertEmail string
@minValue(1)
@maxValue(3)
param instances int = 1
var suffix = uniqueString(resourceGroup().id, appName)
var sqlName = 'casa-sql-${suffix}'
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${appName}-identity'
  location: location
}
resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'casa-kv-${suffix}'
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    publicNetworkAccess: 'Enabled'
  }
}
resource reader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, identity.id, 'secrets-reader')
  scope: vault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}
resource sql 'Microsoft.Sql/servers@2023-08-01' = {
  name: sqlName
  location: location
  properties: {
    administratorLogin: 'casademo'
    administratorLoginPassword: sqlPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}
resource database 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sql
  name: 'CasaDoCodigo'
  location: location
  sku: { name: 'Basic', tier: 'Basic', capacity: 5 }
  properties: { maxSizeBytes: 2147483648 }
}
// Demonstration trade-off: Azure services may reach SQL, but still require SQL authentication.
// Replace with private networking and scoped firewall rules before handling real data.
resource azureFirewall 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: sql
  name: 'AllowAzureServices'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}
resource connection 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'sql-connection'
  properties: {
    value: 'Server=tcp:${sql.properties.fullyQualifiedDomainName},1433;Initial Catalog=${database.name};User ID=casademo;Password=${sqlPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
  }
}
resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${appName}-logs'
  location: location
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}
resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${appName}-insights'
  location: location
  kind: 'web'
  properties: { Application_Type: 'web', WorkspaceResourceId: workspace.id }
}
resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appName}-plan'
  location: location
  kind: 'linux'
  sku: { name: 'S1', tier: 'Standard', capacity: instances }
  properties: { reserved: true }
}
var settings = [
  { name: 'ConnectionStrings__Default', value: '@Microsoft.KeyVault(SecretUri=${vault.properties.vaultUri}secrets/${connection.name}/)' }
  { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
  { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
  { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
  { name: 'WEBSITE_RUN_FROM_PACKAGE', value: '1' }
  { name: 'LocalHttp', value: 'false' }
]
resource web 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  kind: 'app,linux'
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    keyVaultReferenceIdentity: identity.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health/ready'
      appSettings: settings
    }
  }
  dependsOn: [reader]
}
resource staging 'Microsoft.Web/sites/slots@2023-12-01' = {
  parent: web
  name: 'staging'
  location: location
  kind: 'app,linux'
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    keyVaultReferenceIdentity: identity.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health/ready'
      appSettings: settings
    }
  }
}
resource group 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: '${appName}-alerts'
  location: 'global'
  properties: {
    groupShortName: 'casa-alerts'
    enabled: true
    emailReceivers: [{ name: 'operator', emailAddress: alertEmail, useCommonAlertSchema: true }]
  }
}
resource errors 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${appName}-http5xx'
  location: 'global'
  properties: {
    description: 'More than five HTTP 5xx responses in five minutes'
    severity: 2
    enabled: true
    scopes: [web.id]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    autoMitigate: true
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [{ name: 'http5xx', metricName: 'Http5xx', metricNamespace: 'Microsoft.Web/sites', operator: 'GreaterThan', threshold: 5, timeAggregation: 'Total', criterionType: 'StaticThresholdCriterion' }]
    }
    actions: [{ actionGroupId: group.id }]
  }
}
output appUrl string = 'https://${web.properties.defaultHostName}'
output sqlServerName string = sql.name
output stagingUrl string = 'https://${staging.properties.defaultHostName}'
