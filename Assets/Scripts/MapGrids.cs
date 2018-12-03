using System;
using System.IO;
using System.Linq;
using UnityEngine;

public class MapGrids
{
    public readonly float StartX;

    public readonly float StartZ;

    public readonly float Size;

    public readonly int Rows;

    public readonly int Columns;

    public readonly MapGrid[,] Grids;

    private readonly Collider _walkable;

    public MapGrids(BinaryReader reader, Collider walkable)
    {
        _walkable = walkable;
        StartX = reader.ReadInt32() / 100f;
        StartZ = reader.ReadInt32() / 100f;

        Size = reader.ReadInt32() / 100f;

        Rows = reader.ReadInt32();
        Columns = reader.ReadInt32();

        Grids = new MapGrid[Rows, Columns];

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                var type = (GridType) reader.ReadByte();
                Grids[row, column] = new MapGrid(this, row, column, type);
            }
        }

        // 设置区域
        SetAreas();
    }

    public MapGrids(Collider walkable, Collider safety, Collider obstacle, float size)
    {
        var bounds = walkable.bounds;
        StartX = bounds.min.x;
        StartZ = bounds.min.z;
        Rows = Convert.ToInt32(bounds.size.z / size); // 行有高度决定
        Columns = Convert.ToInt32(bounds.size.x / size); // 列有宽度决定
        Size = size;

        Grids = new MapGrid[Rows, Columns];
        // 设置grid类型
        for (var row = 0; row < Rows; row += 1)
        {
            for (var column = 0; column < Columns; column += 1)
            {
                var point = new Vector3(StartX + column * size, bounds.max.y + 1, StartZ + row * size); // 左下角为锚点
                var leftBottom = getPointType(point + new Vector3(0, 0, 0), walkable, safety, obstacle);
                var leftTop = getPointType(point + new Vector3(0, 0, size), walkable, safety, obstacle);
                var rightBottom = getPointType(point + new Vector3(size, 0, 0), walkable, safety, obstacle);
                var rightTop = getPointType(point + new Vector3(size, 0, size), walkable, safety, obstacle);
                var gridType = (GridType) Math.Min(Math.Min((int) leftBottom, (int) leftTop),
                    Math.Min((int) rightBottom, (int) rightTop));
                Grids[row, column] = new MapGrid(this, row, column, gridType);
            }
        }
        // 设置区域id
        SetAreas();
    }

    private void SetAreas()
    {
        // 设置grid所属的区域
        int areaId = 1;
        for (var row = 0; row < Rows; row += 1)
        {
            for (var column = 0; column < Columns; column += 1)
            {
                if (SetGridArea(areaId, row, column))
                {
                    areaId += 1;
                }
            }
        }
    }

    private bool SetGridArea(int areaId, int row, int column)
    {
        if (row < 0 || column < 0 || row >= Rows || column >= Columns) return false;
        var grid = Grids[row, column];
        if (grid.Type != GridType.NotReachable && grid.AreaId == 0)
        {
            grid.AreaId = areaId;
            for (var i = -1; i <= 1; i++)
            {
                for (var j = -1; j <= 1; j++)
                {
                    if (i != 0 || j != 0)
                    {
                        SetGridArea(areaId, row + i, column + j);
                    }
                }
            }

            return true;
        }

        return false;
    }

    private GridType getPointType(Vector3 point, Collider walkable, Collider safety, Collider obstacle)
    {
        var layerMask = 1 << walkable.gameObject.layer;
        if (safety != null) layerMask = layerMask | 1 << safety.gameObject.layer;
        if (obstacle != null) layerMask = layerMask | 1 << obstacle.gameObject.layer;
        var hits = Physics.RaycastAll(point, Vector3.down, Mathf.Infinity, layerMask);
        GridType type = GridType.NotReachable;
        foreach (var hit in hits)
        {
            if (hit.collider == obstacle)
                if (type < GridType.Obstacle)
                    type = GridType.Obstacle;
            if (hit.collider == safety)
                if (type < GridType.Safety)
                    type = GridType.Safety;
            if (hit.collider == walkable)
                if (type < GridType.Walkable)
                    type = GridType.Walkable;
        }

        return type;
    }

    public bool Sample(float x, float z, out float y)
    {
        var grid = GetMapGrid(x, z);
        if (grid != null && grid.IsWalkable())
        {
            var origin = new Vector3(x, _walkable.bounds.max.y + 1, z);
            var hits = Physics.RaycastAll(origin, Vector3.down, Mathf.Infinity, 1 << _walkable.gameObject.layer);
            var idx = hits.ToList().FindIndex(t => t.collider == _walkable);
            if (idx != -1)
            {
                y = hits[idx].point.y;
                return true;
            }
        }
        y = 0;
        return false;
    }

    public MapGrid GetMapGrid(int row, int column)
    {
        if (row >= 0 && row < Rows && column >= 0 && column < Columns)
        {
            return Grids[row, column];
        }

        return null;
    }

    public MapGrid GetMapGrid(Vector3 v)
    {
        return GetMapGrid(v.x, v.z);
    }

    public MapGrid GetMapGrid(float x, float z)
    {
        int row = (int) Math.Floor((z - StartZ) / Size);
        int column = (int) Math.Floor((x - StartX) / Size);

        if (row >= 0 && row < Rows && column >= 0 && column < Columns)
        {
            return Grids[row, column];
        }

        return null;
    }

    public void WriteTo(BinaryWriter writer)
    {
        writer.Write((int) (StartX * 100));
        writer.Write((int) (StartZ * 100));

        writer.Write((int) (Size * 100));

        writer.Write(Rows);
        writer.Write(Columns);

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                var grid = Grids[row, column];
                // 类型
                writer.Write(Convert.ToByte(grid.Type));
            }
        }
    }
    
    
    
}

public enum GridType
{
    NotReachable = 0, // 不可达
    Walkable = 1, // 行走层
    Safety = 2, // 安全区
    Obstacle = 3 // 障碍物
}

public class MapGrid
{
    private readonly MapGrids _grids;

    private readonly int _row;

    private readonly int _column;

    public readonly GridType Type;

    private int _areaId;

    public bool IsWalkable()
    {
        return Type == GridType.Walkable || Type == GridType.Safety;
    }

    public int Row
    {
        get { return _row; }
    }

    public int Column
    {
        get { return _column; }
    }

    public int AreaId
    {
        set
        {
            if (value > 0x7f) throw new Exception("high areaId: " + value);
            _areaId = value;
        }
        get { return _areaId; }
    }

    public Vector3 Center()
    {
        var halfSize = _grids.Size / 2f;
        var x = _grids.StartX + _column * _grids.Size + halfSize;
        var z = _grids.StartZ + _row * _grids.Size + halfSize;
        float y;
        _grids.Sample(x, z, out y);
        return new Vector3(x, y, z);
    }

    public MapGrid(MapGrids grids, int row, int column, GridType type)
    {
        _grids = grids;
        _row = row;
        _column = column;
        Type = type;
    }
}