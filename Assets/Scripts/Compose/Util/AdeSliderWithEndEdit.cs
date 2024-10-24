using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Arcade.Gameplay;

namespace Arcade.Compose.UI
{
	[RequireComponent(typeof(Slider))]
	public class AdeSliderWithEndEdit : MonoBehaviour, IPointerUpHandler, IMoveHandler
	{
		private Slider slider;
		public Slider Slider
		{
			get
			{
				if (slider == null)
				{
					slider = GetComponent<Slider>();
				}
				return slider;
			}
		}

		public delegate void OnEndEdit(float value);

		public OnEndEdit onEndEdit;
		public void OnPointerUp(PointerEventData eventData)
		{
			onEndEdit?.Invoke(Slider.value);
		}

		public void OnMove(AxisEventData eventData)
		{
			onEndEdit?.Invoke(Slider.value);
		}
	}
}