using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using Microsoft.Xrm.Sdk;
using MutuaideWCF.XRM;

namespace MutuaideWCF.DAL
{
    /// <exclude/>
    public class SystemUser
    {

        /// <summary>
        /// Retourne l'utilisateur CRM à partir de son login
        /// </summary>
        /// <param name="login"></param>
        /// <param name="crmServices"></param>
        /// <returns></returns>
        public static EntityReference GetUserFromLogin(string login, XrmServices crmServices)
        {
            return
                crmServices.GetServiceContext().CreateQuery("systemuser")
                    .Where(su => su.GetAttributeValue<string>("domainname") == login)
                    .Select(su => new EntityReference
                    {
                        Id = su.Id,
                        LogicalName = su.LogicalName,
                        Name = su.GetAttributeValue<string>("fullname")
                    })
                    .FirstOrDefault();
        }



    }
}