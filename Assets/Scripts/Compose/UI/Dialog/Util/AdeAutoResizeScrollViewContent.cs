using UnityEngine;

[ExecuteAlways]
public class AdeAutoResizeScrollViewContent : MonoBehaviour
{
	public AdeAutoResizeScrollView scrollview;
	protected void OnRectTransformDimensionsChange()
	{
		scrollview.SetDirty();
	}

}

