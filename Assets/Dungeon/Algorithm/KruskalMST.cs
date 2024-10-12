using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Kruskal'sアルゴリズムクラス
public class KruskalMST
{
    public List<Edge> ComputeMST(int numNodes, List<Edge> edges)
    {
        List<Edge> mst = new List<Edge>();
        edges.Sort(); // エッジを重みの昇順にソート

        UnionFind uf = new UnionFind(numNodes);

        foreach(var edge in edges)
        {
            if(uf.Union(edge.From, edge.To))
            {
                mst.Add(edge);
                if(mst.Count == numNodes - 1)
                    break;
            }
        }

        return mst;
    }
}
