using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewCustomTile", menuName = "Tiles/CustomTile")]
public class CustomTile : Tile
{
    public bool isWalkable = true; // 歩行可能かどうか
    public string tileTag = ""; // タイルのタグ（入口、出口、特殊オブジェクト用）
    public string specialObjectName = ""; // 特殊オブジェクトの名前
}
