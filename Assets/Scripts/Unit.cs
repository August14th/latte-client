using UnityEngine;

public class Unit : MonoBehaviour
{
    public Movement Movement
    {
        get { return GetComponent<Movement>(); }
    }

    protected Outlook Outlook
    {
        get { return GetComponent<Outlook>(); }
    }

    protected Attr Attr
    {
        get { return GetComponent<Attr>(); }
    }
}