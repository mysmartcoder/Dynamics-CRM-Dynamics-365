using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using System.Web.Services;
using System.Xml.Linq;

namespace MutuaideWCF
{
    /// <summary>
    /// Liste des codes d'erreurs spécifiques au webservice et leurs retours.
    /// </summary>
    public enum CustomReturnCode
    {
        /// <summary>
        /// Erreur non prévue
        /// </summary>
        Unknown = -1,
        /// <summary>
        /// Succès
        /// </summary>
        Success = 0,
        /// <summary>
        /// Aucun client trouvé 
        /// </summary>
        CustomerNotFound = 1,
        /// <summary>
        /// Aucun contrat trouvé
        /// </summary>
        ContractNotFound = 2,
        /*// <summary>
        /// Aucun contrat associé au client
        /// </summary>
        
        CustomerWithoutContract = 3,*/
        /// <summary>
        /// Aucune demande trouvée
        /// </summary>
        RequestNotFound = 4,
        /// <summary>
        /// Demande trouvée
        /// </summary>
        RequestFound = 5,
        /// <summary>
        /// Mail non trouvé
        /// </summary>
        MailNotFound = 6,
        /// <summary>
        /// Mail en traitement
        /// </summary>
        MailInProgress = 7,
        /// <summary>
        /// Mail traité
        /// </summary>
        MailClosed = 8,
        /// <summary>
        /// Nouvelle demande
        /// </summary>
        NewRequest = 9,
        /// <summary>
        /// Contrats multiples
        /// </summary>
        MultipleRequest = 10,
        /// <summary>
        /// Sous-type non trouvé (Interne JSI)
        /// </summary>
        SubTypeNotFound = 11,

    }
    /// <summary>
    /// Interface exposant l'ensemble des méthodes disponibles pour les interactions avec CRM
    /// </summary>
    [ServiceContract]

    public interface IMutuaideService
    {
        /// <summary>
        /// Retourne le mail et eventuellement la demande ou le client auquel il est associé.
        /// </summary>
        /// <param name="messageId">Id exchange du mail</param>
        /// <returns>
        ///<para><![CDATA[<error>]]></para>
        ///<para><![CDATA[  <value>Code Erreur (1,2,4,5,6,7,8,9)</value>]]></para>
        ///<para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        ///<para><![CDATA[</error>]]></para>
        ///<para><![CDATA[<result>]]></para>
        ///<para><![CDATA[  <activityid>Identifiant du mail</activityid>]]></para>
        ///<para><![CDATA[  <demandeid>Identifiant de la demande</demandeid>]]></para>
        ///<para><![CDATA[  <customerid>Identifiant du client</customerid>]]></para>
        ///<para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// Flux froids, étape 1. (chapitre 5.2.4.5.1 des specs)
        /// </remarks>
        [OperationContract]
        //[WebGet( RequestFormat = WebMessageFormat.Xml,UriTemplate = "GetEmailByExchangeId/{messageId}")]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml, UriTemplate = "GetEmailByExchangeId/{messageId}", BodyStyle = WebMessageBodyStyle.Bare)]
        XElement GetEmailByExchangeId(string messageId);


        /// <summary>
        /// Affecte le mail au concierge passé en paramètre
        /// </summary>
        /// <param name="messageId">id exchange du mail</param>
        /// <param name="domainName">Domaine du concierge</param>
        /// <param name="userName">Login du concierge</param>
        /// <returns>Retourne le résultat au format XML suivant :
        /// <para><![CDATA[<error>]]></para>
        ///<para><![CDATA[  <value>Code Erreur (spécifique)</value>]]></para>
        ///<para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        ///<para><![CDATA[</error>]]></para>
        ///<para><![CDATA[<result>]]></para>
        ///<para><![CDATA[  <value>True / False</value>]]></para>
        ///<para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// Flux froids, étape 2. (chapitre 5.2.4.5.2 des specs)
        /// </remarks>
        [OperationContract]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml, UriTemplate = "UpdateEmailByExchangeId/{messageId}/{domainName}/{userName}")]
        XElement UpdateEmailByExchangeId(string messageId, string domainName, string userName);

        /// <summary>
        /// Retourne une demande CRM ou un client à partir d'un numéro de téléphone
        /// </summary>
        /// <param name="phoneNumber">Numéro de téléphone associé au client</param>
        /// <param name="dialNumber">Numéro de téléphone du contrat composé</param>
        /// <param name="phoneCallId">Identifiant de l'appel téléphonique</param>
        /// <example></example>
        /// <returns>Retourne le résultat au format XML suivant :
        /// <para><![CDATA[<error>]]></para>
        ///<para><![CDATA[  <value>Code Erreur (1,2,3,4,5)</value>]]></para>
        ///<para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        ///<para><![CDATA[</error>]]></para>
        ///<para><![CDATA[<result>]]></para>
        ///<para><![CDATA[  <demandeid></demandeid>]]></para>
        ///<para><![CDATA[  <conciergeid></conciergeid>]]></para>
        ///<para><![CDATA[  <contactid></contactid>]]></para>
        ///<para><![CDATA[</result>]]></para>        
        ///  </returns>
        /// <remarks>
        /// Flux chauds. (chapitre 5.2.3.2 des specs)
        /// </remarks>
        [OperationContract]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml, UriTemplate = "GetRequestByPhoneNumber/{phoneNumber}/{dialNumber}/{phoneCallId}")]
        XElement GetRequestByPhoneNumber(string phoneNumber, string dialNumber, string phoneCallId);

        /// <summary>
        /// Retourne une partie de l'url de la fiche client et crée automatiquement une activité "Appel Téléphonique" dans la CRM liée au client.
        /// </summary>
        /// <param name="pcidssId">identifiant client (pcidss)</param>
        /// <param name="callerId">identifiant de l'appel téléphonique (Call-id)</param>
        /// <returns>Retourne le résultat au format XML suivant :
        /// <para><![CDATA[<error>]]></para>
        ///<para><![CDATA[  <value>Code Erreur (spécifique)</value>]]></para>
        ///<para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        ///<para><![CDATA[</error>]]></para>
        ///<para><![CDATA[<result>]]></para>
        ///<para><![CDATA[  <url></url>]]></para>
        ///<para><![CDATA[  <status> 100000001 (Consommateur) / 100000002 (Non intéressé) / 100000000 (Enregistré) / 100000003 (Dormant) </status>]]></para>
        ///<para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// (chapitre 6 des specs)
        /// </remarks>
        [OperationContract]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml, UriTemplate = "WelcomeCall/{pcidssId}/{callerId}/")]
        XElement WelcomeCall(string pcidssId, string callerId);

        /// <summary>
        /// Retourne le code contrat correspondant au BIN
        /// </summary>
        /// <param name="binCode">Code BIN</param>
        /// <returns>Retourne le résultat au format XML suivant :
        /// <para><![CDATA[<error>]]></para>
        /// <para><![CDATA[  <value>Code Erreur (2)</value>]]></para>
        /// <para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        /// <para><![CDATA[</error>]]></para>
        /// <para><![CDATA[<result>]]></para>
        /// <para><![CDATA[  <ContractCode></ContractCode>]]></para>
        /// <para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// PCIDSS. (chapitre 7 des specs)
        /// </remarks>
        [OperationContract]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml, UriTemplate = "GetContractByBin/{binCode}")]
        XElement GetContractByBin(string binCode);

        /// <summary>
        /// Permet de modifier le bin d'un client
        /// </summary>
        /// <param name="pcidssId">Identifiant du client</param>
        /// <param name="binCode">Code BIN à modifier</param>
        /// <returns>Retourne le résultat au format XML suivant :
        /// <para><![CDATA[<error>]]></para>
        /// <para><![CDATA[  <value>Code Erreur (spécifique)</value>]]></para>
        /// <para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        /// <para><![CDATA[</error>]]></para>
        /// <para><![CDATA[<result>]]></para>
        /// <para><![CDATA[  <resultCode>true / false</resultCode>]]></para>
        /// <para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// PCIDSS. (chapitre 7 des specs)
        /// </remarks>
        [OperationContract]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml, UriTemplate = "UpdateCustomerBin/{pcidssId}/{binCode}/")]
        XElement UpdateCustomerBin(string pcidssId, string binCode);

        /// <summary>
        /// Modifie le moyen de paiement d'une demande.
        /// </summary>
        /// <param name="demandeId">Identifiant unique de la demande CRM</param>
        /// <param name="paymentId">Identifiant du paiement à mettre à jour</param>
        /// <returns>
        /// <para><![CDATA[<error>]]></para>
        /// <para><![CDATA[  <value>Code Erreur (spécifique)</value>]]></para>
        /// <para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        /// <para><![CDATA[</error>]]></para>
        /// <para><![CDATA[<result>]]></para>
        /// <para><![CDATA[  <resultCode>true / false</resultCode>]]></para>
        /// <para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// PCIDSS. (chapitre 7 des specs)
        /// </remarks>
        [OperationContract]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml, UriTemplate = "UpdatePaymentMethod/{demandeId}/{paymentId}/")]
        XElement UpdatePaymentMethod(string demandeId, string paymentId);

        /// <summary>
        /// Retourne le client porteur de la carte
        /// </summary>
        /// <param name="lastName">Nom du client</param>
        /// <param name="firstName">Prénom du client</param>
        /// <param name="birthday">Date de naissance du client</param>
        /// <returns>Retourne le premier client porteur trouvé au format XML suivant :
        /// <para><![CDATA[<error>]]></para>
        /// <para><![CDATA[  <value>Code Erreur (1)</value>]]></para>
        /// <para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        /// <para><![CDATA[</error>]]></para>
        /// <para><![CDATA[<result>]]></para>
        /// <para><![CDATA[  <cdoid>Identifiant du CDO</cdoid>]]></para>
        /// <para><![CDATA[  <firstname>Prénom du client</firstname>]]></para>
        /// <para><![CDATA[  <lastname>Nom du client</lastname>]]></para>
        /// <para><![CDATA[  <city>Code postal + ville du client</city>]]></para>
        /// <para><![CDATA[  <bankcode>30002</bankcode>]]></para>
        /// <para><![CDATA[  <id_porteur>Identifiant porteur du client</id_porteur>]]></para>
        /// <para><![CDATA[  <pcidssid>Identifiant PCIDSS du client</pcidssid>]]></para>
        /// <para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// Visa Infinite. (chapitre 8.7.1 des specs)
        /// </remarks>
        [OperationContract]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml, UriTemplate = "RetrieveCustomer/{lastName}/{firstName}/{birthday}/")]
        XElement RetrieveCustomer(string lastName, string firstName, string birthday);

        /// <summary>
        /// Indique si le client à le statut actif dans la CRM
        /// </summary>
        /// <param name="pcidssId">identifiant du porteur</param>
        /// <returns>Retourne le résultat au format XML suivant :
        /// <para><![CDATA[<error>]]></para>
        ///<para><![CDATA[  <value>Code Erreur (1)</value>]]></para>
        ///<para><![CDATA[  <message>Message de l'erreur</message>]]></para>
        ///<para><![CDATA[</error>]]></para>
        ///<para><![CDATA[<result>]]></para>
        ///<para><![CDATA[  <resultcode>True / False</resultcode>]]></para>
        ///<para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// Visa Infinite. (chapitre 8.7.2 des specs)
        /// </remarks>
        [OperationContract]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml, UriTemplate = "IsCustomerActive/{pcidssId}")]
        XElement IsCustomerActive(string pcidssId);


        /// <summary>
        /// Retourne une liste des 100 dernières demandes effectuées par un client
        /// </summary>
        /// <param name="pcidssId">identifiant du porteur (id pcidss)</param>
        /// <returns>Retourne le résultat au format XML suivant :
        /// <para><![CDATA[<error>]]></para>
        /// <para><![CDATA[    <value>Code Erreur (2,10)</value>]]></para>
        /// <para><![CDATA[    <message>Message de l'erreur</message>]]></para>
        /// <para><![CDATA[</error>]]></para>
        /// <para><![CDATA[<result>]]></para>
        /// <para><![CDATA[    <request>]]></para>        
        /// <para><![CDATA[        <casenumber>Numéro de la demande</casenumber>]]></para>
        /// <para><![CDATA[        <prestationtype>]]></para>
        /// <para><![CDATA[            <value>Code prestation</value>]]></para>
        /// <para><![CDATA[            <text>Libellé prestation</text>]]></para>
        /// <para><![CDATA[        </prestationtype>]]></para>
        /// <para><![CDATA[        <comments>Commentaire CRM non interprété</comments>]]></para>
        /// <para><![CDATA[        <createdon>Date de création de la demande</createdon>]]></para>
        /// <para><![CDATA[        <status>Statut de la demande (CREATED, CLOSED, CANCEL)</status>]]></para>
        /// <para><![CDATA[    </request>]]></para>
        /// <para><![CDATA[    ...]]></para>
        /// <para><![CDATA[</result>]]></para>
        /// </returns>
        /// <remarks>
        /// Visa Infinite. (chapitre 8.7.3 des specs)
        /// </remarks>
        [OperationContract]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml, UriTemplate = "GetLastRequest/{pcidssId}")]
        XElement GetLastRequest(string pcidssId);
        /// <summary>
        ///  - INTERNE CRM JSI -
        /// Indique si le domaine du mail est valide
        /// </summary>
        /// <param name="email">email à vérifier</param>
        /// <returns>True ou False</returns>
        [OperationContract]
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml, UriTemplate = "MailValidation/{email}")]
        bool MailValidation(string email);

        /// <summary>
        ///  - INTERNE CRM JSI -
        /// Retourne la liste des Services et prestations.
        /// </summary>
        /// <param name="clientId">Identifiant du client</param>
        /// <param name="subtypeId">Identifiant du sous type de prestation</param>
        /// <returns>Liste de services correspondants</returns>
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "SearchServices/{clientId}/{subtypeId}")]
        jsonReturn SearchServices(string clientId, string subtypeId);

    }
}
