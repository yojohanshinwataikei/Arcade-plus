using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arcade.Gameplay.Chart;
using System.Linq;
using Arcade.Gameplay;
using Arcade.Compose.Command;
using System;

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
			return $"{timing.Timing},{timing.Bpm.ToString("f2")},{timing.BeatsPerLine.ToString("f2")}";
		}

		public void Add(ArcTiming caller)
		{
			CommandManager.Instance.Add(new AddTimingEvent(currentTimingGroup, caller.Clone() as ArcTiming));
		}
		public void Delete(ArcTiming caller)
		{
			CommandManager.Instance.Add(new RemoveTimingEvent(currentTimingGroup, caller));
		}
		public void UpdateTiming()
		{
			inUse = 0;

			List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
			options.Add(new Dropdown.OptionData { text = "默认" });
			foreach (var tg in ArcTimingManager.Instance.timingGroups)
			{
				options.Add(new Dropdown.OptionData { text = tg.Id.ToString() });
			}
			CurrentTimingGroupDropdown.options = options;

			CurrentTimingGroupDropdown.SetValueWithoutNotify(currentTimingGroup?.Id ?? 0);

			CurrentTimingGroupDropdown.interactable = ArcGameplayManager.Instance.Chart != null;

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
				CurrentTimingGroupText.text = currentTimingGroup.Id.ToString();
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

		public void ForceUpdate()
		{
			if (View.activeSelf)
			{
				UpdateTiming();
			}
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
			ArcTimingManager.Instance.Add(timing, timingGroup);
		}
		public void Undo()
		{
			ArcTimingManager.Instance.Remove(timing, timingGroup);
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
			ArcTimingManager.Instance.Remove(timing, timingGroup);
		}
		public void Undo()
		{
			ArcTimingManager.Instance.Add(timing, timingGroup);
		}
	}
}
