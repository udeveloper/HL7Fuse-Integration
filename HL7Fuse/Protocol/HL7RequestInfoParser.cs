using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.SocketBase.Protocol;
using NHapi.Base.Parser;
using NHapiTools.Base.Parser;
using NHapiTools.Base.Validation;
using NHapi.Base.Model;
using System.Configuration;
using NHapi.Base.validation.impl;
using NHapiTools.Base.Util;
using NHapi.Model.V25.Message;

namespace HL7Fuse.Protocol
{
    public class HL7RequestInfoParser : IRequestInfoParser<HL7RequestInfo>
    {
        #region Private properties
        private bool HandleEachMessageAsEvent
        {
            get
            {
                bool result = false;
                if (!bool.TryParse(ConfigurationManager.AppSettings["HandleEachMessageAsEvent"], out result))
                    result = false;

                return result;
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Parse the message to a RequestInfo class
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public HL7RequestInfo ParseRequestInfo(string message)
        {
            return ParseRequestInfo(message, string.Empty);
        }

        public HL7RequestInfo ParseRequestInfo(string message, string protocol)
        {
            
            HL7RequestInfo result = new HL7RequestInfo();
            PipeParser parser = new PipeParser(){ ValidationContext=new DefaultValidation()};

            result = new HL7RequestInfo();

            try
            {
                IMessage hl7Message = parser.Parse(message);

                if (HandleEachMessageAsEvent)
                    result.Key = "V" + hl7Message.Version.Replace(".", "") + "." + hl7Message.GetStructureName();
                else
                    result.Key = "V" + hl7Message.Version.Replace(".", "") + ".MessageFactory";

                if (!string.IsNullOrEmpty(protocol))
                    result.Key += protocol;

                result.Message = hl7Message;
            }
            catch (Exception e)
            {
                MakeACKError(result, e);
                //An exception should be shown here 
                Logging.Logger.Error("Message failed during parsing:" + e.Message +   " - Mensaje HL7 Recibido "  + message);
            }

            // Parse the message
            return result;
        }

        private static void MakeACKError(HL7RequestInfo result, Exception e)
        {
            result.ErrorMessage = e.Message;
            string messageString = "MSH|^~\\&|SENDING_APPLICATION|SENDING_FACILITY|RECEIVING_APPLICATION|RECEIVING_FACILITY|20110614075841||ACK|1407511|P|2.5||||||\r\n" +
                                   "MSA|AE|1407511| Fallo la validacion del mensaje . " +  e.Message + "||";
            var ackResponseMessage = new PipeParser().Parse(messageString) as ACK;
            result.Message = ackResponseMessage;
            result.Key = "V25.MessageFactory";
        }
        #endregion
    }
}
