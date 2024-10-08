using System.Collections.Generic;
using UnityEngine;
using Arcade.Util.UnityExtension;
using Arcade.Compose.MarkingMenu;
using DG.Tweening;
using UnityEngine.InputSystem;

namespace Arcade.Compose
{
	public abstract class AdeMarkingMenuItemProvider : MonoBehaviour
	{
		public virtual bool IsOnlyMarkingMenu { get => false; }
		public virtual MarkingMenuItem[] MarkingMenuItems { get => null; }
	}
	public class AdeMarkingMenuManager : MonoBehaviour
	{
		public static AdeMarkingMenuManager Instance { get; private set; }

		public MarkingMenuLine Line;
		public List<AdeMarkingMenuItemProvider> Providers = new List<AdeMarkingMenuItemProvider>();

		private Stack<MarkingMenuItem[]> shownItems = new Stack<MarkingMenuItem[]>();

		private bool isOn = false;
		private void Awake()
		{
			Instance = this;
		}
		private void Update()
		{
			if (Mouse.current.rightButton.wasPressedThisFrame)
			{
				Enter();
			}
			if (Mouse.current.rightButton.wasReleasedThisFrame || (isOn && !Mouse.current.rightButton.isPressed))
			{
				Exit();
			}
			if (isOn && Mouse.current.rightButton.isPressed)
			{
				Check();
			}
		}

		public void Enter()
		{
			Line.Enable = true;
			Line.AddJoint();
			ShowAt(InputExtension.scaledMousePosition, GetCurrentMenuItems());
			isOn = true;
		}
		public void Exit()
		{
			foreach (var item in shownItems.Peek())
			{
				if (item.PointerInItem)
				{
					item.OnConfirmed.Invoke(); break;
				};
			}

			Line.Enable = false;
			foreach (var items in shownItems)
				foreach (var item in items)
					item.Hide();
			shownItems.Clear();
			isOn = false;
		}

		public void Check()
		{
			foreach (var item in shownItems.Peek())
			{
				if (item.PointerHangOver)
				{
					if (item.HasSubMenu)
					{
						item.OnHangOver.Invoke();
						item.OnConfirmed.Invoke();
						ShowSubMenu(item.SubItems);
						return;
					}
				};
			}
		}
		public MarkingMenuItem[] GetCurrentMenuItems()
		{
			List<MarkingMenuItem> items = new List<MarkingMenuItem>();
			foreach (var provider in Providers)
			{
				var it = provider.MarkingMenuItems;
				if (it == null) continue;
				if (provider.IsOnlyMarkingMenu)
				{
					return it;
				}
				items.AddRange(it);
			}
			return items.ToArray();
		}

		public void ShowSubMenu(MarkingMenuItem[] items, float from = 0, float to = 360)
		{
			Line.AddJoint();
			var parent = shownItems.Peek();
			foreach (var item in parent) item.Hide();
			ShowAt(InputExtension.scaledMousePosition, items, from, to);
		}
		public void ReturnParentMenu()
		{
			MarkingMenuItem[] items = shownItems.Pop();
			foreach (var item in items) item.Hide();
			items = shownItems.Peek();
			foreach (var item in items) item.Show();
		}

		private float GetOptimizedDegree(float degree)
		{
			float remain = 0;
			float normalized = degree;
			while (normalized >= 180)
			{
				normalized -= 180;
				remain += 180;
			}
			while (normalized < 0)
			{
				normalized += 180;
				remain -= 180;
			}

			float d = normalized / 90f - 1;
			bool neg = d < 0;
			d = Mathf.Abs(d);
			d = Mathf.Abs(Mathf.Sin(Mathf.PI * Mathf.Pow(d, 1.2f) / 2));
			d = (d * (neg ? -1 : 1) + 1) * 90f;
			d += remain;

			return d;
		}
		private float GetEllipseLength(float degree, float itemCount)
		{
			if (itemCount > 4) itemCount += itemCount % 2;
			return MathfExtension.EvaluateEllipse(degree, 60 + 10 * Mathf.Max(6, itemCount), 50 + 12 * Mathf.Max(3, itemCount));
		}

		public void ShowAt(Vector2 position, MarkingMenuItem[] items, float from = 0, float to = 360)
		{
			int itemCount = items.Length;
			float degreeDelta = (to - from) / (itemCount > 4 ? itemCount % 2 + itemCount : itemCount);
			for (int i = 0; i < itemCount; ++i)
			{
				float degree = from + degreeDelta * i + (itemCount > 4 ? 180 : 0);
				degree = GetOptimizedDegree(degree);
				float length = GetEllipseLength(degree, itemCount);
				items[i].Show();
				items[i].Position = position + new Vector2(length * Mathf.Cos(degree * Mathf.Deg2Rad), length * Mathf.Sin(degree * Mathf.Deg2Rad));
				items[i].Degree = degree;
			}
			shownItems.Push(items);
		}
		public void ShowSubMenuAt(Vector2 position, MarkingMenuItem[] items, float from = 0, float to = 360)
		{
			int itemCount = items.Length;
			float degreeDelta = (to - from) / (itemCount - 1 == 0 ? 1 : itemCount - 1);
			for (int i = 0; i < itemCount; ++i)
			{
				float degree = from + degreeDelta * i;
				float length = MathfExtension.EvaluateEllipse(degree, 180, 100);
				items[i].Show();
				items[i].Position = position + new Vector2(length * Mathf.Cos(degree * Mathf.Deg2Rad), length * Mathf.Sin(degree * Mathf.Deg2Rad));
				items[i].Degree = degree;
			}
			shownItems.Push(items);
		}
	}
}
