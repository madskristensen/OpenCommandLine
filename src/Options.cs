using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.OpenCommandLine
{
    class Options : DialogPage
    {
        public static Dictionary<string, Command> DefaultPresets = new Dictionary<string, Command>();

        string _preset;

        [Category("Command Preset")]
        [DisplayName("Select preset")]
        [Description("Select one of the predefined command configurations")]
        [DefaultValue("cmd")]
        [TypeConverter(typeof(CommandTypeConverter))]
        public string Preset
        {
            get { return _preset; }
            set
            {
                _preset = value;
                if (DefaultPresets.ContainsKey(value))
                {
                    Command command = DefaultPresets[value];
                    this.Command = command.Name;
                    this.Arguments = command.Arguments;
                }
            }
        }

        [Category("Console")]
        [DisplayName("Command")]
        [Description("The command or filepath to an executable such as cmd.exe")]
        [DefaultValue("cmd.exe")]
        public string Command { get; set; }


        [Category("Console")]
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

            if (string.IsNullOrEmpty(Preset))
                Preset = "cmd";

            if (DefaultPresets.Count == 0)
            {
                string installDir = VsHelpers.GetInstallDirectory(ServiceProvider.GlobalProvider);
                string devPromptFile = Path.Combine(installDir, @"..\Tools\VsDevCmd.bat");

                DefaultPresets["cmd"] = new Command("cmd.exe");
                DefaultPresets["Dev Cmd Promt"] = new Command("cmd.exe", "/k \"" + devPromptFile + "\"");
                DefaultPresets["PowerShell"] = new Command("powershell.exe", "-ExecutionPolicy Bypass -NoExit");
                DefaultPresets["posh-git"] = new Command("powershell.exe", @"-ExecutionPolicy Bypass -NoExit -Command .(Resolve-Path ""$env:LOCALAPPDATA\GitHub\shell.ps1""); .(Resolve-Path ""$env:github_posh_git\profile.example.ps1"")");
                DefaultPresets["Custom"] = new Command("", "");
            }
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
            return new StandardValuesCollection(Options.DefaultPresets.Keys);
        }
    }

    class Command
    {
        public Command(string command, string arguments = "")
        {
            this.Name = command;
            this.Arguments = arguments;
        }
        public string Name { get; set; }
        public string Arguments { get; set; }
    }
}
