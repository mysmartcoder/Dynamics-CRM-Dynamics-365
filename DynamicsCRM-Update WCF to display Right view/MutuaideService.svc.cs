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
        public XElement GetEmailByExchangeId(string messageId)
        {
            try
            {


                if (string.IsNullOrEmpty(messageId))
                {
                    throw new Exception("L'id exchange ne peut pas être null.");
                }
                crmService = new XrmServices();
                Entity email = Activity.GetEmail(messageId, crmService);

                if (email != null && email.Id != Guid.Empty)
                {
                    var queue = Activity.GetActivityQueue(email.Id, crmService);
                    result.AddResultElement("activityid", email.Id.ToString());
                    if (queue == null)
                    {
                        throw new Exception("Le mail n'est pas attribué à une file d'attente.");
                    }
                    int status = email.GetAttributeValue<OptionSetValue>("new_status").Value;
                    //Si le mail est au statut <à traiter> ou <rejeté>.
                    if (status == 100000000 || status == 100000002)
                    {
                        //S'il est différent de crm-admin c'est qu'un concierge traite déjà ce mail.
                        if (email.GetAttributeValue<EntityReference>("ownerid").Id != crmService.callerId.Id)
                        {
                            returnCode = (int)CustomReturnCode.MailInProgress;
                        }
                        else
                        {
                            //Entity queue = Activity.GetActivityQueue(email.Id, crmService);
                            if (queue.GetAttributeValue<EntityReference>("new_contractid") != null && queue.GetAttributeValue<EntityReference>("new_contractid").Id != Guid.Empty)
                            {
                                Guid contractId = queue.GetAttributeValue<EntityReference>("new_contractid").Id;
                                switch (queue.GetAttributeValue<OptionSetValue>("new_type").Value)
                                {
                                    #region Email

                                    case 100000000:
                                        //var attachment = Activity.GetEmailAttachement(email.Id, crmService);


                                        /*var sender =
                                            email.GetAttributeValue<EntityCollection>("from")
                                                .Entities.FirstOrDefault(
                                                    e =>
                                                        e.GetAttributeValue<EntityReference>("partyid")
                                                            .LogicalName.ToLower() == "contact");*/

                                        EntityCollection froms = email.GetAttributeValue<EntityCollection>("from");

                                        // MK
                                        Entity sender = null;
                                        if ((froms.Entities.Count > 0) && froms.Entities[0].Contains("partyid") && (froms.Entities[0].GetAttributeValue<EntityReference>("partyid") != null) && (froms.Entities[0].GetAttributeValue<EntityReference>("partyid").LogicalName.ToLower() == "contact"))
                                        {
                                            sender = froms.Entities[0];
                                        }

                                        if (sender != null && sender.Id != Guid.Empty)
                                        {
                                            List<Entity> contactList =
                                                Contact.GetContactByEmailOnContract(
                                                    contractId,
                                                    sender.GetAttributeValue<string>("addressused"),
                                                    crmService);

                                            CheckRequest(contactList, email, crmService, "email");
                                        }

                                        else
                                        {
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
                                            string faxNumber = regResult.ToString();
                                            //Mail de type FAX
                                            email.Attributes.Add("new_type", new OptionSetValue(100000005));
                                            var customerList = Contact.GetContactByPhoneOnContract(contractId,
                                                faxNumber, crmService);
                                            CheckRequest(customerList, email, crmService, "fax");

                                        }
                                        else
                                        {
                                            returnCode = (int)CustomReturnCode.CustomerNotFound;
                                        }


                                        break;

                                    #endregion

                                    default:
                                        throw new Exception("L'email ne répond pas aux critères de traitement.");
                                        break;
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
                                    //Charge le xml du mail pour créer la demande.
                                    var xml =
                                        Activity.GetEmailAttachement(
                                            email.GetAttributeValue<string>("description"), queue.GetAttributeValue<string>("name").ToLower()).Root.Elements().FirstOrDefault();
                                    XNamespace RootNamespace = xml.GetDefaultNamespace();
                                    XNamespace requestNs =
                                        xml.Element(RootNamespace + "request").GetNamespaceOfPrefix("a");
                                    var request = xml.Element(RootNamespace + "request");
                                    Entity customer =
                                        Contact.GetContactById(xml.Element(RootNamespace + "userid").Value,
                                            crmService);

                                    updateEmail.Attributes.Add("new_customeridentification", xml.Element(RootNamespace + "userid").Value);
                                    updateEmail.Attributes.Add("new_type", new OptionSetValue(100000003));
                                    if (customer.Id != Guid.Empty)
                                    {
                                        Entity newIncident = new Entity("incident")
                                        {
                                            Attributes = new AttributeCollection()
                                                            {
                                                                new KeyValuePair<string, object>("customerid", customer.ToEntityReference()),
                                                                new KeyValuePair<string, object>("new_callerphonenumber",
                                                                    request.Element(requestNs + "phone").Value)
                                                                //new KeyValuePair<string, object>("customerid", customer.Id)
                                                            }
                                        };
                                        try
                                        {
                                            newIncident.Id = crmService.GetService().Create(newIncident);
                                            result.AddResultElement("demandeid", newIncident.Id);
                                            if (newIncident.Id != Guid.Empty)
                                            {
                                                //email.Attributes.Remove("regardingobjectid");
                                                updateEmail.Attributes.Add("regardingobjectid", newIncident.ToEntityReference());
                                                string commentaire =
                                                    request.Element(requestNs + "text").Value;
                                                string typeCode =
                                                    request.Element(requestNs + "typecode").Value;
                                                string customerEmail =
                                                    request.Element(requestNs + "email").Value;
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
                                                #endregion

                                                if (crmService.GetService().Create(newPrestation) !=
                                                    Guid.Empty)
                                                {
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
                                        }
                                        crmService.Update(updateEmail);
                                    }
                                    else
                                    {
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
                                        var number = Activity.GetEmailAttachement(email.GetAttributeValue<string>("description"), queue.GetAttributeValue<string>("name").ToLower()).Root;

                                        Guid contractId = Contrat.GetContractByPhone("+" + number.Element("font").Value, crmService);
                                        if (contractId != Guid.Empty)
                                        {
                                            List<Entity> contact = Contact.GetContactByPhoneOnContract(contractId,
                                                "+" + number.Elements("font").ToList()[1].Value, crmService);

                                            CheckRequest(contact, updateEmail, crmService, queue.GetAttributeValue<string>("name"));
                                        }
                                        else
                                        {
                                            returnCode = (int)CustomReturnCode.ContractNotFound;
                                        }

                                        //
                                    }
                                    else
                                    {
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
                            returnCode = (int)CustomReturnCode.MailClosed;
                        }
                    }


                }
                else
                {
                    returnCode = (int)CustomReturnCode.MailNotFound;
                    //result.SetError((int)CustomReturnCode.MailNotFound, customError.getErrorMessage((int)CustomReturnCode.MailNotFound));
                }
                result.SetError(returnCode, customError.getErrorMessage(returnCode));
            }
            catch (Exception e)
            {
                result.SetError((int)CustomReturnCode.Unknown, e.Message, e);
            }
            Dispose();
            return result.getXml((Method == "post") ? new List<string> { messageId } : null);
        }

        public XElement UpdateEmailByExchangeId(string messageId, string domainName = "", string userName = "")
        {
            //return new XElement("root");
            //return new XElement("");
            //string resultMessage = string.Empty;
            bool ownerChanged = false;
            try
            {
                crmService = new XrmServices();
                string user = domainName + "\\" + userName;
                EntityReference email = Activity.GetEmailById(messageId, crmService);

                if (email != null && email.Id != Guid.Empty)
                {
                    //Activity.ReopenActivity(email, crmService);
                    XrmUtility.ChangeOwner(email, SystemUser.GetUserFromLogin(user, crmService), crmService);
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
                    returnCode = (int)CustomReturnCode.MailNotFound;
                }

                result.SetError(returnCode, customError.getErrorMessage(returnCode));
            }
            catch (Exception ex)
            {
                result.SetError((int)CustomReturnCode.Unknown, ex.Message, ex);
            }
            result.AddResultElement("value", ownerChanged);
            return result.getXml((Method == "post") ? new List<string> { messageId, domainName, userName } : null);
            //return resultMessage;
        }

        public XElement GetRequestByPhoneNumber(string phoneNumber, string dialNumber, string phoneCallId)
        {
            try
            {
                crmService = new XrmServices();
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
                    Guid contractId = Contrat.GetContractByPhone(dialNumber, crmService);
                    if (contractId != null && contractId != Guid.Empty)
                    {
                        //Récupération des contacts du Distributeur du phoneNumber.
                        var contact = Contact.GetContactByPhoneOnContract(contractId, phoneNumber, crmService);
                        CheckRequest(contact, phoneCall, crmService, "phonecall");
                    }
                    else
                    {
                        returnCode = (int)CustomReturnCode.ContractNotFound;
                    }
                }
                else
                {
                    throw new Exception("Paramétres incorrects");
                }
                Dispose();
                result.SetError(returnCode, customError.getErrorMessage(returnCode));
            }
            catch (Exception e)
            {
                result.SetError((int)CustomReturnCode.Unknown, e.Message, e);
            }
            Dispose();
            return result.getXml((Method == "post") ? new List<string> { phoneNumber, dialNumber, phoneCallId } : null);
        }

        public XElement WelcomeCall(string pcidssId, string callerId, string lastCall, string tel2)
        {
            try
            {
                crmService = new XrmServices();
                if (!string.IsNullOrEmpty(pcidssId))
                {
                    Entity contact = Contact.GetContactById(pcidssId, crmService);

                    if (contact.Id != Guid.Empty)
                    {
                        crmService.GetService().Create(new Entity("phonecall")
                        {
                            Attributes = new AttributeCollection()
                            {
                                new KeyValuePair<string, object>("regardingobjectid",contact.ToEntityReference()),
                                new KeyValuePair<string, object>("new_callid",callerId),
                                new KeyValuePair<string, object>("new_iswelcomecall",true),
                                new KeyValuePair<string, object>("scheduledstart",DateTime.Now),
                                new KeyValuePair<string, object>("new_secondphonenumber",bool.Parse(tel2)),
                                new KeyValuePair<string, object>("new_lastcall",bool.Parse(lastCall)),
                            }
                        });
                        result.AddResultElement("url", XrmUtility.FormatUrl(contact.Id, contact.LogicalName.ToLower()));
                        result.AddResultElement("status", contact.GetAttributeValue<OptionSetValue>("statuscode").Value);

                        returnCode = (int)CustomReturnCode.Success;
                    }
                    else
                    {
                        returnCode = (int)CustomReturnCode.CustomerNotFound;
                    }
                }
                else
                {
                    returnCode = (int)CustomReturnCode.CustomerNotFound;
                }
                result.SetError(returnCode, customError.getErrorMessage(returnCode));
            }
            catch (Exception ex)
            {
                result.SetError((int)CustomReturnCode.Unknown, ex.Message, ex);
            }
            Dispose();

            return result.getXml((Method == "post") ? new List<string> { pcidssId, callerId } : null);
        }

        public XElement GetContractByBin(string binCode)
        {
            try
            {
                crmService = new XrmServices();
                Entity contract = Contrat.GetContractNumberByBin(int.Parse(binCode), crmService);
                if (contract != null && contract.Id != Guid.Empty)
                {
                    string contractNumber = contract.GetAttributeValue<string>("new_contractnumber");

                    result.AddResultElement("contractcode", contractNumber);
                    returnCode = (int)CustomReturnCode.Success;
                }
                else
                {
                    returnCode = (int)CustomReturnCode.ContractNotFound;
                }
                result.SetError(returnCode, customError.getErrorMessage(returnCode));
            }
            catch (Exception e)
            {
                result.SetError((int)CustomReturnCode.Unknown, e.Message, e);
            }
            Dispose();
            return result.getXml((Method == "post") ? new List<string> { binCode } : null);
        }

        public XElement UpdateCustomerBin(string pcidssId, string binCode)
        {
            bool isUpdated = false;

            try
            {
                crmService = new XrmServices();
                int codeBin;
                if (!string.IsNullOrEmpty(pcidssId) && !string.IsNullOrEmpty(binCode) && int.TryParse(binCode.Replace(" ", ""), out codeBin))
                {
                    Entity contractId = Contrat.GetContractNumberByBin(codeBin, crmService);
                    if (contractId != null && contractId.Id != Guid.Empty)
                    {
                        Entity updateContact = Contact.GetContactById(pcidssId, crmService);
                        if (updateContact != null && updateContact.Id != Guid.Empty)
                        {
                            updateContact.Attributes.Add("new_bin", codeBin);
                            updateContact.Attributes.Add("new_contractid", contractId.ToEntityReference());
                            crmService.Update(updateContact);
                            isUpdated = true;
                            result.SetError((int)CustomReturnCode.Success, customError.getErrorMessage((int)CustomReturnCode.Success));
                        }
                        else
                        {
                            result.SetError((int)CustomReturnCode.CustomerNotFound, customError.getErrorMessage((int)CustomReturnCode.CustomerNotFound));
                        }
                    }
                    else
                    {
                        result.SetError((int)CustomReturnCode.ContractNotFound, customError.getErrorMessage((int)CustomReturnCode.ContractNotFound));
                    }
                }
                else
                {
                    throw new Exception("Paramétre incorrect");
                }
            }
            catch (Exception e)
            {
                result.SetError((int)CustomReturnCode.Unknown, e.Message, e);
            }
            Dispose();
            result.AddResultElement("resultcode", isUpdated);
            return result.getXml((Method == "post") ? new List<string> { pcidssId, binCode } : null);

        }

        public XElement UpdatePaymentMethod(string demandeId, string paymentId)
        {
            try
            {
                crmService = new XrmServices();
                Guid requestId;
                if (!string.IsNullOrEmpty(demandeId) && Guid.TryParse(demandeId, out requestId))
                {
                    Entity updateRequest = Demande.GetRequestById(requestId, crmService);
                    if (updateRequest != null && updateRequest.Id != Guid.Empty)
                    {
                        updateRequest.Attributes["new_paymentid"] = paymentId;
                        crmService.Update(updateRequest);
                        result.AddResultElement("resultcode", true);
                        returnCode = (int)CustomReturnCode.RequestFound;
                    }
                    else
                    {
                        returnCode = (int)CustomReturnCode.RequestNotFound;
                    }
                    result.SetError(returnCode, customError.getErrorMessage(returnCode));
                }
                else
                {
                    throw new Exception("Paramètres incorrects");
                }
            }
            catch (Exception e)
            {
                result.SetError((int)CustomReturnCode.Unknown, e.Message, e);
            }
            Dispose();
            return result.getXml((Method == "post") ? new List<string> { demandeId, paymentId } : null);
        }

        public bool MailValidation(string email)
        {
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


            var results = (from cl in contactList
                           group cl by cl.Id into g
                           select new { Id = g.Key, Counts = g.Select(x => x.Attributes).Count() }
                            ).ToList();


            if ((contactList.Any() && contactList.Count == 1) ||
                contactList.Any() && results.Count == 1)
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
                if (results.Count > 1)
                {
                    //TODO - change 
                   
                    // Here case 3 -> Many entities with the same contact number
                    returnCode = (int)CustomReturnCode.MultipleContactsFounded; // Needs to be updated
                }
                else
                {
                    returnCode = (int)CustomReturnCode.CustomerNotFound;
                }
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
