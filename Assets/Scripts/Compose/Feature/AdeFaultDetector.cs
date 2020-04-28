using Arcade.Compose;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using System.Collections.Generic;
using UnityEngine;
using Arcade.Aff.Faults;
using System.Linq;
using UnityEngine.UI;
using System.IO;

namespace Arcade.Aff.Faults
{
	public abstract class Fault
    {
        public Fault(string Reason)
        {
            this.Reason = Reason;
        }
        public List<ArcEvent> Faults = new List<ArcEvent>();
        public string Reason;

        public abstract void Check(ArcChart chart);
    }

    public class ShortHoldFault : Fault
    {
        public ShortHoldFault() : base("Hold 持续时间 <= 0ms")
        {

        }
        public override void Check(ArcChart chart)
        {
            foreach (var h in chart.Holds)
            {
                if (h.EndTiming <= h.Timing)
                {
                    Faults.Add(h);
                }
            }
            Faults = Faults.Distinct().ToList();
        }
    }
    public class ShortArcFault : Fault
    {
        public ShortArcFault() : base("Arc 持续时间 <= 0ms 且坐标相同")
        {

        }
        public override void Check(ArcChart chart)
        {
            foreach (var a in chart.Arcs)
            {
                if (a.EndTiming <= a.Timing)
                {
                    if (a.XEnd == a.XStart)
                    {
                        if (a.YEnd == a.YStart)
                        {
                            Faults.Add(a);
                        }
                    }
                }
            }
            Faults = Faults.Distinct().ToList();
        }
    }
    public class TileTapFault : Fault
    {
        public TileTapFault() : base("Tap 重叠")
        {

        }
        public override void Check(ArcChart chart)
        {
            for (int i = 0; i < chart.Taps.Count - 1; ++i)
            {
                for (int k = i + 1; k < chart.Taps.Count; ++k)
                {
                    if (chart.Taps[i].Timing == chart.Taps[k].Timing
                        && chart.Taps[i].Track == chart.Taps[k].Track)
                    {
                        Faults.Add(chart.Taps[i]);
                    }
                }
            }
            Faults = Faults.Distinct().ToList();
        }
    }
    public class TileArcTapFault : Fault
    {
        public TileArcTapFault() : base("ArcTap 重叠")
        {

        }
        public override void Check(ArcChart chart)
        {
            List<(ArcArcTap, float, float)> ats = new List<(ArcArcTap, float, float)>();
            foreach (var arc in chart.Arcs)
            {
                foreach (var at in arc.ArcTaps)
                {
                    float t = 1f * (at.Timing - arc.Timing) / (arc.EndTiming - arc.Timing);
                    float x = ArcAlgorithm.X(arc.XStart, arc.XEnd, t, arc.LineType);
                    float y = ArcAlgorithm.Y(arc.YStart, arc.YEnd, t, arc.LineType);
                    ats.Add((at, x, y));
                }
            }
            for (int i = 0; i < ats.Count - 1; ++i)
            {
                for (int k = i + 1; k < ats.Count; ++k)
                {
                    if (ats[i].Item2 == ats[k].Item2
                        && ats[i].Item3 == ats[k].Item3
                        && ats[i].Item1.Timing == ats[k].Item1.Timing)
                    {
                        Faults.Add(ats[i].Item1);
                    }
                }
            }
            Faults = Faults.Distinct().ToList();
        }
    }
    public class TileTapHoldFault : Fault
    {
        public TileTapHoldFault() : base("Hold 与 Tap 重叠")
        {

        }
        public override void Check(ArcChart chart)
        {
            foreach (var h in chart.Holds)
            {
                foreach (var t in chart.Taps)
                {
                    if (t.Timing >= h.Timing && t.Timing <= h.EndTiming
                        && t.Track == h.Track)
                    {
                        Faults.Add(t);
                    }
                }
            }
            Faults = Faults.Distinct().ToList();
        }
    }
    public class TileHoldFault : Fault
    {
        public TileHoldFault() : base("Hold 重叠")
        {

        }
        public override void Check(ArcChart chart)
        {
            for (int i = 0; i < chart.Holds.Count - 1; ++i)
            {
                for (int k = i + 1; k < chart.Holds.Count; ++k)
                {
                    if (chart.Holds[k].Timing <= chart.Holds[i].EndTiming
                        && chart.Holds[k].Timing >= chart.Holds[i].Timing
                        && chart.Holds[i].Track == chart.Holds[k].Track)
                    {
                        Faults.Add(chart.Holds[k]);
                    }
                }
            }
            Faults = Faults.Distinct().ToList();
        }
    }
    public class CrossTimingFault : Fault
    {
        public CrossTimingFault() : base("跨越 Timing")
        {

        }
        public override void Check(ArcChart chart)
        {
            foreach (var h in chart.Holds)
            {
                foreach (var t in chart.Timings)
                {
                    if (t.Timing > h.Timing && t.Timing < h.EndTiming)
                    {
                        Faults.Add(h);
                    }
                }
            }
            foreach (var a in chart.Arcs)
            {
                foreach (var t in chart.Timings)
                {
                    if (t.Timing > a.Timing && t.Timing < a.EndTiming)
                    {
                        Faults.Add(a);
                    }
                }
            }
            Faults = Faults.Distinct().ToList();
        }
    }
}

public class AdeFaultDetector : MonoBehaviour
{
    public Text Status;
    public void OnInvoke()
    {
        if (!ArcGameplayManager.Instance.IsLoaded)
        {
            AdeToast.Instance.Show("未加载工程");
            return;
        }

        Status.text = "请点击检查";

        ArcChart chart = ArcGameplayManager.Instance.Chart;
        Fault[] checks = new Fault[] {new ShortHoldFault(), new ShortArcFault(), new TileTapFault(), new TileArcTapFault(), new TileTapHoldFault(),
                                      new TileHoldFault(), new CrossTimingFault()};
        string path = AdeProjectManager.Instance.CurrentProjectFolder + "/Arcade/ChartFault.txt";
        FileStream fs = new FileStream(path, FileMode.Create);
        StreamWriter sw = new StreamWriter(fs);
        int count = 0;
        sw.WriteLine("错误报告");
        sw.WriteLine("\t时间(原始)为aff中定义的Timing，便于在aff中搜索");
        sw.WriteLine("\t时间(偏移)为偏移后时间，便于在Arcade的时间框中直接跳转定位");
        sw.WriteLine();
        foreach (var c in checks)
        {
            c.Check(chart);
            if (c.Faults.Count > 0)
            {
                sw.WriteLine(c.Reason);
                foreach (var f in c.Faults)
                {
                    sw.WriteLine($"\t时间(原始):{f.Timing}\t时间(偏移):{f.Timing + ArcAudioManager.Instance.AudioOffset}");
                }
                count += c.Faults.Count;
            }
        }

        sw.Close();
        Status.text = $"检查完成，共 {count} 个错误";
		Arcade.Util.Windows.Dialog.OpenExplorer(path);
    }
}

