using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ダンジョン生成に関連するユーティリティ関数を提供するクラス
/// </summary>
public class DungeonUtils
{
    private Dungeon dungeon;

    /// <summary>
    /// DungeonUtilsのコンストラクタ
    /// </summary>
    /// <param name="dungeon">対象となるDungeonインスタンス</param>
    public DungeonUtils(Dungeon dungeon)
    {
        this.dungeon = dungeon;
    }

    /// <summary>
    /// 指定した座標点に存在する部屋のIDを取得する
    /// </summary>
    /// <param name="x">座標のX値</param>
    /// <param name="y">座標のY値</param>
    /// <returns>存在する場合は部屋のID、存在しない場合は-1</returns>
    public int GetRoomIDAtCoordinate(int x, int y)
    {
        for(int i = 0; i < dungeon.rooms.Count; i++)
        {
            var room = dungeon.rooms[i];
            if(x >= room.x && x < room.x + room.width && y >= room.y && y < room.y + room.height)
            {
                return i;
            }
        }
        return -1; // 部屋が存在しない場合
    }

    /// <summary>
    /// 指定した座標がどの部屋にも属していないかを確認する
    /// </summary>
    /// <param name="x">座標のX値</param>
    /// <param name="y">座標のY値</param>
    /// <returns>属していない場合はtrue、それ以外はfalse</returns>
    public bool IsCoordinateInAnyRoom(int x, int y)
    {
        return GetRoomIDAtCoordinate(x, y) != -1;
    }

    /// <summary>
    /// 指定した部屋IDに隣接する部屋のIDリストを取得する
    /// </summary>
    /// <param name="roomID">対象となる部屋のID</param>
    /// <returns>隣接する部屋のIDリスト</returns>
    public List<int> GetAdjacentRooms(int roomID)
    {
        if(roomID < 0 || roomID >= dungeon.room_graph.Count)
        {
            Debug.LogWarning($"無効な部屋ID: {roomID}");
            return new List<int>();
        }
        return dungeon.room_graph[roomID];
    }

    /// <summary>
    /// 指定した部屋IDからの経路を取得する
    /// </summary>
    /// <param name="roomID">出発点となる部屋のID</param>
    /// <returns>経路の部屋IDリスト</returns>
    public List<int> GetPathFromRoom(int roomID)
    {
        // 例として、特定の部屋から全ての部屋への経路を取得する関数を作成できます。
        // 具体的な要件に応じて実装してください。
        // ここでは、単純にその部屋の隣接部屋を返す例を示します。
        return GetAdjacentRooms(roomID);
    }

    /// <summary>
    /// 指定した部屋の中心座標を取得する
    /// </summary>
    /// <param name="roomID">対象となる部屋のID</param>
    /// <returns>中心座標 (x, y)</returns>
    public (int x, int y) GetRoomCenter(int roomID)
    {
        if(roomID < 0 || roomID >= dungeon.rooms.Count)
        {
            Debug.LogWarning($"無効な部屋ID: {roomID}");
            return (-1, -1);
        }
        var room = dungeon.rooms[roomID];
        return (room.x + room.width / 2, room.y + room.height / 2);
    }

    /// <summary>
    /// 指定した範囲内に存在する部屋のIDリストを取得する
    /// </summary>
    /// <param name="x">範囲の中心点X値</param>
    /// <param name="y">範囲の中心点Y値</param>
    /// <param name="range">範囲の半径</param>
    /// <returns>範囲内に存在する部屋のIDリスト</returns>
    public List<int> GetRoomsInRange(int x, int y, float range)
    {
        List<int> roomsInRange = new List<int>();
        for(int i = 0; i < dungeon.rooms.Count; i++)
        {
            var center = GetRoomCenter(i);
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center.x, center.y));
            if(distance <= range)
            {
                roomsInRange.Add(i);
            }
        }
        return roomsInRange;
    }

    /// <summary>
    /// 指定した座標点が廊下内に存在するかを確認する
    /// </summary>
    /// <param name="x">座標のX値</param>
    /// <param name="y">座標のY値</param>
    /// <returns>廊下内に存在する場合はtrue、それ以外はfalse</returns>
    public bool IsCoordinateInCorridor(int x, int y)
    {
        foreach(var corridor in dungeon.corridors)
        {
            foreach(var point in corridor.GetPoints())
            {
                if(point.x == x && point.y == y)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 指定した部屋IDが特定のタイプかを確認する
    /// </summary>
    /// <param name="roomID">部屋のID</param>
    /// <param name="roomType">確認する部屋タイプ</param>
    /// <returns>特定のタイプであればtrue、それ以外はfalse</returns>
    public bool IsRoomOfType(int roomID, AreaType roomType)
    {
        if(roomID < 0 || roomID >= dungeon.areaTypes.Count)
        {
            Debug.LogWarning($"無効な部屋ID: {roomID}");
            return false;
        }
        return dungeon.areaTypes.ContainsKey(roomID) && dungeon.areaTypes[roomID] == roomType;
    }
    /// <summary>
    /// ポイントに一番近い部屋を見つける
    /// </summary>
    /// <param name="point">探索対象のポイント</param>
    /// <returns>最も近い部屋のインデックス、見つからない場合は -1</returns>
    public int find_room_by_point((int, int) point, List<Room> rooms){
        double min_dist = double.PositiveInfinity;
        int room_idx = -1;
        for(int i = 0; i < rooms.Count; i++){
            (int, int) center = (rooms[i].x + rooms[i].width / 2, rooms[i].y + rooms[i].height / 2);
            double dist = Math.Sqrt(Math.Pow(center.Item1 - point.Item1, 2) + Math.Pow(center.Item2 - point.Item2, 2)); // ユークリッド距離
            if(dist < min_dist){
                min_dist = dist;
                room_idx = i;
            }
        }

        if(dungeon.debug){
            Debug.Log($"find_room_by_point: Point({point.Item1}, {point.Item2}) -> Room Index: {room_idx}");
        }

        return room_idx;
    }


}
