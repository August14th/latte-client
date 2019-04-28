using System.Collections;
using UnityEngine;

public class Player : Unit
{

    private void Awake()
    {
        // 移动
        gameObject.AddComponent<Movement>();
        // 外观和动画
        gameObject.AddComponent<Outlook>();
        // 属性
        gameObject.AddComponent<Attr>();

        G.Player = this;
    }

}