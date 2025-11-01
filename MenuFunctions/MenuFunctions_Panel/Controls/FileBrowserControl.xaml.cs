using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using TreeNode = System.Windows.Forms.TreeNode;

namespace MenuFunctions_Panel.Controls
{
    /// <summary>
    /// FileBrowserControl.xaml 的交互逻辑 - 使用 Windows Forms TreeView
    /// </summary>
    public partial class FileBrowserControl : System.Windows.Controls.UserControl
    {
        public event EventHandler<List<string>> SelectionChanged;
        
        private List<string> _selectedPaths = new List<string>();
        private string _currentPath;
        private System.Windows.Forms.TreeView _treeView;

        public FileBrowserControl()
        {
            InitializeComponent();
            
            // 延迟加载，等待控件完全初始化
            this.Loaded += FileBrowserControl_Loaded;
        }
        
        private void EnsureTreeView()
        {
            if (_treeView == null)
            {
                _treeView = TreeViewHost.Child as System.Windows.Forms.TreeView;
            }
        }
        
        private void FileBrowserControl_Loaded(object sender, RoutedEventArgs e)
        {
            EnsureTreeView();
            try
            {
                LoadDrives();
                UpdateSelectedPathDisplay(); // 初始化显示
            }
            catch (Exception ex)
            {
                // 显示错误节点
                var errorNode = new TreeNode($"⚠️ 加载失败: {ex.Message}")
                {
                    Tag = null
                };
                _treeView.Nodes.Add(errorNode);
            }
        }

        public List<string> SelectedPaths => _selectedPaths;
        public string CurrentPath => _currentPath;

        /// <summary>
        /// 加载所有驱动器
        /// </summary>
        private void LoadDrives()
        {
            EnsureTreeView();
            _treeView.Nodes.Clear();
            
            try
            {
                // 添加"此电脑"节点
                var computerNode = new TreeNode("💻 此电脑")
                {
                    Tag = "Computer"
                };

                // 获取所有驱动器，逐个处理避免单个驱动器错误影响整体
                try
                {
                    var drives = DriveInfo.GetDrives();
                    foreach (var drive in drives)
                    {
                        try
                        {
                            if (drive.IsReady)
                            {
                                var driveNode = CreateDriveNode(drive);
                                computerNode.Nodes.Add(driveNode);
                            }
                        }
                        catch
                        {
                            // 忽略单个驱动器的错误
                        }
                    }
                }
                catch
                {
                    var errorNode = new TreeNode("⚠️ 无法访问驱动器")
                    {
                        Tag = null
                    };
                    computerNode.Nodes.Add(errorNode);
                }

                if (computerNode.Nodes.Count > 0)
                {
                    _treeView.Nodes.Add(computerNode);
                    computerNode.Expand();
                }

                // 添加常用文件夹
                AddQuickAccessFolders();
            }
            catch (Exception ex)
            {
                var errorNode = new TreeNode($"⚠️ 初始化失败: {ex.Message}")
                {
                    Tag = null
                };
                _treeView.Nodes.Add(errorNode);
            }
        }

        /// <summary>
        /// 添加常用文件夹快速访问
        /// </summary>
        private void AddQuickAccessFolders()
        {
            var quickAccessNode = new TreeNode("⭐ 快速访问")
            {
                Tag = "QuickAccess"
            };

            // 桌面
            AddQuickAccessFolder(quickAccessNode, "🖥️ 桌面", Environment.SpecialFolder.Desktop);
            
            // 我的文档
            AddQuickAccessFolder(quickAccessNode, "📄 我的文档", Environment.SpecialFolder.MyDocuments);
            
            // 下载
            var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(downloadsPath))
            {
                var downloadsNode = new TreeNode("⬇️ 下载")
                {
                    Tag = downloadsPath
                };
                downloadsNode.Nodes.Add(new TreeNode("")); // 占位符
                quickAccessNode.Nodes.Add(downloadsNode);
            }

            _treeView.Nodes.Add(quickAccessNode);
            quickAccessNode.Expand();
        }

        private void AddQuickAccessFolder(TreeNode parent, string displayName, Environment.SpecialFolder folder)
        {
            try
            {
                var path = Environment.GetFolderPath(folder);
                if (Directory.Exists(path))
                {
                    var node = new TreeNode(displayName)
                    {
                        Tag = path
                    };
                    node.Nodes.Add(new TreeNode("")); // 占位符
                    parent.Nodes.Add(node);
                }
            }
            catch { }
        }

        /// <summary>
        /// 创建驱动器节点
        /// </summary>
        private TreeNode CreateDriveNode(DriveInfo drive)
        {
            string icon = drive.DriveType switch
            {
                DriveType.Fixed => "💾",
                DriveType.Removable => "💿",
                DriveType.Network => "🌐",
                DriveType.CDRom => "💿",
                _ => "📁"
            };

            string label = string.IsNullOrEmpty(drive.VolumeLabel) 
                ? drive.Name 
                : $"{drive.VolumeLabel} ({drive.Name.TrimEnd('\\')})";

            var node = new TreeNode($"{icon} {label}")
            {
                Tag = drive.RootDirectory.FullName
            };
            
            // 添加占位符节点以便展开
            node.Nodes.Add(new TreeNode(""));
            
            return node;
        }

        /// <summary>
        /// 加载目录内容
        /// </summary>
        private void LoadDirectory(TreeNode node)
        {
            if (node.Tag == null || node.Tag.ToString() == "Computer" || node.Tag.ToString() == "QuickAccess")
                return;

            string path = node.Tag.ToString();
            
            if (!Directory.Exists(path))
                return;

            try
            {
                // 清空占位符
                node.Nodes.Clear();

                // 添加子目录
                var dirs = Directory.GetDirectories(path);
                foreach (var dir in dirs.OrderBy(d => d))
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        if ((dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                            continue;

                        var dirNode = new TreeNode(dirInfo.Name)
                        {
                            Tag = dir
                        };
                        dirNode.Nodes.Add(new TreeNode("")); // 占位符
                        node.Nodes.Add(dirNode);
                    }
                    catch { }
                }

                // 添加文件
                var files = Directory.GetFiles(path);
                foreach (var file in files.OrderBy(f => f))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if ((fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                            continue;

                        var fileNode = new TreeNode($"📄 {fileInfo.Name}")
                        {
                            Tag = file
                        };
                        node.Nodes.Add(fileNode);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                node.Nodes.Add(new TreeNode($"⚠️ 错误: {ex.Message}"));
            }
        }

        /// <summary>
        /// TreeView 节点展开前事件
        /// </summary>
        private void FileTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var node = e.Node;
            if (node.Nodes.Count == 1 && node.Nodes[0].Tag == null && string.IsNullOrEmpty(node.Nodes[0].Text))
            {
                LoadDirectory(node);
            }
        }

        /// <summary>
        /// TreeView 节点选择事件
        /// </summary>
        private void FileTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var node = e.Node;
            if (node.Tag != null)
            {
                var path = node.Tag.ToString();
                if (path != "Computer" && path != "QuickAccess")
                {
                    _selectedPaths.Clear();
                    _selectedPaths.Add(path);
                    
                    // 更新当前路径
                    if (Directory.Exists(path))
                    {
                        _currentPath = path;
                    }
                    else if (File.Exists(path))
                    {
                        _currentPath = Path.GetDirectoryName(path);
                    }
                    
                    UpdateSelectedPathDisplay();
                    SelectionChanged?.Invoke(this, _selectedPaths);
                }
            }
        }

        /// <summary>
        /// TreeView 节点双击事件
        /// </summary>
        private void FileTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var node = e.Node;
            if (node.Tag != null)
            {
                var path = node.Tag.ToString();
                if (Directory.Exists(path))
                {
                    NavigateToPath(path);
                }
            }
        }

        /// <summary>
        /// 更新选择路径显示
        /// </summary>
        private void UpdateSelectedPathDisplay()
        {
            if (SelectedPathDisplay != null)
            {
                if (_selectedPaths.Count > 0)
                {
                    var firstPath = _selectedPaths[0];
                    if (File.Exists(firstPath))
                    {
                        SelectedPathDisplay.Text = $"📄 {Path.GetFileName(firstPath)}";
                    }
                    else if (Directory.Exists(firstPath))
                    {
                        SelectedPathDisplay.Text = $"📂 {Path.GetFileName(firstPath)}";
                    }
                    else
                    {
                        SelectedPathDisplay.Text = firstPath;
                    }
                }
                else if (!string.IsNullOrEmpty(_currentPath))
                {
                    SelectedPathDisplay.Text = $"📂 {Path.GetFileName(_currentPath)} (当前目录)";
                }
                else
                {
                    SelectedPathDisplay.Text = "无";
                }
            }
        }

        /// <summary>
        /// 导航到指定路径
        /// </summary>
        public void NavigateToPath(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    _currentPath = path;
                    AddressBar.Text = path;
                    
                    // 查找并展开路径
                    ExpandPath(path);
                    
                    // 如果导航到文件夹，不清空选择，但更新显示
                    UpdateSelectedPathDisplay();
                }
                else if (File.Exists(path))
                {
                    _selectedPaths.Clear();
                    _selectedPaths.Add(path);
                    _currentPath = Path.GetDirectoryName(path);
                    UpdateSelectedPathDisplay();
                    SelectionChanged?.Invoke(this, _selectedPaths);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"导航失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 展开并选中指定路径
        /// </summary>
        private void ExpandPath(string path)
        {
            EnsureTreeView();
            try
            {
                if (string.IsNullOrEmpty(path))
                    return;

                // 标准化路径
                path = Path.GetFullPath(path);
                
                // 解析路径部分
                var parts = new List<string>();
                if (path.Length >= 2 && path[1] == ':')
                {
                    // 驱动器路径，如 C:\
                    parts.Add(path.Substring(0, 2).ToUpper()); // C:
                    if (path.Length > 2)
                    {
                        var remaining = path.Substring(2).TrimStart('\\');
                        if (!string.IsNullOrEmpty(remaining))
                        {
                            parts.AddRange(remaining.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                .Where(p => !string.IsNullOrEmpty(p)));
                        }
                    }
                }
                else
                {
                    parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToList();
                }

                TreeNode currentNode = null;
                
                // 从根节点开始查找
                foreach (TreeNode rootNode in _treeView.Nodes)
                {
                    if (ExpandNodeRecursive(rootNode, parts, path, 0, ref currentNode))
                    {
                        break;
                    }
                }

                if (currentNode != null)
                {
                    _treeView.SelectedNode = currentNode;
                    currentNode.EnsureVisible();
                    
                    // 更新选中状态
                    var selectedPath = currentNode.Tag?.ToString();
                    if (selectedPath != null && (selectedPath == "Computer" || selectedPath == "QuickAccess"))
                    {
                        // 如果是根节点，不清空选择
                    }
                    else if (selectedPath != null)
                    {
                        _selectedPaths.Clear();
                        _selectedPaths.Add(selectedPath);
                        UpdateSelectedPathDisplay();
                        SelectionChanged?.Invoke(this, _selectedPaths);
                    }
                }
                else
                {
                    // 如果没找到节点，至少更新显示
                    UpdateSelectedPathDisplay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"展开路径失败: {ex.Message}");
            }
        }

        private bool ExpandNodeRecursive(TreeNode node, List<string> parts, string fullPath, int index, ref TreeNode foundNode)
        {
            if (node.Tag == null)
                return false;

            string nodePath = node.Tag.ToString();
            if (nodePath == "Computer" || nodePath == "QuickAccess")
            {
                // 展开根节点
                if (!node.IsExpanded)
                    node.Expand();
                
                foreach (TreeNode child in node.Nodes)
                {
                    if (ExpandNodeRecursive(child, parts, fullPath, index, ref foundNode))
                        return true;
                }
                return false;
            }

            // 检查当前节点是否匹配路径的一部分
            string nodeName = Path.GetFileName(nodePath);
            if (string.IsNullOrEmpty(nodeName))
            {
                // 驱动器节点，如 C:\
                nodeName = nodePath.Length >= 2 ? nodePath.Substring(0, 2) : nodePath;
            }
            
            if (index < parts.Count && nodeName.Equals(parts[index], StringComparison.OrdinalIgnoreCase))
            {
                // 匹配成功，展开节点
                if (!node.IsExpanded)
                {
                    LoadDirectory(node);
                    node.Expand();
                }

                // 检查是否到达目标路径
                if (index == parts.Count - 1)
                {
                    // 验证完整路径是否匹配
                    if (nodePath.Equals(fullPath, StringComparison.OrdinalIgnoreCase) || 
                        Path.GetFullPath(nodePath).Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        foundNode = node;
                        return true;
                    }
                }

                // 继续查找子节点
                foreach (TreeNode child in node.Nodes)
                {
                    if (ExpandNodeRecursive(child, parts, fullPath, index + 1, ref foundNode))
                        return true;
                }
            }

            return false;
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            var path = AddressBar.Text;
            if (!string.IsNullOrWhiteSpace(path))
            {
                NavigateToPath(path);
            }
        }

        private void AddressBar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Navigate_Click(sender, e);
            }
        }

        private void GoUp_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentPath))
            {
                var parentPath = Path.GetDirectoryName(_currentPath);
                if (!string.IsNullOrEmpty(parentPath))
                {
                    NavigateToPath(parentPath);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            EnsureTreeView();
            if (_treeView.SelectedNode != null && _treeView.SelectedNode.Tag != null)
            {
                var path = _treeView.SelectedNode.Tag.ToString();
                if (Directory.Exists(path))
                {
                    LoadDirectory(_treeView.SelectedNode);
                }
            }
            else
            {
                LoadDrives();
            }
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            EnsureTreeView();
            // 只取消选中文件，但保留当前目录
            _selectedPaths.Clear();
            _treeView.SelectedNode = null;
            UpdateSelectedPathDisplay();
            SelectionChanged?.Invoke(this, _selectedPaths);
        }

        private void SelectDesktop_Click(object sender, RoutedEventArgs e)
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            NavigateToPath(desktop);
        }

        private void SelectDocuments_Click(object sender, RoutedEventArgs e)
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            NavigateToPath(documents);
        }

        private void SelectDownloads_Click(object sender, RoutedEventArgs e)
        {
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(downloads))
            {
                NavigateToPath(downloads);
            }
        }

        /// <summary>
        /// 公开方法：清空选择
        /// </summary>
        public void ClearCurrentSelection()
        {
            EnsureTreeView();
            _selectedPaths.Clear();
            _treeView.SelectedNode = null;
        }
    }
}
