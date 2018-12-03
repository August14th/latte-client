
public class Fashion : Avatar1
{

    // 套装
    private int _dress;
    
    public void Awake()
    {
        // 其他属性
        ChangePart("Body", "Roles/body_01");
        ChangePart("Hair", "Hair/hair_01");
    }

}