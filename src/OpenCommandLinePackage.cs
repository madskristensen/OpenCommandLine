using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MadsKristensen.OpenCommandLine
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideOptionPage(typeof(Options), "Environment", "Command Line", 101, 104, true, new[] { "cmd", "powershell", "bash" })]
    [ProvideOptionPage(typeof(Options), "Environment", "Command Line", 201, 204, true, new[] { "cmd", "powershell", "bash" })]
    [Guid(GuidList.guidOpenCommandLinePkgString)]
    public sealed class OpenCommandLinePackage : Package
    {
        public const string Version = "1.4";
        private static DTE2 _dte;

        protected override void Initialize()
        {
            base.Initialize();
            _dte = GetService(typeof(DTE)) as DTE2;

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            CommandID cmdCustom = new CommandID(GuidList.guidOpenCommandLineCmdSet, (int)PkgCmdIDList.cmdidOpenCommandLine);
            OleMenuCommand customItem = new OleMenuCommand(OpenCustom, cmdCustom);
            customItem.BeforeQueryStatus += BeforeQueryStatus;
            mcs.AddCommand(customItem);

            CommandID cmdCmd = new CommandID(GuidList.guidOpenCommandLineCmdSet, (int)PkgCmdIDList.cmdidOpenCmd);
            MenuCommand cmdItem = new MenuCommand(OpenCmd, cmdCmd);
            mcs.AddCommand(cmdItem);

            CommandID cmdPowershell = new CommandID(GuidList.guidOpenCommandLineCmdSet, (int)PkgCmdIDList.cmdidOpenPowershell);
            MenuCommand powershellItem = new MenuCommand(OpenPowershell, cmdPowershell);
            mcs.AddCommand(powershellItem);

            CommandID cmdOptions = new CommandID(GuidList.guidOpenCommandLineCmdSet, (int)PkgCmdIDList.cmdidOpenOptions);
            MenuCommand optionsItem = new MenuCommand((s, e) => { ShowOptionPage(typeof(Options)); }, cmdOptions);
            mcs.AddCommand(optionsItem);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            Options options = GetDialogPage(typeof(Options)) as Options;

            int index = options.Command.LastIndexOf('/') + 1;

            button.Text = "Default (" + options.Command.Substring(index) + ")";
        }

        private void OpenCustom(object sender, EventArgs e)
        {
            Options options = GetDialogPage(typeof(Options)) as Options;

            if (IsCustomSameAsCmd(options))
            {
                OpenCmd(sender, e);
                return;
            }

            string folder = VsHelpers.GetFolderPath(options, _dte);

            string arguments = options.Arguments;

            if (!string.IsNullOrWhiteSpace(options.FolderPathReplacementToken))
            {
                arguments = arguments.Replace(options.FolderPathReplacementToken, folder);
            }

            StartProcess(folder, options.Command, arguments);
        }

        private void OpenCmd(object sender, EventArgs e)
        {
            Options options = GetDialogPage(typeof(Options)) as Options;
            string folder = VsHelpers.GetFolderPath(options, _dte);

            string installDir = VsHelpers.GetInstallDirectory(this);
            string devPromptFile = Path.Combine(installDir, @"..\Tools\VsDevCmd.bat");

            StartProcess(folder, "cmd.exe", "/k \"" + devPromptFile + "\"");
        }

        private void OpenPowershell(object sender, EventArgs e)
        {
            Options options = GetDialogPage(typeof(Options)) as Options;
            string folder = VsHelpers.GetFolderPath(options, _dte);

            StartProcess(folder, "powershell.exe", "");
        }

        private static void StartProcess(string workingDirectory, string command, string arguments)
        {
            command = Environment.ExpandEnvironmentVariables(command);
            arguments = Environment.ExpandEnvironmentVariables(arguments);

            ProcessStartInfo start = new ProcessStartInfo(command, arguments);
            start.WorkingDirectory = workingDirectory;

            System.Diagnostics.Process.Start(start);
        }

        private static bool IsCustomSameAsCmd(Options options)
        {
            bool nameMatch = options.Command.Equals("cmd", StringComparison.OrdinalIgnoreCase) || options.Command.Equals("cmd.exe", StringComparison.OrdinalIgnoreCase);

            return nameMatch && string.IsNullOrWhiteSpace(options.Arguments);
        }
    }
}
