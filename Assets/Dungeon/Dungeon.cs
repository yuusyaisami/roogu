using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Diagnostics;

// ダンジョンクラス
public class Dungeon : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int width = 80; // ダンジョン全体の幅
    public int height = 80; // ダンジョン全体の高さ
    public int min_room_size = 10; // 部屋の最小サイズ
    public int min_split_size = 20; // 分割エリア最小サイズ
    public int max_split_depth = 5; // 最大分割深度
    public string corridor_type = "mst"; // 廊下の接続アルゴリズム ('direct', 'bsp', 'mst')
    public Vector2 entrance = Vector2.zero; // 入り口の位置 (Vector2.zeroで自動設定)
    public Vector2 exit = Vector2.zero; // 出口の位置 (Vector2.zeroで自動設定)
    public int seed = 98; // ランダムシード
    public int room_count = 15; // 部屋の数の指定 (0で自動)
    public bool debug = true; // デバッグモード
    [Header("Room Type Settings")]
    public AreaType default_room_type = AreaType.UnKnown; // デフォルトの部屋タイプ

    [Header("Area Type Settings")]
    public AreaType default_area_type = AreaType.Standard; // デフォルトのエリアタイプ
    public List<AreaTypeSetting> areaTypeSettings = new List<AreaTypeSetting>()
    {
        new AreaTypeSetting { areaType = AreaType.Entrance, count = 1 },
        new AreaTypeSetting { areaType = AreaType.Boss, count = 1 },
        new AreaTypeSetting { areaType = AreaType.Plaza, count = 2 },
        new AreaTypeSetting { areaType = AreaType.TreasureRoom, count = 2 },
        new AreaTypeSetting { areaType = AreaType.SmallRoom, count = 5 },
        // Default type is "Standard"
    };
    // Dungeon.cs のフィールド部分を以下のように変更
    [Header("Dungeon Map")]
    public List<List<TileData>> dungeonMap; // 2Dリストでダンジョンを管理


    [HideInInspector]
    public int start_room_idx = -1; // 入り口のインデックス
    [HideInInspector]
    public int end_room_idx = -1; // 出口のインデックス
    [HideInInspector]
    public List<Corridor> corridors = new List<Corridor>(); // 生成された廊下のリスト
    [HideInInspector]
    public List<List<int>> room_graph = new List<List<int>>(); // 部屋間のグラフ（隣接リスト）
    [HideInInspector]
    public BSPNode root; // BSPツリーのルート
    [HideInInspector]
    public List<Room> rooms = new List<Room>(); // 部屋のリスト
    [HideInInspector]
    public Dictionary<int, AreaType> areaTypes = new Dictionary<int, AreaType>(); // エリアの種類
    [Header("Fixed Rooms")]
    public FixedRoomManager fixedRoomManager; // 固定部屋マネージャーへの参照
    public float fixedRoomInsertionChance = 0.2f; // 固定部屋を挿入する確率
    // DungeonUtilsのインスタンス
    private DungeonUtils dungeonUtils;
    private Connect connect;

    // AreaTypeAssignerのインスタンス
    private AreaTypeAssigner areaTypeAssigner; // AreaTypeAssignerのインスタンス
    

    // Start is called before the first frame update
    void Start()
    {
        // ランダムシードの設定
        UnityEngine.Random.InitState(seed);
        
        // BSPツリーのルートノードを初期化
        root = new BSPNode(0, 0, width, height);

        // connectの初期化
        connect = new Connect(this);
        // DungeonUtilsのインスタンスを作成
        dungeonUtils = new DungeonUtils(this);
        // ダンジョン生成を開始
        generateDungeon();

        if(debug) PrintAllRoomTypes();
    }

    /// <summary>
    /// ダンジョンを生成するメソッド。
    /// BSPツリーによるスペースの分割、部屋の生成、廊下の接続、
    /// 入口と出口の配置、部屋タイプの割り当てを行う。
    /// </summary>
    void generateDungeon(){
        if(debug) Debug.Log("生成を開始する");
        recursiveSplit(root, 0); // 深度を0から開始
        AssignAreaTypes(); // エリアタイプを割り当て
        createRooms(root); // 部屋の作成
        // 廊下の接続方法をオプションで実行
        if(corridor_type == "direct") connect.rooms_direct(root);
        else if(corridor_type == "bsp") connect.rooms_bsp(root);
        else if(corridor_type == "mst") connect.rooms_mst();
        connect.place_entrance_and_exit(); // 入口と出口を配置
        connect.ensure_entrance_exit_connected(); // 接続確認
        GenerateDungeonMap(); // ダンジョンマップを生成
        if (debug) Debug.Log("生成完了");
    }

    /// <summary>
    /// 部屋の種類を割り当てるメソッド
    /// </summary>
    void AssignAreaTypes()
    {
        // AreaTypeAssignerのインスタンスを作成
        areaTypeAssigner = new AreaTypeAssigner(this, areaTypeSettings, default_room_type);
        // 部屋の種類を割り当て
        areaTypeAssigner.AssignAreaTypes();

        // AreaTypesの内容を取得\
        areaTypes = areaTypeAssigner.AreaTypes;
        if (debug)
        {
            foreach (var kvp in areaTypes)
            {
                Debug.Log($"部屋 {kvp.Key}: {kvp.Value}");
            }
        }
    }

    /// <summary>
    /// ダンジョンを2次元リストに変換するメソッド。
    /// 壁と床を別々のレイヤーで管理し、部屋の種類や廊下の情報も保持します。
    /// 部屋と廊下が重なった場合は部屋を優先します。
    /// </summary>
    public void GenerateDungeonMap()
    {
        // ダンジョンマップの初期化
        dungeonMap = new List<List<TileData>>();
        for (int x = 0; x < width; x++)
        {
            List<TileData> column = new List<TileData>();
            for (int y = 0; y < height; y++)
            {
                column.Add(new TileData()); // 初期状態は壁
            }
            dungeonMap.Add(column);
        }

        // 部屋を床に設定し、部屋の種類も設定
        for (int i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            AreaType currentRoomType = areaTypes.ContainsKey(room.id) ? areaTypes[room.id] : AreaType.UnKnown;
           if (room.isFixedRoom) Debug.LogWarning($"デバッグログ: {room.fixedRoom.tileLayout[0][0]}");
            if (room.isFixedRoom && room.fixedRoom != null && room.fixedRoom.tileLayout != null)
            {
                // 固定部屋の場合は tileLayout を使用してタイルを設定
                if (debug){
                    string line = "";
                    for(int r = 0; r < room.fixedRoom.tileLayout.Count; r++){
                        for(int c = 0; c < room.fixedRoom.tileLayout[r].Count; c++){
                            if(room.fixedRoom.tileLayout[r][c] != true){
                                line += "■";
                            } 
                            else{
                                line += "□";
                            }
                        }
                        line += "\n";
                    }
                    Debug.Log(line);
                }
                for (int localX = 0; localX < room.fixedRoom.size.x; localX++)
                {
                    for (int localY = 0; localY < room.fixedRoom.size.y; localY++)
                    {
                        int dungeonX = room.x + localX;
                        int dungeonY = room.y + localY;

                        if (dungeonX >= 0 && dungeonX < width && dungeonY >= 0 && dungeonY < height) 
                        { 
                            bool isWalkable = room.fixedRoom.tileLayout[localY][localX];
                            dungeonMap[dungeonX][dungeonY].tileType = isWalkable ? TileType.Floor : TileType.Wall; 
                            dungeonMap[dungeonX][dungeonY].areaType = currentRoomType; 
                            dungeonMap[dungeonX][dungeonY].isCorridor = false; // 部屋なので廊下フラグはfalse 
                        } 
                    } 
                } 
            } 
            else 
            { 
                // 通常の部屋の場合はすべて床
                for (int x = room.x; x < room.x + room.width; x++)
                {
                    for (int y = room.y; y < room.y + room.height; y++)
                    {
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            dungeonMap[x][y].tileType = TileType.Floor; 
                            dungeonMap[x][y].areaType = currentRoomType;
                            dungeonMap[x][y].isCorridor = false; // 部屋なので廊下フラグはfalse
                        }
                    }
                }
            }
        }

        // 廊下を床に設定し、廊下の情報も設定
        foreach (var corridor in corridors)
        {
            foreach (var point in corridor.GetPoints())
            {
                int x = point.x;
                int y = point.y;
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    // 部屋の情報が既に設定されている場合は上書きしない ... 壁の場合も同じように
                    if (dungeonMap[x][y].areaType == AreaType.UnKnown || dungeonMap[x][y].tileType == TileType.Wall)
                    {
                        dungeonMap[x][y].tileType = TileType.Floor;
                        dungeonMap[x][y].isCorridor = true;
                    }
                }
            }
        }

        // 入口と出口を設定
        if (entrance != Vector2.zero)
        {
            int ex = Mathf.RoundToInt(entrance.x);
            int ey = Mathf.RoundToInt(entrance.y);
            if (ex >= 0 && ex < width && ey >= 0 && ey < height)
            {
                dungeonMap[ex][ey].tileType = TileType.Entrance;
                dungeonMap[ex][ey].areaType = AreaType.UnKnown; // 入口は特別なタイプとして扱う
                dungeonMap[ex][ey].isCorridor = false;
            }
        }

        if (exit != Vector2.zero)
        {
            int ex = Mathf.RoundToInt(exit.x);
            int ey = Mathf.RoundToInt(exit.y);
            if (ex >= 0 && ex < width && ey >= 0 && ey < height)
            {
                dungeonMap[ex][ey].tileType = TileType.Exit;
                dungeonMap[ex][ey].areaType = AreaType.UnKnown; // 出口は特別なタイプとして扱う
                dungeonMap[ex][ey].isCorridor = false;
            }
        }

        if (debug)
        {
            Debug.Log("ダンジョンマップが生成されました。");
            Debug.Log($"サイズは{dungeonMap.Count}x{dungeonMap[0].Count}");

            string mapVisual = "\n";
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    switch (dungeonMap[x][y].tileType)
                    {
                        case TileType.Wall:
                            mapVisual += "■";
                            break;
                        case TileType.Floor:
                            if (dungeonMap[x][y].areaType != AreaType.UnKnown)
                            {
                                switch (dungeonMap[x][y].areaType)
                                {
                                    case AreaType.Entrance:
                                        mapVisual += "E"; // 入口部屋
                                        break;
                                    case AreaType.Boss:
                                        mapVisual += "B"; // ボス部屋
                                        break;
                                    case AreaType.TreasureRoom:
                                        mapVisual += "タ"; // 宝箱部屋
                                        break;
                                    case AreaType.Puzzle:
                                        mapVisual += "パ"; // パズル部屋
                                        break;
                                    case AreaType.Shop:
                                        mapVisual += "オ"; // 店舗部屋
                                        break;
                                    case AreaType.Rest:
                                        mapVisual += "休"; // 休憩部屋
                                        break;
                                    default:
                                        mapVisual += "床"; // 一般的な床
                                        break;
                                }
                            }
                            else if (dungeonMap[x][y].isCorridor)
                            {
                                mapVisual += "ろ"; // 廊下
                            }
                            else
                            {
                                mapVisual += "一"; // 一般的な床
                            }
                            break;
                        case TileType.Entrance:
                            mapVisual += "え";
                            break;
                        case TileType.Exit:
                            mapVisual += "で";
                            break;
                        default:
                            mapVisual += "な";
                            break;
                    }
                }
                mapVisual += "\n";
            }
            Debug.Log(mapVisual);
        }
    }



    /// <summary>
    /// BSPツリーを分割する（再帰）
    /// </summary>
    /// <param name="node">分割対象のノード</param>
    /// <param name="depth">現在の分割深度</param>
    void recursiveSplit(BSPNode node, int depth){
        // 最大深度に到達した場合または最小分割サイズ以下は停止
        if(depth >= max_split_depth || node.width <= min_split_size || node.height <= min_split_size) return;

        // 部屋数の制限がある場合
        if(room_count > 0 && rooms.Count >= room_count){
            if(debug) Debug.Log($"部屋数の上限 {room_count} に達したため、分割を停止します。");
            return;
        }

        // 分割方向の決定
        string split_vertically = chooseSplitDirection(node.width, node.height);
        if (split_vertically == "Vertical"){
            // 縦方向に分割
            int max_split = node.width - min_split_size;
            int min_split = min_split_size;
            if(max_split <= min_split) return;
            int split = UnityEngine.Random.Range(min_split, max_split);
            node.left_child = new BSPNode(node.x, node.y, split, node.height);
            node.right_child = new BSPNode(node.x + split, node.y, node.width - split, node.height);
            node.split_line = ("Vertical", node.x + split); // 分割ラインを記録
        }
        else{
            // 横方向に分割
            int max_split = node.height - min_split_size;
            int min_split = min_split_size;
            if(max_split <= min_split) return;
            int split = UnityEngine.Random.Range(min_split, max_split);
            node.left_child = new BSPNode(node.x, node.y, node.width, split);
            node.right_child = new BSPNode(node.x, node.y + split, node.width, node.height - split);
            node.split_line = ("Horizontal", node.y + split); // 分割ラインを記録
        }
        // 子ノードの分割
        recursiveSplit(node.left_child, depth + 1);
        recursiveSplit(node.right_child, depth + 1);
    }

    /// <summary>
    /// 分割方向を決定するメソッド
    /// </summary>
    /// <param name="width">ノードの幅</param>
    /// <param name="height">ノードの高さ</param>
    /// <returns>"Vertical" または "Horizontal"</returns>
    public string chooseSplitDirection(int width, int height){
        if ((double)width / height >= 1.25) return "Vertical"; // 縦割り
        else if((double)height / width >= 1.25) return "Horizontal"; // 横割り
        else return UnityEngine.Random.value > 0.5f ? "Vertical" : "Horizontal"; 
    }

     /// <summary>
    /// BSPツリーから部屋を生成する
    /// </summary>
    /// <param name="node">部屋を生成するノード</param>
    public void createRooms(BSPNode node)
    {
        if (node.left_child != null || node.right_child != null)
        {
            // 子ノードが存在する場合は、再帰
            if (node.left_child != null) createRooms(node.left_child);
            if (node.right_child != null) createRooms(node.right_child);

            if (node.left_child != null && node.right_child != null)
            {
                // 子ノードの部屋を接続
                if (node.left_child.room.id != -1 && node.right_child.room.id != -1)
                    node.room = choiceItem(node.left_child.room, node.right_child.room);
                else if (node.left_child.room.id != -1)
                    node.room = node.left_child.room;
                else if (node.right_child.room.id != -1)
                    node.room = node.right_child.room;
            }
        }
        else
        {
            // 子ノードが存在しない場合、部屋を作成
            // 固定部屋を割り当てるかどうかをランダムで決定
            bool assignFixedRoom = (fixedRoomManager != null) && (fixedRoomManager.fixedRooms.Count > 0) && (UnityEngine.Random.value < fixedRoomInsertionChance);

            if (assignFixedRoom)
            {
                CreateFixedRoom(node);
            }
            else
            {
                CreateRandomRoom(node);
            }
        }
    }

    /// <summary>
    /// 固定部屋を挿入するメソッド
    /// </summary>
    private void CreateFixedRoom(BSPNode node)
    {
        FixedRoom selectedFixedRoom = fixedRoomManager.SelectFixedRoom(node);
        if (selectedFixedRoom != null)
        {
            InsertFixedRoom(node, selectedFixedRoom);
        }
        else
        {
            CreateRandomRoom(node);
        }
    }

    /// <summary>
/// 固定部屋を挿入するメソッド
/// </summary>
private void InsertFixedRoom(BSPNode node, FixedRoom fixedRoom)
{
    // タイルレイアウトを初期化
    fixedRoom.ExtractTileLayout();

    // tileLayout が正しく設定されているか確認
    if (fixedRoom.tileLayout == null || fixedRoom.tileLayout.Count == 0 || fixedRoom.tileLayout[0].Count == 0)
    {
        Debug.LogError($"FixedRoom: 部屋ID {fixedRoom.name} の tileLayout が正しく設定されていません。");
        return;
    }

    int room_x = node.x + UnityEngine.Random.Range(0, node.width - fixedRoom.size.x + 1);
    int room_y = node.y + UnityEngine.Random.Range(0, node.height - fixedRoom.size.y + 1);

    // 部屋が既に存在するか確認（重複を避けるため）
    bool overlaps = rooms.Any(r =>
        room_x < r.x + r.width &&
        room_x + fixedRoom.size.x > r.x &&
        room_y < r.y + r.height &&
        room_y + fixedRoom.size.y > r.y
    );

    if (overlaps)
    {
        if (debug) Debug.LogWarning($"Dungeon: 固定部屋の挿入位置が他の部屋と重複しています。部屋ID: {rooms.Count}");
        return; // 重複する場合は挿入をスキップ
    }

    // 新しい固定部屋を作成
    Room newRoom = new Room
    {
        id = rooms.Count,
        x = room_x,
        y = room_y,
        width = fixedRoom.size.x,
        height = fixedRoom.size.y,
        isFixedRoom = true,
        fixedRoom = fixedRoom,
        entrancePoint = fixedRoom.entrancePoints.Count > 0 ? fixedRoom.entrancePoints[UnityEngine.Random.Range(0, fixedRoom.entrancePoints.Count)] : Vector2Int.zero,
        exitPoint = fixedRoom.exitPoints.Count > 0 ? fixedRoom.exitPoints[UnityEngine.Random.Range(0, fixedRoom.exitPoints.Count)] : Vector2Int.zero
    };

    node.room = newRoom;
    rooms.Add(newRoom);
    areaTypes[newRoom.id] = fixedRoom.areaType; // エリアタイプを設定

    if (debug)
    {
        Debug.Log($"Dungeon: 固定部屋を挿入しました。部屋ID: {newRoom.id}, 位置: ({room_x}, {room_y}), サイズ: ({fixedRoom.size.x}, {fixedRoom.size.y}), Type: {fixedRoom.areaType}");
        Debug.Log($"タイルレイアウトの0,0: {fixedRoom.tileLayout[0][0]}");
    }
}


    /// <summary>
    /// 通常のランダム生成部屋を作成するメソッド
    /// </summary>
    private void CreateRandomRoom(BSPNode node)
    {
        int room_width = UnityEngine.Random.Range(min_room_size, Mathf.CeilToInt(node.width * 0.75f));
        int room_height = UnityEngine.Random.Range(min_room_size, Mathf.CeilToInt(node.height * 0.75f));
        int room_x = node.x + UnityEngine.Random.Range(1, node.width - room_width - 1);
        int room_y = node.y + UnityEngine.Random.Range(1, node.height - room_height - 1);

        // 部屋が既に存在するか確認（重複を避けるため）
        bool overlaps = rooms.Any(r =>
            room_x < r.x + r.width &&
            room_x + room_width > r.x &&
            room_y < r.y + r.height &&
            room_y + room_height > r.y
        );

        if (overlaps)
        {
            if (debug) Debug.LogWarning($"Dungeon: 通常部屋の挿入位置が他の部屋と重複しています。部屋ID: {rooms.Count}");
            return; // 重複する場合は挿入をスキップ
        }

        // 新しい通常部屋を作成
        Room newRoom = new Room
        {
            id = rooms.Count,
            x = room_x,
            y = room_y,
            width = room_width,
            height = room_height,
            isFixedRoom = false,
            fixedRoom = null,
            entrancePoint = Vector2Int.zero,
            exitPoint = Vector2Int.zero
        };

        node.room = newRoom;
        rooms.Add(newRoom);
        areaTypes[newRoom.id] = AreaType.Standard; // デフォルトのエリアタイプを設定

        if (debug)
        {
            Debug.Log($"Dungeon: 通常の部屋を作成しました。部屋ID: {newRoom.id}, 位置: ({room_x}, {room_y}), サイズ: ({room_width}, {room_height}), Type: {AreaType.Standard}");
        }
    }

    /// <summary>
    /// A*アルゴリズムを用いて最短経路を探索する。
    /// </summary>
    /// <param name="startIdx">開始部屋のインデックス</param>
    /// <param name="goalIdx">目標部屋のインデックス</param>
    /// <returns>経路の部屋インデックスリスト、見つからない場合は null</returns>
    public List<int> FindPathAStar(int startIdx, int goalIdx)
    {
        if(startIdx < 0 || startIdx >= rooms.Count || goalIdx < 0 || goalIdx >= rooms.Count){
            if(debug) Debug.LogWarning("startIdx または goalIdx が無効です。");
            return null;
        }

        var openSet = new PriorityQueue<int, int>();
        var cameFrom = new Dictionary<int, int>();
        var gScore = new int[rooms.Count];
        var fScore = new int[rooms.Count];
        for(int i = 0; i < rooms.Count; i++)
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
            if(current < 0 || current >= room_graph.Count){
                if(debug) Debug.LogError($"current ({current}) が room_graph の範囲外です。");
                continue;
            }

            if(current == goalIdx)
            {
                return ReconstructPath(cameFrom, current);
            }

            if(closedSet.Contains(current))
                continue;

            closedSet.Add(current);

            foreach(var neighbor in room_graph[current])
            {
                if(neighbor < 0 || neighbor >= rooms.Count){
                    if(debug) Debug.LogWarning($"隣接部屋のインデックスが無効です: {neighbor}");
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
        var room = rooms[idx];
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

    /// <summary>
    /// 2つの部屋から一方をランダムに選ぶ
    /// </summary>
    /// <param name="node1">部屋1</param>
    /// <param name="node2">部屋2</param>
    /// <returns>選ばれた部屋</returns>
    private Room choiceItem(Room node1, Room node2){
        // ランダムに選ぶ
        return UnityEngine.Random.value > 0.5f ? node1 : node2;
    }


    /// <summary>
    /// BSPツリー内の部屋の中心を取得するメソッド
    /// </summary>
    /// <param name="node">対象のノード</param>
    /// <returns>中心座標</returns>
    public Vector2 GetSubtreeCenter(BSPNode node)
    {
        if (node.room.x != -1) // 部屋がある場合
        {
            int x = node.room.x + node.room.width / 2;
            int y = node.room.y + node.room.height / 2;
            return new Vector2(x, y);
        }
        else if (node.left_child != null && node.right_child != null) // 子ノードがある場合
        {
            Vector2 leftCenter = GetSubtreeCenter(node.left_child);
            Vector2 rightCenter = GetSubtreeCenter(node.right_child);

            if (leftCenter.x != -1 && rightCenter.x != -1) // 両方の子に部屋がある場合
            {
                float x = (leftCenter.x + rightCenter.x) / 2f;
                float y = (leftCenter.y + rightCenter.y) / 2f;
                return new Vector2(x, y);
            }
            else if (leftCenter.x != -1)
            {
                return leftCenter;
            }
            else if (rightCenter.x != -1)
            {
                return rightCenter;
            }
        }
        return new Vector2(-1, -1); // 部屋がない場合
    }
    /// <summary>
    /// すべての部屋の種類をコンソールに出力するメソッド
    /// </summary>
    public void PrintAllRoomTypes()
    {
        if (areaTypes == null || areaTypes.Count == 0)
        {
            Debug.LogWarning("部屋の種類が割り当てられていません。");
            return;
        }

        Debug.Log("----- 部屋の種類一覧 -----");
        foreach (var kvp in areaTypes)
        {
            int roomID = kvp.Key;
            AreaType type = kvp.Value;
            Debug.Log($"部屋ID: {roomID}, 種類: {type}");
        }
        Debug.Log("--------------------------");
    }

    /// <summary>
/// ダンジョンの構造をシーンビューに描画するためのGizmosを描画するメソッド
/// </summary>
void OnDrawGizmos()
{
    if (dungeonMap == null || dungeonMap.Count == 0 || debug == false)
        return;

    for (int x = 0; x < dungeonMap.Count; x++)
    {
        for (int y = 0; y < dungeonMap[x].Count; y++)
        {
            TileData tile = dungeonMap[x][y];
            Vector3 position = new Vector3(x, y, 0); // Y軸を高さとして使用

            switch (tile.tileType)
            {
                case TileType.Wall:
                    Gizmos.color = Color.gray;
                    Gizmos.DrawCube(position, Vector3.one);
                    break;
                case TileType.Floor:
                    if (tile.areaType == AreaType.UnKnown)
                    {
                        // 部屋の種類に応じて色を変更
                        switch (tile.areaType)
                        {
                            case AreaType.Entrance:
                                Gizmos.color = Color.green;
                                break;
                            case AreaType.Boss:
                                Gizmos.color = Color.red;
                                break;
                            case AreaType.TreasureRoom:
                                Gizmos.color = Color.yellow;
                                break;
                            case AreaType.Puzzle:
                                Gizmos.color = Color.magenta;
                                break;
                            case AreaType.Shop:
                                Gizmos.color = Color.blue;
                                break;
                            case AreaType.Rest:
                                Gizmos.color = Color.cyan;
                                break;
                            default:
                                Gizmos.color = Color.white;
                                break;
                        }
                        Gizmos.DrawCube(position, Vector3.one);
                    }
                    else if (tile.isCorridor)
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawCube(position, Vector3.one * 0.5f); // 廊下は小さめのキューブで表示
                    }
                    else
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawCube(position, Vector3.one);
                    }
                    break;
                case TileType.Entrance:
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(position, 0.3f);
                    break;
                case TileType.Exit:
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(position, 0.3f);
                    break;
                default:
                    Gizmos.color = Color.black;
                    Gizmos.DrawWireCube(position, Vector3.one);
                    break;
            }
        }
    }

    // 追加: 各部屋の入口と出口を描画
    if (rooms != null)
    {
        foreach (var room in rooms)
        {
            if (room.isFixedRoom && room.fixedRoom != null)
            {
                // 固定部屋の入口を描画
                foreach (var entrance in room.fixedRoom.entrancePoints)
                {
                    Vector3 entrancePos = new Vector3(room.x + entrance.x, room.y + entrance.y, 0);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(entrancePos, 0.2f);
                }

                // 固定部屋の出口を描画
                foreach (var exitPoint in room.fixedRoom.exitPoints)
                {
                    Vector3 exitPos = new Vector3(room.x + exitPoint.x, room.y + exitPoint.y, 0);
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(exitPos, 0.2f);
                }
            }
            else
            {
                // ランダム生成部屋の場合、中央に入口と出口を1つずつ表示
                Vector3 center = new Vector3(room.x + room.width / 2f, room.y + room.height / 2f, 0);
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(center + Vector3.left * (room.width / 2f - 1), 0.2f); // 仮の入口
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(center + Vector3.right * (room.width / 2f - 1), 0.2f); // 仮の出口
            }
        }
    }

    // 追加: 廊下を描画
    if (corridors != null)
    {
        Gizmos.color = Color.white;
        foreach (var corridor in corridors)
        {
            Gizmos.DrawLine(new Vector3(corridor.Start.x, corridor.Start.y, 0),
                           new Vector3(corridor.Mid.x, corridor.Mid.y, 0));
            Gizmos.DrawLine(new Vector3(corridor.Mid.x, corridor.Mid.y, 0),
                           new Vector3(corridor.End.x, corridor.End.y, 0));
        }
    }
}




    // Update is called once per frame
    void Update()
    {

    }
}
