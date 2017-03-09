using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Xrm.Sdk;
using MutuaideWCF.XRM;

namespace MutuaideWCF.DAL
{
    /// <exclude/>
    public partial class Contact
    {
        internal static List<Entity> GetContactOfAccountByPhone(string phoneNumber, Guid customerId,
           XrmServices crmServices)
        {
            //On récupére la liste des contacts du Distributeur correspondant au phoneNumber
            return (crmServices.GetServiceContext().CreateQuery("contact")
                .Join(crmServices.GetServiceContext().CreateQuery("new_phoneschedule"),
                    c => c.GetAttributeValue<Guid>("contactid"),
                    pc => pc.GetAttributeValue<EntityReference>("new_contactid").Id,
                    (c, pc) => new { c, pc })
                .Where(_ => _.pc.GetAttributeValue<string>("new_phonenumbere164") == phoneNumber && _.c.GetAttributeValue<EntityReference>("new_bankid").Id == customerId)
                .Select(_ => _.c)
                .ToList());


        }
    }
}