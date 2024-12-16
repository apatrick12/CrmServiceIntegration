using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;

namespace CrmServiceIntegration
{
    public class AssociateContactToParent : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;

            IOrganizationServiceFactory serviceFactory = serviceProvider.GetService(typeof(IOrganizationServiceFactory)) as IOrganizationServiceFactory;

            IOrganizationService organizationService = serviceProvider.GetService(typeof(IOrganizationService)) as IOrganizationService;

            // Obtain the tracing service
            ITracingService tracingService =  serviceProvider.GetService(typeof(ITracingService)) as ITracingService;

            // The InputParameters collection contains all the data passed in the message request.
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity)context.InputParameters["Target"];
                try
                {
                    if (entity.LogicalName == CRMConstants.Contact.EntityName)
                    {
                        var parentId = ((EntityReference) entity.Attributes[CRMConstants.Contact.ParentCustomerId]).Id;
                        ValidateAndAssociateContactAddressToMatchingParentAccount(entity, parentId, organizationService, tracingService);
                    }

                 
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    tracingService.Trace("the following error occurred", ex);
                    throw new InvalidPluginExecutionException($"the following error occurred in {typeof(AssociateContactToParent)}", ex);
                }
                catch (Exception ex)
                {

                    tracingService.Trace("the following error occurred", ex);
                    throw;
                }
            }

        }

        /// <summary>
        /// Associates contact with  parent account based on macthing address , PreOperation Task
        /// </summary>
        /// <param name="contact"></param>
        /// <param name="parentId"></param>
        /// <param name="service"></param>
        /// <param name="tracingService"></param>
        /// <exception cref="InvalidPluginExecutionException"></exception>
        private void ValidateAndAssociateContactAddressToMatchingParentAccount(Entity contact, Guid parentId, IOrganizationService service, ITracingService tracingService)
        {
            try
            {
                // Retrieve the parent account
                RetrieveRequest request = new RetrieveRequest()
                {
                    ColumnSet = new ColumnSet(CRMConstants.Address.AddressLine1, CRMConstants.Address.City, CRMConstants.Address.Country),
                    Target = new EntityReference(CRMConstants.Account.EntityName, parentId)
                };
                var response = (RetrieveResponse)service.Execute(request);
                Entity parentAccountEntity = response.Entity;

                if (parentAccountEntity == null)
                {
                    // Log detailed information for debugging
                    tracingService.Trace($"No account record found for the provided parent ID: {parentId}. The parent ID might be invalid or the account might have been deleted.");

                    // Consider a fallback mechanism or exit gracefully
                    tracingService.Trace("Proceeding with the existing selection without matching address validation or reassociating it to any parent account.");
                    return;
                }

                // Validate addresses
                string contactAddressLine1 = contact.GetAttributeValue<string>(CRMConstants.Address.AddressLine1) ?? string.Empty;
                string contactCity = contact.GetAttributeValue<string>(CRMConstants.Address.City) ?? string.Empty;
                string contactCountry = contact.GetAttributeValue<string>(CRMConstants.Address.Country) ?? string.Empty;
                
                if (contactAddressLine1 == string.Empty && contactCity == string.Empty && contactCountry == string.Empty)
                {
                    //exiting because there is no address to validate with. this is subjective to the PMO or BA 
                    tracingService.Trace("No contact address to validate with, system will proceed with the existing selection without matching address validation or reassociating it to any parent account.");
                    return; 
                }

                if (!IsMatched(parentAccountEntity.GetAttributeValue<string>(CRMConstants.Address.AddressLine1), contactAddressLine1) ||
                    !IsMatched(parentAccountEntity.GetAttributeValue<string>(CRMConstants.Address.City), contactCity) ||
                    !IsMatched(parentAccountEntity.GetAttributeValue<string>(CRMConstants.Address.Country), contactCountry))
                {
                    var fetchResponse = FetchParentAccountByAddress(contactAddressLine1, contactCity, contactCountry, service, tracingService);

                    if (fetchResponse.IsSuccess)
                    {
                        //note: this is a pre-operation stage, no need to call update since the flow will still update the contact eventually
                        //we just set and associate the record. 
                        contact[CRMConstants.Contact.ParentCustomerId] = new EntityReference(CRMConstants.Account.EntityName, fetchResponse.parentID);
                        tracingService.Trace("Contact updated with a parent account having matching address.");
                    }
                    else
                    {
                        tracingService.Trace("system could not locate any parent account with matching address, hence selected parent account would be used.");
                    }
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error: {ex.Message}", ex);
                throw new InvalidPluginExecutionException($"Error in {nameof(AssociateContactToParent)} plugin", ex);
            }
        }


        /// <summary>
        /// Compares two values and returns true if matched
        /// </summary>
        /// <param name="var1"></param>
        /// <param name="var2"></param>
        /// <returns></returns>
        private bool IsMatched(string var1, string var2)
        {
            return string.Equals(var1.Trim(), var2.Trim(), StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Finds Account by Address Details
        /// </summary>
        /// <param name="line1"></param>
        /// <param name="city"></param>
        /// <param name="country"></param>
        /// <param name="tracingService1"></param>
        /// <returns>Parent Account Id and a Flag</returns>
        /// <exception cref="InvalidPluginExecutionException"></exception>
        private (Guid parentID, bool IsSuccess) FetchParentAccountByAddress(string line1, string city, string country, IOrganizationService service, ITracingService tracingService1)
        {
            try
            {
                string fetchXml = $@"
                                    <fetch top='1'>
                                      <entity name='account'>
                                        <attribute name='accountid' />
                                        <attribute name='name' />
                                        <attribute name='address1_line1' />
                                        <attribute name='address1_city' />
                                        <attribute name='address1_country' />
                                        <filter type='and'>
                                          <condition attribute='address1_line1' operator='eq' value='{line1}' />
                                          <condition attribute='address1_city' operator='eq' value='{city}' />
                                          <condition attribute='address1_country' operator='eq' value='{country}' />
                                        </filter>
                                      </entity>
                                    </fetch>";

                EntityCollection results = service.RetrieveMultiple(new FetchExpression(fetchXml));

                if (results?.Entities?.Count > 0)
                {
                    // Get the ID of the first account
                    Guid accountId = results.Entities[0].Id;
                    return (accountId, true);
                }                
                tracingService1.Trace("No accounts found matching the criteria.");
                return (Guid.Empty, false);
                ;
            }
            catch (Exception ex)
            {
                tracingService1.Trace("the following error occurred", ex);
                throw new InvalidPluginExecutionException($"the following error occurred in {typeof(AssociateContactToParent)}", ex);
            }
        }
   
    }
}
