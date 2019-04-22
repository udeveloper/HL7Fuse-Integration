using HL7Fuse.Protocol;
using SuperSocket.SocketBase.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HL7Fuse.Hub.V25
{
    public class ORU_R01MLLP : ICommand<MLLPSession, HL7RequestInfo>
    {
        public string Name
        {
            get { return "V25.ORU_R01MLLP"; }
        }

        public void ExecuteCommand(MLLPSession session, HL7RequestInfo requestInfo)
        {
            throw new NotImplementedException();
        }
    }
}
