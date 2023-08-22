using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;
using System.Collections.Generic;

namespace Arcade.Compose
{

	public class AdeInputManager : MonoBehaviour
	{
		public AdeInputControl Controls;
		public AdeInputControl.ArcadeHotkeyActions Hotkeys { get => Controls.ArcadeHotkey; }
		public AdeInputControl.ArcadeInputActions Inputs { get => Controls.ArcadeInput; }
		public EventSystem EventSystem;
		public AdeHotkeyDialog Dialog;

		public static AdeInputManager Instance { get; private set; }

		private bool rebindFinishedThisFrame = false;

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
			var rebinds = PlayerPrefs.GetString("hotkeyRebinding");
			Debug.Log($"hotkeyRebinding:{rebinds}");
			Hotkeys.Get().LoadBindingOverridesFromJson(rebinds);
			Controls.Enable();
		}

		public void OnDisable()
		{
			Controls.Disable();
		}

		private bool IsFocusingOnTextField()
		{
			GameObject selected=EventSystem.currentSelectedGameObject;
			if(selected == null){
				return false;
			}
			InputField inputField = EventSystem.currentSelectedGameObject.GetComponent<InputField>();
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
			if (IsFocusingOnTextField() || rebindFinishedThisFrame)
			{
				return false;
			}
			return action.WasPerformedThisFrame();
		}

		public bool CheckHotkeyActionPressing(InputAction action)
		{
			if (IsFocusingOnTextField() || rebindFinishedThisFrame)
			{
				return false;
			}
			return action.inProgress;
		}

		private AdeHotkeyRebindingButton rebindingButton = null;
		private RebindingOperation rebindingOperation = null;

		public AdeHotkeyRebindingButton CurrentRebindingButton
		{
			get => rebindingButton;
		}

		public void SetHotkeyRebindingButton(AdeHotkeyRebindingButton button)
		{
			// clean focus so will won't restart rebinding after complete
			EventSystem.SetSelectedGameObject(null);
			AdeHotkeyRebindingButton nextRebindingButton = button;
			InputAction action = null;
			if (button != null)
			{
				action = Hotkeys.Get().FindAction(button.HotkeyName);
				if (action == null)
				{
					Debug.LogError($"{button.HotkeyName} is not a known hotkey");
					nextRebindingButton = null;
				}
			};
			// cleanup old operation
			if (rebindingOperation != null)
			{
				rebindingOperation.Cancel();
			}
			// start new operation
			if (nextRebindingButton != null)
			{
				Hotkeys.Disable();
				rebindingOperation = action.PerformInteractiveRebinding()
					.WithExpectedControlType<ButtonControl>()
					.WithControlsHavingToMatchPath("<Keyboard>")
					.WithControlsExcluding("<Keyboard>/leftAltKey")
					.WithControlsExcluding("<Keyboard>/leftCtrlKey")
					.WithControlsExcluding("<Keyboard>/leftShiftKey")
					.WithControlsExcluding("<Keyboard>/rightAltKey")
					.WithControlsExcluding("<Keyboard>/rightCtrlKey")
					.WithControlsExcluding("<Keyboard>/rightShiftKey")
					.WithControlsExcluding("<Keyboard>/altKey")
					.WithControlsExcluding("<Keyboard>/ctrlKey")
					.WithControlsExcluding("<Keyboard>/shiftKey")
					.WithControlsExcluding("<Keyboard>/anyKey")
					.WithControlsExcluding("<Keyboard>/escapeKey")
					.WithControlsExcluding("<Pointer>")
					.WithCancelingThrough(Keyboard.current.escapeKey)
					.WithMatchingEventsBeingSuppressed(true)
					.OnMatchWaitForAnother(float.PositiveInfinity)
					.OnPotentialMatch((operation) =>
					{
						if (operation.candidates.Count > 0)
						{
							operation.Complete();
						}
					})
					.OnApplyBinding((operation, bindingPath) =>
					{
						if (action.bindings.Count != 5)
						{
							Debug.LogError("The action for the rebinding button is not hotkey: not a hotkey binding");
							return;
						}
						if (!action.bindings[0].isComposite)
						{
							Debug.LogError("The action for the rebinding button is not hotkey: not a hotkey binding");
							return;
						}
						if (action.bindings[0].GetNameOfComposite() != "KeyWithModifiers")
						{
							Debug.LogError("The action for the rebinding button is not hotkey: not a hotkey binding");
							return;
						}
						bool hasCtrl = Keyboard.current.ctrlKey.isPressed;
						bool hasAlt = Keyboard.current.altKey.isPressed;
						bool hasShift = Keyboard.current.shiftKey.isPressed;
						bool? needModifier1 = null;
						bool? needModifier2 = null;
						bool? needModifier3 = null;
						for (int i = 1; i < 5; i++)
						{
							InputBinding binding = action.bindings[i];
							if (binding.name == "key")
							{
								action.ApplyBindingOverride(i, bindingPath);
								continue;
							}
							bool? needModifier = null;
							if (binding.effectivePath == "<Keyboard>/ctrl")
							{
								needModifier = hasCtrl;
							}
							else if (binding.effectivePath == "<Keyboard>/alt")
							{
								needModifier = hasAlt;
							}
							else if (binding.effectivePath == "<Keyboard>/shift")
							{
								needModifier = hasShift;
							}
							if (binding.name == "modifier1")
							{
								needModifier1 = needModifier;
							}
							else if (binding.name == "modifier2")
							{
								needModifier2 = needModifier;
							}
							else if (binding.name == "modifier3")
							{
								needModifier3 = needModifier;
							}
							else
							{
								Debug.LogError("The action for the rebinding button is not hotkey: unknown binding name");
								return;
							}
						}
						if (needModifier1 == null || needModifier2 == null || needModifier3 == null)
						{
							Debug.LogError("The action for the rebinding button is not hotkey: missing binding component");
							return;
						}
						List<string> param = new List<string>();
						param.Add($"needModifier1={needModifier1.Value.ToString().ToLower()}");
						param.Add($"needModifier2={needModifier2.Value.ToString().ToLower()}");
						param.Add($"needModifier3={needModifier3.Value.ToString().ToLower()}");
						InputBinding compositeBinding = action.bindings[0];
						// Note: LoadBindingOverridesFromJson will apply empty string
						// and SaveBindingOverridesAsJson will save null to empty string
						// so as a workaround here we set the overridePath to path
						compositeBinding.overridePath = compositeBinding.path;
						compositeBinding.overrideInteractions = $"HotKey({string.Join(",", param)})";
						action.ApplyBindingOverride(0, compositeBinding);
					})
					.OnComplete((operation) =>
					{
						CleanupCurrentOperation();
						PlayerPrefs.SetString("hotkeyRebinding", Hotkeys.Get().SaveBindingOverridesAsJson());
					})
					.OnCancel((operation) =>
					{
						CleanupCurrentOperation();
					}).Start();
				rebindingButton = nextRebindingButton;
			}
			UpdateTextForHotkeyButton(button);
		}

		private void CleanupCurrentOperation()
		{
			rebindingOperation.Dispose();
			rebindingOperation = null;
			AdeHotkeyRebindingButton lastButton = rebindingButton;
			rebindingButton = null;
			UpdateTextForHotkeyButton(lastButton);
			Hotkeys.Enable();
			rebindFinishedThisFrame = true;
		}

		public void UpdateTextForHotkeyButton(AdeHotkeyRebindingButton button)
		{
			if (button == null)
			{
				return;
			}
			if (button == rebindingButton)
			{
				string text = "<?>";
				if (Keyboard.current.shiftKey.isPressed)
				{
					text = "Shift+" + text;
				}
				if (Keyboard.current.altKey.isPressed)
				{
					text = "Alt+" + text;
				}
				if (Keyboard.current.ctrlKey.isPressed)
				{
					text = "Ctrl+" + text;
				}
				button.Text.text = text;
				return;
			}
			InputAction action = Hotkeys.Get().FindAction(button.HotkeyName);
			if (action == null)
			{
				Debug.LogError($"{button.HotkeyName} is not a known hotkey");
				button.Text.text = "<N/A>";
				return;
			}
			if (action.bindings.Count != 5)
			{
				Debug.LogError("The action for the rebinding button is not hotkey: not a hotkey binding");
				return;
			}
			if (!action.bindings[0].isComposite)
			{
				Debug.LogError("The action for the rebinding button is not hotkey: not a hotkey binding");
				return;
			}
			if (action.bindings[0].GetNameOfComposite() != "KeyWithModifiers")
			{
				Debug.LogError("The action for the rebinding button is not hotkey: not a hotkey binding");
				return;
			}
			button.Text.text = "<Error>";
			bool needModifier1 = false;
			bool needModifier2 = false;
			bool needModifier3 = false;
			string interaction = action.bindings[0].effectiveInteractions;
			// Note: this depend on conventions on the effectivePath format
			if (interaction.Contains('('))
			{
				int start = interaction.IndexOf('(');
				int end = interaction.IndexOf(')');
				string param = interaction.Substring(start + 1, end - start - 1);
				foreach (string keyValue in param.Split(","))
				{
					int separator = keyValue.IndexOf('=');
					string paramKey = keyValue.Substring(0, separator);
					string paramValue = keyValue.Substring(separator + 1);
					if (paramKey == "needModifier1")
					{
						needModifier1 = paramValue == "true";
					}
					else if (paramKey == "needModifier2")
					{
						needModifier2 = paramValue == "true";
					}
					else if (paramKey == "needModifier3")
					{
						needModifier3 = paramValue == "true";
					}
				}
			}
			bool? hasCtrl = null;
			bool? hasAlt = null;
			bool? hasShift = null;
			string key = null;
			for (int i = 1; i < 5; i++)
			{
				InputBinding binding = action.bindings[i];
				if (binding.name == "key")
				{
					key = binding.ToDisplayString();
					continue;
				}
				bool? needModifier = null;
				if (binding.name == "modifier1")
				{
					needModifier = needModifier1;
				}
				else if (binding.name == "modifier2")
				{
					needModifier = needModifier2;
				}
				else if (binding.name == "modifier3")
				{
					needModifier = needModifier3;
				}
				if (needModifier == null)
				{
					Debug.LogError("The action for the rebinding button is not hotkey: unknown binding name");
					return;
				}
				if (binding.effectivePath == "<Keyboard>/ctrl")
				{
					hasCtrl = needModifier;
				}
				else if (binding.effectivePath == "<Keyboard>/alt")
				{
					hasAlt = needModifier;
				}
				else if (binding.effectivePath == "<Keyboard>/shift")
				{
					hasShift = needModifier;
				}
			}
			if (hasCtrl == null || hasAlt == null || hasShift == null || key == null)
			{
				Debug.LogError("The action for the rebinding button is not hotkey: missing binding component");
				return;
			}
			string result = key;
			if (hasShift == true)
			{
				result = "Shift+" + result;
			}
			if (hasAlt == true)
			{
				result = "Alt+" + result;
			}
			if (hasCtrl == true)
			{
				result = "Ctrl+" + result;
			}
			button.Text.text = result;
		}

		public void ResetHotkey()
		{
			SetHotkeyRebindingButton(null);
			Hotkeys.Get().RemoveAllBindingOverrides();
			PlayerPrefs.SetString("hotkeyRebinding", Hotkeys.Get().SaveBindingOverridesAsJson());
			foreach (var button in Dialog.RebindingButtons)
			{
				AdeInputManager.Instance.UpdateTextForHotkeyButton(button);
			}
		}

		private void Update()
		{
			if (rebindingButton != null)
			{
				UpdateTextForHotkeyButton(rebindingButton);
				if(Inputs.Cancel.IsPressed()){
					rebindingOperation.Cancel();
				}
			}else{
				rebindFinishedThisFrame = false;
			}
		}

	}

}