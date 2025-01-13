# Deploying Azure OpenAI Service via Bicep 

In order to use the `openai-gpt` agent you will either need a public OpenAI API key or an Azure
OpenAI deployment. Due to its additional features and manage-ability, we recommend using the Azure
OpenAI Service. This document provides easy step-by-step instructions on how to deploy the Azure
OpenAI Service using Bicep files.

## Prerequisites

Before you begin, ensure you have the following:

- An active Azure subscription
- Azure CLI or Azure PowerShell installed
- Proper permissions to create resources in your Azure subscription

## Steps to Deploy

### 1. Getting the Bicep Files
There are two things needed to begin a chat experience with Azure OpenAI Service, an Azure OpenAI
Service and an Azure OpenAI Deployment. The Azure OpenAI Service is a resource that contains
multiple different model deployments. The Azure OpenAI Deployment is a model deployment that can be
called via an API to generate responses. 

Clone the repository and navigate to the `./docs/development/AzureOAIDeployment` directory:

```sh
git clone www.github.com/PowerShell/AIShell
cd AIShell/docs/development/AzureOAIDeployment
```

### 2. Deploy the Azure OpenAI Service

Deploy the Bicep file using the Azure CLI:

#### Azure CLI
```sh
az deployment sub create `
    --name openai-deployment `
    --location eastus `
    --template-file ../openai.solution.bicep `
    --parameters `
        envName=dev `
        resourceGroupName=rg-test-openai `
        resourceGroupLocation=eastus `
        openAiServiceName=<insert service name> `
        openAiCustomDomain=<insert unique domain name> `
        openAiResourceGroupLocation=eastus `
        openAiSkuName=S0 `
        openAiDeploymentName=aishell-deployment `
        openAiDeploymentModelName=gpt-4o `
        openAiDeploymentModelVersion=2024-11-20
```

#### Azure PowerShell
```powershell
$parameters = @{
    envName                   = 'dev'
    resourceGroupName         = <resource group name>
    resourceGroupLocation     = 'eastus'
    openAiServiceName         = '<insert service name>'
    openAiCustomDomain        = '<insert unique domain name>'
    openAiResourceGroupLocation = 'eastus'
    openAiSkuName             = 'S0'
    openAiDeploymentName      = 'aishell-deployment'
    openAiDeploymentModelName = 'gpt-4o'
    openAiDeploymentModelVersion = '2024-11-20'
}

New-AzResourceGroupDeployment -ResourceGroupName <resource group name>
    -TemplateFile ./openai.solution.bicep
    -TemplateParameterObject $parameters
```

### 4. Verify the Deployment and configure the agent

After the deployment is complete, verify that the Azure OpenAI Service has been created:

1. Go to the [Azure Portal](https://portal.azure.com/).
2. Navigate to your resource group.
3. Check for the newly created Azure OpenAI Service.

## Conclusion

You have successfully deployed the Azure OpenAI Service using Bicep files. You can now proceed to
configure and use the service as needed.

For more information, refer to the
[Azure OpenAI Service documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/openai/).
