using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace Arcade.Util.Input
{
	public struct KeyWithModifiersData
	{
		public bool modifierMatched;
		public float keyValue;
	}
#if UNITY_EDITOR
	[InitializeOnLoad] // Automatically register in editor.
#endif
	[DisplayStringFormat("{Key}[{Modifier1}/{Modifier2}/{Modifier3}]")]
	public class KeyWithModifiersComposite : InputBindingComposite<KeyWithModifiersData>
	{
		[InputControl(layout = "Button")]
		public int key;

		[InputControl(layout = "Button")]
		public int modifier1;
		public bool needModifier1;

		[InputControl(layout = "Button")]
		public int modifier2;
		public bool needModifier2;

		[InputControl(layout = "Button")]
		public int modifier3;
		public bool needModifier3;

		public override KeyWithModifiersData ReadValue(ref InputBindingCompositeContext context)
		{
			bool matched1=context.ReadValueAsButton(modifier1) == needModifier1;
			bool matched2=context.ReadValueAsButton(modifier2) == needModifier2;
			bool matched3=context.ReadValueAsButton(modifier3) == needModifier3;
			return new KeyWithModifiersData{
				modifierMatched=matched1 && matched2 && matched3,
				keyValue=context.ReadValue<float>(key)
			};
		}

		public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
		{
			bool matched1=context.ReadValueAsButton(modifier1) == needModifier1;
			bool matched2=context.ReadValueAsButton(modifier2) == needModifier2;
			bool matched3=context.ReadValueAsButton(modifier3) == needModifier3;
			if(matched1 && matched2 && matched3){
				return context.EvaluateMagnitude(key);
			}
			return 0;
		}

		static KeyWithModifiersComposite()
		{
			InputSystem.RegisterBindingComposite<KeyWithModifiersComposite>();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Init() { } // Trigger static constructor.
	}
}