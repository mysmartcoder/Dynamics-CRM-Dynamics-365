using System;
using System.Collections.Generic;
using log4net;
using log4net.Config;
using System.Web;

namespace MutuaideWCF.Log
{
    public class Log4netTraceListener : System.Diagnostics.TraceListener
    {
        private readonly log4net.ILog _log;

        public Log4netTraceListener()
        {
            //XmlConfigurator.Configure();
            _log = log4net.LogManager.GetLogger("System.Diagnostics.Redirection");
        }

        public Log4netTraceListener(log4net.ILog log)
        {
            _log = log;
        }

        public override void Write(string message)
        {
            if (_log != null)
            {
                _log.Debug(message);
            }
        }

        public override void WriteLine(string message)
        {
            if (_log != null)
            {
                _log.Debug(message);
            }
        }
    }
}