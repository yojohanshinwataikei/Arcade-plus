using UnityEngine;
using UnityEngine.UI;

// Note: this class is used to resize the outer scroll view of the content in it
[ExecuteAlways]
public class AdeAutoResizeScrollView : MonoBehaviour, ILayoutElement
{
	[System.NonSerialized] private RectTransform m_Rect;
	protected RectTransform rectTransform
	{
		get
		{
			if (m_Rect == null)
				m_Rect = GetComponent<RectTransform>();
			return m_Rect;
		}
	}
	public RectTransform content;
	[SerializeField] private float m_Max = 100;
	public float max { get { return m_Max; } set { if (m_Max == value) { return; } else { m_Max = value; SetDirty(); } } }
	public float flexibleWidth { get { return -1; } }
	public float minWidth { get { return -1; } }
	public float preferredWidth { get { return -1; } }
	public float flexibleHeight { get { return -1; } }
	public float minHeight { get { return -1; } }
	public float preferredHeight { get { return Mathf.Min(max, LayoutUtility.GetPreferredHeight(content)); } }
	public int layoutPriority { get { return 1; } }
	public void CalculateLayoutInputHorizontal() { }
	public void CalculateLayoutInputVertical() { }
	protected void OnEnable()
	{
		SetDirty();
	}
	protected void OnDisable()
	{
		SetDirty();
	}
	protected void OnTransformParentChanged()
	{
		SetDirty();
	}
	protected void OnDidApplyAnimationProperties()
	{
		SetDirty();
	}

	protected void OnBeforeTransformParentChanged()
	{
		SetDirty();
	}
#if UNITY_EDITOR
	protected void OnValidate()
	{
		SetDirty();
	}
#endif
	public void SetDirty()
	{
		if (!isActiveAndEnabled)
			return;

		LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
	}
}

