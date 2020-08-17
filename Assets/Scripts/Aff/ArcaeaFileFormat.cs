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

	public interface IRawAffValue{}

	public class RawAffWord:IRawAffValue{
		public string data;
	}
	public class RawAffFloat:IRawAffValue{
		public float data;
	}
	public class RawAffInt:IRawAffValue{
		public int data;
	}
	public interface IRawAffEvent{}
	public interface IRawAffItem:IRawAffEvent{}
	public class RawAffTap:IRawAffItem{
		public int Timing;
		public int Track;
	}
	public class RawAffHold:IRawAffItem{
		public int Timing;
		public int EndTiming;
		public int Track;
	}
	public class RawAffTiming:IRawAffItem{
		public int Timing;
		public float Bpm;
		public float BeatsPerLine;
	}
	public class RawAffArc:IRawAffItem{
		public int Timing;
		public int EndTiming;
		public float XStart;
		public float XEnd;
		public ArcLineType LineType;
		public float YStart;
		public float YEnd;
		public int Color;
		public bool IsVoid;
		public List<RawAffArctap> ArcTaps;
	}
	public class RawAffCamera:IRawAffItem{
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
	public class RawAffSceneControl:IRawAffItem{
		public int Timing;
		public string Type;
		public List<IRawAffValue> Params;
	}
	public class RawAffArctap:IRawAffEvent{
		public int Timing;
	}
	public class RawAffChart
	{
		public int AudioOffset = 0;
		public List<IRawAffItem> items =new List<IRawAffItem>();
		public List<string> warning = new List<string>();
		public List<string> error = new List<string>();
	}
	public interface IIntoRawItem{
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
					Debug.Log($"[metadata]'{key}':'{value}'");
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
					else
					{
						chart.warning.Add($"第 {metadataLinesCount} 行：不支持的元信息 {key}:{value}，此行会被忽略");
					}
				}
				ICharStream stream = CharStreams.fromTextReader(reader);
				ArcaeaFileFormatLexer lexer = new ArcaeaFileFormatLexer(stream);
				AffErrorListener<int> lexerErrorListener=new AffErrorListener<int>(chart, metadataLinesCount);
				lexer.RemoveErrorListeners();
				lexer.AddErrorListener(lexerErrorListener);
				ITokenStream tokenStream = new CommonTokenStream(lexer);
				ArcaeaFileFormatParser parser = new ArcaeaFileFormatParser(tokenStream);
				parser.BuildParseTree = true;
				AffErrorListener<IToken> parserErrorListener=new AffErrorListener<IToken>(chart, metadataLinesCount);
				parser.RemoveErrorListeners();
				parser.AddErrorListener(parserErrorListener);
				IParseTree tree = parser.file();
				if(chart.error.Count==0){
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
			Debug.Log($"[item]count: {chart.items.Count}");
			return chart;
		}

		public static void DumpToStream(Stream stream,RawAffChart chart){
			TextWriter writer=new StreamWriter(stream);
			writer.WriteLine($"AudioOffset:{chart.AudioOffset}");
			writer.WriteLine($"-");
			foreach (var item in chart.items){
				writeItem(writer,item);
			}
			writer.Close();
		}
		static void writeItem(TextWriter writer,IRawAffItem item){
			if(item is RawAffTiming){
				var timing=item as RawAffTiming;
				writer.WriteLine($"timing({timing.Timing},{timing.Bpm.ToString("f2")},{timing.BeatsPerLine.ToString("f2")});");
			}else if(item is RawAffTap){
				var tap=item as RawAffTap;
				writer.WriteLine($"({tap.Timing},{tap.Track});");
			}else if(item is RawAffHold){
				var hold=item as RawAffHold;
				writer.WriteLine($"hold({hold.Timing},{hold.EndTiming},{hold.Track});");
			}else if(item is RawAffArc){
				var arc=item as RawAffArc;
				if(arc.ArcTaps.Count>0){
					arc.IsVoid=true;
				}
				writer.WriteLine($"arc({arc.Timing},{arc.EndTiming},{arc.XStart.ToString("f2")},{arc.XEnd.ToString("f2")}"+
					$",{ArcChart.ToLineTypeString(arc.LineType)},{arc.YStart.ToString("f2")},{arc.YEnd.ToString("f2")},{arc.Color},none,{arc.IsVoid.ToString().ToLower()})"+
					(arc.ArcTaps.Count>0?$"[{string.Join(",",arc.ArcTaps.Select(e=>$"arctap({e.Timing})"))}]":"")+
					";");
			}else if(item is RawAffCamera){
				var cam=item as RawAffCamera;
				writer.WriteLine($"camera({cam.Timing},{cam.MoveX.ToString("f2")},{cam.MoveY.ToString("f2")},{cam.MoveZ.ToString("f2")},"+
					$"{cam.RotateX.ToString("f2")},{cam.RotateY.ToString("f2")},{cam.RotateZ.ToString("f2")},{ArcChart.ToCameraTypeString(cam.CameraType)},{cam.Duration});");
			}else if(item is RawAffSceneControl){
				var scenecontrol=item as RawAffSceneControl;
				List<string> values=new List<string>();
				values.Add(scenecontrol.Timing.ToString());
				values.Add(scenecontrol.Type);
				foreach (var @param in scenecontrol.Params)
				{
					if(@param is RawAffInt){
						values.Add((@param as RawAffInt).data.ToString());
					}else if(@param is RawAffFloat){
						values.Add((@param as RawAffFloat).data.ToString("f2"));
					}else if(@param is RawAffWord){
						values.Add((@param as RawAffWord).data);
					}
				}
				writer.WriteLine($"scenecontrol({string.Join(",",values)});");
			}
		}
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
		private int lineOffset;

		public AffTypeChecker(RawAffChart chart, int lineOffset = 0)
		{
			this.lineOffset = lineOffset;
			this.chart = chart;
		}

		public override void ExitValue(ArcaeaFileFormatParser.ValueContext context){
			if(context.Int()!=null){
				int data;
				if(int.TryParse(context.Int().GetText(),out data)){
					context.value=new RawAffInt(){data=data};
				}else{
					chart.warning.Add($"第 {context.Int().Symbol.Line + lineOffset} 行第 {context.Int().Symbol.Column + 1} 列，整数无法解析，可能是超出了数据范围");
				}
			}else if(context.Float()!=null){
				float data;
				if(float.TryParse(context.Float().GetText(),out data)){
					context.value=new RawAffFloat(){data=data};
				}else{
					chart.warning.Add($"第 {context.Int().Symbol.Line + lineOffset} 行第 {context.Int().Symbol.Column + 1} 列，浮点数无法解析，可能是超出了数据范围");
				}
			}else if(context.Word()!=null){
				context.value=new RawAffWord(){data=context.Word().GetText()};
			}
		}
		public override void ExitEvent(ArcaeaFileFormatParser.EventContext context){
			if(context.Word()==null){
				GenTap(context);
			}else{
				string tag=context.Word().GetText();
				if(tag=="hold"){
					GenHold(context);
				}else if(tag=="timing"){
					GenTiming(context);
				}else if(tag=="arc"){
					GenArc(context);
				}else if(tag=="arctap"){
					GenArctap(context);
				}else if(tag=="camera"){
					GenCamera(context);
				}else if(tag=="scenecontrol"){
					GenSceneControl(context);
				}else{
					chart.warning.Add($"第 {context.Start.Line + lineOffset} 行第 {context.Start.Column + 1} 列，不支持的事件类型：{context.Word().GetText()}");
				}
			}
		}

		public override void ExitFile(ArcaeaFileFormatParser.FileContext context){
			foreach (var item in context.body().item())
			{
				IRawAffEvent @event=item.@event().value;
				if(@event!=null){
					if(@event is IRawAffItem){
						chart.items.Add(@event as IRawAffItem);
					}else{
						chart.warning.Add($"第 {item.@event().Start.Line + lineOffset} 行第 {item.@event().Start.Column + 1} 列，不可作为物件使用的事件：{item.@event().GetText()}");
					}
				}
			}
		}

		void GenTap(ArcaeaFileFormatParser.EventContext context){
			RejectSubevents(context,"tap");
			RejectSegment(context,"tap");
			if(!CheckValuesCount(context,"tap",2)){
				return;
			}
			var timing=CheckValueType<RawAffInt>(context.values().value()[0],"tap","时间");
			var track=CheckValueType<RawAffInt>(context.values().value()[1],"tap","轨道");
			if(timing==null||track==null){
				return;
			}
			bool valueError=false;
			if(track.data>4||track.data<=0){
				chart.warning.Add($"第 {context.values().value()[1].Start.Line + lineOffset} 行第 {context.values().value()[1].Start.Column + 1} 列，tap 事件的轨道参数超过范围");
				valueError=true;
			}
			if(!valueError){
				context.value=new RawAffTap(){Timing=timing.data,Track=track.data};
			}
		}
		void GenHold(ArcaeaFileFormatParser.EventContext context){
			RejectSubevents(context,"hold");
			RejectSegment(context,"hold");
			if(!CheckValuesCount(context,"hold",3)){
				return;
			}
			var timing=CheckValueType<RawAffInt>(context.values().value()[0],"hold","时间");
			var endTiming=CheckValueType<RawAffInt>(context.values().value()[1],"hold","结束时间");
			var track=CheckValueType<RawAffInt>(context.values().value()[2],"hold","轨道");
			if(timing==null||endTiming==null||track==null){
				return;
			}
			bool valueError=false;
			if(endTiming.data<timing.data){
				chart.warning.Add($"第 {context.values().value()[1].Start.Line + lineOffset} 行第 {context.values().value()[1].Start.Column + 1} 列，hold 事件的结束时间早于开始时间");
				valueError=true;
			}
			if(track.data>4||track.data<=0){
				chart.warning.Add($"第 {context.values().value()[2].Start.Line + lineOffset} 行第 {context.values().value()[2].Start.Column + 1} 列，hold 事件的轨道参数超过范围");
				valueError=true;
			}
			if(!valueError){
				context.value=new RawAffHold(){Timing=timing.data,EndTiming=endTiming.data,Track=track.data};
			}
		}
		void GenTiming(ArcaeaFileFormatParser.EventContext context){
			RejectSubevents(context,"timing");
			RejectSegment(context,"timing");
			if(!CheckValuesCount(context,"timing",3)){
				return;
			}
			var timing=CheckValueType<RawAffInt>(context.values().value()[0],"hold","时间");
			var bpm=CheckValueType<RawAffFloat>(context.values().value()[1],"hold","BPM");
			var segment=CheckValueType<RawAffFloat>(context.values().value()[2],"hold","单个小节拍数");
			if(timing==null||bpm==null||segment==null){
				return;
			}
			bool valueError=false;
			if(segment.data<0){
				chart.warning.Add($"第 {context.values().value()[2].Start.Line + lineOffset} 行第 {context.values().value()[2].Start.Column + 1} 列，timing 事件的单个小节拍数小于 0");
				valueError=true;
			}
			if(!valueError){
				context.value=new RawAffTiming(){Timing=timing.data,Bpm=bpm.data,BeatsPerLine=segment.data};
			}
		}
		void GenArc(ArcaeaFileFormatParser.EventContext context){
			RejectSegment(context,"arc");
			if(!CheckValuesCount(context,"arc",10)){
				return;
			}
			var timing=CheckValueType<RawAffInt>(context.values().value()[0],"arc","时间");
			var endTiming=CheckValueType<RawAffInt>(context.values().value()[1],"arc","结束时间");
			var xStart=CheckValueType<RawAffFloat>(context.values().value()[2],"arc","起点横坐标");
			var xEnd=CheckValueType<RawAffFloat>(context.values().value()[3],"arc","终点横坐标");
			var rawLineType=CheckValueType<RawAffWord>(context.values().value()[4],"arc","arc 类型");
			var lineType=ParseWord(lineTypes,rawLineType.data,context.values().value()[4],"arc","arc 类型");
			var yStart=CheckValueType<RawAffFloat>(context.values().value()[5],"arc","起点纵坐标");
			var yEnd=CheckValueType<RawAffFloat>(context.values().value()[6],"arc","终点纵坐标");
			var color=CheckValueType<RawAffInt>(context.values().value()[7],"arc","颜色");
			var effect=CheckValueType<RawAffWord>(context.values().value()[8],"arc","效果类型");
			var rawIsVoid=CheckValueType<RawAffWord>(context.values().value()[9],"arc","是否黑线");
			var isVoid=ParseWord(bools,rawIsVoid.data,context.values().value()[9],"arc","是否黑线");
			if(timing==null||endTiming==null||xStart==null||xEnd==null||lineTypes==null||yStart==null||yEnd==null||color==null||effect==null||isVoid==null){
				return;
			}
			bool valueError=false;
			if(endTiming.data<timing.data){
				chart.warning.Add($"第 {context.values().value()[1].Start.Line + lineOffset} 行第 {context.values().value()[1].Start.Column + 1} 列，arc 事件的结束时间早于开始时间");
				valueError=true;
			}
			if(color.data<0||color.data>=3){
				chart.warning.Add($"第 {context.values().value()[7].Start.Line + lineOffset} 行第 {context.values().value()[7].Start.Column + 1} 列，arc 事件的颜色超出范围");
				valueError=true;
			}
			List<RawAffArctap> arctaps=new List<RawAffArctap>();
			var subevents=context.subevents();
			if(subevents!=null){
				foreach (var @event in context.subevents().@event())
				{
					if(@event.value!=null){
						if(@event.value is RawAffArctap){
							var arctap=@event.value as RawAffArctap;
							if(arctap.Timing<timing.data||arctap.Timing>endTiming.data){
								chart.warning.Add($"第 {@event.values().value()[0].Start.Line + lineOffset} 行第 {@event.values().value()[0].Start.Column + 1} 列，arctap 事件的时间超出所属 arc 的时间范围");
							}else{
								arctaps.Add(arctap);
							}
						}else{
							chart.warning.Add($"第 {@event.Start.Line + lineOffset} 行第 {@event.Start.Column + 1} 列，arc 事件的子事件必须为 arctap 事件");
						}
					}
				}
			}
			if(!valueError){
				context.value=new RawAffArc(){
					Timing=timing.data,
					EndTiming=endTiming.data,
					XStart=xStart.data,
					XEnd=xEnd.data,
					LineType=lineType.Value,
					YStart=yStart.data,
					YEnd=yEnd.data,
					Color=color.data,
					IsVoid=isVoid.Value,
					ArcTaps=arctaps,
				};
			}
		}
		void GenArctap(ArcaeaFileFormatParser.EventContext context){
			RejectSubevents(context,"arctap");
			RejectSegment(context,"arctap");
			if(!CheckValuesCount(context,"arctap",1)){
				return;
			}
			var timing=CheckValueType<RawAffInt>(context.values().value()[0],"arctap","时间");
			if(timing==null){
				return;
			}
			context.value=new RawAffArctap(){Timing=timing.data};
		}
		void GenCamera(ArcaeaFileFormatParser.EventContext context){
			RejectSubevents(context,"camera");
			RejectSegment(context,"camera");
			if(!CheckValuesCount(context,"camera",9)){
				return;
			}
			var timing=CheckValueType<RawAffInt>(context.values().value()[0],"camera","时间");
			var moveX=CheckValueType<RawAffFloat>(context.values().value()[1],"camera","平移 X 轴");
			var moveY=CheckValueType<RawAffFloat>(context.values().value()[2],"camera","平移 Y 轴");
			var moveZ=CheckValueType<RawAffFloat>(context.values().value()[3],"camera","平移 Z 轴");
			var rotateX=CheckValueType<RawAffFloat>(context.values().value()[4],"camera","旋转 X 轴");
			var rotateY=CheckValueType<RawAffFloat>(context.values().value()[5],"camera","旋转 Y 轴");
			var rotateZ=CheckValueType<RawAffFloat>(context.values().value()[6],"camera","旋转 Z 轴");
			var rawCameraType=CheckValueType<RawAffWord>(context.values().value()[7],"camera","camera 类型");
			var cameraType=ParseWord<CameraEaseType>(cameraTypes,rawCameraType.data,context.values().value()[7],"camera","camera 类型");
			var duration=CheckValueType<RawAffInt>(context.values().value()[8],"camera","时长");
			if(timing==null||moveX==null||moveY==null||moveZ==null||rotateX==null||rotateY==null||rotateZ==null||cameraType==null||duration==null){
				return;
			}
			bool valueError=false;
			if(duration.data<0){
				chart.warning.Add($"第 {context.values().value()[8].Start.Line + lineOffset} 行第 {context.values().value()[8].Start.Column + 1} 列，camera 事件的时长小于零");
				valueError=true;
			}
			if(!valueError){
				context.value=new RawAffCamera(){
					Timing=timing.data,
					MoveX=moveX.data,
					MoveY=moveY.data,
					MoveZ=moveZ.data,
					RotateX=rotateX.data,
					RotateY=rotateY.data,
					RotateZ=rotateZ.data,
					CameraType=cameraType.Value,
					Duration=duration.data,
				};
			}
		}
		void GenSceneControl(ArcaeaFileFormatParser.EventContext context){
			RejectSubevents(context,"scenecontrol");
			RejectSegment(context,"scenecontrol");
			if(!EnsureValuesCount(context,"scenecontrol",2)){
				return;
			}
			var timing=CheckValueType<RawAffInt>(context.values().value()[0],"scenecontrol","时间");
			var type=CheckValueType<RawAffWord>(context.values().value()[1],"scenecontrol","类型");
			if(timing==null||type==null){
				return;
			}
			List<IRawAffValue> @params=new List<IRawAffValue>();
			foreach(var value in context.values().value().Skip(2)){
				if(value.value==null){
					return;
				}
				@params.Add(value.value);
			}
			context.value=new RawAffSceneControl(){
				Timing=timing.data,
				Type=type.data,
				Params=@params,
			};
		}

		void RejectSubevents(ArcaeaFileFormatParser.EventContext context,string type){
			if(context.subevents()!=null){
				chart.warning.Add($"第 {context.subevents().Start.Line + lineOffset} 行第 {context.subevents().Start.Column + 1} 列，{type} 事件不应包含子事件");
			}
		}

		void RejectSegment(ArcaeaFileFormatParser.EventContext context,string type){
			if(context.segment()!=null){
				chart.warning.Add($"第 {context.subevents().Start.Line + lineOffset} 行第 {context.subevents().Start.Column + 1} 列，{type} 事件不应包含事件块");
			}
		}

		bool CheckValuesCount(ArcaeaFileFormatParser.EventContext context,string type,int count){
			if(context.values().value().Length!=count){
				chart.warning.Add($"第 {context.values().Start.Line + lineOffset} 行第 {context.values().Start.Column + 1} 列，{type} 事件的参数个数应当为 {count} 个而非 {context.values().value().Length} 个");
				return false;
			}
			return true;
		}

		bool EnsureValuesCount(ArcaeaFileFormatParser.EventContext context,string type,int count){
			if(context.values().value().Length<count){
				chart.warning.Add($"第 {context.values().Start.Line + lineOffset} 行第 {context.values().Start.Column + 1} 列，{type} 事件的参数个数应当为至少 {count} 个而非 {context.values().value().Length} 个");
				return false;
			}
			return true;
		}

		T CheckValueType<T>(ArcaeaFileFormatParser.ValueContext context,string type,string field)where T:class,IRawAffValue{
			if(context.value!=null){
				if(context.value is T){
					return context.value as T;
				}else{
					chart.warning.Add($"第 {context.Start.Line + lineOffset} 行第 {context.Start.Column + 1} 列，{type} 事件的 {field} 参数的值类型错误");
				}
			}
			return null;
		}

		T? ParseWord<T>(Dictionary<string,T> values,string word,ArcaeaFileFormatParser.ValueContext context,string type,string field) where T:struct{
			if(word==null){
				return null;
			}
			T result;
			if(values.TryGetValue(word,out result)){
				return result;
			}
			chart.warning.Add($"第 {context.Start.Line + lineOffset} 行第 {context.Start.Column + 1} 列，{type} 事件的 {field} 参数的值不是合法的几个值之一");
			return null;
		}

		Dictionary<string,bool> bools=new Dictionary<string, bool>(){
			["true"]=true,
			["false"]=false,
		};

		Dictionary<string,ArcLineType> lineTypes=new Dictionary<string, ArcLineType>(){
			["b"]=ArcLineType.B,
			["s"]=ArcLineType.S,
			["si"]=ArcLineType.Si,
			["so"]=ArcLineType.So,
			["sisi"]=ArcLineType.SiSi,
			["soso"]=ArcLineType.SoSo,
			["siso"]=ArcLineType.SiSo,
			["sosi"]=ArcLineType.SoSi,
		};

		Dictionary<string,CameraEaseType> cameraTypes=new Dictionary<string, CameraEaseType>(){
			["l"]=CameraEaseType.L,
			["s"]=CameraEaseType.S,
			["qi"]=CameraEaseType.Qi,
			["qo"]=CameraEaseType.Qo,
			["reset"]=CameraEaseType.Reset,
		};
	}

	// Here we use patial class to insert custom fields into generated classes
	public partial class ArcaeaFileFormatParser : Parser {
		public partial class ValueContext : ParserRuleContext {
			public IRawAffValue value;
		}
		public partial class EventContext : ParserRuleContext {
			public IRawAffEvent value;
		}
	}

}