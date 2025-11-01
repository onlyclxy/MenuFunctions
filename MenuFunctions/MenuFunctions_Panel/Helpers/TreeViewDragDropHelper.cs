using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MenuFunctions_Panel.Models;
using MenuFunctions_Panel.ViewModels;

namespace MenuFunctions_Panel.Helpers
{
    /// <summary>
    /// TreeView 拖拽辅助类
    /// </summary>
    public class TreeViewDragDropHelper
    {
        private TreeView _treeView;
        private MainViewModel _viewModel;
        private Point _startPoint;
        private bool _isDragging;
        private MenuItemConfig _draggedItem;
        
        // 拖拽指示器
        private Line _dropIndicator;
        private TreeViewItem _targetItem;
        private DropPosition _dropPosition;

        public enum DropPosition
        {
            None,
            Before,
            Inside,
            After
        }

        public TreeViewDragDropHelper(TreeView treeView, MainViewModel viewModel)
        {
            _treeView = treeView;
            _viewModel = viewModel;
            
            // 创建拖拽指示线
            _dropIndicator = new Line
            {
                Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)), // 蓝色
                StrokeThickness = 2,
                Visibility = Visibility.Collapsed
            };

            // 注册事件
            _treeView.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            _treeView.PreviewMouseMove += OnPreviewMouseMove;
            _treeView.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            _treeView.DragOver += OnDragOver;
            _treeView.Drop += OnDrop;
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            _draggedItem = _viewModel.SelectedItem;
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                Point currentPosition = e.GetPosition(null);
                Vector diff = _startPoint - currentPosition;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (_draggedItem != null)
                    {
                        _isDragging = true;
                        DragDrop.DoDragDrop(_treeView, _draggedItem, DragDropEffects.Move);
                        _isDragging = false;
                        HideDropIndicator();
                    }
                }
            }
        }

        private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            HideDropIndicator();
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(MenuItemConfig)))
            {
                e.Effects = DragDropEffects.Move;
                UpdateDropIndicator(e);
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            HideDropIndicator();

            if (e.Data.GetDataPresent(typeof(MenuItemConfig)) && _targetItem != null)
            {
                var draggedItem = e.Data.GetData(typeof(MenuItemConfig)) as MenuItemConfig;
                var targetConfig = _targetItem.DataContext as MenuItemConfig;

                if (draggedItem != null && targetConfig != null && draggedItem != targetConfig)
                {
                    PerformDrop(draggedItem, targetConfig, _dropPosition);
                }
            }

            _targetItem = null;
            _dropPosition = DropPosition.None;
        }

        private void UpdateDropIndicator(DragEventArgs e)
        {
            var position = e.GetPosition(_treeView);
            var hitTestResult = VisualTreeHelper.HitTest(_treeView, position);

            if (hitTestResult != null)
            {
                var item = FindAncestor<TreeViewItem>(hitTestResult.VisualHit);
                
                if (item != null)
                {
                    _targetItem = item;
                    var itemPosition = e.GetPosition(item);
                    var itemHeight = item.ActualHeight;

                    // 确定拖拽位置（上方、内部、下方）
                    if (itemPosition.Y < itemHeight * 0.25)
                    {
                        _dropPosition = DropPosition.Before;
                        ShowDropIndicator(item, true); // 上方
                    }
                    else if (itemPosition.Y > itemHeight * 0.75)
                    {
                        _dropPosition = DropPosition.After;
                        ShowDropIndicator(item, false); // 下方
                    }
                    else
                    {
                        _dropPosition = DropPosition.Inside;
                        HighlightItem(item); // 高亮显示可以放入子项
                    }
                }
            }
        }

        private void ShowDropIndicator(TreeViewItem item, bool showAbove)
        {
            try
            {
                // 移除旧的指示器
                RemoveDropIndicator();

                // 获取 TreeViewItem 的位置
                var transform = item.TransformToAncestor(_treeView);
                var itemPosition = transform.Transform(new Point(0, 0));

                // 计算缩进（层级）
                int indent = GetItemIndent(item);
                double indentWidth = indent * 20; // 每层缩进 20 像素

                // 创建新的指示线
                _dropIndicator = new Line
                {
                    X1 = indentWidth,
                    X2 = item.ActualWidth,
                    Y1 = showAbove ? itemPosition.Y : itemPosition.Y + item.ActualHeight,
                    Y2 = showAbove ? itemPosition.Y : itemPosition.Y + item.ActualHeight,
                    Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                    StrokeThickness = 2,
                    Visibility = Visibility.Visible
                };

                // 添加到 TreeView
                var adornerLayer = AdornerLayer.GetAdornerLayer(_treeView);
                if (adornerLayer == null)
                {
                    // 如果没有 AdornerLayer，直接添加到 TreeView（作为备选方案）
                    if (_treeView.Parent is Panel panel)
                    {
                        panel.Children.Add(_dropIndicator);
                    }
                }
            }
            catch { }
        }

        private void HighlightItem(TreeViewItem item)
        {
            // 高亮显示项目（可以添加到子项）
            HideDropIndicator();
            item.Background = new SolidColorBrush(Color.FromArgb(50, 33, 150, 243));
        }

        private void HideDropIndicator()
        {
            RemoveDropIndicator();
            
            // 移除所有高亮
            if (_targetItem != null)
            {
                _targetItem.Background = Brushes.Transparent;
            }
        }

        private void RemoveDropIndicator()
        {
            if (_dropIndicator != null && _dropIndicator.Parent is Panel panel)
            {
                panel.Children.Remove(_dropIndicator);
            }
            
            if (_dropIndicator != null)
            {
                _dropIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private int GetItemIndent(TreeViewItem item)
        {
            int indent = 0;
            DependencyObject parent = VisualTreeHelper.GetParent(item);
            
            while (parent != null && parent != _treeView)
            {
                if (parent is TreeViewItem)
                {
                    indent++;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            
            return indent;
        }

        private void PerformDrop(MenuItemConfig draggedItem, MenuItemConfig targetItem, DropPosition position)
        {
            // 从原位置移除
            var sourceCollection = _viewModel.FindParentCollection(_viewModel.MenuItems, draggedItem);
            if (sourceCollection == null) return;

            sourceCollection.Remove(draggedItem);

            // 添加到新位置
            if (position == DropPosition.Inside)
            {
                // 作为子项添加
                if (targetItem.SubItems == null)
                {
                    targetItem.SubItems = new System.Collections.ObjectModel.ObservableCollection<MenuItemConfig>();
                }
                targetItem.SubItems.Add(draggedItem);
            }
            else
            {
                // 作为同级添加
                var targetCollection = _viewModel.FindParentCollection(_viewModel.MenuItems, targetItem);
                if (targetCollection != null)
                {
                    int targetIndex = targetCollection.IndexOf(targetItem);
                    
                    if (position == DropPosition.After)
                    {
                        targetIndex++;
                    }
                    
                    targetCollection.Insert(targetIndex, draggedItem);
                }
            }

            // 更新排序
            UpdateOrder(sourceCollection);
            if (position == DropPosition.Inside && targetItem.SubItems != null)
            {
                UpdateOrder(targetItem.SubItems);
            }
            else
            {
                var targetCollection = _viewModel.FindParentCollection(_viewModel.MenuItems, targetItem);
                if (targetCollection != null)
                {
                    UpdateOrder(targetCollection);
                }
            }
        }

        private void UpdateOrder(System.Collections.ObjectModel.ObservableCollection<MenuItemConfig> collection)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                collection[i].Order = i;
            }
        }

        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            
            return null;
        }
    }
}

