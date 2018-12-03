using System.Collections;
using System.Net.Mime;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class Login : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            StartCoroutine(login());
        }
    }

    IEnumerator login()
    {
        string playerId = "10000";
        // 登录
        var request = new Request(0x0101, new MapBean {{"playerId", playerId}});
        yield return G.Client.Ask(request);
        var response = request.GetResponse();
        // 创建玩家对象
        GameObject go = Instantiate(Resources.Load<GameObject>("player")) as GameObject;
        DontDestroyOnLoad(go);
        G.Player = new Player(response, go);
        // 进场景
        var lastPos = (MapBean) response["lastPos"];
        int sceneId = (int) lastPos["sceneId"];
        Vector3 pos = new Vector3((int) lastPos["x"] / 100f, 0, (int) lastPos["z"] / 100f);
        int angle = (int) lastPos["angle"];
        StartCoroutine(G.Player.EnterScene(sceneId, pos, angle));
    }
}