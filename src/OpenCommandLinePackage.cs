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
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(GuidList.guidOpenCommandLinePkgString)]
    public sealed class OpenCommandLinePackage : Package
    {
        private static DTE2 _dte;

        protected override void Initialize()
        {
            base.Initialize();
            _dte = GetService(typeof(DTE)) as DTE2;

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                CommandID menuCommandID = new CommandID(GuidList.guidOpenCommandLineCmdSet, (int)PkgCmdIDList.cmdidOpenCommandLine);
                MenuCommand menuItem = new MenuCommand(OpenCmd, menuCommandID);
                mcs.AddCommand(menuItem);
            }
        }

        private void OpenCmd(object sender, EventArgs e)
        {
            var path = GetPath();

            if (string.IsNullOrEmpty(path))
                return;

            ProcessStartInfo start = new ProcessStartInfo("cmd", "/k")
            {
                WorkingDirectory = path
            };

            var p = new System.Diagnostics.Process();
            p.StartInfo = start;
            p.Start();
        }

        private static string GetPath()
        {
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
