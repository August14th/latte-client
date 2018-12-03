using System;
using UnityEngine;
using UnityEditor;
using System.IO;

public class GridsCreator : Editor
{
    [MenuItem("latte/生成地图网格信息")]
    static void createGrids()
    {
        // 输出到文件
        var sceneName = Path.GetFileNameWithoutExtension(EditorApplication.currentScene);
        createGrids("Assets/Resources/Grids/" + sceneName + ".bytes");
    }

    static void createGrids(string toFile)
    {
        var colliders = FindObjectsOfType<Collider>();
        Collider walkable = null;
        Collider safety = null;
        Collider obstacle = null;
        foreach (var collider in colliders)
        {
            if (collider.name.EndsWith("_phy")) walkable = collider;
            if (collider.name.EndsWith("_safety")) safety = collider;
            if (collider.name.EndsWith("_obstacle")) obstacle = collider;
        }

        if (walkable != null)
        {
            var grids = new MapGrids(walkable, safety, obstacle, 1f);
            var fileStream = new FileStream(toFile, FileMode.Create, FileAccess.Write);
            var writer = new BinaryWriter(fileStream);
            grids.WriteTo(writer);
            writer.Close();
        }
        else
        {
            throw new Exception("No walkable collider is found.");
        }
    }
}