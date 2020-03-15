using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arcade.Gameplay.Chart;
using Arcade.Compose.Command;

namespace Arcade.Compose.Editing
{
	public class AdeValueEditor : MonoBehaviour, INoteSelectEvent
	{
		public static AdeValueEditor Instance { get; private set; }

		public RectTransform Panel;
		public RectTransform Timing, Track, EndTiming, StartPos, EndPos, LineType, Color, IsVoid;

		private void OnPlay()
		{

		}
		private void OnPause()
		{

		}
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
			ArcadeComposeManager.Instance.OnPlay.AddListener(OnPlay);
			ArcadeComposeManager.Instance.OnPause.AddListener(OnPause);
		}
		private void OnDestroy()
		{
			AdeCursorManager.Instance.NoteEventListeners.Remove(this);
			ArcadeComposeManager.Instance.OnPlay.RemoveListener(OnPlay);
			ArcadeComposeManager.Instance.OnPause.RemoveListener(OnPause);
		}

		private bool canEdit = false;
		private void MakeupFields()
		{
			canEdit = false;
			List<ArcNote> selected = AdeCursorManager.Instance.SelectedNotes;
			int count = selected.Count;
			if (count == 0)
			{
				Panel.gameObject.SetActive(false);
				return;
			}
			else
			{
				Timing.gameObject.SetActive(true);
				Track.gameObject.SetActive(true);
				EndTiming.gameObject.SetActive(true);
				StartPos.gameObject.SetActive(true);
				EndPos.gameObject.SetActive(true);
				LineType.gameObject.SetActive(true);
				Color.gameObject.SetActive(true);
				IsVoid.gameObject.SetActive(true);
				foreach (var s in selected)
				{
					if (Track.gameObject.activeSelf) Track.gameObject.SetActive(s is ArcTap || s is ArcHold);
					if (EndTiming.gameObject.activeSelf) EndTiming.gameObject.SetActive(s is ArcLongNote);
					if (StartPos.gameObject.activeSelf) StartPos.gameObject.SetActive(s is ArcArc);
					if (EndPos.gameObject.activeSelf) EndPos.gameObject.SetActive(s is ArcArc);
					if (LineType.gameObject.activeSelf) LineType.gameObject.SetActive(s is ArcArc);
					if (Color.gameObject.activeSelf) Color.gameObject.SetActive(s is ArcArc);
					if (IsVoid.gameObject.activeSelf) IsVoid.gameObject.SetActive(s is ArcArc);
				}
				bool multiple = count != 1;
				ArcNote note = selected[0];
				Timing.GetComponentInChildren<InputField>().text = multiple ? "-" : note.Timing.ToString();
				if (Track.gameObject.activeSelf)
				{
					Track.GetComponentInChildren<InputField>().text = multiple ? "-" : (note is ArcTap ? (note as ArcTap).Track.ToString() : (note as ArcHold).Track.ToString());
				}
				if (EndTiming.gameObject.activeSelf)
				{
					EndTiming.GetComponentInChildren<InputField>().text = multiple ? "-" : (note as ArcLongNote).EndTiming.ToString();
				}
				if (StartPos.gameObject.activeSelf)
				{
					StartPos.GetComponentInChildren<InputField>().text = multiple ? "-,-" : $"{(note as ArcArc).XStart.ToString("f2")},{(note as ArcArc).YStart.ToString("f2")}";
				}
				if (EndPos.gameObject.activeSelf)
				{
					EndPos.GetComponentInChildren<InputField>().text = multiple ? "-,-" : $"{(note as ArcArc).XEnd.ToString("f2")},{(note as ArcArc).YEnd.ToString("f2")}";
				}
				if (LineType.gameObject.activeSelf)
				{
					LineType.GetComponentInChildren<Dropdown>().value = multiple ? 0 : (int)(note as ArcArc).LineType;
				}
				if (Color.gameObject.activeSelf)
				{
					Color.GetComponentInChildren<Dropdown>().value = multiple ? 0 : (note as ArcArc).Color;
				}
				if (IsVoid.gameObject.activeSelf)
				{
					IsVoid.GetComponentInChildren<Toggle>().isOn = multiple ? false : (note as ArcArc).IsVoid;
				}
				Panel.gameObject.SetActive(true);
			}
			canEdit = true;
		}

		public void OnTiming(InputField inputField)
		{
			if (!canEdit) return;
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
			}
			catch (Exception Ex)
			{
				AdeToast.Instance.Show("赋值时出现错误");
				Debug.LogException(Ex);
			}
		}
		public void OnTrack(InputField inputField)
		{
			if (!canEdit) return;
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
			if (!canEdit) return;
			try
			{
				string t = inputField.text;
				int endTiming = int.Parse(t);
				List<EditArcEventCommand> commands=new List<EditArcEventCommand>();
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcLongNote;
					ne.EndTiming = endTiming;
					commands.Add(new EditArcEventCommand(n, ne));
				}
				if(commands.Count==1){
					CommandManager.Instance.Add(commands[0]);
				}else if(commands.Count>1){
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(),"批量修改 Note"));
				}
			}
			catch (Exception Ex)
			{
				AdeToast.Instance.Show("赋值时出现错误");
				Debug.LogException(Ex);
			}
		}
		public void OnStartPos(InputField inputField)
		{
			if (!canEdit) return;
			try
			{
				string t = inputField.text;
				string[] ts = t.Split(',');
				float x = float.Parse(ts[0]);
				float y = float.Parse(ts[1]);
				List<EditArcEventCommand> commands=new List<EditArcEventCommand>();
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.XStart = x;
					ne.YStart = y;
					commands.Add(new EditArcEventCommand(n, ne));
				}
				if(commands.Count==1){
					CommandManager.Instance.Add(commands[0]);
				}else if(commands.Count>1){
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(),"批量修改 Note"));
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
			if (!canEdit) return;
			try
			{
				string t = inputField.text;
				string[] ts = t.Split(',');
				float x = float.Parse(ts[0]);
				float y = float.Parse(ts[1]);
				List<EditArcEventCommand> commands=new List<EditArcEventCommand>();
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.XEnd = x;
					ne.YEnd = y;
					commands.Add(new EditArcEventCommand(n, ne));
				}
				if(commands.Count==1){
					CommandManager.Instance.Add(commands[0]);
				}else if(commands.Count>1){
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(),"批量修改 Note"));
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
			if (!canEdit) return;
			try
			{
				List<EditArcEventCommand> commands=new List<EditArcEventCommand>();
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.LineType = (ArcLineType)dropdown.value;
					commands.Add(new EditArcEventCommand(n, ne));
				}
				if(commands.Count==1){
					CommandManager.Instance.Add(commands[0]);
				}else if(commands.Count>1){
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(),"批量修改 Note"));
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
			if (!canEdit) return;
			try
			{
				List<EditArcEventCommand> commands=new List<EditArcEventCommand>();
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.Color = dropdown.value;
					commands.Add(new EditArcEventCommand(n, ne));
				}
				if(commands.Count==1){
					CommandManager.Instance.Add(commands[0]);
				}else if(commands.Count>1){
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(),"批量修改 Note"));
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
			if (!canEdit) return;
			try
			{
				List<EditArcEventCommand> commands=new List<EditArcEventCommand>();
				foreach (var n in AdeCursorManager.Instance.SelectedNotes)
				{
					var ne = n.Clone() as ArcArc;
					ne.IsVoid = toggle.isOn;
					commands.Add(new EditArcEventCommand(n, ne));
				}
				if(commands.Count==1){
					CommandManager.Instance.Add(commands[0]);
				}else if(commands.Count>1){
					CommandManager.Instance.Add(new BatchCommand(commands.ToArray(),"批量修改 Note"));
				}
			}
			catch (Exception Ex)
			{
				AdeToast.Instance.Show("赋值时出现错误");
				Debug.LogException(Ex);
			}
		}
	}
}