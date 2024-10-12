using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
/// <summary>
/// 廊下の接続アルゴリズムクラス
/// </summary>
public class Connect
{
    // Start is called before the first frame update
    public Dungeon dungeon;
    public PathFinder finder;
    public DungeonUtils dungeonUtils;
    public Connect(Dungeon dungeon){
        this.dungeon = dungeon;
        dungeonUtils = new DungeonUtils(dungeon);
        finder = new PathFinder(dungeon);
    }

    // Update is called once per frame
    public void rooms_direct(BSPNode node)
    {
        if (node.left_child != null && node.right_child != null)
        {
            rooms_direct(node.left_child);
            rooms_direct(node.right_child);
    
            if (node.left_child.room != null && node.right_child.room != null)
            {
                // 接続ポイントの取得
                Vector2 point1 = GetConnectionPoint(node.left_child.room, ("", -1));
                Vector2 point2 = GetConnectionPoint(node.right_child.room, ("", -1));
    
                // 廊下を生成
                var corridorPoints = CreateCorridorFromPoints(point1, point2);
                var corridor = new Corridor(corridorPoints[0], corridorPoints[1], corridorPoints[2]);
                node.corridor.Add(corridor);
                dungeon.corridors.Add(corridor);
    
                if (dungeon.debug)
                {
                    Debug.Log($"Dungeon: 部屋 {node.left_child.room.id} と部屋 {node.right_child.room.id} を接続しました。");
                }
            }
    
            // room_graph を構築
            buildRoomGraph();
        }
    }
    /// <summary>
    /// BSP分割ラインに沿って部屋を接続する方法
    /// </summary>
    public void rooms_bsp(BSPNode node)
    {
        if (node.left_child != null && node.right_child != null)
        {
            rooms_bsp(node.left_child);
            rooms_bsp(node.right_child);

            if (node.left_child.room != null && node.right_child.room != null)
            {
                // 接続ポイントの取得（分割ラインに基づく）
                Vector2 point1 = GetConnectionPoint(node.left_child.room, node.split_line);
                Vector2 point2 = GetConnectionPoint(node.right_child.room, node.split_line);

                // 廊下を生成
                var corridorPoints = CreateCorridorFromPoints(point1, point2);
                var corridor = new Corridor(corridorPoints[0], corridorPoints[1], corridorPoints[2]);
                node.corridor.Add(corridor);
                dungeon.corridors.Add(corridor);

                if (dungeon.debug)
                {
                    Debug.Log($"Dungeon: 部屋 {node.left_child.room.id} と部屋 {node.right_child.room.id} を BSP 接続しました。");
                }
            }

            // room_graph を構築
            buildRoomGraph();
        }
    }
    /// <summary>
    /// 'mst'タイプの廊下接続方法を実装するメソッド。
    /// 最小全域木（MST）を用いて部屋を接続。
    /// </summary>
    public void rooms_mst()
    {
        List<Vector2> room_centers = new List<Vector2>();
        foreach (var room in dungeon.rooms)
        {
            room_centers.Add(new Vector2(room.x + room.width / 2, room.y + room.height / 2));
        }
        int num_rooms = room_centers.Count;

        // エッジリストの作成
        List<Edge> edges = new List<Edge>();
        for (int i = 0; i < num_rooms; i++)
        {
            for (int j = i + 1; j < num_rooms; j++)
            {
                double dist = Vector2.Distance(room_centers[i], room_centers[j]);
                edges.Add(new Edge(i, j, dist));
            }
        }

        // MSTの計算（Kruskal's Algorithm）
        KruskalMST kruskal = new KruskalMST();
        List<Edge> mst = kruskal.ComputeMST(num_rooms, edges);

        // MSTに基づいて廊下を接続
        foreach (var edge in mst)
        {
            Vector2 point1 = GetConnectionPoint(dungeon.rooms[edge.From], ("", -1));
            Vector2 point2 = GetConnectionPoint(dungeon.rooms[edge.To], ("", -1));
            var corridorPoints = CreateCorridorFromPoints(point1, point2);
            var corridor = new Corridor(corridorPoints[0], corridorPoints[1], corridorPoints[2]);
            dungeon.corridors.Add(corridor);

            if (dungeon.debug)
            {
                Debug.Log($"Dungeon: 部屋 {edge.From} と部屋 {edge.To} を MST 接続しました。");
            }
        }

        // 隣接リストの更新
        buildRoomGraph();
    }
    /// <summary>
    /// 部屋から接続ポイントを取得するメソッド
    /// </summary>
    /// <param name="room">接続する部屋</param>
    /// <param name="splitLine">BSPの分割ライン（必要な場合）</param>
    /// <returns>接続ポイントの座標</returns>
    Vector2 GetConnectionPoint(Room room, (string direction, int position) splitLine)
    { // splitlineは接続ポイントが存在する場合のみ有効
        if (room.isFixedRoom && room.fixedRoom != null)
        {
            // 固定部屋の場合、入口または出口からランダムに1つ選択
            List<Vector2Int> possiblePoints = new List<Vector2Int>();
            possiblePoints.AddRange(room.fixedRoom.entrancePoints);
            possiblePoints.AddRange(room.fixedRoom.exitPoints);
            if (possiblePoints.Count == 0)
            {
                // 入口や出口が設定されていない場合、部屋の中心を返す
                return new Vector2(room.x + room.width / 2f, room.y + room.height / 2f);
            }
            Vector2Int selectedPoint = possiblePoints[UnityEngine.Random.Range(0, possiblePoints.Count)];
            return new Vector2(room.x + selectedPoint.x, room.y + selectedPoint.y);
        }
        else
        {
            // 通常の部屋の場合、部屋の中心を返す
            return new Vector2(room.x + room.width / 2f, room.y + room.height / 2f);
        }
    }
    /// <summary>
    /// 2つのポイントを基に廊下を作成するメソッド
    /// </summary>
    /// <param name="point1">開始ポイント</param>
    /// <param name="point2">終了ポイント</param>
    /// <returns>L字型の廊下ポイントリスト</returns>
    public List<(int x, int y)> CreateCorridorFromPoints(Vector2 point1, Vector2 point2)
    {
        List<(int x, int y)> corridor = new List<(int x, int y)>();

        if (UnityEngine.Random.value > 0.5f)
        {
            corridor.Add((Mathf.RoundToInt(point1.x), Mathf.RoundToInt(point1.y)));
            corridor.Add((Mathf.RoundToInt(point2.x), Mathf.RoundToInt(point1.y)));
            corridor.Add((Mathf.RoundToInt(point2.x), Mathf.RoundToInt(point2.y)));
        }
        else
        {
            corridor.Add((Mathf.RoundToInt(point1.x), Mathf.RoundToInt(point1.y)));
            corridor.Add((Mathf.RoundToInt(point1.x), Mathf.RoundToInt(point2.y)));
            corridor.Add((Mathf.RoundToInt(point2.x), Mathf.RoundToInt(point2.y)));
        }

        return corridor;
    }
    /// <summary>
    /// 入口と出口が接続されていることを確認し、接続されていなければ経路を追加するメソッド。
    /// </summary>
    public void ensure_entrance_exit_connected(){
        // エントランスとエンドポイントが接続されていなければならない
        if(dungeon.start_room_idx == -1 || dungeon.end_room_idx == -1){
            if(dungeon.debug) Debug.LogWarning("入口または出口が無効な部屋に設定されています。");
            return;
        }

        List<int> path = finder.FindPathAStar(dungeon.start_room_idx, dungeon.end_room_idx);
        if(path == null || path.Count == 0){
            if(dungeon.debug) Debug.LogAssertion("入り口と出口の接続が確認できませんでした。");
            var corridorPoints = CreateCorridorFromPoints(dungeon.entrance, dungeon.exit);
            var corridor = new Corridor(corridorPoints[0], corridorPoints[1], corridorPoints[2]);
            dungeon.corridors.Add(corridor);
            buildRoomGraph();
        }
        else{
            if(dungeon.debug) Debug.Log("入り口と出口は接続されています");
        }
    }
    /// <summary>
    /// 入口と出口を配置するメソッド。
    /// 入口はエリアタイプが "Entrance" の部屋、出口はエリアタイプが "Boss" の部屋に自動配置。
    /// </summary>
    public void place_entrance_and_exit()
    {
        // エリアタイプに基づいて入口と出口の部屋を選択
        var entranceRoom = dungeon.rooms.FirstOrDefault(r => dungeon.areaTypes.ContainsKey(r.id) && dungeon.areaTypes[r.id] == AreaType.Entrance);
        var bossRoom = dungeon.rooms.FirstOrDefault(r => dungeon.areaTypes.ContainsKey(r.id) && dungeon.areaTypes[r.id] == AreaType.Boss);

        if (entranceRoom != null)
        {
            dungeon.entrance = new Vector2(entranceRoom.x + entranceRoom.width / 2f, entranceRoom.y + entranceRoom.height / 2f);
            dungeon.start_room_idx = dungeon.rooms.IndexOf(entranceRoom);
        }
        else
        {
            if (dungeon.rooms.Count > 0)
            {
                var leftmost_room = dungeon.rooms.OrderBy(r => r.x).FirstOrDefault();
                if (leftmost_room != null)
                {
                    dungeon.entrance = new Vector2(leftmost_room.x + leftmost_room.width / 2f, leftmost_room.y + leftmost_room.height / 2f);
                    dungeon.start_room_idx = dungeon.rooms.IndexOf(leftmost_room);
                }
            }
        }

        if (bossRoom != null)
        {
            dungeon.exit = new Vector2(bossRoom.x + bossRoom.width / 2f, bossRoom.y + bossRoom.height / 2f);
            dungeon.end_room_idx = dungeon.rooms.IndexOf(bossRoom);
        }
        else
        {
            if (dungeon.rooms.Count > 0)
            {
                var rightmost_room = dungeon.rooms.OrderByDescending(r => r.x).FirstOrDefault();
                if (rightmost_room != null)
                {
                    dungeon.exit = new Vector2(rightmost_room.x + rightmost_room.width / 2f, rightmost_room.y + rightmost_room.height / 2f);
                    dungeon.end_room_idx = dungeon.rooms.IndexOf(rightmost_room);
                }
            }
        }

        if (dungeon.debug)
            Debug.Log($"入り口と出口の座標: {dungeon.entrance}, {dungeon.exit}");
    }
    /// <summary>
    /// 部屋間の隣接リスト形式のグラフを構築
    /// </summary>
    public void buildRoomGraph()
    {
        dungeon.room_graph = new List<List<int>>();
        for (int idx = 0; idx < dungeon.rooms.Count; idx++)
        {
            dungeon.room_graph.Add(new List<int>()); // 空の隣接リストを追加
        }
        // 通路を基に隣接リストを構築
        foreach (var corridor in dungeon.corridors)
        {
            (int, int) start_point = corridor.Start;
            (int, int) end_point = corridor.End;
            int start_room_idx = dungeonUtils.find_room_by_point(start_point, dungeon.rooms);
            int end_room_idx = dungeonUtils.find_room_by_point(end_point, dungeon.rooms);
            // 有効な部屋に属している場合、隣接リストに追加
            if (start_room_idx != -1 && end_room_idx != -1)
            {
                // 重複を避けるためにチェック
                if (!dungeon.room_graph[start_room_idx].Contains(end_room_idx))
                    dungeon.room_graph[start_room_idx].Add(end_room_idx);
                if (!dungeon.room_graph[end_room_idx].Contains(start_room_idx))
                    dungeon.room_graph[end_room_idx].Add(start_room_idx);
            }
        }

        if (dungeon.debug)
        {
            for (int i = 0; i < dungeon.room_graph.Count; i++)
            {
                //Debug.Log($"Room {i} の隣接部屋: {string.Join(",", room_graph[i].Select(x => x.ToString()).ToArray())}");
            }
        }
    }
}
