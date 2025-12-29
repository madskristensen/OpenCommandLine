using System;
using System.Diagnostics;
using System.IO;

namespace MadsKristensen.OpenCommandLine
{
    /// <summary>
    /// Helper class for launching command line processes.
    /// </summary>
    internal static class CommandLineLauncher
    {
        /// <summary>
        /// Determines whether the command is Windows Terminal.
        /// </summary>
        public static bool IsWindowsTerminal(string command)
        {
            if (string.IsNullOrEmpty(command)) return false;
            return command.IndexOf("wt", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Determines whether the command is PowerShell (including pwsh).
        /// </summary>
        public static bool IsPowerShell(string command)
        {
            if (string.IsNullOrEmpty(command)) return false;
            return command.IndexOf("powershell", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   command.IndexOf("pwsh", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Determines whether the command is cmd.exe.
        /// </summary>
        public static bool IsCmd(string command)
        {
            return string.IsNullOrEmpty(command) ||
                   command.IndexOf("cmd", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Resolves a command to its native 64-bit path when running in a WoW64 environment.
        /// This ensures PowerShell and cmd.exe launch as 64-bit even from a 32-bit process.
        /// </summary>
        private static string ResolveToNative64BitPath(string command)
        {
            if (string.IsNullOrEmpty(command))
                return command;

            // Only apply redirection on 64-bit Windows when potentially running in WoW64
            if (!Environment.Is64BitOperatingSystem)
                return command;

            // Already running as 64-bit process, no redirection needed
            if (Environment.Is64BitProcess)
                return command;

            // Check if this is a system executable that should use native 64-bit
            string fileName = Path.GetFileName(command);
            if (string.IsNullOrEmpty(fileName))
                return command;

            // List of executables to redirect to native 64-bit
            bool shouldRedirect = fileName.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase) ||
                                  fileName.Equals("powershell_ise.exe", StringComparison.OrdinalIgnoreCase) ||
                                  fileName.Equals("pwsh.exe", StringComparison.OrdinalIgnoreCase) ||
                                  fileName.Equals("cmd.exe", StringComparison.OrdinalIgnoreCase);

            if (!shouldRedirect)
                return command;

            // If command is just the filename (e.g., "powershell.exe"), use Sysnative path
            if (fileName.Equals(command, StringComparison.OrdinalIgnoreCase))
            {
                string sysnativePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative", fileName);
                if (File.Exists(sysnativePath))
                    return sysnativePath;
            }

            // If command contains SysWOW64, redirect to Sysnative
            string sysWow64 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64");
            if (command.StartsWith(sysWow64, StringComparison.OrdinalIgnoreCase))
            {
                string sysnative = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative");
                string redirectedPath = sysnative + command.Substring(sysWow64.Length);
                if (File.Exists(redirectedPath))
                    return redirectedPath;
            }

            return command;
        }

        /// <summary>
        /// Starts a command line process in the specified working directory.
        /// </summary>
        public static void StartProcess(string workingDirectory, string command, string arguments)
        {
            try
            {
                command = Environment.ExpandEnvironmentVariables(command ?? string.Empty);
                command = ResolveToNative64BitPath(command);
                arguments = Environment.ExpandEnvironmentVariables(arguments ?? string.Empty);

                // Windows Terminal (wt.exe) is a modern Windows app that requires shell execution
                bool useShellExecute = IsWindowsTerminal(command) &&
                                       !command.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

                var start = new ProcessStartInfo(command, arguments)
                {
                    WorkingDirectory = workingDirectory,
                    LoadUserProfile = true,
                    UseShellExecute = useShellExecute
                };

                if (!useShellExecute)
                {
                    ModifyPathVariable(start);
                }

                using (Process.Start(start))
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

            var process = Process.GetCurrentProcess();
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
