using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 固定部屋を管理するクラス
/// </summary>
[CreateAssetMenu(fileName = "FixedRoomManager", menuName = "Dungeon/Fixed/FixedRoomManager")]
public class FixedRoomManager : ScriptableObject
{
    public List<FixedRoom> fixedRooms = new List<FixedRoom>(); // 固定部屋のリスト
    Dungeon dungeon;
    public FixedRoomManager(Dungeon dungeon){
        this.dungeon = dungeon;
    }
    /// <summary>
    /// 固定部屋を優先度とサイズに基づいて選択するメソッド
    /// </summary>
    /// <param name="node">固定部屋を挿入するノード</param>
    /// <returns>選択されたFixedRoom、存在しない場合はnull</returns>
    public FixedRoom SelectFixedRoom(BSPNode node)
    {
        // 対象ノードに収まる固定部屋をフィルタリング
        if(dungeon.debug) Debug.Log(fixedRooms.Count);
        List<FixedRoom> suitableRooms = fixedRooms.FindAll(room => room.size.x <= node.width && room.size.y <= node.height && dungeon.areaTypes[node.room.id] == room.areaType);

        if (suitableRooms.Count == 0)
            return null;

        // 優先度順にソート（高い優先度が先）
        suitableRooms.Sort((a, b) => b.priority.CompareTo(a.priority));

        // 高い優先度の固定部屋を選ぶ
        // 同一優先度の場合はランダムに選択
        int highestPriority = suitableRooms[0].priority;
        List<FixedRoom> highestPriorityRooms = suitableRooms.FindAll(room => room.priority == highestPriority);

        FixedRoom selectedRoom = highestPriorityRooms[UnityEngine.Random.Range(0, highestPriorityRooms.Count)];

        // ExtractTileLayout を呼び出して tileLayout と tileTags を設定
        selectedRoom.ExtractTileLayout();

        return selectedRoom;
    }
}
