using System.Configuration;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using Microsoft.Xrm.Client.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using MutuaideWCF.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using MutuaideWCF.Log;
using MutuaideWCF.XRM;

namespace MutuaideWCF
{
    // http://localhost:53996/MutuaideService.svc/SearchServices/17BD3174-8D5E-E411-80CC-00155D00808B/91E3462F-154A-E411-80C4-00155D00808B
    public partial class MutuaideService : IMutuaideService
    {

        public jsonReturn SearchServices(string clientPorteurId, string subtypeId)
        {
            Logfile.WriteLog(LogLevelL4N.INFO, "Start method execution : SearchServices");

            jsonReturn jsonResult = new jsonReturn();
            try
            {

                Logfile.WriteLog(LogLevelL4N.INFO, "clientPorteurId:" + clientPorteurId);
                Logfile.WriteLog(LogLevelL4N.INFO, "subtypeId:" + subtypeId);

                Logfile.WriteLog(LogLevelL4N.INFO, "Start connecting CRM Org Service");
                crmService = new XrmServices();
                Logfile.WriteLog(LogLevelL4N.INFO, "CRM Org Service connected");

                Guid clientPorteurId_, subtypeId_;

                // Check if the clientId is valid
                if (!Guid.TryParse(clientPorteurId, out clientPorteurId_))
                    return new jsonReturn()
                    {
                        errorCode = CustomReturnCode.CustomerNotFound,
                        errorLabel = customError.getErrorMessage((int)CustomReturnCode.CustomerNotFound)
                    };

                // Check if the subtypeId is valid
                if (!Guid.TryParse(subtypeId, out subtypeId_))
                    return new jsonReturn()
                    {
                        errorCode = CustomReturnCode.SubTypeNotFound,
                        errorLabel = customError.getErrorMessage((int)CustomReturnCode.SubTypeNotFound)
                    };

                Dictionary<Guid, int> dicoServices = new Dictionary<Guid, int>();
                List<ServiceEntity> services = new List<ServiceEntity>();
                int priority = 0;

                #region Services de tous les prestataires imposés/exclus du distributeur associé au client
                Logfile.WriteLog(LogLevelL4N.INFO, "Services de tous les prestataires imposés/exclus du distributeur associé au client");

                // Recherche du distributeur du client porteur
                Entity porteur = crmService.GetService().Retrieve("contact", clientPorteurId_, new ColumnSet(new string[] { "new_bankid" }));

                // S'il y a un distributeur, on continue
                Guid serviceId = Guid.Empty;
                if (porteur.Contains("new_bankid"))
                {                    
                    Guid distributeurId = porteur.GetAttributeValue<EntityReference>("new_bankid").Id;
                    Logfile.WriteLog(LogLevelL4N.INFO, "Bankid found:" + distributeurId.ToString());

                    // On récupère la liste des services des prestataires favoris/exclus du distributeur
                    QueryExpression queryCdoServices = new QueryExpression()
                    {
                        EntityName = "new_favoriteexcluded",
                        ColumnSet = new ColumnSet(new string[] { "new_providerid", "new_choicelist" }),
                        Criteria = new FilterExpression()
                        {
                            FilterOperator = LogicalOperator.And,
                            Conditions =
                            {
                                new ConditionExpression("new_distributorid", ConditionOperator.Equal, distributeurId)
                            }
                        },
                        LinkEntities =
                        {
                            new LinkEntity()
                            {
                                EntityAlias = "prestataire",
                                Columns = new ColumnSet(new string[] {"accountid", "name", "new_typology"}),
                                LinkFromEntityName = "new_favoriteexcluded",
                                LinkToEntityName = "account",
                                LinkFromAttributeName = "new_providerid",
                                LinkToAttributeName = "accountid",
                                LinkEntities =
                                {
                                    new LinkEntity()
                                    {
                                        EntityAlias = "service",
                                        Columns =
                                            new ColumnSet(new string[] {"new_serviceid", "new_service", "new_keywords"}),
                                        LinkFromEntityName = "prestataire",
                                        LinkToEntityName = "new_service",
                                        LinkFromAttributeName = "accountid",
                                        LinkToAttributeName = "new_accountid",
                                        LinkCriteria = new FilterExpression()
                                        {
                                            FilterOperator = LogicalOperator.And,
                                            Conditions =
                                            {
                                                new ConditionExpression("new_subtypeid", ConditionOperator.Equal,
                                                    subtypeId_)
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    };
                    EntityCollection colCdoServices = crmService.GetServiceContext().RetrieveMultiple(queryCdoServices);

                    Logfile.WriteLog(LogLevelL4N.INFO, "new_favoriteexcluded count:" + colCdoServices.Entities.Count.ToString());

                    foreach (Entity service in colCdoServices.Entities)
                    {
                        serviceId = (Guid)service.GetAttributeValue<AliasedValue>("service.new_serviceid").Value;

                        if (!dicoServices.ContainsKey(serviceId))
                        {
                            dicoServices.Add(serviceId, priority);

                            // CDO Exclus ou null
                            priority = 3;
                            if (service.Contains("new_choicelist"))
                            {
                                // CDO Imposé
                                if (service.GetAttributeValue<OptionSetValue>("new_choicelist").Value == 100000002)
                                    priority = 1;

                                    // CDO Favoris
                                else if (service.GetAttributeValue<OptionSetValue>("new_choicelist").Value == 100000001)
                                    priority = 2;
                            }

                            services.Add(new ServiceEntity()
                            {
                                new_serviceid = serviceId,
                                new_service =service.GetAttributeValue<AliasedValue>("service.new_service").Value.ToString(),
                                new_accountid =service.Contains("prestataire.accountid")? Guid.Parse(service.GetAttributeValue<AliasedValue>("prestataire.accountid").Value.ToString()): Guid.Empty,
                                new_accountname =service.Contains("prestataire.name")? service.GetAttributeValue<AliasedValue>("prestataire.name").Value.ToString(): null,
                                new_keywords =service.Contains("service.new_keywords")? service.GetAttributeValue<AliasedValue>("service.new_keywords").Value.ToString(): null,
                                priority = priority
                            });
                        }
                    }

                    Logfile.WriteLog(LogLevelL4N.INFO, "Complete processing Service records");
                }

                #endregion


                #region Services de tous les prestataires imposés/exclus du client

                Logfile.WriteLog(LogLevelL4N.INFO, "Services de tous les prestataires imposés/exclus du client");
                // On récupère la liste des services des prestataires favoris/exclus du client
                QueryExpression queryClientServices = new QueryExpression()
                {
                    EntityName = "new_favoriteexcluded",
                    ColumnSet = new ColumnSet(new string[] { "new_providerid", "new_choicelist" }),
                    Criteria = new FilterExpression()
                    {
                        FilterOperator = LogicalOperator.And,
                        Conditions =
                        {
                            new ConditionExpression("new_porteurid", ConditionOperator.Equal, clientPorteurId_)
                        }
                    },
                    LinkEntities =
                    {
                        new LinkEntity()
                        {
                            EntityAlias = "prestataire",
                            Columns = new ColumnSet(new string[] {"accountid", "name", "new_typology"}),
                            LinkFromEntityName = "new_favoriteexcluded",
                            LinkToEntityName = "account",
                            LinkFromAttributeName = "new_providerid",
                            LinkToAttributeName = "accountid",
                            LinkEntities =
                            {
                                new LinkEntity()
                                {
                                    EntityAlias = "service",
                                    Columns =
                                        new ColumnSet(new string[] {"new_serviceid", "new_service", "new_keywords"}),
                                    LinkFromEntityName = "prestataire",
                                    LinkToEntityName = "new_service",
                                    LinkFromAttributeName = "accountid",
                                    LinkToAttributeName = "new_accountid",
                                    LinkCriteria = new FilterExpression()
                                    {
                                        FilterOperator = LogicalOperator.And,
                                        Conditions =
                                        {
                                            new ConditionExpression("new_subtypeid", ConditionOperator.Equal, subtypeId_)
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
                EntityCollection colClientServices = crmService.GetServiceContext().RetrieveMultiple(queryClientServices);

                Logfile.WriteLog(LogLevelL4N.INFO, "Client Service count:" + colClientServices.Entities.Count.ToString());

                foreach (Entity service in colClientServices.Entities)
                {
                    serviceId = (Guid)service.GetAttributeValue<AliasedValue>("service.new_serviceid").Value;

                    if (!dicoServices.ContainsKey(serviceId))
                    {
                        dicoServices.Add(serviceId, priority);

                        // Client Exclus ou null
                        priority = 9;
                        if (service.Contains("new_choicelist"))
                        {
                            // Client Imposé
                            if (service.GetAttributeValue<OptionSetValue>("new_choicelist").Value == 100000002)
                                priority = 7;

                                // Client Favoris
                            else if (service.GetAttributeValue<OptionSetValue>("new_choicelist").Value == 100000001)
                                priority = 8;
                        }

                        services.Add(new ServiceEntity()
                        {
                            new_serviceid = serviceId,
                            new_service =
                                service.GetAttributeValue<AliasedValue>("service.new_service").Value.ToString(),
                            new_accountid =
                                service.Contains("prestataire.accountid")
                                    ? Guid.Parse(
                                        service.GetAttributeValue<AliasedValue>("prestataire.accountid")
                                            .Value.ToString())
                                    : Guid.Empty,
                            new_accountname =
                                service.Contains("prestataire.name")
                                    ? service.GetAttributeValue<AliasedValue>("prestataire.name").Value.ToString()
                                    : null,
                            new_keywords =
                                service.Contains("service.new_keywords")
                                    ? service.GetAttributeValue<AliasedValue>("service.new_keywords").Value.ToString()
                                    : null,
                            priority = priority
                        });
                    }
                }

                Logfile.WriteLog(LogLevelL4N.INFO, "Complete processing Client Service records");
                #endregion


                #region Services de tous les autres prestataires
                Logfile.WriteLog(LogLevelL4N.INFO, "Services de tous les autres prestataires");
                // On récupère la liste des services des prestataires favoris/exclus du client
                QueryExpression queryPrestataireServices = new QueryExpression()
                {
                    EntityName = "account",
                    Criteria = new FilterExpression()
                    {
                        FilterOperator = LogicalOperator.And,
                        Conditions =
                        {
                            new ConditionExpression("new_accounttype", ConditionOperator.Equal, 100000000)
                            // Prestataire
                        }
                    },
                    ColumnSet = new ColumnSet(new string[] { "accountid", "name", "new_typology" }),
                    LinkEntities =
                    {
                        new LinkEntity()
                        {
                            EntityAlias = "service",
                            Columns = new ColumnSet(new string[] {"new_serviceid", "new_service", "new_keywords"}),
                            LinkFromEntityName = "prestataire",
                            LinkToEntityName = "new_service",
                            LinkFromAttributeName = "accountid",
                            LinkToAttributeName = "new_accountid",
                            LinkCriteria = new FilterExpression()
                            {
                                FilterOperator = LogicalOperator.And,
                                Conditions =
                                {
                                    new ConditionExpression("new_subtypeid", ConditionOperator.Equal, subtypeId_)
                                }
                            }
                        }
                    }
                };
                EntityCollection colPrestataireServices =crmService.GetServiceContext().RetrieveMultiple(queryPrestataireServices);

                Logfile.WriteLog(LogLevelL4N.INFO, "Client Prestataire count:" + colClientServices.Entities.Count.ToString());

                foreach (Entity service in colPrestataireServices.Entities)
                {
                    serviceId = (Guid)service.GetAttributeValue<AliasedValue>("service.new_serviceid").Value;

                    if (!dicoServices.ContainsKey(serviceId))
                    {
                        dicoServices.Add(serviceId, priority);

                        // Prestataire vide ou null
                        priority = 6;
                        if (service.Contains("new_typology"))
                        {
                            // Prestataire contractualisé
                            if (service.GetAttributeValue<OptionSetValue>("new_typology").Value == 100000000)
                                priority = 4;

                                // Prestataire ponctuel
                            else if (service.GetAttributeValue<OptionSetValue>("new_typology").Value == 100000001)
                                priority = 5;
                        }

                        services.Add(new ServiceEntity()
                        {
                            new_serviceid = serviceId,
                            new_service =
                                service.GetAttributeValue<AliasedValue>("service.new_service").Value.ToString(),
                            new_accountid = service.Contains("accountid") ? service.Id : Guid.Empty,
                            new_accountname =
                                service.Contains("accountid") ? service.GetAttributeValue<string>("name") : null,
                            new_keywords =
                                service.Contains("service.new_keywords")
                                    ? service.GetAttributeValue<AliasedValue>("service.new_keywords").Value.ToString()
                                    : null,
                            priority = priority
                        });
                    }
                }

                Logfile.WriteLog(LogLevelL4N.INFO, "Complete processing Prestataire Service records");
                #endregion


                #region Nettoyage

                // On supprime les services dont la priorité est 3 et 9
                services.RemoveAll(s => (s.priority == 3) || (s.priority == 9));
                services.Sort(delegate(ServiceEntity s1, ServiceEntity s2)
                {
                    if (s1.priority == s2.priority) return 0;
                    else if (s1.priority < s2.priority) return -1;
                    else return 1;
                });

                #endregion

                //for (int i = 0; i < 10; i++)
                //{
                //    services.AddRange(services);
                //}

                Dispose();
                Logfile.WriteLog(LogLevelL4N.INFO, "Complete method execution : SearchServices");
                // Success
                jsonResult.errorCode = 0;
                jsonResult.errorLabel = "Success";
                jsonResult.services = services;
                if (ConfigurationManager.AppSettings["LogAll"].ToLower().Equals("true"))
                {
                    FileLogHelper.WriteLine(null, new LogRequestHelper(jsonResult, new List<string> { clientPorteurId, subtypeId }));
                }
            }
            catch (Exception e)
            {
                Logfile.WriteLog(LogLevelL4N.ERROR, "Error occured. Method:SearchServices(). Ex:" + e.Message);
                // new Log.LogRequestHelper(null,)
                Dispose();
                jsonResult.errorCode = CustomReturnCode.Unknown;
                jsonResult.errorLabel = e.Message;
                FileLogHelper.WriteLine(e, new LogRequestHelper(jsonResult, new List<string> { clientPorteurId, subtypeId }));
            }
            return jsonResult;

        }
    }



    /// <summary>
    /// Service helper class
    /// </summary>
    public class ServiceEntity
    {
        public Guid new_serviceid { get; set; }
        public string new_service { get; set; }
        public Guid new_accountid { get; set; }
        public string new_accountname { get; set; }
        public string new_keywords { get; set; }
        public int priority { get; set; }
    }



    /// <summary>
    /// Service helper class
    /// </summary>
    public class jsonReturn
    {
        public CustomReturnCode errorCode { get; set; } // 0 => success
        public string errorLabel { get; set; }
        public List<ServiceEntity> services { get; set; }
    }
}