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
        /// Starts a command line process in the specified working directory.
        /// </summary>
        public static void StartProcess(string workingDirectory, string command, string arguments)
        {
            try
            {
                command = Environment.ExpandEnvironmentVariables(command ?? string.Empty);
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
