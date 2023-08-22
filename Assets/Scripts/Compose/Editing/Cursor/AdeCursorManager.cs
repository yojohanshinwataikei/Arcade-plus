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

	public class AdeCursorManager : MonoBehaviour
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
			private set
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
			private set
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

		private void Awake()
		{
			Instance = this;
		}

		private void Update()
		{
			UpdateHorizontal();
			UpdateVertical();
		}

		private void UpdateHorizontal()
		{
			float xEdgePos = 8.5f * (1 + ArcTimingManager.Instance.BeatlineEnwidenRatio * 0.5f);
			HorizontalCollider.gameObject.transform.localScale=new Vector3(xEdgePos*2f,100f,1);
			Ray ray = GameplayCamera.MousePositionToRay();
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
			Ray ray = GameplayCamera.MousePositionToRay();
			IsVerticalHit = VerticalCollider.Raycast(ray, out verticalHit, 120);
			if (Mode != CursorMode.Vertical) return;
			EnableVertical = IsVerticalHit;
			if (IsVerticalHit)
			{
				VerticalX.DrawLine(new Vector3(-xEdgePos, AttachedVerticalPoint.y), new Vector3(xEdgePos, AttachedVerticalPoint.y));
				VerticalY.DrawLine(new Vector3(AttachedVerticalPoint.x, 0), new Vector3(AttachedVerticalPoint.x, yEdgePos));
			}
		}
	}
}
