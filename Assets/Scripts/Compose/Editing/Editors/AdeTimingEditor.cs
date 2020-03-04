using System.Collections.Generic;
using UnityEngine;
using Arcade.Gameplay.Chart;
using System.Linq;
using Arcade.Gameplay;
using Arcade.Compose.Command;

namespace Arcade.Compose.Editing
{
	public class AdeTimingEditor : MonoBehaviour
    {
        public static AdeTimingEditor Instance { get; private set; }

        public GameObject View;
        public GameObject TimingPrefab;
        public RectTransform TimingContent;

        private List<AdeTimingItem> timingInstances = new List<AdeTimingItem>();
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            ArcGameplayManager.Instance.OnChartLoad.AddListener(BuildList);
        }
        private void OnDestroy()
        {
            ArcGameplayManager.Instance.OnChartLoad.RemoveListener(BuildList);
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
            CommandManager.Instance.Add(new AddArcEventCommand(caller.Clone()));
            BuildList();
        }
        public void Delete(ArcTiming caller)
        {
            CommandManager.Instance.Add(new RemoveArcEventCommand(caller));
            BuildList();
        }
        public void BuildList()
        {
            inUse = 0;

            List<ArcTiming> timings = ArcTimingManager.Instance.Timings;
            timings.Sort((ArcTiming a, ArcTiming b) => a.Timing.CompareTo(b.Timing));

            foreach (var t in timings)
            {
                AdeTimingItem item = GetItemInstance();
                item.TimingReference = t;
                item.Text = GetTimingString(t);
                item.RemoveBtn.interactable = t.Timing != 0;
            }

            CleanUnusedInstance();
        }

        public void SwitchStatus()
        {
            View.SetActive(!View.activeSelf);
            if (View.activeSelf)
            {
                BuildList();
            }
            else
            {
                inUse = 0;
                CleanUnusedInstance();
            }
        }
    }
}