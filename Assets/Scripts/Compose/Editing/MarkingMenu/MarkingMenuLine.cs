using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Arcade.Util.UnityExtension;

namespace Arcade.Compose.MarkingMenu
{
	public class MarkingMenuLine : MonoBehaviour
	{
		public static MarkingMenuLine Instance { get; private set; }

		public GameObject KnobPrefab;
		public Image PointerImage;
		public RectTransform Pointer;
		public LineRenderer Line;

		private bool enable;
		private int knobCount;
		private List<RectTransform> knobInstances = new List<RectTransform>();

		public bool Enable
		{
			get
			{
				return enable;
			}
			set
			{
				if (enable != value)
				{
					PointerImage.enabled = value;
					Line.enabled = value;
					Initialize();
					enable = value;
				}
			}
		}

		private void Awake()
		{
			Instance = this;
		}
		private void Update()
		{
			if (!enable) return;
			UpdatePointer();
		}
		public void Initialize()
		{
			foreach (var knob in knobInstances) Destroy(knob.gameObject);
			knobInstances.Clear();
			knobCount = 0;
			Line.SetPositions(new Vector3[1]);
			Line.positionCount = knobCount + 1;
		}
		public void AddJoint()
		{
			AddJoint(InputExtension.scaledMousePosition);
		}
		public void AddJoint(Vector2 position)
		{
			RectTransform knob = Instantiate(KnobPrefab, transform).GetComponent<RectTransform>();
			knob.anchoredPosition = position;
			knobInstances.Add(knob);

			Line.SetPosition(knobCount, position);
			knobCount++;
			Line.positionCount = knobCount + 1;
		}
		public void RemoveLastJoint()
		{
			if (knobInstances.Count == 0) return;
			RectTransform knob = knobInstances.Last();
			Destroy(knob.gameObject);
			knobInstances.Remove(knob);

			knobCount--;
			Line.positionCount = knobCount + 1;
		}

		private void UpdatePointer()
		{
			Vector2 mousePosition = InputExtension.scaledMousePosition;
			Line.SetPosition(knobCount, mousePosition);
			Pointer.anchoredPosition = mousePosition;
		}
	}
}
