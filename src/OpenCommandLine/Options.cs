using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.OpenCommandLine
{
    internal class Options : UIElementDialogPage
    {
        private OptionsDialogPageControl _optionsDialogControl;
        private IStoredSettingsProvider _storedSettingsProvider;

        protected override UIElement Child => _optionsDialogControl ?? (_optionsDialogControl = new OptionsDialogPageControl(GetDefaultPresets()));

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);

            _storedSettingsProvider = GetSettingProvider();

            _optionsDialogControl.CustomActions.Clear();
            for (int i = 0; i < _storedSettingsProvider.CustomActionsLength; i++)
            {
                _optionsDialogControl.CustomActions.Add(_storedSettingsProvider.GetCustomAction(i));
            }

            _optionsDialogControl.Preset = _storedSettingsProvider.Preset;
            _optionsDialogControl.FriendlyName = _storedSettingsProvider.FriendlyName;
            _optionsDialogControl.Command = _storedSettingsProvider.Command;
            _optionsDialogControl.Arguments = _storedSettingsProvider.Arguments;

            _optionsDialogControl.OpenSlnLevelCheckBox.IsChecked = _storedSettingsProvider.OpenSlnLevel;
            _optionsDialogControl.OpenProjLevelCheckBox.IsChecked = _storedSettingsProvider.OpenProjectLevel;
        }

        protected override void OnApply(PageApplyEventArgs args)
        {
            if (args.ApplyBehavior == ApplyKind.Apply)
            {
                _storedSettingsProvider.Preset = _optionsDialogControl.Preset;
                _storedSettingsProvider.FriendlyName = _optionsDialogControl.FriendlyName;
                _storedSettingsProvider.Command = _optionsDialogControl.Command;
                _storedSettingsProvider.Arguments = _optionsDialogControl.Arguments;

                _storedSettingsProvider.SetCustomActions(_optionsDialogControl.CustomActions.ToArray());

                _storedSettingsProvider.OpenSlnLevel = _optionsDialogControl.OpenSlnLevelCheckBox.IsChecked.GetValueOrDefault(false);
                _storedSettingsProvider.OpenProjectLevel = _optionsDialogControl.OpenProjLevelCheckBox.IsChecked.GetValueOrDefault(false);
            }

            base.OnApply(args);
        }

        private IStoredSettingsProvider GetSettingProvider()
        {
            var componentModel = (IComponentModel)Site.GetService(typeof(SComponentModel));
            return componentModel.DefaultExportProvider.GetExportedValue<IStoredSettingsProvider>();
        }

        private IDictionary<string, Command> GetDefaultPresets()
        {
            string installDir = VsHelpers.GetInstallDirectory(ServiceProvider.GlobalProvider);
            string devPromptFile = Path.Combine(installDir, @"..\Tools\VsDevCmd.bat");

            var defaultPresets = new Dictionary<string, Command>();

            defaultPresets["cmd"] = new Command("cmd.exe");
            defaultPresets["Dev Cmd Prompt"] = new Command("cmd.exe", "/k \"" + devPromptFile + "\"");
            defaultPresets["PowerShell"] = new Command("powershell.exe", "-ExecutionPolicy Bypass -NoExit");
            defaultPresets["PowerShell ISE"] = new Command("powershell_ise.exe");
            defaultPresets["posh-git"] = new Command("powershell.exe", @"-ExecutionPolicy Bypass -NoExit -Command .(Resolve-Path ""$env:LOCALAPPDATA\GitHub\shell.ps1""); .(Resolve-Path ""$env:github_posh_git\profile.example.ps1"")");
            defaultPresets["Git Bash"] = new Command(@"C:\Program Files\Git\git-bash.exe");
            defaultPresets["Babun"] = new Command(@"%UserProfile%\.babun\cygwin\bin\mintty.exe", "/bin/env CHERE_INVOKING=1 /bin/zsh.exe");

            string gitHubForWindowsPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), "GitHub", "GitHub.appref-ms");
            if (File.Exists(gitHubForWindowsPath))
            {
                defaultPresets["GitHub Console"] = new Command(@"%LOCALAPPDATA%\GitHub\GitHub.appref-ms", "-open-shell");
            }

            defaultPresets["cmder"] = new Command("cmder.exe", "/START \"%folder%\"");
            defaultPresets["ConEmu"] = new Command("ConEmu64.exe", "/cmd PowerShell.exe");
            defaultPresets["Custom"] = new Command(string.Empty, string.Empty);

            return defaultPresets;
        }

        public class Command
        {
            public Command(string command, string arguments = "")
            {
                CommandName = command;
                Arguments = arguments;
            }
            public string CommandName { get; set; }
            public string Arguments { get; set; }
        }
    }
}
