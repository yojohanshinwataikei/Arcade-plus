using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arcade.Gameplay.Chart;
using Arcade.Compose.Command;
using Arcade.Gameplay;
using Cysharp.Threading.Tasks;
using System.Threading;
using Arcade.Util.UniTaskHelper;
using Arcade.Util.UI;
using System.Globalization;

namespace Arcade.Compose.Editing
{
	public class AdeValueEditor : AdeOperation, INoteSelectEvent
	{
		public static AdeValueEditor Instance { get; private set; }

		public RectTransform Panel;
		public RectTransform Timing, Track, EndTiming, StartPos, EndPos, CurveType, Color, IsVoid, SelectParent, SeparateArctap, TimingGroup;
		public RectTransform MoveTiming, MoveTrack, MoveEndTiming, MoveStartPos, MoveEndPos;

		public Image IsVoidIntermediate;
		public void OnNoteSelect(ArcNote note)
		{
			UpdateFields();
		}
		public void OnNoteDeselect(ArcNote note)
		{
			UpdateFields();
		}
		public void OnNoteDeselectAll()
		{
			UpdateFields();
		}
		private void OnCommandExecuted(ICommand command, bool undo)
		{
			UpdateFields();
		}

		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			curveTypeDropdownHelper = new DropdownHelper<ArcCurveType?>(CurveType.GetComponentInChildren<Dropdown>());
			colorDropdownHelper = new DropdownHelper<int?>(Color.GetComponentInChildren<Dropdown>());
			timingGroupDropdownHelper = new DropdownHelper<ArcTimingGroupOption?>(TimingGroup.GetComponentInChildren<Dropdown>());
			AdeSelectionManager.Instance.NoteEventListeners.Add(this);
			AdeCommandManager.Instance.onCommandExecuted += OnCommandExecuted;
		}

		private void OnDestroy()
		{
			AdeSelectionManager.Instance.NoteEventListeners.Remove(this);
			AdeCommandManager.Instance.onCommandExecuted -= OnCommandExecuted;
		}

		public void UpdateFields()
		{
			List<ArcNote> selected = AdeSelectionManager.Instance.SelectedNotes;
			int count = selected.Count;
			if (count == 0)
			{
				Panel.gameObject.SetActive(false);
				return;
			}
			else
			{
				SelectParent.gameObject.SetActive(false);
				if (count == 1)
				{
					if (selected[0] is ArcArcTap)
					{
						SelectParent.gameObject.SetActive(true);
					}
				}
				MoveTiming.gameObject.SetActive(count == 1);
				MoveTrack.gameObject.SetActive(count == 1);
				MoveEndTiming.gameObject.SetActive(count == 1);
				MoveStartPos.gameObject.SetActive(count == 1);
				MoveEndPos.gameObject.SetActive(count == 1);
				SeparateArctap.gameObject.SetActive(GetSeparateArctapAvailable());

				UpdateField(
					(note) => true,
					(note) => note.Timing.ToString(CultureInfo.InvariantCulture),
					"-",
					(active) => Timing.gameObject.SetActive(active),
					(data) => Timing.GetComponentInChildren<InputField>().SetTextWithoutNotify(data)
				);
				UpdateField(
					(note) => note is ArcLongNote,
					(note) => (note as ArcLongNote).EndTiming.ToString(CultureInfo.InvariantCulture),
					"-",
					(active) => EndTiming.gameObject.SetActive(active),
					(data) => EndTiming.GetComponentInChildren<InputField>().SetTextWithoutNotify(data)
				);
				UpdateField(
					(note) => note is ArcTap || note is ArcHold,
					(note) => note is ArcTap ? (note as ArcTap).Track.ToString(CultureInfo.InvariantCulture) : (note as ArcHold).Track.ToString(CultureInfo.InvariantCulture),
					"-",
					(active) => Track.gameObject.SetActive(active),
					(data) => Track.GetComponentInChildren<InputField>().SetTextWithoutNotify(data)
				);
				UpdateField(
					(note) => note is ArcArc,
					(note) => $"{(note as ArcArc).XStart.ToString("f2", CultureInfo.InvariantCulture)},{(note as ArcArc).YStart.ToString("f2", CultureInfo.InvariantCulture)}",
					"-,-",
					(active) => StartPos.gameObject.SetActive(active),
					(data) => StartPos.GetComponentInChildren<InputField>().SetTextWithoutNotify(data)
				);
				UpdateField(
					(note) => note is ArcArc,
					(note) => $"{(note as ArcArc).XEnd.ToString("f2", CultureInfo.InvariantCulture)},{(note as ArcArc).YEnd.ToString("f2", CultureInfo.InvariantCulture)}",
					"-,-",
					(active) => EndPos.gameObject.SetActive(active),
					(data) => EndPos.GetComponentInChildren<InputField>().SetTextWithoutNotify(data)
				);
				UpdateField<ArcCurveType?>(
					(note) => note is ArcArc,
					(note) => (note as ArcArc).CurveType,
					null,
					(active) => CurveType.gameObject.SetActive(active),
					(data) => ApplyCurveTypeDropDown(data)
				);
				UpdateField<int?>(
					(note) => note is ArcArc,
					(note) => (note as ArcArc).Color,
					null,
					(active) => Color.gameObject.SetActive(active),
					(data) => ApplyColorDropDown(data)
				);
				UpdateField<bool?>(
					(note) => note is ArcArc,
					(note) => (note as ArcArc).IsVoid,
					null,
					(active) => IsVoid.gameObject.SetActive(active),
					(data) => ApplyIsVoid(data)
				);
				UpdateField<ArcTimingGroupOption?>(
					(note) => note is ISetableTimingGroup,
					(note) => new ArcTimingGroupOption { timingGroup = (note as IHasTimingGroup).TimingGroup },
					null,
					(active) => TimingGroup.gameObject.SetActive(active),
					(data) => ApplyTimingGroupDropdown(data)
				);
				Panel.gameObject.SetActive(true);
			}
		}

		delegate bool GetActive(ArcNote note);
		delegate TData GetValue<TData>(ArcNote note);
		delegate void ApplyActive(bool active);
		delegate void ApplyData<TData>(TData data);

		private void UpdateField<TData>(GetActive getActive, GetValue<TData> getValue, TData genericValue, ApplyActive applyActive, ApplyData<TData> applyData)
		{
			List<ArcNote> selected = AdeSelectionManager.Instance.SelectedNotes;
			bool active = true;
			TData data = default;
			bool first = true;
			foreach (var note in selected)
			{
				if (!getActive(note))
				{
					active = false;
					break;
				}
				TData newData = getValue(note);
				if (first)
				{
					data = newData;
					first = false;
				}
				else
				{
					if (!data.Equals(newData))
					{
						data = genericValue;
					}
				}
			}
			applyActive(active);
			if (active)
			{
				applyData(data);
			}
		}

		private DropdownHelper<ArcCurveType?> curveTypeDropdownHelper;

		private Dictionary<ValueTuple<ArcCurveType?>, string> curveTypeToDropDownLabel = new Dictionary<ValueTuple<ArcCurveType?>, string>{
			{ValueTuple.Create<ArcCurveType?>(ArcCurveType.B),"B"},
			{ValueTuple.Create<ArcCurveType?>(ArcCurveType.S),"S"},
			{ValueTuple.Create<ArcCurveType?>(ArcCurveType.Si),"Si"},
			{ValueTuple.Create<ArcCurveType?>(ArcCurveType.So),"So"},
			{ValueTuple.Create<ArcCurveType?>(ArcCurveType.SiSi),"SiSi"},
			{ValueTuple.Create<ArcCurveType?>(ArcCurveType.SiSo),"SiSo"},
			{ValueTuple.Create<ArcCurveType?>(ArcCurveType.SoSi),"SoSi"},
			{ValueTuple.Create<ArcCurveType?>(ArcCurveType.SoSo),"SoSo"},
			{ValueTuple.Create<ArcCurveType?>(null),"-"},
		};

		private readonly List<ArcCurveType?> defaultCurveTypeOptions = new List<ArcCurveType?>{
			ArcCurveType.B,
			ArcCurveType.S,
			ArcCurveType.Si,
			ArcCurveType.So,
			ArcCurveType.SiSi,
			ArcCurveType.SiSo,
			ArcCurveType.SoSi,
			ArcCurveType.SoSo,
		};
		private readonly List<ArcCurveType?> unknownCurveTypeOptions = new List<ArcCurveType?>{
			null,
			ArcCurveType.B,
			ArcCurveType.S,
			ArcCurveType.Si,
			ArcCurveType.So,
			ArcCurveType.SiSi,
			ArcCurveType.SiSo,
			ArcCurveType.SoSi,
			ArcCurveType.SoSo,
		};

		private void ApplyCurveTypeDropDown(ArcCurveType? data)
		{
			if (data == null)
			{
				curveTypeDropdownHelper.UpdateOptions(unknownCurveTypeOptions, (curveType, _) => curveTypeToDropDownLabel[ValueTuple.Create(curveType)]);
			}
			else
			{
				curveTypeDropdownHelper.UpdateOptions(defaultCurveTypeOptions, (curveType, _) => curveTypeToDropDownLabel[ValueTuple.Create(curveType)]);
			}
			curveTypeDropdownHelper.SetValueWithoutNotify(data);
		}

		private DropdownHelper<int?> colorDropdownHelper;

		private Dictionary<ValueTuple<int?>, string> colorToDropDownLabel = new Dictionary<ValueTuple<int?>, string>{
			{ValueTuple.Create<int?>(null),"-"},
			{ValueTuple.Create<int?>(0),"蓝"},
			{ValueTuple.Create<int?>(1),"红"},
			{ValueTuple.Create<int?>(2),"绿"},
		};

		private string GetColorOptionString(int? color)
		{
			if (colorToDropDownLabel.ContainsKey(ValueTuple.Create(color)))
			{
				return colorToDropDownLabel[ValueTuple.Create(color)];
			}
			else
			{
				return "其他";
			}
		}

		private void ApplyColorDropDown(int? data)
		{
			if (data == null)
			{
				colorDropdownHelper.UpdateOptions(new List<int?> { null, 0, 1 }, (color, _) => GetColorOptionString(color));
			}
			else if (data >= 2 || data < 0)
			{
				colorDropdownHelper.UpdateOptions(new List<int?> { 0, 1, data }, (color, _) => GetColorOptionString(color));
			}
			else
			{
				colorDropdownHelper.UpdateOptions(new List<int?> { 0, 1 }, (color, _) => GetColorOptionString(color));
			}
			colorDropdownHelper.SetValueWithoutNotify(data);
		}

		private void ApplyIsVoid(bool? data)
		{
			IsVoidIntermediate.enabled = data == null;
			IsVoid.GetComponentInChildren<Toggle>().SetIsOnWithoutNotify(data == true);
		}

		private struct ArcTimingGroupOption
		{
			public ArcTimingGroup timingGroup;
		}
		private DropdownHelper<ArcTimingGroupOption?> timingGroupDropdownHelper;

		private string GetArcTimingGroupOptionString(ArcTimingGroupOption? color)
		{
			if (color == null)
			{
				return "-";
			}
			else if (color.Value.timingGroup == null)
			{
				return "默认";
			}
			else
			{
				return color.Value.timingGroup.Id.ToString(CultureInfo.InvariantCulture);
			}
		}
		private void ApplyTimingGroupDropdown(ArcTimingGroupOption? data)
		{
			List<ArcTimingGroupOption?> options = new List<ArcTimingGroupOption?>();
			if (data == null)
			{
				options.Add(null);
			}
			options.Add(new ArcTimingGroupOption { timingGroup = null });
			foreach (var tg in ArcTimingManager.Instance.timingGroups)
			{
				options.Add(new ArcTimingGroupOption { timingGroup = tg });
			}
			timingGroupDropdownHelper.UpdateOptions(options, (option, _) => GetArcTimingGroupOptionString(option));
			timingGroupDropdownHelper.SetValueWithoutNotify(data);
		}

		private struct ValueChangeErrorMessage
		{
			public string message;
		}
		private delegate ValueChangeErrorMessage? ParseValue<TRaw, TValue>(TRaw raw, ref TValue result);
		private delegate ValueChangeErrorMessage? ValidateValue<TValue>(TValue value, ArcNote note);
		private delegate void ApplyValue<TValue>(TValue value, ArcNote note);

		private void HandleValueChange<TRaw, TValue>(TRaw raw, ParseValue<TRaw, TValue> parseValue, ValidateValue<TValue> validateValue, ApplyValue<TValue> applyValue)
		{
			TValue result = default;
			var parseValueErrorMessage = parseValue(raw, ref result);
			if (parseValueErrorMessage != null)
			{
				AdeToast.Instance.Show(parseValueErrorMessage.Value.message);
				UpdateFields();
				return;
			}
			foreach (var note in AdeSelectionManager.Instance.SelectedNotes)
			{
				var validateValueErrorMessage = validateValue(result, note);
				if (validateValueErrorMessage != null)
				{
					AdeToast.Instance.Show(validateValueErrorMessage.Value.message);
					UpdateFields();
					return;
				}
			}
			List<EditArcEventCommand> commands = new List<EditArcEventCommand>();
			foreach (var n in AdeSelectionManager.Instance.SelectedNotes)
			{
				var ne = n.Clone();
				applyValue(result, ne as ArcNote);
				commands.Add(new EditArcEventCommand(n, ne));
			}
			if (commands.Count == 1)
			{
				AdeCommandManager.Instance.Add(commands[0]);
			}
			else if (commands.Count > 1)
			{
				AdeCommandManager.Instance.Add(new BatchCommand(commands.ToArray(), "批量修改 Note"));
			}
		}

		public void OnTiming(InputField inputField)
		{
			HandleValueChange(
				inputField.text,
				(string raw, ref int result) =>
				{
					if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
					{
						return new ValueChangeErrorMessage { message = "时间数值格式错误" };
					}
					if (result < 0)
					{
						return new ValueChangeErrorMessage { message = "时间数值不能为负数" };
					}
					return null;
				},
				(value, note) =>
				{
					if (note is ArcHold hold)
					{
						if (hold.EndTiming <= value)
						{
							return new ValueChangeErrorMessage { message = "Hold 的时间不能晚于结束时间" };
						}
					}
					if (note is ArcArc arc)
					{
						if (arc.EndTiming < value || (arc.EndTiming <= value && arc.ArcTaps.Count > 0))
						{
							return new ValueChangeErrorMessage { message = "Arc 的时间不能晚于结束时间" };
						}
						foreach (var arctap in arc.ArcTaps)
						{
							if (arctap.Timing < value)
							{
								if (!AdeSelectionManager.Instance.SelectedNotes.Contains(arctap))
								{
									return new ValueChangeErrorMessage { message = "Arc 的时间不能晚于其上 Arctap 的时间" };
								}
							}
						}
					}
					if (note is ArcArcTap)
					{
						var arctap = note as ArcArcTap;
						if (arctap.Arc.Timing > value || arctap.Arc.EndTiming < value)
						{
							if (!AdeSelectionManager.Instance.SelectedNotes.Contains(arctap.Arc))
							{
								return new ValueChangeErrorMessage { message = "Arctap 的时间不能超过所在 Arc 的时间范围" };
							}
						}
					}
					return null;
				},
				(value, note) =>
				{
					note.Timing = value;
				}
			);
		}
		public void OnTrack(InputField inputField)
		{
			HandleValueChange(
				inputField.text,
				(string raw, ref int result) =>
				{
					if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
					{
						return new ValueChangeErrorMessage { message = "轨道数值格式错误" };
					}
					if (result < 0 || result > 5)
					{
						return new ValueChangeErrorMessage { message = "轨道只能为 0 - 5" };
					}
					return null;
				},
				(value, note) =>
				{
					return null;
				},
				(value, note) =>
				{
					if (note is ArcTap tap)
					{
						tap.Track = value;
					}
					if (note is ArcHold hold)
					{
						hold.Track = value;
					}
				}
			);
		}
		public void OnEndTiming(InputField inputField)
		{
			HandleValueChange(
				inputField.text,
				(string raw, ref int result) =>
				{
					if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
					{
						return new ValueChangeErrorMessage { message = "时间数值格式错误" };
					}
					if (result < 0)
					{
						return new ValueChangeErrorMessage { message = "时间数值不能为负数" };
					}
					return null;
				},
				(value, note) =>
				{
					if (note is ArcHold)
					{
						var hold = note as ArcHold;
						if (hold.Timing >= value)
						{
							return new ValueChangeErrorMessage { message = "Hold 的结束时间不能早于时间" };
						}
					}
					if (note is ArcArc)
					{
						var arc = note as ArcArc;
						if (arc.Timing > value || (arc.Timing >= value && arc.ArcTaps.Count > 0))
						{
							return new ValueChangeErrorMessage { message = "Arc 的结束时间不能早于时间" };
						}
						foreach (var arctap in arc.ArcTaps)
						{
							if (arctap.Timing > value)
							{
								return new ValueChangeErrorMessage { message = "Arc 的结束时间不早于其上 Arctap 的时间" };
							}
						}
					}
					return null;
				},
				(value, note) =>
				{
					(note as ArcLongNote).EndTiming = value;
				}
			);
		}

		private ValueChangeErrorMessage? ParseCoord(string raw, ref Vector2 result)
		{
			string[] coords = raw.Split(',');
			float x, y;
			if (coords.Length != 2)
			{
				return new ValueChangeErrorMessage { message = "坐标位置格式错误" };
			}
			if (!float.TryParse(coords[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x))
			{
				return new ValueChangeErrorMessage { message = "坐标位置格式错误" };
			}
			if (!float.TryParse(coords[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
			{
				return new ValueChangeErrorMessage { message = "坐标位置格式错误" };
			}
			result = new Vector2(x, y);
			return null;
		}

		public void OnStartPos(InputField inputField)
		{
			HandleValueChange<string, Vector2>(
				inputField.text,
				ParseCoord,
				(value, note) =>
				{
					return null;
				},
				(value, note) =>
				{
					(note as ArcArc).XStart = value.x;
					(note as ArcArc).YStart = value.y;
				}
			);
		}
		public void OnEndPos(InputField inputField)
		{
			HandleValueChange<string, Vector2>(
				inputField.text,
				ParseCoord,
				(value, note) =>
				{
					return null;
				},
				(value, note) =>
				{
					(note as ArcArc).XEnd = value.x;
					(note as ArcArc).YEnd = value.y;
				}
			);
		}
		public void OnCurveType(Dropdown dropdown)
		{
			ArcCurveType? curveType = curveTypeDropdownHelper.QueryDataById(dropdown.value);
			if (curveType == null)
			{
				return;
			}
			HandleValueChange(
				curveType.Value,
				(ArcCurveType raw, ref ArcCurveType result) =>
				{
					result = raw;
					return null;
				},
				(value, note) =>
				{
					return null;
				},
				(value, note) =>
				{
					(note as ArcArc).CurveType = value;
				}
			);
		}
		public void OnColor(Dropdown dropdown)
		{
			int? color = colorDropdownHelper.QueryDataById(dropdown.value);
			if (color == null)
			{
				return;
			}
			HandleValueChange(
				color.Value,
				(int raw, ref int result) =>
				{
					result = raw;
					return null;
				},
				(value, note) =>
				{
					return null;
				},
				(value, note) =>
				{
					(note as ArcArc).Color = value;
				}
			);
		}
		public void OnIsVoid(Toggle toggle)
		{
			HandleValueChange(
				toggle.isOn,
				(bool raw, ref bool result) =>
				{
					result = raw;
					return null;
				},
				(value, note) =>
				{
					if (note is ArcArc arc)
					{
						if ((!value) && arc.ArcTaps.Count > 0)
						{
							return new ValueChangeErrorMessage { message = "Arc 含有 ArcTap 时必须是黑线" };
						}
					}
					return null;
				},
				(value, note) =>
				{
					(note as ArcArc).IsVoid = value;
				}
			);
		}
		public void OnTimingGroup(Dropdown dropdown)
		{
			ArcTimingGroupOption? timingGroupOption = timingGroupDropdownHelper.QueryDataById(dropdown.value);
			if (timingGroupOption == null)
			{
				return;
			}
			HandleValueChange(
				timingGroupOption.Value,
				(ArcTimingGroupOption raw, ref ArcTimingGroupOption result) =>
				{
					result = raw;
					return null;
				},
				(value, note) =>
				{
					return null;
				},
				(value, note) =>
				{
					(note as ISetableTimingGroup).TimingGroup = value.timingGroup;
				}
			);
		}

		public void OnSelectParent()
		{
			List<ArcNote> selectedNotes = AdeSelectionManager.Instance.SelectedNotes;
			if (selectedNotes.Count == 1)
			{
				if (selectedNotes[0] is ArcArcTap arcTap)
				{
					ArcArc arc = arcTap.Arc;
					AdeSelectionManager.Instance.DeselectAllNotes();
					AdeSelectionManager.Instance.SelectNote(arc);
				}
			}
		}
		private bool GetSeparateArctapAvailable()
		{
			bool allVoidArc = true;
			bool hasArctap = false;
			foreach (var note in AdeSelectionManager.Instance.SelectedNotes)
			{
				if (note is ArcArc arc)
				{
					if (arc.IsVoid)
					{
						if (arc.ArcTaps.Count > 0 && arc.EndTiming - arc.Timing > 1)
						{
							hasArctap = true;
						}
						continue;
					}
				}
				allVoidArc = false;
			}
			return allVoidArc && hasArctap;
		}

		public void OnSeparateArctap()
		{
			List<ICommand> commands = new List<ICommand>();
			foreach (var note in AdeSelectionManager.Instance.SelectedNotes)
			{
				if (note is ArcArc arc)
				{
					if (arc.EndTiming - arc.Timing <= 1)
					{
						continue;
					}
					foreach (var arcTap in arc.ArcTaps)
					{
						int timing = arcTap.Timing;
						float t = 1f * (timing - arc.Timing) / (arc.EndTiming - arc.Timing);
						float x = ArcAlgorithm.X(arc.XStart, arc.XEnd, t, arc.CurveType);
						float y = ArcAlgorithm.Y(arc.YStart, arc.YEnd, t, arc.CurveType);
						x = Mathf.RoundToInt(x * 100) / 100f;
						y = Mathf.RoundToInt(y * 100) / 100f;
						commands.Add(new RemoveArcTapCommand(arc, arcTap));
						ArcArc newArc = new ArcArc()
						{
							Timing = timing,
							EndTiming = timing + 1,
							Color = arc.Color,
							Effect = "none",
							IsVoid = true,
							CurveType = ArcCurveType.S,
							TimingGroup = arc.TimingGroup,
							XStart = x,
							XEnd = x,
							YStart = y,
							YEnd = y,
						};
						commands.Add(new AddArcEventCommand(newArc));
						ArcArcTap newArcTap = new ArcArcTap() { Timing = timing };
						commands.Add(new AddArcTapCommand(newArc, newArcTap));

					}
				}
			}
			if (commands.Count > 0)
			{
				AdeCommandManager.Instance.Add(new BatchCommand(commands.ToArray(), "分离 Arctap"));
			}
		}

		private bool IsValidTiming(ArcNote note, int timing)
		{
			if (note is ArcArc arc)
			{
				if (timing > arc.EndTiming)
				{
					return false;
				}
				foreach (ArcArcTap arctap in arc.ArcTaps)
				{
					if (timing > arctap.Timing)
					{
						return false;
					}
				}
			}
			else
			if (note is ArcHold hold)
			{
				if (timing >= hold.EndTiming)
				{
					return false;
				}
			}
			else
			if (note is ArcArcTap arcTap)
			{
				if (timing > arcTap.Arc.EndTiming || timing < arcTap.Arc.Timing)
				{
					return false;
				}
			}
			return true;
		}
		private async UniTask ReselectTiming(CancellationToken cancellationToken)
		{
			var selected = AdeSelectionManager.Instance.SelectedNotes;
			if (selected.Count != 1)
			{
				return;
			}
			ArcNote note = selected[0];
			ArcNote newNote = note.Clone() as ArcNote;
			AdeSelectionManager.Instance.DeselectAllNotes();

			EditArcEventCommand command = new EditArcEventCommand(note, newNote);

			AdeCommandManager.Instance.Prepare(command);
			try
			{
				while (true)
				{
					Action<int> updateTiming = (int timing) =>
					{
						if (IsValidTiming(note, timing))
						{
							(note as ArcArcTap)?.RemoveArcTapConnection();
							note.Timing = timing;
							newNote.Timing = timing;
							(note as ArcArcTap)?.Relocate();
							(note as ArcArc)?.Rebuild();
							(note as ArcTap)?.SetupArcTapConnection();
							(note as ArcArc)?.CalculateJudgeTimings();
							(note as ArcHold)?.CalculateJudgeTimings();
							if (note is ArcArc) ArcArcManager.Instance.CalculateArcRelationship();
							ArcGameplayManager.Instance.ResetJudge();
						}
					};
					var newTiming = await AdeCursorManager.Instance.SelectTiming(Progress.Create(updateTiming), cancellationToken);
					updateTiming(newTiming);
					if (IsValidTiming(note, newTiming))
					{
						break;
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				AdeCommandManager.Instance.Cancel();
				AdeSelectionManager.Instance.SelectNote(note);
				throw ex;
			}
			AdeCommandManager.Instance.Commit();
			AdeSelectionManager.Instance.SelectNote(note);
		}

		private bool IsValidEndTiming(ArcNote note, int timing)
		{
			if (note is ArcArc arc)
			{
				if (timing < arc.Timing)
				{
					return false;
				}
				foreach (ArcArcTap arctap in arc.ArcTaps)
				{
					if (timing < arctap.Timing)
					{
						return false;
					}
				}
			}
			else
			if (note is ArcHold hold)
			{
				if (timing <= hold.Timing)
				{
					return false;
				}
			}
			return true;
		}

		private async UniTask ReselectEndTiming(CancellationToken cancellationToken)
		{
			var selected = AdeSelectionManager.Instance.SelectedNotes;
			if (selected.Count != 1)
			{
				return;
			}
			ArcNote note = selected[0];
			if (!(note is ArcArc || note is ArcHold))
			{
				return;
			}
			ArcNote newNote = note.Clone() as ArcNote;
			AdeSelectionManager.Instance.DeselectAllNotes();

			EditArcEventCommand command = new EditArcEventCommand(note, newNote);

			AdeCommandManager.Instance.Prepare(command);
			try
			{
				while (true)
				{
					Action<int> updateEndTiming = (int endTiming) =>
					{
						if (IsValidEndTiming(note, endTiming))
						{
							if (note is ArcArc)
							{
								(note as ArcArc).EndTiming = endTiming;
								(newNote as ArcArc).EndTiming = endTiming;
							}
							else if (note is ArcHold)
							{
								(note as ArcHold).EndTiming = endTiming;
								(newNote as ArcHold).EndTiming = endTiming;
							}
							(note as ArcArc)?.Rebuild();
							(note as ArcArc)?.CalculateJudgeTimings();
							(note as ArcHold)?.CalculateJudgeTimings();
							if (note is ArcArc) ArcArcManager.Instance.CalculateArcRelationship();
							ArcGameplayManager.Instance.ResetJudge();
						}
					};
					var newEndTiming = await AdeCursorManager.Instance.SelectTiming(Progress.Create(updateEndTiming), cancellationToken);
					updateEndTiming(newEndTiming);
					if (IsValidEndTiming(note, newEndTiming))
					{
						break;
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				AdeCommandManager.Instance.Cancel();
				AdeSelectionManager.Instance.SelectNote(note);
				throw ex;
			}
			AdeCommandManager.Instance.Commit();
			AdeSelectionManager.Instance.SelectNote(note);
		}

		private async UniTask ReselectStartCoordinate(CancellationToken cancellationToken)
		{
			var selected = AdeSelectionManager.Instance.SelectedNotes;
			if (selected.Count != 1)
			{
				return;
			}
			if (!(selected[0] is ArcArc))
			{
				return;
			}
			ArcArc note = selected[0] as ArcArc;
			ArcArc newNote = note.Clone() as ArcArc;
			AdeSelectionManager.Instance.DeselectAllNotes();

			EditArcEventCommand command = new EditArcEventCommand(note, newNote);

			AdeCommandManager.Instance.Prepare(command);
			try
			{
				Action<Vector2> updateStartCoordinate = (Vector2 coordinate) =>
				{
					note.XStart = coordinate.x;
					newNote.XStart = coordinate.x;
					note.YStart = coordinate.y;
					newNote.YStart = coordinate.y;
					note.Rebuild();
					note.CalculateJudgeTimings();
					ArcArcManager.Instance.CalculateArcRelationship();
				};
				var newCoordinate = await AdeCursorManager.Instance.SelectCoordinate(note.Timing, Progress.Create(updateStartCoordinate), cancellationToken);
				updateStartCoordinate(newCoordinate);
			}
			catch (OperationCanceledException ex)
			{
				AdeCommandManager.Instance.Cancel();
				AdeSelectionManager.Instance.SelectNote(note);
				throw ex;
			}
			AdeCommandManager.Instance.Commit();
			AdeSelectionManager.Instance.SelectNote(note);
		}

		private async UniTask ReselectEndCoordinate(CancellationToken cancellationToken)
		{
			var selected = AdeSelectionManager.Instance.SelectedNotes;
			if (selected.Count != 1)
			{
				return;
			}
			if (!(selected[0] is ArcArc))
			{
				return;
			}
			ArcArc note = selected[0] as ArcArc;
			ArcArc newNote = note.Clone() as ArcArc;
			AdeSelectionManager.Instance.DeselectAllNotes();

			EditArcEventCommand command = new EditArcEventCommand(note, newNote);

			AdeCommandManager.Instance.Prepare(command);
			try
			{
				Action<Vector2> updateEndCoordinate = (Vector2 coordinate) =>
				{
					note.XEnd = coordinate.x;
					newNote.XEnd = coordinate.x;
					note.YEnd = coordinate.y;
					newNote.YEnd = coordinate.y;
					note.Rebuild();
					note.CalculateJudgeTimings();
					ArcArcManager.Instance.CalculateArcRelationship();
				};
				var newCoordinate = await AdeCursorManager.Instance.SelectCoordinate(note.EndTiming, Progress.Create(updateEndCoordinate), cancellationToken);
				updateEndCoordinate(newCoordinate);
			}
			catch (OperationCanceledException ex)
			{
				AdeCommandManager.Instance.Cancel();
				AdeSelectionManager.Instance.SelectNote(note);
				throw ex;
			}
			AdeCommandManager.Instance.Commit();
			AdeSelectionManager.Instance.SelectNote(note);
		}

		private async UniTask ReselectTrack(CancellationToken cancellationToken)
		{
			var selected = AdeSelectionManager.Instance.SelectedNotes;
			if (selected.Count != 1)
			{
				return;
			}
			ArcNote note = selected[0];
			if (!(note is ArcTap || note is ArcHold))
			{
				return;
			}
			ArcNote newNote = note.Clone() as ArcNote;
			AdeSelectionManager.Instance.DeselectAllNotes();

			EditArcEventCommand command = new EditArcEventCommand(note, newNote);

			AdeCommandManager.Instance.Prepare(command);
			try
			{
				while (true)
				{
					Action<int> updateTrack = (int track) =>
					{
						if (IsValidTiming(note, track))
						{
							if (note is ArcTap)
							{
								(note as ArcTap).Track = track;
								(newNote as ArcTap).Track = track;
							}
							else if (note is ArcHold)
							{
								(note as ArcHold).Track = track;
								(newNote as ArcHold).Track = track;
							}
							(note as ArcTap)?.SetupArcTapConnection();
							(note as ArcHold)?.CalculateJudgeTimings();
						}
					};
					var newTrack = await AdeCursorManager.Instance.SelectTrack(Progress.Create(updateTrack), cancellationToken);
					updateTrack(newTrack);
					if (IsValidTiming(note, newTrack))
					{
						break;
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				AdeCommandManager.Instance.Cancel();
				AdeSelectionManager.Instance.SelectNote(note);
				throw ex;
			}
			AdeCommandManager.Instance.Commit();
			AdeSelectionManager.Instance.SelectNote(note);
		}

		public void OnReselectTiming()
		{
			AdeOperationManager.Instance.TryExecuteOperation(() =>
			{
				var cancellation = new CancellationTokenSource();
				return new AdeOngoingOperation
				{
					task = ReselectTiming(cancellation.Token).WithExceptionLogger(),
					cancellation = cancellation,
				};
			});
		}

		public void OnReselectEndTiming()
		{
			AdeOperationManager.Instance.TryExecuteOperation(() =>
			{
				var cancellation = new CancellationTokenSource();
				return new AdeOngoingOperation
				{
					task = ReselectEndTiming(cancellation.Token).WithExceptionLogger(),
					cancellation = cancellation,
				};
			});
		}

		public void OnReselectStartCoordinate()
		{
			AdeOperationManager.Instance.TryExecuteOperation(() =>
			{
				var cancellation = new CancellationTokenSource();
				return new AdeOngoingOperation
				{
					task = ReselectStartCoordinate(cancellation.Token).WithExceptionLogger(),
					cancellation = cancellation,
				};
			});
		}

		public void OnReselectEndCoordinate()
		{
			AdeOperationManager.Instance.TryExecuteOperation(() =>
			{
				var cancellation = new CancellationTokenSource();
				return new AdeOngoingOperation
				{
					task = ReselectEndCoordinate(cancellation.Token).WithExceptionLogger(),
					cancellation = cancellation,
				};
			});
		}

		public void OnReselectTrack()
		{
			AdeOperationManager.Instance.TryExecuteOperation(() =>
			{
				var cancellation = new CancellationTokenSource();
				return new AdeOngoingOperation
				{
					task = ReselectTrack(cancellation.Token).WithExceptionLogger(),
					cancellation = cancellation,
				};
			});
		}

		public override AdeOperationResult TryExecuteOperation()
		{
			if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ReselectTiming))
			{
				var cancellation = new CancellationTokenSource();
				return AdeOperationResult.FromOngoingOperation(new AdeOngoingOperation
				{
					task = ReselectTiming(cancellation.Token).WithExceptionLogger(),
					cancellation = cancellation,
				});
			}
			else if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ReselectEndTiming))
			{
				var cancellation = new CancellationTokenSource();
				return AdeOperationResult.FromOngoingOperation(new AdeOngoingOperation
				{
					task = ReselectEndTiming(cancellation.Token).WithExceptionLogger(),
					cancellation = cancellation,
				});
			}
			else if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ReselectStartCoordinate))
			{
				var cancellation = new CancellationTokenSource();
				return AdeOperationResult.FromOngoingOperation(new AdeOngoingOperation
				{
					task = ReselectStartCoordinate(cancellation.Token).WithExceptionLogger(),
					cancellation = cancellation,
				});
			}
			else if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ReselectEndCoordinate))
			{
				var cancellation = new CancellationTokenSource();
				return AdeOperationResult.FromOngoingOperation(new AdeOngoingOperation
				{
					task = ReselectEndCoordinate(cancellation.Token).WithExceptionLogger(),
					cancellation = cancellation,
				});
			}
			else if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.ReselectTrack))
			{
				var cancellation = new CancellationTokenSource();
				return AdeOperationResult.FromOngoingOperation(new AdeOngoingOperation
				{
					task = ReselectTrack(cancellation.Token).WithExceptionLogger(),
					cancellation = cancellation,
				});
			}
			return false;
		}
	}
}
