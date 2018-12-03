using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Avatar0 : MonoBehaviour
{
    private readonly Dictionary<string, Avatar0Part> _parts = new Dictionary<string, Avatar0Part>();

    public void ChangePart(string partName, string partPath)
    {
        var meshes = new List<CombineInstance>();
        var materials = new List<Material>();
        var bones = new List<Transform>();
        var part = Instantiate(Resources.Load<GameObject>(partPath)) as GameObject;
        if (!part) throw new Exception(partName + " at " + partPath + " doesn't exist.");
        try
        {
            var allBones = gameObject.GetComponentsInChildren<Transform>().ToList();
            SkinnedMeshRenderer rend = part.GetComponentInChildren<SkinnedMeshRenderer>();
            // 网格
            for (var i = 0; i < rend.sharedMesh.subMeshCount; i++)
            {
                CombineInstance mesh = new CombineInstance();
                mesh.mesh = rend.sharedMesh;
                mesh.subMeshIndex = i;
                meshes.Add(mesh);
            }

            // 材质
            materials.AddRange(rend.sharedMaterials);
            // 骨骼
            foreach (var bone in rend.bones)
            {
                var temp = allBones.Find(b => b.name == bone.name);
                if (temp) bones.Add(temp);
                else throw new Exception(bone.name + " doesn't exist in skeleton.");
            }

            if (_parts.ContainsKey(partName)) _parts.Remove(partName);
            _parts.Add(partName, new Avatar0Part(partPath, meshes.ToArray(), materials.ToArray(), bones.ToArray()));
        }
        finally
        {
            Destroy(part);
        }

        Apply();
    }

    private void Apply()
    {
        var skin = gameObject.GetComponent<SkinnedMeshRenderer>();
        if (!skin) skin = gameObject.AddComponent<SkinnedMeshRenderer>();
        var meshes = new List<CombineInstance>();
        var materials = new List<Material>();
        var bones = new List<Transform>();
        foreach (Avatar0Part part in _parts.Values)
        {
            meshes.AddRange(part.Meshes);
            materials.AddRange(part.Materials);
            bones.AddRange(part.Bones);
        }

        skin.bones = bones.ToArray();
        skin.sharedMesh = new Mesh();
        skin.sharedMesh.CombineMeshes(meshes.ToArray(), false, false);
        skin.sharedMaterials = materials.ToArray();
    }
}

class Avatar0Part
{
    public Avatar0Part(string path, CombineInstance[] meshes, Material[] materials, Transform[] bones)
    {
        Path = path;
        Meshes = meshes;
        Materials = materials;
        Bones = bones;
    }

    public readonly string Path;

    public readonly Transform[] Bones;

    public readonly Material[] Materials;

    public readonly CombineInstance[] Meshes;
}