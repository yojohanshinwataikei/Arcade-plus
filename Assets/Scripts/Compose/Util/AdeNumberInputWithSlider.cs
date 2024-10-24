using Arcade.Compose;
using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Compose.UI
{
	public class AdeNumberInputWithSlider : MonoBehaviour
	{
		[SerializeField]
		private float value;
		public float Value
		{
			get => value;
			set
			{
				SetValueWithoutNotify(value);
				onValueEdited?.Invoke(value);
			}
		}
		public InputField input;
		public AdeSliderWithEndEdit slider;

		public float SliderValueScale;

		public delegate void OnValueEdited(float value);

		public OnValueEdited onValueEdited;

		private void Awake()
		{
			SetValueWithoutNotify(Value);
			input.onEndEdit.AddListener(OnInputFieldEndEdit);
			input.onValueChanged.AddListener(OnInputValueChanged);
			slider.onEndEdit += OnSliderEndEdit;
			slider.Slider.onValueChanged.AddListener(OnSliderValueChanged);
		}

		public void SetValueWithoutNotify(float value)
		{
			this.value = value;
			input.SetTextWithoutNotify(value.ToString());
			slider.Slider.SetValueWithoutNotify(value / SliderValueScale);
		}

		private void OnInputFieldEndEdit(string str)
		{
			bool result = float.TryParse(str, out float value);
			if (!result)
			{
				SetValueWithoutNotify(this.value);
				return;
			}
			this.Value = value;
		}

		private void OnSliderEndEdit(float sliderValue)
		{
			this.Value = sliderValue * SliderValueScale;
		}

		private void OnInputValueChanged(string str)
		{
			bool result = float.TryParse(str, out float value);
			if (result)
			{
				slider.Slider.SetValueWithoutNotify(value / SliderValueScale);
				return;
			}
		}

		private void OnSliderValueChanged(float sliderValue)
		{
			input.SetTextWithoutNotify((sliderValue * SliderValueScale).ToString());
		}
	}
}
