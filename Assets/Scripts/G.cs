using UnityEngine;
using System.Collections;

public class G : MonoBehaviour
{

    public static Player Player { set; get; }

    public static Scene Scene { set; get; }
    
    public static Connection Connection { private set; get; }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        Connection = GetComponent<Connection>();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            StartCoroutine(Login());
        }
    }

    private IEnumerator Login()
    {
        var playerId = "1000";
        // 登录
        var request = new Request(0x0101, new MapBean {{"playerId", playerId}});
        yield return Connection.Ask(request);
        var response = request.GetResponse();
        // 进入场景
        var lastPos = (MapBean) response["lastPos"];
        int sceneId = (int) lastPos["sceneId"];
        var x = (int) lastPos["x"] / 100f;
        var z = (int) lastPos["z"] / 100f;
        float angle = (int) lastPos["angle"] / 100f;

        request = new Request(0x0201, new MapBean
        {
            {"sceneId", sceneId}, {"x", (int) (x * 100)}, {"z", (int) (z * 100)}, {"angle", (int) (angle * 100)}
        });
        yield return Connection.Ask(request);
        // 加载场景
        var asyncOp = Application.LoadLevelAsync(sceneId.ToString());

        var go = Instantiate(Resources.Load<GameObject>("Player")) as GameObject;
        if (go) go.AddComponent<Player>();
        DontDestroyOnLoad(go);

        StartCoroutine(EnterOnLoaded(asyncOp, go, x, z, angle));
    }

    private static IEnumerator EnterOnLoaded(AsyncOperation async, GameObject go, float x, float z, float angle)
    {
        while (!async.isDone)
        {
            yield return null;
        }

        Scene scene = Instantiate(Resources.Load<Scene>("Scene")) as Scene;
        scene.Enter(go, x, z, angle);
    }
}