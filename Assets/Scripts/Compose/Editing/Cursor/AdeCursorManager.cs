using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Arcade.Gameplay;
using Arcade.Util.UnityExtension;
using Arcade.Gameplay.Chart;
using UnityEngine.Events;
using Arcade.Compose.Command;
using Arcade.Compose.Editing;
using Arcade.Compose.MarkingMenu;

namespace Arcade.Compose
{
	public enum CursorMode
	{
		Idle,
		Horizontal,
		Vertical
	}
	public class OnNoteEvent : UnityEvent<ArcNote>
	{

	}
	public interface INoteSelectEvent
	{
		void OnNoteSelect(ArcNote note);
		void OnNoteDeselect(ArcNote note);
		void OnNoteDeselectAll();
	}

	public class AdeCursorManager : MonoBehaviour, IMarkingMenuItemProvider
	{
		public static AdeCursorManager Instance { get; private set; }

		public Camera GameplayCamera;
		public MeshCollider HorizontalCollider, VerticalCollider;
		public LineRenderer HorizontalX, HorizontalY, VerticalX, VerticalY;
		public MeshRenderer VerticalRenderer;

		public Transform ArcTapCursor;
		public MeshRenderer ArcTapCursorRenderer;

		public Text InfoText;
		public GameObject InfoGameObject;
		public RectTransform InfoRect;

		public MarkingMenuItem DeleteItem;

		private CursorMode mode;
		private bool enableHorizontal, enableVertical, enableVerticalPanel;
		private bool enableArcTapCursor, enableInfo;
		private RaycastHit horizontalHit, verticalHit;

		public bool EnableHorizontal
		{
			get
			{
				return enableHorizontal;
			}
			set
			{
				if (enableHorizontal != value)
				{
					HorizontalX.enabled = value;
					HorizontalY.enabled = value;
					HorizontalX.positionCount = 0;
					HorizontalY.positionCount = 0;
					enableHorizontal = value;
				}
			}
		}
		public bool EnableVertical
		{
			get
			{
				return enableVertical;
			}
			set
			{
				if (enableVertical != value)
				{
					VerticalX.enabled = value;
					VerticalY.enabled = value;
					VerticalX.positionCount = 0;
					VerticalY.positionCount = 0;
					EnableVerticalPanel = value;
					enableVertical = value;
				}
			}
		}
		public bool EnableVerticalPanel
		{
			get
			{
				return enableVerticalPanel;
			}
			set
			{
				if (enableVerticalPanel != value)
				{
					VerticalRenderer.enabled = value;
					enableVerticalPanel = value;
				}
			}
		}
		public bool EnableArcTapCursor
		{
			get
			{
				return enableArcTapCursor;
			}
			set
			{
				if (enableArcTapCursor != value)
				{
					ArcTapCursorRenderer.enabled = value;
					enableArcTapCursor = value;
				}
			}
		}
		public bool EnableInfo
		{
			get
			{
				return enableInfo;
			}
			set
			{
				if (enableInfo != value)
				{
					InfoGameObject.SetActive(value);
					enableInfo = value;
				}
			}
		}
		public Vector2 ArcTapCursorPosition
		{
			get
			{
				return ArcTapCursor.localPosition;
			}
			set
			{
				ArcTapCursor.localPosition = value;
			}
		}
		public CursorMode Mode
		{
			get
			{
				return mode;
			}
			set
			{
				mode = value;
				if (mode != CursorMode.Horizontal) EnableHorizontal = false;
				if (mode != CursorMode.Vertical) EnableVertical = false;
			}
		}

		public bool IsHorizontalHit { get; set; }
		public bool IsVerticalHit { get; set; }
		public Vector3 HorizontalPoint
		{
			get
			{
				return horizontalHit.point;
			}
		}
		public Vector3 VerticalPoint
		{
			get
			{
				return verticalHit.point;
			}
		}
		public Vector3 AttachedHorizontalPoint
		{
			get
			{
				float z = AdeGridManager.Instance.AttachBeatline(horizontalHit.point.z);
				return new Vector3(horizontalHit.point.x, horizontalHit.point.y, z);
			}
		}
		public Vector3 AttachedVerticalPoint
		{
			get
			{
				return new Vector3(ArcAlgorithm.ArcXToWorld(AdeGridManager.Instance.AttachVerticalX(ArcAlgorithm.WorldXToArc(VerticalPoint.x))),
				   ArcAlgorithm.ArcYToWorld(AdeGridManager.Instance.AttachVerticalY(ArcAlgorithm.WorldYToArc(VerticalPoint.y))));
			}
		}

		public float AttachedTiming
		{
			get
			{
				if (!ArcGameplayManager.Instance.IsLoaded) return 0;
				Vector3 pos = AttachedHorizontalPoint;
				var timingGroup = AdeTimingEditor.Instance.currentTimingGroup;
				return ArcTimingManager.Instance.CalculateTimingByPosition(-pos.z * 1000, timingGroup) - ArcAudioManager.Instance.AudioOffset;
			}
		}

		public bool IsOnly
		{
			get
			{
				return false;
			}
		}
		public MarkingMenuItem[] Items
		{
			get
			{
				return SelectedNotes.Count == 0 ? new MarkingMenuItem[] { } : new MarkingMenuItem[] { DeleteItem };
			}
		}

		public List<ArcNote> SelectedNotes = new List<ArcNote>();

		public List<INoteSelectEvent> NoteEventListeners = new List<INoteSelectEvent>();

		private void Start()
		{
			AdeMarkingMenuManager.Instance.Providers.Add(this);
		}
		private void OnDestroy()
		{
			AdeMarkingMenuManager.Instance.Providers.Remove(this);
		}

		private void Awake()
		{
			Instance = this;
		}
		private void Update()
		{
			UpdateHorizontal();
			UpdateVertical();
			RaycastSelecting();
			UpdateInfo();
			DeleteListener();
		}

		private void UpdateHorizontal()
		{
			Ray ray = GameplayCamera.ScreenPointToRay();
			IsHorizontalHit = HorizontalCollider.Raycast(ray, out horizontalHit, 120);
			if (Mode != CursorMode.Horizontal) return;
			EnableHorizontal = IsHorizontalHit;
			if (IsHorizontalHit)
			{
				float z = AdeGridManager.Instance.AttachBeatline(horizontalHit.point.z);
				HorizontalX.DrawLine(new Vector3(-8.5f, z), new Vector3(8.5f, z));
				HorizontalY.DrawLine(new Vector3(horizontalHit.point.x, 0), new Vector3(horizontalHit.point.x, -100));
				VerticalCollider.transform.localPosition = new Vector3(0, 0, z);
			}
		}
		private void UpdateVertical()
		{
			Ray ray = GameplayCamera.ScreenPointToRay();
			IsVerticalHit = VerticalCollider.Raycast(ray, out verticalHit, 120);
			if (Mode != CursorMode.Vertical) return;
			EnableVertical = IsVerticalHit;
			if (IsVerticalHit)
			{
				VerticalX.DrawLine(new Vector3(-8.5f, AttachedVerticalPoint.y), new Vector3(8.5f, AttachedVerticalPoint.y));
				VerticalY.DrawLine(new Vector3(AttachedVerticalPoint.x, 0), new Vector3(AttachedVerticalPoint.x, 5.5f));
			}
		}
		private void RaycastSelecting()
		{
			if (Input.GetMouseButtonDown(0))
			{
				Ray ray = GameplayCamera.ScreenPointToRay();

				RaycastHit[] hits = Physics.RaycastAll(ray, 120, 1 << 9);
				ArcNote n = null;
				float distance = float.MaxValue;
				foreach (var h in hits)
				{
					ArcNote t = ArcGameplayManager.Instance.FindNoteByRaycastHit(h);
					if (t != null)
					{
						if (h.distance < distance)
						{
							distance = h.distance;
							n = t;
						}
					}
				}
				if (n != null)
				{
					if (!Input.GetKey(KeyCode.LeftControl))
					{
						DeselectAllNotes();
						SelectNote(n);
					}
					else
					{
						if (SelectedNotes.Contains(n)) DeselectNote(n);
						else SelectNote(n);
					}
				}
				else
				{
					if (!Input.GetKey(KeyCode.LeftControl) && IsHorizontalHit)
					{
						DeselectAllNotes();
					}
				}
			}
		}
		private void DeleteListener()
		{
			if (Input.GetKeyDown(KeyCode.Delete))
			{
				DeleteSelectedNotes();
			}
		}
		private void UpdateInfo()
		{
			EnableInfo = EnableVertical || EnableHorizontal;
			string content = string.Empty;
			if (!EnableInfo) return;
			content += $"音乐时间: {AttachedTiming + ArcAudioManager.Instance.AudioOffset}\n";
			content += $"谱面时间: {AttachedTiming}";
			if (EnableVertical)
			{
				Vector3 pos = AttachedVerticalPoint;
				content += $"\n坐标: ({ArcAlgorithm.WorldXToArc(pos.x).ToString("f2")},{ArcAlgorithm.WorldYToArc(pos.y).ToString("f2")})";
			}
			if (AdeClickToCreate.Instance.Enable && AdeClickToCreate.Instance.Mode != ClickToCreateMode.Idle)
			{
				content += $"\n点立得: {AdeClickToCreate.Instance.Mode.ToString()}";
				if (AdeClickToCreate.Instance.Mode == ClickToCreateMode.Arc)
				{
					content += $"\n{AdeClickToCreate.Instance.CurrentArcColor}/{AdeClickToCreate.Instance.CurrentArcIsVoid}/{AdeClickToCreate.Instance.CurrentArcType}";
				}
			}
			if (SelectedNotes.Count == 1 && SelectedNotes[0] is ArcArc)
			{
				ArcArc arc = SelectedNotes[0] as ArcArc;
				float p = (AttachedTiming - arc.Timing) / (arc.EndTiming - arc.Timing);
				if (p >= 0 && p <= 1)
				{
					float x = ArcAlgorithm.X(arc.XStart, arc.XEnd, p, arc.LineType);
					float y = ArcAlgorithm.Y(arc.YStart, arc.YEnd, p, arc.LineType);
					content += $"\nArc: {(p * 100).ToString("f2")}%, {x.ToString("f2")}, {y.ToString("f2")}";
				}
			}
			InfoText.text = content;
		}

		public void SelectNotesInRange(int start, int end)
		{
			if (!Input.GetKey(KeyCode.LeftControl))
			{
				DeselectAllNotes();
			}
			List<ArcNote> list = new List<ArcNote>();
			list.AddRange(ArcTapNoteManager.Instance.Taps.Where((ArcTap a) => a.Timing >= start && a.Timing <= end));
			list.AddRange(ArcHoldNoteManager.Instance.Holds.Where((ArcHold a) => a.Timing >= start && a.Timing <= end && a.EndTiming >= start && a.EndTiming <= end));
			IEnumerable<ArcArc> enumerable = ArcArcManager.Instance.Arcs.Where((ArcArc a) => a.Timing >= start && a.Timing <= end && a.EndTiming >= start && a.EndTiming <= end);
			list.AddRange(enumerable);
			foreach (ArcArc item in enumerable)
			{
				list.AddRange(item.ArcTaps);
			}
			foreach (ArcNote item2 in list)
			{
				if (item2.Instance != null)
				{
					item2.Selected = true;
				}
				if (SelectedNotes.Contains(item2))
				{
					continue;
				}
				SelectedNotes.Add(item2);
				foreach (INoteSelectEvent noteEventListener in NoteEventListeners)
				{
					noteEventListener.OnNoteSelect(item2);
				}
			}
		}

		public void SelectNote(ArcNote note)
		{
			if (note.Instance != null) note.Selected = true;
			if (!SelectedNotes.Contains(note))
			{
				SelectedNotes.Add(note);
				foreach (var l in NoteEventListeners) l.OnNoteSelect(note);
			}
		}
		public void DeselectNote(ArcNote note)
		{
			if (note.Instance != null) note.Selected = false;
			if (SelectedNotes.Contains(note))
			{
				SelectedNotes.Remove(note);
				foreach (var l in NoteEventListeners) l.OnNoteDeselect(note);
			}
		}
		public void DeselectAllNotes()
		{
			foreach (var note in SelectedNotes) if (note.Instance != null) note.Selected = false;
			SelectedNotes.Clear();
			foreach (var l in NoteEventListeners) l.OnNoteDeselectAll();
		}
		public void DeleteSelectedNotes()
		{
			List<ICommand> deleteCommands = new List<ICommand>();
			foreach (var s in SelectedNotes)
			{
				if (s is ArcArcTap) deleteCommands.Add(new RemoveArcTapCommand((s as ArcArcTap).Arc, s as ArcArcTap));
				else deleteCommands.Add(new RemoveArcEventCommand(s));
			}
			if (deleteCommands.Count != 0) CommandManager.Instance.Add(new BatchCommand(deleteCommands.ToArray(), "删除"));
			SelectedNotes.Clear();
		}
	}
}
