using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 廊下クラス
public class Corridor
{
    public (int x, int y) Start { get; set; }
    public (int x, int y) Mid { get; set; }
    public (int x, int y) End { get; set; }

    public Corridor((int x, int y) start, (int x, int y) mid, (int x, int y) end)
    {
        Start = start;
        Mid = mid;
        End = end;
    }
    public List<(int x, int y)> GetPoints()
    {
        List<(int x, int y)> points = new List<(int x, int y)>();
        points.Add(Start);
        points.AddRange(GetLinePoints(Start, Mid));
        points.Add(Mid);
        points.AddRange(GetLinePoints(Mid, End));
        points.Add(End);
        return points;
    }
    
    /// <summary>
    /// 2点間の直線上のポイントを取得するメソッド（Bresenham's Line Algorithm）
    /// </summary>
    private List<(int x, int y)> GetLinePoints((int x, int y) start, (int x, int y) end)
    {
        List<(int x, int y)> linePoints = new List<(int x, int y)>();
    
        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;
    
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
    
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
    
        int err = dx - dy;
    
        while (x0 != x1 || y0 != y1)
        {
            linePoints.Add((x0, y0));
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    
        return linePoints;
    }

}