#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using ReCarMatch.Levels;
using ReCarMatch.Game; // CarColorType, DirectionsType
using ReCarMatch.Framework.Resources;
using System.Text;

namespace ReCarMatch.EditorTools.Levels
{
	public class LevelsJsonEditorWindow : EditorWindow
	{
		const string JsonAssetPath = "Assets/Resources/LevelConfigs/levels.json";
		// 布局常量
		const float TopPanelHeight = 160f;
		static readonly Vector2 LevelPopupSize = new Vector2(260f, 360f);

		// 数据容器（JSON结构）
		[Serializable]
		class Container
		{
			public List<LevelData> Levels = new List<LevelData>();
		}

		// 运行时编辑缓存
		private Container _container;
		private LevelData _current;
		private int _currentIndex = -1;
		private bool _randomCarFoldout = true;

		// 各分组列表缓存：便于增删
		private readonly string[] _groupsOrder = new[]
		{
			"Parks","PayParks","Cars","Entities","Emptys","Factorys","Boxs","LockDoors"
		};
		private readonly Dictionary<string, List<GridEntityData>> _groups = new Dictionary<string, List<GridEntityData>>();
		private readonly Dictionary<string, bool> _groupFoldout = new Dictionary<string, bool>();

		// 选择状态
		private string _selectedGroup = null;
		private int _selectedIndex = -1;

		// 滚动与布局
		private Vector2 _leftScroll;
		private Vector2 _rightScroll;
		private int _cellSize = 28;
		private int _gridPadding = 8;

		// 颜色映射（预览）
		private readonly Dictionary<string, Color> _typeColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
		{
			{ "Empty", new Color(0.95f,0.95f,0.95f,1f) },
			{ "Wall", new Color(0.35f,0.35f,0.35f,1f) },
			{ "Hole", new Color(0.15f,0.15f,0.15f,1f) },
			{ "Item", new Color(0.2f,0.6f,0.9f,1f) },
			{ "Car", new Color(0.9f,0.9f,0.2f,1f) },
			{ "Park", new Color(0.2f,0.9f,0.2f,1f) },
			{ "PayPark", new Color(0.2f,0.9f,0.6f,1f) },
			{ "Factory", new Color(0.8f,0.4f,0.15f,1f) },
			{ "Box", new Color(0.7f,0.5f,0.2f,1f) },
            { "LockDoor", new Color(0.9f,0.6f,0.3f,1f) },
        };

		[MenuItem("Tools/Levels JSON 编辑器")]
		public static void Open()
		{
			var win = GetWindow<LevelsJsonEditorWindow>("Levels JSON 编辑器");
			win.minSize = new Vector2(980, 580);
			win.Show();
		}

		void OnEnable()
		{
			// 确保静态资源已初始化（编辑器下也可安全调用）
			StaticResources.Init();
			LoadJson();
			if (_container.Levels.Count == 0)
			{
				_container.Levels.Add(CreateDefaultLevel(1));
			}
			SetCurrentIndex(0);
		}

		void SetCurrentIndex(int idx)
		{
			if (idx < 0 || idx >= _container.Levels.Count) return;
			_currentIndex = idx;
			_current = _container.Levels[_currentIndex];
			BuildGroupCaches();
			ClearSelection();
			Repaint();
		}

		void BuildGroupCaches()
		{
			_groups.Clear();
			_groupFoldout.Clear();
			foreach (var g in _groupsOrder)
			{
				_groupFoldout[g] = true;
				_groups[g] = GetArrayByName(g)?.ToList() ?? new List<GridEntityData>();
			}
		}

		GridEntityData[] GetArrayByName(string name)
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
                default: return Array.Empty<GridEntityData>();
			}
		}

		void ApplyGroupsToCurrent()
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

		void ClearSelection()
		{
			_selectedGroup = null;
			_selectedIndex = -1;
		}

		LevelData CreateDefaultLevel(int lv)
		{
			return new LevelData
			{
				LV = lv,
				HardType = (int)LevelHardType.Normal,
				RandomCar = false,
				GameTimeLimit = 0,
				EnableTimeLimit = false,
				Grid = new GridData { Width = 7, Height = 11, CellSize = 64f },
				Parks = Array.Empty<GridEntityData>(),
				PayParks = Array.Empty<GridEntityData>(),
				Cars = Array.Empty<GridEntityData>(),
				Entities = Array.Empty<GridEntityData>(),
				Emptys = Array.Empty<GridEntityData>(),
				Factorys = Array.Empty<GridEntityData>(),
				Boxs = Array.Empty<GridEntityData>(),
                LockDoors = Array.Empty<GridEntityData>(),
            };
		}

		LevelData CreateLevelFromBase(int lv, LevelData baseLv)
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
				// 仅复制 Emptys, PayParks, Parks
				Emptys = baseLv.Emptys != null ? baseLv.Emptys.Select(CloneEntity).ToArray() : Array.Empty<GridEntityData>(),
				PayParks = baseLv.PayParks != null ? baseLv.PayParks.Select(CloneEntity).ToArray() : Array.Empty<GridEntityData>(),
				Parks = baseLv.Parks != null ? baseLv.Parks.Select(CloneEntity).ToArray() : Array.Empty<GridEntityData>(),
				// 其他清空
				Cars = Array.Empty<GridEntityData>(),
				Entities = Array.Empty<GridEntityData>(),
				Factorys = Array.Empty<GridEntityData>(),
				Boxs = Array.Empty<GridEntityData>(),
                LockDoors = Array.Empty<GridEntityData>(),
                RandomCarColorTypes = baseLv.RandomCarColorTypes,
				RandomCarCounts = baseLv.RandomCarCounts,
                AwardCoin = baseLv.AwardCoin,
				AwardItem1 = baseLv.AwardItem1,
                AwardItem2 = baseLv.AwardItem2,
                AwardItem3 = baseLv.AwardItem3,
                AwardItem4 = baseLv.AwardItem4,
            };
		}

		GridEntityData CloneEntity(GridEntityData s)
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

		void LoadJson()
		{
			var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonAssetPath);
			if (ta == null || string.IsNullOrEmpty(ta.text))
			{
				_container = new Container{ Levels = new List<LevelData>() };
				return;
			}
			try
			{
				var raw = JsonConvert.DeserializeObject<LevelDataContainer>(ta.text);
				_container = new Container{ Levels = raw?.Levels ?? new List<LevelData>() };
			}
			catch (Exception e)
			{
				Debug.LogError("解析 levels.json 失败: " + e.Message);
				_container = new Container{ Levels = new List<LevelData>() };
			}
		}

		void SaveJson()
		{
			if (_current != null)
			{
				ApplyGroupsToCurrent();
			}
			// 校验当前关卡
			if (!ValidateLevel(_current, out var dialogTitle, out var dialogMsg))
			{
				EditorUtility.DisplayDialog(dialogTitle, dialogMsg, "确定");
				return;
			}

			var raw = new LevelDataContainer{ Levels = _container.Levels };
			var json = JsonConvert.SerializeObject(raw, Formatting.Indented);
			File.WriteAllText(JsonAssetPath, json);
			AssetDatabase.Refresh();
			EditorUtility.DisplayDialog("保存成功", dialogMsg, "确定");
		}

		bool ValidateLevel(LevelData level, out string title, out string message)
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
				{"Emptys", level.Emptys ?? Array.Empty<GridEntityData>()},
				{"Entities", level.Entities ?? Array.Empty<GridEntityData>()},
				{"Parks", level.Parks ?? Array.Empty<GridEntityData>()},
				{"PayParks", level.PayParks ?? Array.Empty<GridEntityData>()},
				{"Factorys", level.Factorys ?? Array.Empty<GridEntityData>()},
				{"Boxs", level.Boxs ?? Array.Empty<GridEntityData>()},
                {"LockDoors", level.LockDoors ?? Array.Empty<GridEntityData>()},
                {"Cars", level.Cars ?? Array.Empty<GridEntityData>()},
			};

			// 跨组重复记录（作为警告，避免误报）
			var globalCells = new Dictionary<string, string>(); // key:"x,y" -> groupName

			foreach (var kv in groupMap)
			{
				ValidateList(kv.Key, kv.Value, level.Grid, errors, warnings, ref errorCount, ref warningCount, globalCells);
			}

			// RandomCar 额外校验：空位数量与 RandomCarCounts 总和一致
			if (errorCount == 0 && level.RandomCar)
			{
				int countsLen = level.RandomCarCounts != null ? level.RandomCarCounts.Length : 0;
				int colorsLen = level.RandomCarColorTypes != null ? level.RandomCarColorTypes.Length : 0;
				if (countsLen == 0 || colorsLen == 0 || countsLen != colorsLen)
				{
					errors.AppendLine("[错误] RandomCar 启用时，RandomCarCounts 与 RandomCarColorTypes 必须长度一致且大于 0。");
					errorCount++;
				}
				else
				{
					int totalRandomCars = 0;
					for (int i = 0; i < countsLen; i++) totalRandomCars += Mathf.Max(0, level.RandomCarCounts[i]);

					// 计算当前场景中“没有实体”的格子数（排除 Emptys，按实际实体占用统计）
					int gw = Mathf.Max(0, level.Grid?.Width ?? 0);
					int gh = Mathf.Max(0, level.Grid?.Height ?? 0);
					int totalCells = gw * gh;
					var occupied = new HashSet<string>();
					void Mark(GridEntityData[] arr)
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
					Mark(level.Entities);
					Mark(level.Parks);
					Mark(level.PayParks);
					Mark(level.Cars);
					Mark(level.Factorys);
                    Mark(level.Emptys);
                    //Mark(level.Boxs);
                    //Mark(level.LockDoors);

                    int emptyCells = Mathf.Max(0, totalCells - occupied.Count);
					if (totalRandomCars != emptyCells)
					{
						errors.AppendLine($"[错误] RandomCarCounts 总数({totalRandomCars}) 与场景空格子数({emptyCells})不相等。");
						errorCount++;
					}
				}
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

		void ValidateList(
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

		void OnGUI()
		{
			DrawToolbar();
			using (new GUILayout.VerticalScope(GUILayout.Height(TopPanelHeight)))
			{
				DrawTopLevelInfo();
			}
			using (new GUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
			{
				DrawBottomSplit();
			}
			// 任意编辑器操作都重绘
			if (Event.current.type == EventType.MouseDown ||
			    Event.current.type == EventType.MouseUp ||
			    Event.current.type == EventType.MouseDrag ||
			    GUI.changed)
			{
				Repaint();
			}
		}

		void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
				{
					LoadJson();
					if (_container.Levels.Count > 0) SetCurrentIndex(Mathf.Clamp(_currentIndex, 0, _container.Levels.Count - 1));
					Repaint();
				}
				if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(60)))
				{
					SaveJson();
				}
				GUILayout.FlexibleSpace();

				DrawLevelSelectorToolbar();

				if (GUILayout.Button("新增关卡", EditorStyles.toolbarButton, GUILayout.Width(80)))
				{
					int nextLv = _container.Levels.Count == 0 ? 1 : (_container.Levels.Max(l => l.LV) + 1);
					var baseLv = _container.Levels.FirstOrDefault(l => l.LV == 1) ?? _container.Levels.FirstOrDefault();
					LevelData newLv = baseLv != null ? CreateLevelFromBase(nextLv, baseLv) : CreateDefaultLevel(nextLv);
					_container.Levels.Add(newLv);
					SetCurrentIndex(_container.Levels.Count - 1);
				}
				if (GUILayout.Button("删除当前", EditorStyles.toolbarButton, GUILayout.Width(80)))
				{
					if (_currentIndex >= 0 && _currentIndex < _container.Levels.Count)
					{
						_container.Levels.RemoveAt(_currentIndex);
						SetCurrentIndex(Mathf.Clamp(_currentIndex - 1, 0, Mathf.Max(0, _container.Levels.Count - 1)));
					}
				}
			}
		}

		void DrawLevelSelectorToolbar()
		{
			string label = _currentIndex >= 0 && _currentIndex < _container.Levels.Count
				? $"关卡选择: (LV {_container.Levels[_currentIndex].LV})"
				: "关卡选择";
			var content = new GUIContent(label);
			var style = EditorStyles.toolbarDropDown;
			Rect r = GUILayoutUtility.GetRect(220, EditorGUIUtility.singleLineHeight, style, GUILayout.Width(LevelPopupSize.x));
			if (EditorGUI.DropdownButton(r, content, FocusType.Keyboard, style))
			{
				var popup = new LevelListPopup(_container.Levels, _currentIndex, (idx) =>
				{
					SetCurrentIndex(idx);
				});
				PopupWindow.Show(r, popup);
			}
		}

		class LevelListPopup : PopupWindowContent
		{
			readonly List<LevelData> _levels;
			int _selectedIndex;
			readonly Action<int> _onSelect;
			Vector2 _scroll;

			public LevelListPopup(List<LevelData> levels, int selectedIndex, Action<int> onSelect)
			{
				_levels = levels ?? new List<LevelData>();
				_selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, _levels.Count - 1));
				_onSelect = onSelect;
			}

			public override Vector2 GetWindowSize()
			{
				return LevelPopupSize;
			}

			public override void OnGUI(Rect rect)
			{
				GUILayout.Label("选择关卡", EditorStyles.boldLabel);
				using (var sv = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.ExpandHeight(true)))
				{
					_scroll = sv.scrollPosition;
					for (int i = 0; i < _levels.Count; i++)
					{
						var lv = _levels[i];
						bool isSel = (i == _selectedIndex);
						var rowStyle = isSel ? EditorStyles.toolbarButton : EditorStyles.miniButton;
						string text = $"LV {lv?.LV}";
						if (GUILayout.Button(text, rowStyle, GUILayout.Height(22)))
						{
							_selectedIndex = i;
							_onSelect?.Invoke(i);
							editorWindow.Close();
							GUIUtility.ExitGUI();
						}
					}
				}
			}
		}

		void DrawTopLevelInfo()
		{
			if (_current == null)
			{
				EditorGUILayout.HelpBox("没有可编辑的关卡。请使用工具栏新增关卡。", MessageType.Info);
				return;
			}

			using (new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("关卡信息", EditorStyles.boldLabel);

				float prevLabel = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 100f; // 紧凑的标签宽度

				// 第一行：LV / RandomCar
				using (new EditorGUILayout.HorizontalScope())
				{
					using (new GUILayout.VerticalScope(GUILayout.Width(220)))
					{
						_current.LV = EditorGUILayout.IntField("LV", _current.LV);
					}
					using (new GUILayout.VerticalScope(GUILayout.Width(220)))
					{
						// HardType 枚举编辑（LevelData.HardType 是 int）
						var hardEnum = (LevelHardType)Mathf.Clamp(_current.HardType, 0, int.MaxValue);
						var newHard = (LevelHardType)EditorGUILayout.EnumPopup("HardType", hardEnum);
						if (newHard != hardEnum) _current.HardType = (int)newHard;
					}
					GUILayout.FlexibleSpace();
				}

				// RandomCar 单独一行，保持紧凑
				using (new EditorGUILayout.HorizontalScope())
				{
					using (new GUILayout.VerticalScope(GUILayout.Width(220)))
					{
						_current.RandomCar = EditorGUILayout.Toggle("RandomCar", _current.RandomCar);
					}
					GUILayout.FlexibleSpace();
				}

				// 第二行：GameTimeLimit / EnableTimeLimit
				using (new EditorGUILayout.HorizontalScope())
				{
					using (new GUILayout.VerticalScope(GUILayout.Width(220)))
					{
						_current.GameTimeLimit = EditorGUILayout.FloatField("GameTimeLimit", _current.GameTimeLimit);
					}
					using (new GUILayout.VerticalScope(GUILayout.Width(220)))
					{
						_current.EnableTimeLimit = EditorGUILayout.Toggle("EnableTimeLimit", _current.EnableTimeLimit);
					}
					GUILayout.FlexibleSpace();
				}

				// 第三行：Grid.Width / Grid.Height
				using (new EditorGUILayout.HorizontalScope())
				{
					using (new GUILayout.VerticalScope(GUILayout.Width(220)))
					{
						_current.Grid.Width = EditorGUILayout.IntField("Grid.Width", _current.Grid?.Width ?? 0);
					}
					using (new GUILayout.VerticalScope(GUILayout.Width(220)))
					{
						_current.Grid.Height = EditorGUILayout.IntField("Grid.Height", _current.Grid?.Height ?? 0);
					}
					GUILayout.FlexibleSpace();
				}

				// 随机车配置编辑
				using (new EditorGUILayout.VerticalScope())
				{
					EditorGUI.BeginDisabledGroup(!_current.RandomCar);
					_randomCarFoldout = EditorGUILayout.Foldout(_randomCarFoldout, "随机车辆配置", true);
					if (_randomCarFoldout && _current.RandomCar)
					{
						int curSize = Mathf.Max(_current.RandomCarCounts != null ? _current.RandomCarCounts.Length : 0,
							_current.RandomCarColorTypes != null ? _current.RandomCarColorTypes.Length : 0);
						int newSize = EditorGUILayout.IntField("随机种类数量", curSize);
						newSize = Mathf.Max(0, newSize);
						if (newSize != curSize)
						{
							_current.RandomCarCounts = ResizeArray(_current.RandomCarCounts, newSize);
							_current.RandomCarColorTypes = ResizeArray(_current.RandomCarColorTypes, newSize);
						}

						if (_current.RandomCarCounts == null) _current.RandomCarCounts = new int[newSize];
						if (_current.RandomCarColorTypes == null) _current.RandomCarColorTypes = new string[newSize];

						for (int i = 0; i < newSize; i++)
						{
							using (new EditorGUILayout.HorizontalScope())
							{
								using (new GUILayout.VerticalScope(GUILayout.Width(220)))
								{
									// 颜色枚举 -> 字符串
									CarColorType colorEnum = CarColorType.White;
									Enum.TryParse<CarColorType>(_current.RandomCarColorTypes[i] ?? CarColorType.White.ToString(), true, out colorEnum);
									var newEnum = (CarColorType)EditorGUILayout.EnumPopup($"ColorType[{i}]", colorEnum);
									if (newEnum != colorEnum) _current.RandomCarColorTypes[i] = newEnum.ToString();
								}
								using (new GUILayout.VerticalScope(GUILayout.Width(220)))
								{
									_current.RandomCarCounts[i] = EditorGUILayout.IntField($"Count[{i}]", _current.RandomCarCounts[i]);
								}
								GUILayout.FlexibleSpace();
							}
						}
					}
					EditorGUI.EndDisabledGroup();
				}

				// 奖励配置
				EditorGUILayout.Space(4);
				EditorGUILayout.LabelField("奖励配置", EditorStyles.miniBoldLabel);
				using (new EditorGUILayout.HorizontalScope())
				{
					using (new GUILayout.VerticalScope(GUILayout.Width(220)))
					{
						_current.AwardCoin = EditorGUILayout.IntField("AwardCoin", _current.AwardCoin);
					}
					
					GUILayout.FlexibleSpace();
				}
				using (new EditorGUILayout.HorizontalScope())
				{
                    using (new GUILayout.VerticalScope(GUILayout.Width(220)))
                    {
                        _current.AwardItem1 = EditorGUILayout.IntField("AwardItem1", _current.AwardItem1);
                    }
                    using (new GUILayout.VerticalScope(GUILayout.Width(220)))
					{
						_current.AwardItem2 = EditorGUILayout.IntField("AwardItem2", _current.AwardItem2);
					}
					GUILayout.FlexibleSpace();
				}
				using (new EditorGUILayout.HorizontalScope())
                {
                    using (new GUILayout.VerticalScope(GUILayout.Width(220)))
                    {
                        _current.AwardItem3 = EditorGUILayout.IntField("AwardItem3", _current.AwardItem3);
                    }
                    using (new GUILayout.VerticalScope(GUILayout.Width(220)))
					{
						_current.AwardItem4 = EditorGUILayout.IntField("AwardItem4", _current.AwardItem4);
					}
					GUILayout.FlexibleSpace();
				}

				EditorGUILayout.Space(4);
				using (new EditorGUILayout.HorizontalScope())
				{
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("保存关卡到 levels.json", GUILayout.Width(200), GUILayout.Height(24)))
					{
						SaveJson();
					}
				}

				// 恢复标签宽度
				EditorGUIUtility.labelWidth = prevLabel;
			}
		}

		// 简单数组扩容帮助
		string[] ResizeArray(string[] src, int newSize)
		{
			if (newSize < 0) newSize = 0;
			var dst = new string[newSize];
			if (src != null) Array.Copy(src, dst, Math.Min(src.Length, dst.Length));
			return dst;
		}

		int[] ResizeArray(int[] src, int newSize)
		{
			if (newSize < 0) newSize = 0;
			var dst = new int[newSize];
			if (src != null) Array.Copy(src, dst, Math.Min(src.Length, dst.Length));
			return dst;
		}

		void DrawBottomSplit()
		{
			float viewW = Mathf.Max(300f, position.width - 16f);
			float leftW = Mathf.Max(220, viewW * 0.25f);
			float rightW = Mathf.Max(260, viewW * 0.30f);
			float middleW = Mathf.Max(200f, viewW - leftW - rightW - 8f);

			using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
			{
				using (new GUILayout.VerticalScope(GUILayout.Width(leftW), GUILayout.ExpandHeight(true)))
				{
					DrawLeftTree();
				}

				using (new GUILayout.VerticalScope(GUILayout.Width(middleW), GUILayout.ExpandHeight(true)))
				{
					DrawCenterPreview(middleW);
				}

				using (new GUILayout.VerticalScope(GUILayout.Width(rightW), GUILayout.ExpandHeight(true)))
				{
					DrawRightInspector();
				}
			}
		}

		void DrawLeftTree()
		{
			using (var scroll = new EditorGUILayout.ScrollViewScope(_leftScroll))
			{
				_leftScroll = scroll.scrollPosition;

				foreach (var g in _groupsOrder)
				{
					var rowRect = EditorGUILayout.BeginHorizontal();
					_groupFoldout[g] = EditorGUILayout.Foldout(_groupFoldout[g], g, true);
					EditorGUILayout.EndHorizontal();

					// 根节点右键菜单
					if (Event.current.type == EventType.ContextClick && rowRect.Contains(Event.current.mousePosition))
					{
						var menu = new GenericMenu();
						menu.AddItem(new GUIContent("添加子节点"), false, () =>
						{
							AddEntityToGroup(g);
							Repaint();
						});
						menu.AddItem(new GUIContent("删除选中子节点"), false, () =>
						{
							DeleteSelectedInGroup(g);
							Repaint();
						});
						menu.AddItem(new GUIContent("清空该组"), false, () =>
						{
							_groups[g].Clear();
							if (_selectedGroup == g) ClearSelection();
							Repaint();
						});
						menu.ShowAsContext();
						Event.current.Use();
					}

					if (_groupFoldout[g])
					{
						EditorGUI.indentLevel++;
						var list = _groups[g];
						for (int i = 0; i < list.Count; i++)
						{
							bool selected = (_selectedGroup == g && _selectedIndex == i);
							var style = selected ? EditorStyles.helpBox : EditorStyles.label;
							using (new EditorGUILayout.HorizontalScope(style))
							{
								if (GUILayout.Button($"{DisplayLabel(list[i])}", EditorStyles.label))
								{
									_selectedGroup = g;
									_selectedIndex = i;
									Repaint();
								}
								if (GUILayout.Button("X", GUILayout.Width(22)))
								{
									list.RemoveAt(i);
									if (_selectedGroup == g)
									{
										if (_selectedIndex == i) ClearSelection();
										else if (_selectedIndex > i) _selectedIndex--;
									}
									Repaint();
									break;
								}
							}
						}
						EditorGUI.indentLevel--;
					}
				}
			}
		}

		string DisplayLabel(GridEntityData d)
		{
			return $"{d.Type}  ({d.CellX},{d.CellY})"
			       + (!string.IsNullOrEmpty(d.ColorType) ? $" [{d.ColorType}]" : "")
			       + (d.HasKey ? " Key" : "");
		}

		void AddEntityToGroup(string group)
		{
			var list = _groups[group];
			GridEntityData newItem = null;
			if (list != null && list.Count > 0)
			{
				var last = list[list.Count - 1];
				newItem = CloneEntity(last);
			}
			else
			{
				var type = GroupToDefaultType(group);
				newItem = new GridEntityData
				{
					Type = type,
					CellX = 0,
					CellY = 0,
					ColorType = type.Equals("Car", StringComparison.OrdinalIgnoreCase) ? CarColorType.White.ToString() : null,
					HasKey = false,
					KayColorType = CarColorType.White.ToString(),
					Dir = DirectionsType.Down.ToString(),
					IncludeCarCount = 0
				};
			}
			list.Add(newItem);
			_selectedGroup = group;
			_selectedIndex = list.Count - 1;
		}

		void DeleteSelectedInGroup(string group)
		{
			if (_selectedGroup == group && _selectedIndex >= 0 && _selectedIndex < _groups[group].Count)
			{
				_groups[group].RemoveAt(_selectedIndex);
				ClearSelection();
			}
		}

		string GroupToDefaultType(string group)
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
                case "Entities": return "Wall"; // 默认 Entities 放个墙
				default: return "Empty";
			}
		}

		void DrawCenterPreview(float availableWidth)
		{
			using (new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("场景展示", EditorStyles.boldLabel);

				if (_current?.Grid == null)
				{
					EditorGUILayout.HelpBox("缺少 Grid 配置。", MessageType.Warning);
					return;
				}

				// 基础参数
				int gw = Mathf.Max(1, _current.Grid.Width);
				int gh = Mathf.Max(1, _current.Grid.Height);

				// 在可用空间中自适应填充
				var drawRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
				int cell = Mathf.Clamp(_cellSize, 12, 64);
				int maxCellByW = Mathf.Max(4, Mathf.FloorToInt((drawRect.width - _gridPadding * 2) / gw));
				int maxCellByH = Mathf.Max(4, Mathf.FloorToInt((drawRect.height - _gridPadding * 2) / gh));
				cell = Mathf.Min(cell, maxCellByW, maxCellByH);
				float totalW = gw * cell + _gridPadding * 2;
				float totalH = gh * cell + _gridPadding * 2;

				// 居中绘制框架
				float frameW = Mathf.Min(totalW, drawRect.width);
				float frameH = Mathf.Min(totalH, drawRect.height);
				var frame = new Rect(
					drawRect.x + (drawRect.width - frameW) * 0.5f,
					drawRect.y + (drawRect.height - frameH) * 0.5f,
					frameW,
					frameH);

				// 背景
				EditorGUI.DrawRect(frame, new Color(0.1f, 0.1f, 0.1f, 0.8f));

				// 内容区域
				var content = new Rect(frame.x + _gridPadding, frame.y + _gridPadding, gw * cell, gh * cell);
				EditorGUI.DrawRect(content, new Color(0.18f, 0.18f, 0.18f, 1f));

				// 画网格
				Handles.BeginGUI();
				Color prev = Handles.color;
				Handles.color = new Color(0.35f, 0.35f, 0.35f, 1f);
				for (int x = 0; x <= gw; x++)
				{
					float xx = content.x + x * cell;
					Handles.DrawLine(new Vector2(xx, content.y), new Vector2(xx, content.yMax));
				}
				for (int y = 0; y <= gh; y++)
				{
					float yy = content.y + y * cell;
					Handles.DrawLine(new Vector2(content.x, yy), new Vector2(content.xMax, yy));
				}
				Handles.color = prev;
				Handles.EndGUI();

				// 画实体（各组）
				void DrawList(IEnumerable<GridEntityData> arr)
				{
					foreach (var e in arr)
					{
						if (e == null) continue;
						if (e.CellX < 0 || e.CellX >= gw || e.CellY < 0 || e.CellY >= gh) continue;
						var r = new Rect(
							content.x + e.CellX * cell + 1,
							content.y + (gh - 1 - e.CellY) * cell + 1, // 使(0,0)在左下
							cell - 2, cell - 2);

						// 先尝试用资源 Sprite 绘制
						var sprite = GetSpriteForEntity(e);
						if (sprite != null && sprite.texture != null)
						{
							var tex = sprite.texture;
							var uv = new Rect(
								sprite.rect.x / tex.width,
								sprite.rect.y / tex.height,
								sprite.rect.width / tex.width,
								sprite.rect.height / tex.height);
							GUI.DrawTextureWithTexCoords(r, tex, uv, true);
						}
						else
						{
							// 缺省用颜色块
							Color col;
							if (!_typeColors.TryGetValue(e.Type ?? "Empty", out col)) col = new Color(0.6f, 0.6f, 0.6f, 1f);
							EditorGUI.DrawRect(r, col);
						}

						// 选中高亮
						if (_selectedGroup != null && _selectedIndex >= 0)
						{
							var sel = _groups[_selectedGroup];
							if (_selectedIndex < sel.Count && ReferenceEquals(e, sel[_selectedIndex]))
							{
								DrawRectBorder(r, Color.yellow, 2f);
							}
						}
					}
				}

				DrawList(_groups["Emptys"]);
				DrawList(_groups["Entities"]);
				DrawList(_groups["Parks"]);
				DrawList(_groups["PayParks"]);
				DrawList(_groups["Factorys"]);
                DrawList(_groups["Cars"]);
                DrawList(_groups["Boxs"]);
                DrawList(_groups["LockDoors"]);
            }
		}

		void DrawRectBorder(Rect r, Color c, float t)
		{
			EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), c);
			EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), c);
			EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), c);
			EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), c);
		}

		Color ColorByCarColorType(string colorType, Color fallback)
		{
			if (!Enum.TryParse<CarColorType>(colorType, true, out var ct)) return fallback;
			switch (ct)
			{
				case CarColorType.Red: return new Color(0.9f, 0.25f, 0.25f, 1f);
				case CarColorType.Blue: return new Color(0.2f, 0.5f, 0.95f, 1f);
				case CarColorType.Green: return new Color(0.25f, 0.85f, 0.3f, 1f);
				case CarColorType.Yellow: return new Color(0.95f, 0.85f, 0.2f, 1f);
				case CarColorType.Purple: return new Color(0.65f, 0.35f, 0.9f, 1f);
				default: return fallback;
			}
		}

		Sprite GetSpriteForEntity(GridEntityData e)
		{
			if (e == null || string.IsNullOrEmpty(e.Type)) return null;
			// 按类型映射到静态资源
			switch (e.Type)
			{
				case "Wall": return StaticResources.WallSprite;
				case "Park": return StaticResources.ParkSprite;
				case "PayPark": return StaticResources.PayParkSprite; // 可能为空
				case "Hole": return StaticResources.HoleSprite; // 可能为空
				case "Item": return StaticResources.ItemSprite; // 可能为空
				case "Box": return StaticResources.BoxSprite;
                case "LockDoor":
                    // CarSprites 与 CarColorType 顺序相同
                    if (!Enum.TryParse<CarColorType>(e.ColorType ?? CarColorType.White.ToString(), true, out var ct2))
                        ct2 = CarColorType.White;
                    int colorIndex2 = (int)ct2;
                    if (colorIndex2 >= 0 && colorIndex2 < StaticResources.LockDoorHeadSprites.Count)
                        return StaticResources.LockDoorHeadSprites[colorIndex2];
                    return null;
                case "Factory":
					// 用方向挑选工厂贴图
					var fList = StaticResources.FactorySprites;
					if (fList != null && fList.Count > 0)
					{
						if (!Enum.TryParse<DirectionsType>(e.Dir ?? DirectionsType.Down.ToString(), true, out var dir))
							dir = DirectionsType.Down;
						// 约定 Down/Up/Right/Left 对应 0/1/2/3
						int idx = dir == DirectionsType.Down ? 0 : dir == DirectionsType.Up ? 1 : dir == DirectionsType.Right ? 2 : 3;
						idx = Mathf.Clamp(idx, 0, fList.Count - 1);
						return fList[idx];
					}
					return null;
				case "Car":
					// CarSprites 与 CarColorType 顺序相同
					if (!Enum.TryParse<CarColorType>(e.ColorType ?? CarColorType.White.ToString(), true, out var ct))
						ct = CarColorType.White;
					int colorIndex = (int)ct;
					if (colorIndex >= 0 && colorIndex < StaticResources.CarSprites.Count)
						return StaticResources.CarSprites[colorIndex];
					return null;
				case "Empty":
					return StaticResources.GridCellSprite; // 若为空则回退为颜色块
				default:
					return null;
			}
		}

		void DrawRightInspector()
		{
			using (new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("属性编辑", EditorStyles.boldLabel);

				if (string.IsNullOrEmpty(_selectedGroup) || _selectedIndex < 0 || !_groups.ContainsKey(_selectedGroup) || _selectedIndex >= _groups[_selectedGroup].Count)
				{
					EditorGUILayout.HelpBox("在左侧选择一个实体以编辑属性。", MessageType.Info);
					return;
				}

				var e = _groups[_selectedGroup][_selectedIndex];

				// 统一控件宽度，避免出现滚动条
				float prevLabel = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 110f;

				// 类型（展示/可改）
				EditorGUILayout.LabelField("类型与坐标", EditorStyles.miniBoldLabel);
				string newType = EditorGUILayout.TextField("Type", e.Type, GUILayout.ExpandWidth(true));
				if (!string.Equals(newType, e.Type, StringComparison.Ordinal))
				{
					e.Type = newType;
				}

				// 将坐标分两行，减少横向拥挤
				e.CellX = EditorGUILayout.IntField("CellX", e.CellX, GUILayout.ExpandWidth(true));
				e.CellY = EditorGUILayout.IntField("CellY", e.CellY, GUILayout.ExpandWidth(true));

				EditorGUILayout.Space(4);
				EditorGUILayout.LabelField("方向与颜色", EditorStyles.miniBoldLabel);

				// Dir
				if (Enum.TryParse<DirectionsType>(e.Dir ?? DirectionsType.Down.ToString(), true, out var dirEnum))
				{
					var dirNew = (DirectionsType)EditorGUILayout.EnumPopup("Dir", dirEnum, GUILayout.ExpandWidth(true));
					if (dirNew != dirEnum) e.Dir = dirNew.ToString();
				}
				else
				{
					e.Dir = DirectionsType.Down.ToString();
				}

				// ColorType / KayColorType
				CarColorType colorEnum = CarColorType.White;
				Enum.TryParse<CarColorType>(e.ColorType ?? CarColorType.White.ToString(), true, out colorEnum);
				var colorNew = (CarColorType)EditorGUILayout.EnumPopup("ColorType", colorEnum, GUILayout.ExpandWidth(true));
				if (colorNew != colorEnum) e.ColorType = colorNew.ToString();

				CarColorType keyColorEnum = CarColorType.White;
				Enum.TryParse<CarColorType>(e.KayColorType ?? CarColorType.White.ToString(), true, out keyColorEnum);
				var keyColorNew = (CarColorType)EditorGUILayout.EnumPopup("KayColorType", keyColorEnum, GUILayout.ExpandWidth(true));
				if (keyColorNew != keyColorEnum) e.KayColorType = keyColorNew.ToString();

				EditorGUILayout.Space(4);
				EditorGUILayout.LabelField("特殊属性", EditorStyles.miniBoldLabel);
				e.HasKey = EditorGUILayout.Toggle("HasKey", e.HasKey, GUILayout.ExpandWidth(true));
				e.IncludeCarCount = EditorGUILayout.IntField("IncludeCarCount", e.IncludeCarCount, GUILayout.ExpandWidth(true));

				EditorGUILayout.Space(6);
				if (GUILayout.Button("删除该实体", GUILayout.Height(24), GUILayout.ExpandWidth(true)))
				{
					_groups[_selectedGroup].RemoveAt(_selectedIndex);
					ClearSelection();
				}

				// 与删除按钮之间增加间距
				EditorGUILayout.Space(4);

				// 方向复制按钮：上/下/左/右
				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button("向上复制", GUILayout.Height(24), GUILayout.ExpandWidth(true)))
					{
						var list = _groups[_selectedGroup];
						var cloned = CloneEntity(e);
						cloned.CellY += 1;
						list.Add(cloned);
						_selectedIndex = list.Count - 1;
					}
					if (GUILayout.Button("向下复制", GUILayout.Height(24), GUILayout.ExpandWidth(true)))
					{
						var list = _groups[_selectedGroup];
						var cloned = CloneEntity(e);
						cloned.CellY -= 1;
						list.Add(cloned);
						_selectedIndex = list.Count - 1;
					}
					if (GUILayout.Button("向左复制", GUILayout.Height(24), GUILayout.ExpandWidth(true)))
					{
						var list = _groups[_selectedGroup];
						var cloned = CloneEntity(e);
						cloned.CellX -= 1;
						list.Add(cloned);
						_selectedIndex = list.Count - 1;
					}
					if (GUILayout.Button("向右复制", GUILayout.Height(24), GUILayout.ExpandWidth(true)))
					{
						var list = _groups[_selectedGroup];
						var cloned = CloneEntity(e);
						cloned.CellX += 1;
						list.Add(cloned);
						_selectedIndex = list.Count - 1;
					}
				}

				// 还原 label 宽度
				EditorGUIUtility.labelWidth = prevLabel;
			}
		}
	}
}
#endif