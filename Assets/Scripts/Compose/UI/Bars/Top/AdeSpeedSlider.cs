using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Arcade.Gameplay;

namespace Arcade.Compose.UI
{
	[RequireComponent(typeof(Slider))]
	public class AdeSpeedSlider : MonoBehaviour, IPointerUpHandler
	{
		public static AdeSpeedSlider Instance { get; private set; }
		private void Awake()
		{
			Instance = this;
			speed = GetComponent<Slider>();
			speed.onValueChanged.AddListener((value) =>
			{
				Value.text = (speed.value / 10).ToString("f1");
			});
		}

		public Text Value;
		private Slider speed;

		public void UpdateVelocity(int value)
		{
			speed.SetValueWithoutNotify(ArcTimingManager.Instance.SettingVelocity / 3);
			Value.text = ((float)(speed.value) / 10).ToString("f1");
		}
		public void OnPointerUp(PointerEventData eventData)
		{
			ArcTimingManager.Instance.SettingVelocity = (int)speed.value * 3;
		}
	}
}
