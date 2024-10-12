using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinder
{
    Dungeon dungeon;
    public PathFinder(Dungeon dungeon){
        this.dungeon = dungeon;
    }
    /// <summary>
    /// A*アルゴリズムを用いて最短経路を探索する。
    /// </summary>
    /// <param name="startIdx">開始部屋のインデックス</param>
    /// <param name="goalIdx">目標部屋のインデックス</param>
    /// <returns>経路の部屋インデックスリスト、見つからない場合は null</returns>
    public List<int> FindPathAStar(int startIdx, int goalIdx)
    {
        if(startIdx < 0 || startIdx >= dungeon.rooms.Count || goalIdx < 0 || goalIdx >= dungeon.rooms.Count){
            if(dungeon.debug) Debug.LogWarning("startIdx または goalIdx が無効です。");
            return null;
        }

        var openSet = new PriorityQueue<int, int>();
        var cameFrom = new Dictionary<int, int>();
        var gScore = new int[dungeon.rooms.Count];
        var fScore = new int[dungeon.rooms.Count];
        for(int i = 0; i < dungeon.rooms.Count; i++)
        {
            gScore[i] = int.MaxValue;
            fScore[i] = int.MaxValue;
        }

        gScore[startIdx] = 0;
        fScore[startIdx] = HeuristicCostEstimate(startIdx, goalIdx);
        openSet.Enqueue(startIdx, fScore[startIdx]);

        var closedSet = new HashSet<int>();

        while(openSet.Count > 0)
        {
            int current = openSet.Dequeue();

            // current が room_graph の範囲内にあるかチェック
            if(current < 0 || current >= dungeon.room_graph.Count){
                if(dungeon.debug) Debug.LogError($"current ({current}) が room_graph の範囲外です。");
                continue;
            }

            if(current == goalIdx)
            {
                return ReconstructPath(cameFrom, current);
            }

            if(closedSet.Contains(current))
                continue;

            closedSet.Add(current);

            foreach(var neighbor in dungeon.room_graph[current])
            {
                if(neighbor < 0 || neighbor >= dungeon.rooms.Count){
                    if(dungeon.debug) Debug.LogWarning($"隣接部屋のインデックスが無効です: {neighbor}");
                    continue;
                }

                if(closedSet.Contains(neighbor))
                    continue;

                int tentativeGScore = gScore[current] + 1;
                if(tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + HeuristicCostEstimate(neighbor, goalIdx);
                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        return null; // 経路が見つからなかった場合
    }

    /// <summary>
    /// ヒューリスティックなコスト推定（マンハッタン距離）
    /// </summary>
    /// <param name="idx1">部屋1のインデックス</param>
    /// <param name="idx2">部屋2のインデックス</param>
    /// <returns>推定コスト</returns>
    private int HeuristicCostEstimate(int idx1, int idx2)
    {
        var center1 = GetRoomCenter(idx1);
        var center2 = GetRoomCenter(idx2);
        return Mathf.Abs(center1.x - center2.x) + Mathf.Abs(center1.y - center2.y);
    }

    /// <summary>
    /// 指定された部屋の中心座標を返す
    /// </summary>
    /// <param name="idx">部屋のインデックス</param>
    /// <returns>中心座標</returns>
    private (int x, int y) GetRoomCenter(int idx)
    {
        var room = dungeon.rooms[idx];
        return (room.x + room.width / 2, room.y + room.height / 2);
    }

    /// <summary>
    /// A* アルゴリズムで見つかった経路を再構築
    /// </summary>
    /// <param name="cameFrom">経路の前方情報</param>
    /// <param name="current">現在の部屋インデックス</param>
    /// <returns>経路の部屋インデックスリスト</returns>
    private List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
    {
        var totalPath = new List<int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }
        return totalPath;
    }
}
