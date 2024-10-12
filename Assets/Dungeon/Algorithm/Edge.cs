using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Edge : IComparable<Edge>
{
    public int From { get; set; }
    public int To { get; set; }
    public double Weight { get; set; }

    public Edge(int from, int to, double weight)
    {
        From = from;
        To = to;
        Weight = weight;
    }

    public int CompareTo(Edge other)
    {
        return Weight.CompareTo(other.Weight);
    }
}
