using System.Collections;
using UnityEngine;
using DG.Tweening;
using Arcade.Compose.Dialog;

namespace Arcade.Compose
{
	public class AdeGameplayContentResizer : MonoBehaviour
	{
		public RenderTexture GameplayRenderTexture;
		public Camera EditorCamera;
		private void Update()
		{
			if (GameplayRenderTexture.width != EditorCamera.pixelWidth || GameplayRenderTexture.height != EditorCamera.pixelHeight)
			{
				GameplayRenderTexture.Release();
				GameplayRenderTexture.width = EditorCamera.pixelWidth;
				GameplayRenderTexture.height = EditorCamera.pixelHeight;
			}
		}
	}
}
