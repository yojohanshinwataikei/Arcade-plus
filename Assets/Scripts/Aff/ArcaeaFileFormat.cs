using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Arcade.Gameplay.Chart;

namespace Arcade.Aff
{

	public interface IRawAffValue { }

	public class RawAffWord : IRawAffValue
	{
		public string data;
	}
	public class RawAffFloat : IRawAffValue
	{
		public float data;
	}
	public class RawAffInt : IRawAffValue
	{
		public int data;
	}
	public interface IRawAffEvent { }
	public interface IRawAffItem : IRawAffEvent { }
	public interface IRawAffNestableItem : IRawAffItem { }
	public class RawAffTap : IRawAffNestableItem
	{
		public int Timing;
		public int Track;
	}
	public class RawAffHold : IRawAffNestableItem
	{
		public int Timing;
		public int EndTiming;
		public int Track;
	}
	public class RawAffTiming : IRawAffNestableItem
	{
		public int Timing;
		public float Bpm;
		public float BeatsPerLine;
	}
	public class RawAffArc : IRawAffNestableItem
	{
		public int Timing;
		public int EndTiming;
		public float XStart;
		public float XEnd;
		public ArcLineType LineType;
		public float YStart;
		public float YEnd;
		public int Color;
		public string Effect;
		public bool IsVoid;
		public List<RawAffArctap> ArcTaps;
	}
	public class RawAffCamera : IRawAffNestableItem
	{
		public int Timing;
		public float MoveX;
		public float MoveY;
		public float MoveZ;
		public float RotateX;
		public float RotateY;
		public float RotateZ;
		public CameraEaseType CameraType;
		public int Duration;
	}
	public class RawAffTimingGroup : IRawAffItem
	{
		public string Attributes;
		public List<IRawAffNestableItem> Items;
	}
	public class RawAffSceneControl : IRawAffNestableItem
	{
		public int Timing;
		public string Type;
		public List<IRawAffValue> Params;
	}
	public class RawAffArctap : IRawAffEvent
	{
		public int Timing;
	}
	public class RawAffChart
	{
		public int AudioOffset = 0;
		public float TimingPointDensityFactor = 1;
		public Dictionary<string, string> additionalMetadata = new Dictionary<string, string>();
		public List<IRawAffItem> items = new List<IRawAffItem>();
		public List<string> warning = new List<string>();
		public List<string> error = new List<string>();
	}
	public interface IIntoRawItem
	{
		IRawAffItem IntoRawItem();
	}

	public static class ArcaeaFileFormat
	{
		public static RawAffChart ParseFromPath(string path)
		{
			RawAffChart chart = new RawAffChart();
			using (StreamReader reader = new StreamReader(path))
			{
				// manually parse metadata
				int metadataLinesCount = 0;
				bool AudioOffsetSet = false;
				bool TimingPointDensityFactorSet = false;
				while (true)
				{
					string line = reader.ReadLine();
					if (line == null)
					{
						break;
					}
					metadataLinesCount++;
					if (line == "-")
					{
						break;
					}
					int pos = line.IndexOf(":");
					if (pos < 0)
					{
						chart.warning.Add($"第 {metadataLinesCount} 行：{line} 元信息格式错误，此行会被忽略");
						continue;
					}
					string key = line.Substring(0, pos);
					string value = line.Substring(pos + 1);
					if (key == "AudioOffset")
					{
						if (AudioOffsetSet)
						{
							chart.warning.Add($"第 {metadataLinesCount} 行：AudioOffset 被重复设置为 {value}，此行会被忽略");
						}
						else
						{
							int offset;
							if (Int32.TryParse(value, out offset))
							{
								chart.AudioOffset = offset;
								AudioOffsetSet = true;
							}
							else
							{
								chart.warning.Add($"第 {metadataLinesCount} 行：AudioOffset 的值不是整数，此行会被忽略");
							}
						}
					}
					else if (key == "TimingPointDensityFactor")
					{
						if (TimingPointDensityFactorSet)
						{
							chart.warning.Add($"第 {metadataLinesCount} 行：TimingPointDensityFactor 被重复设置为 {value}，此行会被忽略");
						}
						else
						{
							float factor;
							if (float.TryParse(value, out factor))
							{
								if (factor > 0)
								{
									chart.TimingPointDensityFactor = factor;
									TimingPointDensityFactorSet = true;
								}
								else
								{
									chart.warning.Add($"第 {metadataLinesCount} 行：TimingPointDensityFactor 的值不是正数，此行会被忽略");
								}
							}
							else
							{
								chart.warning.Add($"第 {metadataLinesCount} 行：TimingPointDensityFactor 的值不是浮点数，此行会被忽略");
							}
						}
					}
					else
					{
						if (chart.additionalMetadata.ContainsKey(key))
						{
							chart.warning.Add($"第 {metadataLinesCount} 行：{key}被重复设置为{value}，此行会被忽略");
						}
						else
						{
							chart.additionalMetadata.Add(key, value);
						}
					}
				}
				ICharStream stream = CharStreams.fromTextReader(reader);
				ArcaeaFileFormatLexer lexer = new ArcaeaFileFormatLexer(stream);
				AffErrorListener<int> lexerErrorListener = new AffErrorListener<int>(chart, metadataLinesCount);
				lexer.RemoveErrorListeners();
				lexer.AddErrorListener(lexerErrorListener);
				ITokenStream tokenStream = new CommonTokenStream(lexer);
				ArcaeaFileFormatParser parser = new ArcaeaFileFormatParser(tokenStream);
				parser.BuildParseTree = true;
				AffErrorListener<IToken> parserErrorListener = new AffErrorListener<IToken>(chart, metadataLinesCount);
				parser.RemoveErrorListeners();
				parser.AddErrorListener(parserErrorListener);
				IParseTree tree = parser.file();
				if (chart.error.Count == 0)
				{
					ParseTreeWalker.Default.Walk(new AffTypeChecker(chart, metadataLinesCount), tree);
				}
			}
			foreach (string warning in chart.warning)
			{
				Debug.LogWarning(warning);
			}
			foreach (string error in chart.error)
			{
				Debug.LogError(error);
			}
			return chart;
		}

		public static void DumpToStream(Stream stream, RawAffChart chart)
		{
			TextWriter writer = new StreamWriter(stream);
			writer.WriteLine($"AudioOffset:{chart.AudioOffset}");
			if (chart.TimingPointDensityFactor != 1f)
			{
				writer.WriteLine($"TimingPointDensityFactor:{chart.TimingPointDensityFactor}");
			}
			foreach (var entry in chart.additionalMetadata)
			{
				writer.WriteLine($"{entry.Key}:{entry.Value}");
			}
			writer.WriteLine($"-");
			foreach (var item in chart.items)
			{
				writeItem(writer, item);
			}
			writer.Close();
		}
		static void writeItem(TextWriter writer, IRawAffItem item, string intent = "")
		{
			if (item is RawAffTiming timing)
			{
				writer.WriteLine($"{intent}timing({timing.Timing},{timing.Bpm.ToString("f2")},{timing.BeatsPerLine.ToString("f2")});");
			}
			else if (item is RawAffTap tap)
			{
				writer.WriteLine($"{intent}({tap.Timing},{tap.Track});");
			}
			else if (item is RawAffHold hold)
			{
				writer.WriteLine($"{intent}hold({hold.Timing},{hold.EndTiming},{hold.Track});");
			}
			else if (item is RawAffArc arc)
			{
				if (arc.ArcTaps.Count > 0)
				{
					arc.IsVoid = true;
				}
				writer.WriteLine($"{intent}arc({arc.Timing},{arc.EndTiming},{arc.XStart.ToString("f2")},{arc.XEnd.ToString("f2")}" +
					$",{ArcLineTypeStrings[arc.LineType]},{arc.YStart.ToString("f2")},{arc.YEnd.ToString("f2")},{arc.Color},{arc.Effect},{arc.IsVoid.ToString().ToLower()})" +
					(arc.ArcTaps.Count > 0 ? $"[{string.Join(",", arc.ArcTaps.Select(e => $"arctap({e.Timing})"))}]" : "") +
					";");
			}
			else if (item is RawAffCamera cam)
			{
				writer.WriteLine($"{intent}camera({cam.Timing},{cam.MoveX.ToString("f2")},{cam.MoveY.ToString("f2")},{cam.MoveZ.ToString("f2")}," +
					$"{cam.RotateX.ToString("f2")},{cam.RotateY.ToString("f2")},{cam.RotateZ.ToString("f2")},{CameraEaseTypeStrings[cam.CameraType]},{cam.Duration});");
			}
			else if (item is RawAffSceneControl scenecontrol)
			{
				List<string> values = new List<string>();
				values.Add(scenecontrol.Timing.ToString());
				values.Add(scenecontrol.Type);
				foreach (var @param in scenecontrol.Params)
				{
					if (@param is RawAffInt @int)
					{
						values.Add(@int.data.ToString());
					}
					else if (@param is RawAffFloat @float)
					{
						values.Add(@float.data.ToString("f2"));
					}
					else if (@param is RawAffWord word)
					{
						values.Add(word.data);
					}
				}
				writer.WriteLine($"{intent}scenecontrol({string.Join(",", values)});");
			}
			else if (item is RawAffTimingGroup timinggroup)
			{
				writer.WriteLine($"{intent}timinggroup({timinggroup.Attributes}){{");
				foreach (var nestedItem in timinggroup.Items)
				{
					writeItem(writer, nestedItem, intent + "  ");
				}
				writer.WriteLine($"{intent}}};");
			}
		}
		public static Dictionary<ArcLineType, string> ArcLineTypeStrings = new Dictionary<ArcLineType, string>
		{
			[ArcLineType.B] = "b",
			[ArcLineType.S] = "s",
			[ArcLineType.Si] = "si",
			[ArcLineType.So] = "so",
			[ArcLineType.SiSi] = "sisi",
			[ArcLineType.SiSo] = "siso",
			[ArcLineType.SoSi] = "sosi",
			[ArcLineType.SoSo] = "soso",
		};
		public static Dictionary<CameraEaseType, string> CameraEaseTypeStrings = new Dictionary<CameraEaseType, string>
		{
			[CameraEaseType.L] = "l",
			[CameraEaseType.Reset] = "reset",
			[CameraEaseType.Qi] = "qi",
			[CameraEaseType.Qo] = "qo",
			[CameraEaseType.S] = "s",
		};
	}

	class AffErrorListener<T> : IAntlrErrorListener<T>
	{
		private RawAffChart chart;
		private int lineOffset;
		public AffErrorListener(RawAffChart chart, int lineOffset = 0)
		{
			this.lineOffset = lineOffset;
			this.chart = chart;
		}

		public void SyntaxError(TextWriter output, IRecognizer recognizer, T offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
		{
			chart.error.Add($"第 {line + lineOffset} 行第 {charPositionInLine + 1} 列，谱面文件语法错误：{msg}");
		}
	}

	class AffTypeChecker : ArcaeaFileFormatBaseListener
	{
		private RawAffChart chart;
		private List<IRawAffItem> nonNestableItems = new List<IRawAffItem>();
		private int lineOffset;

		public AffTypeChecker(RawAffChart chart, int lineOffset = 0)
		{
			this.lineOffset = lineOffset;
			this.chart = chart;
		}

		public override void ExitValue(ArcaeaFileFormatParser.ValueContext context)
		{
			if (context.Int() != null)
			{
				int data;
				if (int.TryParse(context.Int().GetText(), out data))
				{
					context.value = new RawAffInt() { data = data };
				}
				else
				{
					chart.warning.Add($"第 {context.Int().Symbol.Line + lineOffset} 行第 {context.Int().Symbol.Column + 1} 列，整数无法解析，可能是超出了数据范围，相关事件将被忽略");
				}
			}
			else if (context.Float() != null)
			{
				float data;
				if (float.TryParse(context.Float().GetText(), out data))
				{
					context.value = new RawAffFloat() { data = data };
				}
				else
				{
					chart.warning.Add($"第 {context.Int().Symbol.Line + lineOffset} 行第 {context.Int().Symbol.Column + 1} 列，浮点数无法解析，可能是超出了数据范围，相关事件将被忽略");
				}
			}
			else if (context.Word() != null)
			{
				context.value = new RawAffWord() { data = context.Word().GetText() };
			}
		}
		public override void ExitEvent(ArcaeaFileFormatParser.EventContext context)
		{
			if (context.Word() == null)
			{
				GenTap(context);
			}
			else
			{
				string tag = context.Word().GetText();
				if (tag == "hold")
				{
					GenHold(context);
				}
				else if (tag == "timing")
				{
					GenTiming(context);
				}
				else if (tag == "arc")
				{
					GenArc(context);
				}
				else if (tag == "arctap")
				{
					GenArctap(context);
				}
				else if (tag == "camera")
				{
					GenCamera(context);
				}
				else if (tag == "scenecontrol")
				{
					GenSceneControl(context);
				}
				else if (tag == "timinggroup")
				{
					GenTimingGroup(context);
				}
				else
				{
					chart.warning.Add($"第 {context.Start.Line + lineOffset} 行第 {context.Start.Column + 1} 列，不支持的事件类型：{context.Word().GetText()}，该事件将被忽略");
				}
			}
		}

		public override void ExitFile(ArcaeaFileFormatParser.FileContext context)
		{
			foreach (var item in context.body().item())
			{
				IRawAffEvent @event = item.@event().value;
				if (@event != null)
				{
					if (@event is IRawAffItem innerItem)
					{
						chart.items.Add(innerItem);
					}
					else
					{
						chart.warning.Add($"第 {item.@event().Start.Line + lineOffset} 行第 {item.@event().Start.Column + 1} 列，不可作为物件使用的事件：{item.@event().GetText()}，该事件将被忽略");
					}
				}
			}
			chart.items.AddRange(nonNestableItems);
		}

		void GenTap(ArcaeaFileFormatParser.EventContext context)
		{
			RejectSubevents(context, "tap");
			RejectSegment(context, "tap");
			if (!CheckValuesCount(context, "tap", 2))
			{
				return;
			}
			var timing = CheckValueType<RawAffInt>(context.values().value()[0], "tap", "时间");
			var track = CheckValueType<RawAffInt>(context.values().value()[1], "tap", "轨道");
			if (timing == null || track == null)
			{
				return;
			}
			bool valueError = false;
			if (track.data > 5 || track.data < 0)
			{
				chart.warning.Add($"第 {context.values().value()[1].Start.Line + lineOffset} 行第 {context.values().value()[1].Start.Column + 1} 列，tap 事件的轨道参数超过范围，此 tap 将被忽略");
				valueError = true;
			}
			if (!valueError)
			{
				context.value = new RawAffTap() { Timing = timing.data, Track = track.data };
			}
		}
		void GenHold(ArcaeaFileFormatParser.EventContext context)
		{
			RejectSubevents(context, "hold");
			RejectSegment(context, "hold");
			if (!CheckValuesCount(context, "hold", 3))
			{
				return;
			}
			var timing = CheckValueType<RawAffInt>(context.values().value()[0], "hold", "时间");
			var endTiming = CheckValueType<RawAffInt>(context.values().value()[1], "hold", "结束时间");
			var track = CheckValueType<RawAffInt>(context.values().value()[2], "hold", "轨道");
			if (timing == null || endTiming == null || track == null)
			{
				return;
			}
			bool valueError = false;
			if (endTiming.data < timing.data)
			{
				chart.warning.Add($"第 {context.values().value()[1].Start.Line + lineOffset} 行第 {context.values().value()[1].Start.Column + 1} 列，hold 事件的结束时间早于开始时间，作为修复将会交换起止时间");
				var tmp = timing;
				timing = endTiming;
				endTiming = tmp;
			}
			if (track.data > 5 || track.data < 0)
			{
				chart.warning.Add($"第 {context.values().value()[2].Start.Line + lineOffset} 行第 {context.values().value()[2].Start.Column + 1} 列，hold 事件的轨道参数超过范围，此 hold 将被忽略");
				valueError = true;
			}
			if (!valueError)
			{
				context.value = new RawAffHold() { Timing = timing.data, EndTiming = endTiming.data, Track = track.data };
			}
		}
		void GenTiming(ArcaeaFileFormatParser.EventContext context)
		{
			RejectSubevents(context, "timing");
			RejectSegment(context, "timing");
			if (!CheckValuesCount(context, "timing", 3))
			{
				return;
			}
			var timing = CheckValueType<RawAffInt>(context.values().value()[0], "hold", "时间");
			var bpm = CheckValueType<RawAffFloat>(context.values().value()[1], "hold", "BPM");
			var segment = CheckValueType<RawAffFloat>(context.values().value()[2], "hold", "单个小节拍数");
			if (timing == null || bpm == null || segment == null)
			{
				return;
			}
			bool valueError = false;
			if (segment.data < 0)
			{
				chart.warning.Add($"第 {context.values().value()[2].Start.Line + lineOffset} 行第 {context.values().value()[2].Start.Column + 1} 列，timing 事件的单个小节拍数小于 0，此 timing 将被忽略");
				valueError = true;
			}
			if (!valueError)
			{
				context.value = new RawAffTiming() { Timing = timing.data, Bpm = bpm.data, BeatsPerLine = segment.data };
			}
		}
		void GenArc(ArcaeaFileFormatParser.EventContext context)
		{
			RejectSegment(context, "arc");
			if (!CheckValuesCount(context, "arc", 10))
			{
				return;
			}
			var timing = CheckValueType<RawAffInt>(context.values().value()[0], "arc", "时间");
			var endTiming = CheckValueType<RawAffInt>(context.values().value()[1], "arc", "结束时间");
			var xStart = CheckValueType<RawAffFloat>(context.values().value()[2], "arc", "起点横坐标");
			var xEnd = CheckValueType<RawAffFloat>(context.values().value()[3], "arc", "终点横坐标");
			var rawLineType = CheckValueType<RawAffWord>(context.values().value()[4], "arc", "arc 类型");
			var lineType = ParseWord(lineTypes, rawLineType.data, context.values().value()[4], "arc", "arc 类型");
			var yStart = CheckValueType<RawAffFloat>(context.values().value()[5], "arc", "起点纵坐标");
			var yEnd = CheckValueType<RawAffFloat>(context.values().value()[6], "arc", "终点纵坐标");
			var color = CheckValueType<RawAffInt>(context.values().value()[7], "arc", "颜色");
			var effect = CheckValueType<RawAffWord>(context.values().value()[8], "arc", "效果类型");
			var rawIsVoid = CheckValueType<RawAffWord>(context.values().value()[9], "arc", "是否黑线");
			var isVoid = ParseWord(bools, rawIsVoid.data, context.values().value()[9], "arc", "是否黑线");
			if (timing == null || endTiming == null || xStart == null || xEnd == null || lineType == null || yStart == null || yEnd == null || color == null || effect == null || isVoid == null)
			{
				return;
			}
			bool valueError = false;
			if (endTiming.data < timing.data)
			{
				chart.warning.Add($"第 {context.values().value()[1].Start.Line + lineOffset} 行第 {context.values().value()[1].Start.Column + 1} 列，arc 事件的结束时间早于开始时间，作为修复将会交换起止时间");
				var tmp = timing;
				timing = endTiming;
				endTiming = tmp;
			}
			List<RawAffArctap> arctaps = new List<RawAffArctap>();
			var subevents = context.subevents();
			if (subevents != null)
			{
				foreach (var @event in context.subevents().@event())
				{
					if (@event.value != null)
					{
						if (@event.value is RawAffArctap arctap)
						{
							if (arctap.Timing < timing.data || arctap.Timing > endTiming.data)
							{
								chart.warning.Add($"第 {@event.values().value()[0].Start.Line + lineOffset} 行第 {@event.values().value()[0].Start.Column + 1} 列，arctap 事件的时间超出所属 arc 的时间范围，此 arctap 事件将被忽略");
							}
							else
							{
								arctaps.Add(arctap);
							}
						}
						else
						{
							chart.warning.Add($"第 {@event.Start.Line + lineOffset} 行第 {@event.Start.Column + 1} 列，arc 事件的子事件必须为 arctap 事件，此事件将被忽略");
						}
					}
				}
			}
			if (!valueError)
			{
				context.value = new RawAffArc()
				{
					Timing = timing.data,
					EndTiming = endTiming.data,
					XStart = xStart.data,
					XEnd = xEnd.data,
					LineType = lineType.Value,
					YStart = yStart.data,
					YEnd = yEnd.data,
					Color = color.data,
					Effect = effect.data,
					IsVoid = isVoid.Value,
					ArcTaps = arctaps,
				};
			}
		}
		void GenArctap(ArcaeaFileFormatParser.EventContext context)
		{
			RejectSubevents(context, "arctap");
			RejectSegment(context, "arctap");
			if (!CheckValuesCount(context, "arctap", 1))
			{
				return;
			}
			var timing = CheckValueType<RawAffInt>(context.values().value()[0], "arctap", "时间");
			if (timing == null)
			{
				return;
			}
			context.value = new RawAffArctap() { Timing = timing.data };
		}
		void GenCamera(ArcaeaFileFormatParser.EventContext context)
		{
			RejectSubevents(context, "camera");
			RejectSegment(context, "camera");
			if (!CheckValuesCount(context, "camera", 9))
			{
				return;
			}
			var timing = CheckValueType<RawAffInt>(context.values().value()[0], "camera", "时间");
			var moveX = CheckValueType<RawAffFloat>(context.values().value()[1], "camera", "平移 X 轴");
			var moveY = CheckValueType<RawAffFloat>(context.values().value()[2], "camera", "平移 Y 轴");
			var moveZ = CheckValueType<RawAffFloat>(context.values().value()[3], "camera", "平移 Z 轴");
			var rotateX = CheckValueType<RawAffFloat>(context.values().value()[4], "camera", "旋转 X 轴");
			var rotateY = CheckValueType<RawAffFloat>(context.values().value()[5], "camera", "旋转 Y 轴");
			var rotateZ = CheckValueType<RawAffFloat>(context.values().value()[6], "camera", "旋转 Z 轴");
			var rawCameraType = CheckValueType<RawAffWord>(context.values().value()[7], "camera", "camera 类型");
			var cameraType = ParseWord<CameraEaseType>(cameraTypes, rawCameraType.data, context.values().value()[7], "camera", "camera 类型");
			var duration = CheckValueType<RawAffInt>(context.values().value()[8], "camera", "时长");
			if (timing == null || moveX == null || moveY == null || moveZ == null || rotateX == null || rotateY == null || rotateZ == null || cameraType == null || duration == null)
			{
				return;
			}
			bool valueError = false;
			if (duration.data < 0)
			{
				chart.warning.Add($"第 {context.values().value()[8].Start.Line + lineOffset} 行第 {context.values().value()[8].Start.Column + 1} 列，camera 事件的时长小于零，作为修复其时长见会被设为零");
				duration.data = 0;
			}
			if (!valueError)
			{
				context.value = new RawAffCamera()
				{
					Timing = timing.data,
					MoveX = moveX.data,
					MoveY = moveY.data,
					MoveZ = moveZ.data,
					RotateX = rotateX.data,
					RotateY = rotateY.data,
					RotateZ = rotateZ.data,
					CameraType = cameraType.Value,
					Duration = duration.data,
				};
			}
		}
		void GenSceneControl(ArcaeaFileFormatParser.EventContext context)
		{
			RejectSubevents(context, "scenecontrol");
			RejectSegment(context, "scenecontrol");
			if (!EnsureValuesCount(context, "scenecontrol", 2))
			{
				return;
			}
			var timing = CheckValueType<RawAffInt>(context.values().value()[0], "scenecontrol", "时间");
			var type = CheckValueType<RawAffWord>(context.values().value()[1], "scenecontrol", "类型");
			if (timing == null || type == null)
			{
				return;
			}
			List<IRawAffValue> @params = new List<IRawAffValue>();
			foreach (var value in context.values().value().Skip(2))
			{
				if (value.value == null)
				{
					return;
				}
				@params.Add(value.value);
			}
			context.value = new RawAffSceneControl()
			{
				Timing = timing.data,
				Type = type.data,
				Params = @params,
			};
		}
		void GenTimingGroup(ArcaeaFileFormatParser.EventContext context)
		{
			RejectSubevents(context, "timinggroup");
			LimitValuesCount(context, "timinggroup", 1);
			List<IRawAffNestableItem> items = new List<IRawAffNestableItem>();
			Arcade.Aff.RawAffWord timingGroupAttributes = new RawAffWord { data = "" };
			if (context.values().value().Length > 0)
			{
				timingGroupAttributes = CheckValueType<RawAffWord>(context.values().value()[0], "arc", "arc 类型");
			}
			foreach (var item in context.segment().body().item())
			{
				IRawAffEvent @event = item.@event().value;
				if (!(@event is IRawAffItem))
				{
					chart.warning.Add($"第 {item.@event().Start.Line + lineOffset} 行第 {item.@event().Start.Column + 1} 列，不可作为物件使用的事件：{item.@event().GetText()}，此事件将被忽略");
					continue;
				}
				IRawAffItem rawItem = @event as IRawAffItem;
				if (!(rawItem is IRawAffNestableItem))
				{
					chart.warning.Add($"第 {item.Start.Line + lineOffset} 行第 {item.Start.Column + 1} 列，不可在 timinggroup 中嵌套使用的物件：{item.@event().GetText()}，此物件将被忽略");
					nonNestableItems.Add(rawItem);
					continue;
				}
				items.Add(rawItem as IRawAffNestableItem);
			}
			if (timingGroupAttributes == null)
			{
				return;
			}
			context.value = new RawAffTimingGroup()
			{
				Items = items,
				Attributes = timingGroupAttributes.data,
			};
		}

		void RejectSubevents(ArcaeaFileFormatParser.EventContext context, string type)
		{
			if (context.subevents() != null)
			{
				chart.warning.Add($"第 {context.subevents().Start.Line + lineOffset} 行第 {context.subevents().Start.Column + 1} 列，{type} 事件不应包含子事件，这些子事件将被忽略");
			}
		}

		void RejectSegment(ArcaeaFileFormatParser.EventContext context, string type)
		{
			if (context.segment() != null)
			{
				chart.warning.Add($"第 {context.subevents().Start.Line + lineOffset} 行第 {context.subevents().Start.Column + 1} 列，{type} 事件不应包含事件块，此事件块将被忽略");
			}
		}

		bool CheckValuesCount(ArcaeaFileFormatParser.EventContext context, string type, int count)
		{
			if (context.values().value().Length != count)
			{
				chart.warning.Add($"第 {context.values().Start.Line + lineOffset} 行第 {context.values().Start.Column + 1} 列，{type} 事件的参数个数应当为 {count} 个而非 {context.values().value().Length} 个，此事件将被忽略");
				return false;
			}
			return true;
		}

		bool EnsureValuesCount(ArcaeaFileFormatParser.EventContext context, string type, int count)
		{
			if (context.values().value().Length < count)
			{
				chart.warning.Add($"第 {context.values().Start.Line + lineOffset} 行第 {context.values().Start.Column + 1} 列，{type} 事件的参数个数应当为至少 {count} 个而非 {context.values().value().Length} 个，此事件将被忽略");
				return false;
			}
			return true;
		}

		bool LimitValuesCount(ArcaeaFileFormatParser.EventContext context, string type, int count)
		{
			if (context.values().value().Length > count)
			{
				chart.warning.Add($"第 {context.values().Start.Line + lineOffset} 行第 {context.values().Start.Column + 1} 列，{type} 事件的参数个数应当为至多 {count} 个而非 {context.values().value().Length} 个，此事件将被忽略");
				return false;
			}
			return true;
		}


		T CheckValueType<T>(ArcaeaFileFormatParser.ValueContext context, string type, string field) where T : class, IRawAffValue
		{
			if (context.value != null)
			{
				if (context.value is T value)
				{
					return value;
				}
				else
				{
					chart.warning.Add($"第 {context.Start.Line + lineOffset} 行第 {context.Start.Column + 1} 列，{type} 事件的 {field} 参数的值类型错误，此事件将被忽略");
				}
			}
			return null;
		}

		T? ParseWord<T>(Dictionary<string, T> values, string word, ArcaeaFileFormatParser.ValueContext context, string type, string field) where T : struct
		{
			if (word == null)
			{
				return null;
			}
			T result;
			if (values.TryGetValue(word, out result))
			{
				return result;
			}
			chart.warning.Add($"第 {context.Start.Line + lineOffset} 行第 {context.Start.Column + 1} 列，{type} 事件的 {field} 参数的值不是合法的几个值之一，此事件将被忽略");
			return null;
		}

		Dictionary<string, bool> bools = new Dictionary<string, bool>()
		{
			["true"] = true,
			["false"] = false,
		};

		Dictionary<string, ArcLineType> lineTypes = new Dictionary<string, ArcLineType>()
		{
			["b"] = ArcLineType.B,
			["s"] = ArcLineType.S,
			["si"] = ArcLineType.Si,
			["so"] = ArcLineType.So,
			["sisi"] = ArcLineType.SiSi,
			["soso"] = ArcLineType.SoSo,
			["siso"] = ArcLineType.SiSo,
			["sosi"] = ArcLineType.SoSi,
		};

		Dictionary<string, CameraEaseType> cameraTypes = new Dictionary<string, CameraEaseType>()
		{
			["l"] = CameraEaseType.L,
			["s"] = CameraEaseType.S,
			["qi"] = CameraEaseType.Qi,
			["qo"] = CameraEaseType.Qo,
			["reset"] = CameraEaseType.Reset,
		};
	}

	// Here we use patial class to insert custom fields into generated classes
	public partial class ArcaeaFileFormatParser : Parser
	{
		public partial class ValueContext : ParserRuleContext
		{
			public IRawAffValue value;
		}
		public partial class EventContext : ParserRuleContext
		{
			public IRawAffEvent value;
		}
	}

}