using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RealtimeITagControl.FileExplorer
{
    /// <summary>
    /// 파일 탐색 컨트롤 (V20 스타일)
    /// TreeView (폴더) + ListView (파일)
    /// </summary>
    public class FileExplorerControl : UserControl
    {
        private SplitContainer splitContainer;
        private TreeView treeViewFolders;
        private ListView listViewFiles;
        private ToolStrip toolStrip;
        private ToolStripButton btnRefresh;
        private ToolStripButton btnBrowse;
        private ToolStripLabel lblCurrentPath;
        private ImageList imageListLarge;
        private ImageList imageListSmall;

        // 이벤트
        public event EventHandler<string> FileSelected;
        public event EventHandler<string> FileDoubleClicked;
        public event EventHandler<string> FolderChanged;

        private string currentPath;
        private string[] mpfExtensions = new string[] { ".mpf", ".MPF", ".txt", ".nc", ".NC" };

        public FileExplorerControl()
        {
            InitializeComponent();
            // Don't call InitializeTreeView here - it will be called from MainForm with saved path
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Image lists
            imageListSmall = new ImageList { ImageSize = new Size(16, 16) };
            imageListLarge = new ImageList { ImageSize = new Size(32, 32) };

            // Add default icons
            imageListSmall.Images.Add("folder", SystemIcons.WinLogo.ToBitmap());
            imageListSmall.Images.Add("file", SystemIcons.Application.ToBitmap());
            imageListLarge.Images.Add("folder", SystemIcons.WinLogo.ToBitmap());
            imageListLarge.Images.Add("file", SystemIcons.Application.ToBitmap());

            // ToolStrip
            toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden
            };

            btnRefresh = new ToolStripButton
            {
                Text = "새로고침",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Image = SystemIcons.Shield.ToBitmap()
            };
            btnRefresh.Click += BtnRefresh_Click;

            btnBrowse = new ToolStripButton
            {
                Text = "폴더 선택",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Image = SystemIcons.Asterisk.ToBitmap()
            };
            btnBrowse.Click += BtnBrowse_Click;

            // Spring label for spacing (pushes lblCurrentPath to right)
            ToolStripLabel spacer = new ToolStripLabel
            {
                Text = "",
                AutoSize = false,
                Width = 20
            };
            
            lblCurrentPath = new ToolStripLabel
            {
                Text = "경로: (선택 안 됨)",
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            toolStrip.Items.AddRange(new ToolStripItem[] { btnRefresh, btnBrowse, spacer, lblCurrentPath });

            // SplitContainer
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 200
            };

            // TreeView (폴더)
            treeViewFolders = new TreeView
            {
                Dock = DockStyle.Fill,
                ImageList = imageListSmall,
                ImageIndex = 0,
                SelectedImageIndex = 0
            };
            treeViewFolders.AfterSelect += TreeViewFolders_AfterSelect;
            treeViewFolders.BeforeExpand += TreeViewFolders_BeforeExpand;

            // ListView (파일)
            listViewFiles = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                SmallImageList = imageListSmall,
                LargeImageList = imageListLarge
            };

            listViewFiles.Columns.Add("파일명", 200);
            listViewFiles.Columns.Add("수정일", 150);
            listViewFiles.Columns.Add("크기", 100);
            listViewFiles.Columns.Add("경로", 300);

            listViewFiles.SelectedIndexChanged += ListViewFiles_SelectedIndexChanged;
            listViewFiles.DoubleClick += ListViewFiles_DoubleClick;

            splitContainer.Panel1.Controls.Add(treeViewFolders);
            splitContainer.Panel2.Controls.Add(listViewFiles);

            this.Controls.Add(splitContainer);
            this.Controls.Add(toolStrip);

            this.Size = new Size(400, 600);
            this.ResumeLayout(false);
        }

        /// <summary>
        /// Initialize TreeView with drives and optional initial path
        /// </summary>
        public void InitializeTreeView(string initialPath = null)
        {
            System.Diagnostics.Debug.WriteLine("[FileExplorer] InitializeTreeView called with path: " + (initialPath ?? "(null)"));
            
            treeViewFolders.Nodes.Clear();

            // Add drives
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    TreeNode driveNode = new TreeNode(drive.Name)
                    {
                        Tag = drive.RootDirectory.FullName,
                        ImageKey = "folder",
                        SelectedImageKey = "folder"
                    };

                    // Add dummy node for expansion
                    driveNode.Nodes.Add(new TreeNode("Loading..."));
                    treeViewFolders.Nodes.Add(driveNode);
                    System.Diagnostics.Debug.WriteLine("[FileExplorer] Added drive: " + drive.Name);
                }
            }

            // Navigate after control handle is created
            string pathToNavigate = null;
            if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
            {
                pathToNavigate = initialPath;
                System.Diagnostics.Debug.WriteLine("[FileExplorer] Will navigate to: " + initialPath);
            }
            else
            {
                string defaultPath = @"C:\ProgramData\Siemens\MotionControl\User\Sinumerik\Data\Prog";
                if (Directory.Exists(defaultPath))
                {
                    pathToNavigate = defaultPath;
                    System.Diagnostics.Debug.WriteLine("[FileExplorer] Will navigate to default: " + defaultPath);
                }
            }

            // Schedule navigation after handle is created
            if (pathToNavigate != null)
            {
                string finalPath = pathToNavigate;
                if (this.IsHandleCreated)
                {
                    // Handle already created, navigate directly
                    System.Diagnostics.Debug.WriteLine("[FileExplorer] Handle already created, navigating immediately");
                    NavigateToPath(finalPath);
                }
                else
                {
                    // Wait for handle creation
                    System.Diagnostics.Debug.WriteLine("[FileExplorer] Waiting for handle creation");
                    EventHandler handler = null;
                    handler = (sender, e) =>
                    {
                        this.HandleCreated -= handler; // Remove handler after first call
                        System.Diagnostics.Debug.WriteLine("[FileExplorer] Handle created, navigating now");
                        NavigateToPath(finalPath);
                    };
                    this.HandleCreated += handler;
                }
            }
        }

        /// <summary>
        /// Navigate to specific path
        /// </summary>
        public void NavigateToPath(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    System.Diagnostics.Debug.WriteLine("[FileExplorer] Path does not exist: " + path);
                    return;
                }

                System.Diagnostics.Debug.WriteLine("[FileExplorer] Navigating to: " + path);
                currentPath = path;
                lblCurrentPath.Text = "경로: " + path;

                // Find the drive node
                TreeNode currentNode = null;
                foreach (TreeNode driveNode in treeViewFolders.Nodes)
                {
                    string driveLetter = (driveNode.Tag as string);
                    if (path.StartsWith(driveLetter, StringComparison.OrdinalIgnoreCase))
                    {
                        currentNode = driveNode;
                        System.Diagnostics.Debug.WriteLine("[FileExplorer] Found drive node: " + driveLetter);
                        break;
                    }
                }

                if (currentNode != null)
                {
                    // Expand the drive node if needed
                    if (!currentNode.IsExpanded && currentNode.Nodes.Count > 0)
                    {
                        TreeViewFolders_BeforeExpand(null, new TreeViewCancelEventArgs(currentNode, false, TreeViewAction.Expand));
                        currentNode.Expand();
                    }

                    // Try to expand to the full path
                    string remainingPath = path.Substring(currentNode.Tag.ToString().Length);
                    if (!string.IsNullOrEmpty(remainingPath))
                    {
                        string[] folders = remainingPath.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (string folder in folders)
                        {
                            TreeNode foundNode = null;
                            foreach (TreeNode childNode in currentNode.Nodes)
                            {
                                if (childNode.Text.Equals(folder, StringComparison.OrdinalIgnoreCase))
                                {
                                    foundNode = childNode;
                                    break;
                                }
                            }

                            if (foundNode != null)
                            {
                                currentNode = foundNode;
                                if (!currentNode.IsExpanded && currentNode.Nodes.Count > 0)
                                {
                                    TreeViewFolders_BeforeExpand(null, new TreeViewCancelEventArgs(currentNode, false, TreeViewAction.Expand));
                                    currentNode.Expand();
                                }
                            }
                            else
                            {
                                // Can't expand further, just load files at current level
                                System.Diagnostics.Debug.WriteLine("[FileExplorer] Could not find subfolder: " + folder);
                                break;
                            }
                        }
                    }

                    // Select the final node and load files
                    treeViewFolders.SelectedNode = currentNode;
                    LoadFiles(currentPath);
                    System.Diagnostics.Debug.WriteLine("[FileExplorer] Navigation completed");
                    
                    // Raise FolderChanged event
                    if (FolderChanged != null)
                    {
                        FolderChanged(this, path);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[FileExplorer] Could not find drive node for path: " + path);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[FileExplorer] NavigateToPath error: " + ex.Message);
            }
        }

        /// <summary>
        /// TreeView before expand event
        /// </summary>
        private void TreeViewFolders_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode node = e.Node;
            
            // Remove dummy node
            if (node.Nodes.Count == 1 && node.Nodes[0].Text == "Loading...")
            {
                node.Nodes.Clear();
                string path = node.Tag as string;
                
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(path);
                    foreach (DirectoryInfo subDir in dir.GetDirectories())
                    {
                        TreeNode subNode = new TreeNode(subDir.Name)
                        {
                            Tag = subDir.FullName,
                            ImageKey = "folder",
                            SelectedImageKey = "folder"
                        };
                        subNode.Nodes.Add(new TreeNode("Loading..."));
                        node.Nodes.Add(subNode);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Ignore access denied
                }
                catch (Exception ex)
                {
                    node.Nodes.Add(new TreeNode("Error: " + ex.Message));
                }
            }
        }

        /// <summary>
        /// TreeView after select event
        /// </summary>
        private void TreeViewFolders_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string path = e.Node.Tag as string;
            if (!string.IsNullOrEmpty(path))
            {
                currentPath = path;
                lblCurrentPath.Text = "경로: " + path;
                LoadFiles(path);
                
                // Raise FolderChanged event
                if (FolderChanged != null)
                {
                    FolderChanged(this, path);
                }
            }
        }

        /// <summary>
        /// Load MPF files in directory
        /// </summary>
        private void LoadFiles(string path)
        {
            listViewFiles.Items.Clear();

            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                FileInfo[] files = dir.GetFiles();

                int count = 0;
                foreach (FileInfo file in files)
                {
                    if (IsMPFFile(file.Extension))
                    {
                        ListViewItem item = new ListViewItem(file.Name);
                        item.SubItems.Add(file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
                        item.SubItems.Add(FormatFileSize(file.Length));
                        item.SubItems.Add(file.FullName);
                        item.ImageKey = "file";
                        item.Tag = file.FullName;

                        listViewFiles.Items.Add(item);
                        count++;
                    }
                }

                lblCurrentPath.Text = string.Format("경로: {0} ({1}개 파일)", path, count);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("이 폴더에 접근할 권한이 없습니다.", "접근 거부", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("파일 로드 오류:\n" + ex.Message, "오류", 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Check if file extension is MPF
        /// </summary>
        private bool IsMPFFile(string extension)
        {
            return mpfExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Format file size
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024) + " KB";
            return (bytes / (1024 * 1024)) + " MB";
        }

        /// <summary>
        /// ListView selected index changed
        /// </summary>
        private void ListViewFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                string filePath = listViewFiles.SelectedItems[0].Tag as string;
                if (FileSelected != null)
                {
                    FileSelected(this, filePath);
                }
            }
        }

        /// <summary>
        /// ListView double click
        /// </summary>
        private void ListViewFiles_DoubleClick(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                string filePath = listViewFiles.SelectedItems[0].Tag as string;
                if (FileDoubleClicked != null)
                {
                    FileDoubleClicked(this, filePath);
                }
            }
        }

        /// <summary>
        /// Refresh button click
        /// </summary>
        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentPath))
            {
                LoadFiles(currentPath);
            }
        }

        /// <summary>
        /// Browse button click
        /// </summary>
        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "MPF 파일이 있는 폴더를 선택하세요";
                dialog.SelectedPath = currentPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    NavigateToPath(dialog.SelectedPath);
                    
                    // Raise FolderChanged event
                    if (FolderChanged != null)
                    {
                        FolderChanged(this, dialog.SelectedPath);
                    }
                }
            }
        }

        /// <summary>
        /// Get current selected file path
        /// </summary>
        public string GetSelectedFilePath()
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                return listViewFiles.SelectedItems[0].Tag as string;
            }
            return null;
        }
    }
}
