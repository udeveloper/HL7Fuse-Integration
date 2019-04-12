using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using HL7Fuse.Protocol;
using NHapi.Base;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapiTools.Base.Util;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using NHapi.Model.V25.Message;
using NHapi.Model.V25.Datatype;
using HL7Fuse.Helpers;
using System.IO;
using System.Diagnostics;

namespace HL7Fuse
{
    public class MLLPSession : AppSession<MLLPSession, HL7RequestInfo>
    {
        #region Private properties
        private bool AcceptEventIfNotImplemented
        {
            get
            {
                bool result = false;
                if (!bool.TryParse(ConfigurationManager.AppSettings["AcceptEventIfNotImplemented"], out result))
                    result = false;

                return result;
            }
        }
        #endregion

        #region Protected methods
        /// <summary>
        /// Handle Unknown request
        /// </summary>
        /// <param name="requestInfo"></param>
        protected override void HandleUnknownRequest(HL7RequestInfo requestInfo)
        {
            string msg = string.Empty;
            requestInfo.WasUnknownRequest = true;

            if (!AcceptEventIfNotImplemented)
            {
                requestInfo.ErrorMessage = "Unknown request.";
                msg = GetAck(requestInfo, requestInfo.ErrorMessage);
            }
            else
                msg = GetAck(requestInfo);

            this.Send(msg);
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Send
        /// </summary>
        /// <param name="requestInfo"></param>
        public void Send(HL7RequestInfo requestInfo)
        {
            string message = string.Empty;
            if (requestInfo.ResponseMessage != null)
            {
                PipeParser parser = new PipeParser();
                message = parser.Encode(requestInfo.ResponseMessage);
            }
            else
                message = GetAck(requestInfo);

            Send(message);
        }

        /// <summary>
        /// Send
        /// </summary>
        /// <param name="message"></param>
        public override void Send(string message)
        {
            message = MLLP.CreateMLLPMessage(message);

            base.Send(message);
        }
        #endregion

        #region Private methods
        private string GetAck(HL7RequestInfo requestInfo)
        {

             GetBinaryDataMessageHL7(requestInfo.Message);

            if (requestInfo.HasError)
                return GetAck(requestInfo, requestInfo.ErrorMessage);
            else
                return GetAck(requestInfo, null);
        }

        private string GetAck(HL7RequestInfo requestInfo, string error)
        {
            Ack ack = new Ack(ConfigurationManager.AppSettings["CommunicationName"], ConfigurationManager.AppSettings["EnvironmentIdentifier"]);
            IMessage result;
            if (error == null)
                result = ack.MakeACK(requestInfo.Message);
            else
                result = ack.MakeACK(requestInfo.Message, AckTypes.AE, error);

            PipeParser parser = new PipeParser();

            return parser.Encode(result);
        }

        private bool GetBinaryDataMessageHL7(IMessage message)
        {
            var processValid = false;

            var messageHL7Parsed = message;

            if (messageHL7Parsed is ORU_R01)
            {
                var oruMessage = (ORU_R01)messageHL7Parsed;

                if (oruMessage != null)
                {
                    // Display the updated HL7 message using Pipe delimited format
                    LogToDebugConsole("Parsed HL7 Message:");
                    LogToDebugConsole(new PipeParser().Encode(messageHL7Parsed));

                    var encapsulatedPdfDataInBase64Format = ExtractEncapsulatedPdfDataInBase64Format(oruMessage);

                    //if no encapsulated data was found, you can cease operation
                    if (encapsulatedPdfDataInBase64Format != null)
                    {
                        var extractedPdfByteData = GetBase64DecodedPdfByteData(encapsulatedPdfDataInBase64Format);

                        WriteExtractedPdfByteDataToFile(extractedPdfByteData);

                        processValid = true;
                    }

                   
                }
            }


            return processValid;
        }

        private byte[] GetBase64DecodedPdfByteData(ED encapsulatedPdfDataInBase64Format)
        {
            var helpeB64 = new Base64Helper();

            LogToDebugConsole("Extracting PDF data stored in Base-64 encoded form from OBX-5..");
            var base64EncodedByteData = encapsulatedPdfDataInBase64Format.Data.Value;
            var extractedPdfByteData = helpeB64.ConvertFromBase64String(base64EncodedByteData);
            return extractedPdfByteData;
        }

        private ED ExtractEncapsulatedPdfDataInBase64Format(ORU_R01 oruMessage)
        {
            //start retrieving the OBX segment data to get at the PDF report content
            LogToDebugConsole("Extracting message data from parsed message..");
            var orderObservation = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION();
            var observation = orderObservation.GetOBSERVATION(0);
            var obxSegment = observation.OBX;

            var encapsulatedPdfDataInBase64Format = obxSegment.GetObservationValue(0).Data as ED;
            return encapsulatedPdfDataInBase64Format;
        }

        private void WriteExtractedPdfByteDataToFile(byte[] extractedPdfByteData)
        {
            string _extractedPdfOutputDirectory = "N:\\HL7TestOutputs";

            LogToDebugConsole($"Creating output directory at '{_extractedPdfOutputDirectory}'..");

            if (!Directory.Exists(_extractedPdfOutputDirectory))
                Directory.CreateDirectory(_extractedPdfOutputDirectory);

            var pdfOutputFile = Path.Combine(_extractedPdfOutputDirectory, Guid.NewGuid() + ".pdf");
            LogToDebugConsole(
                $"Writing the extracted PDF data to '{pdfOutputFile}'. You should be able to see the decoded PDF content..");
            try
            {
                File.WriteAllBytes(pdfOutputFile, extractedPdfByteData);
            }
            catch (Exception ex)
            {
                LogToDebugConsole("Extraction operation was successfully completed.. - " + ex.Message);
            }

        }

        private static void LogToDebugConsole(string informationToLog)
        {
            Debug.WriteLine(informationToLog);
        }

        #endregion
    }
}
