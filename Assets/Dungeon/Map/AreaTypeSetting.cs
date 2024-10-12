using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 部屋の種類とその希望数を設定するクラス
/// </summary>
[System.Serializable]
public class AreaTypeSetting
{
    public AreaType areaType;
    public int count; // 希望するエリアの数
}