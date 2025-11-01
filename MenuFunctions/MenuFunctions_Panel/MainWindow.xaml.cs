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
        private Point _dragStartPoint;
        private List<string> _selectedTestFiles = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;

            // 注册键盘快捷键
            this.KeyDown += MainWindow_KeyDown;

            // 初始化文件浏览器
            InitializeFileTree();

            // 尝试自动加载配置文件
            AutoLoadConfig();
        }

        #region 文件浏览器相关

        private void InitializeFileTree()
        {
            // 加载桌面路径
            LoadFileTree(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        }

        private void LoadFileTree(string path)
        {
            try
            {
                FileTreeView.Items.Clear();
                
                var dirInfo = new DirectoryInfo(path);
                var rootItem = CreateDirectoryNode(dirInfo);
                if (rootItem != null)
                {
                    FileTreeView.Items.Add(rootItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载目录失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private TreeViewItem CreateDirectoryNode(DirectoryInfo dirInfo)
        {
            var item = new TreeViewItem
            {
                Header = $"📁 {dirInfo.Name}",
                Tag = dirInfo.FullName
            };

            try
            {
                // 添加子目录
                var dirs = dirInfo.GetDirectories().Take(50); // 限制数量避免卡顿
                foreach (var dir in dirs)
                {
                    if (!dir.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        var subItem = new TreeViewItem
                        {
                            Header = $"📁 {dir.Name}",
                            Tag = dir.FullName
                        };
                        subItem.Items.Add(null); // 占位符，用于显示展开按钮
                        item.Items.Add(subItem);
                    }
                }

                // 添加文件
                var files = dirInfo.GetFiles().Take(50);
                foreach (var file in files)
                {
                    if (!file.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        var fileItem = new TreeViewItem
                        {
                            Header = $"📄 {file.Name}",
                            Tag = file.FullName
                        };
                        item.Items.Add(fileItem);
                    }
                }
            }
            catch { }

            // 展开事件
            item.Expanded += (s, e) =>
            {
                var tvi = s as TreeViewItem;
                if (tvi.Items.Count == 1 && tvi.Items[0] == null)
                {
                    tvi.Items.Clear();
                    var dir = new DirectoryInfo(tvi.Tag.ToString());
                    try
                    {
                        foreach (var subDir in dir.GetDirectories().Take(50))
                        {
                            if (!subDir.Attributes.HasFlag(FileAttributes.Hidden))
                            {
                                var subItem = new TreeViewItem
                                {
                                    Header = $"📁 {subDir.Name}",
                                    Tag = subDir.FullName
                                };
                                subItem.Items.Add(null);
                                tvi.Items.Add(subItem);
                            }
                        }

                        foreach (var file in dir.GetFiles().Take(50))
                        {
                            if (!file.Attributes.HasFlag(FileAttributes.Hidden))
                            {
                                var fileItem = new TreeViewItem
                                {
                                    Header = $"📄 {file.Name}",
                                    Tag = file.FullName
                                };
                                tvi.Items.Add(fileItem);
                            }
                        }
                    }
                    catch { }
                }
            };

            return item;
        }

        private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item && item.Tag != null)
            {
                var path = item.Tag.ToString();
                if (File.Exists(path))
                {
                    _selectedTestFiles.Clear();
                    _selectedTestFiles.Add(path);
                }
                else if (Directory.Exists(path))
                {
                    _selectedTestFiles.Clear();
                    _selectedTestFiles.Add(path);
                }
            }
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            _selectedTestFiles.Clear();
            MessageBox.Show("已取消选择", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SelectFolderBackground_Click(object sender, RoutedEventArgs e)
        {
            _selectedTestFiles.Clear();
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择文件夹"
            };
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _selectedTestFiles.Add(dialog.SelectedPath);
                LoadFileTree(dialog.SelectedPath);
                MessageBox.Show($"已选择文件夹背景: {dialog.SelectedPath}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SelectDesktop_Click(object sender, RoutedEventArgs e)
        {
            _selectedTestFiles.Clear();
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            _selectedTestFiles.Add(desktop);
            LoadFileTree(desktop);
            MessageBox.Show($"已选择桌面: {desktop}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshFileTree_Click(object sender, RoutedEventArgs e)
        {
            if (FileTreeView.Items.Count > 0 && FileTreeView.Items[0] is TreeViewItem rootItem)
            {
                var rootPath = rootItem.Tag?.ToString();
                if (!string.IsNullOrEmpty(rootPath))
                {
                    LoadFileTree(rootPath);
                }
            }
            else
            {
                InitializeFileTree();
            }
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

        private void MenuTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void MenuTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var mousePos = e.GetPosition(null);
                var diff = _dragStartPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    var treeView = sender as TreeView;
                    var treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                    if (treeViewItem != null && _viewModel.SelectedItem != null)
                    {
                        DragDrop.DoDragDrop(treeViewItem, _viewModel.SelectedItem, DragDropEffects.Move);
                    }
                }
            }
        }

        private void MenuTreeView_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(MenuItemConfig)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void MenuTreeView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(MenuItemConfig)))
            {
                var draggedItem = e.Data.GetData(typeof(MenuItemConfig)) as MenuItemConfig;
                var targetItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
                
                // TODO: 实现拖拽后的重新排序逻辑
                MessageBox.Show("拖拽排序功能开发中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

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
