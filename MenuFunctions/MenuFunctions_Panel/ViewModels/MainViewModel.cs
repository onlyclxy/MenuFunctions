using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using MenuFunctions_Panel.Models;
using Newtonsoft.Json;

namespace MenuFunctions_Panel.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<MenuItemConfig> _menuItems;
        private MenuItemConfig _selectedItem;
        private string _configFilePath;
        private string _fileTypesText;

        public ObservableCollection<MenuItemConfig> MenuItems
        {
            get => _menuItems;
            set { _menuItems = value; OnPropertyChanged(); }
        }

        public MenuItemConfig SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
                UpdateFileTypesText();
            }
        }

        public string FileTypesText
        {
            get => _fileTypesText;
            set
            {
                _fileTypesText = value;
                OnPropertyChanged();
                UpdateFileTypesList();
            }
        }

        public string ConfigFilePath
        {
            get => _configFilePath;
            set { _configFilePath = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel()
        {
            MenuItems = new ObservableCollection<MenuItemConfig>();
        }

        private void UpdateFileTypesText()
        {
            if (SelectedItem?.FileTypes != null && SelectedItem.FileTypes.Count > 0)
            {
                FileTypesText = string.Join(", ", SelectedItem.FileTypes);
            }
            else
            {
                FileTypesText = "";
            }
        }

        private void UpdateFileTypesList()
        {
            if (SelectedItem != null)
            {
                if (string.IsNullOrWhiteSpace(FileTypesText))
                {
                    SelectedItem.FileTypes = new System.Collections.Generic.List<string>();
                }
                else
                {
                    var types = FileTypesText.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(s => s.Trim())
                                             .ToList();
                    SelectedItem.FileTypes = types;
                }
            }
        }

        public void LoadConfig(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"配置文件不存在: {filePath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var json = File.ReadAllText(filePath);
                var items = JsonConvert.DeserializeObject<ObservableCollection<MenuItemConfig>>(json);

                if (items != null)
                {
                    // 转换子项为 ObservableCollection
                    ConvertSubItemsToObservable(items);
                    MenuItems = items;
                    ConfigFilePath = filePath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载配置文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConvertSubItemsToObservable(ObservableCollection<MenuItemConfig> items)
        {
            foreach (var item in items)
            {
                if (item.SubItems != null && !(item.SubItems is ObservableCollection<MenuItemConfig>))
                {
                    var observableSubItems = new ObservableCollection<MenuItemConfig>(item.SubItems);
                    item.SubItems = observableSubItems;
                }

                if (item.SubItems != null && item.SubItems.Count > 0)
                {
                    ConvertSubItemsToObservable(item.SubItems);
                }
            }
        }

        public void SaveConfig(string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(MenuItems, Formatting.Indented);
                File.WriteAllText(filePath, json);
                ConfigFilePath = filePath;
                MessageBox.Show("配置保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddMenuItem()
        {
            var newItem = new MenuItemConfig
            {
                Text = "新建菜单项",
                IsVisible = true,
                ShowOnFiles = true,
                ShowOnFolderBackground = true,
                ShowOnFolder = true,
                UseQuotes = true,
                AppendCommandToEachPath = true,
                Order = MenuItems.Count
            };

            MenuItems.Add(newItem);
            SelectedItem = newItem;
        }

        public void AddSubMenuItem()
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("请先选择一个父菜单项", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newItem = new MenuItemConfig
            {
                Text = "新建子菜单项",
                IsVisible = true,
                ShowOnFiles = true,
                ShowOnFolderBackground = true,
                ShowOnFolder = true,
                UseQuotes = true,
                AppendCommandToEachPath = true,
                Order = SelectedItem.SubItems.Count
            };

            if (SelectedItem.SubItems == null)
            {
                SelectedItem.SubItems = new ObservableCollection<MenuItemConfig>();
            }

            SelectedItem.SubItems.Add(newItem);
        }

        public void DeleteMenuItem()
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("请先选择要删除的菜单项", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要删除菜单项 '{SelectedItem.Text}' 吗？", 
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                RemoveItemFromCollection(MenuItems, SelectedItem);
            }
        }

        private bool RemoveItemFromCollection(ObservableCollection<MenuItemConfig> collection, MenuItemConfig item)
        {
            if (collection.Contains(item))
            {
                collection.Remove(item);
                return true;
            }

            foreach (var parentItem in collection)
            {
                if (parentItem.SubItems != null && RemoveItemFromCollection(parentItem.SubItems, item))
                {
                    return true;
                }
            }

            return false;
        }

        public void MoveUp()
        {
            if (SelectedItem == null) return;

            var collection = FindParentCollection(MenuItems, SelectedItem);
            if (collection != null)
            {
                var index = collection.IndexOf(SelectedItem);
                if (index > 0)
                {
                    collection.Move(index, index - 1);
                    UpdateOrder(collection);
                }
            }
        }

        public void MoveDown()
        {
            if (SelectedItem == null) return;

            var collection = FindParentCollection(MenuItems, SelectedItem);
            if (collection != null)
            {
                var index = collection.IndexOf(SelectedItem);
                if (index < collection.Count - 1)
                {
                    collection.Move(index, index + 1);
                    UpdateOrder(collection);
                }
            }
        }

        private ObservableCollection<MenuItemConfig> FindParentCollection(
            ObservableCollection<MenuItemConfig> collection, MenuItemConfig item)
        {
            if (collection.Contains(item))
            {
                return collection;
            }

            foreach (var parentItem in collection)
            {
                if (parentItem.SubItems != null)
                {
                    var result = FindParentCollection(parentItem.SubItems, item);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        private void UpdateOrder(ObservableCollection<MenuItemConfig> collection)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                collection[i].Order = i;
            }
        }

        public void AddSeparator()
        {
            var separator = new MenuItemConfig
            {
                Text = "分隔线",
                IsSeparator = true,
                IsVisible = true,
                Order = MenuItems.Count
            };

            MenuItems.Add(separator);
        }
    }
}

