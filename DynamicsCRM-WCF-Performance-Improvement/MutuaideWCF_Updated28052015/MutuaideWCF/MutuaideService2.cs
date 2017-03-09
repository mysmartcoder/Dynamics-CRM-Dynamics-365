using System.Configuration;
using System.Globalization;
using Microsoft.Xrm.Sdk;
using MutuaideWCF.DAL;
using MutuaideWCF.XRM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Xml.Linq;
using MutuaideWCF.Log;

namespace MutuaideWCF
{
    public partial class MutuaideService : IMutuaideService
    {
        //[DataMemberAttribute]
        //public readonly XrmServices crmServices = new XrmServices();


        /// <summary>
        /// RGI008  - La méthode permettra de récupérer une liste de porteurs. 
        /// </summary>
        /// <param name="lastName"></param>
        /// <param name="firstName"></param>
        /// <param name="birthday">DDMMYYYY</param>
        /// <returns></returns>
        public XElement RetrieveCustomer(string lastName, string firstName, string birthday)
        {
            Logfile.WriteLog(LogLevelL4N.INFO, "Start method execution : RetrieveCustomer");
            try
            {
                Logfile.WriteLog(LogLevelL4N.INFO, "lastName:" + lastName);
                Logfile.WriteLog(LogLevelL4N.INFO, "firstName:" + firstName);
                Logfile.WriteLog(LogLevelL4N.INFO, "birthday:" + birthday);

                Logfile.WriteLog(LogLevelL4N.INFO, "Start connecting CRM Org Service");
                crmService = new XrmServices();
                Logfile.WriteLog(LogLevelL4N.INFO, "CRM Org Service connected");

                DateTime bod;
                //string  CDO = "identifiant CDO";
                int bankcode = 30002;
                if (!string.IsNullOrEmpty(lastName) && !string.IsNullOrEmpty(firstName) && DateTime.TryParseExact(birthday, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out bod))
                {
                    string codeCdo = ConfigurationManager.AppSettings["cdovisainfinite"] ?? "";

                    Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetRequestById");
                    Entity contact = Contact.GetContactByName(lastName, firstName, bod, codeCdo, crmService);
                    Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetRequestById");
                    
                    if (contact != null && contact.Id != Guid.Empty)
                    {
                        Logfile.WriteLog(LogLevelL4N.INFO, "Customer Found");

                        string ville = string.Empty;
                        result.AddResultElement("cdoid", codeCdo);
                        result.AddResultElement("firstname", contact.GetAttributeValue<string>("firstname"));
                        result.AddResultElement("lastname", contact.GetAttributeValue<string>("lastname"));
                        if (contact.GetAttributeValue<EntityReference>("new_countryid") != null && contact.GetAttributeValue<EntityReference>("new_countryid").Name.ToLower() == "france")
                        {
                            ville = (contact.GetAttributeValue<EntityReference>("new_cityid") ?? new EntityReference()).Name ?? "";
                        }
                        else
                        {
                            ville = contact.GetAttributeValue<string>("new_foreigncity") ?? "";
                        }
                        // <city>Code postal + ville du client</city>

                        string city = (contact.GetAttributeValue<string>("address1_postalcode")) ?? "" + " " + ville;
                        Logfile.WriteLog(LogLevelL4N.INFO, "city: " + city);

                        result.AddResultElement("city", city);
                        result.AddResultElement("bankcode", bankcode);
                        result.AddResultElement("id_porteur", contact.GetAttributeValue<string>("new_idclientporteur"));
                        result.AddResultElement("pcidssid", (contact.Attributes.Contains("new_idcrm") ? contact.GetAttributeValue<string>("new_idcrm") : ""));


                        returnCode = (int)CustomReturnCode.Success;
                    }
                    else
                    {
                        Logfile.WriteLog(LogLevelL4N.INFO, "Customer Not Found");
                        returnCode = (int)CustomReturnCode.CustomerNotFound;
                    }
                }
                else
                {
                    Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:RetrieveCustomer(). 'Paramétres incorrects'");
                    throw new Exception("Paramètres incorrects.");
                }
                result.SetError(returnCode, customError.getErrorMessage(returnCode));

            }
            catch (Exception e)
            {
                Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:RetrieveCustomer(). Ex:" + e.Message);
                result.SetError((int)CustomReturnCode.Unknown, e.Message);
            }
            Logfile.WriteLog(LogLevelL4N.INFO, "Complete method execution : RetrieveCustomer");
            return result.getXml((Method == "post") ? new List<string> { lastName, firstName, birthday } : null);
        }


        /// <summary>
        /// RGI009 - Indique si le client à le statut actif dans la CRM 
        /// </summary>
        /// <param name="pcidssId">BIN du client </param>
        /// <returns></returns>
        public XElement IsCustomerActive(string pcidssId)
        {
            Logfile.WriteLog(LogLevelL4N.INFO, "Start method execution : IsCustomerActive");
            try
            {
                Logfile.WriteLog(LogLevelL4N.INFO, "pcidssId:" + pcidssId);

                Logfile.WriteLog(LogLevelL4N.INFO, "Start connecting CRM Org Service");
                crmService = new XrmServices();
                Logfile.WriteLog(LogLevelL4N.INFO, "CRM Org Service connected");

                Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetStatusContactByPciDssId");
                Entity contact = Contact.GetStatusContactByPciDssId(pcidssId, crmService);
                Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetStatusContactByPciDssId");
                
                if (contact == null)
                {
                    Logfile.WriteLog(LogLevelL4N.INFO, "Customer Not Found");
                    returnCode = (int)CustomReturnCode.CustomerNotFound;
                    result.SetError(returnCode, customError.getErrorMessage(returnCode));
                    Logfile.WriteLog(LogLevelL4N.INFO, "Complete method execution : IsCustomerActive");
                    return result.getXml((Method == "post") ? new List<string> { pcidssId } : null);
                }


                // test si le client est actif ou inactif
                // 0:actif   1:inactif
                if (contact.GetAttributeValue<OptionSetValue>("statecode").Value == 0)
                    result.AddResultElement("resultcode", "True");
                else
                    result.AddResultElement("resultcode", "False");

                
                result.SetError(returnCode, customError.getErrorMessage(returnCode));
            }
            catch (Exception e)
            {
                result.SetError((int)CustomReturnCode.Unknown, customError.getErrorMessage((int)CustomReturnCode.Unknown));
                Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:UpdatePaymentMethod(). Ex:" + e.Message);
            }
            Dispose();

            Logfile.WriteLog(LogLevelL4N.INFO, "Complete method execution : IsCustomerActive");
            return result.getXml((Method == "post") ? new List<string> { pcidssId } : null);

        }

        /// <summary>
        /// RGI010  - La méthode permet de récupérer la liste des 100 dernières demandes effectuées par un client. 
        /// </summary>
        /// <param name="pcidssId"></param>
        /// <returns></returns>
        public XElement GetLastRequest(string pcidssId)
        {
            Logfile.WriteLog(LogLevelL4N.INFO, "Start method execution : GetLastRequest");
            try
            {   
                Logfile.WriteLog(LogLevelL4N.INFO, "pcidssId:" + pcidssId);

                Logfile.WriteLog(LogLevelL4N.INFO, "Start connecting CRM Org Service");
                crmService = new XrmServices();
                Logfile.WriteLog(LogLevelL4N.INFO, "CRM Org Service connected");

                Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetContactById");
                Entity contact = Contact.GetContactById(pcidssId, crmService);
                Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetContactById");                

                if (contact.Id == Guid.Empty)
                {
                    returnCode = (int)CustomReturnCode.CustomerNotFound;
                    Logfile.WriteLog(LogLevelL4N.INFO, "Customer not Found");
                }
                else
                {
                    Logfile.WriteLog(LogLevelL4N.INFO, "Customer  Found");

                    Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetLastRequestContact");
                    var listeIncident = Contact.GetLastRequestContact(pcidssId, contact.Id, crmService);
                    Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetLastRequestContact");  
                    

                    if (listeIncident.Any())
                    {
                        Logfile.WriteLog(LogLevelL4N.INFO, "Multiple Request Found");

                        listeIncident.ForEach(delegate(Entity item)
                        {
                            string status = "";
                            switch (item.GetAttributeValue<OptionSetValue>("statecode").Value)
                            {
                                case 0:
                                    status = "CREATED";
                                    break;
                                case 1:
                                    status = "CLOSED";
                                    break;
                                case 2:
                                    status = "CANCEL";
                                    break;

                            }


                            result.AddResultElement(new XElement("request",
                                new XElement("casenumber", item.GetAttributeValue<string>("new_demandnumber")),
                                new XElement("status", status),
                                new XElement("createdon", item.GetAttributeValue<DateTime>("createdon")),
                                new XElement("comments", item.GetAttributeValue<string>("description")),
                                new XElement("prestations", item.GetAttributeValue<List<Entity>>("prestations")
                                        .Select(s => new XElement("prestation",
                                            new XElement("value", s.GetAttributeValue<Entity>("subtype").GetAttributeValue<string>("new_code")),
                                            new XElement("text", s.GetAttributeValue<Entity>("subtype").GetAttributeValue<string>("new_label")))).ToList())));
                        });

                        returnCode = (int)CustomReturnCode.MultipleRequest;
                    }
                    else
                    {
                        Logfile.WriteLog(LogLevelL4N.INFO, "Request not Found");
                        returnCode = (int)CustomReturnCode.RequestNotFound;
                    }
                }
                result.SetError(returnCode, customError.getErrorMessage(returnCode));
            }
            catch (Exception e)
            {
                result.SetError((int)CustomReturnCode.Unknown, e.Message);
                Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:UpdatePaymentMethod(). Ex:" + e.Message);
            }
            Dispose();

            Logfile.WriteLog(LogLevelL4N.INFO, "Complete method execution : GetLastRequest");
            return result.getXml((Method == "post") ? new List<string> { pcidssId } : null);

        }


    }
}