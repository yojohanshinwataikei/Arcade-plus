using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arcade.Gameplay;
using Arcade.Util.UnityExtension;
using System.Linq;
using Arcade.Compose.MarkingMenu;
using Newtonsoft.Json;
using Arcade.Compose.Editing;
using System.Globalization;

namespace Arcade.Compose
{
	[Serializable]
	public class CustomGrid
	{
		public string X = "-0.5>1.5/0.25";
		public string Y = "0>1/0.2;0.5";
	}
	public struct BeatlineProperty
	{
		public int Timing;
		public int Importance;
	}

	public class AdeGridManager : AdeMarkingMenuItemProvider
	{
		public static AdeGridManager Instance { get; private set; }

		public GameObject BeatlinePrefab, VerticalPrefab;
		public Transform VerticalPanel;
		public float BeatlineDensity;
		public Color[] BeatlineColors;
		public Color VerticalXColor, VerticalYColor;
		public InputField DensityInputField, VerticalXInputField, VerticalYInputField;
		public MarkingMenuItem[] Entry;

		public string PreferencesSavePath
		{
			get
			{
				return ArcadeComposeManager.ArcadePersistentFolder + "/Grid.json";
			}
		}

		private int beatlineInUse;
		private int verticalInUse;

		private List<BeatlineProperty> beatlineTimings = new List<BeatlineProperty>();
		private List<int> beatTimings = new List<int>();
		private List<int> measureTimings = new List<int>();
		private List<float> verticalXPositions = new List<float>();
		private List<float> verticalYPositions = new List<float>();

		private class RenderingBeatlineInfo
		{
			public LineRenderer renderer;
			public int timing;
		};

		private List<RenderingBeatlineInfo> beatlineInstances = new List<RenderingBeatlineInfo>();
		private List<LineRenderer> verticalInstances = new List<LineRenderer>();

		private CustomGrid customGrid;

		private void Awake()
		{
			Instance = this;
			LoadCustomGrids();
		}
		private void Start()
		{
			ArcGameplayManager.Instance.OnChartLoad.AddListener(ReBuildBeatline);
		}
		private void Update()
		{
			if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ToggleGrid))
			{
				SwitchGridStatus();
			}
			if (!ArcGameplayManager.Instance.IsLoaded)
			{
				beatlineInUse = 0;
				verticalInUse = 0;
				HideExceededBeatlineInstance();
				HideExceededVerticalInstance();
				return;
			}
			UpdateBeatline();
			UpdateVertical();
		}
		private void OnDestroy()
		{
			ArcGameplayManager.Instance.OnChartLoad.RemoveListener(ReBuildBeatline);
			SaveCustomGrids();
		}

		public bool Enable
		{
			get
			{
				return EnableBeatline & EnableVertical;
			}
			set
			{
				EnableBeatline = value;
				EnableVertical = value;
			}
		}
		public bool EnableBeatline { get; set; }
		public bool EnableVertical { get; set; }

		public override bool IsOnlyMarkingMenu
		{
			get
			{
				return false;
			}
		}
		public override MarkingMenuItem[] MarkingMenuItems
		{
			get
			{
				return Entry;
			}
		}

		private void HideExceededBeatlineInstance()
		{
			int count = beatlineInstances.Count;
			while (count > beatlineInUse)
			{
				beatlineInstances[count - 1].renderer.enabled = false;
				count--;
			}
		}
		private void HideExceededVerticalInstance()
		{
			int count = verticalInstances.Count;
			while (count > verticalInUse)
			{
				verticalInstances[count - 1].enabled = false;
				count--;
			}
		}

		private LineRenderer GetBeatlineInstance(int timing)
		{
			while (beatlineInstances.Count < beatlineInUse + 1)
			{
				beatlineInstances.Add(new RenderingBeatlineInfo
				{
					renderer = Instantiate(BeatlinePrefab, transform).GetComponent<LineRenderer>(),
					timing = timing,
				});
			}
			beatlineInstances[beatlineInUse].timing = timing;
			return beatlineInstances[beatlineInUse++].renderer;
		}
		private LineRenderer GetVerticalInstance()
		{
			while (verticalInstances.Count < verticalInUse + 1)
			{
				verticalInstances.Add(Instantiate(VerticalPrefab, VerticalPanel).GetComponent<LineRenderer>());
			}
			return verticalInstances[verticalInUse++];
		}

		private void CalculateBeatlineTimes()
		{
			beatlineTimings.Clear();
			HideExceededBeatlineInstance();
			var Timings = ArcTimingManager.Instance.GetTiming(AdeTimingEditor.Instance.currentTimingGroup);

			for (int i = 0; i < Timings.Count; ++i)
			{
				int nextTiming = i + 1 >= Timings.Count ? ArcGameplayManager.Instance.Length : Timings[i + 1].Timing;
				float segment = Timings[i].Bpm == 0 ? (nextTiming - Timings[i].Timing) : (60000 / Mathf.Abs(Timings[i].Bpm) / BeatlineDensity);
				if (segment < 1 || float.IsNaN(segment)) segment = Math.Max(nextTiming - Timings[i].Timing, 1);
				int primaryCount = 0;
				while (true)
				{
					float j = Timings[i].Timing + primaryCount * segment;
					// Unlike timing line, we plus one here to avoid the 1ms time error
					if (j + 1 >= nextTiming)
						break;
					int importance = CalculateImportance(primaryCount, BeatlineDensity);
					primaryCount++;
					if (beatlineTimings.Count > 0)
					{
						var last = beatlineTimings[beatlineTimings.Count - 1];
						if (last.Timing == j)
						{
							last.Importance = Mathf.Min(last.Importance, importance);
							continue;
						}
					}
					beatlineTimings.Add(new BeatlineProperty() { Timing = Mathf.RoundToInt(j), Importance = importance });
				}
			}
			if (Timings.Count > 0)
			{
				var firstTiming = Timings[0];
				int primaryCount = 1;
				float segment = firstTiming.Bpm == 0 ? (firstTiming.Timing) : (60000 / Mathf.Abs(firstTiming.Bpm) / BeatlineDensity);
				if (segment < 1 || float.IsNaN(segment)) segment = Math.Max(firstTiming.Timing, 1);
				while (true)
				{
					float j = firstTiming.Timing - primaryCount * segment;
					if (j < 0)
						break;
					int importance = CalculateImportance(primaryCount, BeatlineDensity);
					primaryCount++;
					if (beatlineTimings.Count > 0)
					{
						var first = beatlineTimings[0];
						if (first.Timing == j)
						{
							first.Importance = Mathf.Min(first.Importance, importance);
							continue;
						}
					}
					beatlineTimings.Insert(0, new BeatlineProperty() { Timing = Mathf.RoundToInt(j), Importance = importance });
				}
			}
		}

		private void CalculateBeatTimes()
		{
			beatTimings.Clear();
			var Timings = ArcTimingManager.Instance.GetTiming(AdeTimingEditor.Instance.currentTimingGroup);

			for (int i = 0; i < Timings.Count; ++i)
			{
				int nextTiming = i + 1 >= Timings.Count ? ArcGameplayManager.Instance.Length : Timings[i + 1].Timing;
				float segment = Timings[i].Bpm == 0 ? (nextTiming - Timings[i].Timing) : (60000 / Mathf.Abs(Timings[i].Bpm));
				if (segment < 1 || float.IsNaN(segment)) segment = Math.Max(nextTiming - Timings[i].Timing, 1);
				int primaryCount = 0;
				while (true)
				{
					float j = Timings[i].Timing + primaryCount * segment;
					// Unlike timing line, we plus one here to avoid the 1ms time error
					if (j + 1 >= nextTiming)
						break;
					primaryCount++;
					if (beatTimings.Count > 0)
					{
						var last = beatTimings[beatTimings.Count - 1];
						if (last == j)
						{
							continue;
						}
					}
					beatTimings.Add(Mathf.RoundToInt(j));
				}
			}
			if (Timings.Count > 0)
			{
				var firstTiming = Timings[0];
				int primaryCount = 1;
				float segment = firstTiming.Bpm == 0 ? (firstTiming.Timing) : (60000 / Mathf.Abs(firstTiming.Bpm));
				if (segment < 1 || float.IsNaN(segment)) segment = Math.Max(firstTiming.Timing, 1);
				while (true)
				{
					float j = firstTiming.Timing - primaryCount * segment;
					if (j < 0)
						break;
					primaryCount++;
					if (beatTimings.Count > 0)
					{
						var first = beatTimings[0];
						if (first == j)
						{
							continue;
						}
					}
					beatTimings.Insert(0, Mathf.RoundToInt(j));
				}
			}
		}

		private void CalculateMeasureTimes()
		{
			measureTimings.Clear();
			var Timings = ArcTimingManager.Instance.GetTiming(AdeTimingEditor.Instance.currentTimingGroup);

			for (int i = 0; i < Timings.Count; ++i)
			{
				int nextTiming = i + 1 >= Timings.Count ? ArcGameplayManager.Instance.Length : Timings[i + 1].Timing;
				float segment = Timings[i].Bpm == 0 ? (nextTiming - Timings[i].Timing) : (60000 / Mathf.Abs(Timings[i].Bpm) * Timings[i].BeatsPerLine);
				if (segment < 1 || float.IsNaN(segment)) segment = Math.Max(nextTiming - Timings[i].Timing, 1);
				int primaryCount = 0;
				while (true)
				{
					float j = Timings[i].Timing + primaryCount * segment;
					// Unlike timing line, we plus one here to avoid the 1ms time error
					if (j + 1 >= nextTiming)
						break;
					primaryCount++;
					if (measureTimings.Count > 0)
					{
						var last = measureTimings[measureTimings.Count - 1];
						if (last == j)
						{
							continue;
						}
					}
					measureTimings.Add(Mathf.RoundToInt(j));
				}
			}
			if (Timings.Count > 0)
			{
				var firstTiming = Timings[0];
				int primaryCount = 1;
				float segment = firstTiming.Bpm == 0 ? (firstTiming.Timing) : (60000 / Mathf.Abs(firstTiming.Bpm) * firstTiming.BeatsPerLine);
				if (segment < 1 || float.IsNaN(segment)) segment = Math.Max(firstTiming.Timing, 1);
				while (true)
				{
					float j = firstTiming.Timing - primaryCount * segment;
					if (j < 0)
						break;
					primaryCount++;
					if (measureTimings.Count > 0)
					{
						var first = measureTimings[0];
						if (first == j)
						{
							continue;
						}
					}
					measureTimings.Insert(0, Mathf.RoundToInt(j));
				}
			}
		}

		private int CalculateImportance(int primaryCount, float BeatlineDensity)
		{

			if (Mathf.Approximately(primaryCount % BeatlineDensity, 0))
			{
				return 0;
			}
			if (Mathf.Approximately(primaryCount % (BeatlineDensity / 2), 0))
			{
				return 1;
			}
			if (Mathf.Approximately(primaryCount % (BeatlineDensity / 4), 0))
			{
				return 2;
			}
			if (Mathf.Approximately(primaryCount % (BeatlineDensity / 6), 0))
			{
				return 3;
			}
			if (Mathf.Approximately(primaryCount % (BeatlineDensity / 8), 0))
			{
				return 4;
			}
			return 5;
		}

		private void ParseVerticals()
		{
			verticalXPositions = Parse(customGrid.X);
			verticalYPositions = Parse(customGrid.Y);
		}
		private List<float> Parse(string str)
		{
			List<float> ret = new List<float>();
			try
			{
				string[] ss = str.Split(';');
				foreach (var s in ss)
				{
					if (s.Contains('>') && s.Contains('/'))
					{
						int v1 = s.IndexOf('>');
						int v2 = s.IndexOf('/');
						float start = float.Parse(s.Substring(0, v1), CultureInfo.InvariantCulture);
						float end = float.Parse(s.Substring(v1 + 1, v2 - v1 - 1), CultureInfo.InvariantCulture);
						float interval = float.Parse(s.Substring(v2 + 1, s.Length - v2 - 1), CultureInfo.InvariantCulture);
						for (float i = start; i <= end; i += interval)
						{
							ret.Add(i);
						}
					}
					else
					{
						ret.Add(float.Parse(s, CultureInfo.InvariantCulture));
					}
				}
			}
			catch (Exception)
			{

			}
			return ret;
		}

		private void UpdateBeatline()
		{

			var timingGroup = AdeTimingEditor.Instance.currentTimingGroup;
			float xEdgePos = 8.5f * (1 + ArcTimingManager.Instance.BeatlineEnwidenRatio * 0.5f);
			beatlineInUse = 0;
			if (EnableBeatline)
			{
				foreach (var t in beatlineTimings)
				{
					if (!ArcTimingManager.Instance.ShouldTryRender(((int)t.Timing), timingGroup, 0, false))
					{
						continue;
					}
					float pos = ArcTimingManager.Instance.CalculatePositionByTiming((int)t.Timing, timingGroup);
					if (pos > 100000 || pos < -100000)
					{
						continue;
					}
					float z = pos / 1000f;
					LineRenderer l = GetBeatlineInstance(t.Timing);
					l.enabled = true;
					l.DrawLine(new Vector3(-xEdgePos, -z), new Vector3(xEdgePos, -z));
					l.endColor = l.startColor = BeatlineColors[t.Importance];
					l.endWidth = l.startWidth = 0.2f - 0.021f * t.Importance;
				}
			}
			HideExceededBeatlineInstance();
		}
		private void UpdateVertical()
		{
			verticalInUse = 0;
			float xEdgePos = 8.5f * (1 + ArcTimingManager.Instance.BeatlineEnwidenRatio * 0.5f);
			float yEdgePos = 5.5f + ArcCameraManager.Instance.EnwidenRatio * 2.745f;
			if (EnableVertical && AdeCursorManager.Instance.WallPanelEnabled)
			{
				foreach (var t in verticalXPositions)
				{
					LineRenderer l = GetVerticalInstance();
					l.DrawLine(new Vector3(ArcAlgorithm.ArcXToWorld(t), 0), new Vector3(ArcAlgorithm.ArcXToWorld(t), yEdgePos));
					l.endColor = l.startColor = VerticalXColor;
					l.enabled = true;
				}
				foreach (var t in verticalYPositions)
				{
					LineRenderer l = GetVerticalInstance();
					l.DrawLine(new Vector3(-xEdgePos, ArcAlgorithm.ArcYToWorld(t)), new Vector3(xEdgePos, ArcAlgorithm.ArcYToWorld(t)));
					l.endColor = l.startColor = VerticalYColor;
					l.enabled = true;
				}
			}
			HideExceededVerticalInstance();
		}

		public int AttachBeatlineTimingFromPos(float z)
		{
			var timingGroup = AdeTimingEditor.Instance.currentTimingGroup;
			int defaultTiming = ArcTimingManager.Instance.CalculateChartTimingByPosition(-z * 1000, timingGroup);
			if (!EnableBeatline) return defaultTiming;
			if (beatlineInUse == 0) return defaultTiming;
			List<float> deltas = new List<float>();
			for (int i = 0; i < beatlineInUse; ++i)
			{
				float lz = beatlineInstances[i].renderer.GetPosition(0).y;
				deltas.Add(Mathf.Abs(lz - z));
			}
			int index = deltas.IndexOf(deltas.Min());
			if (index < 0)
			{
				return defaultTiming;
			}
			float tz = beatlineInstances[index].renderer.GetPosition(0).y;
			int t = beatlineInstances[index].timing;
			if (Mathf.Abs(tz - z) < 5f) return t;
			return defaultTiming;
		}
		public float AttachVerticalX(float x)
		{
			if (verticalInUse == 0) return x;
			if (verticalXPositions.Count == 0) return x;
			List<float> deltas = new List<float>();
			for (int i = 0; i < verticalXPositions.Count; ++i)
			{
				deltas.Add(Mathf.Abs(verticalXPositions[i] - x));
			}
			int index = deltas.IndexOf(deltas.Min());
			if (deltas[index] < 0.3f) return verticalXPositions[index];
			else return x;
		}
		public float AttachVerticalY(float y)
		{
			if (verticalInUse == 0) return y;
			if (verticalYPositions.Count == 0) return y;
			List<float> deltas = new List<float>();
			for (int i = 0; i < verticalYPositions.Count; ++i)
			{
				deltas.Add(Mathf.Abs(verticalYPositions[i] - y));
			}
			int index = deltas.IndexOf(deltas.Min());
			if (deltas[index] < 0.3f) return verticalYPositions[index];
			else return y;
		}
		public int AttachScroll(int t, float scroll)
		{
			if (beatlineTimings.Count == 0) return Mathf.RoundToInt(t + 50 * scroll);
			List<int> deltas = new List<int>();
			for (int i = 0; i < beatlineTimings.Count; ++i)
			{
				deltas.Add(Mathf.Abs(beatlineTimings[i].Timing - t));
			}
			int index = deltas.IndexOf(deltas.Min());
			if (deltas[index] <= 1f || ArcGameplayManager.Instance.IsPlaying)
			{
				index += (scroll > 0 ? 1 : -1);
			}
			else if (t > beatlineTimings[index].Timing)
			{
				index += (scroll > 0 ? 1 : 0);
			}
			else
			{
				index += (scroll > 0 ? 0 : -1);
			}
			if (index >= deltas.Count) return Mathf.Max(t, beatlineTimings[deltas.Count - 1].Timing);
			else if (index < 0) return Mathf.Min(t, beatlineTimings[0].Timing);
			return beatlineTimings[index].Timing;
		}
		public int AttachBeatScroll(int t, float scroll)
		{
			if (beatTimings.Count == 0) return Mathf.RoundToInt(t + 200 * scroll);
			List<int> deltas = new List<int>();
			for (int i = 0; i < beatTimings.Count; ++i)
			{
				deltas.Add(Mathf.Abs(beatTimings[i] - t));
			}
			int index = deltas.IndexOf(deltas.Min());
			if (deltas[index] <= 1f || ArcGameplayManager.Instance.IsPlaying)
			{
				index += (scroll > 0 ? 1 : -1);
			}
			else if (t > beatTimings[index])
			{
				index += (scroll > 0 ? 1 : 0);
			}
			else
			{
				index += (scroll > 0 ? 0 : -1);
			}
			if (index >= deltas.Count) return Mathf.Max(t, beatTimings[deltas.Count - 1]);
			else if (index < 0) return Mathf.Min(t, beatTimings[0]);
			return beatTimings[index];
		}
		public int AttachMeasureScroll(int t, float scroll)
		{
			if (measureTimings.Count == 0) return Mathf.RoundToInt(t + 800 * scroll);
			List<int> deltas = new List<int>();
			for (int i = 0; i < measureTimings.Count; ++i)
			{
				deltas.Add(Mathf.Abs(measureTimings[i] - t));
			}
			int index = deltas.IndexOf(deltas.Min());
			if (deltas[index] <= 1f || ArcGameplayManager.Instance.IsPlaying)
			{
				index += (scroll > 0 ? 1 : -1);
			}
			else if (t > measureTimings[index])
			{
				index += (scroll > 0 ? 1 : 0);
			}
			else
			{
				index += (scroll > 0 ? 0 : -1);
			}
			if (index >= deltas.Count) return Mathf.Max(t, measureTimings[deltas.Count - 1]);
			else if (index < 0) return Mathf.Min(t, measureTimings[0]);
			return measureTimings[index];
		}
		public int AttachTiming(int t)
		{
			if (beatlineTimings.Count == 0) return t;
			List<int> deltas = new List<int>();
			for (int i = 0; i < beatlineTimings.Count; ++i)
			{
				deltas.Add(Mathf.Abs(beatlineTimings[i].Timing - t));
			}
			int index = deltas.IndexOf(deltas.Min());
			return beatlineTimings[index].Timing;
		}

		public void ReBuildBeatline()
		{
			CalculateBeatlineTimes();
			CalculateBeatTimes();
			CalculateMeasureTimes();
		}
		private void LoadCustomGrids()
		{
			try
			{
				if (File.Exists(PreferencesSavePath))
				{
					PlayerPrefs.SetString("AdeGridManager", File.ReadAllText(PreferencesSavePath));
					File.Delete(PreferencesSavePath);
				}
				customGrid = JsonConvert.DeserializeObject<CustomGrid>(PlayerPrefs.GetString("AdeGridManager", ""));
				if (customGrid == null) customGrid = new CustomGrid();
			}
			catch (Exception)
			{
				customGrid = new CustomGrid();
			}
			finally
			{
				VerticalXInputField.text = customGrid.X;
				VerticalYInputField.text = customGrid.Y;
				ParseVerticals();
			}
		}
		private void SaveCustomGrids()
		{
			PlayerPrefs.SetString("AdeGridManager", JsonConvert.SerializeObject(customGrid));
		}

		public void SwitchGridStatus()
		{
			Enable = !Enable;
		}
		public void OnDensityChange()
		{
			float density;
			if (!float.TryParse(DensityInputField.text, NumberStyles.Float, CultureInfo.InvariantCulture, out density)) return;
			if (density <= 0 || density > 64) return;
			BeatlineDensity = density;
			CalculateBeatlineTimes();
		}
		public void OnVerticalXChange()
		{
			List<float> ret = Parse(VerticalXInputField.text);
			if (ret == null)
			{
				VerticalXInputField.text = customGrid.X;
				return;
			}
			verticalXPositions = ret;
			customGrid.X = VerticalXInputField.text;
			SaveCustomGrids();
		}
		public void OnVerticalYChange()
		{
			List<float> ret = Parse(VerticalYInputField.text);
			if (ret == null)
			{
				VerticalYInputField.text = customGrid.Y;
				return;
			}
			verticalYPositions = ret;
			customGrid.Y = VerticalYInputField.text;
			SaveCustomGrids();
		}
	}
}
