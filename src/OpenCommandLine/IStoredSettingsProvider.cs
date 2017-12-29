namespace MadsKristensen.OpenCommandLine
{
    public interface IStoredSettingsProvider
    {
        string Preset { get; set; }
        string FriendlyName { get; set; }
        string Command { get; set; }
        string Arguments { get; set; }
        bool OpenSlnLevel { get; set; }
        bool OpenProjectLevel { get; set; }

        int CustomActionsLength { get; set; }
        CustomAction GetCustomAction(int index);
        void SetCustomActions(CustomAction[] customActions);
    }
}
