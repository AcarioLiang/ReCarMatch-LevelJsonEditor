using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        
        // 原始关卡数据缓存（用于在切换关卡时恢复未保存的修改）
        private readonly Dictionary<int, LevelData> _originalLevels = new Dictionary<int, LevelData>();

        // 实体分组
        private readonly string[] _groupsOrder = new[]
        {
            "Parks","PayParks","Cars","Entities","Emptys","Factorys","Boxs","LockDoors"
        };
        private readonly Dictionary<string, List<GridEntityData>> _groups = new Dictionary<string, List<GridEntityData>>();

        // 选择状态
        private string _selectedGroup = null;
        private int _selectedIndex = -1;

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
            // 初始化图像资源
            ImageResources.Initialize();

            LoadJson();
            if (_container.Levels.Count == 0)
            {
                MessageBox.Show("找不到json配置文件“levels.json”"); 
                _container.Levels.Add(CreateDefaultLevel(1));
            }
            SetCurrentIndex(0);
            CreateLevelInfoControls();
            
            // 绑定事件
            btnRefresh.Click += BtnRefresh_Click;
            btnSave.Click += BtnSave_Click;
            btnNewLevel.Click += BtnNewLevel_Click;
            btnDeleteLevel.Click += BtnDeleteLevel_Click;
            btnValidateAll.Click += BtnValidateAll_Click;
            cmbLevelSelect.SelectedIndexChanged += CmbLevelSelect_SelectedIndexChanged;
            
            treeViewEntities.AfterSelect += TreeViewEntities_AfterSelect;
            treeViewEntities.MouseClick += TreeViewEntities_MouseClick;
            
            panelPreview.Paint += PanelPreview_Paint;
            panelPreview.Resize += PanelPreview_Resize;

            // 绑定PropertyGrid属性值改变事件
            propertyGrid.PropertyValueChanged += PropertyGrid_PropertyValueChanged;
            // 为PropertyGrid添加实时输入监听（文本变化即触发）
            propertyGrid.KeyDown += PropertyGrid_KeyDown;
            propertyGrid.KeyUp += PropertyGrid_KeyUp;

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

        // 深拷贝LevelData
        private LevelData DeepCloneLevel(LevelData source)
        {
            if (source == null) return null;
            // 使用JSON序列化实现深拷贝
            string json = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<LevelData>(json);
        }

        private void LoadJson()
        {
            string jsonPath = GetJsonFilePath();
            if (!File.Exists(jsonPath))
            {
                _container = new Container { Levels = new List<LevelData>() };
                _originalLevels.Clear();
                return;
            }

            try
            {
                string json = File.ReadAllText(jsonPath);
                var raw = JsonConvert.DeserializeObject<LevelDataContainer>(json);
                _container = new Container { Levels = raw?.Levels ?? new List<LevelData>() };
                
                // 加载后按LV排序
                SortLevelsByLV();
                
                // 缓存原始关卡数据（深拷贝）
                _originalLevels.Clear();
                foreach (var level in _container.Levels)
                {
                    _originalLevels[level.LV] = DeepCloneLevel(level);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"解析 levels.json 失败: {e.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _container = new Container { Levels = new List<LevelData>() };
                _originalLevels.Clear();
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
                
                // 保存成功后，更新所有关卡的原始数据缓存
                _originalLevels.Clear();
                foreach (var level in _container.Levels)
                {
                    _originalLevels[level.LV] = DeepCloneLevel(level);
                }
                
                MessageBox.Show(dialogMsg, dialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                MessageBox.Show($"保存失败: {e.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateLevel(LevelData level, out string title, out string message)
        {
            var errors = new StringBuilder();
            var warnings = new StringBuilder();
            int errorCount = 0;
            int warningCount = 0;

            if (level == null)
            {
                title = "保存失败";
                message = "关卡数据为空。";
                return false;
            }

            // Grid 基础检查
            if (level.Grid == null)
            {
                errors.AppendLine("[错误] 缺少 Grid 配置。");
                errorCount++;
            }
            else
            {
                if (level.Grid.Width <= 0 || level.Grid.Height <= 0)
                {
                    errors.AppendLine($"[错误] Grid 尺寸非法 Width:{level.Grid.Width}, Height:{level.Grid.Height}。");
                    errorCount++;
                }
            }

            // 位置合法性与重复校验
            var groupMap = new Dictionary<string, GridEntityData[]>(StringComparer.OrdinalIgnoreCase)
            {
                {"Emptys", level.Emptys ?? new GridEntityData[0]},
                {"Entities", level.Entities ?? new GridEntityData[0]},
                {"Parks", level.Parks ?? new GridEntityData[0]},
                {"PayParks", level.PayParks ?? new GridEntityData[0]},
                {"Factorys", level.Factorys ?? new GridEntityData[0]},
                {"Boxs", level.Boxs ?? new GridEntityData[0]},
                {"LockDoors", level.LockDoors ?? new GridEntityData[0]},
                {"Cars", level.Cars ?? new GridEntityData[0]},
            };

            // 跨组重复记录（作为警告，避免误报）
            var globalCells = new Dictionary<string, string>(); // key:"x,y" -> groupName

            foreach (var kv in groupMap)
            {
                ValidateEntityList(kv.Key, kv.Value, level.Grid, errors, warnings, ref errorCount, ref warningCount, globalCells);
            }

            //校验车辆总数是否一直

            if (errorCount == 0)
            {
                int countsLen = level.TotalCarCounts != null ? level.TotalCarCounts.Length : 0;
                int colorsLen = level.TotalCarColorTypes != null ? level.TotalCarColorTypes.Length : 0;
                if (countsLen == 0 || colorsLen == 0 || countsLen != colorsLen)
                {
                    errors.AppendLine("[错误] TotalCarCounts 与 TotalCarColorTypes 必须长度一致且大于 0。");
                    errorCount++;
                }
                else
                {
                    int totalCars = 0;
                    for (int i = 0; i < countsLen; i++) totalCars += Math.Max(0, level.TotalCarCounts[i]);

                    //校验所有车辆的颜色配置个数为3的倍数

                    // 统计每种颜色的车辆总数量
                    var colorCounts = new Dictionary<string, int>();

                    // 初始化所有颜色计数
                    for (int i = 0; i < level.TotalCarColorTypes.Length; i++)
                    {
                        colorCounts[level.TotalCarColorTypes[i]] = level.TotalCarCounts[i];
                    }

                    // 4. 验证每种颜色的车辆数量是否为3的倍数
                    bool hasColorError = false;
                    foreach (var kvp in colorCounts)
                    {
                        if (kvp.Value > 0 && kvp.Value % 3 != 0)
                        {
                            errors.AppendLine($"[错误] {kvp.Key} 颜色车辆总数为 {kvp.Value}，不是3的倍数。");
                            errorCount++;
                            hasColorError = true;
                        }
                    }

                    // 可选：显示颜色统计信息（作为调试信息）
                    if (!hasColorError && colorCounts.Values.Any(count => count > 0))
                    {

                        var colorInfo = new StringBuilder();
                        colorInfo.AppendLine("车辆颜色统计:");
                        foreach (var kvp in colorCounts)
                        {
                            if (kvp.Value > 0)
                            {
                                colorInfo.AppendLine($"  {kvp.Key}: {kvp.Value}辆");
                            }
                        }
                        // 这里可以选择将colorInfo作为警告信息添加，或者仅用于调试
                        // warnings.AppendLine(colorInfo.ToString());
                        // warningCount++;
                    }

                    // 计算当前场景中"没有实体"的格子数（排除 Emptys，按实际实体占用统计）
                    int gw = Math.Max(0, level.Grid?.Width ?? 0);
                    int gh = Math.Max(0, level.Grid?.Height ?? 0);
                    int totalCells = gw * gh;
                    var occupied = new HashSet<string>();
                    
                    void MarkOccupied(GridEntityData[] arr)
                    {
                        if (arr == null) return;
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var e = arr[i];
                            if (e == null) continue;
                            if (e.CellX < 0 || e.CellY < 0 || e.CellX >= gw || e.CellY >= gh) continue;
                            occupied.Add($"{e.CellX},{e.CellY}");
                        }
                    }
                    
                    // 标记占用
                    MarkOccupied(level.Entities);
                    MarkOccupied(level.Parks);
                    MarkOccupied(level.PayParks);
                    MarkOccupied(level.Cars);
                    MarkOccupied(level.Factorys);
                    MarkOccupied(level.Emptys);
                    MarkOccupied(level.Boxs);
                    //MarkOccupied(level.LockDoors);

                    int carsCarCount = 0;
                    if (level.Cars != null && level.Cars.Length > 0)
                    {
                        carsCarCount = level.Cars.Length;
                    }


                    int factorysCarCount = 0;
                    if (level.Factorys != null && level.Factorys.Length > 0)
                    {
                        for (int i = 0; i < level.Factorys.Length; i++) factorysCarCount += level.Factorys[i].IncludeCarCount;
                    }

                    int boxsCarCount = 0;
                    if (level.Boxs != null && level.Boxs.Length > 0)
                    {
                        boxsCarCount = level.Boxs.Length;
                    }

                    int emptyCells = Math.Max(0, totalCells - occupied.Count);

                    if(factorysCarCount > 0 || boxsCarCount > 0)
                    {
                        if (totalCars != carsCarCount + factorysCarCount + boxsCarCount + emptyCells)
                        {
                            errors.AppendLine($"[错误] totalCars 总数({totalCars}) 与 各实体总数{carsCarCount + factorysCarCount + boxsCarCount + emptyCells}不一致(场景空格子数量({emptyCells}) 场景静态车辆数({carsCarCount}) 车库总车辆数({factorysCarCount}) 箱子总车辆数({boxsCarCount}))。");
                            errorCount++;
                        }
                    }
                    else
                    {
                        if (totalCars != carsCarCount + factorysCarCount + boxsCarCount + emptyCells)
                        {
                            warnings.AppendLine($"[警告] totalCars 总数({totalCars}) 与 各实体总数{carsCarCount + factorysCarCount + boxsCarCount + emptyCells}不一致(场景空格子数量({emptyCells}) 场景静态车辆数({carsCarCount}) 车库总车辆数({factorysCarCount}) 箱子总车辆数({boxsCarCount}))。");
                            warningCount++;
                        }
                    }
                    
                }
            }

            //校验生成后所有车辆的颜色配置个数为3的倍数
            if (errorCount == 0)
            {
                
            }

            if (errorCount > 0)
            {
                title = "保存失败";
                message = $"发现 {errorCount} 个错误:\n" + errors.ToString();
                return false;
            }

            title = "保存成功";
            if (warningCount > 0)
            {
                message = $"校验通过，保存成功, 但有 {warningCount} 条警告:\n" + warnings.ToString();
            }
            else
            {
                message = "校验通过。保存成功";
            }
            return true;
        }

        private void ValidateEntityList(
            string groupName,
            GridEntityData[] list,
            GridData grid,
            StringBuilder errors,
            StringBuilder warnings,
            ref int errorCount,
            ref int warningCount,
            Dictionary<string, string> globalCells)
        {
            var localSet = new HashSet<string>();
            for (int i = 0; i < list.Length; i++)
            {
                var e = list[i];
                if (e == null) continue;
                string key = $"{e.CellX},{e.CellY}";

                // 坐标合法性
                if (grid != null)
                {
                    if (e.CellX < 0 || e.CellY < 0 || e.CellX >= grid.Width || e.CellY >= grid.Height)
                    {
                        errors.AppendLine($"[错误] {groupName}[{i}] 坐标越界 ({e.CellX},{e.CellY}) 不在 [0,{grid.Width-1}]x[0,{grid.Height-1}] 内。");
                        errorCount++;
                    }
                }

                // 本组重复
                if (!localSet.Add(key))
                {
                    errors.AppendLine($"[错误] {groupName} 存在重复坐标 ({e.CellX},{e.CellY})。");
                    errorCount++;
                }

                // 跨组重复（警告）
                if (globalCells.TryGetValue(key, out var existedGroup))
                {
                    if (!string.Equals(existedGroup, groupName, StringComparison.OrdinalIgnoreCase))
                    {
                        warnings.AppendLine($"[警告] {groupName}[{i}] 坐标与 {existedGroup} 冲突 ({e.CellX},{e.CellY})。");
                        warningCount++;
                    }
                }
                else
                {
                    globalCells[key] = groupName;
                }
            }
        }

        private LevelData CreateDefaultLevel(int lv)
        {
            return new LevelData
            {
                LV = lv,
                HardType = (int)LevelHardType.Normal,
                //RandomCar = false,
                //GameTimeLimit = 0,
                //EnableTimeLimit = false,
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
            
            // 如果切换关卡（索引不同），先恢复之前关卡的原始数据
            if (_currentIndex >= 0 && _currentIndex < _container.Levels.Count && idx != _currentIndex && _current != null)
            {
                int previousLv = _current.LV;
                // 如果原始数据缓存中有这个LV的原始数据，恢复到之前关卡的索引位置
                if (_originalLevels.ContainsKey(previousLv))
                {
                    LevelData original = DeepCloneLevel(_originalLevels[previousLv]);
                    _container.Levels[_currentIndex] = original;
                }
            }
            
            _currentIndex = idx;
            _current = _container.Levels[_currentIndex];
            
            // 如果新关卡有原始数据，用原始数据恢复（确保显示的是原始数据，而不是可能被修改过的数据）
            if (_originalLevels.ContainsKey(_current.LV))
            {
                LevelData original = DeepCloneLevel(_originalLevels[_current.LV]);
                _container.Levels[_currentIndex] = original;
                _current = original;
            }
            
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
            SortLevelsByLV();
            if (_container.Levels.Count > 0)
                SetCurrentIndex(_currentIndex);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SortLevelsByLV();
            SaveJson();
        }

        private void BtnNewLevel_Click(object sender, EventArgs e)
        {
            int nextLv = _container.Levels.Count == 0 ? 1 : (_container.Levels.Max(l => l.LV) + 1);
            var baseLv = _container.Levels.FirstOrDefault(l => l.LV == 1) ?? _container.Levels.FirstOrDefault();
            LevelData newLv = baseLv != null ? CreateLevelFromBase(nextLv, baseLv) : CreateDefaultLevel(nextLv);
            _container.Levels.Add(newLv);
            
            // 将新关卡添加到原始数据缓存（新关卡本身作为原始数据）
            _originalLevels[nextLv] = DeepCloneLevel(newLv);
            
            SortLevelsByLV(); // 排序后会重新定位当前关卡
            
            // 找到新添加的关卡并设为当前关卡
            for (int i = 0; i < _container.Levels.Count; i++)
            {
                if (_container.Levels[i].LV == nextLv)
                {
                    SetCurrentIndex(i);
                    break;
                }
            }
        }

        private void BtnDeleteLevel_Click(object sender, EventArgs e)
        {
            if (_currentIndex >= 0 && _currentIndex < _container.Levels.Count)
            {
                // 记录要删除的关卡LV，以便从原始数据缓存中删除
                int deletedLv = _container.Levels[_currentIndex].LV;
                
                _container.Levels.RemoveAt(_currentIndex);
                
                // 从原始数据缓存中删除
                if (_originalLevels.ContainsKey(deletedLv))
                {
                    _originalLevels.Remove(deletedLv);
                }
                
                SortLevelsByLV(); // 删除后排序并重新定位
                SetCurrentIndex(_currentIndex);
            }
        }

        private void BtnValidateAll_Click(object sender, EventArgs e)
        {
            if (_container?.Levels == null || _container.Levels.Count == 0)
            {
                MessageBox.Show("没有关卡需要检查", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var allResults = new StringBuilder();
            int totalErrorCount = 0;
            int totalWarningCount = 0;
            int checkedCount = 0;

            foreach (var level in _container.Levels)
            {
                if (level == null) continue;

                checkedCount++;
                bool isValid = ValidateLevel(level, out string title, out string message);

                // 判断是否有错误或警告需要显示
                bool hasError = !isValid;
                bool hasWarning = isValid && message.Contains("警告");

                // 只有当有错误或警告时才添加到结果中
                if (hasError || hasWarning)
                {
                    allResults.AppendLine($"========== 关卡 LV{level.LV} ==========");
                    allResults.AppendLine(message);
                    allResults.AppendLine();
                }

                // 统计错误和警告数量
                if (!isValid)
                {
                    // 从message中提取错误数量
                    var errorMatch = Regex.Match(message, @"发现 (\d+) 个错误");
                    if (errorMatch.Success)
                    {
                        totalErrorCount += int.Parse(errorMatch.Groups[1].Value);
                    }
                    else if (message.Contains("[错误]"))
                    {
                        totalErrorCount += message.Split(new[] { "[错误]" }, StringSplitOptions.None).Length - 1;
                    }
                }
                else
                {
                    // 从message中提取警告数量
                    var warningMatch = Regex.Match(message, @"有 (\d+) 条警告");
                    if (warningMatch.Success)
                    {
                        totalWarningCount += int.Parse(warningMatch.Groups[1].Value);
                    }
                }
            }

            // 组装最终结果
            var finalMessage = new StringBuilder();
            finalMessage.AppendLine($"已检查 {checkedCount} 个关卡");
            finalMessage.AppendLine();

            if (totalErrorCount == 0 && totalWarningCount == 0)
            {
                finalMessage.AppendLine("无错误");
            }
            else
            {
                if (totalErrorCount > 0)
                {
                    finalMessage.AppendLine($"总错误数: {totalErrorCount}");
                }
                if (totalWarningCount > 0)
                {
                    finalMessage.AppendLine($"总警告数: {totalWarningCount}");
                }
            }

            // 只有当有详细信息时才显示
            if (allResults.Length > 0)
            {
                finalMessage.AppendLine();
                finalMessage.AppendLine("详细信息：");
                finalMessage.AppendLine("----------------------------------------");
                finalMessage.Append(allResults.ToString());
            }

            // 显示结果
            string resultTitle = totalErrorCount > 0 ? "检查完成(可按Ctrl+C复制文本) - 发现错误" : "检查完成";
            MessageBoxIcon icon = totalErrorCount > 0 ? MessageBoxIcon.Error : 
                                  totalWarningCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information;

            MessageBox.Show(finalMessage.ToString(), resultTitle, MessageBoxButtons.OK, icon);
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
                //RandomCar = baseLv.RandomCar,
                //GameTimeLimit = baseLv.GameTimeLimit,
                //EnableTimeLimit = baseLv.EnableTimeLimit,
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
                TotalCarColorTypes = baseLv.TotalCarColorTypes,
                TotalCarCounts = baseLv.TotalCarCounts,
                AwardCoin = baseLv.AwardCoin,
                AwardItem1 = baseLv.AwardItem1,
                AwardItem2 = baseLv.AwardItem2,
                AwardItem3 = baseLv.AwardItem3,
                AwardItem4 = baseLv.AwardItem4,

                //SpaceProbabilityConfigs = baseLv.SpaceProbabilityConfigs,
                //SpaceGuaranteeConfigs = baseLv.SpaceGuaranteeConfigs
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
                //HasKey = s.HasKey,
                //KayColorType = s.KayColorType,
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
                    //if (entity.HasKey)
                    //    displayText += " Key";
                    
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
                Dock = DockStyle.None,
                ColumnCount = 4,
                RowCount = 10,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10),
                Location = new Point(10, 10)
            };
            
            // 创建一个Panel来包含table，并支持滚动
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Name = "scrollPanel"
            };
            scrollPanel.Controls.Add(table);
            
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
            
            //// RandomCar
            //table.Controls.Add(new Label { Text = "RandomCar:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            //var chkRandomCar = new CheckBox { Name = "chkRandomCar" };
            //table.Controls.Add(chkRandomCar, 1, row);
            //row++;
            
            //// GameTimeLimit
            //table.Controls.Add(new Label { Text = "GameTimeLimit:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            //var numTimeLimit = new NumericUpDown { 
            //    Minimum = 0, Maximum = 999999, DecimalPlaces = 1, 
            //    Increment = 0.1m, Name = "numTimeLimit" 
            //};
            //table.Controls.Add(numTimeLimit, 1, row);
            //
            //// EnableTimeLimit
            //table.Controls.Add(new Label { Text = "EnableTimeLimit:", TextAlign = ContentAlignment.MiddleRight }, 2, row);
            //var chkEnableTimeLimit = new CheckBox { Name = "chkEnableTimeLimit" };
            //table.Controls.Add(chkEnableTimeLimit, 3, row);
            //row++;
            
            // Grid Width/Height
            table.Controls.Add(new Label { Text = "Grid Width:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var numWidth = new NumericUpDown { Minimum = 1, Maximum = 50, Value = 7, Name = "numWidth" };
            table.Controls.Add(numWidth, 1, row);
            
            table.Controls.Add(new Label { Text = "Grid Height:", TextAlign = ContentAlignment.MiddleRight }, 2, row);
            var numHeight = new NumericUpDown { Minimum = 1, Maximum = 50, Value = 11, Name = "numHeight" };
            table.Controls.Add(numHeight, 3, row);
            row++;
            
            //// Grid CellSize
            //table.Controls.Add(new Label { Text = "Grid CellSize:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            //var numCellSize = new NumericUpDown { 
            //    Minimum = 0, Maximum = 256, Value = 64, 
            //    DecimalPlaces = 1, Increment = 0.1m, Name = "numCellSize" 
            //};
            //table.Controls.Add(numCellSize, 1, row);
            //row++;
            
            // 奖励配置
            table.Controls.Add(new Label { Text = "AwardCoin:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var numAwardCoin = new NumericUpDown { Minimum = 0, Maximum = 999999, Name = "numAwardCoin" };
            table.Controls.Add(numAwardCoin, 1, row);
            row++;

            // 奖励配置第二行
            table.Controls.Add(new Label { Text = "AwardItem1:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var numAwardItem1 = new NumericUpDown { Minimum = 0, Maximum = 999999, Name = "numAwardItem1" };
            table.Controls.Add(numAwardItem1, 1, row);
            table.Controls.Add(new Label { Text = "AwardItem2:", TextAlign = ContentAlignment.MiddleRight }, 2, row);
            var numAwardItem2 = new NumericUpDown { Minimum = 0, Maximum = 999999, Name = "numAwardItem2" };
            table.Controls.Add(numAwardItem2, 3, row);
            row++;


            // 奖励配置第三行
            table.Controls.Add(new Label { Text = "AwardItem3:", TextAlign = ContentAlignment.MiddleRight }, 0, row);
            var numAwardItem3 = new NumericUpDown { Minimum = 0, Maximum = 999999, Name = "numAwardItem3" };
            table.Controls.Add(numAwardItem3, 1, row);
            table.Controls.Add(new Label { Text = "AwardItem4:", TextAlign = ContentAlignment.MiddleRight }, 2, row);
            var numAwardItem4 = new NumericUpDown { Minimum = 0, Maximum = 999999, Name = "numAwardItem4" };
            table.Controls.Add(numAwardItem4, 3, row);
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

            /*
            // 保底调控概率配置区域
            var spaceProbabilityPanel = new Panel
            {
                Name = "spaceProbabilityPanel",
                Size = new Size(600, 200),
                MinimumSize = new Size(600, 200),
                MaximumSize = new Size(600, 500),
                AutoSize = false,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            table.Controls.Add(spaceProbabilityPanel, 0, row);
            table.SetColumnSpan(spaceProbabilityPanel, 4);
            row++;

            // 必定保底次数配置区域
            var spaceGuaranteePanel = new Panel
            {
                Name = "spaceGuaranteePanel",
                Size = new Size(600, 200),
                MinimumSize = new Size(600, 200),
                MaximumSize = new Size(600, 500),
                AutoSize = false,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            table.Controls.Add(spaceGuaranteePanel, 0, row);
            table.SetColumnSpan(spaceGuaranteePanel, 4);
            row++;
            */
            // 保存按钮
            var btnSaveLevel = new Button { Text = "保存关卡", Name = "btnSaveLevel" };
            btnSaveLevel.Click += (s, e) => SaveJson();
            table.Controls.Add(btnSaveLevel, 0, row);
            table.SetColumnSpan(btnSaveLevel, 4);
            
            groupBoxLevelInfo.Controls.Add(scrollPanel);
            
            // 绑定值变更事件（包含实时文本输入触发）
            numLV.ValueChanged += (s, e) => { 
                if (_current != null && !_isUpdatingUI) 
                { 
                    _current.LV = (int)numLV.Value; 
                    // LV变更后重新排序和更新UI
                    SortLevelsByLV();
                    UpdateUI();
                } 
            };
            AddRealTimeTrigger(numLV, () => {
                if (_current != null && !_isUpdatingUI)
                {
                    _current.LV = (int)numLV.Value;
                    SortLevelsByLV();
                    UpdateUI();
                }
            });
            
            cmbHardType.SelectedIndexChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.HardType = (int)cmbHardType.SelectedItem; };
            
            //chkRandomCar.CheckedChanged += (s, e) => { 
            //    if (_current != null && !_isUpdatingUI) { 
            //        _current.RandomCar = chkRandomCar.Checked; 
            //        randomCarPanel.Enabled = chkRandomCar.Checked;
            //        UpdateRandomCarPanel(randomCarPanel);
            //    } 
            //};
            //numTimeLimit.ValueChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.GameTimeLimit = (float)numTimeLimit.Value; };
            //chkEnableTimeLimit.CheckedChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.EnableTimeLimit = chkEnableTimeLimit.Checked; };
            
            numWidth.ValueChanged += (s, e) => { if (_current?.Grid != null && !_isUpdatingUI) { _current.Grid.Width = (int)numWidth.Value; panelPreview.Invalidate(); } };
            AddRealTimeTrigger(numWidth, () => {
                if (_current?.Grid != null && !_isUpdatingUI)
                {
                    _current.Grid.Width = (int)numWidth.Value;
                    panelPreview.Invalidate();
                }
            });
            
            numHeight.ValueChanged += (s, e) => { if (_current?.Grid != null && !_isUpdatingUI) { _current.Grid.Height = (int)numHeight.Value; panelPreview.Invalidate(); } };
            AddRealTimeTrigger(numHeight, () => {
                if (_current?.Grid != null && !_isUpdatingUI)
                {
                    _current.Grid.Height = (int)numHeight.Value;
                    panelPreview.Invalidate();
                }
            });
            //numCellSize.ValueChanged += (s, e) => { if (_current?.Grid != null && !_isUpdatingUI) _current.Grid.CellSize = (float)numCellSize.Value; };
            
            numAwardCoin.ValueChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.AwardCoin = (int)numAwardCoin.Value; };
            AddRealTimeTrigger(numAwardCoin, () => {
                if (_current != null && !_isUpdatingUI) _current.AwardCoin = (int)numAwardCoin.Value;
            });
            
            numAwardItem1.ValueChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.AwardItem1 = (int)numAwardItem1.Value; };
            AddRealTimeTrigger(numAwardItem1, () => {
                if (_current != null && !_isUpdatingUI) _current.AwardItem1 = (int)numAwardItem1.Value;
            });
            
            numAwardItem2.ValueChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.AwardItem2 = (int)numAwardItem2.Value; };
            AddRealTimeTrigger(numAwardItem2, () => {
                if (_current != null && !_isUpdatingUI) _current.AwardItem2 = (int)numAwardItem2.Value;
            });
            
            numAwardItem3.ValueChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.AwardItem3 = (int)numAwardItem3.Value; };
            AddRealTimeTrigger(numAwardItem3, () => {
                if (_current != null && !_isUpdatingUI) _current.AwardItem3 = (int)numAwardItem3.Value;
            });
            
            numAwardItem4.ValueChanged += (s, e) => { if (_current != null && !_isUpdatingUI) _current.AwardItem4 = (int)numAwardItem4.Value; };
            AddRealTimeTrigger(numAwardItem4, () => {
                if (_current != null && !_isUpdatingUI) _current.AwardItem4 = (int)numAwardItem4.Value;
            });

            // 初始化随机车辆配置面板
            randomCarPanel.Enabled = true;//_current?.RandomCar ?? false;
            UpdateRandomCarPanel(randomCarPanel);

            // 初始化保底调控配置面板
            //UpdateSpaceProbabilityPanel(spaceProbabilityPanel);
            //UpdateSpaceGuaranteePanel(spaceGuaranteePanel);
        }

        // 辅助方法：获取scrollPanel并保存滚动位置
        private Point SaveScrollPosition(out Panel scrollPanel)
        {
            scrollPanel = null;
            Point savedScrollPosition = Point.Empty;
            foreach (Control control2 in groupBoxLevelInfo.Controls)
            {
                if (control2 is Panel panel2 && panel2.Name == "scrollPanel")
                {
                    scrollPanel = panel2;
                    // AutoScrollPosition返回的是负数，保存时需要取反
                    var currentPos = panel2.AutoScrollPosition;
                    savedScrollPosition = new Point(-currentPos.X, -currentPos.Y);
                    break;
                }
            }
            return savedScrollPosition;
        }

        // 辅助方法：恢复滚动位置
        private void RestoreScrollPosition(Panel scrollPanel, Point savedScrollPosition)
        {
            if (scrollPanel != null && savedScrollPosition != Point.Empty)
            {
                // 延迟恢复滚动位置，确保布局完成后再设置
                scrollPanel.SuspendLayout();
                scrollPanel.AutoScrollPosition = savedScrollPosition;
                scrollPanel.ResumeLayout();
            }
        }

        private void UpdateLevelInfoControls()
        {
            if (_current == null) return;
            
            // 保存滚动位置
            Point savedScrollPosition = SaveScrollPosition(out Panel scrollPanel);
            
            foreach (Control control2 in groupBoxLevelInfo.Controls)
            {
                if (control2 is Panel panel2)
                {
                    foreach (Control control in panel2.Controls)
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
                                    //case "chkRandomCar":
                                    //    ((CheckBox)c).Checked = _current.RandomCar;
                                    //    break;
                                    //case "numTimeLimit":
                                    //    ((NumericUpDown)c).Value = (decimal)_current.GameTimeLimit;
                                    //    break;
                                    //case "chkEnableTimeLimit":
                                    //    ((CheckBox)c).Checked = _current.EnableTimeLimit;
                                    //    break;
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
                                        //panel.Enabled = _current.RandomCar;
                                        UpdateRandomCarPanel(panel);
                                        break;
                                    //case "spaceProbabilityPanel":
                                    //    UpdateSpaceProbabilityPanel((Panel)c);
                                    //    break;
                                    //case "spaceGuaranteePanel":
                                    //    UpdateSpaceGuaranteePanel((Panel)c);
                                    //    break;
                                }
                            }
                        }
                    }
                }
                
            }
            
            // 恢复滚动位置
            RestoreScrollPosition(scrollPanel, savedScrollPosition);
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
                //HasKey = false,
                //KayColorType = CarColorType.White.ToString(),
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

        // 根据Type获取对应的组名
        private string TypeToGroupName(string type)
        {
            if (string.IsNullOrEmpty(type)) return "Emptys";
            
            switch (type)
            {
                case "Park": return "Parks";
                case "PayPark": return "PayParks";
                case "Car": return "Cars";
                case "Empty": return "Emptys";
                case "Factory": return "Factorys";
                case "Box": return "Boxs";
                case "LockDoor": return "LockDoors";
                case "Wall":
                case "Hole":
                case "Item":
                default: return "Entities"; // 其他类型都归到Entities组
            }
        }

        // PropertyGrid属性值改变事件处理
        private void PropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (_current == null || _selectedGroup == null || _selectedIndex < 0) return;
            if (!_groups.ContainsKey(_selectedGroup) || _selectedIndex >= _groups[_selectedGroup].Count) return;
            
            var entity = _groups[_selectedGroup][_selectedIndex];
            if (entity == null) return;
            
            // 如果修改的是Type属性，需要将实体移动到对应的组
            if (e.ChangedItem.Label == "Type")
            {
                string newType = entity.Type ?? "";
                string newGroupName = TypeToGroupName(newType);
                
                // 如果新组名和当前组名不同，需要移动实体
                if (newGroupName != _selectedGroup)
                {
                    // 从当前组移除实体
                    _groups[_selectedGroup].RemoveAt(_selectedIndex);
                    
                    // 添加到新组
                    if (!_groups.ContainsKey(newGroupName))
                    {
                        _groups[newGroupName] = new List<GridEntityData>();
                    }
                    _groups[newGroupName].Add(entity);
                    
                    // 更新选中状态：选中新组中的这个实体
                    _selectedGroup = newGroupName;
                    _selectedIndex = _groups[newGroupName].Count - 1;
                    
                    // 更新UI
                    UpdateEntityTree();
                    
                    // 在树视图中选中新移动的实体
                    SelectEntityInTree(_selectedGroup, _selectedIndex);
                    
                    // 确保PropertyGrid显示移动后的实体
                    propertyGrid.SelectedObject = entity;
                    
                    // 刷新预览
                    panelPreview.Invalidate();
                    return; // Type变更后直接返回，不需要再执行通用的属性更新
                }
            }
            
            // 对于其他属性的变更，执行通用的更新逻辑
            if (!_isUpdatingUI)
            {
                // 重新构建实体树以更新显示文字（坐标、颜色等）
                UpdateEntityTree();

                // 重绘场景预览以显示新的位置和属性
                panelPreview.Invalidate();

                // 重新选中当前编辑的实体节点（因为UpdateEntityTree会清空选择）
                SelectEntityInTree(_selectedGroup, _selectedIndex);
            }
        }

        // 为NumericUpDown添加实时触发支持（文本输入即触发）
        private void AddRealTimeTrigger(NumericUpDown control, Action action)
        {
            control.KeyDown += (s, e) => {
                if (!_isUpdatingUI && action != null)
                {
                    try
                    {
                        action();
                    }
                    catch { } // 忽略解析错误，等待ValueChanged事件处理
                }
            };
            control.KeyUp += (s, e) => {
                if (!_isUpdatingUI && action != null)
                {
                    try
                    {
                        action();
                    }
                    catch { }
                }
            };
        }

        // PropertyGrid实时输入监听（KeyDown和KeyUp）
        private void PropertyGrid_KeyDown(object sender, KeyEventArgs e)
        {
            // 在输入过程中触发更新（实时反馈）
            TriggerPropertyGridUpdate();
        }

        private void PropertyGrid_KeyUp(object sender, KeyEventArgs e)
        {
            // 按键释放时触发更新（确认输入）
            TriggerPropertyGridUpdate();
        }

        // 触发PropertyGrid属性更新的辅助方法
        private void TriggerPropertyGridUpdate()
        {
            if (_isUpdatingUI || _current == null || _selectedGroup == null || _selectedIndex < 0) return;
            if (!_groups.ContainsKey(_selectedGroup) || _selectedIndex >= _groups[_selectedGroup].Count) return;

            var entity = _groups[_selectedGroup][_selectedIndex];
            if (entity == null || propertyGrid.SelectedObject != entity) return;

            // 检查是否有属性变化，如果有则触发更新
            var selectedItem = propertyGrid.SelectedGridItem;
            if (selectedItem != null && selectedItem.GridItemType == System.Windows.Forms.GridItemType.Property)
            {
                // 实时更新实体树和预览（不需要等待PropertyValueChanged）
                UpdateEntityTree();
                panelPreview.Invalidate();
                
                // 重新选中节点（UpdateEntityTree会清空选择）
                SelectEntityInTree(_selectedGroup, _selectedIndex);
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
            
            // 绘制坐标轴标签
            DrawGridLabels(g, content, cell, gw, gh);
            
            // 画实体
            DrawEntities(g, content, cell, gw, gh, _groups["Emptys"]);
            DrawEntities(g, content, cell, gw, gh, _groups["Entities"]);
            DrawEntities(g, content, cell, gw, gh, _groups["Parks"]);
            DrawEntities(g, content, cell, gw, gh, _groups["PayParks"]);
            DrawEntities(g, content, cell, gw, gh, _groups["Factorys"]);
            DrawEntities(g, content, cell, gw, gh, _groups["Cars"]);
            DrawEntities(g, content, cell, gw, gh, _groups["Boxs"]);
            DrawEntities(g, content, cell, gw, gh, _groups["LockDoors"]);

            // 绘制左上角统计信息
            DrawSceneCounters(g);
        }

        // 绘制场景关键数量信息（左上角）
        private void DrawSceneCounters(Graphics g)
        {
            if (_current == null || _current.Grid == null) return;

            int gridWidth = Math.Max(0, _current.Grid.Width);
            int gridHeight = Math.Max(0, _current.Grid.Height);
            int totalCells = gridWidth * gridHeight;

            // 统计占用格子
            var occupied = new HashSet<string>();
            void Mark(GridEntityData[] arr)
            {
                if (arr == null) return;
                foreach (var a in arr)
                {
                    if (a == null) continue;
                    int x = a.CellX;
                    int y = a.CellY;
                    if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) continue;
                    occupied.Add(x + "," + y);
                }
            }

            
            // 标记除Emptys以外的所有实体
            Mark(_groups["Parks"]?.ToArray());
            Mark(_groups["PayParks"]?.ToArray());
            Mark(_groups["Cars"]?.ToArray());
            Mark(_groups["Entities"]?.ToArray());
            Mark(_groups["Factorys"]?.ToArray());
            Mark(_groups["Boxs"]?.ToArray());
            Mark(_groups["Emptys"]?.ToArray());
            //Mark(_current.LockDoors);

            int emptyCells = Math.Max(0, totalCells - occupied.Count);

            // 统计各类车辆数量
            int totalCars = 0;
            if (_current.TotalCarCounts != null)
            {
                for (int i = 0; i < _current.TotalCarCounts.Length; i++)
                {
                    totalCars += Math.Max(0, _current.TotalCarCounts[i]);
                }
            }

            int carsCarCount = _groups["Cars"] != null ? _groups["Cars"].Count : 0;

            int factorysCarCount = 0;
            if (_groups["Factorys"] != null)
            {
                foreach (var f in _groups["Factorys"])
                {
                    if (f != null) factorysCarCount += Math.Max(0, f.IncludeCarCount);
                }
            }

            int boxsCarCount = _groups["Boxs"] != null ? _groups["Boxs"].Count : 0;

            // 组装文本
            string[] lines = new[]
            {
                "总车辆配置总数: " + totalCars,
                "",
                "场景空格子数量: " + emptyCells,
                "场景静态车辆数: " + carsCarCount,
                "车库总车辆数: " + factorysCarCount,
                "箱子总车辆数: " + boxsCarCount
            };

            string text = string.Join("\n", lines);

            using (var font = new Font("Microsoft YaHei", 9f, FontStyle.Regular))
            using (var bgBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
            using (var textBrush = new SolidBrush(Color.White))
            {
                SizeF size = g.MeasureString(text, font);
                var padding = 6;
                var rect = new RectangleF(5, 5, size.Width + padding * 2, size.Height + padding * 2);
                g.FillRectangle(bgBrush, rect);
                g.DrawString(text, font, textBrush, rect.Left + padding, rect.Top + padding);
            }
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

                // 先尝试用图像绘制
                var image = GetImageForEntity(entity);
                if (image != null)
                {
                    // 绘制图像，保持宽高比
                    g.DrawImage(image, rect);
                }
                else
                {
                    // 缺省用颜色块（保留原有逻辑）
                    Color color;
                    if (!_typeColors.TryGetValue(entity.Type ?? "Empty", out color))
                        color = Color.Gray;

                    // 如果是Car类型，根据颜色类型调整颜色
                    if ("Car".Equals(entity.Type, StringComparison.OrdinalIgnoreCase))
                    {
                        color = GetCarColor(entity.ColorType);
                    }

                    g.FillRectangle(new SolidBrush(color), rect);
                }

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

        private void DrawGridLabels(Graphics g, RectangleF content, int cell, int gw, int gh)
        {
            using (var font = new Font("Arial", 8, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(180, 180, 180)))
            using (var bgBrush = new SolidBrush(Color.FromArgb(46, 46, 46)))
            {
                // 绘制X轴标签（在底部）
                for (int x = 0; x < gw; x++)
                {
                    string label = x.ToString();
                    SizeF textSize = g.MeasureString(label, font);
                    float xx = content.X + x * cell + (cell - textSize.Width) / 2 ;
                    float yy = content.Bottom + 15;
                    
                    // 绘制背景（去除多余绘制）
                    g.FillRectangle(bgBrush, xx, yy - textSize.Height, textSize.Width, textSize.Height);
                    g.DrawString(label, font, brush, xx, yy - textSize.Height);
                }
                
                // 绘制Y轴标签（在左侧，注意Y坐标是翻转的）
                for (int y = 0; y < gh; y++)
                {
                    string label = (gh - 1 - y).ToString(); // 翻转显示
                    SizeF textSize = g.MeasureString(label, font);
                    float xx = content.X - textSize.Width - 2 - 5;
                    float yy = content.Y + y * cell + (cell - textSize.Height) / 2;
                    
                    // 绘制背景
                    g.FillRectangle(bgBrush, xx, yy, textSize.Width, textSize.Height);
                    g.DrawString(label, font, brush, xx, yy);
                }
            }
        }

        private Image GetImageForEntity(GridEntityData entity)
        {
            if (entity == null || string.IsNullOrEmpty(entity.Type)) return null;

            // 按类型映射到图像资源
            switch (entity.Type.ToLower())
            {
                case "wall":
                    return ImageResources.WallImage;

                case "park":
                    return ImageResources.ParkImage;

                case "paypark":
                    return ImageResources.ParkImage; // 使用相同的Park图像

                case "box":
                    return ImageResources.BoxImage;

                case "factory":
                    // 用方向选择工厂图像
                    if (ImageResources.FactoryImages.Count > 0)
                    {
                        if (Enum.TryParse<DirectionsType>(entity.Dir ?? DirectionsType.Down.ToString(), true, out var dir))
                        {
                            // 约定 Down/Up/Right/Left 对应 0/1/2/3
                            int idx = dir == DirectionsType.Down ? 0 :
                                      dir == DirectionsType.Up ? 1 :
                                      dir == DirectionsType.Right ? 2 : 3;
                            idx = Math.Max(0, Math.Min(idx, ImageResources.FactoryImages.Count - 1));
                            return ImageResources.FactoryImages[idx];
                        }
                        return ImageResources.FactoryImages[0];
                    }
                    return null;

                case "car":
                    // 按颜色选择车辆图像
                    if (ImageResources.CarImages.Count > 0)
                    {
                        if (Enum.TryParse<CarColorType>(entity.ColorType ?? CarColorType.White.ToString(), true, out var colorType))
                        {
                            int colorIndex = (int)colorType;
                            if (colorIndex >= 0 && colorIndex < ImageResources.CarImages.Count)
                                return ImageResources.CarImages[colorIndex];
                        }
                        return ImageResources.CarImages[0]; // 默认返回第一个
                    }
                    return null;

                case "lockdoor":
                    // 按颜色选择锁门图像 (使用Head图像)
                    if (ImageResources.LockDoorHeadImages.Count > 0)
                    {
                        if (Enum.TryParse<CarColorType>(entity.ColorType ?? CarColorType.White.ToString(), true, out var colorType))
                        {
                            int colorIndex = (int)colorType;
                            if (colorIndex >= 0 && colorIndex < ImageResources.LockDoorHeadImages.Count)
                                return ImageResources.LockDoorHeadImages[colorIndex];
                        }
                        return ImageResources.LockDoorHeadImages[0];
                    }
                    return null;

                case "empty":
                case "hole":
                case "item":
                default:
                    return null; // 这些类型暂时没有图像，使用颜色块
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
            
            // 保存滚动位置
            Point savedScrollPosition = SaveScrollPosition(out Panel scrollPanel);
            
            panel.Controls.Clear();
            
            //if (!_current.RandomCar) 
            //{
            //    // 当RandomCar为false时，显示提示信息
            //    var lblDisabled = new Label 
            //    { 
            //        Text = "随机车辆配置已禁用", 
            //        ForeColor = System.Drawing.SystemColors.GrayText,
            //        AutoSize = true,
            //        Margin = new Padding(0, 10, 0, 0)
            //    };
            //
            //    panel.Size = panel.MaximumSize = panel.MinimumSize = new Size(600, 50); // 最小100px，最大400px
            //
            //    panel.Controls.Add(lblDisabled);
            //   
            //    // 恢复滚动位置
            //    RestoreScrollPosition(scrollPanel, savedScrollPosition);
            //    return;
            //}
            
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
                Text = "总车辆配置", 
                Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold),
                AutoSize = true
            };
            layout.Controls.Add(lblTitle, 0, 0);
            layout.SetColumnSpan(lblTitle, 4);
            
            // 添加数量控制
            int curSize = Math.Max(_current.TotalCarCounts?.Length ?? 0,
                _current.TotalCarColorTypes?.Length ?? 0);
            
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
                    _current.TotalCarCounts = ResizeArray(_current.TotalCarCounts, newSize);
                    _current.TotalCarColorTypes = ResizeArray(_current.TotalCarColorTypes, newSize);
                    UpdateRandomCarPanel(panel);
                    panelPreview.Invalidate();
                }
            };
            AddRealTimeTrigger(numSize, () => {
                if (!_isUpdatingUI)
                {
                    int newSize = (int)numSize.Value;
                    _current.TotalCarCounts = ResizeArray(_current.TotalCarCounts, newSize);
                    _current.TotalCarColorTypes = ResizeArray(_current.TotalCarColorTypes, newSize);
                    UpdateRandomCarPanel(panel);
                    panelPreview.Invalidate();
                }
            });
            
            layout.Controls.Add(new Label { Text = "车辆种类数量:", AutoSize = true }, 0, 1);
            layout.Controls.Add(numSize, 1, 1);
            
            // 确保数组初始化
            if (_current.TotalCarCounts == null) _current.TotalCarCounts = new int[curSize];
            if (_current.TotalCarColorTypes == null) _current.TotalCarColorTypes = new string[curSize];
            
            // 确保数组长度一致
            if (_current.TotalCarCounts.Length != curSize)
                _current.TotalCarCounts = ResizeArray(_current.TotalCarCounts, curSize);
            if (_current.TotalCarColorTypes.Length != curSize)
                _current.TotalCarColorTypes = ResizeArray(_current.TotalCarColorTypes, curSize);
            
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
                string colorTypeStr = (i < _current.TotalCarColorTypes.Length && _current.TotalCarColorTypes[i] != null) 
                    ? _current.TotalCarColorTypes[i] 
                    : CarColorType.White.ToString();
                    
                // 设置选中项
                if (Enum.TryParse<CarColorType>(colorTypeStr, true, out var colorEnum))
                {
                    cmbColor.SelectedItem = colorEnum;
                }
                else
                {
                    cmbColor.SelectedItem = CarColorType.White;
                    _current.TotalCarColorTypes[i] = CarColorType.White.ToString();
                }
                
                cmbColor.SelectedIndexChanged += (s, e) => {
                    if (!_isUpdatingUI)
                    {
                        var cb = (ComboBox)s;
                        int index = (int)cb.Tag;
                        _current.TotalCarColorTypes[index] = cb.SelectedItem.ToString();
                    }
                };
                
                // 数量
                int countValue = (i < _current.TotalCarCounts.Length) ? _current.TotalCarCounts[i] : 0;
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
                        _current.TotalCarCounts[index] = (int)num.Value;
                        panelPreview.Invalidate();
                    }
                };
                AddRealTimeTrigger(numCount, () => {
                    if (!_isUpdatingUI)
                    {
                        int index = (int)numCount.Tag;
                        _current.TotalCarCounts[index] = (int)numCount.Value;
                        panelPreview.Invalidate();
                    }
                });
                
                layout.Controls.Add(new Label { Text = $"颜色[{i}]:", AutoSize = true }, 0, row);
                layout.Controls.Add(cmbColor, 1, row);
                layout.Controls.Add(new Label { Text = $"数量[{i}]:", AutoSize = true }, 2, row);
                layout.Controls.Add(numCount, 3, row);
            }

            int dynamicHeight = 50 + (curSize * 25) + 10; // 基础高度 + 配置行数 * 行高 + 余量
            panel.Size = panel.MaximumSize = panel.MinimumSize = new Size(600, Math.Max(50, Math.Min(dynamicHeight, 300))); // 最小100px，最大400px

            panel.Controls.Add(layout);
            
            // 恢复滚动位置
            RestoreScrollPosition(scrollPanel, savedScrollPosition);
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

        private SpaceProbabilityData[] ResizeSpaceProbabilityArray(SpaceProbabilityData[] src, int newSize)
        {
            if (newSize < 0) newSize = 0;
            var dst = new SpaceProbabilityData[newSize];
            if (src != null)
            {
                Array.Copy(src, dst, Math.Min(src.Length, dst.Length));
                for (int i = src.Length; i < newSize; i++)
                {
                    dst[i] = new SpaceProbabilityData { Space = 1, RestrictNoMatch = false, Probability = 50 };
                }
            }
            else
            {
                for (int i = 0; i < newSize; i++)
                {
                    dst[i] = new SpaceProbabilityData { Space = 1, RestrictNoMatch = false, Probability = 50 };
                }
            }
            return dst;
        }

        private SpaceGuaranteeData[] ResizeSpaceGuaranteeArray(SpaceGuaranteeData[] src, int newSize)
        {
            if (newSize < 0) newSize = 0;
            var dst = new SpaceGuaranteeData[newSize];
            if (src != null)
            {
                Array.Copy(src, dst, Math.Min(src.Length, dst.Length));
                for (int i = src.Length; i < newSize; i++)
                {
                    dst[i] = new SpaceGuaranteeData { Space = 1, RestrictNoMatch = false, Count = 0 };
                }
            }
            else
            {
                for (int i = 0; i < newSize; i++)
                {
                    dst[i] = new SpaceGuaranteeData { Space = 1, RestrictNoMatch = false, Count = 0 };
                }
            }
            return dst;
        }

        // 关卡排序方法
        private void SortLevelsByLV()
        {
            if (_container?.Levels == null || _container.Levels.Count <= 1) return;

            // 保存当前选中关卡的LV，以便排序后重新定位
            int currentLV = -1;
            if (_currentIndex >= 0 && _currentIndex < _container.Levels.Count)
            {
                currentLV = _container.Levels[_currentIndex].LV;
            }

            // 按LV排序
            _container.Levels.Sort((a, b) => a.LV.CompareTo(b.LV));

            // 排序后重新找到当前关卡的位置
            if (currentLV >= 0)
            {
                for (int i = 0; i < _container.Levels.Count; i++)
                {
                    if (_container.Levels[i].LV == currentLV)
                    {
                        _currentIndex = i;
                        break;
                    }
                }
            }
            
            // 确保索引在有效范围内
            _currentIndex = Math.Max(0, Math.Min(_currentIndex, _container.Levels.Count - 1));
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

        /*
        // 保底调控概率配置面板更新方法
        private void UpdateSpaceProbabilityPanel(Panel panel)
        {
            if (_current == null) return;

            // 保存滚动位置
            Point savedScrollPosition = SaveScrollPosition(out Panel scrollPanel);

            panel.Controls.Clear();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 6,
                Margin = new Padding(0, 10, 0, 0)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

            // 添加标题
            var lblTitle = new Label
            {
                Text = "保底调控概率配置（SpaceProbabilityConfigs）",
                Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold),
                AutoSize = true
            };
            layout.Controls.Add(lblTitle, 0, 0);
            layout.SetColumnSpan(lblTitle, 6);

            // 添加数量控制
            int curSize = _current.SpaceProbabilityConfigs?.Length ?? 0;

            var numSize = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 20,
                Value = curSize,
                Name = "numSpaceProbabilitySize"
            };
            numSize.ValueChanged += (s, e) =>
            {
                if (!_isUpdatingUI)
                {
                    int newSize = (int)numSize.Value;
                    _current.SpaceProbabilityConfigs = ResizeSpaceProbabilityArray(_current.SpaceProbabilityConfigs, newSize);
                    UpdateSpaceProbabilityPanel(panel);
                }
            };
            AddRealTimeTrigger(numSize, () => {
                if (!_isUpdatingUI)
                {
                    int newSize = (int)numSize.Value;
                    _current.SpaceProbabilityConfigs = ResizeSpaceProbabilityArray(_current.SpaceProbabilityConfigs, newSize);
                    UpdateSpaceProbabilityPanel(panel);
                }
            });

            layout.Controls.Add(new Label { Text = "配置数量:", AutoSize = true }, 0, 1);
            layout.Controls.Add(numSize, 1, 1);
            layout.SetColumnSpan(numSize, 5);

            // 初始化数组
            if (_current.SpaceProbabilityConfigs == null) _current.SpaceProbabilityConfigs = new SpaceProbabilityData[curSize];

            // 添加每个配置项
            for (int i = 0; i < curSize; i++)
            {
                int row = i + 2;
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                if (_current.SpaceProbabilityConfigs[i] == null)
                    _current.SpaceProbabilityConfigs[i] = new SpaceProbabilityData { Space = 1, RestrictNoMatch = false, Probability = 50 };

                // 剩余车位数
                var numSpace = new NumericUpDown
                {
                    Minimum = 1,
                    Maximum = 7,
                    Value = _current.SpaceProbabilityConfigs[i].Space,
                    Tag = i
                };
                numSpace.ValueChanged += (s, e) =>
                {
                    if (!_isUpdatingUI)
                    {
                        var num = (NumericUpDown)s;
                        int index = (int)num.Tag;
                        _current.SpaceProbabilityConfigs[index].Space = (int)num.Value;
                    }
                };
                AddRealTimeTrigger(numSpace, () => {
                    if (!_isUpdatingUI)
                    {
                        int index = (int)numSpace.Tag;
                        _current.SpaceProbabilityConfigs[index].Space = (int)numSpace.Value;
                    }
                });

                // 是否限定无匹配
                var chkRestrict = new CheckBox
                {
                    Checked = _current.SpaceProbabilityConfigs[i].RestrictNoMatch,
                    Tag = i
                };
                chkRestrict.CheckedChanged += (s, e) =>
                {
                    if (!_isUpdatingUI)
                    {
                        var cb = (CheckBox)s;
                        int index = (int)cb.Tag;
                        _current.SpaceProbabilityConfigs[index].RestrictNoMatch = cb.Checked;
                    }
                };

                // 概率
                var numProbability = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = (decimal)_current.SpaceProbabilityConfigs[i].Probability,
                    DecimalPlaces = 2,
                    Increment = 1,
                    Tag = i
                };
                numProbability.ValueChanged += (s, e) =>
                {
                    if (!_isUpdatingUI)
                    {
                        var num = (NumericUpDown)s;
                        int index = (int)num.Tag;
                        _current.SpaceProbabilityConfigs[index].Probability = (float)num.Value;
                    }
                };
                AddRealTimeTrigger(numProbability, () => {
                    if (!_isUpdatingUI)
                    {
                        int index = (int)numProbability.Tag;
                        _current.SpaceProbabilityConfigs[index].Probability = (float)numProbability.Value;
                    }
                });

                layout.Controls.Add(new Label { Text = $"[{i}]车位数:", AutoSize = true }, 0, row);
                layout.Controls.Add(numSpace, 1, row);
                layout.Controls.Add(new Label { Text = "限定无匹配:", AutoSize = true }, 2, row);
                layout.Controls.Add(chkRestrict, 3, row);
                layout.Controls.Add(new Label { Text = "概率:", AutoSize = true }, 4, row);
                layout.Controls.Add(numProbability, 5, row);
            }

            int dynamicHeight = 80 + (curSize * 60) + 10;
            panel.Size = panel.MaximumSize = panel.MinimumSize = new Size(600, Math.Max(50, Math.Min(dynamicHeight, 500)));

            panel.Controls.Add(layout);
            
            // 恢复滚动位置
            RestoreScrollPosition(scrollPanel, savedScrollPosition);
        }

        // 必定保底次数配置面板更新方法
        private void UpdateSpaceGuaranteePanel(Panel panel)
        {
            if (_current == null) return;

            // 保存滚动位置
            Point savedScrollPosition = SaveScrollPosition(out Panel scrollPanel);

            panel.Controls.Clear();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 6,
                Margin = new Padding(0, 10, 0, 0)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

            // 添加标题
            var lblTitle = new Label
            {
                Text = "必定保底次数配置（SpaceGuaranteeConfigs）",
                Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold),
                AutoSize = true
            };
            layout.Controls.Add(lblTitle, 0, 0);
            layout.SetColumnSpan(lblTitle, 6);

            // 添加数量控制
            int curSize = _current.SpaceGuaranteeConfigs?.Length ?? 0;

            var numSize = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 20,
                Value = curSize,
                Name = "numSpaceGuaranteeSize"
            };
            numSize.ValueChanged += (s, e) =>
            {
                if (!_isUpdatingUI)
                {
                    int newSize = (int)numSize.Value;
                    _current.SpaceGuaranteeConfigs = ResizeSpaceGuaranteeArray(_current.SpaceGuaranteeConfigs, newSize);
                    UpdateSpaceGuaranteePanel(panel);
                }
            };
            AddRealTimeTrigger(numSize, () => {
                if (!_isUpdatingUI)
                {
                    int newSize = (int)numSize.Value;
                    _current.SpaceGuaranteeConfigs = ResizeSpaceGuaranteeArray(_current.SpaceGuaranteeConfigs, newSize);
                    UpdateSpaceGuaranteePanel(panel);
                }
            });

            layout.Controls.Add(new Label { Text = "配置数量:", AutoSize = true }, 0, 1);
            layout.Controls.Add(numSize, 1, 1);
            layout.SetColumnSpan(numSize, 5);

            // 初始化数组
            if (_current.SpaceGuaranteeConfigs == null) _current.SpaceGuaranteeConfigs = new SpaceGuaranteeData[curSize];

            // 添加每个配置项
            for (int i = 0; i < curSize; i++)
            {
                int row = i + 2;
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                if (_current.SpaceGuaranteeConfigs[i] == null)
                    _current.SpaceGuaranteeConfigs[i] = new SpaceGuaranteeData { Space = 1, RestrictNoMatch = false, Count = 0 };

                // 剩余车位数
                var numSpace = new NumericUpDown
                {
                    Minimum = 1,
                    Maximum = 7,
                    Value = _current.SpaceGuaranteeConfigs[i].Space,
                    Tag = i
                };
                numSpace.ValueChanged += (s, e) =>
                {
                    if (!_isUpdatingUI)
                    {
                        var num = (NumericUpDown)s;
                        int index = (int)num.Tag;
                        _current.SpaceGuaranteeConfigs[index].Space = (int)num.Value;
                    }
                };
                AddRealTimeTrigger(numSpace, () => {
                    if (!_isUpdatingUI)
                    {
                        int index = (int)numSpace.Tag;
                        _current.SpaceGuaranteeConfigs[index].Space = (int)numSpace.Value;
                    }
                });

                // 是否限定无匹配
                var chkRestrict = new CheckBox
                {
                    Checked = _current.SpaceGuaranteeConfigs[i].RestrictNoMatch,
                    Tag = i
                };
                chkRestrict.CheckedChanged += (s, e) =>
                {
                    if (!_isUpdatingUI)
                    {
                        var cb = (CheckBox)s;
                        int index = (int)cb.Tag;
                        _current.SpaceGuaranteeConfigs[index].RestrictNoMatch = cb.Checked;
                    }
                };

                // 必定触发次数
                var numCount = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 999,
                    Value = _current.SpaceGuaranteeConfigs[i].Count,
                    Tag = i
                };
                numCount.ValueChanged += (s, e) =>
                {
                    if (!_isUpdatingUI)
                    {
                        var num = (NumericUpDown)s;
                        int index = (int)num.Tag;
                        _current.SpaceGuaranteeConfigs[index].Count = (int)num.Value;
                    }
                };
                AddRealTimeTrigger(numCount, () => {
                    if (!_isUpdatingUI)
                    {
                        int index = (int)numCount.Tag;
                        _current.SpaceGuaranteeConfigs[index].Count = (int)numCount.Value;
                    }
                });

                layout.Controls.Add(new Label { Text = $"[{i}]车位数:", AutoSize = true }, 0, row);
                layout.Controls.Add(numSpace, 1, row);
                layout.Controls.Add(new Label { Text = "限定无匹配:", AutoSize = true }, 2, row);
                layout.Controls.Add(chkRestrict, 3, row);
                layout.Controls.Add(new Label { Text = "触发次数:", AutoSize = true }, 4, row);
                layout.Controls.Add(numCount, 5, row);
            }

            int dynamicHeight = 80 + (curSize * 60) + 10;
            panel.Size = panel.MaximumSize = panel.MinimumSize = new Size(600, Math.Max(50, Math.Min(dynamicHeight, 500)));

            panel.Controls.Add(layout);
            
            // 恢复滚动位置
            RestoreScrollPosition(scrollPanel, savedScrollPosition);
        }
        */
    }
}
