using UnityEngine;
using System.Collections;

public class G : MonoBehaviour
{

    public static Player Player { set; get; }

    public static Scene Scene { set; get; }
    
    public static SceneGrids SceneGrids { set; get; }

    public static GameClient Client { private set; get; }

    // Use this for initialization
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        Client = GetComponent<GameClient>();
    }
}