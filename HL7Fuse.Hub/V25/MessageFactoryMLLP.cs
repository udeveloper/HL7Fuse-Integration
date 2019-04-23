using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HL7Fuse.Protocol;
using NHapi.Base.Model;
using NHapi.Model.V25.Message;
using NHapi.Model.V25.Datatype;
using NHapi.Base.Parser;
using HL7Fuse.Helpers;
using System.Diagnostics;
using System.IO;

namespace HL7Fuse.Hub.V25
{
    /// <summary>
    /// Message factory
    /// </summary>
    public class MessageFactoryMLLP : MessageFactoryBaseMLLP
    {
        #region Public properties
        public override string Name
        {
            get { return "V25.MessageFactoryMLLP"; }
        }

        public override void ExecuteCommand(MLLPSession session, HL7RequestInfo requestInfo)
        {
            Logging.Logger.Info(new PipeParser().Encode(requestInfo.Message));

            if (requestInfo.Message is ORU_R01)
            {
                if (!GetBinaryDataMessageHL7(requestInfo.Message))
                    requestInfo.ErrorMessage = "";

            }

            base.ExecuteCommand(session, requestInfo);
            
        }

        private bool GetBinaryDataMessageHL7(IMessage message)
        {
            var processValid = false;
            var messageHL7Parsed = message;

            if (messageHL7Parsed is  ORU_R01)
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
