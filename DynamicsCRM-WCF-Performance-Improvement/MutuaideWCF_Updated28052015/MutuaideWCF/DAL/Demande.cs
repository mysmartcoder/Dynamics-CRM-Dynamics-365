using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Xrm.Sdk;
using MutuaideWCF.XRM;

namespace MutuaideWCF.DAL
{
    /// <exclude/>
    public class Demande
    {

        /// <summary>
        /// Retourne un objet demande à partir du numéro de téléphone
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="dialNumber"></param>
        /// <param name="crmService"></param>
        /// <returns></returns>
        public Entity GetRequestByPhoneNumber(string phoneNumber, string dialNumber, XrmServices crmService)
        {
            //return (crmService.GetServiceContext().CreateQuery("incident")
            //        .Join(crmService.GetServiceContext().CreateQuery("contact"),
            //        d=>d.GetAttributeValue<>())
            return new Entity();

        }

        internal static Entity GetRequestById(Guid demandeId, XrmServices crmServices)
        {
            return crmServices.GetServiceContext().CreateQuery("incident")
                .Where(i => i.GetAttributeValue<Guid>("incidentid") == demandeId)
                .Select(i => new Entity("incident")
                {
                    Id = i.GetAttributeValue<Guid>("incidentid"),
                    LogicalName = i.LogicalName,
                    Attributes = new AttributeCollection()
                    {
                        new KeyValuePair<string, object>("new_paymentid", i.GetAttributeValue<string>("new_paymentid"))
                    }
                }).FirstOrDefault();
        }


        public static Entity GetCustomerActiveRequest(Guid customerId, XrmServices crmService)
        {

            return (crmService.GetServiceContext().CreateQuery("incident")
                .Where(i => i.GetAttributeValue<EntityReference>("customerid").Id == customerId
                    && i.GetAttributeValue<OptionSetValue>("statecode").Value == 0
                )
                .OrderByDescending(i => i.GetAttributeValue<DateTime>("createdon"))
                .Select(i => new Entity("incident")
                {
                    Id = i.GetAttributeValue<Guid>("incidentid"),
                    Attributes = new AttributeCollection()
                    {
                        new KeyValuePair<string, object>("ownerid",i.GetAttributeValue<EntityReference>("ownerid"))
                    }
                }))
                .FirstOrDefault();
        }
    }
}