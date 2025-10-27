using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LevelsJsonEditor
{
    // 枚举定义
    public enum LevelHardType
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Expert = 3
    }

    public enum CarColorType
    {
        White = 0,
        Red = 1,
        Blue = 2,
        Green = 3,
        Yellow = 4,
        Purple = 5
    }

    public enum DirectionsType
    {
        Down = 0,
        Up = 1,
        Right = 2,
        Left = 3
    }

    // 网格数据
    [Serializable]
    public class GridData
    {
        public int Width { get; set; } = 7;
        public int Height { get; set; } = 11;
        public float CellSize { get; set; } = 64f;
    }

    // 网格实体数据
    [Serializable]
    public class GridEntityData
    {
        public string Type { get; set; } = "";
        public int CellX { get; set; } = 0;
        public int CellY { get; set; } = 0;
        public string ColorType { get; set; } = "";
        public bool HasKey { get; set; } = false;
        public string KayColorType { get; set; } = "White";
        public string Dir { get; set; } = "Down";
        public int IncludeCarCount { get; set; } = 0;
    }

    // 关卡数据
    [Serializable]
    public class LevelData
    {
        public int LV { get; set; } = 1;
        public int HardType { get; set; } = 1;
        public bool RandomCar { get; set; } = false;
        public float GameTimeLimit { get; set; } = 0;
        public bool EnableTimeLimit { get; set; } = false;
        public GridData Grid { get; set; } = new GridData();
        public GridEntityData[] Parks { get; set; } = new GridEntityData[0];
        public GridEntityData[] PayParks { get; set; } = new GridEntityData[0];
        public GridEntityData[] Cars { get; set; } = new GridEntityData[0];
        public GridEntityData[] Entities { get; set; } = new GridEntityData[0];
        public GridEntityData[] Emptys { get; set; } = new GridEntityData[0];
        public GridEntityData[] Factorys { get; set; } = new GridEntityData[0];
        public GridEntityData[] Boxs { get; set; } = new GridEntityData[0];
        public GridEntityData[] LockDoors { get; set; } = new GridEntityData[0];
        public string[] RandomCarColorTypes { get; set; } = new string[0];
        public int[] RandomCarCounts { get; set; } = new int[0];
        public int AwardCoin { get; set; } = 0;
        public int AwardItem1 { get; set; } = 0;
        public int AwardItem2 { get; set; } = 0;
        public int AwardItem3 { get; set; } = 0;
        public int AwardItem4 { get; set; } = 0;
    }

    // 关卡数据容器（用于JSON序列化）
    [Serializable]
    public class LevelDataContainer
    {
        public List<LevelData> Levels { get; set; } = new List<LevelData>();
    }

    // 编辑器内部容器
    [Serializable]
    public class Container
    {
        public List<LevelData> Levels { get; set; } = new List<LevelData>();
    }
}
