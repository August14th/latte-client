using UnityEngine;

public class JoySticker : MonoBehaviour
{

	public UISprite sprite;
	
	private Vector2 center;

	public void OnPress(bool press)
	{
		if (press)
		{
			center = convertScreenToNGUI(UICamera.currentTouch.pos);
		}
		else
		{
			center = Vector2.zero;
		}
		sprite.transform.localPosition = center;
	}

	public void OnDrag(Vector2 delta)
	{
		float radius = 100f;
		Vector2 pos = convertScreenToNGUI(UICamera.currentTouch.pos);
		float distance = Vector2.Distance(pos, center);
		if ( distance > radius)
		{
			pos = center + (pos - center) / distance * radius;
		}
		sprite.transform.localPosition = pos;
	}

	private Vector2 convertScreenToNGUI(Vector2 screenPos)
	{
		UIRoot root = FindObjectOfType<UIRoot>();
		if (root != null)
		{
			float scale = (float) root.activeHeight / Screen.height;
			Vector2 pos = (screenPos - new Vector2(Screen.width / 2, Screen.height / 2)) * scale;
			return pos;
		}
		else
		{
			return Vector2.zero;
		}
		
	}
	
}
