# Project

PHI Deidentification Portal

Installation Instructions

![318910463-a3d9905d-6df7-4e2d-b0eb-d0c4e7e2ecb5](https://github.com/microsoft/PHIDeIDPortal/assets/112185610/1f74e6b9-0f94-40db-9fa8-aadd04433d24)
 
Deployment Steps –
1. Clone or Fork repo  
2. Create a new Storage Account  
  a. **az storage account create** -n _storageaccount_ -g _resourcegroup_ --sku Standard_LRS
  
3. Create a Storage Account container for document uploads  
  a. **az storage container create** -n _container_ --account-name _storageaccount_  
  
4. Create a new Azure AI multi-service resource  
  a. **az cognitiveservices account create** --name _aiservice_ --location _location_ --resource-group _resourcegroup_ --kind CognitiveServices --sku s0 --yes  
  
5. Create a new Azure AI Search instance  
  a. **az search service create** --name _searchservice_ --resource-group _resourcegroup_ –sku standard

6.  Create the Cosmos NoSQL database  
  a. **az cosmosdb create** --name _cosmosdb_ --resource-group _resourcegroup_ --kind GlobalDocumentDB --locations regionName = _location_  
  b. **az cosmosdb sql database create** -g _resourcegroup_ -a _cosmosaccountname_ -n deid --throughput 400  
  c. **az cosmosdb sql container create** -g _resourcegroup_ -a _cosmosaccountname_ -d deid -n metadata --partition-key-path "/uri"  
  
8. Create two new App Service Plans – one for the Web application and one for standard Functions  
  a. **az appservice plan create** -g _resourcegroup_ -n _plan1_ --sku S1  
  b. **az appservice plan create** -g _resourcegroup_ -n _plan2_ --sku S1  
  
9. Create a new Azure Function instance for the metadata sync and custom skill  
  a. **az functionapp create** --resource-group _resourcegroup_ --name _functionappname_ --os-type Windows --runtime dotnet --storage-account _storageaccount_ --plan _plan1_  
  b. Publish the Azure Function to the Function App Service    

10. Create the Web application for the DeID Web Portal  
  a. **az webapp create** --resource-group _resourcegroup_ --name _webname_ --runtime dotnet:8 --plan _plan2_  
  b. **az resource update** --resource-group resourcegroup --name scm --namespace Microsoft.Web --resource-type basicPublishingCredentialsPolicies --_parent sites/webname_ --set properties.allow=true  
  c. Publish the Web solution to the Web App Service  
  d. **az webapp identity assign** -g resourcegroup -n _webname_ (results used in e.)
  e. **az role assignment create** --assignee _systemassignedidentityguid_ --role "Storage Blob Data Contributor" --scope _storageaccountid_ 
  f. **az ad app create** --display-name _appid_ --web-redirect-uris _https://{webname}.azurewebsites.net/signin-oidc_  
  g. Modify App Registration to include Group Claims (modify groupMembershipClaims property)  
  h. Add Entra group to support Admins. Note group name for updating the web app configuration value  

12.	Deploy the metadata sync and custom Function app by configuring the Azure Function to pull from your forked GH repo or by cloning the repo and doing a publish.
13.	Create the AI Search Index, Custom Skill and Indexer definitions (in that order) using the three JSON configuration files in the search-config folder of the Repo
14.	Upload documents to the Blob Storage Container created in #3 and ensure the Indexer is running.

This project conforms to the MIT licensing terms. Code is not indended as a complete production-ready solution and no warranty is implied.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
