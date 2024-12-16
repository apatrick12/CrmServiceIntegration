using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace ContactWorkflowIntegration
{
    public class GetContactInformation : CodeActivity
    {
        [Input("CMAH ID")]
        public InArgument<string> CMAHID { get; set; }

        [Output("IsRecordFound")]
        public OutArgument<bool> IsRecordFound { get; set; }

        [Output("Result")]
        public OutArgument<string> Result { get; set; }

        [Input("Int input")]
        [Output("Int output")]
        public InOutArgument<int> MAtchedCount { get; set; }

        [Input("Bool input")]
        [Default("True")]
        public InArgument<bool> IsCreated { get; set; }

        protected override void Execute(CodeActivityContext context)
        {


            var workflowContext = context.GetExtension<IWorkflowContext>();
            var serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            var service = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            ITracingService ts=context.GetExtension<ITracingService>();

            ts.Trace("commencing Operation");

            string cmahId = CMAHID.Get(context);

            var results = searchForContact(service, cmahId, ts);
            IsRecordFound.Set(context, results != null);
            Result.Set(context, results);
            ts.Trace((results != null) ? "found contact and returning results" : "found no matches, or more than one match");

        }

        private string searchForContact(IOrganizationService service, string cmahId, ITracingService ts)
        {
            if (!string.IsNullOrEmpty(cmahId))
            {
                string fetchXMLbyCMAHId = $@"<fetch>
                          <entity name='contact'>
                            <attribute name='emailaddress1' />
                            <attribute name='new_deathdate' />
                            <attribute name='new_contacttypeid' />
                            <attribute name='new_contactsubtypeid' />
                            <attribute name='new_practicestatus' />
                            <attribute name='new_1styearinpractice' />
                            <attribute name='lastname' />
                            <attribute name='firstname' /><attribute name='birthdate' />
                            <attribute name='new_cmah_id' />
                            <attribute name='new_language' alias='language' />
                            <attribute name='telephone1' />
                            <attribute name='telephone2' />
                            <attribute name='new_familyphysiciannumber' />
                            <attribute name='new_businessphoneextension' />
                            <attribute name='mobilephone' />
                            <attribute name='new_cmasmsconsent' />
                            <attribute name='new_salutation' />
                            <attribute name='new_physicianpreferredname' />
                            <attribute name='new_communityplatformbio' />
                            <attribute name='new_pronoun' />
                            <attribute name='new_otherpronoun' />
                            <attribute name='new_residencyprogram' />
                            <attribute name='new_universityofresidency' />
                            <attribute name='new_collegeofgraduation' />
                            <attribute name='new_yearenrolledinmedicalschool' />
                            <attribute name='new_fellowship' />
                            <attribute name='new_residencycompletion' />
                            <attribute name='new_locum_estcompdate' />
                            <attribute name='new_locum' /><attribute name='new_contact_id' />
                            <attribute name='new_membershipstatus' /><attribute name='new_dateretirement' />
                            <link-entity link-type='outer' name='customeraddress' from='parentid' to='contactid' alias='home'>
                              <attribute name='new_stateorprovincemasterlist' alias='home_stateorprovince' />
                              <attribute name='line1' alias='home_line1' />
                              <attribute name='line2' alias='home_line2' />
                              <attribute name='new_country' alias='home_country' />
                              <attribute name='postalcode' alias='home_postalcode' />
                              <attribute name='city' alias='home_city' />
                              <attribute name='new_cmapreferred' alias='home_preferred' />
                              <attribute name='addresstypecode' alias='home_addresstype' />
                              <attribute name='addressnumber' alias='home_addressnumber'/>
                              <attribute name='new_isverified' alias='home_verified' />
                              <attribute name='new_status' alias='home_status' />
                              <filter>
                                <condition attribute='addresstypecode' operator='eq' value='100000004' />
                              </filter>
                            </link-entity>
                            <link-entity link-type='outer' name='customeraddress' from='parentid' to='contactid' alias='office'>
                              <attribute name='new_stateorprovincemasterlist' alias='office_stateorprovince' />
                              <attribute name='line1' alias='office_line1' />
                              <attribute name='line2' alias='office_line2' />
                              <attribute name='new_country' alias='office_country' />
                              <attribute name='postalcode' alias='office_postalcode' />
                              <attribute name='city' alias='office_city' />
                              <attribute name='new_cmapreferred' alias='office_preferred' />
                              <attribute name='addresstypecode' alias='office_addresstype' />
                              <attribute name='addressnumber' alias='office_addressnumber' />
                              <attribute name='new_status' alias='office_status' />
                              <filter>
                                <condition attribute='addresstypecode' operator='eq' value='100000002' />
                              </filter>
                            </link-entity>
                            <link-entity name='new_cmamembershipdetail' from='new_cmamembershipdetailid' to='new_cmamembershipdetailid' link-type='outer' alias='cmd'>
                              <attribute name='new_status' alias='memStatus' />
                              <attribute name='new_expirydate' alias='memExpDate' />
                              <attribute name='new_membershipyear' alias='memYear'/>
                              <attribute name='new_categoryproductid' />
                              <link-entity name='salesorder' from='salesorderid' to='new_orderid' link-type='outer' alias='ord'>
                                <attribute name='new_amountoutstanding' />
                                <attribute name='new_totallineitemdiscount' />
                                <attribute name='new_amountpaid' />
                                <attribute name='totalamount' />
                                <attribute name='createdon' />
                                <attribute name='totallineitemamount' />
                                <attribute name='new_tax1' />
                                <attribute name='new_tax2' />
                                <attribute name='new_tax3' />
                                <attribute name='new_tax1_percentage' />
                                <attribute name='new_tax2_percentage' />
                                <attribute name='new_tax3_percentage' />
                                <attribute name='new_jurisdiction' />
                              </link-entity>
                              <link-entity name='account' from='accountid' to='new_divassocaccountid' link-type='outer' alias='ptma'>
                                <attribute name='accountid' alias='ptmaid' />
                                <attribute name='new_englishname' alias='english' />
                                <attribute name='new_frenchname' alias='french' />
                              </link-entity>
                            </link-entity>
                            <filter>
                                <condition attribute='new_cmah_id' operator='eq' value='{cmahId}' />
                                <condition attribute='statuscode' operator='eq' value='1' />
                            </filter>
                         </entity>
                         </fetch>";
                ts.Trace($"searching by cmah id '{cmahId}'");
                ts.Trace(fetchXMLbyCMAHId);
                var lookupResult = service.RetrieveMultiple(new FetchExpression(fetchXMLbyCMAHId));
                if (lookupResult.Entities.Count > 0)
                    return BuildResults(service, lookupResult.Entities[0], ts);
            }
            return null;
        }

        private string BuildResults(IOrganizationService service, Entity entity, ITracingService ts)
        {
            ts.Trace("start build results");
            string content = "{";
            //content += !entity.Contains("new_contact_id") ? @"""cmaID"":""""," : @"""cmaID"":""" + EscpateDoubleQuoteAndBackSlash(entity["new_contact_id"] as string) + @""",";
            content += !entity.Contains("new_contacttypeid") ? @"""contactTypeId"":""""," : @"""contactTypeId"":""" + ((EntityReference)entity["new_contacttypeid"]).Id + @""",";

            ////bool isMember = entity.Contains("new_membershipstatus") ? (((OptionSetValue)entity["new_membershipstatus"]).Value == 100000000) : false;
            ////int practiceStatus = entity.Contains("new_practicestatus") ? ((OptionSetValue)entity["new_practicestatus"]).Value : 0;
            ////string _contact_sub_type = ((EntityReference)entity["new_contactsubtypeid"]).Id.ToString();
            ////bool isPhysician = (((EntityReference)entity["new_contacttypeid"]).Id.ToString() == "9860eaf2-4b7a-e811-a956-000d3af475a9");
            ////content += BuildCMARole(isMember, practiceStatus, isPhysician, _contact_sub_type);

            //content += !entity.Contains("new_contactsubtypeid") ? @"""contactSubTypeId"":""""," : @"""contactSubTypeId"":""" + ((EntityReference)entity["new_contactsubtypeid"]).Id + @""",";

            //content += @"""licencing"":{";
            ////content += !entity.Contains("new_familyphysiciannumber") ? @"""CfpcId"":""""," : @"""CfpcId"":""" + EscpateDoubleQuoteAndBackSlash(entity["new_familyphysiciannumber"] as string) + @""",";
            ////content += "},";

            ////ts.Trace($" contains new_1styearinpractice: {entity.Contains("new_1styearinpractice")}");
            ////ts.Trace($" contains new_locum: {entity.Contains("new_locum")}");

            ////content += !entity.Contains("new_practicestatus") ? @"""practiceStatus"":""""," : @"""practiceStatus"":""" + (TranslateToApplicationPracticeStatus((OptionSetValue)entity["new_practicestatus"], entity)) + @""",";
            ////content += !entity.Contains("new_dateretirement") ? @"""retirementDate"":""""," : @"""retirementDate"":""" + DateTime.Parse(entity["new_dateretirement"].ToString()).ToString("O") + @""",";

            ////content += !entity.Contains("new_cmah_id") ? @"""cmahID"":""""," : @"""cmahID"":""" + EscpateDoubleQuoteAndBackSlash(entity["new_cmah_id"] as string) + @""",";
            ////content += !entity.Contains("firstname") ? @"""firstName"":""""," : @"""firstName"":""" + EscpateDoubleQuoteAndBackSlash(entity["firstname"] as string) + @""",";
            ////content += !entity.Contains("lastname") ? @"""lastName"":""""," : @"""lastName"":""" + EscpateDoubleQuoteAndBackSlash(entity["lastname"] as string) + @""",";
            ////content += !entity.Contains("birthdate") ? @"""dateOfBirth"":""""," : @"""dateOfBirth"":""" + DateTime.Parse(entity["birthdate"].ToString()).ToString("O") + @""",";
            ////content += !entity.Contains("emailaddress1") ? @"""emailAddress"":""""," : @"""emailAddress"":""" + EscpateDoubleQuoteAndBackSlash(entity["emailaddress1"] as string) + @""",";
            ////content += !entity.Contains("language") ? @"""language"":""""," : @"""language"":""" + (((OptionSetValue)((AliasedValue)entity["language"]).Value).Value) + @""",";
            ////content += !entity.Contains("new_physicianpreferredname") ? @"""preferredName"":""""," : @"""preferredName"":""" + EscpateDoubleQuoteAndBackSlash(entity["new_physicianpreferredname"] as string) + @""",";
            //content += !entity.Contains("new_contactsubtypeid") ? @"""careerStatus"":""""," : @"""careerStatus"":""" + ((EntityReference)entity["new_contactsubtypeid"]).Id + @""",";
            //content += !entity.Contains("new_salutation") ? @"""salutation"":""""," : @"""salutation"":""" + ((OptionSetValue)entity["new_salutation"]).Value + @""",";
            //content += !entity.Contains("new_pronoun") ? @"""pronoun"":""""," : @"""pronoun"":""" + ((OptionSetValue)entity["new_pronoun"]).Value + @""",";
            //content += !entity.Contains("new_otherpronoun") ? @"""otherpronoun"":""""," : @"""otherpronoun"":""" + EscpateDoubleQuoteAndBackSlash(entity["new_otherpronoun"] as string) + @""",";
            //content += @"""membership"":{";
            //content += !entity.Contains("ptmaid") ? @"""ptma"":""""," : @"""ptma"":""" + getPtmaValue(entity) + @""",";
            //content += !entity.Contains("english") ? @"""ptmaenglish"":""""," : @"""ptmaenglish"":""" + (((AliasedValue)entity["english"]).Value).ToString() + @""",";
            //content += !entity.Contains("french") ? @"""ptmafrench"":""""," : @"""ptmafrench"":""" + (((AliasedValue)entity["french"]).Value).ToString() + @""",";
            //content += !entity.Contains("cmd.new_categoryproductid") ? @"""category"":""""," : @"""category"":""" + ((EntityReference)((AliasedValue)entity["cmd.new_categoryproductid"]).Value).Id + @""",";
            //content += !entity.Contains("ord.totalamount") ? @"""amount"":""""," : @"""amount"":""" + ((Money)((AliasedValue)entity["ord.totalamount"]).Value).Value + @""",";
            //content += !entity.Contains("ord.new_totallineitemdiscount") ? @"""discount"":""""," : @"""discount"":""" + ((Money)((AliasedValue)entity["ord.new_totallineitemdiscount"]).Value).Value + @""",";
            //content += !entity.Contains("ord.new_amountpaid") ? @"""payment"":""""," : @"""payment"":""" + ((Money)((AliasedValue)entity["ord.new_amountpaid"]).Value).Value + @""",";
            //content += !entity.Contains("ord.new_amountoutstanding") ? @"""balance"":""""," : @"""balance"":""" + ((Money)((AliasedValue)entity["ord.new_amountoutstanding"]).Value).Value + @""",";
            //content += !entity.Contains("ord.createdon") ? @"""receiptDate"":""""," : @"""receiptDate"":""" + ((DateTime)((AliasedValue)entity["ord.createdon"]).Value).ToString() + @""",";
            //content += !entity.Contains("memStatus") ? @"""status"":""""," : @"""status"":""" + ((OptionSetValue)((AliasedValue)entity["memStatus"]).Value).Value + @""",";
            //content += !entity.Contains("memExpDate") ? @"""exp"":""""," : @"""exp"":""" + ((DateTime)((AliasedValue)entity["memExpDate"]).Value).ToString("d") + @""",";
            //content += !entity.Contains("memYear") ? @"""year"":""""," : @"""year"":""" + (((AliasedValue)entity["memYear"]).Value).ToString() + @""",";
            //content += !entity.Contains("memYear") ? @"""can_join"":true," : @"""can_join"":false,";
            ////checkYearForJoin(service,ts,(((AliasedValue)entity["memYear"]).Value).ToString());
            ////content += !entity.Contains("memYear") ? @"""can_renew"":false" : checkYearForRenew(service, ts, (((AliasedValue)entity["memYear"]).Value).ToString());
            //content += "},";
            //content += @"""phones"":[";
            ////content += addPhone(entity, "telephone1") + ",";
            ////content += addPhone(entity, "telephone2") + ",";
            ////content += addPhone(entity, "mobilephone") + "],";
            //content += @"""education"":{";
            //content += !entity.Contains("new_collegeofgraduation") ? @"""medschool"":""""," : @"""medschool"":""" + ((EntityReference)entity["new_collegeofgraduation"]).Id + @""",";
            //content += !entity.Contains("new_yearenrolledinmedicalschool") ? @"""yearEnrolledMedSchool"":""""," : @"""yearEnrolledMedSchool"":""" + ((DateTime)entity["new_yearenrolledinmedicalschool"]).Year.ToString() + @""",";
            //content += !entity.Contains("new_fellowship") ? @"""fellowship"":""""," : @"""fellowship"":""" + ((OptionSetValue)entity["new_fellowship"]).Value + @""",";
            //content += !entity.Contains("new_universityofresidency") ? @"""universityOfResidency"":""""," : @"""universityOfResidency"":""" + ((EntityReference)entity["new_universityofresidency"]).Id + @""",";
            //content += !entity.Contains("new_residencyprogram") ? @"""residencyProgram"":""""," : @"""residencyProgram"":""" + ((OptionSetValue)entity["new_residencyprogram"]).Value + @""",";
            //content += !entity.Contains("new_residencycompletion") ? @"""expectedyearofcompletion"":""""," : @"""expectedyearofcompletion"":""" + entity["new_residencycompletion"] + @""",";
            //content += !entity.Contains("new_locum") ? @"""locum"":""""," : @"""locum"":""" + ((OptionSetValue)entity["new_locum"]).Value + @""",";
            //content += !entity.Contains("new_locum_estcompdate") ? @"""locumComplete"":""""" : @"""locumComplete"":""" + ((DateTime)entity["new_locum_estcompdate"]).ToString("d") + @"""";
            //content += "},";
            //content += @"""addresses"":[";
            ////content += addAddress(entity, "home") + ",";
            ////content += addAddress(entity, "office") + "],";

            ////// Order            
            ////var orderService = new OrderService();
            ////var orderDetails = GetOrderDetails(entity);

            //content += orderService
            //    .Build(orderDetails)
            //    .Serialize()
            //    .TrimOuterCurlyBraces() + ",";

            ////last one has no comma at the end
            ////bio
            //content += !entity.Contains("new_communityplatformbio") ? @"""bio"":""""" : @"""bio"":""" + EscpateDoubleQuoteAndBackSlash(entity["new_communityplatformbio"] as string) + @"""";

            content += "}";

            return content;
        }
    }
}
