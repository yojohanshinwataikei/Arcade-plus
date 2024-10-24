using Arcade.Compose;
using UnityEngine;

namespace Arcade.Compose.UI
{
	[ExecuteAlways]
	public class AdeScreenResizeListener : MonoBehaviour
	{
		protected void OnRectTransformDimensionsChange()
		{
			ArcadeComposeManager.Instance?.UpdateResolution();
		}

	}
}