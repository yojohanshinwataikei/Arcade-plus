using Arcade.Compose;
using UnityEngine;

[ExecuteAlways]
public class AdeScreenResizeListener : MonoBehaviour
{
	protected void OnRectTransformDimensionsChange()
	{
		ArcadeComposeManager.Instance?.UpdateResolution();
	}

}

