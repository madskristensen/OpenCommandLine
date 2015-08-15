using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MadsKristensen.OpenCommandLine
{
    public class CmdLanguage
    {
        private static Regex _rComment = new Regex("(?<=(^[\\s]+))?(rem|::).+|((?<=([\\s]+))&(rem|::).+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex _rIdentifier = new Regex("(?<=(\\bset([\\s]+)))([\\S]+)(?=([\\s]+)?=)|%([^%\\s]+)%|%~([fdpnxsatz]+\\d)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex _rKeyword = GetKeywordRegex();
        private static Regex _rLabel = new Regex("^(([\\s]+)?):([^\\s:]+)|(?<=(\\bgoto(:|\\s)([\\s]+)?))([^\\s:]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex _rOperator = new Regex(@"(&|&&|\|\||([012]?>>?)|<|!|=|^)", RegexOptions.Compiled);
        private static Regex _rParameter = new Regex("(?<=(\\s))(/|-?-)([\\w]+)", RegexOptions.Compiled);
        private static Regex _rString = new Regex("(\"|')([^\\1]+)\\1|(?<=echo([\\s]+)).+", RegexOptions.Compiled);
        private static Dictionary<string, string> _keywords = GetList();

        public static Regex KeywordRegex { get { return _rKeyword; } }
        public static Regex StringRegex { get { return _rString; } }
        public static Regex IdentifierRegex { get { return _rIdentifier; } }
        public static Regex CommentRegex { get { return _rComment; } }
        public static Regex OperatorRegex { get { return _rOperator; } }
        public static Regex ParameterRegex { get { return _rParameter; } }
        public static Regex LabelRegex { get { return _rLabel; } }

        public static Dictionary<string, string> Keywords
        {
            get { return _keywords; }
        }

        private static Regex GetKeywordRegex()
        {
            var list = GetList().Keys;
            string keywords = string.Join("|", list);
            return new Regex("(?<=(^|([\\s]+)))@?(" + keywords + ")\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        private static Dictionary<string, string> GetList()
        {
            return new Dictionary<string, string>
            {
                {"comp",        "Compares the contents of two files or sets of files."},
                {"compact",     "Displays or alters the compression of files on NTFS partitions."},
                {"convert",     "Converts FAT volumes to NTFS.  You cannot convert the current drive."},
                {"copy",        "Copies one or more files to another location."},
                {"date",        "Displays or sets the date."},
                {"define",      "Define."},
                {"del",         "Deletes one or more files."},
                {"dir",         "Displays a list of files and subdirectories in a directory."},
                {"diskcomp",    "Compares the contents of two floppy disks."},
                {"diskcopy",    "Copies the contents of one floppy disk to another."},
                {"diskpart",    "Displays or configures Disk Partition properties."},
                {"do",          "Used with for"},
                {"call",        "Call"},
                {"doskey",      "Edits command lines, recalls Windows commands, and creates macros."},
                {"driverquery", "Displays current device driver status and properties."},
                {"echo",        "Displays messages, or turns command echoing on or off."},
                {"endlocal",    "Ends localization of environment changes in a batch file."},
                {"erase",       "Deletes one or more files."},
                {"exist",       "If exist detect file and folder existance"},
                {"exit",        "Quits the CMD.EXE program (command interpreter)."},
                {"fc",          "Compares two files or sets of files, and displays the differences between them."},
                {"find",        "Searches for a text string in a file or files."},
                {"findstr",     "Searches for strings in files."},
                {"for",         "Runs a specified command for each file in a set of files."},
                {"format",      "Formats a disk for use with Windows."},
                {"fsutil",      "Displays or configures the file system properties."},
                {"ftype",       "Displays or modifies file types used in file extension associations."},
                {"goto",        "Directs the Windows command interpreter to a labeled line in a batch program."},
                {"gpresult",    "Displays Group Policy information for machine or user."},
                {"graftabl",    "Enables Windows to display an extended character set in graphics mode."},
                {"help",        "Provides Help information for Windows commands."},
                {"icacls",      "Display, modify, backup, or restore ACLs for files and directories."},
                {"if",          "Performs conditional processing in batch programs."},
                {"in",          ""},
                {"label",       "Creates, changes, or deletes the volume label of a disk."},
                {"md",          "Creates a directory."},
                {"mkdir",       "Creates a directory."},
                {"mklink",      "Creates Symbolic Links and Hard Links"},
                {"mode",        "Configures a system device."},
                {"more",        "Displays output one screen at a time."},
                {"move",        "Moves one or more files from one directory to another directory."},
                {"net",         "NET"},
                {"not",         "Not for if"},
                {"nul",         "Null value"},
                {"openfiles",   "Displays files opened by remote users for a file share."},
                {"path",        "Displays or sets a search path for executable files."},
                {"pause",       "Suspends processing of a batch file and displays a message."},
                {"popd",        "Restores the previous value of the current directory saved by PUSHD."},
                {"print",       "Prints a text file."},
                {"prompt",      "Changes the Windows command prompt."},
                {"pushd",       "Saves the current directory then changes it."},
                {"rd",          "Removes a directory."},
                {"recover",     "Recovers readable information from a bad or defective disk."},
                {"rem",         "Records comments (remarks) in batch files or CONFIG.SYS."},
                {"ren",         "Renames a file or files."},
                {"rename",      "Renames a file or files."},
                {"replace",     "Replaces files."},
                {"rmdir",       "Removes a directory."},
                {"robocopy",    "Advanced utility to copy files and directory trees."},
                {"sc",          "Displays or configures services (background processes)."},
                {"schtasks",    "Schedules commands and programs to run on a computer."},
                {"set",         "Displays, sets, or removes Windows environment variables."},
                {"setlocal",    "Begins localization of environment changes in a batch file."},
                {"shift",       "Shifts the position of replaceable parameters in batch files."},
                {"shutdown",    "Allows proper local or remote shutdown of machine."},
                {"sort",        "Sorts input."},
                {"start",       "Starts a separate window to run a specified program or command."},
                {"subst",       "Associates a path with a drive letter."},
                {"systeminfo",  "Displays machine specific properties and configuration."},
                {"taskkill",    "Kill or stop a running process or application."},
                {"tasklist",    "Displays all currently running tasks including services."},
                {"time",        "Displays or sets the system time."},
                {"title",       "Sets the window title for a CMD.EXE session."},
                {"tree",        "Graphically displays the directory structure of a drive or path."},
                {"type",        "Displays the contents of a text file."},
                {"ver",         "Displays the Windows version."},
                {"verify",      "Tells Windows whether to verify that your files are written correctly to a disk."},
                {"vol",         "Displays a disk volume label and serial number."},
                {"wmic",        "Displays WMI information inside interactive command shell."},
                {"xcopy",       "Copies files and directory trees."},
            };
        }
    }
}
