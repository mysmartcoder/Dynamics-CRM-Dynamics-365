using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Xrm.Sdk;
using MutuaideWCF.XRM;

namespace MutuaideWCF.DAL
{
    /// <exclude/>
    public class Compte
    {


        internal static EntityReference GetDistributeurByPhoneNumber(string phoneNumber, XrmServices crmService)
        {
            return (crmService.GetServiceContext().CreateQuery("account")
                .Where(a => a.GetAttributeValue<OptionSetValue>("new_accounttype").Value == 100000002
                            && a.GetAttributeValue<string>("new_telephone1e164") == phoneNumber)
                .Select(a => new EntityReference("account", a.GetAttributeValue<Guid>("accountid"))))
                .FirstOrDefault();
        }

    }
}