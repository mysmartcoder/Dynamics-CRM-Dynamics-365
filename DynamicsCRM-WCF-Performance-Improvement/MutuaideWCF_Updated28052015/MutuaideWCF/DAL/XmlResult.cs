using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel.Web;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using MutuaideWCF.Log;

namespace MutuaideWCF.DAL
{
    /// <exclude/>
    [DataContractAttribute]
    public class XmlResult
    {
        [DataMemberAttribute]
        private XDocument _xml;

        private bool logAll;

        public XmlResult()
        {

            _xml = new XDocument(new XElement("root"));
            _xml.Declaration = new XDeclaration("1.0", "utf-8", "true");
            //_xml.Declaration.Version = "1.0";
            //_xml.Declaration.Encoding = "utf-8";
            _xml.Root.Add(new XElement("error",
                                 new XElement("value"),
                                 new XElement("message"))
                           , new XElement("result"));
            logAll = ConfigurationManager.AppSettings["LogAll"].ToLower().Equals("true");

            //_xml.Add();
        }

        public void AddResultElement(string attributeName, object value, string nodeName = "")
        {
            this._xml.Root.Element("result").Add(new XElement(attributeName, value));
        }

        public void AddResultElement(XElement xelement)
        {

            this._xml.Root.Element("result").Add(xelement);

        }



        public void SetError(int code, string message, Exception e = null)
        {

            FileLogHelper.WriteLine(e ?? new Exception(message));
            _xml.Root.Element("error").Elements("value").FirstOrDefault().SetValue(code);
            _xml.Root.Element("error").Elements("message").FirstOrDefault().SetValue(message);

        }

        public override string ToString()
        {
            return _xml.Declaration + _xml.ToString(SaveOptions.None);
        }

        public XElement getXml(List<String> parameter)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Cache-Control", "no-cache");
            if (logAll && _xml != null && _xml.Root != null)
            {
                Log.FileLogHelper.WriteLine(null, new LogRequestHelper(_xml.Root, parameter));
            }

            return _xml.Root;
        }
    }
}