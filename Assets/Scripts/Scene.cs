using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Scene : MonoBehaviour
{
    public MapGrids Grids;

    public FollowTarget Follower;

    private Collider _walkable;

    public int SceneId { get; private set; }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnMouseDown();
        }
    }

    private void Awake()
    {
        var colliders = FindObjectsOfType<Collider>();
        _walkable = colliders.ToList().Find(t => t.name.EndsWith("_phy"));
        string sceneId = Path.GetFileNameWithoutExtension(Application.loadedLevelName);
        TextAsset grids = Resources.Load<TextAsset>("Grids/" + sceneId);
        if (grids)
        {
            var reader = new BinaryReader(new MemoryStream(grids.bytes));
            try
            {
                Grids = new MapGrids(reader, _walkable);
            }
            finally
            {
                reader.Close();
            }
        }

        SceneId = Convert.ToInt32(sceneId);
        Follower.SetTarget(G.Player.Transform);

        G.Scene = this;
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

    public List<MapGrid> FindPath(Vector3 from, Vector3 to)
    {
        var start = Grids.GetMapGrid(from);
        var end = Grids.GetMapGrid(to);
        return new AStarPath(Grids, start, end).Find();
    }
}