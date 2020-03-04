using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Arcade.Gameplay;

namespace Arcade.Compose.UI
{
	[RequireComponent(typeof(Slider))]
	public class AdeTimingSlider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
	{
		public static AdeTimingSlider Instance { get; private set; }
		private Slider timingSlider;
		private bool pointerDown;
		private bool enable;
		public bool Enable
		{
			get
			{
				return enable;
			}
			set
			{
				if (!value) timingSlider.value = 0;
				timingSlider.interactable = value;
				enable = value;
			}
		}
		public int Length
		{
			set
			{
				timingSlider.maxValue = value;
			}
		}
		private void Awake()
		{
			Instance = this;
			timingSlider = GetComponent<Slider>();
		}
		private void Update()
		{
			if (!Enable) return;
			if (!pointerDown) timingSlider.value = ArcGameplayManager.Instance.Timing;
		}
		public void OnPointerDown(PointerEventData eventData)
		{
			pointerDown = true;
		}
		public void OnPointerUp(PointerEventData eventData)
		{
			pointerDown = false;
		}
		public void OnDrag(PointerEventData eventData)
		{
			ArcGameplayManager.Instance.Timing = (int)timingSlider.value;
			ArcGameplayManager.Instance.ResetJudge();
		}
	}
}