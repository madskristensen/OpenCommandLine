using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace MadsKristensen.OpenCommandLine
{
    internal class Options : DialogPage
    {
        public static Dictionary<string, Command> DefaultPresets = new Dictionary<string, Command>();
        private bool _isLoading, _isChanging;
        private string _preset;

        [Category("Command Preset")]
        [DisplayName("Select preset")]
        [Description("Select one of the predefined command configurations")]
        [DefaultValue("cmd")]
        [TypeConverter(typeof(CommandTypeConverter))]
        public string Preset
        {
            get => _preset;
            set
            {
                _preset = value;

                if (!_isLoading && DefaultPresets.ContainsKey(value))
                {
                    _isChanging = true;

                    Command command = DefaultPresets[value];
                    Command = command.Name;
                    Arguments = command.Arguments;
                    FriendlyName = "Default (" + value + ")";

                    _isChanging = false;
                }
            }
        }

        [Category("Command Preset")]
        [DisplayName("Friendly name")]
        [Description("Specify the friendly name for the default command")]
        [DefaultValue("Default (cmd)")]
        public string FriendlyName { get; set; }

        private string _command;

        [Category("Console")]
        [DisplayName("Command")]
        [Description("The command or filepath to an executable such as cmd.exe")]
        [DefaultValue("cmd.exe")]
        public string Command
        {
            get => _command;
            set
            {
                _command = value;
                SetCustom();
            }
        }

        private string _arguments;

        [Category("Console")]
        [DisplayName("Command arguments")]
        [Description(@"Any arguments to pass to the command.\n
%folder% parameter pass to argument current file path.\n
%configuration% parameter pass to argument current build configuration.\n
%platform% parameter pass to argument current build platform.")]
        [DefaultValue("")]
        public string Arguments
        {
            get => _arguments;
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

        [Category("Settings")]
        [DisplayName("Open files at project level")]
        [Description("Opening a command line when a document is active will open it at the project level.")]
        [DefaultValue(false)]
        public bool OpenProjectLevel { get; set; }

        public override void LoadSettingsFromStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _isLoading = true;

            base.LoadSettingsFromStorage();

            if (string.IsNullOrEmpty(Command))
            {
                Command = "cmd.exe";
            }

            if (string.IsNullOrEmpty(FriendlyName))
            {
                FriendlyName = "Default (cmd)";
            }

            if (string.IsNullOrEmpty(Preset))
            {
                Preset = "cmd";
            }

            if (DefaultPresets.Count == 0)
            {
                string installDir = VsHelpers.GetInstallDirectory();
                string devPromptFile = Path.Combine(installDir, @"..\Tools\VsDevCmd.bat");

                DefaultPresets["cmd"] = new Command("cmd.exe");
                DefaultPresets["Dev Cmd Prompt"] = new Command("cmd.exe", "/k \"" + devPromptFile + "\"");
                DefaultPresets["PowerShellCore"] = new Command("pwsh.exe", "-ExecutionPolicy Bypass -NoExit");
                DefaultPresets["PowerShell"] = new Command("powershell.exe", "-ExecutionPolicy Bypass -NoExit");
                DefaultPresets["PowerShell ISE"] = new Command("powershell_ise.exe");
                DefaultPresets["posh-git"] = new Command("powershell.exe", @"-ExecutionPolicy Bypass -NoExit -Command .(Resolve-Path ""$env:LOCALAPPDATA\GitHub\shell.ps1""); .(Resolve-Path ""$env:github_posh_git\profile.example.ps1"")");
                DefaultPresets["Git Bash"] = new Command(@"C:\Program Files\Git\git-bash.exe");
                DefaultPresets["Babun"] = new Command(@"%UserProfile%\.babun\cygwin\bin\mintty.exe", "/bin/env CHERE_INVOKING=1 /bin/zsh.exe");

                string GitHubForWindowsPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), "GitHub", "GitHub.appref-ms");
                if (File.Exists(GitHubForWindowsPath))
                {
                    DefaultPresets["GitHub Console"] = new Command(@"%LOCALAPPDATA%\GitHub\GitHub.appref-ms", "-open-shell");
                }

                DefaultPresets["cmder"] = new Command("cmder.exe", "/START \"%folder%\"");
                DefaultPresets["ConEmu"] = new Command("ConEmu64.exe", "/cmd PowerShell.exe");
                DefaultPresets["Windows Terminal"] = new Command("wt", "/d \"%folder%\"");
                DefaultPresets["Custom"] = new Command(string.Empty, string.Empty);
            }

            _isLoading = false;
        }

        private void SetCustom()
        {
            if (!_isChanging && !_isLoading)
            {
                _preset = "Custom";
            }
        }
    }

    internal class CommandTypeConverter : StringConverter
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

    internal class Command
    {
        public Command(string command, string arguments = "")
        {
            Name = command;
            Arguments = arguments;
        }
        public string Name { get; set; }
        public string Arguments { get; set; }
    }
}
