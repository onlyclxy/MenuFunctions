using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MenuFunctions_Panel.Models;
using MenuFunctions_Panel.ViewModels;
using MenuFunctions_Panel.Helpers;
using Microsoft.Win32;

namespace MenuFunctions_Panel
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private MenuItemConfig _copiedItem;
        private bool _isCutOperation;
        private TreeViewDragDropHelper _dragDropHelper;
        private List<string> _selectedTestFiles = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;

            // 初始化拖拽辅助类
            _dragDropHelper = new TreeViewDragDropHelper(MenuTreeView, _viewModel);

            // 注册键盘快捷键
            this.KeyDown += MainWindow_KeyDown;

            // 尝试自动加载配置文件
            AutoLoadConfig();
            
            // 确保窗口显示在最前面
            this.Loaded += (s, e) =>
            {
                this.Activate();
                this.Focus();
            };
        }

        #region 文件浏览器相关

        private void FileBrowser_SelectionChanged(object sender, List<string> selectedPaths)
        {
            _selectedTestFiles = selectedPaths;
        }

        private void FileBrowser_FolderBackgroundSelected(object sender, string folderPath)
        {
            _selectedTestFiles.Clear();
            _selectedTestFiles.Add(folderPath);
        }

        #endregion

        #region 配置文件操作

        private void AutoLoadConfig()
        {
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MenuConfig.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MenuFunctions", "bin", "Release", "MenuConfig.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "MenuFunctions", "bin", "Release", "MenuConfig.json")
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    var fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        _viewModel.LoadConfig(fullPath);
                        return;
                    }
                }
                catch { }
            }
        }

        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON 文件|*.json|所有文件|*.*",
                Title = "打开配置文件"
            };

            if (dialog.ShowDialog() == true)
            {
                _viewModel.LoadConfig(dialog.FileName);
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_viewModel.ConfigFilePath))
            {
                SaveAsConfig_Click(sender, e);
            }
            else
            {
                _viewModel.SaveConfig(_viewModel.ConfigFilePath);
            }
        }

        private void SaveAsConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON 文件|*.json|所有文件|*.*",
                Title = "保存配置文件",
                FileName = "MenuConfig.json"
            };

            if (dialog.ShowDialog() == true)
            {
                _viewModel.SaveConfig(dialog.FileName);
            }
        }

        #endregion

        #region 菜单项操作

        private void AddMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AddMenuItem();
        }

        private void AddSubMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AddSubMenuItem();
        }

        private void AddSeparator_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AddSeparator();
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DeleteMenuItem();
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.MoveUp();
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.MoveDown();
        }

        private void MenuTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _viewModel.SelectedItem = e.NewValue as MenuItemConfig;
        }

        private void BrowseProgram_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "可执行文件|*.exe;*.bat;*.cmd|所有文件|*.*",
                Title = "选择程序"
            };

            if (dialog.ShowDialog() == true && _viewModel.SelectedItem != null)
            {
                _viewModel.SelectedItem.ProgramPath = dialog.FileName;
            }
        }

        private void BrowseIcon_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "图标文件|*.ico;*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*",
                Title = "选择图标"
            };

            if (dialog.ShowDialog() == true && _viewModel.SelectedItem != null)
            {
                _viewModel.SelectedItem.IconPath = dialog.FileName;
            }
        }

        #endregion

        #region 复制粘贴功能

        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedItem != null)
            {
                _copiedItem = CloneMenuItem(_viewModel.SelectedItem);
                _isCutOperation = false;
                MessageBox.Show("已复制菜单项", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedItem != null)
            {
                _copiedItem = _viewModel.SelectedItem;
                _isCutOperation = true;
                MessageBox.Show("已剪切菜单项", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_copiedItem != null)
            {
                var itemToPaste = _isCutOperation ? _copiedItem : CloneMenuItem(_copiedItem);
                
                if (_viewModel.SelectedItem != null)
                {
                    // 粘贴为子项
                    if (_viewModel.SelectedItem.SubItems == null)
                    {
                        _viewModel.SelectedItem.SubItems = new System.Collections.ObjectModel.ObservableCollection<MenuItemConfig>();
                    }
                    _viewModel.SelectedItem.SubItems.Add(itemToPaste);
                }
                else
                {
                    // 粘贴为根项
                    _viewModel.MenuItems.Add(itemToPaste);
                }

                if (_isCutOperation)
                {
                    _viewModel.DeleteMenuItem();
                    _copiedItem = null;
                    _isCutOperation = false;
                }

                MessageBox.Show("已粘贴菜单项", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PasteBefore_Click(object sender, RoutedEventArgs e)
        {
            if (_copiedItem != null && _viewModel.SelectedItem != null)
            {
                var itemToPaste = _isCutOperation ? _copiedItem : CloneMenuItem(_copiedItem);
                var collection = _viewModel.FindParentCollection(_viewModel.MenuItems, _viewModel.SelectedItem);
                
                if (collection != null)
                {
                    var index = collection.IndexOf(_viewModel.SelectedItem);
                    collection.Insert(index, itemToPaste);
                    
                    if (_isCutOperation)
                    {
                        collection.Remove(_copiedItem);
                        _copiedItem = null;
                        _isCutOperation = false;
                    }
                }
            }
        }

        private void PasteAfter_Click(object sender, RoutedEventArgs e)
        {
            if (_copiedItem != null && _viewModel.SelectedItem != null)
            {
                var itemToPaste = _isCutOperation ? _copiedItem : CloneMenuItem(_copiedItem);
                var collection = _viewModel.FindParentCollection(_viewModel.MenuItems, _viewModel.SelectedItem);
                
                if (collection != null)
                {
                    var index = collection.IndexOf(_viewModel.SelectedItem);
                    collection.Insert(index + 1, itemToPaste);
                    
                    if (_isCutOperation)
                    {
                        collection.Remove(_copiedItem);
                        _copiedItem = null;
                        _isCutOperation = false;
                    }
                }
            }
        }

        private MenuItemConfig CloneMenuItem(MenuItemConfig source)
        {
            var cloned = new MenuItemConfig
            {
                Text = source.Text + " (副本)",
                Command = source.Command,
                ProgramPath = source.ProgramPath,
                FileTypes = source.FileTypes?.ToList(),
                ShowInRootMenu = source.ShowInRootMenu,
                IconPath = source.IconPath,
                IsVisible = source.IsVisible,
                ShowOnFiles = source.ShowOnFiles,
                AppendCommandToEachPath = source.AppendCommandToEachPath,
                ShowOnFolderBackground = source.ShowOnFolderBackground,
                ShowOnFolder = source.ShowOnFolder,
                OnlyUsingProgram = source.OnlyUsingProgram,
                RunningProgramWithCMD = source.RunningProgramWithCMD,
                HideCmdWindow = source.HideCmdWindow,
                KeepCmdWindows = source.KeepCmdWindows,
                IsSeparator = source.IsSeparator,
                DisplayCompletePathAndCommand = source.DisplayCompletePathAndCommand,
                UseQuotes = source.UseQuotes,
                Order = source.Order,
                AddFileTypeParameter = source.AddFileTypeParameter
            };

            if (source.SubItems != null)
            {
                cloned.SubItems = new System.Collections.ObjectModel.ObservableCollection<MenuItemConfig>();
                foreach (var subItem in source.SubItems)
                {
                    cloned.SubItems.Add(CloneMenuItem(subItem));
                }
            }

            return cloned;
        }

        #endregion

        #region 拖拽功能
        
        // 拖拽功能现在由 TreeViewDragDropHelper 类处理

        #endregion

        #region 其他功能

        private void ToggleEnabled_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedItem != null)
            {
                _viewModel.SelectedItem.IsVisible = !_viewModel.SelectedItem.IsVisible;
                var status = _viewModel.SelectedItem.IsVisible ? "已启用" : "已停用";
                MessageBox.Show($"菜单项{status}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void TestRun_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedItem == null)
            {
                MessageBox.Show("请先选择要测试的菜单项", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedTestFiles.Count == 0)
            {
                MessageBox.Show("请在左侧文件浏览器中选择要测试的文件或文件夹", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var config = _viewModel.SelectedItem;
                var files = string.Join(" ", _selectedTestFiles.Select(f => $"\"{f}\""));
                
                var message = $"即将执行:\n\n" +
                             $"程序: {config.ProgramPath}\n" +
                             $"参数: {config.Command}\n" +
                             $"文件: {string.Join(", ", _selectedTestFiles)}\n\n" +
                             $"是否继续?";

                if (MessageBox.Show(message, "测试运行确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (!string.IsNullOrEmpty(config.ProgramPath))
                    {
                        var args = string.IsNullOrEmpty(config.Command) ? files : $"{config.Command} {files}";
                        Process.Start(config.ProgramPath, args);
                        MessageBox.Show("命令已执行", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("未设置程序路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"执行失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.C:
                        CopyMenuItem_Click(null, null);
                        break;
                    case Key.X:
                        CutMenuItem_Click(null, null);
                        break;
                    case Key.V:
                        PasteMenuItem_Click(null, null);
                        break;
                    case Key.S:
                        SaveConfig_Click(null, null);
                        e.Handled = true;
                        break;
                }
            }
            else if (e.Key == Key.Delete)
            {
                DeleteMenuItem_Click(null, null);
            }
        }

        #endregion
    }

    /// <summary>
    /// 反向布尔值到可见性转换器
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 非空判断转换器
    /// </summary>
    public class NotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
