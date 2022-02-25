

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Arcade.Util.Input
{
#if UNITY_EDITOR
	[InitializeOnLoad]
#endif
	public class HotKeyInteraction : IInputInteraction<KeyWithModifiersData>
	{
		private bool pressing = false;
		public void Process(ref InputInteractionContext context)
		{
			KeyWithModifiersData data = context.ReadValue<KeyWithModifiersData>();
			if (pressing)
			{
				if (data.keyValue < InputSystem.settings.defaultButtonPressPoint)
				{
					pressing = false;
					context.Canceled();
				}
			}
			else if (data.modifierMatched)
			{
				if (data.keyValue >= InputSystem.settings.defaultButtonPressPoint)
				{
					pressing = true;
					context.PerformedAndStayPerformed();
				}
				else if (data.keyValue > 0 && !context.isStarted)
				{
					context.Started();
				}
				else
				{
					context.Waiting();
				}
			}
			else
			{
				if (data.keyValue >= InputSystem.settings.defaultButtonPressPoint)
				{
					pressing = true;
				}
				context.Waiting();
			}
		}

		public void Reset()
		{
			pressing = false;
		}

		static HotKeyInteraction()
		{
			InputSystem.RegisterInteraction<HotKeyInteraction>();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize()
		{
			// Will execute the static constructor as a side effect.
		}
	}
}