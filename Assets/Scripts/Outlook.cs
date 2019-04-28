
public class Outlook : Avatar1
{

    public void Awake()
    {
        // 其他属性
        ChangePart("Body", "Roles/body_01");
        ChangePart("Hair", "Hair/hair_01");
    }

    public void Play(string action)
    {
        Animator.Play(action);
    }

}