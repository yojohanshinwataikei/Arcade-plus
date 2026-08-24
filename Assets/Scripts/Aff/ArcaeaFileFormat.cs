using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Arcade.Gameplay.Chart;
using System.Globalization;
using Arcade.Util.I18n;

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
		public ArcCurveType CurveType;
		public float YStart;
		public float YEnd;
		public int Color;
		public string Effect;
		public ArcLineType LineType;
		public float? Smoothness;
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
						chart.warning.Add(I18n.Translate("compose.chart_format.warning.ignored_metadata", 
							metadataLinesCount.ToString(CultureInfo.InvariantCulture),
							line));
						continue;
					}
					string key = line.Substring(0, pos);
					string value = line.Substring(pos + 1);
					if (key == "AudioOffset")
					{
						if (AudioOffsetSet)
						{
							chart.warning.Add(I18n.Translate("compose.chart_format.warning.duplicated_offset", 
								metadataLinesCount.ToString(CultureInfo.InvariantCulture),
								value));
						}
						else
						{
							int offset;
							if (Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out offset))
							{
								chart.AudioOffset = offset;
								AudioOffsetSet = true;
							}
							else
							{
								chart.warning.Add(I18n.Translate("compose.chart_format.warning.floated_offset",
									metadataLinesCount.ToString(CultureInfo.InvariantCulture)));
							}
						}
					}
					else if (key == "TimingPointDensityFactor")
					{
						if (TimingPointDensityFactorSet)
						{
							chart.warning.Add(I18n.Translate("compose.chart_format.warning.duplicated_tpdf",
								metadataLinesCount.ToString(CultureInfo.InvariantCulture),
								value));
						}
						else
						{
							float factor;
							if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out factor))
							{
								if (factor > 0)
								{
									chart.TimingPointDensityFactor = factor;
									TimingPointDensityFactorSet = true;
								}
								else
								{
									chart.warning.Add(I18n.Translate("compose.chart_format.warning.negative_tpdf",
										metadataLinesCount.ToString(CultureInfo.InvariantCulture)));
								}
							}
							else
							{
								chart.warning.Add(I18n.Translate("compose.chart_format.warning.int_tpdf",
									metadataLinesCount.ToString(CultureInfo.InvariantCulture)));
							}
						}
					}
					else
					{
						if (chart.additionalMetadata.ContainsKey(key))
						{
							chart.warning.Add(I18n.Translate("compose.chart_format.warning.duplicated_key",
								metadataLinesCount.ToString(CultureInfo.InvariantCulture),
								key,
								value));
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
			writer.WriteLine($"AudioOffset:{chart.AudioOffset.ToString(CultureInfo.InvariantCulture)}");
			if (chart.TimingPointDensityFactor != 1f)
			{
				writer.WriteLine($"TimingPointDensityFactor:{chart.TimingPointDensityFactor.ToString(CultureInfo.InvariantCulture)}");
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
				writer.WriteLine($"{intent}timing({timing.Timing.ToString(CultureInfo.InvariantCulture)},{timing.Bpm.ToString("f2", CultureInfo.InvariantCulture)},{timing.BeatsPerLine.ToString("f2", CultureInfo.InvariantCulture)});");
			}
			else if (item is RawAffTap tap)
			{
				writer.WriteLine($"{intent}({tap.Timing.ToString(CultureInfo.InvariantCulture)},{tap.Track.ToString(CultureInfo.InvariantCulture)});");
			}
			else if (item is RawAffHold hold)
			{
				writer.WriteLine($"{intent}hold({hold.Timing.ToString(CultureInfo.InvariantCulture)},{hold.EndTiming.ToString(CultureInfo.InvariantCulture)},{hold.Track.ToString(CultureInfo.InvariantCulture)});");
			}
			else if (item is RawAffArc arc)
			{
				if (arc.ArcTaps.Count > 0 && arc.LineType == ArcLineType.FalseNotVoid)
				{
					arc.LineType = ArcLineType.TrueIsVoid;
				}
				writer.WriteLine($"{intent}arc({arc.Timing.ToString(CultureInfo.InvariantCulture)},{arc.EndTiming.ToString(CultureInfo.InvariantCulture)},{arc.XStart.ToString("f2", CultureInfo.InvariantCulture)},{arc.XEnd.ToString("f2", CultureInfo.InvariantCulture)}" +
					$",{ArcCurveTypeStrings[arc.CurveType]},{arc.YStart.ToString("f2", CultureInfo.InvariantCulture)},{arc.YEnd.ToString("f2", CultureInfo.InvariantCulture)},{arc.Color},{arc.Effect},{ArcLineTypeStrings[arc.LineType]}"
					 + (arc.Smoothness == null ? "" : $",{arc.Smoothness.Value.ToString("f2", CultureInfo.InvariantCulture)}") + ")" +
					(arc.ArcTaps.Count > 0 ? $"[{string.Join(",", arc.ArcTaps.Select(e => $"arctap({e.Timing.ToString(CultureInfo.InvariantCulture)})"))}]" : "") +
					";");
			}
			else if (item is RawAffCamera cam)
			{
				writer.WriteLine($"{intent}camera({cam.Timing.ToString(CultureInfo.InvariantCulture)},{cam.MoveX.ToString("f2", CultureInfo.InvariantCulture)},{cam.MoveY.ToString("f2", CultureInfo.InvariantCulture)},{cam.MoveZ.ToString("f2", CultureInfo.InvariantCulture)}," +
					$"{cam.RotateX.ToString("f2", CultureInfo.InvariantCulture)},{cam.RotateY.ToString("f2", CultureInfo.InvariantCulture)},{cam.RotateZ.ToString("f2", CultureInfo.InvariantCulture)},{CameraEaseTypeStrings[cam.CameraType]},{cam.Duration.ToString(CultureInfo.InvariantCulture)});");
			}
			else if (item is RawAffSceneControl scenecontrol)
			{
				List<string> values = new List<string>();
				values.Add(scenecontrol.Timing.ToString(CultureInfo.InvariantCulture));
				values.Add(scenecontrol.Type);
				foreach (var @param in scenecontrol.Params)
				{
					if (@param is RawAffInt @int)
					{
						values.Add(@int.data.ToString(CultureInfo.InvariantCulture));
					}
					else if (@param is RawAffFloat @float)
					{
						values.Add(@float.data.ToString("f2", CultureInfo.InvariantCulture));
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
		public static Dictionary<ArcCurveType, string> ArcCurveTypeStrings = new Dictionary<ArcCurveType, string>
		{
			[ArcCurveType.B] = "b",
			[ArcCurveType.S] = "s",
			[ArcCurveType.Si] = "si",
			[ArcCurveType.So] = "so",
			[ArcCurveType.SiSi] = "sisi",
			[ArcCurveType.SiSo] = "siso",
			[ArcCurveType.SoSi] = "sosi",
			[ArcCurveType.SoSo] = "soso",
		};
		public static Dictionary<ArcLineType, string> ArcLineTypeStrings = new Dictionary<ArcLineType, string>
		{
			[ArcLineType.TrueIsVoid] = "true",
			[ArcLineType.FalseNotVoid] = "false",
			[ArcLineType.Designant] = "designant",
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
			chart.error.Add(I18n.Translate("compose.chart_format.error.synatx",
				(line + lineOffset).ToString(CultureInfo.InvariantCulture),
				(charPositionInLine + 1).ToString(CultureInfo.InvariantCulture),
				msg));
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
				if (int.TryParse(context.Int().GetText(), NumberStyles.Integer, CultureInfo.InvariantCulture, out data))
				{
					context.value = new RawAffInt() { data = data };
				}
				else
				{
					chart.warning.Add(I18n.Translate("compose.chart_format.warning.invalid_int",
						(context.Int().Symbol.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
						(context.Int().Symbol.Column + 1).ToString(CultureInfo.InvariantCulture)));
				}
			}
			else if (context.Float() != null)
			{
				float data;
				if (float.TryParse(context.Float().GetText(), NumberStyles.Float, CultureInfo.InvariantCulture, out data))
				{
					context.value = new RawAffFloat() { data = data };
				}
				else
				{
					chart.warning.Add(I18n.Translate("compose.chart_format.warning.invalid_float",
						(context.Int().Symbol.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
						(context.Int().Symbol.Column + 1).ToString(CultureInfo.InvariantCulture)));
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
					chart.warning.Add(I18n.Translate("compose.chart_format.warning.invalid_event",
						(context.Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
						(context.Start.Column + 1).ToString(CultureInfo.InvariantCulture),
						context.Word().GetText()));
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
						chart.warning.Add(I18n.Translate("compose.chart_format.warning.non_note_event",
							(item.@event().Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
							(item.@event().Start.Column + 1).ToString(CultureInfo.InvariantCulture),
							item.@event().GetText()));
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
			var timing = CheckValueType<RawAffInt>(context.values().value()[0], "tap", I18n.Translate("compose.chart_format.field.timing"));
			var track = CheckValueType<RawAffInt>(context.values().value()[1], "tap", I18n.Translate("compose.chart_format.field.track"));
			if (timing == null || track == null)
			{
				return;
			}
			bool valueError = false;
			if (track.data > 5 || track.data < 0)
			{
				chart.warning.Add(I18n.Translate("compose.chart_format.warning.tap_overflow",
					(context.values().value()[1].Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
					(context.values().value()[1].Start.Column + 1).ToString(CultureInfo.InvariantCulture)));
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
			var timing = CheckValueType<RawAffInt>(context.values().value()[0], "hold", I18n.Translate("compose.chart_format.field.timing"));
			var endTiming = CheckValueType<RawAffInt>(context.values().value()[1], "hold", I18n.Translate("compose.chart_format.field.end_timing"));
			var track = CheckValueType<RawAffInt>(context.values().value()[2], "hold", I18n.Translate("compose.chart_format.field.track"));
			if (timing == null || endTiming == null || track == null)
			{
				return;
			}
			bool valueError = false;
			if (endTiming.data < timing.data)
			{
				chart.warning.Add(I18n.Translate("compose.chart_format.warning.hold_negative_duration",
					(context.values().value()[1].Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
					(context.values().value()[1].Start.Column + 1).ToString(CultureInfo.InvariantCulture)));
				(timing, endTiming) = (endTiming, timing);
			}
			if (track.data > 5 || track.data < 0)
			{
				chart.warning.Add(I18n.Translate("compose.chart_format.warning.hold_overflow",
					(context.values().value()[2].Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
					(context.values().value()[2].Start.Column + 1).ToString(CultureInfo.InvariantCulture)));
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
			var timing = CheckValueType<RawAffInt>(context.values().value()[0], "hold", I18n.Translate("compose.chart_format.field.timing"));
			var bpm = CheckValueType<RawAffFloat>(context.values().value()[1], "hold", I18n.Translate("compose.chart_format.field.bpm"));
			var segment = CheckValueType<RawAffFloat>(context.values().value()[2], "hold", I18n.Translate("compose.chart_format.field.beats"));
			if (timing == null || bpm == null || segment == null)
			{
				return;
			}
			bool valueError = false;
			if (segment.data < 0)
			{
				chart.warning.Add(I18n.Translate("compose.chart_format.warning.timing_negative_beats",
					(context.values().value()[2].Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
					(context.values().value()[2].Start.Column + 1).ToString(CultureInfo.InvariantCulture)));
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
			if (!EnsureValuesCount(context, "arc", 10))
			{
				return;
			}
			if (!LimitValuesCount(context, "arc", 11))
			{
				return;
			}
			var timing = CheckValueType<RawAffInt>(context.values().value()[0], "arc", I18n.Translate("compose.chart_format.field.timing"));
			var endTiming = CheckValueType<RawAffInt>(context.values().value()[1], "arc", I18n.Translate("compose.chart_format.field.end_timing"));
			var xStart = CheckValueType<RawAffFloat>(context.values().value()[2], "arc", I18n.Translate("compose.chart_format.field.x_start"));
			var xEnd = CheckValueType<RawAffFloat>(context.values().value()[3], "arc", I18n.Translate("compose.chart_format.field.x_end"));
			var rawCurveType = CheckValueType<RawAffWord>(context.values().value()[4], "arc", I18n.Translate("compose.chart_format.field.curve_type"));
			var curveType = ParseWord(curveTypes, rawCurveType.data, context.values().value()[4], "arc", I18n.Translate("compose.chart_format.field.curve_type"));
			var yStart = CheckValueType<RawAffFloat>(context.values().value()[5], "arc", I18n.Translate("compose.chart_format.field.y_start"));
			var yEnd = CheckValueType<RawAffFloat>(context.values().value()[6], "arc", I18n.Translate("compose.chart_format.field.y_end"));
			var color = CheckValueType<RawAffInt>(context.values().value()[7], "arc", I18n.Translate("compose.chart_format.field.color"));
			var effect = CheckValueType<RawAffWord>(context.values().value()[8], "arc", I18n.Translate("compose.chart_format.field.effect"));
			var rawLineType = CheckValueType<RawAffWord>(context.values().value()[9], "arc", I18n.Translate("compose.chart_format.field.line_type"));
			var lineType = ParseWord(lineTypes, rawLineType.data, context.values().value()[9], "arc", I18n.Translate("compose.chart_format.field.line_type"));
			RawAffFloat smoothness = null;
			if (context.values().value().Length >= 11)
			{
				smoothness = CheckValueType<RawAffFloat>(context.values().value()[10], "arc", I18n.Translate("compose.chart_format.field.smoothness"));
			}
			if (timing == null || endTiming == null || xStart == null || xEnd == null || curveType == null || yStart == null || yEnd == null || color == null || effect == null || lineType == null)
			{
				return;
			}
			bool valueError = false;
			if (endTiming.data < timing.data)
			{
				chart.warning.Add(I18n.Translate("compose.chart_format.warning.arc_negative_duration",
					(context.values().value()[1].Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
					(context.values().value()[1].Start.Column + 1).ToString(CultureInfo.InvariantCulture)));
				(timing, endTiming) = (endTiming, timing);
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
								chart.warning.Add(I18n.Translate("compose.chart_format.warning.arctap_overflow",
									(@event.values().value()[0].Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
									(@event.values().value()[0].Start.Column + 1).ToString(CultureInfo.InvariantCulture)));
							}
							else
							{
								arctaps.Add(arctap);
							}
						}
						else
						{
							chart.warning.Add(I18n.Translate("compose.chart_format.warning.invalid_arc_subevent",
								(@event.Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
								(@event.Start.Column + 1).ToString(CultureInfo.InvariantCulture)));
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
					CurveType = curveType.Value,
					YStart = yStart.data,
					YEnd = yEnd.data,
					Color = color.data,
					Effect = effect.data,
					LineType = lineType.Value,
					Smoothness = smoothness?.data,
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
			var timing = CheckValueType<RawAffInt>(context.values().value()[0], "arctap", I18n.Translate("compose.chart_format.field.timing"));
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
			var timing = CheckValueType<RawAffInt>(context.values().value()[0], "camera", I18n.Translate("compose.chart_format.field.timing"));
			var moveX = CheckValueType<RawAffFloat>(context.values().value()[1], "camera", I18n.Translate("compose.chart_format.field.x_move"));
			var moveY = CheckValueType<RawAffFloat>(context.values().value()[2], "camera", I18n.Translate("compose.chart_format.field.y_move"));
			var moveZ = CheckValueType<RawAffFloat>(context.values().value()[3], "camera", I18n.Translate("compose.chart_format.field.z_move"));
			var rotateX = CheckValueType<RawAffFloat>(context.values().value()[4], "camera", I18n.Translate("compose.chart_format.field.x_rotate"));
			var rotateY = CheckValueType<RawAffFloat>(context.values().value()[5], "camera", I18n.Translate("compose.chart_format.field.y_rotate"));
			var rotateZ = CheckValueType<RawAffFloat>(context.values().value()[6], "camera", I18n.Translate("compose.chart_format.field.z_rotate"));
			var rawCameraType = CheckValueType<RawAffWord>(context.values().value()[7], "camera", I18n.Translate("compose.chart_format.field.camera_type"));
			var cameraType = ParseWord<CameraEaseType>(cameraTypes, rawCameraType.data, context.values().value()[7], "camera", I18n.Translate("compose.chart_format.field.camera_type"));
			var duration = CheckValueType<RawAffInt>(context.values().value()[8], "camera", I18n.Translate("compose.chart_format.field.duration"));
			if (timing == null || moveX == null || moveY == null || moveZ == null || rotateX == null || rotateY == null || rotateZ == null || cameraType == null || duration == null)
			{
				return;
			}
			bool valueError = false;
			if (duration.data < 0)
			{
				chart.warning.Add(I18n.Translate("compose.chart_format.warning.camera_negative_duration",
					(context.values().value()[8].Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
					(context.values().value()[8].Start.Column + 1).ToString(CultureInfo.InvariantCulture)));
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
			var timing = CheckValueType<RawAffInt>(context.values().value()[0], "scenecontrol", I18n.Translate("compose.chart_format.field.timing"));
			var type = CheckValueType<RawAffWord>(context.values().value()[1], "scenecontrol", I18n.Translate("compose.chart_format.field.type"));
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
				timingGroupAttributes = CheckValueType<RawAffWord>(context.values().value()[0], "timinggroup", I18n.Translate("compose.chart_format.field.tg_properties"));
			}
			foreach (var item in context.segment().body().item())
			{
				IRawAffEvent @event = item.@event().value;
				if (!(@event is IRawAffItem))
				{
					chart.warning.Add(I18n.Translate("compose.chart_format.warning.non_note_event", 
						(item.@event().Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
						(item.@event().Start.Column + 1).ToString(CultureInfo.InvariantCulture),
						item.@event().GetText()));
					continue;
				}
				IRawAffItem rawItem = @event as IRawAffItem;
				if (!(rawItem is IRawAffNestableItem))
				{
					chart.warning.Add(I18n.Translate("compose.chart_format.warning.nested",
						(item.Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
						(item.Start.Column + 1).ToString(CultureInfo.InvariantCulture),
						item.@event().GetText()));
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
				chart.warning.Add(I18n.Translate("compose.chart_format.warning.reject_subevent",
					(context.subevents().Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
					(context.subevents().Start.Column + 1).ToString(CultureInfo.InvariantCulture),
					type));
			}
		}

		void RejectSegment(ArcaeaFileFormatParser.EventContext context, string type)
		{
			if (context.segment() != null)
			{
				chart.warning.Add(I18n.Translate("compose.chart_format.warning.reject_segment",
					(context.subevents().Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
					(context.subevents().Start.Column + 1).ToString(CultureInfo.InvariantCulture),
					type));
			}
		}

		bool CheckValuesCount(ArcaeaFileFormatParser.EventContext context, string type, int count)
		{
			if (context.values().value().Length != count)
			{
				chart.warning.Add(I18n.Translate("compose.chart_format.warning.value_count_mismatch",
					(context.values().Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
					(context.values().Start.Column + 1).ToString(CultureInfo.InvariantCulture),
					type,
					count,
					context.values().value().Length));
				return false;
			}
			return true;
		}

		bool EnsureValuesCount(ArcaeaFileFormatParser.EventContext context, string type, int count)
		{
			if (context.values().value().Length < count)
			{
				chart.warning.Add(I18n.Translate("compose.chart_format.warning.value_missing",
					(context.values().Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
					(context.values().Start.Column + 1).ToString(CultureInfo.InvariantCulture),
					type,
					count,
					context.values().value().Length));
				return false;
			}
			return true;
		}

		bool LimitValuesCount(ArcaeaFileFormatParser.EventContext context, string type, int count)
		{
			if (context.values().value().Length > count)
			{
				chart.warning.Add(I18n.Translate("compose.chart_format.warning.value_exceed",
					(context.values().Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
					(context.values().Start.Column + 1).ToString(CultureInfo.InvariantCulture),
					type,
					count,
					context.values().value().Length));
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
					chart.warning.Add(I18n.Translate("compose.chart_format.warning.value_type",
						(context.Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
						(context.Start.Column + 1).ToString(CultureInfo.InvariantCulture),
						type,
						field));
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
			chart.warning.Add(I18n.Translate("compose.chart_format.warning.out_of_options",
				(context.Start.Line + lineOffset).ToString(CultureInfo.InvariantCulture),
				(context.Start.Column + 1).ToString(CultureInfo.InvariantCulture),
				type,
				field));
			return null;
		}

		Dictionary<string, ArcLineType> lineTypes = new Dictionary<string, ArcLineType>()
		{
			["true"] = ArcLineType.TrueIsVoid,
			["false"] = ArcLineType.FalseNotVoid,
			["designant"] = ArcLineType.Designant
		};

		Dictionary<string, ArcCurveType> curveTypes = new Dictionary<string, ArcCurveType>()
		{
			["b"] = ArcCurveType.B,
			["s"] = ArcCurveType.S,
			["si"] = ArcCurveType.Si,
			["so"] = ArcCurveType.So,
			["sisi"] = ArcCurveType.SiSi,
			["soso"] = ArcCurveType.SoSo,
			["siso"] = ArcCurveType.SiSo,
			["sosi"] = ArcCurveType.SoSi,
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