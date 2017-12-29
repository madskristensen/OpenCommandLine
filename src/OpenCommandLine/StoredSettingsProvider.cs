using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;

namespace MadsKristensen.OpenCommandLine
{
    [Export(typeof(IStoredSettingsProvider))]
    public class StoredSettingsProvider : IStoredSettingsProvider
    {
        private const string CollectionName = "DialogPage\\MadsKristensen.OpenCommandLine.Options";

        private readonly WritableSettingsStore _writableSettingsStore;

        [ImportingConstructor]
        public StoredSettingsProvider(SVsServiceProvider vsServiceProvider)
        {
            var shellSettingsManager = new ShellSettingsManager(vsServiceProvider);
            _writableSettingsStore = shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!_writableSettingsStore.CollectionExists(CollectionName))
            {
                _writableSettingsStore.CreateCollection(CollectionName);
            }
        }

        public string Preset
        {
            get
            {
                return _writableSettingsStore.GetString(CollectionName, nameof(Preset), string.Empty);
            }
            set
            {
                _writableSettingsStore.SetString(CollectionName, nameof(Preset), value);
            }
        }

        public string FriendlyName
        {
            get
            {
                return _writableSettingsStore.GetString(CollectionName, nameof(FriendlyName), string.Empty);
            }
            set
            {
                _writableSettingsStore.SetString(CollectionName, nameof(FriendlyName), value);
            }
        }

        public string Command
        {
            get
            {
                return _writableSettingsStore.GetString(CollectionName, nameof(Command), string.Empty);
            }
            set
            {
                _writableSettingsStore.SetString(CollectionName, nameof(Command), value);
            }
        }

        public string Arguments
        {
            get
            {
                return _writableSettingsStore.GetString(CollectionName, nameof(Arguments), string.Empty);
            }
            set
            {
                _writableSettingsStore.SetString(CollectionName, nameof(Arguments), value);
            }
        }

        public bool OpenSlnLevel
        {
            get
            {
                return bool.Parse(_writableSettingsStore.GetString(CollectionName, nameof(OpenSlnLevel), string.Empty));
            }
            set
            {
                _writableSettingsStore.SetString(CollectionName, nameof(OpenSlnLevel), value.ToString());
            }
        }

        public bool OpenProjectLevel
        {
            get
            {
                return bool.Parse(_writableSettingsStore.GetString(CollectionName, nameof(OpenProjectLevel), string.Empty));
            }
            set
            {
                _writableSettingsStore.SetString(CollectionName, nameof(OpenProjectLevel), value.ToString());
            }
        }

        public int CustomActionsLength
        {
            get
            {
                return int.Parse(_writableSettingsStore.GetString(CollectionName, nameof(CustomActionsLength), "0"));
            }
            set
            {
                _writableSettingsStore.SetString(CollectionName, nameof(CustomActionsLength), value.ToString());
            }
        }

        public CustomAction GetCustomAction(int index)
        {
            var val = _writableSettingsStore.GetString(CollectionName, $"CustomAction{index}", string.Empty);

            if (!string.IsNullOrEmpty(val))
            {
                var customAction = val.Split(new[] {"##"}, StringSplitOptions.None);
                return new CustomAction(customAction[0], customAction[1], customAction[2], customAction[3]);
            }

            return null;
        }

        public void SetCustomActions(CustomAction[] customActions)
        {
            for (var i = 0; i < CustomActionsLength; i++)
            {
                _writableSettingsStore.DeleteProperty(CollectionName, $"CustomAction{i}");
            }

            CustomActionsLength = customActions.Length;

            for (var i = 0; i < customActions.Length; i++)
            {
                _writableSettingsStore.SetString(CollectionName, $"CustomAction{i}", $"{customActions[i].Preset}##{customActions[i].FriendlyName}##{customActions[i].Command}##{customActions[i].Arguments}");
            }
        }
    }
}
