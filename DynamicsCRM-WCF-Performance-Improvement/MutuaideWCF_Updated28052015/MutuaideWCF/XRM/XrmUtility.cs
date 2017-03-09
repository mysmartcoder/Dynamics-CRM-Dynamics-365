using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace MutuaideWCF.XRM
{
    /// <exclude/>
    public class XrmUtility
    {
        /// <summary>
        /// Retourne l'url de l'enregistrement CRM.
        /// </summary>
        /// <param name="id">Guid de l'enregistrement</param>
        /// <param name="entityName">Nom de l'entité.</param>
        /// <returns>URL enregistremnt CRM</returns>
        public static string FormatUrl(Guid id, string entityName)
        {
            return
                 "main.aspx?etn=" + entityName + "&pagetype=entityrecord&id=" + id;
            // GetServeurUrl() + "main.aspx?etn=" + entityName + "&pagetype=entityrecord&id=" + id;
        }
        /// <summary>
        /// Retourne l'url du serveur CRM sous la forme http://{serverName:Port}/{Organization}/
        /// </summary>
        /// <returns></returns>
        public static string GetServeurUrl()
        {
            return ConfigurationManager.AppSettings["UrlCrm"];
        }

        /// <summary>
        /// Assign un enregistrement à un utilisateur
        /// </summary>
        /// <param name="entityRef">Entité à assigner</param>
        /// <param name="owner">Nouveau propriétaire</param>
        /// <param name="crmServices">Service CRM</param>
        public static void ChangeOwner(EntityReference entityRef, EntityReference owner, XrmServices crmServices)
        {
            AssignRequest assign = new AssignRequest
            {
                //Nouveau Owner
                Assignee = owner,
                //Entity a modifier
                Target = entityRef
            };
            // Execute la requete.
            crmServices.GetService().Execute(assign);

        }

    }
}