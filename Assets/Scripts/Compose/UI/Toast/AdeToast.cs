using UnityEngine;
using UnityEngine.UI;
using Arcade.Util.UnityExtension;
using DG.Tweening;

namespace Arcade.Compose
{
	public class AdeToast : MonoBehaviour
	{
		public const float Duration = 0.4f;
		public const float Stay = 1f;
		public static AdeToast Instance { get; private set; }
		public RectTransform ToastRect;
		public RectTransform ContentRect;
		public Text Content;

		private void Awake()
		{
			Instance = this;
		}
		public void Show(string text)
		{
			ToastRect.DOKill(true);
			float height = Content.CalculateHeight(text);
			Content.text = text;
			ToastRect.DOPivotY(1, Duration).OnComplete(() =>
			{
				ToastRect.DOPivotY(0, Duration).SetDelay(Stay * height / 60);
			});
		}
	}
}
