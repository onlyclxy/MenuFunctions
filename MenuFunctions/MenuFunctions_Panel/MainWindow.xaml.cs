using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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

        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;

            // 尝试自动加载配置文件
            AutoLoadConfig();
        }

        private void AutoLoadConfig()
        {
            // 尝试从 MenuFunctions 项目的 bin/Release 目录加载配置
            var possiblePaths = new[]
            {
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MenuConfig.json"),
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "MenuFunctions", "bin", "Release", "MenuConfig.json"),
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "MenuFunctions", "bin", "Release", "MenuConfig.json")
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    var fullPath = System.IO.Path.GetFullPath(path);
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
            else
            {
                _viewModel.SaveConfig(_viewModel.ConfigFilePath);
            }
        }

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
                Filter = "可执行文件|*.exe|所有文件|*.*",
                Title = "选择程序"
            };

            if (dialog.ShowDialog() == true)
            {
                if (_viewModel.SelectedItem != null)
                {
                    _viewModel.SelectedItem.ProgramPath = dialog.FileName;
                }
            }
        }

        private void BrowseIcon_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "图标文件|*.ico;*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*",
                Title = "选择图标"
            };

            if (dialog.ShowDialog() == true)
            {
                if (_viewModel.SelectedItem != null)
                {
                    _viewModel.SelectedItem.IconPath = dialog.FileName;
                }
            }
        }
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
