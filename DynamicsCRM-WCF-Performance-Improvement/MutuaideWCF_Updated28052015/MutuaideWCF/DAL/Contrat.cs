using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Sdk;
using MutuaideWCF.XRM;

namespace MutuaideWCF.DAL
{
    /// <exclude/>
    public class Contrat
    {
        internal static Entity GetContractNumberByBin(int bin, XrmServices crmServices)
        {
            return (crmServices.GetServiceContext().CreateQuery("new_contract")
                .Join(crmServices.GetServiceContext().CreateQuery("new_bin"),
                    c => c.GetAttributeValue<EntityReference>("new_contractid").Id,
                    b => b.GetAttributeValue<EntityReference>("new_contratid").Id,
                    (c, b) => new { c, b })
                .Where(
                    cb =>
                        cb.b.GetAttributeValue<int>("new_beginbin") <= bin &&
                        cb.b.GetAttributeValue<int>("new_endbin") >= bin))
                .Select(cb => new Entity("new_contract")
                {
                    Id = cb.c.GetAttributeValue<Guid>("new_contractid"),
                    Attributes = new AttributeCollection()
                    {
                        new KeyValuePair<string, object>("new_contractnumber",cb.c.GetAttributeValue<string>("new_contractnumber"))
                    }
                }).FirstOrDefault();
        }

        internal static Guid GetContractByPhone(string phoneNumber, XrmServices crmServices)
        {
            return crmServices.GetServiceContext().CreateQuery("new_contract")
                .Join(crmServices.GetServiceContext().CreateQuery("new_phonecontract"),
                    c => c.GetAttributeValue<Guid>("new_contractid"),
                    p => p.GetAttributeValue<EntityReference>("new_contratid").Id,
                    (c, p) => new {c, p})
                .Where(cp => cp.p.GetAttributeValue<string>("new_phonenumbere164").Equals(phoneNumber))
                .Select(cp => cp.c.GetAttributeValue<Guid>("new_contractid")).FirstOrDefault();
        }

    }
}