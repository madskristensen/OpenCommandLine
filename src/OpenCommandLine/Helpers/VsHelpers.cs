﻿using System;
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

            Window2 window = dte.ActiveWindow as Window2;

            if (window != null)
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
                    var hierarchy = window.Object as UIHierarchy;
                    var projectItem = GetSelectedProjectItem(hierarchy);
                    if (projectItem != null && projectItem.FileCount > 0)
                    {
                        if (Directory.Exists(projectItem.FileNames[1]))
                            return projectItem.FileNames[1];

                        if (IsValidFileName(projectItem.FileNames[1]))
                            return Path.GetDirectoryName(projectItem.FileNames[1]);
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

        private static UIHierarchyItem GetSelectedHierarchyItem(UIHierarchy hierarchy)
        {
            var hierarchyItems = hierarchy?.SelectedItems as UIHierarchyItem[];
            if (hierarchyItems != null && hierarchyItems.Length > 0)
            {
                return hierarchyItems[0];
            }
            return null;
        }

        private static ProjectItem GetSelectedProjectItem(UIHierarchy hierarchy)
        {
            var hierarchyItem = GetSelectedHierarchyItem(hierarchy);
            return hierarchyItem?.Object as ProjectItem;
        }

        public static string GetOutputPath(DTE2 dte)
        {
            var project = GetSelectedProject(dte);
            if (project == null) return null;

            if (project.Kind == VsProjectKindPython)
            {
                var startupFile = project.Properties.Item("StartupFile").Value.ToString();
                return Path.GetDirectoryName(startupFile);
            }

            // ConfigurationManager is null for virtual folder in solution
            var activeConfigurationProperties = project.ConfigurationManager?.ActiveConfiguration.Properties;

            // Unknow 'custom' project like Python etc.
            //
            // TODO: this is a bug in VS2017
            // https://developercommunity.visualstudio.com/content/problem/44682/activeconfigurationproperties-returns-null.html
            // I don't want to use VCProject because every version of VS reqiures a corresponding version file in reference
            //
            if (activeConfigurationProperties == null) return null;

            var outputPath = activeConfigurationProperties.Item("OutputPath").Value.ToString();

            // C++ project - always
            // C#/JavaScript/etc project with absolute path in 'Output path'
            if (Path.IsPathRooted(outputPath))
            {
                return outputPath;
            }

            // C#/JavavScript/etc project with relative path in 'Output path'
            try
            {
                var fullPathObject = project.Properties.Item("FullPath");
                if (fullPathObject != null)
                {
                    var fullPath = fullPathObject.Value.ToString();
                    // e.g. JavaScript - fullPath is path to project file
                    if (File.Exists(fullPath)) // maybe FileAttributes?
                    {
                        fullPath = Path.GetDirectoryName(fullPath);
                    }
                    var outputDir = Path.Combine(fullPath, outputPath);
                    return outputDir = Path.GetFullPath(outputDir);
                }
            }
            catch (ArgumentException)
            { }

            return null;
        }

        private static Project GetSelectedProject(DTE2 dte)
        {
            Window2 window = dte.ActiveWindow as Window2;

            if (window != null)
            {
                if (window.Type == vsWindowType.vsWindowTypeDocument)
                {
                    return dte.ActiveDocument?.ProjectItem?.ContainingProject;
                }

                if (window.Type == vsWindowType.vsWindowTypeSolutionExplorer)
                {
                    var hierarchy = window.Object as UIHierarchy;
                    var hierarchyItem = GetSelectedHierarchyItem(hierarchy);
                    var project = hierarchyItem?.Object as Project;
                    if (project != null)
                    {
                        return project;
                    }
                    // e.g. file/virtual foler in project
                    var projectItem = hierarchyItem?.Object as ProjectItem;
                    return projectItem?.ContainingProject;
                }
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

        public static ProjectItem GetProjectItem(DTE2 dte)
        {
            Window2 window = dte.ActiveWindow as Window2;

            if (window == null)
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
                ProjectItem item = selItem.Object as ProjectItem;

                if (item != null)
                    yield return item;
            }
        }

        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            Uri pathUri;
            Boolean isValidUri = Uri.TryCreate(fileName, UriKind.Absolute, out pathUri);

            return isValidUri && pathUri != null && pathUri.IsLoopback;
        }

        public static string GetSolutionConfigurationName(DTE2 dte)
        {
            return dte.Solution.SolutionBuild.ActiveConfiguration?.Name;
        }

        public static string GetSolutionConfigurationPlatformName(DTE2 dte)
        {
            var configuration2 = dte.Solution.SolutionBuild.ActiveConfiguration as SolutionConfiguration2;
            return configuration2 != null ? configuration2.PlatformName : null;
        }

        private static string VsProjectKindPython = "{888888a0-9f3d-457c-b088-3a5042f75d52}";
    }
}
