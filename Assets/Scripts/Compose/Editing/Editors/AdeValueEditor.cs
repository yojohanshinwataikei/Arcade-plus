using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arcade.Gameplay.Chart;
using Arcade.Compose.Command;
using Arcade.Gameplay;

namespace Arcade.Compose.Editing
{
	public class AdeValueEditor : MonoBehaviour, INoteSelectEvent
	{
		public static AdeValueEditor Instance { get; private set; }

		public RectTransform Panel;
		public RectTransform Timing, Track, EndTiming, StartPos, EndPos, LineType, Color, IsVoid, SelectParent, TimingGroup;

		public void OnNoteSelect(ArcNote note)
		{
			MakeupFields();
		}
		public void OnNoteDeselect(ArcNote note)
		{
			MakeupFields();
		}
		public void OnNoteDeselectAll()
		{
			MakeupFields();
		}

		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			AdeCursorManager.Instance.NoteEventListeners.Add(this);
		}
		private void OnDestroy()
		{
			AdeCursorManager.Instance.NoteEventListeners.Remove(this);
		}

		private void MakeupFields()
		{
			List<ArcNote> selected = AdeCursorManager.Instance.SelectedNotes;
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
				Timing.gameObject.SetActive(true);
				Track.gameObject.SetActive(true);
				EndTiming.gameObject.SetActive(true);
				StartPos.gameObject.SetActive(true);
				EndPos.gameObject.SetActive(true);
				LineType.gameObject.SetActive(true);
				Color.gameObject.SetActive(true);
				IsVoid.gameObject.SetActive(true);
				TimingGroup.gameObject.SetActive(true);
				foreach (var s in selected)
				{
					if (Track.gameObject.activeSelf) Track.gameObject.SetActive(s is ArcTap || s is ArcHold);
					if (EndTiming.gameObject.activeSelf) EndTiming.gameObject.SetActive(s is ArcLongNote);
					if (StartPos.gameObject.activeSelf) StartPos.gameObject.SetActive(s is ArcArc);
					if (EndPos.gameObject.activeSelf) EndPos.gameObject.SetActive(s is ArcArc);
					if (LineType.gameObject.activeSelf) LineType.gameObject.SetActive(s is ArcArc);
					if (Color.gameObject.activeSelf) Color.gameObject.SetActive(s is ArcArc);
					if (IsVoid.gameObject.activeSelf) IsVoid.gameObject.SetActive(s is ArcArc);
					if (TimingGroup.gameObject.activeSelf) TimingGroup.gameObject.SetActive(s is ISetableTimingGroup);
				}
				bool multiple = count != 1;
				ArcNote note = selected[0];
				Timing.GetComponentInChildren<InputField>().SetTextWithoutNotify(multiple ? "-" : note.Timing.ToString());
				if (Track.gameObject.activeSelf)
				{
					Track.GetComponentInChildren<InputField>().SetTextWithoutNotify(multiple ? "-" : (note is ArcTap ? (note as ArcTap).Track.ToString() : (note as ArcHold).Track.ToString()));
				}
				if (EndTiming.gameObject.activeSelf)
				{
					EndTiming.GetComponentInChildren<InputField>().SetTextWithoutNotify(multiple ? "-" : (note as ArcLongNote).EndTiming.ToString());
				}
				if (StartPos.gameObject.activeSelf)
				{
					StartPos.GetComponentInChildren<InputField>().SetTextWithoutNotify(multiple ? "-,-" : $"{(note as ArcArc).XStart.ToString("f2")},{(note as ArcArc).YStart.ToString("f2")}");
				}
				if (EndPos.gameObject.activeSelf)
				{
					EndPos.GetComponentInChildren<InputField>().SetTextWithoutNotify(multiple ? "-,-" : $"{(note as ArcArc).XEnd.ToString("f2")},{(note as ArcArc).YEnd.ToString("f2")}");
				}
				if (LineType.gameObject.activeSelf)
				{
					LineType.GetComponentInChildren<Dropdown>().SetValueWithoutNotify(multiple ? 0 : (int)(note as ArcArc).LineType);
				}
				if (Color.gameObject.activeSelf)
				{
					Color.GetComponentInChildren<Dropdown>().SetValueWithoutNotify(multiple ? 0 : (note as ArcArc).Color);
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

		public void OnTiming(InputField inputField)
		{
			try
			{
				string t = inputField.text;
				int timing = int.Parse(t);
				List<EditArcEventCommand> commands = new List<EditArcEventCommand>();
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
				{
					var ne = n.Clone();
					ne.Timing = timing;
					commands.Add(new EditArcEventCommand(n, ne));
				}
				if (commands.Count == 1)
				{
					CommandManager.Instance.Add(commands[0]);
				}
				else if (commands.Count > 1)
				{
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(), "批量修改 Note"));
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
				if (track <= 0 || track >= 5) throw new InvalidDataException("轨道只能为 1 - 4");
				List<EditArcEventCommand> commands = new List<EditArcEventCommand>();
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
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
					CommandManager.Instance.Add(commands[0]);
				}
				else if (commands.Count > 1)
				{
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(), "批量修改 Note"));
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
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcLongNote;
					ne.EndTiming = endTiming;
					commands.Add(new EditArcEventCommand(n, ne));
				}
				if (commands.Count == 1)
				{
					CommandManager.Instance.Add(commands[0]);
				}
				else if (commands.Count > 1)
				{
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(), "批量修改 Note"));
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
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.XStart = x;
					ne.YStart = y;
					commands.Add(new EditArcEventCommand(n, ne));
				}
				if (commands.Count == 1)
				{
					CommandManager.Instance.Add(commands[0]);
				}
				else if (commands.Count > 1)
				{
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(), "批量修改 Note"));
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
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.XEnd = x;
					ne.YEnd = y;
					commands.Add(new EditArcEventCommand(n, ne));
				}
				if (commands.Count == 1)
				{
					CommandManager.Instance.Add(commands[0]);
				}
				else if (commands.Count > 1)
				{
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(), "批量修改 Note"));
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
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.LineType = (ArcLineType)dropdown.value;
					commands.Add(new EditArcEventCommand(n, ne));
				}
				if (commands.Count == 1)
				{
					CommandManager.Instance.Add(commands[0]);
				}
				else if (commands.Count > 1)
				{
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(), "批量修改 Note"));
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
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.Color = dropdown.value;
					commands.Add(new EditArcEventCommand(n, ne));
				}
				if (commands.Count == 1)
				{
					CommandManager.Instance.Add(commands[0]);
				}
				else if (commands.Count > 1)
				{
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(), "批量修改 Note"));
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
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.IsVoid = toggle.isOn;
					commands.Add(new EditArcEventCommand(n, ne));
				}
				if (commands.Count == 1)
				{
					CommandManager.Instance.Add(commands[0]);
				}
				else if (commands.Count > 1)
				{
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(), "批量修改 Note"));
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
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ISetableTimingGroup;
					ne.TimingGroup = dropdown.value == 0 ? null : ArcTimingManager.Instance.timingGroups[dropdown.value - 1];
					commands.Add(new EditArcEventCommand(n, ne as ArcEvent));
				}
				if (commands.Count == 1)
				{
					CommandManager.Instance.Add(commands[0]);
				}
				else if (commands.Count > 1)
				{
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(), "批量修改 Note"));
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
			List<ArcNote> selectedNotes = AdeCursorManager.Instance.SelectedNotes;
			if (selectedNotes.Count == 1)
			{
				if (selectedNotes[0] is ArcArcTap)
				{
					ArcArc arc = (selectedNotes[0] as ArcArcTap).Arc;
					AdeCursorManager.Instance.DeselectAllNotes();
					AdeCursorManager.Instance.SelectNote(arc);
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
			List<ArcNote> selected = AdeCursorManager.Instance.SelectedNotes;
			int count = selected.Count;
			if (TimingGroup.gameObject.activeSelf && count > 0)
			{
				bool multiple = count != 1;
				ArcNote note = selected[0];
				TimingGroup.GetComponentInChildren<Dropdown>().SetValueWithoutNotify(multiple ? 0 : ((note as IHasTimingGroup).TimingGroup?.Id ?? 0));
			}
		}
	}
}
