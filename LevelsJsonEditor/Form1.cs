using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace LevelsJsonEditor
{
    public partial class Form1 : Form
    {
        // 数据容器和当前编辑状态
        private Container _container;
        private LevelData _current;
        private int _currentIndex = -1;
        private bool _isUpdatingUI = false;

        // 实体分组
        private readonly string[] _groupsOrder = new[]
        {
            "Parks","PayParks","Cars","Entities","Emptys","Factorys","Boxs","LockDoors"
        };
        private readonly Dictionary<string, List<GridEntityData>> _groups = new Dictionary<string, List<GridEntityData>>();

        // 选择状态
        private string _selectedGroup = null;
        private int _selectedIndex = -1;
        private bool _randomCarFoldout = true;

        // 颜色映射（预览用）
        private readonly Dictionary<string, Color> _typeColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            { "Empty", Color.FromArgb(243, 243, 243) },
            { "Wall", Color.FromArgb(89, 89, 89) },
            { "Hole", Color.FromArgb(38, 38, 38) },
            { "Item", Color.FromArgb(51, 153, 230) },
            { "Car", Color.FromArgb(230, 230, 51) },
            { "Park", Color.FromArgb(51, 230, 51) },
            { "PayPark", Color.FromArgb(51, 230, 153) },
            { "Factory", Color.FromArgb(204, 102, 38) },
            { "Box", Color.FromArgb(179, 128, 51) },
            { "LockDoor", Color.FromArgb(230, 153, 77) },
        };

        // 网格预览参数
        private int _cellSize = 28;
        private int _gridPadding = 8;

        public Form1()
        {
            InitializeComponent();
            InitializeEditor();
        }

        private void InitializeEditor()
        {
            LoadJson();
            if (_container.Levels.Count == 0)
            {
                MessageBox.Show("找不到json配置文件“levels.json”"); 
                return;
                _container.Levels.Add(CreateDefaultLevel(1));
            }
            SetCurrentIndex(0);
            CreateLevelInfoControls();
            
            // 绑定事件
            btnRefresh.Click += BtnRefresh_Click;
            btnSave.Click += BtnSave_Click;
            btnNewLevel.Click += BtnNewLevel_Click;
            btnDeleteLevel.Click += BtnDeleteLevel_Click;
            cmbLevelSelect.SelectedIndexChanged += CmbLevelSelect_SelectedIndexChanged;
            
            treeViewEntities.AfterSelect += TreeViewEntities_AfterSelect;
            treeViewEntities.MouseClick += TreeViewEntities_MouseClick;
            
            panelPreview.Paint += PanelPreview_Paint;
            panelPreview.Resize += PanelPreview_Resize;

            // 绑定PropertyGrid属性值改变事件
            propertyGrid.PropertyValueChanged += PropertyGrid_PropertyValueChanged;

            // 绑定属性编辑按钮事件
            btnDeleteEntity.Click += BtnDeleteEntity_Click;
            btnCopyUp.Click += BtnCopyUp_Click;
            btnCopyDown.Click += BtnCopyDown_Click;
            btnCopyLeft.Click += BtnCopyLeft_Click;
            btnCopyRight.Click += BtnCopyRight_Click;

            // 窗口大小变化时重新分配三个区域的宽度
            this.Resize += Form1_Resize;
            
            // 确保启动时也平分三个区域的宽度
            this.Load += (s, e) => Form1_Resize(s, e);
        }

        private string GetJsonFilePath()
        {
            // JSON文件路径读取exe所在目录
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            return Path.Combine(exeDir, "levels.json");
        }

        private void LoadJson()
        {
            string jsonPath = GetJsonFilePath();
            if (!File.Exists(jsonPath))
            {
                _container = new Container { Levels = new List<LevelData>() };
                return;
            }

            try
            {
                string json = File.ReadAllText(jsonPath);
                var raw = JsonConvert.DeserializeObject<LevelDataContainer>(json);
                _container = new Container { Levels = raw?.Levels ?? new List<LevelData>() };
            }
            catch (Exception e)
            {
                MessageBox.Show($"解析 levels.json 失败: {e.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _container = new Container { Levels = new List<LevelData>() };
            }
        }

        private void SaveJson()
        {
            if (_current != null)
            {
                ApplyGroupsToCurrent();
            }

            if (!ValidateLevel(_current, out var dialogTitle, out var dialogMsg))
            {
                MessageBox.Show(dialogMsg, dialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var raw = new LevelDataContainer { Levels = _container.Levels };
                string json = JsonConvert.SerializeObject(raw, Formatting.Indented);
                string jsonPath = GetJsonFilePath();
                File.WriteAllText(jsonPath, json);
                MessageBox.Show(dialogMsg, dialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                MessageBox.Show($"保存失败: {e.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateLevel(LevelData level, out string title, out string message)
        {
            if (level == null)
            {
                title = "保存失败";
                message = "关卡数据为空。";
                return false;
            }

            if (level.Grid == null)
            {
                title = "保存失败";
                message = "缺少 Grid 配置。";
                return false;
            }

            if (level.Grid.Width <= 0 || level.Grid.Height <= 0)
            {
                title = "保存失败";
                message = $"Grid 尺寸非法 Width:{level.Grid.Width}, Height:{level.Grid.Height}。";
                return false;
            }

            title = "保存成功";
            message = "校验通过，保存成功。";
            return true;
        }

        private LevelData CreateDefaultLevel(int lv)
        {
            return new LevelData
            {
                LV = lv,
                HardType = (int)LevelHardType.Normal,
                RandomCar = false,
                GameTimeLimit = 0,
                EnableTimeLimit = false,
                Grid = new GridData { Width = 7, Height = 11, CellSize = 64f },
                Parks = new GridEntityData[0],
                PayParks = new GridEntityData[0],
                Cars = new GridEntityData[0],
                Entities = new GridEntityData[0],
                Emptys = new GridEntityData[0],
                Factorys = new GridEntityData[0],
                Boxs = new GridEntityData[0],
                LockDoors = new GridEntityData[0]
            };
        }

        private void SetCurrentIndex(int idx)
        {
            if (idx < 0 || idx >= _container.Levels.Count) return;
            _currentIndex = idx;
            _current = _container.Levels[_currentIndex];
            BuildGroupCaches();
            ClearSelection();
            UpdateUI();
        }

        private void BuildGroupCaches()
        {
            _groups.Clear();
            foreach (var g in _groupsOrder)
            {
                _groups[g] = GetArrayByName(g)?.ToList() ?? new List<GridEntityData>();
            }
        }

        private GridEntityData[] GetArrayByName(string name)
        {
            switch (name)
            {
                case "Parks": return _current.Parks;
                case "PayParks": return _current.PayParks;
                case "Cars": return _current.Cars;
                case "Entities": return _current.Entities;
                case "Emptys": return _current.Emptys;
                case "Factorys": return _current.Factorys;
                case "Boxs": return _current.Boxs;
                case "LockDoors": return _current.LockDoors;
                default: return new GridEntityData[0];
            }
        }

        private void ApplyGroupsToCurrent()
        {
            _current.Parks = _groups["Parks"].ToArray();
            _current.PayParks = _groups["PayParks"].ToArray();
            _current.Cars = _groups["Cars"].ToArray();
            _current.Entities = _groups["Entities"].ToArray();
            _current.Emptys = _groups["Emptys"].ToArray();
            _current.Factorys = _groups["Factorys"].ToArray();
            _current.Boxs = _groups["Boxs"].ToArray();
            _current.LockDoors = _groups["LockDoors"].ToArray();
        }

        private void ClearSelection()
        {
            _selectedGroup = null;
            _selectedIndex = -1;
            propertyGrid.SelectedObject = null;
        }

        private void UpdateUI()
        {
            if (_isUpdatingUI) return; // 防止递归调用
            
            _isUpdatingUI = true;
            try
            {
                UpdateLevelSelector();
                UpdateEntityTree();
                UpdateLevelInfoControls();
                panelPreview.Invalidate();
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        // 事件处理方法
        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadJson();
            if (_container.Levels.Count > 0)
                SetCurrentIndex(Math.Max(0, Math.Min(_currentIndex, _container.Levels.Count - 1)));
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveJson();
        }

        private void BtnNewLevel_Click(object sender, EventArgs e)
        {
            int nextLv = _container.Levels.Count == 0 ? 1 : (_container.Levels.Max(l => l.LV) + 1);
            var baseLv = _container.Levels.FirstOrDefault(l => l.LV == 1) ?? _container.Levels.FirstOrDefault();
            LevelData newLv = baseLv != null ? CreateLevelFromBase(nextLv, baseLv) : CreateDefaultLevel(nextLv);
            _container.Levels.Add(newLv);
            SetCurrentIndex(_container.Levels.Count - 1);
        }

        private void BtnDeleteLevel_Click(object sender, EventArgs e)
        {
            if (_currentIndex >= 0 && _currentIndex < _container.Levels.Count)
            {
                _container.Levels.RemoveAt(_currentIndex);
                SetCurrentIndex(Math.Max(0, Math.Min(_currentIndex - 1, _container.Levels.Count - 1)));
            }
        }

        private void CmbLevelSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 如果正在更新UI，忽略此事件以避免死循环
            if (_isUpdatingUI) return;
            
            if (cmbLevelSelect.SelectedIndex >= 0 && cmbLevelSelect.SelectedIndex < _container.Levels.Count)
            {
                SetCurrentIndex(cmbLevelSelect.SelectedIndex);
            }
        }

        private LevelData CreateLevelFromBase(int lv, LevelData baseLv)
        {
            return new LevelData
            {
                LV = lv,
                HardType = baseLv.HardType,
                RandomCar = baseLv.RandomCar,
                GameTimeLimit = baseLv.GameTimeLimit,
                EnableTimeLimit = baseLv.EnableTimeLimit,
                Grid = new GridData
                {
                    Width = baseLv.Grid?.Width ?? 7,
                    Height = baseLv.Grid?.Height ?? 11,
                    CellSize = baseLv.Grid?.CellSize == 0 ? 64f : baseLv.Grid.CellSize
                },
                Emptys = baseLv.Emptys?.Select(CloneEntity).ToArray() ?? new GridEntityData[0],
                PayParks = baseLv.PayParks?.Select(CloneEntity).ToArray() ?? new GridEntityData[0],
                Parks = baseLv.Parks?.Select(CloneEntity).ToArray() ?? new GridEntityData[0],
                Cars = new GridEntityData[0],
                Entities = new GridEntityData[0],
                Factorys = new GridEntityData[0],
                Boxs = new GridEntityData[0],
                LockDoors = new GridEntityData[0],
                RandomCarColorTypes = baseLv.RandomCarColorTypes,
                RandomCarCounts = baseLv.RandomCarCounts,
                AwardCoin = baseLv.AwardCoin,
                AwardItem1 = baseLv.AwardItem1,
                AwardItem2 = baseLv.AwardItem2,
                AwardItem3 = baseLv.AwardItem3,
                AwardItem4 = baseLv.AwardItem4,
            };
        }

        private GridEntityData CloneEntity(GridEntityData s)
        {
            if (s == null) return null;
            return new GridEntityData
            {
                Type = s.Type,
                CellX = s.CellX,
                CellY = s.CellY,
                ColorType = s.ColorType,
                HasKey = s.HasKey,
                KayColorType = s.KayColorType,
                Dir = s.Dir,
                IncludeCarCount = s.IncludeCarCount
            };
        }
        
        // UI 更新方法
        private void UpdateLevelSelector()
        {
            cmbLevelSelect.Items.Clear();
            for (int i = 0; i < _container.Levels.Count; i++)
            {
                var level = _container.Levels[i];
                cmbLevelSelect.Items.Add($"LV {level.LV}");
            }
            if (_currentIndex >= 0 && _currentIndex < cmbLevelSelect.Items.Count)
            {
                cmbLevelSelect.SelectedIndex = _currentIndex;
            }
        }

        private void UpdateEntityTree()
        {
            treeViewEntities.Nodes.Clear();
            
            foreach (var groupName in _groupsOrder)
            {
                var groupNode = new TreeNode(groupName) { Tag = new { Type = "Group", Name = groupName } };
                var entities = _groups[groupName];
                
                for (int i = 0; i < entities.Count; i++)
                {
                    var entity = entities[i];
                    string displayText = $"{entity.Type} ({entity.CellX},{entity.CellY})";
                    if (!string.IsNullOrEmpty(entity.ColorType))
                        displayText += $" [{entity.ColorType}]";
                    if (entity.HasKey)
                        displayText += " Key";
                    
                    var entityNode = new TreeNode(displayText) 
                    { 
                        Tag = new { Type = "Entity", GroupName = groupName, Index = i } 
                    };
                    groupNode.Nodes.Add(entityNode);
                }
                
                treeViewEntities.Nodes.Add(groupNode);
                groupNode.Expand();
            }
        }

        private void CreateLevelInfoControls()
        {
            // 创建关卡信息编辑控件
            groupBoxLevelInfo.Controls.Clear();
            
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 10,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10)
            };
            
            // 设置列宽
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            
            // 设置行高为自适应
            for (int i = 0; i < 10; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            
            int row = 0;
            
            // LV
            table.Controls.Add(new Label { Text = "LV:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var numLV = new NumericUpDown { Minimum = 1, Maximum = 9999, Value = 1, Name = "numLV" };
            table.Controls.Add(numLV, 1, row);
            
            // HardType
            table.Controls.Add(new Label { Text = "HardType:", TextAlign = ContentAlignment.MiddleRight }, 2, row);
            var cmbHardType = new ComboBox { 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                Name = "cmbHardType",
                DataSource = Enum.GetValues(typeof(LevelHardType))
            };
            table.Controls.Add(cmbHardType, 3, row);
            row++;
            
            // RandomCar
            table.Controls.Add(new Label { Text = "RandomCar:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var chkRandomCar = new CheckBox { Name = "chkRandomCar" };
            table.Controls.Add(chkRandomCar, 1, row);
            row++;
            
            // GameTimeLimit
            table.Controls.Add(new Label { Text = "GameTimeLimit:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var numTimeLimit = new NumericUpDown { 
                Minimum = 0, Maximum = 999999, DecimalPlaces = 1, 
                Increment = 0.1m, Name = "numTimeLimit" 
            };
            table.Controls.Add(numTimeLimit, 1, row);
            
            // EnableTimeLimit
            table.Controls.Add(new Label { Text = "EnableTimeLimit:", TextAlign = ContentAlignment.MiddleRight }, 2, row);
            var chkEnableTimeLimit = new CheckBox { Name = "chkEnableTimeLimit" };
            table.Controls.Add(chkEnableTimeLimit, 3, row);
            row++;
            
            // Grid Width/Height
            table.Controls.Add(new Label { Text = "Grid Width:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var numWidth = new NumericUpDown { Minimum = 1, Maximum = 50, Value = 7, Name = "numWidth" };
            table.Controls.Add(numWidth, 1, row);
            
            table.Controls.Add(new Label { Text = "Grid Height:", TextAlign = ContentAlignment.MiddleRight }, 2, row);
            var numHeight = new NumericUpDown { Minimum = 1, Maximum = 50, Value = 11, Name = "numHeight" };
            table.Controls.Add(numHeight, 3, row);
            row++;
            
            // Grid CellSize
            table.Controls.Add(new Label { Text = "Grid CellSize:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var numCellSize = new NumericUpDown { 
                Minimum = 0, Maximum = 256, Value = 64, 
                DecimalPlaces = 1, Increment = 0.1m, Name = "numCellSize" 
            };
            table.Controls.Add(numCellSize, 1, row);
            row++;
            
            // 奖励配置
            table.Controls.Add(new Label { Text = "AwardCoin:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var numAwardCoin = new NumericUpDown { Minimum = 0, Maximum = 999999, Name = "numAwardCoin" };
            table.Controls.Add(numAwardCoin, 1, row);
            
            table.Controls.Add(new Label { Text = "AwardItem1:", TextAlign = ContentAlignment.MiddleRight }, 2, row);
            var numAwardItem1 = new NumericUpDown { Minimum = 0, Maximum = 999999, Name = "numAwardItem1" };
            table.Controls.Add(numAwardItem1, 3, row);
            row++;
            
            // 奖励配置第二行
            table.Controls.Add(new Label { Text = "AwardItem2:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var numAwardItem2 = new NumericUpDown { Minimum = 0, Maximum = 999999, Name = "numAwardItem2" };
            table.Controls.Add(numAwardItem2, 1, row);
            
            table.Controls.Add(new Label { Text = "AwardItem3:", TextAlign = ContentAlignment.MiddleRight }, 2, row);
            var numAwardItem3 = new NumericUpDown { Minimum = 0, Maximum = 999999, Name = "numAwardItem3" };
            table.Controls.Add(numAwardItem3, 3, row);
            row++;
            
            // 奖励配置第三行
            table.Controls.Add(new Label { Text = "AwardItem4:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var numAwardItem4 = new NumericUpDown { Minimum = 0, Maximum = 999999, Name = "numAwardItem4" };
            table.Controls.Add(numAwardItem4, 1, row);
            row++;

            // 随机车辆配置区域
            var randomCarPanel = new Panel
            {
                Name = "randomCarPanel",
                Size = new Size(600, 320),  // 固定宽度600px，高度320px
                MinimumSize = new Size(600, 320),
                MaximumSize = new Size(600, 320),
                AutoSize = false,
                AutoScroll = true,  // 添加滚动条，防止内容超出时被截断
                BorderStyle = BorderStyle.FixedSingle,  // 添加边框便于查看区域
                Enabled = false
            };

            table.Controls.Add(randomCarPanel, 0, row);
            table.SetColumnSpan(randomCarPanel, 4);
            row++;
            
            // 保存按钮
            var btnSaveLevel = new Button { Text = "保存关卡到 levels.json", Name = "btnSaveLevel" };
            btnSaveLevel.Click += (s, e) => SaveJson();
            table.Controls.Add(btnSaveLevel, 0, row);
            table.SetColumnSpan(btnSaveLevel, 4);
            
            groupBoxLevelInfo.Controls.Add(table);
            
            // 绑定值变更事件
            numLV.ValueChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.LV = (int)numLV.Value; };
            cmbHardType.SelectedIndexChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.HardType = (int)cmbHardType.SelectedItem; };
            chkRandomCar.CheckedChanged += (s, e) => { 
                if (_current != null && !_isUpdatingUI) { 
                    _current.RandomCar = chkRandomCar.Checked; 
                    randomCarPanel.Enabled = chkRandomCar.Checked;
                    UpdateRandomCarPanel(randomCarPanel);
                } 
            };
            numTimeLimit.ValueChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.GameTimeLimit = (float)numTimeLimit.Value; };
            chkEnableTimeLimit.CheckedChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.EnableTimeLimit = chkEnableTimeLimit.Checked; };
            numWidth.ValueChanged += (s, e) => { if (_current?.Grid != null && !_isUpdatingUI) { _current.Grid.Width = (int)numWidth.Value; panelPreview.Invalidate(); } };
            numHeight.ValueChanged += (s, e) => { if (_current?.Grid != null && !_isUpdatingUI) { _current.Grid.Height = (int)numHeight.Value; panelPreview.Invalidate(); } };
            numCellSize.ValueChanged += (s, e) => { if (_current?.Grid != null && !_isUpdatingUI) _current.Grid.CellSize = (float)numCellSize.Value; };
            numAwardCoin.ValueChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.AwardCoin = (int)numAwardCoin.Value; };
            numAwardItem1.ValueChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.AwardItem1 = (int)numAwardItem1.Value; };
            numAwardItem2.ValueChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.AwardItem2 = (int)numAwardItem2.Value; };
            numAwardItem3.ValueChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.AwardItem3 = (int)numAwardItem3.Value; };
            numAwardItem4.ValueChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.AwardItem4 = (int)numAwardItem4.Value; };
            
            // 初始化随机车辆配置面板
            randomCarPanel.Enabled = _current?.RandomCar ?? false;
            UpdateRandomCarPanel(randomCarPanel);
        }

        private void UpdateLevelInfoControls()
        {
            if (_current == null) return;
            
            foreach (Control control in groupBoxLevelInfo.Controls)
            {
                if (control is TableLayoutPanel table)
                {
                    foreach (Control c in table.Controls)
                    {
                        switch (c.Name)
                        {
                            case "numLV":
                                ((NumericUpDown)c).Value = _current.LV;
                                break;
                            case "cmbHardType":
                                ((ComboBox)c).SelectedItem = (LevelHardType)_current.HardType;
                                break;
                            case "chkRandomCar":
                                ((CheckBox)c).Checked = _current.RandomCar;
                                break;
                            case "numTimeLimit":
                                ((NumericUpDown)c).Value = (decimal)_current.GameTimeLimit;
                                break;
                            case "chkEnableTimeLimit":
                                ((CheckBox)c).Checked = _current.EnableTimeLimit;
                                break;
                            case "numWidth":
                                ((NumericUpDown)c).Value = _current.Grid?.Width ?? 7;
                                break;
                            case "numHeight":
                                ((NumericUpDown)c).Value = _current.Grid?.Height ?? 11;
                                break;
                            case "numCellSize":
                                ((NumericUpDown)c).Value = (decimal)(_current.Grid?.CellSize ?? 64f);
                                break;
                            case "numAwardCoin":
                                ((NumericUpDown)c).Value = _current.AwardCoin;
                                break;
                            case "numAwardItem1":
                                ((NumericUpDown)c).Value = _current.AwardItem1;
                                break;
                            case "numAwardItem2":
                                ((NumericUpDown)c).Value = _current.AwardItem2;
                                break;
                            case "numAwardItem3":
                                ((NumericUpDown)c).Value = _current.AwardItem3;
                                break;
                            case "numAwardItem4":
                                ((NumericUpDown)c).Value = _current.AwardItem4;
                                break;
                            case "randomCarPanel":
                                var panel = (Panel)c;
                                panel.Enabled = _current.RandomCar;
                                UpdateRandomCarPanel(panel);
                                break;
                        }
                    }
                }
            }
        }

        private void TreeViewEntities_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag == null) return;
            
            dynamic tag = e.Node.Tag;
            if (tag.Type == "Entity")
            {
                _selectedGroup = tag.GroupName;
                _selectedIndex = tag.Index;
                
                if (_groups.ContainsKey(_selectedGroup) && _selectedIndex >= 0 && _selectedIndex < _groups[_selectedGroup].Count)
                {
                    propertyGrid.SelectedObject = _groups[_selectedGroup][_selectedIndex];
                    panelPreview.Invalidate(); // 重绘预览以显示选中状态
                }
            }
            else
            {
                ClearSelection();
            }
        }

        private void TreeViewEntities_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var node = treeViewEntities.GetNodeAt(e.X, e.Y);
                if (node?.Tag == null) return;
                
                dynamic tag = node.Tag;
                var menu = new ContextMenuStrip();
                
                if (tag.Type == "Group")
                {
                    string groupName = tag.Name;
                    menu.Items.Add("添加实体", null, (s, args) => AddEntityToGroup(groupName));
                    menu.Items.Add("清空该组", null, (s, args) => {
                        _groups[groupName].Clear();
                        UpdateEntityTree();
                        panelPreview.Invalidate();
                    });
                }
                else if (tag.Type == "Entity")
                {
                    menu.Items.Add("删除实体", null, (s, args) => {
                        _groups[tag.GroupName].RemoveAt(tag.Index);
                        ClearSelection();
                        UpdateEntityTree();
                        panelPreview.Invalidate();
                    });
                }
                
                menu.Show(treeViewEntities, e.Location);
            }
        }

        private void AddEntityToGroup(string groupName)
        {
            var newEntity = new GridEntityData
            {
                Type = GroupToDefaultType(groupName),
                CellX = 0,
                CellY = 0,
                ColorType = groupName.Equals("Cars", StringComparison.OrdinalIgnoreCase) ? CarColorType.White.ToString() : "",
                HasKey = false,
                KayColorType = CarColorType.White.ToString(),
                Dir = DirectionsType.Down.ToString(),
                IncludeCarCount = 0
            };
            
            _groups[groupName].Add(newEntity);
            UpdateEntityTree();
            panelPreview.Invalidate();
        }

        private string GroupToDefaultType(string group)
        {
            switch (group)
            {
                case "Parks": return "Park";
                case "PayParks": return "PayPark";
                case "Cars": return "Car";
                case "Emptys": return "Empty";
                case "Factorys": return "Factory";
                case "Boxs": return "Box";
                case "LockDoors": return "LockDoor";
                case "Entities": return "Wall";
                default: return "Empty";
            }
        }

        private void PanelPreview_Paint(object sender, PaintEventArgs e)
        {
            if (_current?.Grid == null) return;
            
            var g = e.Graphics;
            g.Clear(Color.FromArgb(46, 46, 46));
            
            int gw = Math.Max(1, _current.Grid.Width);
            int gh = Math.Max(1, _current.Grid.Height);
            
            // 计算网格大小和位置
            var drawRect = panelPreview.ClientRectangle;
            int cell = Math.Max(4, Math.Min(_cellSize, 
                Math.Min((drawRect.Width - _gridPadding * 2) / gw, 
                        (drawRect.Height - _gridPadding * 2) / gh)));
            
            float totalW = gw * cell + _gridPadding * 2;
            float totalH = gh * cell + _gridPadding * 2;
            
            var frame = new RectangleF(
                (drawRect.Width - totalW) * 0.5f,
                (drawRect.Height - totalH) * 0.5f,
                totalW,
                totalH);
            
            // 画背景
            g.FillRectangle(new SolidBrush(Color.FromArgb(25, 25, 25)), frame);
            
            var content = new RectangleF(frame.X + _gridPadding, frame.Y + _gridPadding, gw * cell, gh * cell);
            g.FillRectangle(new SolidBrush(Color.FromArgb(46, 46, 46)), content);
            
            // 画网格线
            using (var gridPen = new Pen(Color.FromArgb(89, 89, 89), 1))
            {
                for (int x = 0; x <= gw; x++)
                {
                    float xx = content.X + x * cell;
                    g.DrawLine(gridPen, xx, content.Y, xx, content.Bottom);
                }
                for (int y = 0; y <= gh; y++)
                {
                    float yy = content.Y + y * cell;
                    g.DrawLine(gridPen, content.X, yy, content.Right, yy);
                }
            }
            
            // 画实体
            DrawEntities(g, content, cell, gw, gh, _groups["Emptys"]);
            DrawEntities(g, content, cell, gw, gh, _groups["Entities"]);
            DrawEntities(g, content, cell, gw, gh, _groups["Parks"]);
            DrawEntities(g, content, cell, gw, gh, _groups["PayParks"]);
            DrawEntities(g, content, cell, gw, gh, _groups["Factorys"]);
            DrawEntities(g, content, cell, gw, gh, _groups["Cars"]);
            DrawEntities(g, content, cell, gw, gh, _groups["Boxs"]);
            DrawEntities(g, content, cell, gw, gh, _groups["LockDoors"]);
        }

        private void DrawEntities(Graphics g, RectangleF content, int cell, int gw, int gh, List<GridEntityData> entities)
        {
            foreach (var entity in entities)
            {
                if (entity == null) continue;
                if (entity.CellX < 0 || entity.CellX >= gw || entity.CellY < 0 || entity.CellY >= gh) continue;
                
                var rect = new RectangleF(
                    content.X + entity.CellX * cell + 1,
                    content.Y + (gh - 1 - entity.CellY) * cell + 1, // Y坐标翻转，使(0,0)在左下
                    cell - 2, cell - 2);
                
                // 根据类型选择颜色
                Color color;
                if (!_typeColors.TryGetValue(entity.Type ?? "Empty", out color))
                    color = Color.Gray;
                
                // 如果是Car类型，根据颜色类型调整颜色
                if ("Car".Equals(entity.Type, StringComparison.OrdinalIgnoreCase))
                {
                    color = GetCarColor(entity.ColorType);
                }
                
                g.FillRectangle(new SolidBrush(color), rect);
                
                // 选中高亮
                if (_selectedGroup != null && _selectedIndex >= 0)
                {
                    var selectedEntities = _groups[_selectedGroup];
                    if (_selectedIndex < selectedEntities.Count && ReferenceEquals(entity, selectedEntities[_selectedIndex]))
                    {
                        using (var pen = new Pen(Color.Yellow, 2))
                        {
                            g.DrawRectangle(pen, Rectangle.Round(rect));
                        }
                    }
                }
            }
        }

        private Color GetCarColor(string colorType)
        {
            if (Enum.TryParse<CarColorType>(colorType, true, out var ct))
            {
                switch (ct)
                {
                    case CarColorType.Red: return Color.FromArgb(230, 64, 64);
                    case CarColorType.Blue: return Color.FromArgb(51, 128, 242);
                    case CarColorType.Green: return Color.FromArgb(64, 217, 77);
                    case CarColorType.Yellow: return Color.FromArgb(242, 217, 51);
                    case CarColorType.Purple: return Color.FromArgb(166, 89, 230);
                    default: return Color.FromArgb(230, 230, 51);
                }
            }
            return Color.FromArgb(230, 230, 51);
        }

        private void PanelPreview_Resize(object sender, EventArgs e)
        {
            panelPreview.Invalidate();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            // 窗口大小变化时重新分配三个区域的宽度，让它们平分
            if (splitContainer1.Width > 0)
            {
                int totalWidth = splitContainer1.Width;
                int thirdWidth = totalWidth / 3;
                
                // 设置第一个分割器位置（实体树区域）
                splitContainer1.SplitterDistance = thirdWidth;
                
                // 设置第二个分割器位置（预览和属性区域各占一半）
                int remainingWidth = totalWidth - thirdWidth - splitContainer1.SplitterWidth;
                if (remainingWidth > 0)
                {
                    splitContainer2.SplitterDistance = remainingWidth / 2;
                }
            }
        }

        private void PropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            // 当实体属性被修改后，刷新实体树和场景预览
            if (!_isUpdatingUI && _selectedGroup != null && _selectedIndex >= 0)
            {
                // 重新构建实体树以更新显示文字（坐标、颜色等）
                UpdateEntityTree();

                // 重绘场景预览以显示新的位置和属性
                panelPreview.Invalidate();

                // 重新选中当前编辑的实体节点（因为UpdateEntityTree会清空选择）
                SelectEntityInTree(_selectedGroup, _selectedIndex);
            }
        }

        private void SelectEntityInTree(string groupName, int entityIndex)
        {
            foreach (TreeNode groupNode in treeViewEntities.Nodes)
            {
                if (groupNode.Tag is object groupTag &&
                    groupTag.GetType().GetProperty("Name")?.GetValue(groupTag)?.ToString() == groupName)
                {
                    if (entityIndex >= 0 && entityIndex < groupNode.Nodes.Count)
                    {
                        treeViewEntities.SelectedNode = groupNode.Nodes[entityIndex];
                        groupNode.Expand();
                        break;
                    }
                }
            }
        }

        private void UpdateRandomCarPanel(Panel panel)
        {
            if (_current == null) return;
            
            panel.Controls.Clear();
            
            if (!_current.RandomCar) 
            {
                // 当RandomCar为false时，显示提示信息
                var lblDisabled = new Label 
                { 
                    Text = "随机车辆配置已禁用", 
                    ForeColor = System.Drawing.SystemColors.GrayText,
                    AutoSize = true,
                    Margin = new Padding(0, 10, 0, 0)
                };

                panel.Size = panel.MaximumSize = panel.MinimumSize = new Size(600, 50); // 最小100px，最大400px

                panel.Controls.Add(lblDisabled);
               
                return;
            }
            
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4,
                Margin = new Padding(0, 10, 0, 0)
            };
            
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            
            // 添加标题
            var lblTitle = new Label 
            { 
                Text = "随机车辆配置", 
                Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold),
                AutoSize = true
            };
            layout.Controls.Add(lblTitle, 0, 0);
            layout.SetColumnSpan(lblTitle, 4);
            
            // 添加数量控制
            int curSize = Math.Max(_current.RandomCarCounts?.Length ?? 0,
                _current.RandomCarColorTypes?.Length ?? 0);
            
            var numSize = new NumericUpDown 
            { 
                Minimum = 0, 
                Maximum = 10, 
                Value = curSize,
                Name = "numRandomCarSize"
            };
            numSize.ValueChanged += (s, e) => {
                if (!_isUpdatingUI)
                {
                    int newSize = (int)numSize.Value;
                    _current.RandomCarCounts = ResizeArray(_current.RandomCarCounts, newSize);
                    _current.RandomCarColorTypes = ResizeArray(_current.RandomCarColorTypes, newSize);
                    UpdateRandomCarPanel(panel);
                }
            };
            
            layout.Controls.Add(new Label { Text = "随机种类数量:", AutoSize = true }, 0, 1);
            layout.Controls.Add(numSize, 1, 1);
            
            // 确保数组初始化
            if (_current.RandomCarCounts == null) _current.RandomCarCounts = new int[curSize];
            if (_current.RandomCarColorTypes == null) _current.RandomCarColorTypes = new string[curSize];
            
            // 确保数组长度一致
            if (_current.RandomCarCounts.Length != curSize)
                _current.RandomCarCounts = ResizeArray(_current.RandomCarCounts, curSize);
            if (_current.RandomCarColorTypes.Length != curSize)
                _current.RandomCarColorTypes = ResizeArray(_current.RandomCarColorTypes, curSize);
            
            // 添加每个随机车配置
            for (int i = 0; i < curSize; i++)
            {
                int row = i + 2;
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                
                // 颜色类型
                var cmbColor = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Tag = i
                };
                
                // 手动添加枚举项到Items集合（不使用DataSource）
                foreach (CarColorType color in Enum.GetValues(typeof(CarColorType)))
                {
                    cmbColor.Items.Add(color);
                }
                
                // 安全获取颜色类型
                string colorTypeStr = (i < _current.RandomCarColorTypes.Length && _current.RandomCarColorTypes[i] != null) 
                    ? _current.RandomCarColorTypes[i] 
                    : CarColorType.White.ToString();
                    
                // 设置选中项
                if (Enum.TryParse<CarColorType>(colorTypeStr, true, out var colorEnum))
                {
                    cmbColor.SelectedItem = colorEnum;
                }
                else
                {
                    cmbColor.SelectedItem = CarColorType.White;
                    _current.RandomCarColorTypes[i] = CarColorType.White.ToString();
                }
                
                cmbColor.SelectedIndexChanged += (s, e) => {
                    if (!_isUpdatingUI)
                    {
                        var cb = (ComboBox)s;
                        int index = (int)cb.Tag;
                        _current.RandomCarColorTypes[index] = cb.SelectedItem.ToString();
                    }
                };
                
                // 数量
                int countValue = (i < _current.RandomCarCounts.Length) ? _current.RandomCarCounts[i] : 0;
                var numCount = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 99,
                    Value = countValue,
                    Tag = i
                };
                
                numCount.ValueChanged += (s, e) => {
                    if (!_isUpdatingUI)
                    {
                        var num = (NumericUpDown)s;
                        int index = (int)num.Tag;
                        _current.RandomCarCounts[index] = (int)num.Value;
                    }
                };
                
                layout.Controls.Add(new Label { Text = $"颜色[{i}]:", AutoSize = true }, 0, row);
                layout.Controls.Add(cmbColor, 1, row);
                layout.Controls.Add(new Label { Text = $"数量[{i}]:", AutoSize = true }, 2, row);
                layout.Controls.Add(numCount, 3, row);
            }

            int dynamicHeight = 50 + (curSize * 25) + 10; // 基础高度 + 配置行数 * 行高 + 余量
            panel.Size = panel.MaximumSize = panel.MinimumSize = new Size(600, Math.Max(50, Math.Min(dynamicHeight, 300))); // 最小100px，最大400px

            panel.Controls.Add(layout);
        }

        // 数组扩容辅助方法
        private string[] ResizeArray(string[] src, int newSize)
        {
            if (newSize < 0) newSize = 0;
            var dst = new string[newSize];
            if (src != null) 
            {
                Array.Copy(src, dst, Math.Min(src.Length, dst.Length));
                // 为新增的元素设置默认值
                for (int i = src.Length; i < newSize; i++)
                {
                    dst[i] = CarColorType.White.ToString();
                }
            }
            else
            {
                // 全新数组，设置默认值
                for (int i = 0; i < newSize; i++)
                {
                    dst[i] = CarColorType.White.ToString();
                }
            }
            return dst;
        }

        private int[] ResizeArray(int[] src, int newSize)
        {
            if (newSize < 0) newSize = 0;
            var dst = new int[newSize];
            if (src != null) Array.Copy(src, dst, Math.Min(src.Length, dst.Length));
            return dst;
        }

        // 属性编辑按钮事件处理方法
        private void BtnDeleteEntity_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedGroup) || _selectedIndex < 0 || 
                !_groups.ContainsKey(_selectedGroup) || _selectedIndex >= _groups[_selectedGroup].Count)
            {
                MessageBox.Show("请先选择一个实体", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 删除选中的实体
            _groups[_selectedGroup].RemoveAt(_selectedIndex);
            ClearSelection();
            UpdateEntityTree();
            panelPreview.Invalidate();
        }

        private void BtnCopyUp_Click(object sender, EventArgs e)
        {
            CopyEntity(0, 1); // Y坐标增加1（向上）
        }

        private void BtnCopyDown_Click(object sender, EventArgs e)
        {
            CopyEntity(0, -1); // Y坐标减少1（向下）
        }

        private void BtnCopyLeft_Click(object sender, EventArgs e)
        {
            CopyEntity(-1, 0); // X坐标减少1（向左）
        }

        private void BtnCopyRight_Click(object sender, EventArgs e)
        {
            CopyEntity(1, 0); // X坐标增加1（向右）
        }

        private void CopyEntity(int deltaX, int deltaY)
        {
            if (string.IsNullOrEmpty(_selectedGroup) || _selectedIndex < 0 || 
                !_groups.ContainsKey(_selectedGroup) || _selectedIndex >= _groups[_selectedGroup].Count)
            {
                MessageBox.Show("请先选择一个实体", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedEntity = _groups[_selectedGroup][_selectedIndex];
            var clonedEntity = CloneEntity(selectedEntity);
            clonedEntity.CellX += deltaX;
            clonedEntity.CellY += deltaY;
            
            // 添加克隆的实体到同一组
            _groups[_selectedGroup].Add(clonedEntity);
            
            // 更新选择到新创建的实体
            _selectedIndex = _groups[_selectedGroup].Count - 1;
            
            // 更新UI
            UpdateEntityTree();
            panelPreview.Invalidate();
            
            // 在PropertyGrid中显示新创建的实体
            propertyGrid.SelectedObject = clonedEntity;
            
            // 在树视图中选中新创建的实体
            SelectEntityInTree(_selectedGroup, _selectedIndex);
        }
    }
}
