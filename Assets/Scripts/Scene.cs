using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Serialization;

public class Scene : MonoBehaviour
{
    private MapGrid _grid;

    private Collider _walkable;

    private void Awake()
    {
        var colliders = FindObjectsOfType<Collider>();
        _walkable = colliders.ToList().Find(t => t.name.EndsWith("_phy"));
        var sceneId = Path.GetFileNameWithoutExtension(Application.loadedLevelName);
        var grid = Resources.Load<TextAsset>("Grids/" + sceneId);
        if (!grid) return;
        var reader = new BinaryReader(new MemoryStream(grid.bytes));
        try
        {
            _grid = new MapGrid(reader);
        }
        finally
        {
            reader.Close();
        }

        G.Scene = this;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnMouseDown();
        }

        Camera.main.transform.position = G.Player.transform.position + Offset;
    }


    public void Enter(GameObject actor, float x, float z, float angle)
    {
        float height;
        if (GetHeight(x, z, out height))
        {
            actor.transform.position = new Vector3(x, height, z);
            actor.transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Camera.main.transform.rotation = Quaternion.Euler(Rotation);
        }
    }

    public bool GetHeight(float x, float z, out float y)
    {
        var grid = _grid.GetMapCell(x, z);
        if (grid != null && grid.IsWalkable())
        {
            var from = new Vector3(x, _walkable.bounds.max.y + 1, z);
            var hits = Physics.RaycastAll(from, Vector3.down, Mathf.Infinity, 1 << _walkable.gameObject.layer);
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

    private void OnMouseDown()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hits = Physics.RaycastAll(ray, 1 << _walkable.gameObject.layer);
        var idx = hits.ToList().FindIndex(t => t.collider == _walkable);
        if (idx != -1)
        {
            var target = hits[idx].point;
            G.Player.Movement.MoveToTarget(target);
        }
    }

    public List<Vector3> FindPath(Vector3 from, Vector3 to)
    {
        var start = _grid.GetMapCell(from);
        var end = _grid.GetMapCell(to);
        var cells = new AStarPath(_grid, start, end).Find();
        var path = new List<Vector3> {from};
        for (int i = 1; i < cells.Count - 1; i++)
        {
            var center = cells[i].Center();
            float height;
            if (GetHeight(center.x, center.z, out height))
            {
                center.y = height;
                path.Add(center);
            }
        }

        path.Add(to);
        return path;
    }

    // 位置
    public Vector3 Offset = new Vector3(0, 9f, -7f);

    // 固定视角
    public Vector3 Rotation = new Vector3(45, 0, 0);

    // ==============Gizmos======================

    private List<Vector3> _path;

    private readonly Color[] _colors = {Color.red, Color.blue, Color.green, Color.yellow};

    public void DrawPath(List<Vector3> path)
    {
        _path = new List<Vector3>(path);
    }

    private void OnDrawGizmos()
    {
        DrawCells();
        DrawPath();
    }

    private void DrawPath()
    {
        if (_path != null && _path.Count > 1)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < _path.Count - 1; i++)
            {
                Gizmos.DrawLine(_path[i] + new Vector3(0, 0.5f, 0),
                    _path[i + 1] + new Vector3(0, 0.5f, 0));
            }
        }
    }

    private void DrawCells()
    {
        if (_grid != null)
        {
            for (var z = 0; z < _grid.Columns; z++)
            {
                for (var x = 0; x < _grid.Rows; x++)
                {
                    DrawCell(_grid.Cells[x, z]);
                }
            }
        }
    }

    private void DrawCell(MapCell cell)
    {
        if (cell.Type != GridType.NotReachable)
        {
            Gizmos.color = _colors[Convert.ToInt32(cell.Type)];
            var center = cell.Center();
            float height;
            if (GetHeight(center.x, center.z, out height))
            {
                center.y = height;
                Gizmos.DrawCube(center, new Vector3(_grid.Size, 0.2f, _grid.Size) * 0.8f);
            }

        }
    }
}