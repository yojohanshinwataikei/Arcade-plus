using System.Collections;
using UnityEngine;
using DG.Tweening;
using Arcade.Compose.Dialog;

namespace Arcade.Compose
{
	public class AdeShutterManager : MonoBehaviour
	{
		public const float Duration = 0.65f;
		public static AdeShutterManager Instance { get; private set; }
		private void Awake()
		{
			if (Instance != null)
			{
				Destroy(gameObject);
				return;
			}
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}

		public RectTransform Left, Right;
		public AudioClip CloseAudio, OpenAudio;

		// TODO:Remove Usage of AudioSource.PlayClipAtPoint since you can not remove the playing audioClip
		// We need to remove playing sound effects when we are swaping skin
		public void Open()
		{
			Left.DOPivotX(1, Duration).SetEase(Ease.InCubic);
			Right.DOPivotX(0, Duration).SetEase(Ease.InCubic);
			AudioSource.PlayClipAtPoint(OpenAudio, new Vector3(),AdeSoundDialog.Instance.ChartSource.volume);
		}
		public void Close()
		{
			Left.DOPivotX(0, Duration).SetEase(Ease.OutCubic);
			Right.DOPivotX(1, Duration).SetEase(Ease.OutCubic);
			AudioSource.PlayClipAtPoint(CloseAudio, new Vector3(),AdeSoundDialog.Instance.ChartSource.volume);
		}
		public IEnumerator OpenCoroutine()
		{
			Open();
			yield return new WaitForSeconds(Duration);
		}
		public IEnumerator CloseCoroutine()
		{
			Close();
			yield return new WaitForSeconds(Duration);
		}
	}
}
