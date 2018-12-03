using System.Collections;
using UnityEngine;

public class Player
{
    public readonly Transform Transform;

    public readonly Movement Movement;

    public readonly Fashion Fashion;

    public readonly string Id;

    public string Name;

    public float Speed = 7f;

    public Player(MapBean data, GameObject go)
    {
        Transform = go.transform;
        Id = data.GetString("playerId");
        Name = data.GetString("playerName");
        go.name = "Player-" + Id;
        // 移动
        Movement = go.AddComponent<Movement>();
        // 展示相关
        Fashion = go.AddComponent<Fashion>();
    }

    public void Play(string animation)
    {
        Fashion.Animator.Play(animation);
    }

    public IEnumerator EnterScene(int sceneId, Vector3 pos, double angle)
    {
        var request = new Request(0x0201, new MapBean
        {
            {"sceneId", sceneId}, {"x", (int) (pos.x * 100)}, {"z", (int) (pos.z * 100)}, {"angle", (int) (angle * 100)}
        });
        yield return G.Client.Ask(request);
        request.GetResponse();
        // 位置
        Transform.position = pos;
        if (G.Scene == null || G.Scene.SceneId != sceneId)
        {
            Application.LoadLevel(sceneId.ToString());
        }
    }
}