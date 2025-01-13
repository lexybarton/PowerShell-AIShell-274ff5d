// Name of the current environment
param envName string

// Name of the resource group to deploy the resources to
param resourceGroupName string = ''

// Location of the resource group to deploy the resources to
param resourceGroupLocation string = ''

// Name of the OpenAI service
param openAiServiceName string

// Location of the OpenAI service
param openAiResourceGroupLocation string

// Custom domain name for the OpenAI service
param openAiCustomDomain string

// SKU name for the OpenAI service
param openAiSkuName string

// Name of the OpenAI deployment
param openAiDeploymentName string

// Name of the OpenAI deployment model
param openAiDeploymentModelName string

// Version of the OpenAI deployment model
param openAiDeploymentModelVersion string

// Array of deployments, currently just one deployment will be used
param deployments array = [
  {
    name: openAiDeploymentName
    model: {
      format: 'OpenAI'
      name: openAiDeploymentModelName
      version: openAiDeploymentModelVersion
    }
    scaleSettings: {
      scaleType: 'Standard'
    }
  }
]

// Tags for the resource group
param tags object = {
  Creator: 'ServiceAccount'
  Service: 'OpenAI'
  Environment: envName
}

// Scope of the deployment, currently just the subscription is supported
targetScope = 'subscription'

// Create a the resource group
resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: resourceGroupName
  location: resourceGroupLocation
}

// Create the OpenAI service by using a separate file
module openAi 'openai.resources.bicep' = {
  name: 'openai'
  scope: rg
  params: {
    name: openAiServiceName
    customDomainName: openAiCustomDomain
    location: openAiResourceGroupLocation
    tags: tags
    sku: {
      name: openAiSkuName
    }
    deployments: deployments
  }
}
