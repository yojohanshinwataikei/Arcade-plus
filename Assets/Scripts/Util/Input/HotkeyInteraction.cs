

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Arcade.Util.Input
{
#if UNITY_EDITOR
	[InitializeOnLoad]
#endif
	// Note: since interaction can not change magnitude, please use action.inProgress instead of action.isPressed
	public class HotKeyInteraction : IInputInteraction<KeyWithModifiersData>
	{
		public bool needModifier1;
		public bool needModifier2;
		public bool needModifier3;

		private bool pressing = false;
		public void Process(ref InputInteractionContext context)
		{

			KeyWithModifiersData data = context.ReadValue<KeyWithModifiersData>();
			bool modifierMatched = (needModifier1 == data.modifier1) && (needModifier2 == data.modifier2) && (needModifier3 == data.modifier3);
			if (pressing)
			{
				if (data.keyValue < InputSystem.settings.defaultButtonPressPoint)
				{
					pressing = false;
					context.Canceled();
				}
			}
			else if (modifierMatched)
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