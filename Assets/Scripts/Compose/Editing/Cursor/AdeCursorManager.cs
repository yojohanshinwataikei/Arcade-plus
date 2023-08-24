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
using UnityEngine.Serialization;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Arcade.Compose
{
	public class AdeCursorManager : MonoBehaviour
	{
		public static AdeCursorManager Instance { get; private set; }

		public Camera GameplayCamera;
		public MeshCollider TrackCollider;
		public MeshCollider WallCollider;
		public GameObject WallPanel;
		public LineRenderer TrackX;
		public LineRenderer TrackY;
		public LineRenderer WallX;
		public LineRenderer WallY;
		public MeshRenderer WallRenderer;

		public Transform ArcTapCursor;
		public MeshRenderer ArcTapCursorRenderer;
		public Transform SfxArcTapCursor;
		public MeshRenderer SfxArcTapCursorRenderer;

		private bool trackEnabled, wallEnabled, wallPanelEnabled;
		private bool arcTapCursorEnable, arcTapCursorIsSfx;
		private RaycastHit trackHit, wallHit;

		public bool TrackEnabled
		{
			get
			{
				return trackEnabled;
			}
			private set
			{
				if (trackEnabled != value)
				{
					TrackX.enabled = value;
					TrackY.enabled = value;
					TrackX.positionCount = 0;
					TrackY.positionCount = 0;
					trackEnabled = value;
				}
			}
		}
		public bool WallEnabled
		{
			get
			{
				return wallEnabled;
			}
			private set
			{
				if (wallEnabled != value)
				{
					WallX.enabled = value;
					WallY.enabled = value;
					WallX.positionCount = 0;
					WallY.positionCount = 0;
					WallPanelEnabled = value;
					wallEnabled = value;
				}
			}
		}
		public bool WallPanelEnabled
		{
			get
			{
				return wallPanelEnabled;
			}
			private set
			{
				if (wallPanelEnabled != value)
				{
					WallRenderer.enabled = value;
					wallPanelEnabled = value;
				}
			}
		}
		public bool ArcTapCursorEnabled
		{
			get
			{
				return arcTapCursorEnable;
			}
			set
			{
				ArcTapCursorRenderer.enabled = (!arcTapCursorIsSfx) && value;
				SfxArcTapCursorRenderer.enabled = (arcTapCursorIsSfx) && value;
				arcTapCursorEnable = value;
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
				ArcTapCursorRenderer.enabled = (!value) && arcTapCursorEnable;
				SfxArcTapCursorRenderer.enabled = (value) && arcTapCursorEnable;
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

		public bool VisibleWhenIdle = false;

		private enum SelectTaskType
		{
			Timing,
			Track,
			Coordinate,
		}
		private SelectTaskType? currentSelectTaskType = null;
		private bool shouldRenderTrack
		{
			get => VisibleWhenIdle || currentSelectTaskType == SelectTaskType.Timing || currentSelectTaskType == SelectTaskType.Track;
		}
		private bool shouldRenderWall
		{
			get => currentSelectTaskType == SelectTaskType.Coordinate;
		}
		private bool shouldRenderWallPanel
		{
			get => ArcTapCursorEnabled;
		}

		public bool IsTrackHit { get; private set; }
		public bool IsWallHit { get; private set; }
		public Vector3 TrackPoint
		{
			get
			{
				return trackHit.point;
			}
		}
		public Vector3 WallPoint
		{
			get
			{
				return wallHit.point;
			}
		}
		public Vector3 AttachedTrackPoint
		{
			get
			{
				float z = AdeGridManager.Instance.AttachBeatline(trackHit.point.z);
				return new Vector3(trackHit.point.x, trackHit.point.y, z);
			}
		}
		public Vector3 AttachedWallPoint
		{
			get
			{
				return new Vector3(ArcAlgorithm.ArcXToWorld(AdeGridManager.Instance.AttachVerticalX(ArcAlgorithm.WorldXToArc(WallPoint.x))),
				   ArcAlgorithm.ArcYToWorld(AdeGridManager.Instance.AttachVerticalY(ArcAlgorithm.WorldYToArc(WallPoint.y))));
			}
		}

		public int AttachedTiming
		{
			get
			{
				if (!ArcGameplayManager.Instance.IsLoaded) return 0;
				Vector3 pos = AttachedTrackPoint;
				var timingGroup = AdeTimingEditor.Instance.currentTimingGroup;
				return ArcTimingManager.Instance.CalculateTimingByPosition(-pos.z * 1000, timingGroup) - ArcAudioManager.Instance.AudioOffset;
			}
		}

		private void Awake()
		{
			Instance = this;
		}

		private void Update()
		{
			UpdateTrackCursor();
			UpdateWallCursor();
		}

		private void UpdateTrackCursor()
		{
			float xEdgePos = 8.5f * (1 + ArcTimingManager.Instance.BeatlineEnwidenRatio * 0.5f);
			TrackCollider.gameObject.transform.localScale = new Vector3(xEdgePos * 2f, 100f, 1);
			Ray ray = GameplayCamera.MousePositionToRay();
			IsTrackHit = TrackCollider.Raycast(ray, out trackHit, 120);
			TrackEnabled = IsTrackHit && shouldRenderTrack;
			if (TrackEnabled)
			{
				float z = AdeGridManager.Instance.AttachBeatline(trackHit.point.z);
				TrackX.DrawLine(new Vector3(-xEdgePos, z), new Vector3(xEdgePos, z));
				TrackY.DrawLine(new Vector3(trackHit.point.x, 0), new Vector3(trackHit.point.x, -100));
				WallPanel.transform.localPosition = new Vector3(0, 0, z);
			}
		}
		private void UpdateWallCursor()
		{
			float xEdgePos = 8.5f * (1 + ArcTimingManager.Instance.BeatlineEnwidenRatio * 0.5f);
			float yEdgePos = 5.5f + ArcCameraManager.Instance.EnwidenRatio * 2.745f;
			WallCollider.gameObject.transform.localScale = new Vector3(xEdgePos * 2f, yEdgePos, 1);
			WallCollider.gameObject.transform.localPosition = new Vector3(0, yEdgePos / 2f, 0);
			Ray ray = GameplayCamera.MousePositionToRay();
			IsWallHit = WallCollider.Raycast(ray, out wallHit, 120);
			WallEnabled = IsWallHit && shouldRenderWall;
			if (WallEnabled)
			{
				WallX.DrawLine(new Vector3(-xEdgePos, AttachedWallPoint.y), new Vector3(xEdgePos, AttachedWallPoint.y));
				WallY.DrawLine(new Vector3(AttachedWallPoint.x, 0), new Vector3(AttachedWallPoint.x, yEdgePos));
			}
			WallPanelEnabled=shouldRenderWallPanel;
		}

		public async UniTask<int> SelectTiming(IProgress<int> progress, CancellationToken cancellationToken)
		{
			if (currentSelectTaskType != null)
			{
				throw new Exception("Cannot select two thing at the same time");
			}

			currentSelectTaskType = SelectTaskType.Timing;
			try
			{
				while (true)
				{
					await UniTask.NextFrame(cancellationToken);
					if (AdeGameplayContentInputHandler.InputActive && IsTrackHit)
					{
						progress.Report(AttachedTiming);
						if (Mouse.current.leftButton.wasPressedThisFrame)
						{
							return AttachedTiming;
						}
					}
				}
			}
			finally
			{
				currentSelectTaskType = null;
			}
		}
	}
}
