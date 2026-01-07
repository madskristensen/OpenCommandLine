using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace MadsKristensen.OpenCommandLine
{
    internal class Options : DialogPage
    {
        private static Dictionary<string, Command> _defaultPresets;
        private bool _isLoading, _isChanging;
        private string _preset;

        public static Dictionary<string, Command> DefaultPresets
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (_defaultPresets == null)
                {
                    InitializePresets();
                }
                return _defaultPresets;
            }
        }

        private static void InitializePresets()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string installDir = VsHelpers.GetInstallDirectory();
            string devPromptFile = installDir != null 
                ? Path.Combine(installDir, @"..\Tools\VsDevCmd.bat") 
                : "";
            string launchDevShellPs1 = installDir != null
                ? Path.Combine(installDir, @"..\Tools\Launch-VsDevShell.ps1")
                : "";

            _defaultPresets = new Dictionary<string, Command>
            {
                ["cmd"] = new Command("cmd.exe"),
                ["Dev Cmd Prompt"] = new Command("cmd.exe", "/k \"" + devPromptFile + "\""),
                ["Dev PowerShell"] = new Command("powershell.exe", "-ExecutionPolicy Bypass -NoExit -File \"" + launchDevShellPs1 + "\""),
                ["PowerShellCore"] = new Command("pwsh.exe", "-ExecutionPolicy Bypass -NoExit"),
                ["PowerShell"] = new Command("powershell.exe", "-ExecutionPolicy Bypass -NoExit"),
                ["PowerShell ISE"] = new Command("powershell_ise.exe"),
                ["Nushell"] = new Command("nu.exe"),
                ["posh-git"] = new Command("powershell.exe", @"-ExecutionPolicy Bypass -NoExit -Command .(Resolve-Path ""$env:LOCALAPPDATA\GitHub\shell.ps1""); .(Resolve-Path ""$env:github_posh_git\profile.example.ps1"")"),
                ["Git Bash"] = new Command(@"C:\Program Files\Git\git-bash.exe"),
                ["Babun"] = new Command(@"%UserProfile%\.babun\cygwin\bin\mintty.exe", "/bin/env CHERE_INVOKING=1 /bin/zsh.exe"),
                ["cmder"] = new Command("cmder.exe", "/START \"%folder%\""),
                ["ConEmu"] = new Command("ConEmu64.exe", "/cmd PowerShell.exe"),
                ["Windows Terminal"] = new Command("wt.exe", "-d \"%folder%\""),
                ["Custom"] = new Command(string.Empty, string.Empty)
            };

            string GitHubForWindowsPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData") ?? "", "GitHub", "GitHub.appref-ms");
            if (File.Exists(GitHubForWindowsPath))
            {
                _defaultPresets["GitHub Console"] = new Command(@"%LOCALAPPDATA%\GitHub\GitHub.appref-ms", "-open-shell");
            }
        }


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
                // Only update Command/Arguments when user explicitly changes preset (not during load)
                if (!_isLoading && !_isChanging && _preset != value)
                {
                    _preset = value;

                    // Only access DefaultPresets on UI thread since it requires VS services
                    if (ThreadHelper.CheckAccess() && DefaultPresets.ContainsKey(value))
                    {
                        _isChanging = true;

                        Command command = DefaultPresets[value];
                        Command = command.Name;
                        Arguments = command.Arguments;
                        FriendlyName = "Default (" + value + ")";

                        _isChanging = false;
                    }
                }
                else
                {
                    _preset = value;
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
                if (_command != value)
                {
                    _command = value;
                }
            }
        }

        private string _arguments;

        [Category("Console")]
        [DisplayName("Command arguments")]
        [Description(@"Any arguments to pass to the command.
%folder% parameter pass to argument current file path.
%configuration% parameter pass to argument current build configuration.
%platform% parameter pass to argument current build platform.")]
        [DefaultValue("")]
        public string Arguments
        {
            get => _arguments;
            set
            {
                if (_arguments != value)
                {
                    _arguments = value;
                }
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

        [Category("Settings")]
        [DisplayName("Open at Git repository root")]
        [Description("Always open command prompt at the Git repository root folder.")]
        [DefaultValue(false)]
        public bool OpenGitRepoLevel { get; set; }

        [Category("Settings")]
        [DisplayName("Run as Administrator")]
        [Description("Open the command prompt with elevated (Administrator) privileges.")]
        [DefaultValue(false)]
        public bool RunAsAdministrator { get; set; }

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

            _isLoading = false;
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
            // Must be called on UI thread to access DefaultPresets
            if (!ThreadHelper.CheckAccess())
                return true; // Allow any value if not on UI thread
            
            ThreadHelper.ThrowIfNotOnUIThread();
            return Options.DefaultPresets.ContainsKey(value.ToString());
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            // Must be called on UI thread to access DefaultPresets
            if (!ThreadHelper.CheckAccess())
                return new StandardValuesCollection(new string[] { "Custom" });
            
            ThreadHelper.ThrowIfNotOnUIThread();
            return new StandardValuesCollection(Options.DefaultPresets.Keys);
        }
    }

    /// <summary>
    /// Represents a command preset configuration.
    /// </summary>
    internal readonly struct Command
    {
        public Command(string name, string arguments = "")
        {
            Name = name;
            Arguments = arguments;
        }

        public string Name { get; }
        public string Arguments { get; }
    }
}
