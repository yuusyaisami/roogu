using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Assets/Scripts/Dungeon/TileData.cs
public class TileData
{
    public TileType tileType;     // タイルの種類（壁、床、入口、出口）
    public AreaType areaType;       // 部屋の種類（"Start", "Boss" など。部屋でない場合は null または空文字）
    public bool isCorridor;       // 廊下であるかどうか

    public TileData()
    {
        tileType = TileType.Wall;
        areaType = AreaType.UnKnown;
        isCorridor = false;
    }
}
