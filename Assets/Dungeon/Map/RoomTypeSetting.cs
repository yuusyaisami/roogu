using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// シリアライズ可能な部屋タイプ設定クラス
[System.Serializable]
public class RoomTypeSetting
{
    public string roomType;
    [Tooltip("0以上の値を指定してください。-1を指定すると、残りの全ての部屋にこのタイプを割り当てます。")]
    public int count; // -1の場合は残りの全ての部屋に割り当て
}