using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;

public class FollowTarget : MonoBehaviour
{
    // 位置
    public Vector3 Offset = new Vector3(0, 9f, -7f);

    // 固定视角
    public Vector3 Rotation = new Vector3(45, 0, 0);

    private Transform _target;

    public void SetTarget(Transform target)
    {
        _target = target;
        transform.position = target.position + Offset;
        transform.rotation = Quaternion.Euler(Rotation);
    }

    private void Update()
    {
        if (_target)
        {
            transform.position = _target.position + Offset;
        }
    }
}