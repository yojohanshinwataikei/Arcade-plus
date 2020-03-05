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
        public float Timing;
        public int Importance;
    }

    public class AdeGridManager : MonoBehaviour, IMarkingMenuItemProvider
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
        private List<float> verticalXTimings = new List<float>();
        private List<float> verticalYTimings = new List<float>();

        private List<LineRenderer> beatlineInstances = new List<LineRenderer>();
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
            AdeMarkingMenuManager.Instance.Providers.Add(this);
        }
        private void Update()
        {
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
            AdeMarkingMenuManager.Instance.Providers.Remove(this);
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

        public bool IsOnly
        {
            get
            {
                return false;
            }
        }
        public MarkingMenuItem[] Items
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
                beatlineInstances[count - 1].enabled = false;
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

        private LineRenderer GetBeatlineInstance()
        {
            while (beatlineInstances.Count < beatlineInUse + 1)
            {
                beatlineInstances.Add(Instantiate(BeatlinePrefab, transform).GetComponent<LineRenderer>());
            }
            return beatlineInstances[beatlineInUse++];
        }
        private LineRenderer GetVerticalInstance()
        {
            while (verticalInstances.Count < verticalInUse + 1)
            {
                verticalInstances.Add(Instantiate(VerticalPrefab, VerticalPanel).GetComponent<LineRenderer>());
            }
            return verticalInstances[verticalInUse++];
        }

        private void AddBeatlineTiming(float timing, int importance)
        {
            var query = beatlineTimings.Where((b) => b.Timing == timing).ToArray();
            if (query.Length != 0)
            {
                query[0].Importance = Mathf.Min(query[0].Importance, importance);
            }
            else
            {
                beatlineTimings.Add(new BeatlineProperty() { Timing = timing, Importance = importance });
            }
        }
        private void CalculateBeatlineTimes()
        {
            beatlineTimings.Clear();
            HideExceededBeatlineInstance();
            var Timings = ArcTimingManager.Instance.Timings;

            for (int i = 0; i < Timings.Count; ++i)
            {
                int nextTiming=i+1>=Timings.Count?ArcGameplayManager.Instance.Length:Timings[i + 1].Timing;
                float segment = Timings[i].Bpm == 0 ? (nextTiming - Timings[i].Timing) : (60000 / Mathf.Abs(Timings[i].Bpm) / BeatlineDensity);
                if (segment == 0) continue;
                int primaryCount = 0;
                while (true)
                {
                    float j = Timings[i].Timing + primaryCount * segment;
                    // Unlike timing line, we plus one here to avoid the 1ms time error
                    if(j +1 >= nextTiming)
                        break;
                    AddBeatlineTiming(j, (primaryCount % BeatlineDensity == 0 ? 0 : (primaryCount % (BeatlineDensity / 2) == 0 ? 1 : (primaryCount % (BeatlineDensity / 4) == 0 ? 2 : 3))));
                    primaryCount++;
                }
            }
        }

        private void ParseVerticals()
        {
            verticalXTimings = Parse(customGrid.X);
            verticalYTimings = Parse(customGrid.Y);
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
                        float start = float.Parse(s.Substring(0, v1));
                        float end = float.Parse(s.Substring(v1 + 1, v2 - v1 - 1));
                        float interval = float.Parse(s.Substring(v2 + 1, s.Length - v2 - 1));
                        for (float i = start; i <= end; i += interval)
                        {
                            ret.Add(i);
                        }
                    }
                    else
                    {
                        ret.Add(float.Parse(s));
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
            beatlineInUse = 0;
            if (EnableBeatline)
            {
                int offset = ArcAudioManager.Instance.AudioOffset;
                foreach (var t in beatlineTimings)
                {
                    if (!ArcTimingManager.Instance.ShouldRender(((int)t.Timing + offset), 0))
                    {
                        continue;
                    }
                    LineRenderer l = GetBeatlineInstance();
                    l.enabled = true;
                    float z = ArcTimingManager.Instance.CalculatePositionByTiming(((int)t.Timing + offset)) / 1000f;
                    l.DrawLine(new Vector3(-8.5f, -z), new Vector3(8.5f, -z));
                    l.endColor = l.startColor = BeatlineColors[t.Importance];
                    l.endWidth = l.startWidth = 0.2f - 0.035f * t.Importance;
                }
            }
            HideExceededBeatlineInstance();
        }
        private void UpdateVertical()
        {
            verticalInUse = 0;
            if (EnableVertical && AdeCursorManager.Instance.EnableVerticalPanel)
            {
                foreach (var t in verticalXTimings)
                {
                    LineRenderer l = GetVerticalInstance();
                    l.DrawLine(new Vector3(ArcAlgorithm.ArcXToWorld(t), 0), new Vector3(ArcAlgorithm.ArcXToWorld(t), 5.5f));
                    l.endColor = l.startColor = VerticalXColor;
                    l.enabled = true;
                }
                foreach (var t in verticalYTimings)
                {
                    LineRenderer l = GetVerticalInstance();
                    l.DrawLine(new Vector3(-8.5f, ArcAlgorithm.ArcYToWorld(t)), new Vector3(8.5f, ArcAlgorithm.ArcYToWorld(t)));
                    l.endColor = l.startColor = VerticalYColor;
                    l.enabled = true;
                }
            }
            HideExceededVerticalInstance();
        }

        public float AttachBeatline(float z)
        {
            if (!EnableBeatline) return z;
            if (beatlineInUse == 0) return z;
            List<float> deltas = new List<float>();
            for (int i = 0; i < beatlineInUse; ++i)
            {
                float lz = beatlineInstances[i].GetPosition(0).y;
                deltas.Add(Mathf.Abs(lz - z));
            }
            int index = deltas.IndexOf(deltas.Min());
            float tz = beatlineInstances[index].GetPosition(0).y;
            if (Mathf.Abs(tz - z) < 5f) return tz;
            return z;
        }
        public float AttachVerticalX(float x)
        {
            if (verticalInUse == 0) return x;
            if (verticalXTimings.Count == 0) return x;
            List<float> deltas = new List<float>();
            for (int i = 0; i < verticalXTimings.Count; ++i)
            {
                deltas.Add(Mathf.Abs(verticalXTimings[i] - x));
            }
            int index = deltas.IndexOf(deltas.Min());
            if (deltas[index] < 0.3f) return verticalXTimings[index];
            else return x;
        }
        public float AttachVerticalY(float y)
        {
            if (verticalInUse == 0) return y;
            if (verticalYTimings.Count == 0) return y;
            List<float> deltas = new List<float>();
            for (int i = 0; i < verticalYTimings.Count; ++i)
            {
                deltas.Add(Mathf.Abs(verticalYTimings[i] - y));
            }
            int index = deltas.IndexOf(deltas.Min());
            if (deltas[index] < 0.3f) return verticalYTimings[index];
            else return y;
        }
        public float AttachScroll(float t, float scroll)
        {
            if (beatlineTimings.Count == 0) return t + 50 * scroll;
            List<float> deltas = new List<float>();
            for (int i = 0; i < beatlineTimings.Count; ++i)
            {
                deltas.Add(Mathf.Abs(beatlineTimings[i].Timing - t));
            }
            int index = deltas.IndexOf(deltas.Min());
            index += (scroll > 0 ? 1 : -1);
            if (index >= deltas.Count) index = 0;
            else if (index < 0) index = deltas.Count - 1;
            return beatlineTimings[index].Timing;
        }

        public void ReBuildBeatline()
        {
            CalculateBeatlineTimes();
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
            if (!float.TryParse(DensityInputField.text, out density)) return;
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
            verticalXTimings = ret;
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
            verticalYTimings = ret;
            customGrid.Y = VerticalYInputField.text;
            SaveCustomGrids();
        }
    }
}