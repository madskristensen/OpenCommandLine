using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.OpenCommandLine
{
    class Options : DialogPage
    {
        public static Dictionary<string, Command> DefaultPresets = new Dictionary<string, Command>();
        private bool _isLoading, _isChanging;

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

                if (!_isLoading && DefaultPresets.ContainsKey(value))
                {
                    _isChanging = true;
                    Command command = DefaultPresets[value];
                    this.Command = command.Name;
                    this.Arguments = command.Arguments;
                    _isChanging = false;
                }
            }
        }

        string _command;

        [Category("Console")]
        [DisplayName("Command")]
        [Description("The command or filepath to an executable such as cmd.exe")]
        [DefaultValue("cmd.exe")]
        public string Command
        {
            get { return _command; }
            set
            {
                _command = value;
                SetCustom();
            }
        }

        string _arguments;

        [Category("Console")]
        [DisplayName("Command arguments")]
        [Description("Any arguments to pass to the command.\n%folder% parameter pass to argument current file path.")]
        [DefaultValue("")]
        public string Arguments
        {
            get { return _arguments; }
            set
            {
                _arguments = value;
                SetCustom();
            }
        }

        [Category("Settings")]
        [DisplayName("Always open at solution level")]
        [Description("Always open command prompt at the solution level.")]
        [DefaultValue(false)]
        public bool OpenSlnLevel { get; set; }

        public override void LoadSettingsFromStorage()
        {
            _isLoading = true;

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
                DefaultPresets["Dev Cmd Prompt"] = new Command("cmd.exe", "/k \"" + devPromptFile + "\"");
                DefaultPresets["PowerShell"] = new Command("powershell.exe", "-ExecutionPolicy Bypass -NoExit");
                DefaultPresets["posh-git"] = new Command("powershell.exe", @"-ExecutionPolicy Bypass -NoExit -Command .(Resolve-Path ""$env:LOCALAPPDATA\GitHub\shell.ps1""); .(Resolve-Path ""$env:github_posh_git\profile.example.ps1"")");

                string GitHubForWindowsPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), "GitHub", "GitHub.appref-ms");
                if (File.Exists(GitHubForWindowsPath))
                {
                    DefaultPresets["GitHub Console"] = new Command(@"%LOCALAPPDATA%\GitHub\GitHub.appref-ms", "-open-shell");
                }

                DefaultPresets["cmder"] = new Command("cmder.exe", "/START \"%folder%\"");
                DefaultPresets["Custom"] = new Command(string.Empty, string.Empty);
            }

            _isLoading = false;
        }

        private void SetCustom()
        {
            if (!_isChanging && !_isLoading)
                _preset = "Custom";
        }
    }

    class CommandTypeConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            return Options.DefaultPresets.ContainsKey(value.ToString());
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
