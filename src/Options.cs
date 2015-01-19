using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.OpenCommandLine
{
    class Options : DialogPage
    {
        [DisplayName("Command")]
        [Description("The command or filepath to an executable such as cmd.exe")]
        [DefaultValue("cmd")]
        public string Command { get; set; }

        [DisplayName("Command arguments")]
        [Description("Any arguments to pass to the command.")]
        [DefaultValue("")]
        public string Arguments { get; set; }

        public override void LoadSettingsFromStorage()
        {
            base.LoadSettingsFromStorage();

            if (string.IsNullOrEmpty(Command))
                Command = "cmd";
        }
    }
}
