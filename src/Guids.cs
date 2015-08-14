using System;

namespace MadsKristensen.OpenCommandLine
{
    static class GuidList
    {
        public const string guidOpenCommandLinePkgString = "f4ab1e64-5d35-4f06-bad9-bf414f4b3bbb";
        public const string guidOpenCommandLineCmdSetString = "59c8a2ef-e017-4f2d-93ee-ca161749897d";

        public static readonly Guid guidOpenCommandLineCmdSet = new Guid(guidOpenCommandLineCmdSetString);
    }

    static class PkgCmdIDList
    {
        public const uint cmdidOpenCommandLine = 0x100;
        public const uint cmdidOpenCmd = 0x200;
        public const uint cmdidOpenPowershell = 0x300;
        public const uint cmdidOpenOptions = 0x400;
        public const uint cmdExecuteCmd = 0x500;
    }
}