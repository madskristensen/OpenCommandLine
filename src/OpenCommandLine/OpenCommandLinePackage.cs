using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.OpenCommandLine
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(Options), "Environment", "Command Line", 101, 104, true, new[] { "cmd", "powershell", "bash" }, ProvidesLocalizedCategoryName = false)]
    [ProvideUIContextRule(PackageGuids.guidBatFileRuleString,
        name: "Supported Files",
        expression: "scripts",
        termNames: new[] { "scripts" },
        termValues: new[] { "HierSingleSelectionName:.(cmd|bat|ps1)$" })]
    [Guid(PackageGuids.guidOpenCommandLinePkgString)]
    public sealed class OpenCommandLinePackage : AsyncPackage
    {
        private static DTE2 _dte;

        /// <summary>
        /// Gets the singleton instance of the package.
        /// </summary>
        public static OpenCommandLinePackage Instance { get; private set; }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Instance = this;

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            _dte = await GetServiceAsync(typeof(DTE)) as DTE2;
            var mcs = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Assumes.Present(mcs);

            var cmdCustom = new CommandID(PackageGuids.guidOpenCommandLineCmdSet, PackageIds.cmdidOpenCommandLine);
            var customItem = new OleMenuCommand(OpenCustom, cmdCustom);
            customItem.BeforeQueryStatus += BeforeQueryStatus;
            mcs.AddCommand(customItem);

            var cmdCmd = new CommandID(PackageGuids.guidOpenCommandLineCmdSet, PackageIds.cmdidOpenCmd);
            var cmdItem = new MenuCommand(OpenCmd, cmdCmd);
            mcs.AddCommand(cmdItem);

            var cmdPowershell = new CommandID(PackageGuids.guidOpenCommandLineCmdSet, PackageIds.cmdidOpenPowershell);
            var powershellItem = new MenuCommand(OpenPowershell, cmdPowershell);
            mcs.AddCommand(powershellItem);

            var cmdOptions = new CommandID(PackageGuids.guidOpenCommandLineCmdSet, PackageIds.cmdidOpenOptions);
            var optionsItem = new MenuCommand((s, e) => { ShowOptionPage(typeof(Options)); }, cmdOptions);
            mcs.AddCommand(optionsItem);

            var cmdExe = new CommandID(PackageGuids.guidOpenCommandLineCmdSet, PackageIds.cmdExecuteCmd);
            var exeItem = new OleMenuCommand(ExecuteFile, cmdExe)
            {
                Supported = false
            };
            mcs.AddCommand(exeItem);
        }

        private void ExecuteFile(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ProjectItem item = VsHelpers.GetProjectItem(_dte);
            string path = item.FileNames[1];
            string folder = Path.GetDirectoryName(path);

            string ext = Path.GetExtension(path);

            if (!string.IsNullOrEmpty(ext) && ext.ToLower() == ".ps1")
            {
                CommandLineLauncher.StartProcess(folder, "powershell.exe", "-ExecutionPolicy Bypass -NoExit -File \"" + Path.GetFileName(path) + "\"");
            }
            else
            {
                CommandLineLauncher.StartProcess(folder, "cmd.exe", "/k \"" + Path.GetFileName(path) + "\"");
            }
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            var options = GetDialogPage(typeof(Options)) as Options;

            button.Text = options.FriendlyName;
        }

        private void OpenCustom(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var options = GetDialogPage(typeof(Options)) as Options;
            string folder = VsHelpers.GetFolderPath(options, _dte);
            string arguments = (options.Arguments ?? string.Empty).Replace("%folder%", folder);

            string confName = VsHelpers.GetSolutionConfigurationName(_dte);
            arguments = arguments.Replace("%configuration%", confName);

            string confPlatform = VsHelpers.GetSolutionConfigurationPlatformName(_dte);
            arguments = arguments.Replace("%platform%", confPlatform);

            CommandLineLauncher.StartProcess(folder, options.Command, arguments);
        }

        private void OpenCmd(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string installDir = VsHelpers.GetInstallDirectory();
            string devPromptFile = Path.Combine(installDir, @"..\Tools\VsDevCmd.bat");

            SetupProcess("cmd.exe", "/k \"" + devPromptFile + "\"");
        }

        private void OpenPowershell(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            SetupProcess("powershell.exe", "-ExecutionPolicy Bypass -NoExit");
        }

        private void SetupProcess(string command, string arguments)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var options = GetDialogPage(typeof(Options)) as Options;
            string folder = VsHelpers.GetFolderPath(options, _dte);

            CommandLineLauncher.StartProcess(folder, command, arguments);
        }
    }
}
