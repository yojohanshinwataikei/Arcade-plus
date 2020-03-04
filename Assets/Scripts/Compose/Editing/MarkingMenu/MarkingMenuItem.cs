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

		public bool PointerInItem { get; set; }
		private float pointerInItemTiming;

		private void Start()
		{
			Text = StartupText;
		}
		private void OnEnable()
		{
			ItemCanvasGroup.DOFade(1, 0.15f);
		}
		public void Hide()
		{
			ItemCanvasGroup.DOFade(0, 0.15f).OnComplete(() =>
			{
				Background.color = OutColor;
				gameObject.SetActive(false);
			});
		}

		public void OnPointerEnter(PointerEventData data)
		{
			PointerInItem = true;
			Background.color = InColor;
			pointerInItemTiming = 0;
		}
		public void OnPointerExit(PointerEventData data)
		{
			PointerInItem = false;
			Background.color = OutColor;
			pointerInItemTiming = 0;
		}
		private void Update()
		{
			if (Input.GetMouseButton(1) && PointerInItem)
			{
				pointerInItemTiming += Time.deltaTime;
				if (pointerInItemTiming > EnterTimeThreshold && HasSubMenu)
				{
					OnHangOver.Invoke();
					OnConfirmed.Invoke();
					PointerInItem = false;
					ShowSubMenu();
					Hide();
				}
			}
			if (Input.GetMouseButtonUp(1) && PointerInItem)
			{
				OnConfirmed.Invoke();
				PointerInItem = false;
				AdeMarkingMenuManager.Instance.Hide();
			}
		}

		private void ShowSubMenu()
		{
			int subCount = SubItems.Length - 1;
			AdeMarkingMenuManager.Instance.ShowSubMenu(SubItems);
		}
	}
}