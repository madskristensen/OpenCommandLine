using System;
using System.Diagnostics;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace MadsKristensen.OpenCommandLine
{
    static class VsHelpers
    {
        public static string GetFolderPath(Options options, DTE2 dte)
        {
            // If option to always open at sln level is chosen, use that.
            if (options.OpenSlnLevel && dte.Solution != null && !string.IsNullOrEmpty(dte.Solution.FullName))
                return Path.GetDirectoryName(dte.Solution.FullName);

            Window2 window = dte.ActiveWindow as Window2;

            if (window != null)
            {
                if (window.Type == vsWindowType.vsWindowTypeDocument)
                {
                    // if a document is active, use the document's containing project
                    Document doc = dte.ActiveDocument;
                    if (doc != null && !string.IsNullOrEmpty(doc.FullName))
                    {
                        ProjectItem item = dte.Solution.FindProjectItem(doc.FullName);

                        if (item != null && item.ContainingProject != null && !string.IsNullOrEmpty(item.ContainingProject.FullName))
                            return item.ContainingProject.Properties.Item("FullPath").Value.ToString();
                    }
                }
                else if (window.Type == vsWindowType.vsWindowTypeSolutionExplorer)
                {
                    // if solution explorer is active, use the path of the first selected item
                    UIHierarchy hierarchy = window.Object as UIHierarchy;
                    if (hierarchy != null && hierarchy.SelectedItems != null)
                    {
                        UIHierarchyItem[] hierarchyItems = hierarchy.SelectedItems as UIHierarchyItem[];
                        if (hierarchyItems != null && hierarchyItems.Length > 0)
                        {
                            UIHierarchyItem hierarchyItem = hierarchyItems[0] as UIHierarchyItem;
                            if (hierarchyItem != null)
                            {
                                ProjectItem projectItem = hierarchyItem.Object as ProjectItem;
                                if (projectItem != null && projectItem.FileCount > 0 && File.Exists(projectItem.FileNames[1]))
                                {
                                    string file = projectItem.FileNames[1];
                                    return Path.GetDirectoryName(file);
                                }
                            }
                        }
                    }
                }
            }

            Project project = GetActiveProject(dte);

            if (project != null)
                return project.GetRootFolder();

            if (dte.Solution != null && !string.IsNullOrEmpty(dte.Solution.FullName))
                return Path.GetDirectoryName(dte.Solution.FullName);

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        public static string GetRootFolder(this Project project)
        {
            if (string.IsNullOrEmpty(project.FullName))
                return null;

            string fullPath;

            try
            {
                fullPath = project.Properties.Item("FullPath").Value as string;
            }
            catch (ArgumentException)
            {
                try
                {
                    // MFC projects don't have FullPath, and there seems to be no way to query existence
                    fullPath = project.Properties.Item("ProjectDirectory").Value as string;
                }
                catch (ArgumentException)
                {
                    // Installer projects have a ProjectPath.
                    fullPath = project.Properties.Item("ProjectPath").Value as string;
                }
            }

            if (string.IsNullOrEmpty(fullPath))
                return File.Exists(project.FullName) ? Path.GetDirectoryName(project.FullName) : null;

            if (Directory.Exists(fullPath))
                return fullPath;

            if (File.Exists(fullPath))
                return Path.GetDirectoryName(fullPath);

            return null;
        }

        private static Project GetActiveProject(DTE2 dte)
        {
            try
            {
                Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;

                if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
                    return activeSolutionProjects.GetValue(0) as Project;
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }

            return null;
        }


        public static string GetInstallDirectory(IServiceProvider serviceProvider)
        {
            string installDirectory = null;

            IVsShell shell = (IVsShell)serviceProvider.GetService(typeof(SVsShell));
            if (shell != null)
            {
                object installDirectoryObj = null;
                shell.GetProperty((int)__VSSPROPID.VSSPROPID_InstallDirectory, out installDirectoryObj);
                if (installDirectoryObj != null)
                {
                    installDirectory = installDirectoryObj as string;
                }
            }

            return installDirectory;
        }
    }
}
