using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AreaTypeAssigner
{
    private Dungeon dungeon;
    private List<AreaTypeSetting> areaTypeSettings;
    private AreaType defaultAreaType;

    public Dictionary<int, AreaType> AreaTypes { get; private set; } = new Dictionary<int, AreaType>();

    public AreaTypeAssigner(Dungeon dungeon, List<AreaTypeSetting> areaTypeSettings, AreaType defaultAreaType)
    {
        this.dungeon = dungeon;
        this.areaTypeSettings = areaTypeSettings;
        this.defaultAreaType = defaultAreaType;
    }

    /// <summary>
    /// エリアタイプを割り当てるメソッド
    /// </summary>
    public void AssignAreaTypes()
    {
        // エリアタイプの希望数をカウント
        Dictionary<AreaType, int> typeCounts = new Dictionary<AreaType, int>();
        foreach (var setting in areaTypeSettings)
        {
            if (typeCounts.ContainsKey(setting.areaType))
                typeCounts[setting.areaType] += setting.count;
            else
                typeCounts[setting.areaType] = setting.count;
        }

        // ルートノードはEntranceに設定
        if (dungeon.root != null && dungeon.root.room != null)
        {
            AreaTypes[dungeon.root.room.id] = AreaType.Entrance;

            if (typeCounts.ContainsKey(AreaType.Entrance))
                typeCounts[AreaType.Entrance]--;
        }
        else
        {
            Debug.Log("Dungeon.root または Dungeon.root.room が null です。");
            return;
        }

        // その他のノードに対してエリアタイプを割り当て
        AssignAreaTypesRecursively(dungeon.root, typeCounts);
    }

    private void AssignAreaTypesRecursively(BSPNode node, Dictionary<AreaType, int> typeCounts)
    {
        if (node.left_child != null && node.right_child != null)
        {
            AssignAreaTypesRecursively(node.left_child, typeCounts);
            AssignAreaTypesRecursively(node.right_child, typeCounts);

            // 親ノードのエリアタイプを取得
            if (!AreaTypes.ContainsKey(node.room.id))
            {
                Debug.LogWarning($"AreaTypes に部屋ID {node.room.id} が存在しません。デフォルトエリアタイプを割り当てます。");
                AreaTypes[node.room.id] = defaultAreaType;
            }

            AreaType parentType = AreaTypes[node.room.id];

            // 子ノードに親のエリアタイプを継承
            if (node.left_child.room != null)
            {
                AreaTypes[node.left_child.room.id] = parentType;
            }

            if (node.right_child.room != null)
            {
                AreaTypes[node.right_child.room.id] = parentType;
            }
        }
        else
        {
            // 子ノードが存在しない場合、エリアタイプをランダムに割り当て
            AreaType assignedType = defaultAreaType;

            // 希望数が残っているエリアタイプからランダムに選択
            var availableTypes = typeCounts.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();
            if (availableTypes.Count > 0)
            {
                assignedType = availableTypes[UnityEngine.Random.Range(0, availableTypes.Count)];
                typeCounts[assignedType]--;
            }

            // エリアタイプがまだ割り当てられていない場合にのみ割り当てる
            if (!AreaTypes.ContainsKey(node.room.id))
            {
                AreaTypes[node.room.id] = assignedType;
            }
        }
    }
}
