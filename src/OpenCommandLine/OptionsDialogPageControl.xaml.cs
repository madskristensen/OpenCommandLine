using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace MadsKristensen.OpenCommandLine
{
    /// <summary>
    /// Interaction logic for OptionsDialogPageControl.xaml
    /// </summary>
    public partial class OptionsDialogPageControl : UserControl, INotifyPropertyChanged
    {
        private readonly IDictionary<string, Command> _defaultPresets;

        public ObservableCollection<string> Presets { get; }
        public ObservableCollection<CustomAction> CustomActions { get; }

        internal OptionsDialogPageControl(IDictionary<string, Command> defaultPresets)
        {
            _defaultPresets = defaultPresets;
            Presets = new ObservableCollection<string>(defaultPresets.Keys);

            CustomActions = new ObservableCollection<CustomAction>();

            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region Properties

        private CustomAction _customAction;

        public CustomAction CustomAction
        {
            get { return _customAction; }
            set
            {
                _customAction = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCustomActionSelected));
            }
        }

        private string _preset;

        public string Preset
        {
            get { return _preset; }
            set
            {
                _preset = value;
                OnPropertyChanged();
            }
        }

        private string _friendlyName;

        public string FriendlyName
        {
            get { return _friendlyName; }
            set
            {
                _friendlyName = value;
                OnPropertyChanged();
            }
        }

        private string _command;

        public string Command
        {
            get { return _command; }
            set
            {
                _command = value;
                OnPropertyChanged();
            }
        }

        private string _arguments;

        public string Arguments
        {
            get { return _arguments; }
            set
            {
                _arguments = value;
                OnPropertyChanged();
            }
        }

        private string _dynamicPreset;

        public string DynamicPreset
        {
            get { return _dynamicPreset; }
            set
            {
                _dynamicPreset = value;
                OnPropertyChanged();
            }
        }

        private string _dynamicFriendlyName;

        public string DynamicFriendlyName
        {
            get { return _dynamicFriendlyName; }
            set
            {
                _dynamicFriendlyName = value;
                OnPropertyChanged();
            }
        }

        private string _dynamicCommand;

        public string DynamicCommand
        {
            get { return _dynamicCommand; }
            set
            {
                _dynamicCommand = value;
                OnPropertyChanged();
            }
        }

        private string _dynamicArguments;

        public string DynamicArguments
        {
            get { return _dynamicArguments; }
            set
            {
                _dynamicArguments = value;
                OnPropertyChanged();
            }
        }

        public bool IsCustomActionSelected => CustomAction != null;

        #endregion

        #region INotifyPropertyChanged
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private void OnPresetChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var newVal = (string)e.AddedItems[0];
                var preset = _defaultPresets[newVal];

                FriendlyName = $"Default ({newVal})";
                Command = preset.Name;
                Arguments = preset.Arguments;
            }
        }

        private void OnDynamicCustomActionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CustomAction != null)
            {
                DynamicPreset = CustomAction.Preset;
                DynamicFriendlyName = CustomAction.FriendlyName;
                DynamicCommand = CustomAction.Command;
                DynamicArguments = CustomAction.Arguments;
            }
            else
            {
                DynamicPreset = null;
                DynamicFriendlyName = null;
                DynamicCommand = null;
                DynamicArguments = null;
            }
        }

        private void OnApplyButtonClick(object sender, RoutedEventArgs e)
        {
            CustomActions[CustomActions.IndexOf(CustomAction)] = new CustomAction(DynamicPreset, DynamicFriendlyName, DynamicCommand, DynamicArguments);
        }

        private void OnDynamicPresetChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var newVal = (string)e.AddedItems[0];
                var preset = _defaultPresets[newVal];

                DynamicFriendlyName = $"{newVal}";
                DynamicCommand = preset.Name;
                DynamicArguments = preset.Arguments;
            }
        }

        private void AddButtonClick(object sender, RoutedEventArgs e)
        {
            var defaultPreset = _defaultPresets.First();

            var newAction = new CustomAction(defaultPreset.Key, $"{defaultPreset.Key}", defaultPreset.Value.Name, defaultPreset.Value.Arguments);

            CustomActions.Add(newAction);
            CustomAction = newAction;
        }

        private void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            CustomActions.Remove(CustomAction);
        }

        private void MoveUpButtonClick(object sender, RoutedEventArgs e)
        {
            var currentIndex = CustomActions.IndexOf(CustomAction);

            if (currentIndex > 0)
                CustomActions.Move(currentIndex, currentIndex - 1);
        }

        private void MoveDownButtonClick(object sender, RoutedEventArgs e)
        {
            var currentIndex = CustomActions.IndexOf(CustomAction);

            if (currentIndex < CustomActions.Count - 1)
                CustomActions.Move(currentIndex, currentIndex + 1);
        }
    }
}
