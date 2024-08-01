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

namespace Arcade.Compose.Editing
{
	public class AdeValueEditor : AdeOperation, INoteSelectEvent
	{
		public static AdeValueEditor Instance { get; private set; }

		public RectTransform Panel;
		public RectTransform Timing, Track, EndTiming, StartPos, EndPos, LineType, Color, IsVoid, SelectParent, TimingGroup;
		public RectTransform MoveTiming, MoveTrack, MoveEndTiming, MoveStartPos, MoveEndPos;

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
			AdeSelectionManager.Instance.NoteEventListeners.Add(this);
			AdeCommandManager.Instance.onCommandExecuted+=OnCommandExecuted;
		}

        private void OnDestroy()
		{
			AdeSelectionManager.Instance.NoteEventListeners.Remove(this);
			AdeCommandManager.Instance.onCommandExecuted-=OnCommandExecuted;
		}

		private void UpdateFields()
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

				UpdateField(
					(note) => true,
					(note) => note.Timing.ToString(),
					"-",
					(active) => Timing.gameObject.SetActive(active),
					(data) => Timing.GetComponentInChildren<InputField>().SetTextWithoutNotify(data)
				);
				UpdateField(
					(note) => note is ArcLongNote,
					(note) => (note as ArcLongNote).EndTiming.ToString(),
					"-",
					(active) => EndTiming.gameObject.SetActive(active),
					(data) => EndTiming.GetComponentInChildren<InputField>().SetTextWithoutNotify(data)
				);
				UpdateField(
					(note) => note is ArcTap || note is ArcHold,
					(note) => note is ArcTap ? (note as ArcTap).Track.ToString() : (note as ArcHold).Track.ToString(),
					"-",
					(active) => Track.gameObject.SetActive(active),
					(data) => Track.GetComponentInChildren<InputField>().SetTextWithoutNotify(data)
				);
				UpdateField(
					(note) => note is ArcArc,
					(note) => $"{(note as ArcArc).XStart:f2},{(note as ArcArc).YStart:f2}",
					"-,-",
					(active) => StartPos.gameObject.SetActive(active),
					(data) => StartPos.GetComponentInChildren<InputField>().SetTextWithoutNotify(data)
				);
				UpdateField(
					(note) => note is ArcArc,
					(note) => $"{(note as ArcArc).XEnd:f2},{(note as ArcArc).YEnd:f2}",
					"-,-",
					(active) => EndPos.gameObject.SetActive(active),
					(data) => EndPos.GetComponentInChildren<InputField>().SetTextWithoutNotify(data)
				);
				UpdateField(
					(note) => note is ArcArc,
					(note) => (note as ArcArc).LineType,
					ArcLineType.Unknown,
					(active) => LineType.gameObject.SetActive(active),
					(data) => ApplyLineTypeDropDown(data)
				);

				Color.gameObject.SetActive(true);
				IsVoid.gameObject.SetActive(true);
				TimingGroup.gameObject.SetActive(true);
				foreach (var s in selected)
				{
					if (Color.gameObject.activeSelf) Color.gameObject.SetActive(s is ArcArc);
					if (IsVoid.gameObject.activeSelf) IsVoid.gameObject.SetActive(s is ArcArc);
					if (TimingGroup.gameObject.activeSelf) TimingGroup.gameObject.SetActive(s is ISetableTimingGroup);
				}
				bool multiple = count != 1;
				ArcNote note = selected[0];
				if (Color.gameObject.activeSelf)
				{
					Color.GetComponentInChildren<Dropdown>().interactable = true;
					Color.GetComponentInChildren<Dropdown>().SetValueWithoutNotify(multiple ? 0 : (note as ArcArc).Color);
					if (!multiple)
					{
						if ((note as ArcArc).Color >= 3)
						{
							Color.GetComponentInChildren<Dropdown>().interactable = false;
						}
					}
				}
				if (IsVoid.gameObject.activeSelf)
				{
					IsVoid.GetComponentInChildren<Toggle>().SetIsOnWithoutNotify(multiple ? false : (note as ArcArc).IsVoid);
				}
				if (TimingGroup.gameObject.activeSelf)
				{
					TimingGroup.GetComponentInChildren<Dropdown>().SetValueWithoutNotify(multiple ? 0 : ((note as IHasTimingGroup).TimingGroup?.Id ?? 0));
				}
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

		private List<ArcLineType> lineTypeOptions = new List<ArcLineType>();

		private Dictionary<ArcLineType, int> lineTypeIds = new Dictionary<ArcLineType, int>();

		private Dictionary<ArcLineType, string> lineTypeToDropDownLabel = new Dictionary<ArcLineType, string>{
			{ArcLineType.B,"B"},
			{ArcLineType.S,"S"},
			{ArcLineType.Si,"Si"},
			{ArcLineType.So,"So"},
			{ArcLineType.SiSi,"SiSi"},
			{ArcLineType.SiSo,"SiSo"},
			{ArcLineType.SoSi,"SoSi"},
			{ArcLineType.SoSo,"SoSo"},
			{ArcLineType.Unknown,"-"},
		};

		private void UpdateLineTypeOptions(List<ArcLineType> options)
		{
			Dropdown dropdown = LineType.GetComponentInChildren<Dropdown>();
			dropdown.ClearOptions();
			List<string> optionStrings = new List<string>();
			lineTypeIds = new Dictionary<ArcLineType, int>();
			foreach (var option in options)
			{
				optionStrings.Add(lineTypeToDropDownLabel[option]);
			}
			for (int i = 0; i < options.Count; i++)
			{
				lineTypeIds.Add(options[i], i);
			}
			dropdown.AddOptions(optionStrings);
			lineTypeOptions = options;
		}

		private readonly List<ArcLineType> defaultLineTypeOptions = new List<ArcLineType>{
			ArcLineType.B,
			ArcLineType.S,
			ArcLineType.Si,
			ArcLineType.So,
			ArcLineType.SiSi,
			ArcLineType.SiSo,
			ArcLineType.SoSi,
			ArcLineType.SoSo,
		};
		private readonly List<ArcLineType> unknownLineTypeOptions = new List<ArcLineType>{
			ArcLineType.Unknown,
			ArcLineType.B,
			ArcLineType.S,
			ArcLineType.Si,
			ArcLineType.So,
			ArcLineType.SiSi,
			ArcLineType.SiSo,
			ArcLineType.SoSi,
			ArcLineType.SoSo,
		};

		private void ApplyLineTypeDropDown(ArcLineType data)
		{
			if (data == ArcLineType.Unknown)
			{
				UpdateLineTypeOptions(unknownLineTypeOptions);
			}
			else
			{
				UpdateLineTypeOptions(defaultLineTypeOptions);
			}
			Dropdown dropdown = LineType.GetComponentInChildren<Dropdown>();
			dropdown.SetValueWithoutNotify(lineTypeIds[data]);
		}

		public void OnTiming(InputField inputField)
		{
			try
			{
				string t = inputField.text;
				int timing = int.Parse(t);
				List<EditArcEventCommand> commands = new List<EditArcEventCommand>();
				foreach (var n in AdeSelectionManager.Instance.SelectedNotes)
				{
					var ne = n.Clone();
					ne.Timing = timing;
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
				ArcGameplayManager.Instance.ResetJudge();
			}
			catch (Exception Ex)
			{
				AdeToast.Instance.Show("赋值时出现错误");
				Debug.LogException(Ex);
			}
		}
		public void OnTrack(InputField inputField)
		{
			try
			{
				string t = inputField.text;
				int track = int.Parse(t);
				if (track < 0 || track > 5) throw new InvalidDataException("轨道只能为 0 - 5");
				List<EditArcEventCommand> commands = new List<EditArcEventCommand>();
				foreach (var n in AdeSelectionManager.Instance.SelectedNotes)
				{
					if (n is ArcTap)
					{
						ArcTap ne = n.Clone() as ArcTap;
						ne.Track = track;
						commands.Add(new EditArcEventCommand(n, ne));

					}
					else if (n is ArcHold)
					{
						ArcHold ne = n.Clone() as ArcHold;
						ne.Track = track;
						commands.Add(new EditArcEventCommand(n, ne));
					}
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
			catch (Exception Ex)
			{
				AdeToast.Instance.Show("赋值时出现错误");
				Debug.LogException(Ex);
			}
		}
		public void OnEndTiming(InputField inputField)
		{
			try
			{
				string t = inputField.text;
				int endTiming = int.Parse(t);
				List<EditArcEventCommand> commands = new List<EditArcEventCommand>();
				foreach (var n in AdeSelectionManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcLongNote;
					ne.EndTiming = endTiming;
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
				ArcGameplayManager.Instance.ResetJudge();
			}
			catch (Exception Ex)
			{
				AdeToast.Instance.Show("赋值时出现错误");
				Debug.LogException(Ex);
			}
		}
		public void OnStartPos(InputField inputField)
		{
			try
			{
				string t = inputField.text;
				string[] ts = t.Split(',');
				float x = float.Parse(ts[0]);
				float y = float.Parse(ts[1]);
				List<EditArcEventCommand> commands = new List<EditArcEventCommand>();
				foreach (var n in AdeSelectionManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.XStart = x;
					ne.YStart = y;
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
			catch (Exception Ex)
			{
				AdeToast.Instance.Show("赋值时出现错误");
				Debug.LogException(Ex);
			}
		}
		public void OnEndPos(InputField inputField)
		{
			try
			{
				string t = inputField.text;
				string[] ts = t.Split(',');
				float x = float.Parse(ts[0]);
				float y = float.Parse(ts[1]);
				List<EditArcEventCommand> commands = new List<EditArcEventCommand>();
				foreach (var n in AdeSelectionManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.XEnd = x;
					ne.YEnd = y;
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
			catch (Exception Ex)
			{
				AdeToast.Instance.Show("赋值时出现错误");
				Debug.LogException(Ex);
			}
		}
		public void OnLineType(Dropdown dropdown)
		{
			try
			{
				List<EditArcEventCommand> commands = new List<EditArcEventCommand>();
				ArcLineType lineType = lineTypeOptions[dropdown.value];
				if (lineType == ArcLineType.Unknown)
				{
					return;
				}
				foreach (var n in AdeSelectionManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.LineType = lineType;
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
			catch (Exception Ex)
			{
				AdeToast.Instance.Show("赋值时出现错误");
				Debug.LogException(Ex);
			}
		}
		public void OnColor(Dropdown dropdown)
		{
			try
			{
				List<EditArcEventCommand> commands = new List<EditArcEventCommand>();
				foreach (var n in AdeSelectionManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.Color = dropdown.value;
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
			catch (Exception Ex)
			{
				AdeToast.Instance.Show("赋值时出现错误");
				Debug.LogException(Ex);
			}
		}
		public void OnIsVoid(Toggle toggle)
		{
			try
			{
				List<EditArcEventCommand> commands = new List<EditArcEventCommand>();
				foreach (var n in AdeSelectionManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.IsVoid = toggle.isOn;
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
			catch (Exception Ex)
			{
				AdeToast.Instance.Show("赋值时出现错误");
				Debug.LogException(Ex);
			}
		}

		public void OnTimingGroup(Dropdown dropdown)
		{
			try
			{
				List<EditArcEventCommand> commands = new List<EditArcEventCommand>();
				foreach (var n in AdeSelectionManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ISetableTimingGroup;
					ne.TimingGroup = dropdown.value == 0 ? null : ArcTimingManager.Instance.timingGroups[dropdown.value - 1];
					commands.Add(new EditArcEventCommand(n, ne as ArcEvent));
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
			catch (Exception Ex)
			{
				AdeToast.Instance.Show("赋值时出现错误");
				Debug.LogException(Ex);
			}
		}

		public void OnSelectParent()
		{
			List<ArcNote> selectedNotes = AdeSelectionManager.Instance.SelectedNotes;
			if (selectedNotes.Count == 1)
			{
				if (selectedNotes[0] is ArcArcTap)
				{
					ArcArc arc = (selectedNotes[0] as ArcArcTap).Arc;
					AdeSelectionManager.Instance.DeselectAllNotes();
					AdeSelectionManager.Instance.SelectNote(arc);
				}
			}
		}

		public void UpdateTimingGroupOptions()
		{
			List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
			options.Add(new Dropdown.OptionData { text = "默认" });
			foreach (var tg in ArcTimingManager.Instance.timingGroups)
			{
				options.Add(new Dropdown.OptionData { text = tg.Id.ToString() });
			}
			TimingGroup.GetComponentInChildren<Dropdown>().options = options;
			List<ArcNote> selected = AdeSelectionManager.Instance.SelectedNotes;
			int count = selected.Count;
			if (TimingGroup.gameObject.activeSelf && count > 0)
			{
				bool multiple = count != 1;
				ArcNote note = selected[0];
				TimingGroup.GetComponentInChildren<Dropdown>().SetValueWithoutNotify(multiple ? 0 : ((note as IHasTimingGroup).TimingGroup?.Id ?? 0));
			}
		}

		private bool IsValidTiming(ArcNote note, int timing)
		{
			if (note is ArcArc)
			{
				var arc = note as ArcArc;
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
			if (note is ArcHold)
			{
				var hold = note as ArcHold;
				if (timing >= hold.EndTiming)
				{
					return false;
				}
			}
			else
			if (note is ArcArcTap)
			{
				var arcTap = note as ArcArcTap;
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
			if (note is ArcArc)
			{
				var arc = note as ArcArc;
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
			if (note is ArcHold)
			{
				var hold = note as ArcHold;
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
