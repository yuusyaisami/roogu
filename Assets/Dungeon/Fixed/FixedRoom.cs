using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewFixedRoom", menuName = "Dungeon/Fixed/FixedRoom")]
public class FixedRoom : ScriptableObject
{
    public Vector2Int size; // 部屋のサイズ (幅, 高さ)
    public GameObject prefab; // 部屋のプレハブ（Tilemap を含む）
    public AreaType areaType; // 部屋の種類
    public List<Vector2Int> entrancePoints = new List<Vector2Int>(); // 入口の相対位置
    public List<Vector2Int> exitPoints = new List<Vector2Int>(); // 出口の相対位置
    public int priority = 1; // 挿入の優先度（高いほど優先的に選ばれる）

    // Tilemap のデータを取得するプロパティ
    [HideInInspector]
    public List<List<bool>> tileLayout = new List<List<bool>>(); // タイルレイアウト (true: Walkable, false: Wall)
    [HideInInspector]
    public List<List<string>> tileTags = new List<List<string>>(); // タイルごとのタグ（特殊オブジェクト用）

    // プレハブから Tilemap を取得して tileLayout と tileTags を設定するメソッド
    public void ExtractTileLayout()
    {
        if (prefab == null)
        {
            Debug.LogError("FixedRoom: Prefab が割り当てられていません。");
            return;
        }

        // プレハブを一時的にインスタンス化して Tilemap データを取得
        GameObject tempPrefab = Instantiate(prefab);
        Tilemap tilemap = tempPrefab.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("FixedRoom: Prefab に Tilemap コンポーネントが見つかりません。");
            DestroyImmediate(tempPrefab);
            return;
        }

        tileLayout.Clear();
        tileTags.Clear();

        // Tilemap の範囲を取得
        BoundsInt bounds = tilemap.cellBounds;
        size = new Vector2Int(bounds.size.x, bounds.size.y);

        for (int y = 0; y < size.y; y++)
        {
            List<bool> rowLayout = new List<bool>();
            List<string> rowTags = new List<string>();
            for (int x = 0; x < size.x; x++)
            {
                Vector3Int tilePos = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);
                TileBase tile = tilemap.GetTile(tilePos);

                if (tile != null)
                {
                    // CustomTile にキャスト
                    CustomTile customTile = tile as CustomTile;
                    if (customTile != null)
                    {
                        rowLayout.Add(customTile.isWalkable); // 歩行可能か
                        rowTags.Add(customTile.tileTag); // タイルのタグ
                    }
                    else
                    {
                        // CustomTile でない場合はデフォルトで歩行不可
                        rowLayout.Add(false);
                        rowTags.Add(null);
                    }
                }
                else
                {
                    rowLayout.Add(false); // タイルが存在しない場合は歩行不可
                    rowTags.Add(null);
                }
            }
            tileLayout.Add(rowLayout);
            tileTags.Add(rowTags);
        }

        // デバッグ用にタイルレイアウトをログ出力
        string line = "---FixedRoom.cs---\n";
        for (int r = 0; r < tileLayout.Count; r++)
        {
            for (int c = 0; c < tileLayout[r].Count; c++)
            {
                if (!tileLayout[r][c])
                {
                    line += "■";
                }
                else
                {
                    line += "□";
                }
            }
            line += "\n";
        }
        Debug.Log(line);

        DestroyImmediate(tempPrefab); // 一時的に生成したプレハブを削除
    }
}
