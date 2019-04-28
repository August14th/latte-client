using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : Unit
{
    private MoveState _state = new MoveState(0);

    public void Update()
    {
        var forward = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if (forward.magnitude > 0.01f)
        {
            if ((transform.forward - forward.normalized).magnitude > 0.01f || _state.State == 0)
            {
                // 方向有变化
                MoveTowards(forward);
            }
        }
        else if (_state.State == 1)
        {
            StopMoving();
        }
    }

    // 停止移动
    public void StopMoving()
    {
        _state = new MoveState(0);
        Outlook.Play("atstand");

        StartCoroutine(SyncMoving());
    }

    // 朝指定方向移动
    public void MoveTowards(Vector3 forward)
    {
        transform.forward = forward;
        _state = new MoveState(1);
        Outlook.Play("run");
        StartCoroutine(SyncMoving());
    }

    // 自动寻路到目标点
    public void MoveToTarget(Vector3 target)
    {
        var path = G.Scene.FindPath(transform.position, target);
        if (path.Count != 0)
        {
            G.Scene.DrawPath(path);
            path.RemoveAt(0);
            transform.LookAt(path[0]);
            _state = new MoveState(2, path);
            Outlook.Play("run");

            StartCoroutine(SyncMoving());
        }
    }

    private IEnumerator SyncMoving()
    {
        // 同步位置
        var pos = transform.position;
        var fwd = transform.forward;
        var angle = Vector3.Angle(Vector3.forward, new Vector3(fwd.x, 0, fwd.z));
        if (transform.forward.x < 0) angle = -angle;
        var request = new Request(0x0202, new MapBean
        {
            {"x", (int) (pos.x * 100)},
            {"z", (int) (pos.z * 100)},
            {"angle", (int) (angle * 100)},
            {"state", _state.State > 0 ? 1 : 0}
        });
        yield return G.Connection.Ask(request);
        request.GetResponse();
    }

    public void SetPosition(Vector3 newPos)
    {
        float y;
        if (G.Scene.GetHeight(newPos.x, newPos.z, out y))
        {
            transform.position = new Vector3(newPos.x, y, newPos.z);
        }
    }

    public void FixedUpdate()
    {
        switch (_state.State)
        {
            case 0:
                break;
            case 1:
                var proceed = Attr.Speed * Time.fixedDeltaTime * transform.forward;
                var nextPos = transform.position + proceed;
                SetPosition(nextPos);
                break;
            case 2:
                // 自动寻路
                proceed = Attr.Speed * Time.fixedDeltaTime * transform.forward;
                var path = (List<Vector3>) _state.Param;
                if ((path[0] - transform.position).sqrMagnitude < proceed.sqrMagnitude)
                {
                    path.RemoveAt(0);
                    if (path.Count != 0)
                    {
                        transform.LookAt(path[0]);
                    }
                    else
                    {
                        _state = new MoveState(0);
                        Outlook.Play("atstand");
                    }

                    // 每个节点都会同步
                    StartCoroutine(SyncMoving());
                }
                else
                {
                    SetPosition(transform.position + proceed);
                }

                break;
        }
    }
}

internal class MoveState
{
    public readonly int State;

    public readonly object Param;

    public MoveState(int state, object param = null)
    {
        State = state;
        Param = param;
    }
}