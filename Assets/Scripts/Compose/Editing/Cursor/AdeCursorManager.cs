using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arcade.Gameplay;
using Arcade.Util.UnityExtension;
using Arcade.Gameplay.Chart;
using UnityEngine.Events;
using Arcade.Compose.Command;
using Arcade.Compose.Editing;
using Arcade.Compose.MarkingMenu;
using UnityEngine.InputSystem;

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
		public GameObject VerticalPanel;
		public LineRenderer HorizontalX, HorizontalY, VerticalX, VerticalY;
		public MeshRenderer VerticalRenderer;

		public Transform ArcTapCursor;
		public MeshRenderer ArcTapCursorRenderer;
		public Transform SfxArcTapCursor;
		public MeshRenderer SfxArcTapCursorRenderer;

		public MarkingMenuItem DeleteItem;

		private CursorMode mode;
		private bool enableHorizontal, enableVertical, enableVerticalPanel;
		private bool enableArcTapCursor,arcTapCursorIsSfx;
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
				ArcTapCursorRenderer.enabled = (!arcTapCursorIsSfx)&&value;
				SfxArcTapCursorRenderer.enabled = (arcTapCursorIsSfx)&&value;
				enableArcTapCursor = value;
			}
		}
		public bool ArcTapCursorIsSfx
		{
			get
			{
				return arcTapCursorIsSfx;
			}
			set
			{
				ArcTapCursorRenderer.enabled = (!value)&&enableArcTapCursor;
				SfxArcTapCursorRenderer.enabled = (value)&&enableArcTapCursor;
				arcTapCursorIsSfx = value;
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
				SfxArcTapCursor.localPosition = value;
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
			ArcGameplayManager.Instance.OnChartLoad.AddListener(this.DeselectAllNotes);
		}
		private void OnDestroy()
		{
			AdeMarkingMenuManager.Instance.Providers.Remove(this);
			ArcGameplayManager.Instance.OnChartLoad.RemoveListener(this.DeselectAllNotes);
		}

		private void Awake()
		{
			Instance = this;
		}
		private void Update()
		{
			UpdateHorizontal();
			UpdateVertical();
			Selecting();
			DeleteListener();
		}

		private void UpdateHorizontal()
		{
			float xEdgePos = 8.5f * (1 + ArcTimingManager.Instance.BeatlineEnwidenRatio * 0.5f);
			HorizontalCollider.gameObject.transform.localScale=new Vector3(xEdgePos*2f,100f,1);
			Ray ray = GameplayCamera.ScreenPointToRay();
			IsHorizontalHit = HorizontalCollider.Raycast(ray, out horizontalHit, 120);
			if (Mode != CursorMode.Horizontal) return;
			EnableHorizontal = IsHorizontalHit;
			if (IsHorizontalHit)
			{
				float z = AdeGridManager.Instance.AttachBeatline(horizontalHit.point.z);
				HorizontalX.DrawLine(new Vector3(-xEdgePos, z), new Vector3(xEdgePos, z));
				HorizontalY.DrawLine(new Vector3(horizontalHit.point.x, 0), new Vector3(horizontalHit.point.x, -100));
				VerticalPanel.transform.localPosition = new Vector3(0, 0, z);
			}
		}
		private void UpdateVertical()
		{
			float xEdgePos = 8.5f * (1 + ArcTimingManager.Instance.BeatlineEnwidenRatio * 0.5f);
			float yEdgePos = 5.5f + ArcCameraManager.Instance.EnwidenRatio * 2.745f;
			VerticalCollider.gameObject.transform.localScale=new Vector3(xEdgePos*2f,yEdgePos,1);
			VerticalCollider.gameObject.transform.localPosition=new Vector3(0,yEdgePos/2f,0);
			Ray ray = GameplayCamera.ScreenPointToRay();
			IsVerticalHit = VerticalCollider.Raycast(ray, out verticalHit, 120);
			if (Mode != CursorMode.Vertical) return;
			EnableVertical = IsVerticalHit;
			if (IsVerticalHit)
			{
				VerticalX.DrawLine(new Vector3(-xEdgePos, AttachedVerticalPoint.y), new Vector3(xEdgePos, AttachedVerticalPoint.y));
				VerticalY.DrawLine(new Vector3(AttachedVerticalPoint.x, 0), new Vector3(AttachedVerticalPoint.x, yEdgePos));
			}
		}

		private float? rangeSelectPosition = null;
		public float? RangeSelectPosition{get=>rangeSelectPosition;}
		private void Selecting()
		{
			if (!AdeInputManager.Instance.Inputs.RangeSelection.IsPressed())
			{
				rangeSelectPosition = null;
			}

			if (Mouse.current.leftButton.wasPressedThisFrame)
			{
				//range selection shortcut
				if (AdeInputManager.Instance.Inputs.RangeSelection.IsPressed())
				{
					if (rangeSelectPosition == null)
					{
						rangeSelectPosition = AttachedTiming;
					}
					else
					{
						if (!AdeInputManager.Instance.Inputs.MultipleSelection.IsPressed())
						{
							DeselectAllNotes();
						}
						RangeSelectNote(rangeSelectPosition.Value, AttachedTiming);
						rangeSelectPosition = null;
					}
					return;
				}

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
					if (!AdeInputManager.Instance.Inputs.MultipleSelection.IsPressed())
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
					if (!AdeInputManager.Instance.Inputs.MultipleSelection.IsPressed() && IsHorizontalHit)
					{
						DeselectAllNotes();
					}
				}
			}
		}
		private void DeleteListener()
		{
			if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.Delete))
			{
				DeleteSelectedNotes();
			}
		}
		private void UpdateInfo()
		{

		}

		public void RangeSelectNote(float from, float to)
		{
			float start = Mathf.Min(from, to);
			float end = Mathf.Max(from, to);
			List<ArcNote> list = new List<ArcNote>();
			foreach (var tap in ArcTapNoteManager.Instance.Taps)
			{
				if (tap.Timing >= start && tap.Timing <= end)
				{
					SelectNote(tap);
				}
			}
			foreach (var hold in ArcHoldNoteManager.Instance.Holds)
			{
				if (hold.Timing >= start && hold.EndTiming <= end)
				{
					SelectNote(hold);
				}
			}
			foreach (var arc in ArcArcManager.Instance.Arcs)
			{
				if (arc.Timing >= start && arc.EndTiming <= end)
				{
					SelectNote(arc);
				}
				if (arc.Timing <= end && arc.EndTiming >= start)
				{
					foreach (var arctap in arc.ArcTaps)
					{
						if (arctap.Timing >= start && arctap.Timing <= end)
						{
							SelectNote(arctap);
						}
					}
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
