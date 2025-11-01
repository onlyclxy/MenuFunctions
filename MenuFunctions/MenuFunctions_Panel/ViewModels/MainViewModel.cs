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
        
        // 撤销/恢复堆栈
        private readonly System.Collections.Generic.Stack<string> _undoStack = new System.Collections.Generic.Stack<string>();
        private readonly System.Collections.Generic.Stack<string> _redoStack = new System.Collections.Generic.Stack<string>();
        private const int MaxHistorySize = 50;

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

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

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
                    
                    // 清空撤销/恢复历史记录
                    ClearHistory();
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
            SaveState();
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

            SaveState();
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
                SaveState();
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
                    SaveState();
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
                    SaveState();
                    collection.Move(index, index + 1);
                    UpdateOrder(collection);
                }
            }
        }

        public ObservableCollection<MenuItemConfig> FindParentCollection(
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
            SaveState();
            var separator = new MenuItemConfig
            {
                Text = "分隔线",
                IsSeparator = true,
                IsVisible = true,
                Order = MenuItems.Count
            };

            MenuItems.Add(separator);
        }

        #region 撤销/恢复功能

        /// <summary>
        /// 保存当前状态到撤销堆栈
        /// </summary>
        public void SaveState()
        {
            try
            {
                var json = JsonConvert.SerializeObject(MenuItems, Formatting.None);
                
                // 限制历史记录大小
                if (_undoStack.Count >= MaxHistorySize)
                {
                    var temp = new System.Collections.Generic.Stack<string>();
                    while (_undoStack.Count > MaxHistorySize - 1)
                    {
                        _undoStack.Pop();
                    }
                    while (_undoStack.Count > 0)
                    {
                        temp.Push(_undoStack.Pop());
                    }
                    while (temp.Count > 0)
                    {
                        _undoStack.Push(temp.Pop());
                    }
                }
                
                _undoStack.Push(json);
                _redoStack.Clear(); // 执行新操作时清除重做堆栈
                
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 撤销操作
        /// </summary>
        public void Undo()
        {
            if (_undoStack.Count == 0) return;

            try
            {
                // 保存当前状态到重做堆栈
                var currentJson = JsonConvert.SerializeObject(MenuItems, Formatting.None);
                _redoStack.Push(currentJson);

                // 恢复上一个状态
                var previousJson = _undoStack.Pop();
                var items = JsonConvert.DeserializeObject<ObservableCollection<MenuItemConfig>>(previousJson);

                if (items != null)
                {
                    ConvertSubItemsToObservable(items);
                    MenuItems = items;
                    
                    // 清除选中项，避免引用已删除的对象
                    SelectedItem = null;
                }

                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"撤销失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 恢复操作
        /// </summary>
        public void Redo()
        {
            if (_redoStack.Count == 0) return;

            try
            {
                // 保存当前状态到撤销堆栈
                var currentJson = JsonConvert.SerializeObject(MenuItems, Formatting.None);
                _undoStack.Push(currentJson);

                // 恢复下一个状态
                var nextJson = _redoStack.Pop();
                var items = JsonConvert.DeserializeObject<ObservableCollection<MenuItemConfig>>(nextJson);

                if (items != null)
                {
                    ConvertSubItemsToObservable(items);
                    MenuItems = items;
                    
                    // 清除选中项
                    SelectedItem = null;
                }

                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"恢复失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 清空撤销/恢复历史记录
        /// </summary>
        public void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        #endregion
    }
}

