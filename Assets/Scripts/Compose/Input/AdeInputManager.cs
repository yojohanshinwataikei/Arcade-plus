using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Arcade.Compose
{

	public partial class AdeInputManager : MonoBehaviour
	{
		public AdeInputControl Controls;
		public AdeInputControl.ArcadeHotkeyActions Hotkeys { get => Controls.ArcadeHotkey; }
		public AdeInputControl.ArcadeInputActions Inputs { get => Controls.ArcadeInput; }
		public EventSystem eventSystem;

		public static AdeInputManager Instance { get; private set; }

		private void Awake()
		{
			if (Controls == null)
			{
				Controls = new AdeInputControl();
			}
			Instance = this;
		}
		public void OnEnable()
		{
			Controls.Enable();
		}

		public void OnDisable()
		{
			Controls.Disable();
		}

		private bool IsFocusingOnTextField()
		{
			InputField inputField = eventSystem.currentSelectedGameObject?.GetComponent<InputField>();
			if (inputField != null)
			{
				if (inputField.isFocused)
				{
					return true;
				}
			}
			return false;
		}

		public bool CheckHotkeyActionPressed(InputAction action)
		{
			if (IsFocusingOnTextField())
			{
				return false;
			}
			return action.WasPressedThisFrame();
		}

		public bool CheckHotkeyActionPressing(InputAction action)
		{
			if (IsFocusingOnTextField())
			{
				return false;
			}
			return action.IsPressed();
		}
	}

}