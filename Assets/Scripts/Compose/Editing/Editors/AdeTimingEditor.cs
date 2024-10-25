using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arcade.Gameplay.Chart;
using System.Linq;
using Arcade.Gameplay;
using Arcade.Compose.Command;
using System;
using System.Globalization;

namespace Arcade.Compose.Editing
{
	public class AdeTimingEditor : MonoBehaviour
	{
		public static AdeTimingEditor Instance { get; private set; }

		public GameObject View;
		public GameObject TimingPrefab;
		public RectTransform TimingContent;
		public Dropdown CurrentTimingGroupDropdown;
		public Text CurrentTimingGroupText;
		public Button AddTimingGroupButton, RemoveTimingGroupButton;

		public ArcTimingGroup currentTimingGroup { get; private set; } = null;

		private List<AdeTimingItem> timingInstances = new List<AdeTimingItem>();
		private void Awake()
		{
			Instance = this;
			CurrentTimingGroupDropdown.onValueChanged.AddListener((value) =>
			{
				SetCurrentTimingGroup(value == 0 ? null : ArcTimingManager.Instance.timingGroups[value - 1]);
			});
		}

		private void Start()
		{
			ArcGameplayManager.Instance.OnChartLoad.AddListener(UpdateTiming);
		}
		private void OnDestroy()
		{
			ArcGameplayManager.Instance.OnChartLoad.RemoveListener(UpdateTiming);
		}

		private int inUse = 0;
		private AdeTimingItem GetItemInstance()
		{
			while (inUse >= timingInstances.Count)
			{
				timingInstances.Add(Instantiate(TimingPrefab, TimingContent).GetComponent<AdeTimingItem>());
			}
			return timingInstances[inUse++];
		}
		private void CleanUnusedInstance()
		{
			while (inUse < timingInstances.Count)
			{
				Destroy(timingInstances.Last().gameObject);
				timingInstances.Remove(timingInstances.Last());
			}
		}
		private string GetTimingString(ArcTiming timing)
		{
			return $"{timing.Timing.ToString(CultureInfo.InvariantCulture)},{timing.Bpm.ToString("f2", CultureInfo.InvariantCulture)},{timing.BeatsPerLine.ToString("f2", CultureInfo.InvariantCulture)}";
		}

		public void AddTiming(ArcTiming caller)
		{
			AdeCommandManager.Instance.Add(new AddTimingEvent(currentTimingGroup, caller.Clone() as ArcTiming));
		}
		public void RemoveTiming(ArcTiming caller)
		{
			AdeCommandManager.Instance.Add(new RemoveTimingEvent(currentTimingGroup, caller));
		}
		public void AddTimingGroup()
		{
			ArcTimingGroup timingGroup = new ArcTimingGroup
			{
				Id = ArcTimingManager.Instance.timingGroups.Count + 1,
				Timings = ArcTimingManager.Instance.GetTiming(currentTimingGroup).Select((timing) => timing.Clone() as ArcTiming).ToList(),
			};
			AdeCommandManager.Instance.Add(new AddTimingGroup(timingGroup));
			SetCurrentTimingGroup(timingGroup);
		}
		public void RemoveTimingGroup()
		{
			if (currentTimingGroup == null)
			{
				return;
			}
			AdeCommandManager.Instance.Add(new RemoveTimingGroup(currentTimingGroup));
		}

		public void UpdateTiming()
		{
			inUse = 0;

			List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
			options.Add(new Dropdown.OptionData { text = "默认" });
			foreach (var tg in ArcTimingManager.Instance.timingGroups)
			{
				options.Add(new Dropdown.OptionData { text = tg.Id.ToString(CultureInfo.InvariantCulture) });
			}
			CurrentTimingGroupDropdown.options = options;

			CurrentTimingGroupDropdown.SetValueWithoutNotify(currentTimingGroup?.Id ?? 0);

			if (ArcGameplayManager.Instance.Chart == null)
			{
				CurrentTimingGroupDropdown.interactable = false;
				AddTimingGroupButton.interactable = false;
				RemoveTimingGroupButton.interactable = false;
			}
			else
			{
				CurrentTimingGroupDropdown.interactable = true;
				AddTimingGroupButton.interactable = true;
				RemoveTimingGroupButton.interactable = currentTimingGroup != null;
			}
			List<ArcTiming> timings = ArcTimingManager.Instance.GetTiming(currentTimingGroup);
			foreach (var t in timings)
			{
				AdeTimingItem item = GetItemInstance();
				item.TimingReference = t;
				item.Text = GetTimingString(t);
				item.RemoveBtn.interactable = timings.Count > 1;
			}

			CleanUnusedInstance();
		}

		public void SetCurrentTimingGroup(ArcTimingGroup arcTimingGroup)
		{
			currentTimingGroup = arcTimingGroup;
			if (View.activeSelf)
			{
				UpdateTiming();
			}
			if (currentTimingGroup == null)
			{
				CurrentTimingGroupText.text = "默认";
			}
			else
			{
				CurrentTimingGroupText.text = currentTimingGroup.Id.ToString(CultureInfo.InvariantCulture);
			}
			AdeGridManager.Instance.ReBuildBeatline();
		}

		public void SwitchStatus()
		{
			View.SetActive(!View.activeSelf);
			if (View.activeSelf)
			{
				UpdateTiming();
			}
			else
			{
				inUse = 0;
				CleanUnusedInstance();
			}
		}

		public void Display()
		{
			View.SetActive(true);
			UpdateTiming();
		}

		public void ForceUpdateTimingGroup(ArcTimingGroup timingGroup)
		{
			if (timingGroup == currentTimingGroup)
			{
				if (View.activeSelf)
				{
					UpdateTiming();
				}
				AdeGridManager.Instance.ReBuildBeatline();
			}
		}

		public void ForceUpdateAll()
		{
			if (View.activeSelf)
			{
				UpdateTiming();
			}
			AdeValueEditor.Instance.UpdateFields();
		}
	}

	public class AddTimingEvent : ICommand
	{
		private readonly ArcTimingGroup timingGroup;
		private readonly ArcTiming timing;
		public AddTimingEvent(ArcTimingGroup timingGroup, ArcTiming timing)
		{
			this.timingGroup = timingGroup;
			this.timing = timing;
		}
		public string Name
		{
			get
			{
				return "添加 Timing";
			}
		}
		public void Do()
		{
			ArcTimingManager.Instance.AddTiming(timing, timingGroup);
		}
		public void Undo()
		{
			ArcTimingManager.Instance.RemoveTiming(timing, timingGroup);
		}
	}

	public class RemoveTimingEvent : ICommand
	{
		private readonly ArcTimingGroup timingGroup;
		private readonly ArcTiming timing;
		public RemoveTimingEvent(ArcTimingGroup timingGroup, ArcTiming timing)
		{
			this.timingGroup = timingGroup;
			this.timing = timing;
		}
		public string Name
		{
			get
			{
				return "删除 Timing";
			}
		}
		public void Do()
		{
			ArcTimingManager.Instance.RemoveTiming(timing, timingGroup);
		}
		public void Undo()
		{
			ArcTimingManager.Instance.AddTiming(timing, timingGroup);
		}
	}
	public class EditTimingEvent : ICommand
	{
		private readonly ArcTimingGroup timingGroup;
		private readonly ArcTiming timing;
		private readonly ArcTiming oldValues, newValues;
		public EditTimingEvent(ArcTimingGroup timingGroup, ArcTiming timing, ArcTiming newValues)
		{
			this.timingGroup = timingGroup;
			this.timing = timing;
			this.oldValues = timing.Clone() as ArcTiming;
			this.newValues = newValues;
		}
		public string Name
		{
			get
			{
				return "修改 Timing";
			}
		}
		public void Do()
		{
			timing.Assign(newValues);
			ArcTimingManager.Instance.UpdateTimingGroup(timingGroup);
		}
		public void Undo()
		{
			timing.Assign(oldValues);
			ArcTimingManager.Instance.UpdateTimingGroup(timingGroup);
		}
	}

	public class AddTimingGroup : ICommand
	{
		private readonly ArcTimingGroup timingGroup;
		public AddTimingGroup(ArcTimingGroup timingGroup)
		{
			this.timingGroup = timingGroup;
		}
		public string Name
		{
			get
			{
				return "添加 Timing Group";
			}
		}
		public void Do()
		{
			ArcTimingManager.Instance.AddTimingGroup(timingGroup);
		}
		public void Undo()
		{
			if (AdeTimingEditor.Instance.currentTimingGroup == timingGroup)
			{
				AdeTimingEditor.Instance.SetCurrentTimingGroup(null);
			}
			ArcTimingManager.Instance.RemoveTimingGroup(timingGroup);
		}

	}
	public class RemoveTimingGroup : ICommand
	{
		private readonly ArcTimingGroup timingGroup;
		private readonly List<ArcTap> taps;
		private readonly List<ArcHold> holds;
		private readonly List<ArcArc> arcs;
		public RemoveTimingGroup(ArcTimingGroup timingGroup)
		{
			this.timingGroup = timingGroup;
			this.taps = ArcTapNoteManager.Instance.Taps.Where((tap) => tap.TimingGroup == timingGroup).ToList();
			this.holds = ArcHoldNoteManager.Instance.Holds.Where((hold) => hold.TimingGroup == timingGroup).ToList();
			this.arcs = ArcArcManager.Instance.Arcs.Where((arc) => arc.TimingGroup == timingGroup).ToList();
		}
		public string Name
		{
			get
			{
				return "删除 Timing Group";
			}
		}
		public void Do()
		{
			foreach (var tap in taps)
			{
				AdeSelectionManager.Instance.DeselectNote(tap);
				ArcTapNoteManager.Instance.Remove(tap);
			}
			foreach (var hold in holds)
			{
				AdeSelectionManager.Instance.DeselectNote(hold);
				ArcHoldNoteManager.Instance.Remove(hold);
			}
			foreach (var arc in arcs)
			{
				AdeSelectionManager.Instance.DeselectNote(arc);
				foreach (var arctap in arc.ArcTaps)
				{
					AdeSelectionManager.Instance.DeselectNote(arctap);
				}
				ArcArcManager.Instance.Remove(arc);
			}
			if (AdeTimingEditor.Instance.currentTimingGroup == timingGroup)
			{
				AdeTimingEditor.Instance.SetCurrentTimingGroup(null);
			}
			ArcTimingManager.Instance.RemoveTimingGroup(timingGroup);
		}
		public void Undo()
		{
			foreach (var tap in taps)
			{
				ArcTapNoteManager.Instance.Add(tap);
			}
			foreach (var hold in holds)
			{
				ArcHoldNoteManager.Instance.Add(hold);
			}
			foreach (var arc in arcs)
			{
				ArcArcManager.Instance.Add(arc);
			}
			ArcTimingManager.Instance.AddTimingGroup(timingGroup);
		}
	}
}
