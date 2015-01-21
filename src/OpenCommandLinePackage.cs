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
    [Guid(GuidList.guidOpenCommandLinePkgString)]
    public sealed class OpenCommandLinePackage : Package
    {
        public const string Version = "1.2";
        private static DTE2 _dte;

        protected override void Initialize()
        {
            base.Initialize();
            _dte = GetService(typeof(DTE)) as DTE2;

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            CommandID menuCommandID = new CommandID(GuidList.guidOpenCommandLineCmdSet, (int)PkgCmdIDList.cmdidOpenCommandLine);
            MenuCommand menuItem = new MenuCommand(OpenCmd, menuCommandID);
            mcs.AddCommand(menuItem);
        }

        private void OpenCmd(object sender, EventArgs e)
        {
            string folder = GetFolderPath();

            if (string.IsNullOrEmpty(folder))
                return;

            Options options = GetDialogPage(typeof(Options)) as Options;
            string command = options.Command;
            string arguments = options.Arguments;

            if (options.ReplaceEnvironmentVariables)
            {
                command = Environment.ExpandEnvironmentVariables(options.Command);
                arguments = Environment.ExpandEnvironmentVariables(options.Arguments);
            }

            if (!string.IsNullOrWhiteSpace(options.FolderPathReplacementToken))
            {
                arguments = arguments.Replace(options.FolderPathReplacementToken, folder);
            }

            ProcessStartInfo start = new ProcessStartInfo(command, arguments);
            start.WorkingDirectory = folder;

            System.Diagnostics.Process.Start(start);
        }

        private static string GetFolderPath()
        {
            Window2 window = _dte.ActiveWindow as Window2;

            // If Solution Explorer isn't active but document is, then use the document's containing project
            if (window != null && window.Type == vsWindowType.vsWindowTypeDocument)
            {
                Document doc = _dte.ActiveDocument;
                if (doc != null && !string.IsNullOrEmpty(doc.FullName))
                {
                    ProjectItem item = _dte.Solution.FindProjectItem(doc.FullName);

                    if (item != null && item.ContainingProject != null && !string.IsNullOrEmpty(item.ContainingProject.FullName))
                        return item.ContainingProject.Properties.Item("FullPath").Value.ToString();
                }
            }

            Project project = GetActiveProject();

            if (project != null && !string.IsNullOrEmpty(project.FullName))
                return project.Properties.Item("FullPath").Value.ToString();

            if (_dte.Solution != null && !string.IsNullOrEmpty(_dte.Solution.FullName))
                return Path.GetDirectoryName(_dte.Solution.FullName);

            return null;
        }

        public static Project GetActiveProject()
        {
            try
            {
                Array activeSolutionProjects = _dte.ActiveSolutionProjects as Array;

                if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
                    return activeSolutionProjects.GetValue(0) as Project;
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }

            return null;
        }
    }
}
