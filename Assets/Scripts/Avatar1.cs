using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Avatar1 : MonoBehaviour
{
    //  挂载点和go
    private readonly Dictionary<string, Avatar1Part> _parts = new Dictionary<string, Avatar1Part>();

    public Animator Animator
    {
        get
        {
            return _parts["Body"].Go.GetComponent<Animator>();
        }
    }

    public void ChangePart(string partName, string partPath)
    {
        Transform hang = null;
        switch (partName)
        {
            case "Body":
                hang = transform;
                break;
            case "Hair":
            case "Face":
                var hangs = _parts["Body"].Go.GetComponentsInChildren<Transform>();
                hang = hangs.ToList().Find(h => h.name == "S_Head");
                break;
        }

        var part = Instantiate(Resources.Load<GameObject>(partPath)) as GameObject;
        if (!part) throw new Exception("Resource at " + partPath + " doesn't exist.");
        Avatar1Part old;
        if (_parts.TryGetValue(partName, out old))
        {
            _parts.Remove(partName);
            Destroy(old.Go);
        }

        part.transform.parent = hang;
        ResetTransform(part.transform);
        _parts.Add(partName, new Avatar1Part(partPath, part));

        if (partName == "Body")
        {
            foreach (var p in _parts)
            {
                if (p.Key != "Body") ChangePart(p.Key, p.Value.Path);
            }
        }
    }

    private static void ResetTransform(Transform transform)
    {
        transform.localPosition = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }
}

class Avatar1Part
{
    public Avatar1Part(string path, GameObject go)
    {
        Path = path;
        Go = go;
    }

    public readonly string Path;

    public readonly GameObject Go;
}