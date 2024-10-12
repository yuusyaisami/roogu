// Room.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room
{
    public int id; // ユニークな識別子
    public int x;
    public int y;
    public int width;
    public int height;
    public bool isFixedRoom;
    public FixedRoom fixedRoom; // 固定部屋の場合の参照

    // 固定部屋の入口と出口の絶対位置
    public Vector2Int entrancePoint;
    public Vector2Int exitPoint;

    // デフォルトコンストラクター
    public Room()
    {
        id = -1;
        x = y = width = height = 0;
        isFixedRoom = false;
        fixedRoom = null;
        entrancePoint = Vector2Int.zero;
        exitPoint = Vector2Int.zero;
    }

    // コピーコンストラクター
    public Room(Room other)
    {
        this.id = other.id;
        this.x = other.x;
        this.y = other.y;
        this.width = other.width;
        this.height = other.height;
        this.isFixedRoom = other.isFixedRoom;
        this.fixedRoom = other.fixedRoom;
        this.entrancePoint = other.entrancePoint;
        this.exitPoint = other.exitPoint;
    }
}
