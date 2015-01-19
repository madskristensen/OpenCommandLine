## Open Command Line
### A Visual Studio extension

[![Build status](https://ci.appveyor.com/api/projects/status/1jah71aylecjbkeh?svg=true)](https://ci.appveyor.com/project/madskristensen/opencommandline)

Download from the
[Visual Studio Gallery](https://visualstudiogallery.msdn.microsoft.com/4e84e2cf-2d6b-472a-b1e2-b84932511379)
or get the
[nightly build](https://ci.appveyor.com/project/madskristensen/opencommandline/build/1.0.2/artifacts)

## Supported consoles

The Open Command Line extension supports all types of consoles like cmd, PowerShell,
Bash and more. You can easily configure which to use by setting the paths and arguments
in the Options.

![Open Command Line](https://raw.githubusercontent.com/madskristensen/OpenCommandLine/master/screenshots/options.png)

### How it works

This extension adds a new command to the project context menu that will open
a command prompt on the project's path. If the solution node is selection in Solution
Explorer, then a console will open at the root of the .sln file.

![Open Command Line](https://raw.githubusercontent.com/madskristensen/OpenCommandLine/master/screenshots/context-menu.png)

You can access the command by hitting **ALT+Space** as well.