using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using MutuaideWCF.XRM;

namespace MutuaideWCF.DAL
{
    /// <exclude/>
    internal class Activity
    {
        /// <summary>
        /// Retourne l'identifiant unique du mail à partir de l'id Exchange unique en paramétre
        /// </summary>
        /// <param name="messageId">Id exchange</param>
        /// <param name="crmServices">CRM Services</param>
        /// <returns></returns>
        public static EntityReference GetEmailById(string messageId, XrmServices crmServices)
        {
            return
                crmServices.GetServiceContext().CreateQuery("email")
                .Where(e => e.GetAttributeValue<string>("messageid").Contains(messageId))
                .Select(e => new EntityReference
                {
                    Id = e.GetAttributeValue<Guid>("activityid"),
                    LogicalName = e.LogicalName,
                    Name = e.GetAttributeValue<string>("subject")
                })
                .FirstOrDefault();


        }

        /// <summary>
        /// Renvoi l'enregistrement Email de CRM
        /// </summary>
        /// <param name="messageId">identifiant Exchange</param>
        /// <param name="crmServices">CRM Services</param>
        /// <returns></returns>
        public static Entity GetEmail(string messageId, XrmServices crmServices)
        {
            return (
                crmServices.GetServiceContext()
                    .CreateQuery("email")
                    .Where(e => e.GetAttributeValue<string>("messageid") == messageId)
                    .Select(e => new Entity
                    {
                        Id = e.GetAttributeValue<Guid>("activityid"),
                        LogicalName = e.LogicalName,
                        Attributes = new AttributeCollection
                        {
                            new KeyValuePair<string, object>("subject",e.GetAttributeValue<string>("subject")),
                            new KeyValuePair<string, object>("statecode",e.GetAttributeValue<OptionSetValue>("statecode") ?? new OptionSetValue()),
                            new KeyValuePair<string, object>("new_status",e.GetAttributeValue<OptionSetValue>("new_status") ?? new OptionSetValue()),
                            new KeyValuePair<string, object>("ownerid",e.GetAttributeValue<EntityReference>("ownerid") ?? new EntityReference()),
                            new KeyValuePair<string, object>("description",e.GetAttributeValue<string>("description") ?? string.Empty),
                            new KeyValuePair<string, object>("from", e.GetAttributeValue<EntityCollection>("from") ?? new EntityCollection())
                        }
                    })
                ).FirstOrDefault();
            /*(crmServices.GetServiceContext()
                    .CreateQuery("email")
                    .Join(crmServices.GetServiceContext().CreateQuery("incident"),
                        e => e.GetAttributeValue<EntityReference>("regardingobjectid").Id,
                        d => d.GetAttributeValue<Guid>("incidentid"), (e, d) => new { e, d })
                    .Where(@t => @t.e.GetAttributeValue<string>("messageid").Contains(messageId))
                    .Select(@t => new Entity
                    {
                        Id = @t.e.GetAttributeValue<Guid>("activityid"),
                        LogicalName = @t.e.LogicalName,
                        Attributes = new AttributeCollection
                        {
                            new KeyValuePair<string, object>("subject", @t.e.GetAttributeValue<string>("subject")),
                            new KeyValuePair<string, object>("demandeid",@t.d.GetAttributeValue<Guid>("incidentid")),
                            new KeyValuePair<string, object>("customerid",@t.d.GetAttributeValue<EntityReference>("customerid") ?? new EntityReference()),
                            new KeyValuePair<string, object>("statecode",@t.e.GetAttributeValue<OptionSetValue>("statecode") ?? new OptionSetValue()),
                            new KeyValuePair<string, object>("ownerid",@t.e.GetAttributeValue<EntityReference>("ownerid") ?? new EntityReference())

                        }
                    })).FirstOrDefault();*/


        }

        /// <summary>
        /// Réouvre une activité fermé.
        /// </summary>
        /// <param name="activity">Activité à ouvrir</param>
        /// <param name="crmServices"></param>
        public static void ReopenActivity(EntityReference activity, XrmServices crmServices)
        {
            SetStateRequest ssr = new SetStateRequest();
            ssr.EntityMoniker = activity;
            ssr.State = new OptionSetValue(0);
            ssr.Status = new OptionSetValue(1);
            SetStateResponse resp1 = (SetStateResponse)crmServices.GetService().Execute(ssr);
        }

        /// <summary>
        /// Ferme une activité avec le statut Terminé par defaut.
        /// </summary>
        /// <param name="activity">Activité à fermer</param>
        /// <param name="crmServices">Service CRM</param>
        /// <param name="statecode">Statut de l'activité (1 si valeur null)</param>
        /// <param name="statuscode">Raison du statut (2 si valeur null)</param>
        public static void CloseActivity(EntityReference activity, XrmServices crmServices, int statecode = 1, int statuscode = 2)
        {
            SetStateRequest ssr = new SetStateRequest();
            ssr.EntityMoniker = activity;
            ssr.State = new OptionSetValue(statecode);
            ssr.Status = new OptionSetValue(statuscode);
            SetStateResponse resp1 = (SetStateResponse)crmServices.GetService().Execute(ssr);
        }

        public static Entity GetActivityQueue(Guid activityId, XrmServices crmServices)
        {
            return (crmServices.GetServiceContext().CreateQuery("queueitem")
                    .Join(crmServices.GetServiceContext().CreateQuery("queue"),
                    qi => qi.GetAttributeValue<EntityReference>("queueid").Id,
                    q => q.GetAttributeValue<Guid>("queueid"),
                    (qi, q) => new { qi, q })
                    .Where(join => join.qi.GetAttributeValue<EntityReference>("objectid").Id == activityId)
                    .Select(join => new Entity
                    {
                        Id = join.q.GetAttributeValue<Guid>("queueid"),
                        LogicalName = join.q.LogicalName,
                        Attributes = new AttributeCollection
                        {
                            new KeyValuePair<string, object>("new_type", join.q.GetAttributeValue<OptionSetValue>("new_type")??new OptionSetValue()),
                            new KeyValuePair<string, object>("new_contractid", join.q.GetAttributeValue<EntityReference>("new_contractid")??new EntityReference()),
                            new KeyValuePair<string, object>("name", join.q.GetAttributeValue<string>("name")??string.Empty),
                        }
                    })
                .FirstOrDefault());
        }

        public static XDocument GetEmailAttachement(string description, string queueName)
        {
            XDocument doc = new XDocument();
            description = HttpUtility.HtmlDecode(description.ToLower());
            //Regex rRemScript = new Regex((queueName == "<smartphone>" ? @"<s:Body[^\>]*>(.*)</s:Body>" : @"<font[^\>]*>(\d{0,15}?)<\/font>"));
            Regex rRemScript = new Regex((queueName.Contains("smartphone") ? @"<s:body[^\>]*>(.*)</s:body>" : @"<font[^\>]*>(\d{0,15}?)<\/font>"));
            MatchCollection match = rRemScript.Matches(description.Replace("\r\n", ""));
            //Console.WriteLine(match[0].Groups[1].Value);




            if (queueName.Contains("smartphone"))
            {
                try
                {
                    doc = XDocument.Parse(match[0].Value.Replace("s:body", "body"));
                }
                catch (Exception ex)
                {
                    throw new Exception("Le format xml du mail n'est pas correcte.", ex);
                }
            }
            else
            {
                try
                {
                    doc = XDocument.Parse("<number>" + match[0].Value.ToLower() + match[1].Value.ToLower() + "</number>");
                }
                catch (Exception ex)
                {
                    throw new Exception("Le template HTML présent dans le mail n'est pas valide.");

                }

            }
            return doc;
        }

        // .FirstOrDefault(a => a.GetAttributeValue<EntityReference>("objectid").Id == activityId);
        //var x = doc.Element("postConciergeServiceRequest");

    }
}