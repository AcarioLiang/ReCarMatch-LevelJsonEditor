using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LevelsJsonEditor
{
    public static class ImageResources
    {
        // 图像缓存
        private static readonly Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();

        // 各类型图像资源
        public static Image WallImage { get; private set; }
        public static Image ParkImage { get; private set; }
        public static Image BoxImage { get; private set; }
        public static Image SubLevelImage { get; private set; }
        public static List<Image> CarImages { get; private set; } = new List<Image>();
        public static List<Image> FactoryImages { get; private set; } = new List<Image>();
        public static List<Image> LockDoorHeadImages { get; private set; } = new List<Image>();
        public static List<Image> LockDoorBodyImages { get; private set; } = new List<Image>();
        public static List<Image> LockDoorKeyImages { get; private set; } = new List<Image>();

        private static string _resourcesPath;
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                // 获取Resources目录路径
                _resourcesPath = Path.Combine(Application.StartupPath, "..", "..", "Resources");
                if (!Directory.Exists(_resourcesPath))
                {
                    _resourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
                }

                if (!Directory.Exists(_resourcesPath))
                {
                    Console.WriteLine("警告: 找不到Resources目录，将尝试使用嵌入资源");
                    _resourcesPath = ""; // 设置为空，这样LoadImage会直接尝试加载嵌入资源
                }

                LoadAllImages();
                _initialized = true;
                Console.WriteLine("图像资源初始化完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"图像资源初始化失败: {ex.Message}");
            }
        }

        private static void LoadAllImages()
        {
            // 加载Wall图像
            WallImage = LoadImage("Wall", "Game_Grid_wall.png");

            // 加载Park图像  
            ParkImage = LoadImage("Park", "Common_popPanel_bg.png");

            // 加载Box图像
            BoxImage = LoadImage("Box", "Game_Grid_box.png");

            // 加载SubLevel图像
            SubLevelImage = LoadImage("SubLevel", "Game_Grid_subLevel.png");

            // 加载Car图像 (按颜色顺序: White=0, Red=1, Blue=2, Green=3, Yellow=4, Purple=5)
            CarImages.Clear();
            for (int i = 0; i < 8; i++) // 加载更多车辆图像以防万一
            {
                var carImage = LoadImage("Car", $"Game_SnakeHead{i}.png");
                CarImages.Add(carImage);
            }

            // 加载Factory图像 (按方向顺序: Down=0, Up=1, Right=2, Left=3)
            FactoryImages.Clear();
            FactoryImages.Add(LoadImage("Factory", "Game_Factory_Down.png"));   // Down = 0
            FactoryImages.Add(LoadImage("Factory", "Game_Factory_Up.png"));     // Up = 1
            FactoryImages.Add(LoadImage("Factory", "Game_Factory_Right.png"));  // Right = 2
            FactoryImages.Add(LoadImage("Factory", "Game_Factory_Left.png"));   // Left = 3

            // 加载LockDoor图像
            LockDoorHeadImages.Clear();
            LockDoorBodyImages.Clear();
            LockDoorKeyImages.Clear();

            for (int i = 0; i < 8; i++)
            {
                LockDoorHeadImages.Add(LoadImage("LockDoor", $"Game_LockDoor_Head_{i}.png"));
                LockDoorBodyImages.Add(LoadImage("LockDoor", $"Game_LockDoor_Body_{i}.png"));
                LockDoorKeyImages.Add(LoadImage("LockDoor", $"Game_LockDoor_Key_{i}.png"));
            }
        }

        private static Image LoadImage(string category, string fileName)
        {
            string key = $"{category}/{fileName}";

            if (_imageCache.ContainsKey(key))
                return _imageCache[key];

            try
            {

                // 先尝试从文件系统加载（如果有Resources目录）
                if (!string.IsNullOrEmpty(_resourcesPath))
                {
                    string fullPath = Path.Combine(_resourcesPath, category, fileName);
                    if (File.Exists(fullPath))
                    {
                        // 使用副本避免文件锁定
                        using (var original = Image.FromFile(fullPath))
                        {
                            var copy = new Bitmap(original);
                            _imageCache[key] = copy;
                            Console.WriteLine($"从文件加载图像: {key}");
                            return copy;
                        }
                    }
                }

                // 文件不存在或没有Resources目录时，尝试加载Properties.Resources资源
                var resourceImage = LoadImageFromResources(category, fileName);
                if (resourceImage != null)
                {
                    // 创建副本避免资源锁定
                    var copy = new Bitmap(resourceImage);
                    _imageCache[key] = copy;
                    Console.WriteLine($"从嵌入资源加载图像: {key}");
                    return copy;
                }
                else
                {
                    Console.WriteLine($"图像资源未找到: {key}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载图像失败 {key}: {ex.Message}");
            }

            return null;
        }

        private static Image LoadImageFromResources(string category, string fileName)
        {
            try
            {
                // 将文件名转换为资源名（去掉扩展名，将.和-替换为_）
                string resourceName = Path.GetFileNameWithoutExtension(fileName).Replace(".", "_").Replace("-", "_");
                
                // 根据类别和文件名映射到Properties.Resources中的具体资源
                switch (category.ToLower())
                {
                    case "wall":
                        if (resourceName == "Game_Grid_wall")
                            return Properties.Resources.Game_Grid_wall;
                        break;

                    case "park":
                        if (resourceName == "Common_popPanel_bg")
                            return Properties.Resources.Common_popPanel_bg;
                        break;

                    case "box":
                        if (resourceName == "Game_Grid_box")
                            return Properties.Resources.Game_Grid_box;
                        break;

                    case "sublevel":
                        if (resourceName == "Game_Grid_subLevel")
                            return Properties.Resources.Game_Grid_subLevel;
                        break;

                    case "factory":
                        switch (resourceName)
                        {
                            case "Game_Factory_Down":
                                return Properties.Resources.Game_Factory_Down;
                            case "Game_Factory_Up":
                                return Properties.Resources.Game_Factory_Up;
                            case "Game_Factory_Left":
                                return Properties.Resources.Game_Factory_Left;
                            case "Game_Factory_Right":
                                return Properties.Resources.Game_Factory_Right;
                        }
                        break;

                    case "car":
                        switch (resourceName)
                        {
                            case "Game_SnakeHead0":
                                return Properties.Resources.Game_SnakeHead0;
                            case "Game_SnakeHead1":
                                return Properties.Resources.Game_SnakeHead1;
                            case "Game_SnakeHead2":
                                return Properties.Resources.Game_SnakeHead2;
                            case "Game_SnakeHead3":
                                return Properties.Resources.Game_SnakeHead3;
                            case "Game_SnakeHead4":
                                return Properties.Resources.Game_SnakeHead4;
                            case "Game_SnakeHead5":
                                return Properties.Resources.Game_SnakeHead5;
                            case "Game_SnakeHead6":
                                return Properties.Resources.Game_SnakeHead6;
                            case "Game_SnakeHead7":
                                return Properties.Resources.Game_SnakeHead7;
                        }
                        break;

                    case "lockdoor":
                        // LockDoor Head资源
                        switch (resourceName)
                        {
                            case "Game_LockDoor_Head_0":
                                return Properties.Resources.Game_LockDoor_Head_0;
                            case "Game_LockDoor_Head_1":
                                return Properties.Resources.Game_LockDoor_Head_1;
                            case "Game_LockDoor_Head_2":
                                return Properties.Resources.Game_LockDoor_Head_2;
                            case "Game_LockDoor_Head_3":
                                return Properties.Resources.Game_LockDoor_Head_3;
                            case "Game_LockDoor_Head_4":
                                return Properties.Resources.Game_LockDoor_Head_4;
                            case "Game_LockDoor_Head_5":
                                return Properties.Resources.Game_LockDoor_Head_5;
                            case "Game_LockDoor_Head_6":
                                return Properties.Resources.Game_LockDoor_Head_6;
                            case "Game_LockDoor_Head_7":
                                return Properties.Resources.Game_LockDoor_Head_7;
                            // LockDoor Body资源
                            case "Game_LockDoor_Body_0":
                                return Properties.Resources.Game_LockDoor_Body_0;
                            case "Game_LockDoor_Body_1":
                                return Properties.Resources.Game_LockDoor_Body_1;
                            case "Game_LockDoor_Body_2":
                                return Properties.Resources.Game_LockDoor_Body_2;
                            case "Game_LockDoor_Body_3":
                                return Properties.Resources.Game_LockDoor_Body_3;
                            case "Game_LockDoor_Body_4":
                                return Properties.Resources.Game_LockDoor_Body_4;
                            case "Game_LockDoor_Body_5":
                                return Properties.Resources.Game_LockDoor_Body_5;
                            case "Game_LockDoor_Body_6":
                                return Properties.Resources.Game_LockDoor_Body_6;
                            case "Game_LockDoor_Body_7":
                                return Properties.Resources.Game_LockDoor_Body_7;
                            // LockDoor Key资源
                            case "Game_LockDoor_Key_0":
                                return Properties.Resources.Game_LockDoor_Key_0;
                            case "Game_LockDoor_Key_1":
                                return Properties.Resources.Game_LockDoor_Key_1;
                            case "Game_LockDoor_Key_2":
                                return Properties.Resources.Game_LockDoor_Key_2;
                            case "Game_LockDoor_Key_3":
                                return Properties.Resources.Game_LockDoor_Key_3;
                            case "Game_LockDoor_Key_4":
                                return Properties.Resources.Game_LockDoor_Key_4;
                            case "Game_LockDoor_Key_5":
                                return Properties.Resources.Game_LockDoor_Key_5;
                            case "Game_LockDoor_Key_6":
                                return Properties.Resources.Game_LockDoor_Key_6;
                            case "Game_LockDoor_Key_7":
                                return Properties.Resources.Game_LockDoor_Key_7;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"从嵌入资源加载失败 {category}/{fileName}: {ex.Message}");
            }

            return null;
        }

        public static void Cleanup()
        {
            foreach (var image in _imageCache.Values)
            {
                image?.Dispose();
            }
            _imageCache.Clear();
            _initialized = false;
        }
    }
}