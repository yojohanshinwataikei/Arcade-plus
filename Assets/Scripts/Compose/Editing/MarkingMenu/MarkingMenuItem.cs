using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.Events;

namespace Arcade.Compose.MarkingMenu
{
	public class MarkingMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		public CanvasGroup ItemCanvasGroup;
		public RectTransform ItemRectTransform;
		public Text ItemText;
		public Image Background;
		public Color InColor, OutColor;

		[Header("Settings")]
		public string StartupText;
		public UnityEvent OnConfirmed = new UnityEvent();

		[Header("Sub Menu")]
		public bool HasSubMenu;
		public float EnterTimeThreshold = 0.4f;
		public MarkingMenuItem[] SubItems;
		public UnityEvent OnHangOver = new UnityEvent();

		public float Degree { get; set; }
		public string Text
		{
			get
			{
				return ItemText.text;
			}
			set
			{
				if (HasSubMenu) value += " ·";
				ItemText.text = value;
			}
		}
		public Vector2 Position
		{
			get
			{
				return ItemRectTransform.anchoredPosition;
			}
			set
			{
				ItemRectTransform.anchoredPosition = value;
			}
		}

		public bool PointerInItem { get; private set; }
		public bool PointerHangOver
		{
			get
			{
				return PointerInItem && pointerInItemTiming > EnterTimeThreshold;
			}
		}
		private float pointerInItemTiming;
		private bool active = false;

		private void Start()
		{
			Text = StartupText;
		}
		public void Show(){
			gameObject.SetActive(true);
			active = true;
			ItemCanvasGroup.DOComplete();
			ItemCanvasGroup.DOFade(1, 0.15f);
		}
		public void Hide()
		{
			active = false;
			PointerInItem = false;
			pointerInItemTiming = 0;
			ItemCanvasGroup.DOComplete();
			ItemCanvasGroup.DOFade(0, 0.15f).OnComplete(() =>
			{
				Background.color = OutColor;
				if (!active)
				{
					gameObject.SetActive(false);
				}
			});
		}

		public void OnPointerEnter(PointerEventData data)
		{
			if(active){
				PointerInItem = true;
				Background.color = InColor;
				pointerInItemTiming = 0;
			}
		}
		public void OnPointerExit(PointerEventData data)
		{
			if(active){
				PointerInItem = false;
				Background.color = OutColor;
				pointerInItemTiming = 0;
			}
		}
		private void Update()
		{
			if (Input.GetMouseButton(1) && PointerInItem)
			{
				pointerInItemTiming += Time.deltaTime;
			}
		}
	}
}
