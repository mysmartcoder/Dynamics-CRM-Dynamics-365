//using System.Windows;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.EnterpriseServices;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using log4net;
using log4net.Config;


namespace MutuaideWCF.Log
{
    //<exclude/>
    public static class FileLogHelper
    {
        public static string FILE_FOLDER_PATH = @"c:\Logs";
        public static bool IS_LOGS_WRITABLE = true;
        public static string DATE_FORMAT = "yyyyMMdd";
        public static string FileName = "Log-WebService";



        /// <summary>
        /// Check if the file exists
        /// </summary>
        /// <param name="filePath_">Path to the file</param>
        /// <returns></returns>
        public static bool doExists(string filePath_)
        {
            string file = string.Format(filePath_, DateTime.Now.ToString(DATE_FORMAT));
            return File.Exists(file);
        }
        

        /// <summary>
        /// Write a message to a file
        /// </summary>
        /// <param name="ex_"></param>
        public static void WriteLine(Exception ex_)
        {
            _WriteLine(ex_, null, null);
        }
        

        /// <summary>
        /// Write a message to a file
        /// </summary>
        /// <param name="ex_"></param>
        /// <param name="message_"></param>
        public static void WriteLine(Exception ex_, string message_)
        {
            _WriteLine(ex_, message_, null);
        }
        

        /// <summary>
        /// Write a message to a file
        /// </summary>
        /// <param name="ex_"></param>
        /// <param name="obj_"></param>
        public static void WriteLine(Exception ex_, object obj_)
        {
            _WriteLine(ex_, null, obj_);
        }
        

        /// <summary>
        /// Write a message to a file
        /// </summary>
        /// <param name="ex_"></param>
        /// <param name="message_"></param>
        /// <param name="obj_"></param>
        public static void WriteLine(Exception ex_, string message_, object obj_)
        {
            _WriteLine(ex_, message_, obj_);
        }
        

        /// <summary>
        /// Write a message to a file
        /// </summary>
        /// <param name="filePath_"></param>
        /// <param name="message_"></param>
        private static void _WriteLine(Exception ex_, string message_, object obj_, string fileName = "")
        {
            // If we don't want to write on the log files
            if (!IS_LOGS_WRITABLE)
                return;
            string logPath = ConfigurationManager.AppSettings["logPath"];

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = FileName;
            }

            if (!string.IsNullOrEmpty(logPath))
            {
                FILE_FOLDER_PATH = logPath;
            }

            try
            {
                // If we need to write to the %TEMP% directory
                if (FILE_FOLDER_PATH.ToLower() == FILE_FOLDER_PATH)
                    FILE_FOLDER_PATH = System.IO.Path.GetTempPath();

                // Remove the last '/' of the folder path
                if (FILE_FOLDER_PATH.EndsWith(@"/") || FILE_FOLDER_PATH.EndsWith(@"\"))
                    FILE_FOLDER_PATH = FILE_FOLDER_PATH.Substring(0, FILE_FOLDER_PATH.Length - 1);

                // Does the folder exist ?
                if (!Directory.Exists(FILE_FOLDER_PATH))
                {
                    Directory.CreateDirectory((FILE_FOLDER_PATH));
                    return;
                }

                string file = FILE_FOLDER_PATH + @"\" + fileName + DateTime.Now.ToString(DATE_FORMAT) + ".txt";
                // Does the file exist ?
                if (!File.Exists(file))
                {
                    FileStream fs = File.Create(file);
                    fs.Close();
                }


                StringBuilder builder = new StringBuilder();
                using (TextWriter txtwriter = new StreamWriter(new FileStream(file, FileMode.Append, FileAccess.Write), Encoding.UTF8))
                {
                    // Custom Message
                    if (!String.IsNullOrEmpty(message_))
                    {
                        txtwriter.WriteLine("--------------------------------------------------");
                        txtwriter.WriteLine(DateTime.Now.ToString());
                        txtwriter.WriteLine(message_);
                    }
                    // Object seriaization
                    if (obj_ != null)
                    {
                        XmlSerializer xs = new XmlSerializer(obj_.GetType());
                        StringWriter sw = new StringWriter();
                        xs.Serialize(sw, obj_);
                        txtwriter.WriteLine("--------------------------------------------------");
                        txtwriter.WriteLine(DateTime.Now.ToString());
                        txtwriter.WriteLine(sw.ToString());
                    }
                    // Exception error
                    if (ex_ != null)
                    {
                        txtwriter.WriteLine("--------------------------------------------------");
                        txtwriter.WriteLine(DateTime.Now.ToString());
                        txtwriter.WriteLine("Message: " + ex_.Message);
                        if (ex_.InnerException != null)
                            txtwriter.WriteLine("Inner exception: " + ex_.InnerException.ToString());
                        txtwriter.WriteLine("Stack trace: " + ex_.StackTrace);
                    }



                }
            }
            catch { }
        }
    }
    [Serializable]
    public class LogRequestHelper
    {
        public string Request { get; set; }
        public string Method { get; set; }
        public string Function { get; set; }
        public List<string> Param { get; set; }
        public XElement Result { get; set; }
        public jsonReturn ResultJson { get; set; }

        public LogRequestHelper()
        {
        }

        public LogRequestHelper(XElement resultXml, List<string> parameter)
        {
            try
            {

                var httpRequest = WebOperationContext.Current.IncomingRequest;
                if (httpRequest.Method.ToLower() == "get")
                {
                    Request = httpRequest.UriTemplateMatch.RequestUri.ToString();
                    Function = httpRequest.UriTemplateMatch.RelativePathSegments.FirstOrDefault();
                    Param =
                    httpRequest.UriTemplateMatch.RelativePathSegments.Where(
                        p => p != httpRequest.UriTemplateMatch.RelativePathSegments.FirstOrDefault()).ToList();
                }
                else
                {
                    Function = httpRequest.Headers["SOAPAction"];
                    Param = parameter;

                }
                Method = WebOperationContext.Current.IncomingRequest.Method;


                Result = resultXml;


            }
            catch (Exception ex)
            {
                // On ne fait rien
            }
        }

        public LogRequestHelper(jsonReturn resultJson, List<string> parameter)
        {
            try
            {

                var httpRequest = WebOperationContext.Current.IncomingRequest;
                if (httpRequest.Method.ToLower() == "get")
                {
                    Request = httpRequest.UriTemplateMatch.RequestUri.ToString();
                    Function = httpRequest.UriTemplateMatch.RelativePathSegments.FirstOrDefault();
                    Param =
                    httpRequest.UriTemplateMatch.RelativePathSegments.Where(
                        p => p != httpRequest.UriTemplateMatch.RelativePathSegments.FirstOrDefault()).ToList();
                }
                else
                {
                    Function = httpRequest.Headers["SOAPAction"];
                    Param = parameter;

                }
                Method = WebOperationContext.Current.IncomingRequest.Method;
                ResultJson = resultJson;


            }
            catch (Exception ex)
            {
                // On ne fait rien
            }
        }
    }



    ///************************** 5/28/2015 ************************************
    /// <summary>
    /// Enum for log4net levels
    /// </summary>
    public enum LogLevelL4N
    {
        DEBUG = 1,
        ERROR,
        FATAL,
        INFO,
        WARN
    }
    /// <summary>
    /// Static class to log error messagesS
    /// </summary>
    public static class Logfile
    {
        #region Members
        private static ILog logger;
        #endregion

        #region Constructors
        static Logfile()
        {
            XmlConfigurator.Configure();
        }
        #endregion

        #region Methods
        /// <summary>
        /// It resolves module name and writes log in the file 
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="log"></param>
        public static void WriteLog(LogLevelL4N logLevel, String log)
        {
            string methodName = new System.Diagnostics.StackFrame(1, true).GetMethod().Name;
            string moduleName = new System.Diagnostics.StackFrame(1, true).GetMethod().ReflectedType.FullName;

            logger = LogManager.GetLogger(moduleName);
            if (logLevel.Equals(LogLevelL4N.DEBUG))
            {
                logger.Debug(" " + methodName + ": " + log);
            }

            else if (logLevel.Equals(LogLevelL4N.ERROR))
            {
                logger.Error(" " + methodName + ": " + log);
            }

            else if (logLevel.Equals(LogLevelL4N.FATAL))
            {
                logger.Fatal(" " + methodName + ": " + log);
            }

            else if (logLevel.Equals(LogLevelL4N.INFO))
            {
                logger.Info(" " + methodName + ": " + log);
            }

            else if (logLevel.Equals(LogLevelL4N.WARN))
            {

                logger.Warn(" " + methodName + ": " + log);

            }

        }

        #endregion

    }
}
