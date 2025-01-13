# Deploying Azure OpenAI Service via Bicep 

In order to use the `openai-gpt` agent you will either need a public OpenAI API key or an Azure
OpenAI deployment. Due to its additional features and manage-ability, we recommend using the Azure
OpenAI Service. This document provides easy step-by-step instructions on how to deploy the Azure
OpenAI Service using Bicep files.

There are two things needed to begin a chat experience with Azure OpenAI Service, an Azure OpenAI
Service and an Azure OpenAI Deployment. The Azure OpenAI Service is a resource that contains
multiple different model deployments. The Azure OpenAI Deployment is a model deployment that can be
called via an API to generate responses. 

## Prerequisites

Before you begin, ensure you have the following:

- An active Azure subscription
- Azure CLI or Azure PowerShell installed
- Proper permissions to create resources in your Azure subscription

## Steps to Deploy

### 1. Getting the Bicep Files

Clone the repository and navigate to the `./docs/development/AzureOAIDeployment` directory:

```sh
git clone www.github.com/PowerShell/AIShell
cd AIShell/docs/development/AzureOAIDeployment
```

There are two bicep files that are provided to help deploy the Azure OpenAI Service. The first file
is the `openai.resources.bicep` file which contains the definition and parameters for the Azure
OpenAI Service and deployment. The second file `openai.solutions.bicep` utilizes the
`openai.resources.bicep` file to deploy the Azure OpenAI Service as well as a resource group.

For more detailed explanation of the files please see [Deploy an Azure OpenAI service with LLM deployments via Bicep][01].

### 2. Deploy the Azure OpenAI Service

Now that you have the bicep files you can add your own parameter values and deploy your own Azure OpenAI instance! Simply use either Azure CLI or Azure PowerShell and modify the parameters to your liking. The below configuration does have gpt-4o as the default model to use but you are welcome to 

#### Azure CLI
```sh
az deployment sub create `
    --name openai-deployment `
    --location eastus `
    --template-file ./openai.solution.bicep `
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






## Conclusion

You have successfully deployed the Azure OpenAI Service using Bicep files. You can now proceed to
configure and use the service as needed in the portal! For more information, refer to the
[Azure OpenAI Service documentation][02].

A big thank you to Sebastian Jensen's medium article
[Deploy an Azure OpenAI service with LLM deployments via Bicep][01] for the inspiration and guidance
on how to deploy the Azure OpenAI Service using Bicep files. Please check out his blog for more
information and great AI content!


[01]: https://medium.com/medialesson/deploy-an-azure-openai-service-with-llm-deployments-via-bicep-244411472d40
[02]: https://docs.microsoft.com/azure/cognitive-services/openai/