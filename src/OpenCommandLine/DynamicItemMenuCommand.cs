using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.OpenCommandLine
{
    public class DynamicItemMenuCommand : OleMenuCommand
    {
        private readonly Func<int> _getCapacity;

        public DynamicItemMenuCommand(EventHandler invokeHandler, CommandID rootId, Func<int> getCapacity) : base(invokeHandler, rootId)
        {
            _getCapacity = getCapacity;
        }

        public override bool DynamicItemMatch(int cmdId)
        {
            if (cmdId >= CommandID.ID && cmdId - CommandID.ID < _getCapacity())
            {
                MatchedCommandId = cmdId;
                return true;
            }

            MatchedCommandId = 0;
            return false;
        }
    }
}
