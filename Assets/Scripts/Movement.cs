using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
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
        G.Player.Play("atstand");

        StartCoroutine(Sync());
    }

    // 朝指定方向移动
    public void MoveTowards(Vector3 forward)
    {
        transform.forward = forward;
        _state = new MoveState(1);
        G.Player.Play("run");
        StartCoroutine(Sync());
    }

    // 自动寻路到目标点
    public void MoveToTarget(Vector3 target)
    {
        var path = G.Scene.FindPath(transform.position, target);
        if (path.Count != 0)
        {
            path.RemoveAt(0);
            transform.LookAt(path[0].Center());
            G.SceneGrids.SetPath(path);
            _state = new MoveState(2, path);
            G.Player.Play("run");

            StartCoroutine(Sync());
        }
    }

    private IEnumerator Sync()
    {
        // 同步位置
        var pos = transform.position;
        var angle = Vector3.Angle(Vector3.forward, transform.forward);
        if (transform.forward.x < 0) angle = -angle;
        var request = new Request(0x0202, new MapBean
        {
            {"x", (int) (pos.x * 100)},
            {"z", (int) (pos.z * 100)},
            {"angle", (int) (angle * 100)},
            {"state", _state.State > 0 ? 1 : 0}
        });
        yield return G.Client.Ask(request);
        request.GetResponse();
    }

    private void OnLevelWasLoaded(int level)
    {
        SetPosition(transform.position);
    }

    public void SetPosition(Vector3 newPos)
    {
        float y;
        if (G.Scene.Grids.Sample(newPos.x, newPos.z, out y))
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
                var nextPos = transform.position + G.Player.Speed * Time.fixedDeltaTime * transform.forward;
                SetPosition(nextPos);
                break;
            case 2:
                // 自动寻路
                nextPos = transform.position + G.Player.Speed * Time.fixedDeltaTime * transform.forward;
                SetPosition(nextPos);
                var path = (List<MapGrid>) _state.Param;
                if (G.Scene.Grids.GetMapGrid(nextPos) == path[0])
                {
                    path.RemoveAt(0);
                    if (path.Count != 0)
                    {
                        transform.LookAt(path[0].Center());
                    }
                    else
                    {
                        _state = new MoveState(0);
                        G.Player.Play("atstand");
                    }

                    // 每个节点都会同步
                    StartCoroutine(Sync());
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