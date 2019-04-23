﻿using System.Configuration;
using HL7Fuse.Protocol;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapiTools.Base.Util;
using SuperSocket.SocketBase;

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
        

       

        #endregion
    }
}
