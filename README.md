# Open Command Line

[![Build](https://github.com/madskristensen/OpenCommandLine/actions/workflows/build.yaml/badge.svg)](https://github.com/madskristensen/OpenCommandLine/actions/workflows/build.yaml)

Download from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.OpenCommandLine)
or get the [CI build](https://www.vsixgallery.com/extension/f4ab1e64-5d35-4f06-bad9-bf414f4b3bbb/)

Opens a command line at the root of the project or solution. Supports all popular consoles including CMD, PowerShell, Windows Terminal, Git Bash, and many more. Also provides syntax highlighting, IntelliSense, and execution of `.cmd`, `.bat`, and `.ps1` files.

## Features

### Open Command Line from Context Menu

Right-click on any project, solution, folder, or file in Solution Explorer to open a command line at that location.

![Context Menu](screenshots/context-menu.png)

### Keyboard Shortcuts

| Shortcut | Command |
|----------|---------|
| `Alt+Space` | Open default command line |
| `Alt+Shift+,` | Open Developer Command Prompt |
| `Alt+Shift+.` | Open PowerShell |
| `Alt+Shift+5` | Execute current batch/script file |

You can customize these shortcuts in **Tools → Options → Environment → Keyboard**. Search for commands starting with `ProjectAndSolutionContextMenus.Project.OpenCommandLine`.

### Built-in Presets

The extension comes with presets for popular command line tools:

- **cmd** - Windows Command Prompt
- **Developer Command Prompt** - VS Developer Command Prompt with build tools in PATH
- **PowerShell** - Windows PowerShell
- **PowerShell Core** - Cross-platform PowerShell (pwsh.exe)
- **PowerShell ISE** - PowerShell Integrated Scripting Environment
- **Windows Terminal** - Modern Windows Terminal
- **Git Bash** - Git for Windows Bash shell
- **posh-git** - PowerShell with Git integration
- **cmder** - Portable console emulator
- **ConEmu** - Windows console emulator
- **Babun** - Cygwin-based shell
- **Custom** - Configure your own command and arguments

### Configurable Options

Open **Tools → Options → Environment → Command Line** to configure:

![Options](screenshots/options.png)

- **Select preset** - Choose from built-in presets or create a custom configuration
- **Friendly name** - Display name shown in the context menu
- **Command** - Path to the executable
- **Command arguments** - Arguments passed to the command
  - `%folder%` - Current folder path
  - `%configuration%` - Active build configuration (Debug/Release)
  - `%platform%` - Active build platform (x86/x64/AnyCPU)
- **Always open at solution level** - Always open at the solution root regardless of selection
- **Open files at project level** - Open at project root when a document is active

### Execute Script Files

Execute `.cmd`, `.bat`, and `.ps1` files directly from the context menu or with `Alt+Shift+5`.

![Execute Script](screenshots/execute-context-menu.png)

PowerShell scripts run with `-ExecutionPolicy Bypass` for convenience.

## License

[Apache 2.0](LICENSE)
