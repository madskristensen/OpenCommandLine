using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MadsKristensen.OpenCommandLine
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionHasSingleProject)]
    [ProvideAutoLoad(UIContextGuids80.SolutionHasMultipleProjects)]
    [ProvideOptionPage(typeof(Options), "Environment", "Command Line", 101, 104, true, new[] { "cmd", "powershell", "bash" }, ProvidesLocalizedCategoryName = false)]
    [Guid(PackageGuids.guidOpenCommandLinePkgString)]
    public sealed class OpenCommandLinePackage : Package
    {
        private static DTE2 _dte;
        public Package Instance;

        protected override void Initialize()
        {
            _dte = GetService(typeof(DTE)) as DTE2;

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            CommandID cmdCustom = new CommandID(PackageGuids.guidOpenCommandLineCmdSet, PackageIds.cmdidOpenCommandLine);
            OleMenuCommand customItem = new OleMenuCommand(OpenCustom, cmdCustom);
            customItem.BeforeQueryStatus += BeforeQueryStatus;
            mcs.AddCommand(customItem);

            CommandID cmdCmd = new CommandID(PackageGuids.guidOpenCommandLineCmdSet, PackageIds.cmdidOpenCmd);
            MenuCommand cmdItem = new MenuCommand(OpenCmd, cmdCmd);
            mcs.AddCommand(cmdItem);

            CommandID cmdPowershell = new CommandID(PackageGuids.guidOpenCommandLineCmdSet, PackageIds.cmdidOpenPowershell);
            MenuCommand powershellItem = new MenuCommand(OpenPowershell, cmdPowershell);
            mcs.AddCommand(powershellItem);

            CommandID cmdOptions = new CommandID(PackageGuids.guidOpenCommandLineCmdSet, PackageIds.cmdidOpenOptions);
            MenuCommand optionsItem = new MenuCommand((s, e) => { ShowOptionPage(typeof(Options)); }, cmdOptions);
            mcs.AddCommand(optionsItem);

            CommandID cmdExe = new CommandID(PackageGuids.guidOpenCommandLineCmdSet, PackageIds.cmdExecuteCmd);
            OleMenuCommand exeItem = new OleMenuCommand(ExecuteFile, cmdExe);
            exeItem.BeforeQueryStatus += BeforeExeQuery;
            mcs.AddCommand(exeItem);
        }

        void BeforeExeQuery(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            button.Enabled = button.Visible = false;
            var item = VsHelpers.GetProjectItem(_dte);

            if (item == null || item.FileCount == 0)
                return;

            string path = item.FileNames[1];

            if (!VsHelpers.IsValidFileName(path))
                return;

            string[] allowed = { ".CMD", ".BAT", ".PS1" };
            string ext = Path.GetExtension(path);
            bool isEnabled = allowed.Contains(ext, StringComparer.OrdinalIgnoreCase) && File.Exists(path);

            button.Enabled = button.Visible = isEnabled;
        }

        private void ExecuteFile(object sender, EventArgs e)
        {
            var item = VsHelpers.GetProjectItem(_dte);
            string path = item.FileNames[1];
            string folder = Path.GetDirectoryName(path);

            string ext = Path.GetExtension(path);

            if (!string.IsNullOrEmpty(ext) && ext.ToLower() == ".ps1")
            {
                StartProcess(folder, "powershell.exe", "-ExecutionPolicy Bypass -NoExit -File \"" + Path.GetFileName(path) + "\"");
            }
            else
            {
                StartProcess(folder, "cmd.exe", "/k \"" + Path.GetFileName(path) + "\"");
            }
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand button = (OleMenuCommand)sender;
            Options options = GetDialogPage(typeof(Options)) as Options;

            button.Text = options.FriendlyName;
        }

        private void OpenCustom(object sender, EventArgs e)
        {
            Options options = GetDialogPage(typeof(Options)) as Options;
            string folder = VsHelpers.GetFolderPath(options, _dte);
            string arguments = (options.Arguments ?? string.Empty).Replace("%folder%", folder);

            string confName = VsHelpers.GetSolutionConfigurationName(_dte);
            arguments = arguments.Replace("%configuration%", confName);

            string confPlatform = VsHelpers.GetSolutionConfigurationPlatformName(_dte);
            arguments = arguments.Replace("%platform%", confPlatform);

            StartProcess(folder, options.Command, arguments);
        }

        private void OpenCmd(object sender, EventArgs e)
        {
            string installDir = VsHelpers.GetInstallDirectory(this);
            string devPromptFile = Path.Combine(installDir, @"..\Tools\VsDevCmd.bat");

            SetupProcess("cmd.exe", "/k \"" + devPromptFile + "\"");
        }

        private void OpenPowershell(object sender, EventArgs e)
        {
            SetupProcess("powershell.exe", "-ExecutionPolicy Bypass -NoExit");
        }

        private void SetupProcess(string command, string arguments)
        {
            Options options = GetDialogPage(typeof(Options)) as Options;
            string folder = VsHelpers.GetFolderPath(options, _dte);

            StartProcess(folder, command, arguments);
        }

        private static void StartProcess(string workingDirectory, string command, string arguments)
        {
            try
            {
                command = Environment.ExpandEnvironmentVariables(command ?? string.Empty);
                arguments = Environment.ExpandEnvironmentVariables(arguments ?? string.Empty);

                ProcessStartInfo start = new ProcessStartInfo(command, arguments);
                start.WorkingDirectory = workingDirectory;
                start.LoadUserProfile = true;
                start.UseShellExecute = false;

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
