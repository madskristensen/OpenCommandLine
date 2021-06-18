using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.OpenCommandLine
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Version, Vsix.Version)]
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
        public Package Instance;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
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
                StartProcess(folder, "powershell.exe", "-ExecutionPolicy Bypass -NoExit -File \"" + Path.GetFileName(path) + "\"");
            }
            else
            {
                var options = GetDialogPage(typeof(Options)) as Options;
                string arguments = (options.Arguments ?? string.Empty).Replace("%folder%", folder);

                string confName = VsHelpers.GetSolutionConfigurationName(_dte);
                arguments = arguments.Replace("%configuration%", confName);

                string confPlatform = VsHelpers.GetSolutionConfigurationPlatformName(_dte);
                arguments = arguments.Replace("%platform%", confPlatform);

                arguments = arguments.Replace("%file%", Path.GetFileName(path));

                StartProcess(folder, options.Command, arguments);
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

            arguments = arguments.Replace("%file%", string.Empty);

            StartProcess(folder, options.Command, arguments);
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

            StartProcess(folder, command, arguments);
        }

        private static void StartProcess(string workingDirectory, string command, string arguments)
        {
            try
            {
                command = Environment.ExpandEnvironmentVariables(command ?? string.Empty);
                arguments = Environment.ExpandEnvironmentVariables(arguments ?? string.Empty);

                var start = new ProcessStartInfo(command, arguments)
                {
                    WorkingDirectory = workingDirectory,
                    LoadUserProfile = true,
                    UseShellExecute = false
                };

                ModifyPathVariable(start);

                using (System.Diagnostics.Process.Start(start))
                {
                    // Makes sure the process handle is disposed
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
        }

        private static void ModifyPathVariable(ProcessStartInfo start)
        {
            string path = ".\\node_modules\\.bin" + ";" + start.EnvironmentVariables["PATH"];

            var process = System.Diagnostics.Process.GetCurrentProcess();
            string ideDir = Path.GetDirectoryName(process.MainModule.FileName);

            if (Directory.Exists(ideDir))
            {
                string parent = Directory.GetParent(ideDir).Parent.FullName;

                string rc2Preview1Path = new DirectoryInfo(Path.Combine(parent, @"Web\External")).FullName;

                if (Directory.Exists(rc2Preview1Path))
                {
                    path += ";" + rc2Preview1Path;
                    path += ";" + rc2Preview1Path + "\\git";
                }
                else
                {
                    path += ";" + Path.Combine(ideDir, @"Extensions\Microsoft\Web Tools\External");
                    path += ";" + Path.Combine(ideDir, @"Extensions\Microsoft\Web Tools\External\git");
                }
            }

            start.EnvironmentVariables["PATH"] = path;
        }
    }
}
