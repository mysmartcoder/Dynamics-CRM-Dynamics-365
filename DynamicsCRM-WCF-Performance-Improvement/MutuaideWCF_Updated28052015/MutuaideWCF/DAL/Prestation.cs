using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Microsoft.Xrm.Sdk;
using MutuaideWCF.XRM;

namespace MutuaideWCF.DAL
{
    /// <exclude/>
    public class Prestation
    {
        public static Entity GetSubTypeId(string subTypeName, XrmServices crmService)
        {
            return crmService.GetServiceContext().CreateQuery("new_subtype")
                .GroupJoin(crmService.GetServiceContext().CreateQuery("new_codevisa"),
                s => s.GetAttributeValue<EntityReference>("new_codevisaid").Id,
                c => c.GetAttributeValue<Guid>("new_codevisaid"),
                (s, c) => new { s, c })
                .SelectMany(sc => sc.c.DefaultIfEmpty(), (sc, c) => new { sc, c })
                .Where(sc => sc.sc.s.GetAttributeValue<string>("new_subtype").Contains(subTypeName))
                .Select(s => new Entity("new_subtype")
                {
                    Id = s.sc.s.Id,
                    Attributes = new AttributeCollection()
                    {
                        new KeyValuePair<string, object>("new_code",s.c.GetAttributeValue<string>("new_name")),
                        new KeyValuePair<string, object>("new_label",s.c.GetAttributeValue<string>("new_label"))

                    }
                }).FirstOrDefault();

        }

        public static EntityReference GetCategoryId(string categorieName, XrmServices crmService)
        {
            return crmService.GetServiceContext().CreateQuery("new_category")
                .Where(c => c.GetAttributeValue<string>("new_category").Contains(categorieName))
                .Select(c => new EntityReference("new_category", c.Id))
                .FirstOrDefault();

        }
    }
}