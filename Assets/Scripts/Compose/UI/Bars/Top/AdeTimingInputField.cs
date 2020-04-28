using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Arcade.Gameplay;

namespace Arcade.Compose.UI
{
	[RequireComponent(typeof(InputField))]
	public class AdeTimingInputField : MonoBehaviour, ISelectHandler, IDeselectHandler
	{
		public static AdeTimingInputField Instance { get; private set; }
		private void Start()
		{
			Instance = this;
			timing = GetComponent<InputField>();
			timing.onValueChanged.AddListener(OnValueChange);
		}

		private InputField timing;
		private bool timingSelected;

		private void Update()
		{
			if (!timingSelected) timing.text = ArcGameplayManager.Instance.Timing.ToString();
		}

		public void OnDeselect(BaseEventData eventData)
		{
			timingSelected = false;
		}
		public void OnSelect(BaseEventData eventData)
		{
			timingSelected = true;
		}
		public void OnValueChange(string s)
		{
			if (!timingSelected) return;
			int value = 0;
			if (int.TryParse(s, out value)) ArcGameplayManager.Instance.Timing = value;
		}
	}
}
