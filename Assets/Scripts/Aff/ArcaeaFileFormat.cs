using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Arcade.Aff
{

	public class RawAffChart
	{
		public int AudioOffset = 0;
		public List<string> warning = new List<string>();
		public List<string> error = new List<string>();
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
				ParseTreeWalker.Default.Walk(new AffTypeChecker(chart, metadataLinesCount), tree);
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
			chart.error.Add($"第 {line + lineOffset} 行第 {charPositionInLine} 列，谱面文件语法错误：{msg}");
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

		public override void VisitTerminal(ITerminalNode node)
		{

		}
	}
}