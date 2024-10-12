using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// 部屋の種類を割り当てるクラス
public class RoomTypeAssigner
{
    public Dictionary<int, string> RoomTypes { get; private set; }
    private Dungeon dungeon;
    private List<RoomTypeSetting> roomTypeSettings;
    private string defaultRoomType;
    private bool debug;

    public RoomTypeAssigner(Dungeon dungeon, List<RoomTypeSetting> roomTypeSettings, string defaultRoomType, bool debug)
    {
        this.dungeon = dungeon;
        this.roomTypeSettings = roomTypeSettings;
        this.defaultRoomType = defaultRoomType;
        this.debug = debug;
        this.RoomTypes = new Dictionary<int, string>();
    }

    public void AssignRoomTypes()
    {
        int totalRooms = dungeon.rooms.Count;
        HashSet<int> assignedRooms = new HashSet<int>();

        // Start と Boss の部屋を割り当て
        if (dungeon.start_room_idx != -1)
        {
            RoomTypes[dungeon.start_room_idx] = "Start";
            assignedRooms.Add(dungeon.start_room_idx);
        }

        if (dungeon.end_room_idx != -1)
        {
            RoomTypes[dungeon.end_room_idx] = "Boss";
            assignedRooms.Add(dungeon.end_room_idx);
        }

        // 他の部屋タイプを割り当て
        foreach (var setting in roomTypeSettings)
        {
            string roomType = setting.roomType;
            int count = setting.count;

            if (roomType == "Start" || roomType == "Boss")
                continue; // 既に割り当て済み

            if (count == -1)
            {
                // 残りのすべての部屋に割り当て
                foreach (int idx in Enumerable.Range(0, totalRooms))
                {
                    if (!assignedRooms.Contains(idx))
                    {
                        RoomTypes[idx] = roomType;
                        assignedRooms.Add(idx);
                    }
                }
            }
            else if (count > 0)
            {
                // ランダムに指定された数だけ部屋に割り当て
                List<int> availableRooms = Enumerable.Range(0, totalRooms).Where(idx => !assignedRooms.Contains(idx)).ToList();
                if (availableRooms.Count < count)
                    count = availableRooms.Count; // 利用可能な部屋数を超えないよう調整

                List<int> selectedRooms = availableRooms.OrderBy(x => UnityEngine.Random.value).Take(count).ToList();

                foreach (int idx in selectedRooms)
                {
                    RoomTypes[idx] = roomType;
                    assignedRooms.Add(idx);
                }
            }
        }

        // 未割り当ての部屋にデフォルトタイプを割り当て
        for (int i = 0; i < totalRooms; i++)
        {
            if (!RoomTypes.ContainsKey(i))
            {
                RoomTypes[i] = defaultRoomType;
            }
        }

        if (debug)
        {
            foreach (var kvp in RoomTypes)
            {
                Debug.Log($"部屋 {kvp.Key}: {kvp.Value}");
            }
        }
    }
}
