using System.Collections.Generic;
using System.Linq;
using Arcade.Compose;
using Arcade.Compose.Editing;
using Arcade.Compose.UI;
using Arcade.Gameplay.Chart;
using UnityEngine;

namespace Arcade.Gameplay
{
	public class ArcTimingManager : MonoBehaviour
	{
		public static ArcTimingManager Instance { get; private set; }
		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			phaseShaderId = Shader.PropertyToID("_Phase");
		}

		private int settingVelocity = 30;
		public float BaseBpm = 100;
		public Transform BeatlineLayer;
		public GameObject BeatlinePrefab;

		public const int LostDelay = 120;
		public float BeatlineEnwidenRatio = 0;

		// Note: This should be ordered!
		[HideInInspector]
		private List<ArcTiming> timings = new List<ArcTiming>();
		[HideInInspector]
		public List<ArcTimingGroup> timingGroups = new List<ArcTimingGroup>();
		public SpriteRenderer[] TrackComponentRenderers;
		private List<float> beatlineTimings = new List<float>();
		private List<SpriteRenderer> beatLineInstances = new List<SpriteRenderer>();
		private float earliestRenderTime = 0;
		private float latestRenderTime = 0;
		private int phaseShaderId = 0;
		private float phase = 0;
		public float CurrentSpeed { get; set; }
		public List<ArcTiming> Timings { get => timings; }
		private float Velocity
		{
			get => settingVelocity/3*2.65f;
		}

		public int SettingVelocity{
			get => settingVelocity; set
			{
				settingVelocity = value;
				ArcArcManager.Instance.Rebuild();
				AdeSpeedSlider.Instance.UpdateVelocity(value);
			}
		}

		private void Update()
		{
			if (Timings == null) return;
			if (Timings.Count == 0) return;
			UpdateChartSpeedStatus();
			UpdateRenderRange(null);
			foreach (var timingGroup in timingGroups)
			{
				UpdateRenderRange(timingGroup);
			}
			UpdateBeatline();
			UpdateTrackSpeed();
		}

		public void Clean()
		{
			CurrentSpeed = 0;
			Timings.Clear();
			timingGroups.Clear();
			AdeTimingEditor.Instance.SetCurrentTimingGroup(null);
			foreach (var renderer in TrackComponentRenderers)
			{
				renderer.sharedMaterial.SetFloat(phaseShaderId, 0);
			}
			HideExceededBeatlineInstance(0);
		}
		public void Load(List<ArcTiming> arcTimings, List<ArcTimingGroup> arcTimingGroups)
		{
			// Note: We replaced the inplace sort by sort to another list and reassign
			// just because we do not have stable inplace sort now in dot net
			timings = arcTimings.OrderBy((timing) => timing.Timing).ToList();
			ArcGameplayManager.Instance.Chart.Timings = timings;
			timingGroups = arcTimingGroups;
			foreach (var timingGroup in timingGroups)
			{
				timingGroup.Timings = timingGroup.Timings.OrderBy((timing) => timing.Timing).ToList();
			}
			OnTimingGroupChange();
			OnTimingChange(null);
		}

		public List<ArcTiming> GetTiming(ArcTimingGroup timingGroup)
		{
			if (timingGroup == null)
			{
				return Timings;
			}
			return timingGroup.Timings;
		}
		private void HideExceededBeatlineInstance(int quantity)
		{
			int count = beatLineInstances.Count;
			while (count > quantity)
			{
				beatLineInstances[count - 1].enabled = false;
				count--;
			}
		}
		private SpriteRenderer GetBeatlineInstance(int index)
		{
			while (beatLineInstances.Count < index + 1)
			{
				beatLineInstances.Add(Instantiate(BeatlinePrefab, BeatlineLayer).GetComponent<SpriteRenderer>());
			}
			return beatLineInstances[index];
		}
		public void CalculateBeatlineTimes()
		{
			beatlineTimings.Clear();
			HideExceededBeatlineInstance(0);
			if (Timings.Count == 0)
			{
				return;
			}
			for (int i = 0; i < Timings.Count; ++i)
			{
				if (Timings[i].Bpm == 0 || Timings[i].BeatsPerLine == 0)
				{
					beatlineTimings.Add(Timings[i].Timing);
					continue;
				}
				float nextTiming = i + 1 >= Timings.Count ? ArcGameplayManager.Instance.Length : Timings[i + 1].Timing;
				float segment = (60000 / Mathf.Abs(Timings[i].Bpm) * Timings[i].BeatsPerLine);
				if (segment == 0) continue;
				int n = 0;
				while (true)
				{
					float j = Timings[i].Timing + n * segment;
					if (j >= nextTiming)
					{
						break;
					}
					beatlineTimings.Add(j);
					n++;
				}
			}

			if (Timings[0].Bpm != 0 && Timings[0].BeatsPerLine != 0)
			{
				float t = 0;
				float segment = 60000 / Mathf.Abs(Timings[0].Bpm) * Timings[0].BeatsPerLine;
				int n = 0;
				while (true)
				{
					n++;
					t = -n * segment;
					if (t < -ArcAudioManager.Instance.AudioOffset)
					{
						break;
					}
					beatlineTimings.Insert(0, t);
				}
			}
		}

		public float CalculatePositionByTiming(int timing, ArcTimingGroup timingGroup)
		{
			return CalculatePositionByTimingAndStart(ArcGameplayManager.Instance.Timing, timing, timingGroup);
		}
		public float CalculatePositionByTimingAndStart(int pivotTiming, int targetTiming, ArcTimingGroup timingGroup)
		{
			var Timings = GetTiming(timingGroup);
			if (Timings.Count == 0)
			{
				return 0;
			}
			int offset = ArcAudioManager.Instance.AudioOffset;
			bool reversed = pivotTiming > targetTiming;
			int startTiming = (reversed ? targetTiming : pivotTiming) - offset;
			int endTiming = (reversed ? pivotTiming : targetTiming) - offset;
			int startTimingId = Timings.FindLastIndex((timing) => timing.Timing <= startTiming);
			int endTimingId = Timings.FindLastIndex((timing) => timing.Timing <= endTiming);
			if (startTimingId == -1)
			{
				startTimingId = 0;
			}
			if (endTimingId == -1)
			{
				endTimingId = 0;
			}
			float result = 0;
			for (int i = startTimingId; i <= endTimingId; i++)
			{
				int segmentStartTiming = i == startTimingId ? startTiming : Timings[i].Timing;
				int segmentEndTiming = i == endTimingId ? endTiming : Timings[i + 1].Timing;
				result += (segmentEndTiming - segmentStartTiming) * Timings[i].Bpm / BaseBpm * Velocity;
			}
			float newresult = reversed ? -result : result;

			return newresult;
		}

		public int CalculateTimingByPosition(float position, ArcTimingGroup timingGroup)
		{
			var Timings = GetTiming(timingGroup);
			if (Timings.Count == 0)
			{
				return 0;
			}
			int currentTiming = ArcGameplayManager.Instance.Timing - ArcAudioManager.Instance.AudioOffset;
			if (position < 0)
			{
				return currentTiming;
			}
			int currentTimingId = Timings.FindLastIndex((timing) => timing.Timing <= currentTiming);
			int allEndTime = ArcGameplayManager.Instance.Length - ArcAudioManager.Instance.AudioOffset;
			float positionRemain = position;
			for (int i = currentTimingId; i < Timings.Count; i++)
			{
				int startTiming = i == currentTimingId ? currentTiming : Timings[i].Timing;
				int endTiming = i + 1 == Timings.Count ? allEndTime : Timings[i + 1].Timing;
				float bpm = i == -1 ? Timings[0].Bpm : Timings[i].Bpm;
				float delta = (endTiming - startTiming) * bpm / BaseBpm * Velocity;
				if (delta < positionRemain)
				{
					positionRemain -= delta;
					continue;
				}
				if (delta == 0)
				{
					return startTiming;
				}
				return Mathf.RoundToInt(Mathf.Lerp(startTiming, endTiming, positionRemain / delta)) + ArcAudioManager.Instance.AudioOffset;
			}
			return allEndTime + ArcAudioManager.Instance.AudioOffset;
		}


		public float CalculateBpmByTiming(int timing, ArcTimingGroup timingGroup)
		{
			var Timings = GetTiming(timingGroup);
			if (Timings.Count == 0)
			{
				return 0;
			}
			return Timings.Last(timingEvent => timingEvent.Timing <= timing).Bpm;
		}

		private void UpdateChartSpeedStatus()
		{
			int offset = ArcAudioManager.Instance.AudioOffset;
			int currentTiming = ArcGameplayManager.Instance.Timing - offset;
			if (Timings.Count == 0)
			{
				CurrentSpeed = 0;
				return;
			}
			int currentTimingId = Timings.FindLastIndex((timing) => timing.Timing <= currentTiming);
			if (currentTimingId == -1)
			{
				currentTimingId = 0;
			}
			CurrentSpeed = Timings[currentTimingId].Bpm / BaseBpm;
		}
		private void UpdateRenderRange(ArcTimingGroup timingGroup)
		{
			var Timings = GetTiming(timingGroup);
			int nearPosition = -100000;
			int farPosition = 100000;
			float earliestRenderTime, latestRenderTime;
			if (Timings.Count == 0)
			{
				earliestRenderTime = float.NegativeInfinity;
				latestRenderTime = float.PositiveInfinity;
			}
			else
			{
				int currentTiming = ArcGameplayManager.Instance.Timing - ArcAudioManager.Instance.AudioOffset;
				int currentTimingId = Timings.FindLastIndex((timing) => timing.Timing <= currentTiming);
				float[] TimingPosition = new float[Timings.Count];
				for (int i = currentTimingId; i + 1 < Timings.Count; i++)
				{
					int startTiming = i == currentTimingId ? currentTiming : Timings[i].Timing;
					int endTiming = Timings[i + 1].Timing;
					float startPosition = i == currentTimingId ? 0 : TimingPosition[i];
					float bpm = i == -1 ? Timings[0].Bpm : Timings[i].Bpm;
					TimingPosition[i + 1] = startPosition + (endTiming - startTiming) * bpm / BaseBpm * Velocity;
				}
				for (int i = currentTimingId; i >= 0; i--)
				{
					int startTiming = i == currentTimingId ? currentTiming : Timings[i + 1].Timing;
					int endTiming = Timings[i].Timing;
					float startPosition = i == currentTimingId ? 0 : TimingPosition[i + 1];
					TimingPosition[i] = startPosition + (endTiming - startTiming) * Timings[i].Bpm / BaseBpm * Velocity;
				}
				earliestRenderTime = float.PositiveInfinity;
				latestRenderTime = float.NegativeInfinity;
				int allBeginTime = -ArcAudioManager.Instance.AudioOffset;
				float allBeginPosition = TimingPosition[0] + (allBeginTime - Timings[0].Timing) * Timings[0].Bpm / BaseBpm * Velocity;
				int allEndTime = ArcGameplayManager.Instance.Length - ArcAudioManager.Instance.AudioOffset;
				float allEndPosition = TimingPosition[Timings.Count - 1] + (allEndTime - Timings[Timings.Count - 1].Timing) * Timings[Timings.Count - 1].Bpm / BaseBpm * Velocity;

				for (int i = -1; i < Timings.Count; i++)
				{
					int startTime = i == -1 ? allBeginTime : Timings[i].Timing;
					int finishTime = i + 1 == Timings.Count ? allEndTime : Timings[i + 1].Timing;
					float startPosition = i == -1 ? allBeginPosition : TimingPosition[i];
					float finishPosition = i + 1 == Timings.Count ? allEndPosition : TimingPosition[i + 1];
					if (finishTime < startTime)
					{
						continue;
					}
					if (startPosition > farPosition && finishPosition > farPosition)
					{
						continue;
					}
					if (startPosition < nearPosition && finishPosition < nearPosition)
					{
						continue;
					}
					float nearTime;
					float farTime;
					if (Mathf.Approximately(startPosition, finishPosition))
					{
						nearTime = startTime;
						farTime = finishTime;
					}
					else
					{
						nearTime = Mathf.Lerp(startTime, finishTime, Mathf.InverseLerp(startPosition, finishPosition, nearPosition));
						farTime = Mathf.Lerp(startTime, finishTime, Mathf.InverseLerp(startPosition, finishPosition, farPosition));
					}
					earliestRenderTime = Mathf.Min(earliestRenderTime, nearTime, farTime);
					latestRenderTime = Mathf.Max(latestRenderTime, nearTime, farTime);
				}

				if (Timings[0].Bpm == 0)
				{
					if (allBeginPosition <= farPosition && allBeginPosition >= nearPosition)
					{
						earliestRenderTime = Mathf.Min(earliestRenderTime, allBeginTime, float.NegativeInfinity);
						latestRenderTime = Mathf.Max(latestRenderTime, allBeginTime, float.NegativeInfinity);
					}
				}
				else
				{
					float beforeAllBeginTargetPosition = Timings[0].Bpm > 0 ? nearPosition : farPosition;
					float beforeAllBeginTime = allBeginTime + (beforeAllBeginTargetPosition - allBeginPosition) / (Timings[0].Bpm / BaseBpm * Velocity);
					if (beforeAllBeginTime <= allBeginTime)
					{
						earliestRenderTime = Mathf.Min(earliestRenderTime, allBeginTime, beforeAllBeginTime);
						latestRenderTime = Mathf.Max(latestRenderTime, allBeginTime, beforeAllBeginTime);
					}
				}

				if (Timings[Timings.Count - 1].Bpm == 0)
				{
					if (allEndPosition <= farPosition && allEndPosition >= nearPosition)
					{
						earliestRenderTime = Mathf.Min(earliestRenderTime, allEndTime, float.PositiveInfinity);
						latestRenderTime = Mathf.Max(latestRenderTime, allEndTime, float.PositiveInfinity);
					}
				}
				else
				{
					float afterAllEndTargetPosition = Timings[Timings.Count - 1].Bpm > 0 ? farPosition : nearPosition;
					float afterAllEndTime = allEndTime + (afterAllEndTargetPosition - allEndPosition) / (Timings[Timings.Count - 1].Bpm / BaseBpm * Velocity);
					if (afterAllEndTime >= allEndTime)
					{
						earliestRenderTime = Mathf.Min(earliestRenderTime, allEndTime, afterAllEndTime);
						latestRenderTime = Mathf.Max(latestRenderTime, allEndTime, afterAllEndTime);
					}
				}

				earliestRenderTime = Mathf.Min(earliestRenderTime, currentTiming);
				latestRenderTime = Mathf.Max(latestRenderTime, currentTiming);
				earliestRenderTime += ArcAudioManager.Instance.AudioOffset;
				latestRenderTime += ArcAudioManager.Instance.AudioOffset;
			}
			if (timingGroup == null)
			{
				this.earliestRenderTime = earliestRenderTime;
				this.latestRenderTime = latestRenderTime;
			}
			else
			{
				timingGroup.earliestRenderTime = earliestRenderTime;
				timingGroup.latestRenderTime = latestRenderTime;
			}
		}
		private void UpdateBeatline()
		{
			int index = 0;
			int offset = ArcAudioManager.Instance.AudioOffset;
			foreach (float t in beatlineTimings)
			{
				if (!ShouldTryRender((int)(t + offset), null, 0, false))
				{
					continue;
				}
				float pos = CalculatePositionByTiming((int)(t + offset), null);
				if (pos > 100000 || pos < -100000)
				{
					continue;
				}
				SpriteRenderer s = GetBeatlineInstance(index);
				s.enabled = true;
				float z = pos / 1000f;
				s.transform.localPosition = new Vector3(0, 0, -z);
				s.transform.localScale = new Vector3(1700 * (1 + BeatlineEnwidenRatio * 0.5f), 20 + z);
				index++;
			}
			HideExceededBeatlineInstance(index);
		}
		private void UpdateTrackSpeed()
		{
			phase += 3f * (ArcGameplayManager.Instance.IsPlaying ? CurrentSpeed : 0) * Time.deltaTime;
			phase -= Mathf.Floor(phase);
			foreach (var renderer in TrackComponentRenderers)
			{
				renderer.sharedMaterial.SetFloat(phaseShaderId, phase);
			}
		}

		public void ReOrderTimingGroup(ArcTimingGroup timingGroup)
		{
			var Timings = GetTiming(timingGroup);
			var timings = Timings.OrderBy((timing) => timing.Timing).ToList();
			Timings.Clear();
			Timings.AddRange(timings);
		}
		public void AddTiming(ArcTiming newTiming, ArcTimingGroup timingGroup)
		{
			var Timings = GetTiming(timingGroup);
			Timings.Add(newTiming);
			ReOrderTimingGroup(timingGroup);
			OnTimingChange(timingGroup);
		}
		public void RemoveTiming(ArcTiming timing, ArcTimingGroup timingGroup)
		{
			GetTiming(timingGroup).Remove(timing);
			OnTimingChange(timingGroup);
		}
		public void UpdateTimingGroup(ArcTimingGroup timingGroup)
		{
			ReOrderTimingGroup(timingGroup);
			OnTimingChange(timingGroup);
		}
		public void OnTimingChange(ArcTimingGroup timingGroup)
		{
			if (timingGroup == null)
			{
				CalculateBeatlineTimes();
			}
			AdeTimingEditor.Instance.ForceUpdateTimingGroup(timingGroup);
		}
		public void AddTimingGroup(ArcTimingGroup timingGroup)
		{
			if (timingGroup.Id > timingGroups.Count + 1)
			{
				timingGroup.Id = timingGroups.Count + 1;
			}
			if (timingGroup.Id < 1)
			{
				timingGroup.Id = 1;
			}
			int pos = timingGroup.Id - 1;
			timingGroups.Insert(pos, timingGroup);
			for (int i = pos + 1; i < timingGroups.Count; i++)
			{
				timingGroups[i].Id = i + 1;
			}
			OnTimingGroupChange();
		}
		public void RemoveTimingGroup(ArcTimingGroup timingGroup)
		{
			int pos = timingGroup.Id - 1;
			timingGroups.Remove(timingGroup);
			for (int i = pos; i < timingGroups.Count; i++)
			{
				timingGroups[i].Id = i + 1;
			}
			OnTimingGroupChange();
		}
		public void OnTimingGroupChange()
		{
			AdeTimingEditor.Instance.ForceUpdateAll();
		}
		// Note: this is a function used to optimize rendering by avoid not needed position calculation
		// Invoker should manually check position again after this check passed
		public bool ShouldTryRender(int timing, ArcTimingGroup timingGroup, int duration = 0, bool note = true)
		{
			float earliestRenderTime;
			float latestRenderTime;
			if (timingGroup == null)
			{
				earliestRenderTime = this.earliestRenderTime;
				latestRenderTime = this.latestRenderTime;
			}
			else
			{
				earliestRenderTime = timingGroup.earliestRenderTime;
				latestRenderTime = timingGroup.latestRenderTime;
			}
			if (timing + duration >= earliestRenderTime && timing <= latestRenderTime)
			{
				if (note && timing + duration + LostDelay < ArcGameplayManager.Instance.Timing)
				{
					return false;
				}
				return true;
			}
			return false;
		}
	}
}
