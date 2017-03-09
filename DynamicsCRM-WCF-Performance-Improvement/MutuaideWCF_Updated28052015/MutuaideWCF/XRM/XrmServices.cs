using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using MutuaideWCF.DAL;

namespace MutuaideWCF.XRM
{

    /// <exclude/>
    public class XrmServices : IDisposable
    {

        private readonly OrganizationService _service;
        private readonly OrganizationServiceContext _serviceContext;
        private readonly string _serverUrl;
        public readonly EntityReference callerId;

        public XrmServices()
        {
            try
            {
                CrmConnection connect =
                    CrmConnection.Parse("Url=" + ConfigurationManager.AppSettings["UrlCrm"] + "; Username=" +
                                        ConfigurationManager.AppSettings["Login"] + "; Password=" +
                                        ConfigurationManager.AppSettings["Password"]);
                this._service = new OrganizationService(connect);
                this._serviceContext = new OrganizationServiceContext(this._service);
                this.callerId = SystemUser.GetUserFromLogin(ConfigurationManager.AppSettings["Login"], this) ??
                                new EntityReference();
                _serverUrl = ConfigurationManager.AppSettings["UrlCrm"];
            }
            catch (Exception e)
            {
                throw new Exception("Erreur de connexion à la CRM : \n" + e.Message);
            }
        }

        public OrganizationService GetService()
        {
            return this._service;
        }

        public OrganizationServiceContext GetServiceContext()
        {
            return this._serviceContext;
        }

        public void Update(Entity updateEntity)
        {
            if (_serviceContext.GetAttachedEntities().Contains(updateEntity))
            {
                _serviceContext.UpdateObject(updateEntity);
                _service.Update(updateEntity);
            }
            else
            {
                _service.Update(updateEntity);
            }
        }



        #region IDisposable Membres

        public void Dispose()
        {
            this._serviceContext.Dispose();
            this._service.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}