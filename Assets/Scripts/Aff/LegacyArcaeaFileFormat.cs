using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Arcade.Aff;

namespace Arcade.Aff
{
	public class ArcaeaAffFormatException : Exception
	{
		public string Reason;
		public ArcaeaAffFormatException(string reason)
		{
			Reason = reason;
		}
		public ArcaeaAffFormatException(string content, int line)
			   : base($"在第 {line} 行发生读取错误。\n{content}")
		{

		}
		public ArcaeaAffFormatException(EventType type, string content, int line)
			: base($"在第 {line} 行处的 {type} 型事件中发生读取错误。\n{content}")
		{

		}
		public ArcaeaAffFormatException(EventType type, string content, int line, string reason)
			: base($"在第 {line} 行处的 {type} 型事件中发生读取错误。\n{content}\n{reason}")
		{

		}
	}
	public enum EventType
	{
		Timing,
		Tap,
		Hold,
		Arc,
		Camera,
		SceneControl,
		Unknown
	}
	public class ArcaeaAffEvent
	{
		public int Timing;
		public EventType Type;
	}
	public class ArcaeaAffTiming : ArcaeaAffEvent
	{
		public float Bpm;
		public float BeatsPerLine;
	}
	public class ArcaeaAffTap : ArcaeaAffEvent
	{
		public int Track;
	}
	public class ArcaeaAffHold : ArcaeaAffEvent
	{
		public int EndTiming;
		public int Track;
	}
	public class ArcaeaAffArc : ArcaeaAffEvent
	{
		public int EndTiming;
		public float XStart;
		public float XEnd;
		public string LineType;
		public float YStart;
		public float YEnd;
		public int Color;
		public bool IsVoid;
		public List<int> ArcTaps;
	}
	public class ArcaeaAffCamera : ArcaeaAffEvent
	{
		public Vector3 Move, Rotate;
		public string CameraType;
		public int Duration;
	}
	public enum SceneControlType
	{
		TrackHide,
		TrackShow,
		Unknown,
		TextArea,
		Fade
	}
	public class ArcaeaAffSceneControl : ArcaeaAffEvent
	{
		public SceneControlType SceneControlType;
	}
	public class ArcaeaAffReader
	{
		public int AudioOffset;
		public List<ArcaeaAffEvent> Events = new List<ArcaeaAffEvent>();
		public ArcaeaAffReader(string path)
		{
			Parse(path);
		}
		private EventType DetermineType(string line)
		{
			if (line.StartsWith("(")) return EventType.Tap;
			else if (line.StartsWith("timing")) return EventType.Timing;
			else if (line.StartsWith("hold")) return EventType.Hold;
			else if (line.StartsWith("arc")) return EventType.Arc;
			else if (line.StartsWith("camera")) return EventType.Camera;
			else if (line.StartsWith("scenecontrol")) return EventType.SceneControl;
			return EventType.Unknown;
		}
		private void ParseTiming(string line)
		{
			try
			{
				StringParser s = new StringParser(line);
				s.Skip(7);
				int tick = s.ReadInt(",");
				float bpm = s.ReadFloat(",");
				float beatsPerLine = s.ReadFloat(")");
				Events.Add(new ArcaeaAffTiming()
				{
					Timing = tick,
					BeatsPerLine = beatsPerLine,
					Bpm = bpm,
					Type = EventType.Timing
				});
				if (Mathf.Abs(bpm) > 1000000) throw new ArcaeaAffFormatException("BPM过大 ( -1000000 <= BPM <= 1000000)");
				if (beatsPerLine < 0) throw new ArcaeaAffFormatException("节拍线密度小于0");
			}
			catch (ArcaeaAffFormatException Ex)
			{
				throw Ex;
			}
			catch (Exception)
			{
				throw new ArcaeaAffFormatException("符号错误");
			}
		}
		private void ParseTap(string line)
		{
			try
			{
				StringParser s = new StringParser(line);
				s.Skip(1);
				int tick = s.ReadInt(",");
				int track = s.ReadInt(")");
				Events.Add(new ArcaeaAffTap()
				{
					Timing = tick,
					Track = track,
					Type = EventType.Tap
				});
				if (track <= 0 || track >= 5) throw new ArcaeaAffFormatException("轨道错误");
			}
			catch (ArcaeaAffFormatException Ex)
			{
				throw Ex;
			}
			catch (Exception)
			{
				throw new ArcaeaAffFormatException("符号错误");
			}
		}
		private void ParseHold(string line)
		{
			try
			{
				StringParser s = new StringParser(line);
				s.Skip(5);
				int tick = s.ReadInt(",");
				int endtick = s.ReadInt(",");
				int track = s.ReadInt(")");
				Events.Add(new ArcaeaAffHold()
				{
					Timing = tick,
					EndTiming = endtick,
					Track = track,
					Type = EventType.Hold
				});
				if (track <= 0 || track >= 5) throw new ArcaeaAffFormatException("轨道错误");
				if (endtick < tick) throw new ArcaeaAffFormatException("持续时间小于0");
			}
			catch (ArcaeaAffFormatException Ex)
			{
				throw Ex;
			}
			catch (Exception)
			{
				throw new ArcaeaAffFormatException("符号错误");
			}
		}
		private void ParseArc(string line)
		{
			try
			{
				StringParser s = new StringParser(line);
				s.Skip(4);
				int tick = s.ReadInt(",");
				int endtick = s.ReadInt(",");
				float startx = s.ReadFloat(",");
				float endx = s.ReadFloat(",");
				string linetype = s.ReadString(",");
				float starty = s.ReadFloat(",");
				float endy = s.ReadFloat(",");
				int color = s.ReadInt(",");
				s.ReadString(",");
				bool isvoid = s.ReadBool(")");
				List<int> arctap = null;
				if (s.Current != ";")
				{
					arctap = new List<int>();
					isvoid = true;
					while (true)
					{
						s.Skip(8);
						arctap.Add(s.ReadInt(")"));
						if (s.Current != ",") break;
					}
				}
				Events.Add(new ArcaeaAffArc()
				{
					Timing = tick,
					EndTiming = endtick,
					XStart = startx,
					XEnd = endx,
					LineType = linetype,
					YStart = starty,
					YEnd = endy,
					Color = color,
					IsVoid = isvoid,
					Type = EventType.Arc,
					ArcTaps = arctap
				});
				if (endtick < tick) throw new ArcaeaAffFormatException("持续时间小于0");
			}
			catch (ArcaeaAffFormatException Ex)
			{
				throw Ex;
			}
			catch (Exception)
			{
				throw new ArcaeaAffFormatException("符号错误");
			}
		}
		private void ParseCamera(string line)
		{
			try
			{
				StringParser s = new StringParser(line);
				s.Skip(7);
				int tick = s.ReadInt(",");
				Vector3 move = new Vector3(s.ReadFloat(","), s.ReadFloat(","), s.ReadFloat(","));
				Vector3 rotate = new Vector3(s.ReadFloat(","), s.ReadFloat(","), s.ReadFloat(","));
				string type = s.ReadString(",");
				int duration = s.ReadInt(")");
				Events.Add(new ArcaeaAffCamera()
				{
					Timing = tick,
					Duration = duration,
					Move = move,
					Rotate = rotate,
					CameraType = type,
					Type = EventType.Camera,
				});
				if (duration < 0) throw new ArcaeaAffFormatException("持续时间小于0");
			}
			catch (ArcaeaAffFormatException Ex)
			{
				throw Ex;
			}
			catch (Exception)
			{
				throw new ArcaeaAffFormatException("符号错误");
			}
		}
		private void ParseSceneControl(string line)
		{
			try
			{
				StringParser s = new StringParser(line);
				s.Skip(13);
				int tick = s.ReadInt(",");
				string type = s.ReadString(")");
				SceneControlType sceneControlType = SceneControlType.Unknown;
				switch (type)
				{
					case "trackhide":
						sceneControlType = SceneControlType.TrackHide;
						break;
					case "trackshow":
						sceneControlType = SceneControlType.TrackShow;
						break;
				}
				Events.Add(new ArcaeaAffSceneControl()
				{
					Timing = tick,
					Type = EventType.SceneControl,
					SceneControlType = sceneControlType
				});
			}
			catch (ArcaeaAffFormatException Ex)
			{
				throw Ex;
			}
			catch (Exception)
			{
				throw new ArcaeaAffFormatException("符号错误");
			}
		}
		public void Parse(string path)
		{
			string[] lines = File.ReadAllLines(path);
			try
			{
				AudioOffset = int.Parse(lines[0].Replace("AudioOffset:", ""));
			}
			catch (Exception)
			{
				throw new ArcaeaAffFormatException(lines[0], 1);
			}
			for (int i = 2; i < lines.Length; ++i)
			{
				string line = lines[i];
				EventType type = DetermineType(line);
				try
				{
					switch (type)
					{
						case EventType.Timing:
							ParseTiming(line);
							break;
						case EventType.Tap:
							ParseTap(line);
							break;
						case EventType.Hold:
							ParseHold(line);
							break;
						case EventType.Arc:
							ParseArc(line);
							break;
						case EventType.Camera:
							ParseCamera(line);
							break;
						case EventType.SceneControl:
							ParseSceneControl(line);
							break;
					}
				}
				catch (ArcaeaAffFormatException Ex)
				{
					throw new ArcaeaAffFormatException(type, line, i + 1, Ex.Reason);
				}
				catch (Exception)
				{
					throw new ArcaeaAffFormatException(type, line, i + 1);
				}
			}
		}
	}
	public class ArcaeaAffWriter : StreamWriter
	{
		public ArcaeaAffWriter(Stream stream, int audioOffset) : base(stream)
		{
			WriteLine($"AudioOffset:{audioOffset}");
			WriteLine("-");
		}
		public void WriteEvent(ArcaeaAffEvent affEvent)
		{
			switch (affEvent.Type)
			{
				case EventType.Timing:
					ArcaeaAffTiming timing = affEvent as ArcaeaAffTiming;
					WriteLine($"timing({timing.Timing},{timing.Bpm.ToString("f2")},{timing.BeatsPerLine.ToString("f2")});");
					break;
				case EventType.Tap:
					ArcaeaAffTap tap = affEvent as ArcaeaAffTap;
					WriteLine($"({tap.Timing},{tap.Track});");
					break;
				case EventType.Hold:
					ArcaeaAffHold hold = affEvent as ArcaeaAffHold;
					WriteLine($"hold({hold.Timing},{hold.EndTiming},{hold.Track});");
					break;
				case EventType.Arc:
					ArcaeaAffArc arc = affEvent as ArcaeaAffArc;
					string arcStr = $"arc({arc.Timing},{arc.EndTiming},{arc.XStart.ToString("f2")},{arc.XEnd.ToString("f2")}";
					arcStr += $",{arc.LineType},{arc.YStart.ToString("f2")},{arc.YEnd.ToString("f2")},{arc.Color},none,{((arc.ArcTaps == null || arc.ArcTaps.Count == 0) ? arc.IsVoid.ToString().ToLower() : "true")})";
					if (arc.ArcTaps != null && arc.ArcTaps.Count != 0)
					{
						arcStr += "[";
						for (int i = 0; i < arc.ArcTaps.Count; ++i)
						{
							arcStr += $"arctap({arc.ArcTaps[i]})";
							if (i != arc.ArcTaps.Count - 1) arcStr += ",";
						}
						arcStr += "]";
					}
					arcStr += ";";
					WriteLine(arcStr);
					break;
				case EventType.Camera:
					ArcaeaAffCamera cam = affEvent as ArcaeaAffCamera;
					WriteLine($"camera({cam.Timing},{cam.Move.x.ToString("f2")},{cam.Move.y.ToString("f2")},{cam.Move.z.ToString("f2")},{cam.Rotate.x.ToString("f2")},{cam.Rotate.y.ToString("f2")},{cam.Rotate.z.ToString("f2")},{cam.CameraType},{cam.Duration});");
					break;
				case EventType.SceneControl:
					ArcaeaAffSceneControl spe = affEvent as ArcaeaAffSceneControl;
					string type = "error";
					switch (spe.SceneControlType)
					{
						case SceneControlType.TrackHide:
							type="trackhide";
							WriteLine($"scenecontrol({spe.Timing},{type});");
							break;
						case SceneControlType.TrackShow:
							type="trackshow";
							WriteLine($"scenecontrol({spe.Timing},{type});");
							break;
					}
					break;
			}
		}
	}
}
