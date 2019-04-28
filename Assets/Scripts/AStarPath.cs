using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;


public class AStarPath
{
    private readonly MapGrid _grid;

    private readonly MapCell _to;

    private readonly MapCell _from;

    private readonly List<Node> _list = new List<Node>();

    public AStarPath(MapGrid grid, MapCell from, MapCell to)
    {
        if (!from.IsWalkable() || !to.IsWalkable()) throw new Exception("Start or End point is not reachable.");
        _grid = grid;
        _from = from;
        _to = to;
        int h = Math.Abs(from.Row - to.Row) + Math.Abs(from.Column - to.Column);
        _list.Add(new Node(null, from, 0, h));
    }

    public List<MapCell> Find()
    {
        return Compress(MergePath(Find0()));
    }

    private List<MapCell> Compress(List<MapCell> path)
    {
        var list = new List<MapCell> {path[0]};
        while (list.Last() != path.Last())
        {
            list.Add(path.FindLast(t => DirectTo(list.Last(), t)));
        }

        return list;
    }
    
    private bool DirectTo(MapCell c1, MapCell c2)
    {
        if (c1 == c2) return true;
        var from = c1.Center();
        var to = c2.Center();

        var direction = (to - from).normalized;
        var position = from;
        var cell = c1;
        MapCell prev = null;
        while (cell != c2)
        {
            if (!cell.IsWalkable()) return false;
            if (prev != null && prev.Row != cell.Row && prev.Column != cell.Column)
            {
                if (!_grid.GetMapCell(prev.Row, cell.Column).IsWalkable() ||
                    !_grid.GetMapCell(cell.Row, prev.Column).IsWalkable())
                {
                    return false;
                }
            }

            prev = cell;
            position += direction * _grid.Size;
            cell = _grid.GetMapCell(position);
        }

        return true;
    }

    private List<MapCell> MergePath(List<MapCell> path)
    {
        var grids = path;
        var list = new List<MapCell>();
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

    private List<MapCell> Find0()
    {
        if (_from == _to) return new List<MapCell> {_from, _to};
        var minNode = GetMinNode();
        while (minNode != null)
        {
            minNode.Checked = true;
            foreach (var child in SurroundNodes(minNode))
            {
                if (child.Cell == _to) // find it
                {
                    List<MapCell> path = new List<MapCell>();
                    var node = child;
                    while (node != null)
                    {
                        path.Add(node.Cell);
                        node = node.Parent;
                    }

                    path.Reverse(); // 翻转后输出
                    return path;
                }
                else
                {
                    var node = _list.Find(t => t.Cell == child.Cell);
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
                    var row = node.Cell.Row + r;
                    var column = node.Cell.Column + c;
                    if (row >= 0 && row < _grid.Rows && column >= 0 && column < _grid.Columns)
                    {
                        var nextGrid = _grid.Cells[row, column];
                        if (nextGrid.IsWalkable())
                        {
                            if (Math.Abs(r) == 1 && Math.Abs(c) == 1)
                            {
                                if (!_grid.Cells[row, node.Cell.Column].IsWalkable() ||
                                    !_grid.Cells[node.Cell.Row, column].IsWalkable()) continue;
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
    public Node(Node parent, MapCell cell, int g, int h)
    {
        Parent = parent;
        Cell = cell;
        H = h;
        G = g;
    }

    public Node Parent;

    public readonly MapCell Cell;

    public int G; // 到原点的距离

    public readonly int H; // 到终点的距离

    public bool Checked;

    public int F()
    {
        return G + H;
    }
}