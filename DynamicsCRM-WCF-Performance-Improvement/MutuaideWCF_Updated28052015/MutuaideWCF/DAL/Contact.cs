using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.Xrm.Sdk;
using MutuaideWCF.XRM;
using Microsoft.Xrm.Sdk.Client;

namespace MutuaideWCF.DAL
{
    /// <exclude/>
    public partial class Contact
    {
        /// <summary>
        /// Retourne le contact correspondant à l'id du porteur de carte
        /// </summary>
        /// <param name="pcidssId">id du porteur de carte</param>
        /// <param name="crmServices">CRM Services</param>
        /// <returns></returns>
        internal static Entity GetContactById(string pcidssId, XrmServices crmServices)
        {
            return (crmServices.GetServiceContext().CreateQuery("contact")
                .Where(c => c.GetAttributeValue<string>("new_idcrm") == pcidssId)
                .Select(c => new Entity("contact")
                {
                    Id = c.Id,
                    LogicalName = "contact",
                    Attributes = new AttributeCollection()
                    {
                        new KeyValuePair<string, object>("emailaddress1", c.GetAttributeValue<string>("emailaddress1")),
                        new KeyValuePair<string, object>("emailaddress2", c.GetAttributeValue<string>("emailaddress2")),
                        new KeyValuePair<string, object>("statuscode", c.GetAttributeValue<OptionSetValue>("statuscode"))
                    }

                }).FirstOrDefault() ?? new Entity());

        }

        internal static Entity GetContactByName(string lastName, string firstName, DateTime birthDate, string codeCdo, XrmServices crmServices)
        {
            return (crmServices.GetServiceContext().CreateQuery("contact")
                .Join(crmServices.GetServiceContext().CreateQuery("account"),
                c => c.GetAttributeValue<EntityReference>("new_cdoid").Id,
                a => a.GetAttributeValue<Guid>("accountid"),
                (c, a) => new { c, a })
                .Where(ca => (ca.c.GetAttributeValue<string>("lastname") == lastName
                             && ca.c.GetAttributeValue<string>("firstname") == firstName
                             && ca.c.GetAttributeValue<DateTime>("birthdate") == birthDate.ToUniversalTime()
                             && ca.a.GetAttributeValue<string>("new_code") == codeCdo)))
                .Select(ca => ca.c).FirstOrDefault();



            //return (crmServices.GetServiceContext().CreateQuery("contact")
            //    .Where(c => c.GetAttributeValue<string>("lastname") == lastName
            //                   && c.GetAttributeValue<string>("firstname") == firstName
            //                   && c.GetAttributeValue<DateTime>("birthdate") == birthDate)
            //    .Select(c => new { contact = c,}).FirstOrDefault());


        }


        internal static Entity GetStatusContactByPciDssId(string id, XrmServices crmService)
        {
            Guid contact = Guid.Empty;
            return (crmService.GetServiceContext()
                 .CreateQuery("contact")
                 .Where(e => e.GetAttributeValue<string>("new_idcrm") == id)
                 .Select(e => new Entity("contact")
                 {
                     Id = e.Id,
                     Attributes = new AttributeCollection()
                     {
                        new KeyValuePair<string, object>("statecode", e.GetAttributeValue<OptionSetValue>("statecode")??new OptionSetValue()) 
                     }
                 })
                 .FirstOrDefault());

        }

        internal static bool UpdateEmail(string customerEmail, ref Entity customer)
        {
            bool update = false;
            if (customer.GetAttributeValue<string>("emailaddress1") == null)
            {
                customer.Attributes["emailaddress1"] = customerEmail;
                update = true;
            }
            else
            {
                if (customer.GetAttributeValue<string>("emailaddress2") == null &&
                    customer.GetAttributeValue<string>("emailaddress1") != customerEmail)
                {
                    customer.Attributes["emailaddress2"] = customerEmail;
                    update = true;
                }
            }
            return update;
        }

        internal static List<Entity> GetContactByEmailOnContract(Guid contractId, string email, XrmServices crmService)
        {
            var result = (crmService.GetServiceContext().CreateQuery("contact")
                .GroupJoin(crmService.GetServiceContext().CreateQuery("contact"),
                //CP est égale au contact porteur
                    cp => cp.GetAttributeValue<Guid>("contactid"),
                    c => c.GetAttributeValue<EntityReference>("new_contactcustomerid").Id,
                    (cp, c) => new { cp, c })
                .SelectMany(@t => @t.c.DefaultIfEmpty(), (@t, c) => new { @t, c })
                .Where(_ => ((_.t.cp.GetAttributeValue<string>("emailaddress1") == email || _.t.cp.GetAttributeValue<string>("emailaddress2") == email
                        && _.t.cp.GetAttributeValue<EntityReference>("new_contractid").Id == contractId)
                        || (_.c.GetAttributeValue<string>("emailaddress1") == email || _.c.GetAttributeValue<string>("emailaddress2") == email)))
                .ToList());
            // .Where(cpc =>
            //     ((cpc.cpc.cp.GetAttributeValue<string>("emailaddress1").Contains(email) || cpc.cpc.cp.GetAttributeValue<string>("emailaddress2").Contains(email))
            //         && cpc.cpc.cp.GetAttributeValue<EntityReference>("new_contractid").Id == contractId)
            //)
            // .Select(cpc => new { cpc.cpc, cpc.c })).ToList();
            List<Entity> listContact = new List<Entity>();
            if (result.Any())
            {
                listContact =
                     result.Where(
                         _ =>
                             _.t.cp.GetAttributeValue<string>("emailaddress1") == email ||
                             _.t.cp.GetAttributeValue<string>("emailaddress2") == email)
                             .Select(_ => new Entity("contact")
                             {
                                 Id = _.t.cp.GetAttributeValue<Guid>("contactid"),
                                 Attributes = new AttributeCollection()
                                {
                                    new KeyValuePair<string, object>("new_idcrm",_.t.cp.GetAttributeValue<string>("new_idcrm"))
                                }
                             }).ToList();
                if (!listContact.Any())
                {
                    listContact =
                    result.Where(
                        _ =>
                            _.c.GetAttributeValue<string>("emailaddress1") == email ||
                            _.c.GetAttributeValue<string>("emailaddress2") == email)
                            .Select(_ => new Entity("contact")
                            {
                                Id = _.t.cp.GetAttributeValue<Guid>("contactid"),
                                Attributes = new AttributeCollection()
                                {
                                    new KeyValuePair<string, object>("new_idcrm",_.t.cp.GetAttributeValue<string>("new_idcrm"))
                                }
                            }).ToList();
                }
                //Console.Write("test");
            }

            return listContact;
        }


        internal static List<Entity> GetLastRequestContact(string id, Guid customerId, XrmServices crmServices)
        {
            OrganizationServiceContext crmContext = crmServices.GetServiceContext();
            List<Entity> incidentList = new List<Entity>();


            // récupération de la liste de demande (incident) d'iun client
            var result = (crmContext.CreateQuery("incident")
               .GroupJoin(crmContext.CreateQuery("new_prestation"),
                   @i => i.GetAttributeValue<Guid>("incidentid"),
                   p => p.GetAttributeValue<EntityReference>("new_demandid").Id, (@i, p) => new { @i, p })
               .SelectMany(@t => @t.p.DefaultIfEmpty(), (@t, ic) => new { @t, ic })
               .Where(@t => @t.@t.i.GetAttributeValue<EntityReference>("customerid").Id == customerId))
               .OrderByDescending(o => o.t.i.GetAttributeValue<DateTime>("createdon"))
               .Select(e => new
               {
                   incident = e.t.i,
                   prestation = e.ic,

               }).Take(100).ToList().GroupBy(_ => _.incident.Id).ToList();

            //Chargement de la liste des prestations dans une seule entité et chargement de l'entité subType et CodeVise pour chaque prestation

            //result.ToList().Where(_=>_.)ForEach();
            foreach (var r in result)
            {
                // var ip = r.FirstOrDefault(i => i.incident.Id == r.Key);
                r.ToList().ForEach(_ => _.prestation.Attributes.Add("subtype", Prestation.GetSubTypeId((_.prestation.GetAttributeValue<EntityReference>("new_subtypeid") ?? new EntityReference()).Name, crmServices)));
                Entity incident = r.FirstOrDefault().incident;
                //ip.prestation.Attributes.Add("subtype", Prestation.GetSubTypeId(ip.prestation.GetAttributeValue<EntityReference>("new_subtypeid").Name, crmServices));
                incident.Attributes.Add("prestations", r.ToList().Select(_ => _.prestation).ToList());
                incidentList.Add(incident);
            }
            return incidentList;
            //var group = result.ToList().GroupBy(_ => _.incident.Id);




            //Dictionary<Guid, Entity> listIncident = new Dictionary<Guid, Entity>();

            //// chargement des incidents dans une list pour les avoirs une seul fois
            //foreach (var item in result)
            //{
            //    try
            //    {
            //        listIncident.Add((Guid)item.incident.Attributes["incidentid"], item.incident);
            //    }
            //    catch (Exception e) { }
            //}


            //// chargmeent de la liste des prestation pour chaque incident
            //foreach (var item in result)
            //{
            //    // récupératino de la demande 
            //    Entity demande = listIncident[(Guid)item.incident.Attributes["incidentid"]];
            //    List<Entity> listPresta = new List<Entity>();

            //    // on verifie si la demande contient deja une liste de prestation 
            //    if (demande.Attributes.Contains("listPresta"))
            //        listPresta = (List<Entity>)demande.Attributes["listPresta"];

            //    listPresta.Add(item.prestation);

            //    if (!demande.Attributes.Contains("listPresta"))
            //        demande.Attributes.Add("listPresta", listPresta);
            //    else
            //        demande["listPresta"] = listPresta;

            //}



            //////// récupération de la liste de demande (incident) d'iun client
            ////var result = (crmContext.CreateQuery("incident")
            ////    .GroupJoin(crmContext.CreateQuery("new_prestation"),
            ////        @i => i.GetAttributeValue<Guid>("incidentid"),
            ////        p => p.GetAttributeValue<EntityReference>("new_demandid").Id, (@i, p) => new { @i, p })
            ////    .SelectMany(@t => @t.p.DefaultIfEmpty(), (@t, ic) => new { @t, ic })
            ////    .Where(@t => @t.@t.i.GetAttributeValue<EntityReference>("customerid").Id == customerId))
            ////    .Select(e => new
            ////    {
            ////        incident = e.t.i,
            ////        prestation = e.ic,

            ////    }).Take(100).ToList().GroupBy(t => t.incident.Id).ToList();//.OrderBy(i => i.GetAttributeValue<DateTime>("createdon")); // <i.GetAttributeValue<Guid>("accountid")>;

            ////.Select(@t => new Entity("incident")
            ////    {
            ////        Attributes = new AttributeCollection
            ////        {
            ////            new KeyValuePair<string, object>("new_demandnumber",
            ////                (string.IsNullOrEmpty(@t.@t.@i.GetAttributeValue<string>("new_demandnumber"))?"":@t.@t.@i.GetAttributeValue<string>("new_demandnumber"))),
            ////            new KeyValuePair<string, object>("createdon",
            ////                @t.@t.@i.GetAttributeValue<DateTime>("createdon")),
            ////            new KeyValuePair<string, object>("statuscode",
            ////                @t.@t.@i.GetAttributeValue<OptionSetValue>("statuscode")),
            ////            new KeyValuePair<string, object>("incidentid", @t.@t.@i.GetAttributeValue<Guid>("incidentid")),
            ////            new KeyValuePair<string, object>("customerId", customerId)
            ////        }
            ////    })




            //return listIncident;
        }


        internal static List<Entity> GetContactByPhoneOnContract(Guid contractId, string phoneNumber,
            XrmServices crmsServices)
        {
            List<Entity> contactList = new List<Entity>();
            var contacts = (crmsServices.GetServiceContext().CreateQuery("contact")
                .Join(crmsServices.GetServiceContext().CreateQuery("new_phoneschedule"),
                    c => c.GetAttributeValue<Guid>("contactid"),
                    t => t.GetAttributeValue<EntityReference>("new_contactid").Id,
                    (c, t) => new { c, t })
                .Where(ct => //ct.t.GetAttributeValue<OptionSetValue>("new_phonetype").Value == 100000001
                              ((ct.c.GetAttributeValue<EntityReference>("new_contractid") == null && ct.t.GetAttributeValue<string>("new_phonenumbere164") == phoneNumber)
                              || (ct.c.GetAttributeValue<EntityReference>("new_contractid").Id == contractId && ct.t.GetAttributeValue<string>("new_phonenumbere164") == phoneNumber))
							  && (ct.c.GetAttributeValue<OptionSetValue>("statecode").Value == 0)
                              )
                .Select(ct => ct.c)
                .ToList());

            if (contacts.Any())
            {
                //Si tous les enregistrements n'ont pas de contrats c'est que ceux ne sont pas des contacts porteurs.
                if (contacts.All(c => c.GetAttributeValue<EntityReference>("new_contractid") == null))
                {


                    contactList = crmsServices.GetServiceContext().CreateQuery("contact")
                     .Join(crmsServices.GetServiceContext().CreateQuery("contact"),
                         c => c.GetAttributeValue<EntityReference>("new_contactcustomerid").Id,
                         cp => cp.GetAttributeValue<Guid>("contactid"),
                         (c, cp) => new { c, cp })
                     .Join(crmsServices.GetServiceContext().CreateQuery("new_phoneschedule"),
                     cp => cp.c.GetAttributeValue<Guid>("contactid"),
                     tel => tel.GetAttributeValue<EntityReference>("new_contactid").Id,
                     (cp, tel) => new { cp, tel })
                     .Where(cpt => //ct.t.GetAttributeValue<OptionSetValue>("new_phonetype").Value == 100000001
                         cpt.tel.GetAttributeValue<string>("new_phonenumbere164") == phoneNumber
                         && cpt.cp.cp.GetAttributeValue<EntityReference>("new_contractid").Id == contractId
						 && cpt.cp.c.GetAttributeValue<OptionSetValue>("statecode").Value == 0
                              )
                     .Select(cpc => new Entity("contact")
                     {
                         Id = cpc.cp.c.GetAttributeValue<Guid>("contactid"),
                         Attributes = new AttributeCollection()
                     {
                         new KeyValuePair<string, object>("new_idcrm",cpc.cp.c.GetAttributeValue<string>("new_idcrm"))
                     }
                     }).ToList();

                   



                }
                //Sinon c'est qu'il y a forcement un contact porteur qui a ce contrat dans la liste.
                else
                {
                    contactList =
                        contacts.Where(
							c => c.Contains("new_contractid")
							&& c.GetAttributeValue<EntityReference>("new_contractid").Id == contractId
							&& c.GetAttributeValue<OptionSetValue>("statecode").Value == 0
							)
                           .Select(c => new Entity("contact")
                           {
                               Id = c.GetAttributeValue<Guid>("contactid"),
                               Attributes = new AttributeCollection()
                             {
                                 new KeyValuePair<string, object>("new_idcrm",c.GetAttributeValue<string>("new_idcrm"))
                             }
                           }).ToList();
                }
            }
            return contactList;
        }

    }
}