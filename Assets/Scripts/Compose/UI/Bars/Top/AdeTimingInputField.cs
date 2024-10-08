using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Arcade.Gameplay;
using System;

namespace Arcade.Compose.UI
{
	public class AdeTimingInputField : MonoBehaviour
	{
		public static AdeTimingInputField Instance { get; private set; }
		private void Start()
		{
			Instance = this;
			TimingInput.onValueChanged.AddListener(OnValueChange);
			SwitchModeButton.onClick.AddListener(OnSwitchMode);
			UpdateLabel();
		}

		public InputField TimingInput;
		public Button SwitchModeButton;
		public Text Label;

		private bool useChartTiming = false;

		public void UpdateLabel()
		{
			if (useChartTiming)
			{
				Label.text = "谱面时间";
			}
			else
			{
				Label.text = "音频时间";
			}
		}

		private void Update()
		{
			if (!TimingInput.isFocused)
			{
				int timing = useChartTiming ? ArcGameplayManager.Instance.ChartTiming : ArcGameplayManager.Instance.AudioTiming;
				TimingInput.SetTextWithoutNotify(timing.ToString());
			}
		}

		public void OnValueChange(string s)
		{
			if (int.TryParse(s, out int value))
			{
				if (useChartTiming)
				{
					ArcGameplayManager.Instance.ChartTiming = value;
				}
				else
				{
					ArcGameplayManager.Instance.AudioTiming = value;
				}
			}
		}
		private void OnSwitchMode()
		{
			useChartTiming = !useChartTiming;
			UpdateLabel();
		}
	}
}
