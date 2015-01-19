using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.OpenCommandLine
{
    class Options : DialogPage
    {
        [DisplayName("Command")]
        [Description("The command or filepath to an executable such as cmd.exe")]
        [DefaultValue("cmd")]
        [TypeConverter(typeof(CommandTypeConverter))]
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

    class CommandTypeConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new[] { "cmd", "PowerShell" });
        }
    }
}
