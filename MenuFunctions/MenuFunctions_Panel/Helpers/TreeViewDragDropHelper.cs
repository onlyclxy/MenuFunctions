using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using MenuFunctions_Panel.Models;
using MenuFunctions_Panel.ViewModels;

namespace MenuFunctions_Panel.Helpers
{
    /// <summary>
    /// TreeView 拖拽辅助类 - 改进版，使用 Adorner 显示拖放指示线
    /// </summary>
    public class TreeViewDragDropHelper
    {
        private TreeView _treeView;
        private MainViewModel _viewModel;
        private Point _startPoint;
        private bool _isDragging;
        private MenuItemConfig _draggedItem;
        
        // 拖拽指示器
        private DropLineAdorner _dropAdorner;
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
            
            // 注册事件
            _treeView.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            _treeView.PreviewMouseMove += OnPreviewMouseMove;
            _treeView.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            _treeView.DragOver += OnDragOver;
            _treeView.Drop += OnDrop;
            _treeView.DragLeave += OnDragLeave;
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            var item = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (item != null)
            {
                _draggedItem = item.DataContext as MenuItemConfig;
            }
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging && _draggedItem != null)
            {
                Point currentPosition = e.GetPosition(null);
                Vector diff = _startPoint - currentPosition;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDragging = true;
                    DragDrop.DoDragDrop(_treeView, _draggedItem, DragDropEffects.Move);
                    _isDragging = false;
                    HideDropIndicator();
                    _draggedItem = null;
                }
            }
        }

        private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            HideDropIndicator();
            _draggedItem = null;
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
                HideDropIndicator();
            }
            e.Handled = true;
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            HideDropIndicator();
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
            _draggedItem = null;
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
                    var itemConfig = item.DataContext as MenuItemConfig;
                    var draggedItem = e.Data.GetData(typeof(MenuItemConfig)) as MenuItemConfig;
                    
                    // 不能拖到自己或自己的子项
                    if (draggedItem != null && IsDescendantOf(draggedItem, itemConfig))
                    {
                        HideDropIndicator();
                        return;
                    }

                    _targetItem = item;
                    var itemPosition = e.GetPosition(item);
                    var itemHeight = item.ActualHeight;

                    // 确定拖拽位置（上方、内部、下方）
                    // 使用更细的阈值，让横线更容易出现
                    double threshold = itemHeight * 0.3;
                    
                    if (itemPosition.Y < threshold)
                    {
                        _dropPosition = DropPosition.Before;
                        ShowDropIndicator(item, true); // 上方
                    }
                    else if (itemPosition.Y > itemHeight - threshold)
                    {
                        _dropPosition = DropPosition.After;
                        ShowDropIndicator(item, false); // 下方
                    }
                    else
                    {
                        _dropPosition = DropPosition.Inside;
                        ShowDropIndicator(item, null); // 作为子项
                    }
                }
                else
                {
                    HideDropIndicator();
                }
            }
            else
            {
                HideDropIndicator();
            }
        }

        private bool IsDescendantOf(MenuItemConfig ancestor, MenuItemConfig item)
        {
            if (item == null || ancestor == null) return false;
            if (ancestor == item) return true;
            
            if (ancestor.SubItems != null)
            {
                foreach (var subItem in ancestor.SubItems)
                {
                    if (IsDescendantOf(subItem, item)) return true;
                }
            }
            return false;
        }

        private void ShowDropIndicator(TreeViewItem item, bool? showAbove)
        {
            try
            {
                // 移除旧的指示器
                HideDropIndicator();

                // 获取 AdornerLayer
                var adornerLayer = AdornerLayer.GetAdornerLayer(_treeView);
                if (adornerLayer == null)
                {
                    // 如果 TreeView 没有 AdornerLayer，尝试从父容器获取
                    var parent = VisualTreeHelper.GetParent(_treeView) as Visual;
                    while (parent != null && adornerLayer == null)
                    {
                        adornerLayer = AdornerLayer.GetAdornerLayer(parent);
                        parent = VisualTreeHelper.GetParent(parent) as Visual;
                    }
                }

                if (adornerLayer != null)
                {
                    if (showAbove.HasValue)
                    {
                        // 显示横线（在项目上方或下方）
                        _dropAdorner = new DropLineAdorner(_treeView, item, showAbove.Value);
                        adornerLayer.Add(_dropAdorner);
                    }
                    else
                    {
                        // 显示高亮（作为子项）
                        _dropAdorner = new DropLineAdorner(_treeView, item, null);
                        adornerLayer.Add(_dropAdorner);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示拖放指示器失败: {ex.Message}");
            }
        }

        private void HideDropIndicator()
        {
            if (_dropAdorner != null)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(_treeView);
                if (adornerLayer == null)
                {
                    var parent = VisualTreeHelper.GetParent(_treeView) as Visual;
                    while (parent != null && adornerLayer == null)
                    {
                        adornerLayer = AdornerLayer.GetAdornerLayer(parent);
                        parent = VisualTreeHelper.GetParent(parent) as Visual;
                    }
                }
                
                if (adornerLayer != null)
                {
                    adornerLayer.Remove(_dropAdorner);
                }
                _dropAdorner = null;
            }
            
            // 移除所有高亮
            if (_targetItem != null)
            {
                _targetItem.Background = Brushes.Transparent;
            }
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

    /// <summary>
    /// 拖放指示线 Adorner
    /// </summary>
    public class DropLineAdorner : Adorner
    {
        private TreeViewItem _targetItem;
        private bool? _showAbove;
        private Pen _linePen;
        private Brush _highlightBrush;

        public DropLineAdorner(UIElement adornedElement, TreeViewItem targetItem, bool? showAbove) 
            : base(adornedElement)
        {
            _targetItem = targetItem;
            _showAbove = showAbove;
            
            // 蓝色横线
            _linePen = new Pen(new SolidColorBrush(Color.FromRgb(33, 150, 243)), 2);
            _linePen.Freeze();
            
            // 高亮背景
            _highlightBrush = new SolidColorBrush(Color.FromArgb(30, 33, 150, 243));
            _highlightBrush.Freeze();
            
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_targetItem == null) return;

            try
            {
                // 获取目标项在 TreeView 中的位置
                var transform = _targetItem.TransformToAncestor(AdornedElement);
                var itemRect = new Rect(0, 0, _targetItem.ActualWidth, _targetItem.ActualHeight);
                var transformedRect = transform.TransformBounds(itemRect);

                if (_showAbove.HasValue)
                {
                    // 绘制横线
                    double y = _showAbove.Value ? transformedRect.Top : transformedRect.Bottom;
                    
                    // 计算缩进（根据层级）
                    int indent = GetItemIndent(_targetItem);
                    double indentWidth = indent * 20; // 每层缩进 20 像素
                    
                    // 横线从左缩进位置到右边界
                    double x1 = transformedRect.Left + indentWidth;
                    double x2 = transformedRect.Right;
                    
                    drawingContext.DrawLine(_linePen, new Point(x1, y), new Point(x2, y));
                }
                else
                {
                    // 绘制高亮背景（作为子项）
                    drawingContext.DrawRectangle(_highlightBrush, null, transformedRect);
                    
                    // 也绘制一个左侧的竖线表示缩进
                    int indent = GetItemIndent(_targetItem);
                    double indentWidth = indent * 20;
                    double lineX = transformedRect.Left + indentWidth - 10;
                    
                    drawingContext.DrawLine(_linePen, 
                        new Point(lineX, transformedRect.Top), 
                        new Point(lineX, transformedRect.Bottom));
                }
            }
            catch { }
        }

        private int GetItemIndent(TreeViewItem item)
        {
            int indent = 0;
            DependencyObject parent = VisualTreeHelper.GetParent(item);
            var treeView = AdornedElement as TreeView;
            
            while (parent != null && parent != treeView)
            {
                if (parent is TreeViewItem)
                {
                    indent++;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            
            return indent;
        }
    }
}
