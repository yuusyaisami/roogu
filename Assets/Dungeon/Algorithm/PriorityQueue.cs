using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    private List<(TElement Element, TPriority Priority)> elements = new List<(TElement, TPriority)>();
    public int Count => elements.Count;
    public void Enqueue(TElement element, TPriority priority)
    {
        elements.Add((element, priority));
        int ci = elements.Count - 1; // 子のインデックス
        while (ci > 0)
        {
            int pi = (ci - 1) / 2; // 親のインデックス
            if (elements[ci].Priority.CompareTo(elements[pi].Priority) >= 0)
                break; // 親より優先度が低い
            var tmp = elements[ci];
            elements[ci] = elements[pi];
            elements[pi] = tmp;
            ci = pi;
        }
    }
    public TElement Dequeue()
    {
        int li = elements.Count - 1; // 最後のインデックス
        var frontItem = elements[0].Element;
        elements[0] = elements[li];
        elements.RemoveAt(li);
        --li;
        int pi = 0; // 親のインデックス
        while (true)
        {
            int ci = pi * 2 + 1; // 左の子
            if (ci > li)
                break;
            int rc = ci + 1; // 右の子
            if (rc <= li && elements[rc].Priority.CompareTo(elements[ci].Priority) < 0)
                ci = rc;
            if (elements[pi].Priority.CompareTo(elements[ci].Priority) <= 0)
                break;
            var tmp = elements[pi];
            elements[pi] = elements[ci];
            elements[ci] = tmp;
            pi = ci;
        }
        return frontItem;
    }
}