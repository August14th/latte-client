using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;


public class AStarPath
{
    private readonly MapGrids _grids;

    private readonly MapGrid _to;

    private readonly MapGrid _from;

    private readonly List<Node> _list = new List<Node>();

    public AStarPath(MapGrids grids, MapGrid from, MapGrid to)
    {
        if (!from.IsWalkable() || !to.IsWalkable()) throw new Exception("Start or End point is not reachable.");
        _grids = grids;
        _from = from;
        _to = to;
        int h = Math.Abs(from.Row - to.Row) + Math.Abs(from.Column - to.Column);
        _list.Add(new Node(null, from, 0, h));
    }

    public List<MapGrid> Find()
    {
        return Compress(MergePath(Find0()));
    }

    private List<MapGrid> Compress(List<MapGrid> path)
    {
        var list = new List<MapGrid> {path[0]};
        while (list[list.Count - 1] != path[path.Count - 1])
        {
            list.Add(path.FindLast(t => IsNotStop(list[list.Count - 1], t)));
        }

        return list;
    }

    private bool IsNotStop(MapGrid g1, MapGrid g2)
    {
        if (g1 == g2) return true;
        var from = g1.Center();
        var to = g2.Center();

        var direction = (to - from).normalized;
        var position = from;
        var grid = g1;
        MapGrid prev = null;
        while (grid != g2)
        {
            if (!grid.IsWalkable()) return false;
            if (prev != null && prev.Row != grid.Row && prev.Column != grid.Column)
            {
                if (!_grids.GetMapGrid(prev.Row, grid.Column).IsWalkable() ||
                    !_grids.GetMapGrid(grid.Row, prev.Column).IsWalkable())
                {
                    return false;
                }
            }

            prev = grid;
            position += direction * _grids.Size;
            grid = _grids.GetMapGrid(position);
        }

        return true;
    }

    private List<MapGrid> MergePath(List<MapGrid> path)
    {
        var grids = path;
        var list = new List<MapGrid>();
        var z = 0;
        var x = 0;

        while (grids.Count > 1)
        {
            var zz = grids[1].Row - grids[0].Row;
            var xx = grids[1].Column - grids[0].Column;
            if (list.Count == 0 || z != zz || x != xx)
            {
                list.Add(grids[0]);
                z = zz;
                x = xx;
            }

            grids.RemoveAt(0);
        }

        list.Add(grids[0]);
        return list;
    }

    private List<MapGrid> Find0()
    {
        if (_from == _to) return new List<MapGrid> {_from, _to};
        var minNode = GetMinNode();
        while (minNode != null)
        {
            minNode.Checked = true;
            foreach (var child in SurroundNodes(minNode))
            {
                if (child.Grid == _to) // find it
                {
                    List<MapGrid> path = new List<MapGrid>();
                    var node = child;
                    while (node != null)
                    {
                        path.Add(node.Grid);
                        node = node.Parent;
                    }

                    path.Reverse(); // 翻转后输出
                    return path;
                }
                else
                {
                    var node = _list.Find(t => t.Grid == child.Grid);
                    if (node == null)
                    {
                        _list.Add(child);
                    }
                    else if (!node.Checked && node.G > child.G)
                    {
                        node.Parent = child.Parent;
                        node.G = child.G;
                    }
                }
            }

            minNode = GetMinNode();
        }

        throw new Exception("Path not found.");
    }

    private Node GetMinNode()
    {
        Node min = null;
        foreach (var node in _list.FindAll(t => !t.Checked))
        {
            if (min == null) min = node;
            else
            {
                if (min.F() > node.F()) min = node;
            }
        }

        return min;
    }

    private Node[] SurroundNodes(Node node)
    {
        List<Node> surroundings = new List<Node>();
        for (int r = -1; r <= 1; r++)
        {
            for (int c = -1; c <= 1; c++)
            {
                if (r != 0 || c != 0)
                {
                    var row = node.Grid.Row + r;
                    var column = node.Grid.Column + c;
                    if (row >= 0 && row < _grids.Rows && column >= 0 && column < _grids.Columns)
                    {
                        var nextGrid = _grids.Grids[row, column];
                        if (nextGrid.IsWalkable())
                        {
                            if (Math.Abs(r) == 1 && Math.Abs(c) == 1)
                            {
                                if (!_grids.Grids[row, node.Grid.Column].IsWalkable() ||
                                    !_grids.Grids[node.Grid.Row, column].IsWalkable()) continue;
                            }

                            var g = node.G + 10;
                            if (Math.Abs(r) == 1 && Math.Abs(c) == 1) g = node.G + 14;
                            int h = Math.Abs(nextGrid.Row - _to.Row) + Math.Abs(nextGrid.Column - _to.Column);
                            surroundings.Add(new Node(node, nextGrid, g, h));
                        }
                    }
                }
            }
        }

        return surroundings.ToArray();
    }
}

public class Node
{
    public Node(Node parent, MapGrid grid, int g, int h)
    {
        Parent = parent;
        Grid = grid;
        H = h;
        G = g;
    }

    public Node Parent;

    public readonly MapGrid Grid;

    public int G; // 到原点的距离

    public readonly int H; // 到终点的距离

    public bool Checked;

    public int F()
    {
        return G + H;
    }
}