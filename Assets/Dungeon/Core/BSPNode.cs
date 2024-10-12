using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class BSPNode
{
    public int x;
    public int y;
    public int width;
    public int height;
    public BSPNode left_child;
    public BSPNode right_child;
    public Room room = new Room(); // 部屋の情報
    public List<Corridor> corridor = new List<Corridor>(); // 廊下のリスト
    public (string direction, int x_or_y) split_line = ("Vertical", -1);
    public string areaType; // エリアの種類

    public BSPNode(int x, int y, int width, int height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
        this.left_child = null;
        this.right_child = null;
        this.room = null;
        this.corridor = new List<Corridor>();
        this.split_line = ("Vertical", -1);
    }
}