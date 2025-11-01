using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MenuFunctions_Panel.Models
{
    /// <summary>
    /// 菜单项配置类，对应 JSON 配置文件的数据结构
    /// </summary>
    public class MenuItemConfig : INotifyPropertyChanged
    {
        private string _text;
        private string _command;
        private string _programPath;
        private ObservableCollection<MenuItemConfig> _subItems;
        private List<string> _fileTypes;
        private bool _showInRootMenu;
        private string _iconPath;
        private bool _isVisible = true;
        private bool _showOnFiles = true;
        private bool _appendCommandToEachPath = true;
        private bool _showOnFolderBackground = true;
        private bool _showOnFolder = true;
        private bool _onlyUsingProgram;
        private bool _runningProgramWithCMD;
        private bool _hideCmdWindow;
        private bool _keepCmdWindows;
        private bool _isSeparator;
        private bool _displayCompletePathAndCommand;
        private bool _useQuotes = true;
        private int _order;
        private bool _addFileTypeParameter;

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        public string Command
        {
            get => _command;
            set { _command = value; OnPropertyChanged(); }
        }

        public string ProgramPath
        {
            get => _programPath;
            set { _programPath = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MenuItemConfig> SubItems
        {
            get => _subItems;
            set { _subItems = value; OnPropertyChanged(); }
        }

        public List<string> FileTypes
        {
            get => _fileTypes;
            set { _fileTypes = value; OnPropertyChanged(); }
        }

        public bool ShowInRootMenu
        {
            get => _showInRootMenu;
            set { _showInRootMenu = value; OnPropertyChanged(); }
        }

        public string IconPath
        {
            get => _iconPath;
            set { _iconPath = value; OnPropertyChanged(); }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set { _isVisible = value; OnPropertyChanged(); }
        }

        public bool ShowOnFiles
        {
            get => _showOnFiles;
            set { _showOnFiles = value; OnPropertyChanged(); }
        }

        public bool AppendCommandToEachPath
        {
            get => _appendCommandToEachPath;
            set { _appendCommandToEachPath = value; OnPropertyChanged(); }
        }

        public bool ShowOnFolderBackground
        {
            get => _showOnFolderBackground;
            set { _showOnFolderBackground = value; OnPropertyChanged(); }
        }

        public bool ShowOnFolder
        {
            get => _showOnFolder;
            set { _showOnFolder = value; OnPropertyChanged(); }
        }

        public bool OnlyUsingProgram
        {
            get => _onlyUsingProgram;
            set { _onlyUsingProgram = value; OnPropertyChanged(); }
        }

        public bool RunningProgramWithCMD
        {
            get => _runningProgramWithCMD;
            set { _runningProgramWithCMD = value; OnPropertyChanged(); }
        }

        public bool HideCmdWindow
        {
            get => _hideCmdWindow;
            set { _hideCmdWindow = value; OnPropertyChanged(); }
        }

        public bool KeepCmdWindows
        {
            get => _keepCmdWindows;
            set { _keepCmdWindows = value; OnPropertyChanged(); }
        }

        public bool IsSeparator
        {
            get => _isSeparator;
            set { _isSeparator = value; OnPropertyChanged(); }
        }

        public bool DisplayCompletePathAndCommand
        {
            get => _displayCompletePathAndCommand;
            set { _displayCompletePathAndCommand = value; OnPropertyChanged(); }
        }

        public bool UseQuotes
        {
            get => _useQuotes;
            set { _useQuotes = value; OnPropertyChanged(); }
        }

        public int Order
        {
            get => _order;
            set { _order = value; OnPropertyChanged(); }
        }

        public bool AddFileTypeParameter
        {
            get => _addFileTypeParameter;
            set { _addFileTypeParameter = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MenuItemConfig()
        {
            SubItems = new ObservableCollection<MenuItemConfig>();
            FileTypes = new List<string>();
        }
    }
}

