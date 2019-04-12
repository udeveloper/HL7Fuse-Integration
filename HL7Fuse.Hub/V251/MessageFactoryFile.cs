﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HL7Fuse.Hub.V251
{
    /// <summary>
    /// Message factory
    /// </summary>
    public class MessageFactoryFile : MessageFactoryBaseFile
    {
        #region Public properties
        public override string Name
        {
            get { return "V251.MessageFactoryFile"; }
        }
        #endregion
    }
}
