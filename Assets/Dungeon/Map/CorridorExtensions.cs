using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 廊下のすべてのポイントを取得する拡張メソッド。
/// </summary>
public static class CorridorExtensions
{
    public static List<(int x, int y)> GetPoints(this Corridor corridor)
    {
        return new List<(int x, int y)> { corridor.Start, corridor.Mid, corridor.End };
    }
}
