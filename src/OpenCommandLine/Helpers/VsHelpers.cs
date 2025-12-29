using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MadsKristensen.OpenCommandLine
{
    internal static class VsHelpers
    {
        public static string GetFolderPath(Options options, DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // If option to always open at sln level is chosen, use that.
            if (options.OpenSlnLevel && dte.Solution != null && !string.IsNullOrEmpty(dte.Solution.FullName))
            {
                return Path.GetDirectoryName(dte.Solution.FullName);
            }

            // Always try to get the selected folder path first (works for both solution and Open Folder mode)
            // This handles context menu invocations where user right-clicked on a specific folder
            string selectedPath = GetSelectedItemPath();
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (Directory.Exists(selectedPath))
                {
                    return selectedPath;
                }
                if (File.Exists(selectedPath))
                {
                    return Path.GetDirectoryName(selectedPath);
                }
            }

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
                            ProjectItem item = dte.Solution?.FindProjectItem(doc.FullName);

                            if (item != null && item.ContainingProject != null && !string.IsNullOrEmpty(item.ContainingProject.FullName))
                            {
                                string folder = item.ContainingProject.GetRootFolder();

                                if (!string.IsNullOrEmpty(folder))
                                {
                                    return folder;
                                }
                            }
                        }

                        return Path.GetDirectoryName(dte.ActiveDocument.FullName);
                    }
                }
                else if (window.Type == vsWindowType.vsWindowTypeSolutionExplorer)
                {
                    // if solution explorer is active, use the path of the first selected item
                    string folderPath = GetSelectedFolderPath(dte);
                    if (!string.IsNullOrEmpty(folderPath))
                    {
                        return folderPath;
                    }
                }
            }

            Project project = GetActiveProject(dte);

            if (project != null && !project.Kind.Equals("{66A26720-8FB5-11D2-AA7E-00C04F688DDE}", StringComparison.OrdinalIgnoreCase)) //ProjectKinds.vsProjectKindSolutionFolder
            {
                string rootFolder = project.GetRootFolder();
                if (!string.IsNullOrEmpty(rootFolder))
                {
                    return rootFolder;
                }
            }

            if (dte.Solution != null && !string.IsNullOrEmpty(dte.Solution.FullName))
            {
                return Path.GetDirectoryName(dte.Solution.FullName);
            }

            // Handle "Open Folder" mode - no solution but folder is open
            string openFolderPath = GetOpenFolderPath(dte);
            if (!string.IsNullOrEmpty(openFolderPath))
            {
                return openFolderPath;
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        /// <summary>
        /// Gets the full path of the currently selected item using IVsMonitorSelection.
        /// Works in both Solution mode and Open Folder mode.
        /// </summary>
        public static string GetSelectedItemPath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
                if (monitorSelection == null)
                {
                    return null;
                }

                monitorSelection.GetCurrentSelection(out IntPtr hierarchyPtr, out uint itemId, out _, out _);

                if (hierarchyPtr == IntPtr.Zero)
                {
                    return null;
                }

                try
                {
                    var vsHierarchy = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                    if (vsHierarchy == null)
                    {
                        return null;
                    }

                    // Get the canonical name which contains the full path
                    if (vsHierarchy.GetCanonicalName(itemId, out string canonicalName) == VSConstants.S_OK &&
                        !string.IsNullOrEmpty(canonicalName))
                    {
                        return canonicalName;
                    }

                    // Try VSHPROPID_Name for the item name (may need to build full path)
                    if (vsHierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_SaveName, out object saveNameObj) == VSConstants.S_OK)
                    {
                        string saveName = saveNameObj as string;
                        if (!string.IsNullOrEmpty(saveName) && (File.Exists(saveName) || Directory.Exists(saveName)))
                        {
                            return saveName;
                        }
                    }
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.Release(hierarchyPtr);
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Gets the path of the selected item in Solution Explorer using DTE.
        /// Used as fallback for solution mode when window type is SolutionExplorer.
        /// </summary>
        private static string GetSelectedFolderPath(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var hierarchy = dte.ToolWindows.SolutionExplorer;
                if (hierarchy?.SelectedItems == null)
                {
                    return null;
                }

                var selectedItems = hierarchy.SelectedItems as Array;
                if (selectedItems == null || selectedItems.Length == 0)
                {
                    return null;
                }

                var firstItem = selectedItems.GetValue(0) as UIHierarchyItem;
                if (firstItem == null)
                {
                    return null;
                }

                // Try to get path from ProjectItem (solution mode)
                if (firstItem.Object is ProjectItem projectItem && projectItem.FileCount > 0)
                {
                    string itemPath = projectItem.FileNames[1];
                    if (Directory.Exists(itemPath))
                    {
                        return itemPath;
                    }
                    if (IsValidFileName(itemPath))
                    {
                        return Path.GetDirectoryName(itemPath);
                    }
                }

                // Try to get path from Project
                if (firstItem.Object is Project proj)
                {
                    string rootFolder = proj.GetRootFolder();
                    if (!string.IsNullOrEmpty(rootFolder))
                    {
                        return rootFolder;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Gets the root folder when VS is in "Open Folder" mode (no solution).
        /// </summary>
        private static string GetOpenFolderPath(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // In Open Folder mode, Solution.FullName is empty but we can try to get the folder
                // from the solution's properties or the active document
                if (dte.Solution != null)
                {
                    // Try to get the solution directory even in folder mode
                    try
                    {
                        var solutionDir = dte.Solution.Properties?.Item("Path")?.Value as string;
                        if (!string.IsNullOrEmpty(solutionDir) && Directory.Exists(solutionDir))
                        {
                            return solutionDir;
                        }
                    }
                    catch
                    {
                        // Property doesn't exist
                    }
                }

                // Fall back to active document's folder
                if (dte.ActiveDocument != null && IsValidFileName(dte.ActiveDocument.FullName))
                {
                    return Path.GetDirectoryName(dte.ActiveDocument.FullName);
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }

            return null;
        }

        public static string GetRootFolder(this Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(project.FullName))
            {
                return null;
            }

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
            {
                return File.Exists(project.FullName) ? Path.GetDirectoryName(project.FullName) : null;
            }

            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }

            if (File.Exists(fullPath))
            {
                return Path.GetDirectoryName(fullPath);
            }

            return null;
        }

        private static Project GetActiveProject(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {

                if (dte.ActiveSolutionProjects is Array activeSolutionProjects && activeSolutionProjects.Length > 0)
                {
                    return activeSolutionProjects.GetValue(0) as Project;
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }

            return null;
        }


        public static string GetInstallDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string installDirectory = null;

            var shell = (IVsShell)Package.GetGlobalService(typeof(SVsShell));
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
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!(dte.ActiveWindow is Window2 window))
            {
                return null;
            }

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
            ThreadHelper.ThrowIfNotOnUIThread();

            var items = (Array)dte.ToolWindows.SolutionExplorer.SelectedItems;

            foreach (UIHierarchyItem selItem in items)
            {

                if (selItem.Object is ProjectItem item)
                {
                    yield return item;
                }
            }
        }

        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            bool isValidUri = Uri.TryCreate(fileName, UriKind.Absolute, out Uri pathUri);

            return isValidUri && pathUri != null && pathUri.IsLoopback;
        }

        public static string GetSolutionConfigurationName(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return dte.Solution?.SolutionBuild?.ActiveConfiguration?.Name;
        }

        public static string GetSolutionConfigurationPlatformName(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var configuration2 = dte.Solution.SolutionBuild.ActiveConfiguration as SolutionConfiguration2;
            return configuration2?.PlatformName;
        }

        /// <summary>
        /// Replaces common argument placeholders (%folder%, %configuration%, %platform%) with their values.
        /// </summary>
        public static string ReplaceArgumentPlaceholders(string arguments, string folder, DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(arguments))
                return string.Empty;

            arguments = arguments.Replace("%folder%", folder ?? string.Empty);
            arguments = arguments.Replace("%configuration%", GetSolutionConfigurationName(dte) ?? string.Empty);
            arguments = arguments.Replace("%platform%", GetSolutionConfigurationPlatformName(dte) ?? string.Empty);

            return arguments;
        }
    }
}
