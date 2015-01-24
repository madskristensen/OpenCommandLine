using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.OpenCommandLine
{
    class Options : DialogPage
    {
        [Category("Command")]
        [DisplayName("Command")]
        [Description("The command or filepath to an executable such as cmd.exe")]
        [DefaultValue("cmd.exe")]
        [TypeConverter(typeof(CommandTypeConverter))]
        public string Command { get; set; }

        [Category("Command")]
        [DisplayName("Command arguments")]
        [Description("Any arguments to pass to the command.")]
        [DefaultValue("")]
        public string Arguments { get; set; }

        [Category("Settings")]
        [DisplayName("Folder path replacement token")]
        [Description("If not empty, the token will be replaced verbatim in the command line. Example: $FolderPath$")]
        [DefaultValue("$FolderPath$")]
        public string FolderPathReplacementToken { get; set; }

        [Category("Settings")]
		[DisplayName("Always open at solution level")]
		[Description("Always open command prompt at the solution level.")]
		[DefaultValue(false)]
		public bool OpenSlnLevel { get; set; }

        public override void LoadSettingsFromStorage()
        {
            base.LoadSettingsFromStorage();

            if (string.IsNullOrEmpty(Command))
                Command = "cmd.exe";
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
            return new StandardValuesCollection(new[] { "cmd.exe", "powershell.exe" });
        }
    }
}
