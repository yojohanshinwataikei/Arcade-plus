using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace Arcade.Util.Input
{
	public struct KeyWithModifiersData
	{
		public bool modifier1;
		public bool modifier2;
		public bool modifier3;
		public float keyValue;
	}
#if UNITY_EDITOR
	[InitializeOnLoad] // Automatically register in editor.
#endif
	[DisplayStringFormat("{Key}[{Modifier1}/{Modifier2}/{Modifier3}]")]
	public class KeyWithModifiersComposite : InputBindingComposite<KeyWithModifiersData>
	{
		// Note: Rebinding Composite binding itself do not works, so you should move it to interaction
		[InputControl(layout = "Button")]
		public int key;

		[InputControl(layout = "Button")]
		public int modifier1;

		[InputControl(layout = "Button")]
		public int modifier2;

		[InputControl(layout = "Button")]
		public int modifier3;

		public override KeyWithModifiersData ReadValue(ref InputBindingCompositeContext context)
		{
			return new KeyWithModifiersData{
				modifier1=context.ReadValueAsButton(modifier1),
				modifier2=context.ReadValueAsButton(modifier2),
				modifier3=context.ReadValueAsButton(modifier3),
				keyValue=context.ReadValue<float>(key)
			};
		}

		public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
		{
			return context.EvaluateMagnitude(key);
		}

		static KeyWithModifiersComposite()
		{
			InputSystem.RegisterBindingComposite<KeyWithModifiersComposite>();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Init() { } // Trigger static constructor.
	}
}