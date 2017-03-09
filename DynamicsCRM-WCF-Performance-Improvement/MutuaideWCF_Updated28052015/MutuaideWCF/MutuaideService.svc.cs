using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using Microsoft.Xrm.Sdk;
using MutuaideWCF.DAL;
using MutuaideWCF.Log;
using MutuaideWCF.XRM;

namespace MutuaideWCF
{
    /// <summary>
    /// WebService contenant les méthodes implémentées exposées dans l'interface
    /// </summary>
    /// <exclude/>
    public partial class MutuaideService : IDisposable
    {
        [DataMemberAttribute]
        public XmlResult result = new XmlResult();

        private XrmServices crmService;
        private int returnCode;
        private string Method = WebOperationContext.Current.IncomingRequest.Method.ToLower();

        #region Methodes exposées

        /// <summary>
        /// Retourne le mail et eventuellement la demande ou le client auquel il est associé.
        /// </summary>
        /// <param name="messageId">Id exchange du mail</param>
        /// <returns>
        ///<para><![CDATA[<error>]]></para>
        ///<para><![CDATA[  <value>Code Erreur (1,2,4,5,6,7,8,9)</value>]]></para>
        ///<para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        ///<para><![CDATA[</error>]]></para>
        ///<para><![CDATA[<result>]]></para>
        ///<para><![CDATA[  <activityid>Identifiant du mail</activityid>]]></para>
        ///<para><![CDATA[  <demandeid>Identifiant de la demande</demandeid>]]></para>
        ///<para><![CDATA[  <customerid>Identifiant du client</customerid>]]></para>
        ///<para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// Flux froids, étape 1. (chapitre 5.2.4.5.1 des specs)
        /// </remarks>
        public XElement GetEmailByExchangeId(string messageId)
        {
            Logfile.WriteLog(LogLevelL4N.INFO, "Start method execution : GetEmailByExchangeId");
            try
            {


                if (string.IsNullOrEmpty(messageId))
                {
                    throw new Exception("L'id exchange ne peut pas être null.");
                }
                Logfile.WriteLog(LogLevelL4N.INFO, "MessageId : " + messageId);

                Logfile.WriteLog(LogLevelL4N.INFO, "Start connecting CRM Org Service");
                crmService = new XrmServices();
                Logfile.WriteLog(LogLevelL4N.INFO, "CRM Org Service connected");

                Logfile.WriteLog(LogLevelL4N.INFO, "Start getting Email report from MessageId. Method : GetEmail");
                Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetEmail");
                Entity email = Activity.GetEmail(messageId, crmService);
                Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetEmail");

                if (email != null && email.Id != Guid.Empty)
                {
                    Logfile.WriteLog(LogLevelL4N.INFO, "Email activity record Found Id:" + email.Id.ToString());

                    Logfile.WriteLog(LogLevelL4N.INFO, "Start getting Email activity Queue. Method : GetActivityQueue");
                    Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetActivityQueue");
                    var queue = Activity.GetActivityQueue(email.Id, crmService);
                    Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetActivityQueue");

                    result.AddResultElement("activityid", email.Id.ToString());
                    if (queue == null)
                    {
                        throw new Exception("Le mail n'est pas attribué à une file d'attente.");
                    }


                    if (email.Attributes.Contains("new_status"))
                    {
                        int status = email.GetAttributeValue<OptionSetValue>("new_status").Value;
                        Logfile.WriteLog(LogLevelL4N.INFO, "Email activity contain new_status: " + status);
                        //Si le mail est au statut <à traiter> ou <rejeté>.
                        if (status == 100000000 || status == 100000002)
                        {
                            //S'il est différent de crm-admin c'est qu'un concierge traite déjà ce mail.
                            if (email.GetAttributeValue<EntityReference>("ownerid").Id != crmService.callerId.Id)
                            {
                                Logfile.WriteLog(LogLevelL4N.INFO, "Email's OwnerId is not match with Service's CallerId");
                                returnCode = (int)CustomReturnCode.MailInProgress;
                            }
                            else
                            {
                                Logfile.WriteLog(LogLevelL4N.INFO, "Email's OwnerId is match with Service's CallerId");
                                //Entity queue = Activity.GetActivityQueue(email.Id, crmService);

                                if (queue.Attributes.Contains("new_contractid")) // GetAttributeValue<EntityReference>("new_contractid") != null && queue.GetAttributeValue<EntityReference>("new_contractid").Id != Guid.Empty)
                                {
                                    Logfile.WriteLog(LogLevelL4N.INFO, "Email activity Queue contain new_contractid: " + queue.GetAttributeValue<EntityReference>("new_contractid").Name);

                                    Guid contractId = queue.GetAttributeValue<EntityReference>("new_contractid").Id;

                                    if (queue.Attributes.Contains("new_type"))
                                    {
                                        Logfile.WriteLog(LogLevelL4N.INFO, "Email activity Queue contain new_type: " + ((OptionSetValue)queue["new_type"]).Value);

                                        switch (queue.GetAttributeValue<OptionSetValue>("new_type").Value)
                                        {
                                            #region Email

                                            case 100000000:

                                                EntityCollection froms = email.GetAttributeValue<EntityCollection>("from");

                                                // MK
                                                Entity sender = null;
                                                if ((froms.Entities.Count > 0) && froms.Entities[0].Contains("partyid") && (froms.Entities[0].GetAttributeValue<EntityReference>("partyid") != null) && (froms.Entities[0].GetAttributeValue<EntityReference>("partyid").LogicalName.ToLower() == "contact"))
                                                {
                                                    sender = froms.Entities[0];
                                                }

                                                if (sender != null && sender.Id != Guid.Empty)
                                                {
                                                    Logfile.WriteLog(LogLevelL4N.INFO, "Sender found in Email activity record");

                                                    List<Entity> contactList =
                                                        Contact.GetContactByEmailOnContract(
                                                            contractId,
                                                            sender.GetAttributeValue<string>("addressused"),
                                                            crmService);
                                                    Logfile.WriteLog(LogLevelL4N.INFO, "Contacts in Sender field count : " + contactList.Count.ToString());

                                                    Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : CheckRequest");
                                                    CheckRequest(contactList, email, crmService, "email");
                                                    Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : CheckRequest");

                                                }

                                                else
                                                {
                                                    Logfile.WriteLog(LogLevelL4N.INFO, "Sender not found in Email activity record");
                                                    returnCode = (int)CustomReturnCode.CustomerNotFound;
                                                }
                                                break;

                                            #endregion

                                            #region Fax

                                            case 100000001:


                                                var regResult = Regex.Match(email.GetAttributeValue<string>("subject").Replace(" ", ""),
                                                    @"(\+\d{2,3}.?\d{9}?)", RegexOptions.IgnorePatternWhitespace);

                                                if (regResult.Groups.Count > 0)
                                                {
                                                    Logfile.WriteLog(LogLevelL4N.INFO, "Fax number : " + regResult.ToString());
                                                    string faxNumber = regResult.ToString();
                                                    //Mail de type FAX
                                                    email.Attributes.Add("new_type", new OptionSetValue(100000005));
                                                    var customerList = Contact.GetContactByPhoneOnContract(contractId, faxNumber, crmService);

                                                    Logfile.WriteLog(LogLevelL4N.INFO, "Contacts in Sender field count : " + customerList.Count.ToString());

                                                    Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : CheckRequest");
                                                    CheckRequest(customerList, email, crmService, "fax");
                                                    Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : CheckRequest");

                                                }
                                                else
                                                {
                                                    Logfile.WriteLog(LogLevelL4N.INFO, "Customer not found in Fax activity record");
                                                    returnCode = (int)CustomReturnCode.CustomerNotFound;
                                                }


                                                break;

                                            #endregion

                                            default:
                                                Logfile.WriteLog(LogLevelL4N.ERROR, "L'email ne répond pas aux critères de traitement.");
                                                throw new Exception("L'email ne répond pas aux critères de traitement.");
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    #region SmartPhone / SMS

                                    #region Traitement Smartphone

                                    Entity updateEmail = new Entity("email");
                                    updateEmail.Id = email.Id;
                                    if (queue.GetAttributeValue<string>("name").ToLower().Contains("smartphone"))
                                    {
                                        Logfile.WriteLog(LogLevelL4N.INFO, "Traitement Smartphone");
                                        //Charge le xml du mail pour créer la demande.
                                        var xml =
                                            Activity.GetEmailAttachement(
                                                email.GetAttributeValue<string>("description"), queue.GetAttributeValue<string>("name").ToLower()).Root.Elements().FirstOrDefault();
                                        XNamespace RootNamespace = xml.GetDefaultNamespace();
                                        XNamespace requestNs = xml.Element(RootNamespace + "request").GetNamespaceOfPrefix("a");

                                        var request = xml.Element(RootNamespace + "request");

                                        Entity customer = Contact.GetContactById(xml.Element(RootNamespace + "userid").Value, crmService);

                                        updateEmail.Attributes.Add("new_customeridentification", xml.Element(RootNamespace + "userid").Value);
                                        updateEmail.Attributes.Add("new_type", new OptionSetValue(100000003));
                                        if (customer.Id != Guid.Empty)
                                        {
                                            Logfile.WriteLog(LogLevelL4N.INFO, "Customer found in Smartphone XML");
                                            Entity newIncident = new Entity("incident")
                                            {
                                                Attributes = new AttributeCollection()
                                                            {
                                                                new KeyValuePair<string, object>("customerid", customer.ToEntityReference()),
                                                                new KeyValuePair<string, object>("new_callerphonenumber",request.Element(requestNs + "phone").Value)
                                                                //new KeyValuePair<string, object>("customerid", customer.Id)
                                                            }
                                            };
                                            try
                                            {
                                                newIncident.Id = crmService.GetService().Create(newIncident);

                                                result.AddResultElement("demandeid", newIncident.Id);
                                                if (newIncident.Id != Guid.Empty)
                                                {
                                                    Logfile.WriteLog(LogLevelL4N.INFO, "Case record is created. CaseId:" + newIncident.Id.ToString());
                                                    //email.Attributes.Remove("regardingobjectid");
                                                    updateEmail.Attributes.Add("regardingobjectid", newIncident.ToEntityReference());

                                                    string commentaire = request.Element(requestNs + "text").Value;
                                                    string typeCode = request.Element(requestNs + "typecode").Value;
                                                    string customerEmail = request.Element(requestNs + "email").Value;

                                                    if (Contact.UpdateEmail(customerEmail, ref customer))
                                                    {
                                                        crmService.Update(customer);
                                                    }
                                                    else
                                                    {
                                                        commentaire += "\n" + customerEmail;
                                                    }

                                                    #region création prestation
                                                    Entity newPrestation = new Entity("new_prestation")
                                                    {

                                                        Attributes = new AttributeCollection()
                                                                    {
                                                                        new KeyValuePair<string, object>("new_demandid",
                                                                            newIncident.ToEntityReference()),
                                                                        new KeyValuePair<string, object>("new_subtypeid",
                                                                            Prestation.GetSubTypeId(typeCode, crmService).ToEntityReference()),
                                                                        new KeyValuePair<string, object>("new_categoryid",
                                                                            Prestation.GetCategoryId("services", crmService)),
                                                                        new KeyValuePair<string, object>("new_description",
                                                                            commentaire)
                                                                    }
                                                    };

                                                    Guid newPrestationId = crmService.GetService().Create(newPrestation);

                                                    #endregion

                                                    if (newPrestationId != Guid.Empty)
                                                    {
                                                        Logfile.WriteLog(LogLevelL4N.INFO, "Prestation record is created. PrestationId :" + newPrestationId.ToString());
                                                        returnCode = (int)CustomReturnCode.NewRequest;
                                                    }
                                                    //newPrestation.Attributes.Add();
                                                }

                                            }
                                            catch (Exception e)
                                            {
                                                result.SetError(-1,
                                                    "Impossible de créer la demande et ses prestations :\n" +
                                                    e.Message, e);
                                                //                                            new KeyValuePair<string, object>("title",
                                                //request.Element(requestNs + "text").Value)
                                                Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:GetEmailByExchangeId() - Region:Traitement Smartphone. Ex:" + e.Message);
                                            }
                                            crmService.Update(updateEmail);
                                        }
                                        else
                                        {
                                            Logfile.WriteLog(LogLevelL4N.INFO, "Customer not found in SmartPhone activity record");
                                            returnCode = (int)CustomReturnCode.CustomerNotFound;
                                        }


                                    }
                                    #endregion

                                    #region SMS
                                    //File d'attente SMS
                                    else
                                    {
                                        if (queue.GetAttributeValue<string>("name").ToLower().Contains("sms"))
                                        {
                                            Logfile.WriteLog(LogLevelL4N.INFO, "Traitement SMS");

                                            Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetEmailAttachement");
                                            var number = Activity.GetEmailAttachement(email.GetAttributeValue<string>("description"), queue.GetAttributeValue<string>("name").ToLower()).Root;
                                            Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetEmailAttachement");

                                            Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetContractByPhone");
                                            Guid contractId = Contrat.GetContractByPhone("+" + number.Element("font").Value, crmService);
                                            Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetContractByPhone");

                                            if (contractId != Guid.Empty)
                                            {
                                                List<Entity> contact = Contact.GetContactByPhoneOnContract(contractId, "+" + number.Elements("font").ToList()[1].Value, crmService);

                                                Logfile.WriteLog(LogLevelL4N.INFO, "Contacts count : " + contact.Count.ToString());

                                                Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : CheckRequest");
                                                CheckRequest(contact, updateEmail, crmService, queue.GetAttributeValue<string>("name"));
                                                Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : CheckRequest");
                                            }
                                            else
                                            {
                                                Logfile.WriteLog(LogLevelL4N.INFO, "Contact not found in SMS activity record");
                                                returnCode = (int)CustomReturnCode.ContractNotFound;
                                            }

                                            //
                                        }
                                        else
                                        {
                                            Logfile.WriteLog(LogLevelL4N.INFO, "Contact not found in SMS activity record");
                                            returnCode = (int)CustomReturnCode.ContractNotFound;
                                        }
                                    }
                                    #endregion

                                    #endregion

                                }

                            }

                        }
                        else
                        {
                            if (status == 100000001)
                            {
                                Logfile.WriteLog(LogLevelL4N.INFO, "Mail activity record is Closed");
                                returnCode = (int)CustomReturnCode.MailClosed;
                            }
                        }

                    }
                }
                else
                {
                    returnCode = (int)CustomReturnCode.MailNotFound;
                    Logfile.WriteLog(LogLevelL4N.INFO, "Mail activity record not found");
                    //result.SetError((int)CustomReturnCode.MailNotFound, customError.getErrorMessage((int)CustomReturnCode.MailNotFound));
                }
                result.SetError(returnCode, customError.getErrorMessage(returnCode));
            }
            catch (Exception e)
            {
                Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:GetEmailByExchangeId(). Ex:" + e.Message);
                result.SetError((int)CustomReturnCode.Unknown, e.Message, e);
            }
            Logfile.WriteLog(LogLevelL4N.INFO, "Complete method execution : GetEmailByExchangeId");
            Dispose();
            return result.getXml((Method == "post") ? new List<string> { messageId } : null);
        }

        /// <summary>
        /// Affecte le mail au concierge passé en paramètre
        /// </summary>
        /// <param name="messageId">id exchange du mail</param>
        /// <param name="domainName">Domaine du concierge</param>
        /// <param name="userName">Login du concierge</param>
        /// <returns>Retourne le résultat au format XML suivant :
        /// <para><![CDATA[<error>]]></para>
        ///<para><![CDATA[  <value>Code Erreur (spécifique)</value>]]></para>
        ///<para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        ///<para><![CDATA[</error>]]></para>
        ///<para><![CDATA[<result>]]></para>
        ///<para><![CDATA[  <value>True / False</value>]]></para>
        ///<para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// Flux froids, étape 2. (chapitre 5.2.4.5.2 des specs)
        /// </remarks>
        public XElement UpdateEmailByExchangeId(string messageId, string domainName = "", string userName = "")
        {
            Logfile.WriteLog(LogLevelL4N.INFO, "Start method execution : UpdateEmailByExchangeId");
            //return new XElement("root");
            //return new XElement("");
            //string resultMessage = string.Empty;
            bool ownerChanged = false;
            try
            {
                Logfile.WriteLog(LogLevelL4N.INFO, "Start connecting CRM Org Service");
                crmService = new XrmServices();
                Logfile.WriteLog(LogLevelL4N.INFO, "CRM Org Service connected");

                string user = domainName + "\\" + userName;
                Logfile.WriteLog(LogLevelL4N.INFO, "User:" + user);

                Logfile.WriteLog(LogLevelL4N.INFO, "Start getting Email from MessageId. Method : GetEmailById");
                Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetEmailById");
                EntityReference email = Activity.GetEmailById(messageId, crmService);
                Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetEmailById");

                if (email != null && email.Id != Guid.Empty)
                {
                    Logfile.WriteLog(LogLevelL4N.INFO, "Email record Found : " + email.Id.ToString());
                    //Activity.ReopenActivity(email, crmService);

                    EntityReference newUser = SystemUser.GetUserFromLogin(user, crmService);

                    if (newUser != null && newUser.Id != Guid.Empty)
                    {
                        Logfile.WriteLog(LogLevelL4N.INFO, "CRM User record Found for user: " + user);
                        Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : ChangeOwner");
                        XrmUtility.ChangeOwner(email, newUser, crmService);
                        Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : ChangeOwner");

                        Entity updateEmail = new Entity("email");
                        updateEmail.Id = email.Id;
                        //Status du mail à "A traiter"
                        updateEmail.Attributes.Add("new_status", new OptionSetValue(100000000));
                        crmService.Update(updateEmail);
                        //Activity.CloseActivity(email, crmService);
                        ownerChanged = true;
                        // resultMessage = XrmUtility.FormatUrl(email.Id, email.LogicalName.ToLower());
                    }
                    else
                    {
                        Logfile.WriteLog(LogLevelL4N.INFO, "CRM User record not found for user: " + user);
                    }

                }
                else
                {
                    Logfile.WriteLog(LogLevelL4N.INFO, "Mail activity record not found");
                    returnCode = (int)CustomReturnCode.MailNotFound;
                }

                result.SetError(returnCode, customError.getErrorMessage(returnCode));

            }
            catch (Exception ex)
            {
                Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:UpdateEmailByExchangeId(). Ex:" + ex.Message);
                result.SetError((int)CustomReturnCode.Unknown, ex.Message, ex);
            }
            result.AddResultElement("value", ownerChanged);
            Logfile.WriteLog(LogLevelL4N.INFO, "Complete method execution : UpdateEmailByExchangeId");
            return result.getXml((Method == "post") ? new List<string> { messageId, domainName, userName } : null);
            //return resultMessage;
        }


        /// <summary>
        /// Retourne une demande CRM ou un client à partir d'un numéro de téléphone
        /// </summary>
        /// <param name="phoneNumber">Numéro de téléphone associé au client</param>
        /// <param name="dialNumber">Numéro de téléphone du contrat composé</param>
        /// <param name="phoneCallId">Identifiant de l'appel téléphonique</param>
        /// <example></example>
        /// <returns>Retourne le résultat au format XML suivant :
        /// <para><![CDATA[<error>]]></para>
        ///<para><![CDATA[  <value>Code Erreur (1,2,3,4,5)</value>]]></para>
        ///<para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        ///<para><![CDATA[</error>]]></para>
        ///<para><![CDATA[<result>]]></para>
        ///<para><![CDATA[  <demandeid></demandeid>]]></para>
        ///<para><![CDATA[  <conciergeid></conciergeid>]]></para>
        ///<para><![CDATA[  <contactid></contactid>]]></para>
        ///<para><![CDATA[</result>]]></para>        
        ///  </returns>
        /// <remarks>
        /// Flux chauds. (chapitre 5.2.3.2 des specs)
        /// </remarks>
        public XElement GetRequestByPhoneNumber(string phoneNumber, string dialNumber, string phoneCallId)
        {
            Logfile.WriteLog(LogLevelL4N.INFO, "Start method execution : GetRequestByPhoneNumber");
            try
            {
                Logfile.WriteLog(LogLevelL4N.INFO, "phoneNumber:" + phoneNumber);
                Logfile.WriteLog(LogLevelL4N.INFO, "dialNumber:" + dialNumber);
                Logfile.WriteLog(LogLevelL4N.INFO, "phoneCallId:" + phoneCallId);

                Logfile.WriteLog(LogLevelL4N.INFO, "Start connecting CRM Org Service");
                crmService = new XrmServices();
                Logfile.WriteLog(LogLevelL4N.INFO, "CRM Org Service connected");

                //vérifie que les paramètres ne soient pas null
                if (!string.IsNullOrEmpty(phoneNumber) && !string.IsNullOrEmpty(dialNumber) &&
                    !string.IsNullOrEmpty(phoneCallId))
                {
                    //on prépare un objet vide dans le cas ou rien n'est trouvé.
                    Entity phoneCall = new Entity("phonecall");
                    phoneCall.Attributes.Add("new_callid", phoneCallId);
                    phoneCall.Attributes.Add("regardingobjectid", null);
                    phoneCall.Attributes.Add("phonenumber", phoneNumber);
                    //suppression des espaces pour les numéros e164
                    phoneNumber = phoneNumber.Replace(" ", "");
                    dialNumber = dialNumber.Replace(" ", "");
                    //récupération du Compte Distributeur

                    Logfile.WriteLog(LogLevelL4N.INFO, "get Contract from dialNumber. Method : GetContractByPhone");
                    Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetContractByPhone");
                    Guid contractId = Contrat.GetContractByPhone(dialNumber, crmService);
                    Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetContractByPhone");

                    if (contractId != null && contractId != Guid.Empty)
                    {
                        Logfile.WriteLog(LogLevelL4N.INFO, "Contract Found with Id: " + contractId.ToString());
                        //Récupération des contacts du Distributeur du phoneNumber.
                        var contact = Contact.GetContactByPhoneOnContract(contractId, phoneNumber, crmService);

                        Logfile.WriteLog(LogLevelL4N.INFO, "Contact count : " + contact.Count.ToString());

                        Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : CheckRequest");
                        CheckRequest(contact, phoneCall, crmService, "phonecall");
                        Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : CheckRequest");

                    }
                    else
                    {
                        Logfile.WriteLog(LogLevelL4N.INFO, "Contract Not Found");
                        returnCode = (int)CustomReturnCode.ContractNotFound;
                    }
                }
                else
                {
                    Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:GetRequestByPhoneNumber(). 'Paramétres incorrects'");
                    throw new Exception("Paramétres incorrects");
                }
                Dispose();
                result.SetError(returnCode, customError.getErrorMessage(returnCode));
            }
            catch (Exception e)
            {
                Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:GetRequestByPhoneNumber(). Ex:" + e.Message);
                result.SetError((int)CustomReturnCode.Unknown, e.Message, e);
            }
            Dispose();
            Logfile.WriteLog(LogLevelL4N.INFO, "Complete method execution : GetRequestByPhoneNumber");
            return result.getXml((Method == "post") ? new List<string> { phoneNumber, dialNumber, phoneCallId } : null);
        }


        /// <summary>
        /// Retourne une partie de l'url de la fiche client et crée automatiquement une activité "Appel Téléphonique" dans la CRM liée au client.
        /// </summary>
        /// <param name="pcidssId">identifiant client (pcidss)</param>
        /// <param name="callerId">identifiant de l'appel téléphonique (Call-id)</param>
        /// <returns>Retourne le résultat au format XML suivant :
        /// <para><![CDATA[<error>]]></para>
        ///<para><![CDATA[  <value>Code Erreur (spécifique)</value>]]></para>
        ///<para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        ///<para><![CDATA[</error>]]></para>
        ///<para><![CDATA[<result>]]></para>
        ///<para><![CDATA[  <url></url>]]></para>
        ///<para><![CDATA[  <status> 100000001 (Consommateur) / 100000002 (Non intéressé) / 100000000 (Enregistré) / 100000003 (Dormant) </status>]]></para>
        ///<para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// (chapitre 6 des specs)
        /// </remarks>
        public XElement WelcomeCall(string pcidssId, string callerId)
        {
            Logfile.WriteLog(LogLevelL4N.INFO, "Start method execution : WelcomeCall");
            try
            {
                Logfile.WriteLog(LogLevelL4N.INFO, "pcidssId:" + pcidssId);
                Logfile.WriteLog(LogLevelL4N.INFO, "callerId:" + callerId);

                Logfile.WriteLog(LogLevelL4N.INFO, "Start connecting CRM Org Service");
                crmService = new XrmServices();
                Logfile.WriteLog(LogLevelL4N.INFO, "CRM Org Service connected");

                if (!string.IsNullOrEmpty(pcidssId))
                {
                    Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetContactById");
                    Entity contact = Contact.GetContactById(pcidssId, crmService);
                    Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetContactById");

                    if (contact.Id != Guid.Empty)
                    {
                        Logfile.WriteLog(LogLevelL4N.INFO, "Costomer Found");
                        crmService.GetService().Create(new Entity("phonecall")
                        {
                            Attributes = new AttributeCollection()
                            {
                                new KeyValuePair<string, object>("regardingobjectid",contact.ToEntityReference()),
                                new KeyValuePair<string, object>("new_callid",callerId),
                                new KeyValuePair<string, object>("new_iswelcomecall",true),
                                new KeyValuePair<string, object>("scheduledstart",DateTime.Now),
                            }
                        });
                        Logfile.WriteLog(LogLevelL4N.INFO, "Phone Call activity created");

                        result.AddResultElement("url", XrmUtility.FormatUrl(contact.Id, contact.LogicalName.ToLower()));
                        result.AddResultElement("status", contact.GetAttributeValue<OptionSetValue>("statuscode").Value);

                        returnCode = (int)CustomReturnCode.Success;
                    }
                    else
                    {
                        Logfile.WriteLog(LogLevelL4N.INFO, "Costomer Not Found");
                        returnCode = (int)CustomReturnCode.CustomerNotFound;
                    }
                }
                else
                {
                    Logfile.WriteLog(LogLevelL4N.INFO, "Costomer Not Found");
                    returnCode = (int)CustomReturnCode.CustomerNotFound;
                }
                result.SetError(returnCode, customError.getErrorMessage(returnCode));
            }
            catch (Exception ex)
            {
                Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:WelcomeCall(). Ex:" + ex.Message);
                result.SetError((int)CustomReturnCode.Unknown, ex.Message, ex);
            }
            Dispose();
            Logfile.WriteLog(LogLevelL4N.INFO, "Complete method execution : WelcomeCall");
            return result.getXml((Method == "post") ? new List<string> { pcidssId, callerId } : null);
        }


        /// <summary>
        /// Retourne le code contrat correspondant au BIN
        /// </summary>
        /// <param name="binCode">Code BIN</param>
        /// <returns>Retourne le résultat au format XML suivant :
        /// <para><![CDATA[<error>]]></para>
        /// <para><![CDATA[  <value>Code Erreur (2)</value>]]></para>
        /// <para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        /// <para><![CDATA[</error>]]></para>
        /// <para><![CDATA[<result>]]></para>
        /// <para><![CDATA[  <ContractCode></ContractCode>]]></para>
        /// <para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// PCIDSS. (chapitre 7 des specs)
        /// </remarks>
        public XElement GetContractByBin(string binCode)
        {
            Logfile.WriteLog(LogLevelL4N.INFO, "Start method execution : GetContractByBin");
            try
            {
                Logfile.WriteLog(LogLevelL4N.INFO, "binCode:" + binCode);

                Logfile.WriteLog(LogLevelL4N.INFO, "Start connecting CRM Org Service");
                crmService = new XrmServices();
                Logfile.WriteLog(LogLevelL4N.INFO, "CRM Org Service connected");

                Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetContactById");
                Entity contract = Contrat.GetContractNumberByBin(int.Parse(binCode), crmService);
                Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetContactById");

                if (contract != null && contract.Id != Guid.Empty)
                {
                    if (contract.Attributes.Contains("new_contractnumber"))
                    {
                        string contractNumber = contract.GetAttributeValue<string>("new_contractnumber");

                        result.AddResultElement("contractcode", contractNumber);
                        returnCode = (int)CustomReturnCode.Success;
                    }
                    else
                    {
                        Logfile.WriteLog(LogLevelL4N.INFO, "Contract Number Not Found");
                        returnCode = (int)CustomReturnCode.ContractNotFound;
                    }
                }
                else
                {
                    Logfile.WriteLog(LogLevelL4N.INFO, "Contract Not Found");
                    returnCode = (int)CustomReturnCode.ContractNotFound;
                }
                result.SetError(returnCode, customError.getErrorMessage(returnCode));
            }
            catch (Exception e)
            {
                Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:GetContractByBin(). Ex:" + e.Message);
                result.SetError((int)CustomReturnCode.Unknown, e.Message, e);
            }
            Dispose();
            Logfile.WriteLog(LogLevelL4N.INFO, "Complete method execution : GetContractByBin");
            return result.getXml((Method == "post") ? new List<string> { binCode } : null);
        }


        /// <summary>
        /// Permet de modifier le bin d'un client
        /// </summary>
        /// <param name="pcidssId">Identifiant du client</param>
        /// <param name="binCode">Code BIN à modifier</param>
        /// <returns>Retourne le résultat au format XML suivant :
        /// <para><![CDATA[<error>]]></para>
        /// <para><![CDATA[  <value>Code Erreur (spécifique)</value>]]></para>
        /// <para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        /// <para><![CDATA[</error>]]></para>
        /// <para><![CDATA[<result>]]></para>
        /// <para><![CDATA[  <resultCode>true / false</resultCode>]]></para>
        /// <para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// PCIDSS. (chapitre 7 des specs)
        /// </remarks>
        public XElement UpdateCustomerBin(string pcidssId, string binCode)
        {
            bool isUpdated = false;
            Logfile.WriteLog(LogLevelL4N.INFO, "Start method execution : UpdateCustomerBin");
            try
            {
                Logfile.WriteLog(LogLevelL4N.INFO, "pcidssId:" + pcidssId);
                Logfile.WriteLog(LogLevelL4N.INFO, "binCode:" + binCode);

                Logfile.WriteLog(LogLevelL4N.INFO, "Start connecting CRM Org Service");
                crmService = new XrmServices();
                Logfile.WriteLog(LogLevelL4N.INFO, "CRM Org Service connected");

                int codeBin;
                if (!string.IsNullOrEmpty(pcidssId) && !string.IsNullOrEmpty(binCode) && int.TryParse(binCode.Replace(" ", ""), out codeBin))
                {
                    Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetContractNumberByBin");
                    Entity contractId = Contrat.GetContractNumberByBin(codeBin, crmService);
                    Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetContractNumberByBin");

                    if (contractId != null && contractId.Id != Guid.Empty)
                    {
                        Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetContactById");
                        Entity updateContact = Contact.GetContactById(pcidssId, crmService);
                        Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetContactById");

                        if (updateContact != null && updateContact.Id != Guid.Empty)
                        {
                            updateContact.Attributes.Add("new_bin", codeBin);
                            updateContact.Attributes.Add("new_contractid", contractId.ToEntityReference());
                            crmService.Update(updateContact);
                            isUpdated = true;
                            result.SetError((int)CustomReturnCode.Success, customError.getErrorMessage((int)CustomReturnCode.Success));

                            Logfile.WriteLog(LogLevelL4N.INFO, "Costomer Found");
                        }
                        else
                        {
                            Logfile.WriteLog(LogLevelL4N.INFO, "Costomer Not Found");
                            result.SetError((int)CustomReturnCode.CustomerNotFound, customError.getErrorMessage((int)CustomReturnCode.CustomerNotFound));
                        }
                    }
                    else
                    {
                        Logfile.WriteLog(LogLevelL4N.INFO, "Contract Not Found");
                        result.SetError((int)CustomReturnCode.ContractNotFound, customError.getErrorMessage((int)CustomReturnCode.ContractNotFound));
                    }
                }
                else
                {
                    Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:UpdateCustomerBin(). 'Paramétres incorrects'");
                    throw new Exception("Paramétre incorrect");
                }
            }
            catch (Exception e)
            {
                Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:UpdateCustomerBin(). Ex:" + e.Message);
                result.SetError((int)CustomReturnCode.Unknown, e.Message, e);
            }
            Dispose();
            result.AddResultElement("resultcode", isUpdated);
            Logfile.WriteLog(LogLevelL4N.INFO, "Complete method execution : UpdateCustomerBin");
            return result.getXml((Method == "post") ? new List<string> { pcidssId, binCode } : null);

        }

        /// <summary>
        /// Modifie le moyen de paiement d'une demande.
        /// </summary>
        /// <param name="demandeId">Identifiant unique de la demande CRM</param>
        /// <param name="paymentId">Identifiant du paiement à mettre à jour</param>
        /// <returns>
        /// <para><![CDATA[<error>]]></para>
        /// <para><![CDATA[  <value>Code Erreur (spécifique)</value>]]></para>
        /// <para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        /// <para><![CDATA[</error>]]></para>
        /// <para><![CDATA[<result>]]></para>
        /// <para><![CDATA[  <resultCode>true / false</resultCode>]]></para>
        /// <para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// PCIDSS. (chapitre 7 des specs)
        /// </remarks>
        public XElement UpdatePaymentMethod(string demandeId, string paymentId)
        {
            Logfile.WriteLog(LogLevelL4N.INFO, "Start method execution : UpdatePaymentMethod");
            try
            {

                Logfile.WriteLog(LogLevelL4N.INFO, "demandeId:" + demandeId);
                Logfile.WriteLog(LogLevelL4N.INFO, "paymentId:" + paymentId);

                Logfile.WriteLog(LogLevelL4N.INFO, "Start connecting CRM Org Service");
                crmService = new XrmServices();
                Logfile.WriteLog(LogLevelL4N.INFO, "CRM Org Service connected");

                Guid requestId;
                if (!string.IsNullOrEmpty(demandeId) && Guid.TryParse(demandeId, out requestId))
                {
                    Logfile.WriteLog(LogLevelL4N.INFO, "Start execution of method : GetRequestById");
                    Entity updateRequest = Demande.GetRequestById(requestId, crmService);
                    Logfile.WriteLog(LogLevelL4N.INFO, "Complete execution of method : GetRequestById");

                    if (updateRequest != null && updateRequest.Id != Guid.Empty)
                    {
                        updateRequest.Attributes["new_paymentid"] = paymentId;
                        crmService.Update(updateRequest);
                        result.AddResultElement("resultcode", true);
                        returnCode = (int)CustomReturnCode.RequestFound;

                        Logfile.WriteLog(LogLevelL4N.INFO, "Request Found");
                    }
                    else
                    {
                        Logfile.WriteLog(LogLevelL4N.INFO, "Request not Found");
                        returnCode = (int)CustomReturnCode.RequestNotFound;
                    }
                    result.SetError(returnCode, customError.getErrorMessage(returnCode));
                }
                else
                {
                    Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:UpdatePaymentMethod(). 'Paramétres incorrects'");
                    throw new Exception("Paramètres incorrects");
                }
            }
            catch (Exception e)
            {
                Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:UpdatePaymentMethod(). Ex:" + e.Message);
                result.SetError((int)CustomReturnCode.Unknown, e.Message, e);
            }
            Dispose();
            Logfile.WriteLog(LogLevelL4N.INFO, "Complete method execution : UpdatePaymentMethod");
            return result.getXml((Method == "post") ? new List<string> { demandeId, paymentId } : null);
        }

        /// <summary>
        ///  - INTERNE CRM JSI -
        /// Indique si le domaine du mail est valide
        /// </summary>
        /// <param name="email">email à vérifier</param>
        /// <returns>True ou False</returns>
        public bool MailValidation(string email)
        {
            Logfile.WriteLog(LogLevelL4N.INFO, "Start method execution : UpdatePaymentMethod");

            Logfile.WriteLog(LogLevelL4N.INFO, "email:" + email);

            return Mutuaide.Conciergerie.Library.Generics.IsValidEmailDomain(email);

            
        }

        #endregion

        #region Méthodes internes

        internal void CheckRequest(List<Entity> contactList, Entity activity, XrmServices crmServices, string queueName)
        {
            Entity newActivity = new Entity(queueName == "phonecall" ? "phonecall" : "email");
            string customerFieldName = (queueName == "phonecall" ? "contactid" : "customerid");
            OptionSetValue activityType = (activity.Contains("new_type") ? activity.GetAttributeValue<OptionSetValue>("new_type") : new OptionSetValue(100000001));
            EntityReference regarding = new EntityReference();

            if (contactList.Any() && contactList.Count == 1)
            {
                newActivity.Attributes.Add("regardingobjectid", null);
                //Récupération d'une demande ouverte.
                Entity customerRequest =
                     Demande.GetCustomerActiveRequest(
                         contactList[0].Id,
                         crmService);

                switch (queueName)
                {

                    case "phonecall":
                        newActivity.LogicalName = "phonecall";
                        newActivity.Attributes.Add("new_callid", activity.GetAttributeValue<string>("new_callid"));
                        newActivity.Attributes.Add("phonenumber", activity.GetAttributeValue<string>("phonenumber"));
                        newActivity.Attributes.Add("subject", activity.GetAttributeValue<string>("phonenumber"));

                        EntityCollection froms = new EntityCollection();
                        Entity activityParty = new Entity("activityparty");
                        activityParty.Attributes.Add("partyid", contactList[0].ToEntityReference());
                        froms.Entities.Add(activityParty);
                        newActivity.Attributes.Add("from", froms);
                        //Statut de l'activité a terminé et reçu
                        //newActivity.Attributes.Add("statecode", new OptionSetValue(1));
                        //newActivity.Attributes.Add("statuscode", new OptionSetValue(4));
                        //Type d'appel entrant
                        newActivity.Attributes.Add("directioncode", false);
                        break;

                    //pour l'instant tout sauf les phonecall(on verra après pour les SMS)
                    default:
                        newActivity.LogicalName = "email";
                        newActivity.Id = activity.Id;
                        newActivity.Attributes.Add("new_type", activityType);
                        newActivity.Attributes.Add("new_customeridentification", contactList[0].GetAttributeValue<string>("new_idcrm"));
                        break;
                }
                //Si une demande ouverte existe
                if (customerRequest != null && customerRequest.Id != Guid.Empty)
                {
                    if (queueName == "phonecall")
                    {
                        result.AddResultElement("conciergeid", customerRequest.GetAttributeValue<EntityReference>("ownerid").Id.ToString());
                    }
                    newActivity.Attributes["regardingobjectid"] = customerRequest.ToEntityReference();
                    result.AddResultElement("demandeid", customerRequest.Id.ToString());
                    result.AddResultElement(customerFieldName, contactList[0].Id.ToString());
                    returnCode = (int)CustomReturnCode.RequestFound;
                }
                else
                {
                    newActivity.Attributes["regardingobjectid"] = contactList[0].ToEntityReference();
                    //result.AddResultElement("activityid", email.Id.ToString());
                    result.AddResultElement(customerFieldName, contactList[0].Id.ToString());
                    returnCode = (int)CustomReturnCode.RequestNotFound;
                }

                if (newActivity.Id != Guid.Empty)
                {
                    crmService.Update(newActivity);
                }
                else
                {
                    newActivity.Id = crmService.GetService().Create(newActivity);
                    Activity.CloseActivity(newActivity.ToEntityReference(), crmServices, 1, 4);
                }
            }
            else
            {
                returnCode = (int)CustomReturnCode.CustomerNotFound;
            }
            //return returnCode;
        }


        #endregion

        #region IDisposable Membres

        public void Dispose()
        {
            if (crmService != null)
            {
                this.crmService.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
