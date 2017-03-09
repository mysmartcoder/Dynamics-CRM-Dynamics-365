using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MutuaideWCF.DAL
{
    /// <exclude/>
    public class customError
    {
        private static readonly List<customError> ErrorDictionnary = new List<customError>
        {
            new customError((int)CustomReturnCode.Unknown, "Erreur non prévue"),
            new customError((int)CustomReturnCode.Success, "Succès"),
            new customError((int)CustomReturnCode.CustomerNotFound, "Aucun client trouvé"),
            new customError((int)CustomReturnCode.ContractNotFound, "Aucun contrat trouvé"),
            /*new customError((int)CustomReturnCode.CustomerWithoutContract, "Aucun contrat associé au client"),*/
            new customError((int)CustomReturnCode.RequestNotFound, "Aucune demande trouvée"),
            new customError((int)CustomReturnCode.RequestFound, "Demande trouvée"),
            new customError((int)CustomReturnCode.MailNotFound, "Mail non trouvé"),
            new customError((int)CustomReturnCode.MailInProgress, "Mail en traitement"),
            new customError((int)CustomReturnCode.NewRequest, "Nouvelle demande"),
            new customError((int)CustomReturnCode.MultipleRequest, "Demandes multiples"),
            new customError((int)CustomReturnCode.MailClosed, "Mail traité "),
			new customError((int)CustomReturnCode.SubTypeNotFound, "Sous-type non trouvé"),
        };

        public customError(int _code, string _message)
        {
            code = _code;
            errorMessage = _message;
        }

        private readonly int code;
        private readonly string errorMessage;

        public static string getErrorMessage(int code)
        {
            return ErrorDictionnary.Where(e => e.code == code).Select(e => e.errorMessage).FirstOrDefault();
        }
    }
}