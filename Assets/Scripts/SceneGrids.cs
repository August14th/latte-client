using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SceneGrids : MonoBehaviour
{
    private MapGrids _grids;

    private List<MapGrid> _path;

    private readonly Color[] _colors = {Color.red, Color.blue, Color.green, Color.yellow};

    private void Awake()
    {
        string scene = Path.GetFileNameWithoutExtension(Application.loadedLevelName);
        TextAsset grids = Resources.Load<TextAsset>("Grids/" + scene);
        if (grids)
        {
            var colliders = FindObjectsOfType<Collider>();
            Collider walkable = colliders.ToList().Find(t => t.name.EndsWith("_phy"));
            var reader = new BinaryReader(new MemoryStream(grids.bytes));
            _grids = new MapGrids(reader, walkable);
            reader.Close();
        }

        G.SceneGrids = this;
    }

    public void SetPath(List<MapGrid> path)
    {
        _path = new List<MapGrid>(path.ToArray());
    }

    public void OnDrawGizmos()
    {
        DrawGrids();

        DrawPath();
    }

    private void DrawPath()
    {
        if (_path.Count > 1)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < _path.Count - 1; i++)
            {
                Gizmos.DrawLine(_path[i].Center() + new Vector3(0, 0.5f, 0),
                    _path[i + 1].Center() + new Vector3(0, 0.5f, 0));
            }
        }
    }

    private void DrawGrids()
    {
        if (_grids != null)
        {
            for (var z = 0; z < _grids.Columns; z++)
            {
                for (var x = 0; x < _grids.Rows; x++)
                {
                    DrawGrid(_grids.Grids[x, z]);
                }
            }
        }
        
        
    }

    private void DrawGrid(MapGrid grid)
    {
        if (grid.Type != GridType.NotReachable)
        {
            Gizmos.color = _colors[Convert.ToInt32(grid.Type)];
            var center = grid.Center();
            Gizmos.DrawCube(center, new Vector3(_grids.Size, 0.2f, _grids.Size) * 0.8f);
        }
    }
}