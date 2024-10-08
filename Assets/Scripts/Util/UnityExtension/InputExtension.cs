using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcade.Util.UnityExtension
{
	public static class InputExtension
	{
		public static Vector2 scaledMousePosition
		{
			get
			{
				return ScaledMousePosition(new Vector2(1920, 1080));
			}
		}
		public static Vector2 ScaledMousePosition(Vector2 size)
		{
			float ratio = size.x / Screen.width;
			Vector2 mousePosition = Mouse.current.position.ReadValue();
			return mousePosition * ratio;
		}
	}
}
