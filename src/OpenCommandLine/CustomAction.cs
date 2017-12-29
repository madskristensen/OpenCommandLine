namespace MadsKristensen.OpenCommandLine
{
    public class CustomAction
    {
        public string Preset { get; set; }
        public string FriendlyName { get; set; }
        public string Command { get; set; }
        public string Arguments { get; set; }

        public CustomAction(string preset, string friendlyName, string command, string arguments)
        {
            Preset = preset;
            FriendlyName = friendlyName;
            Command = command;
            Arguments = arguments;
        }
    }
}
