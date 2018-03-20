using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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


            if (dte.ActiveWindow is Window2 window)
            {
                if (window.Type == vsWindowType.vsWindowTypeDocument)
                {
                    // if a document is active, use the document's containing folder
                    Document doc = dte.ActiveDocument;
                    if (doc != null && IsValidFileName(doc.FullName))
                    {
                        if (options.OpenProjectLevel)
                        {
                            ProjectItem item = dte.Solution.FindProjectItem(doc.FullName);

                            if (item != null && item.ContainingProject != null && !string.IsNullOrEmpty(item.ContainingProject.FullName))
                            {
                                string folder = item.ContainingProject.GetRootFolder();

                                if (!string.IsNullOrEmpty(folder))
                                    return folder;
                            }
                        }
                        else
                        {
                            return Path.GetDirectoryName(dte.ActiveDocument.FullName);
                        }
                    }
                }
                else if (window.Type == vsWindowType.vsWindowTypeSolutionExplorer)
                {
                    // if solution explorer is active, use the path of the first selected item
                    if (window.Object is UIHierarchy hierarchy && hierarchy.SelectedItems != null)
                    {
                        if (hierarchy.SelectedItems is UIHierarchyItem[] hierarchyItems && hierarchyItems.Length > 0)
                        {
                            if (hierarchyItems[0] is UIHierarchyItem hierarchyItem)
                            {
                                if (hierarchyItem.Object is ProjectItem projectItem && projectItem.FileCount > 0)
                                {
                                    if (Directory.Exists(projectItem.FileNames[1]))
                                        return projectItem.FileNames[1];

                                    if (IsValidFileName(projectItem.FileNames[1]))
                                        return Path.GetDirectoryName(projectItem.FileNames[1]);
                                }
                            }
                        }
                    }
                }
            }

            Project project = GetActiveProject(dte);

            if (project != null && !project.Kind.Equals(ProjectKinds.vsProjectKindSolutionFolder, StringComparison.OrdinalIgnoreCase))
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

                if (dte.ActiveSolutionProjects is Array activeSolutionProjects && activeSolutionProjects.Length > 0)
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

            var shell = (IVsShell)serviceProvider.GetService(typeof(SVsShell));
            if (shell != null)
            {
                shell.GetProperty((int)__VSSPROPID.VSSPROPID_InstallDirectory, out object installDirectoryObj);
                if (installDirectoryObj != null)
                {
                    installDirectory = installDirectoryObj as string;
                }
            }

            return installDirectory;
        }

        public static ProjectItem GetProjectItem(DTE2 dte)
        {

            if (!(dte.ActiveWindow is Window2 window))
                return null;

            if (window.Type == vsWindowType.vsWindowTypeDocument)
            {
                Document doc = dte.ActiveDocument;

                if (doc != null && !string.IsNullOrEmpty(doc.FullName))
                {
                    return dte.Solution.FindProjectItem(doc.FullName);
                }
            }

            return GetSelectedItems(dte).FirstOrDefault();
        }

        private static IEnumerable<ProjectItem> GetSelectedItems(DTE2 dte)
        {
            var items = (Array)dte.ToolWindows.SolutionExplorer.SelectedItems;

            foreach (UIHierarchyItem selItem in items)
            {

                if (selItem.Object is ProjectItem item)
                    yield return item;
            }
        }

        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            bool isValidUri = Uri.TryCreate(fileName, UriKind.Absolute, out Uri pathUri);

            return isValidUri && pathUri != null && pathUri.IsLoopback;
        }
    }
}
