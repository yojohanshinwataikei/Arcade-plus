using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Arcade.Gameplay.Chart;

namespace Arcade.Gameplay
{
	public class ArcCameraManager : MonoBehaviour
	{
		public Camera GameplayCamera;
		public Transform SkyInputLabel;
		public static ArcCameraManager Instance { get; private set; }

		[HideInInspector]
		public float CurrentTilt = 0;

		[HideInInspector]
		public float EnwidenPrecent = 0;

		[HideInInspector]
		public List<ArcCamera> Cameras = new List<ArcCamera>();

		[HideInInspector]
		public bool IsReset = true;

		public Vector3 ResetPosition
		{
			get
			{
				return new Vector3(0, 9f + 4.5f * EnwidenPrecent, (Is16By9 ? 9f : 8f) + 4.5f * EnwidenPrecent);
			}
		}
		public Vector3 ResetRotation
		{
			get
			{
				return new Vector3(Is16By9 ? 26.565f : 27.378f, 180, 0);
			}
		}

		public bool Is16By9
		{
			get
			{
				return (1f * GameplayCamera.pixelWidth / GameplayCamera.pixelHeight) - (16f / 9f) > -1f / 9f;
			}
		}

		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			ResetCamera();
		}

		public void Clean()
		{
			Cameras.Clear();
		}
		public void Load(List<ArcCamera> cameras)
		{
			// Note: We replaced the inplace sort by sort to another list and reassign
			// just because we do not have stable inplace sort now in dot net
			Cameras = cameras.OrderBy(camera => camera.Timing).ToList();
			ArcGameplayManager.Instance.Chart.Cameras = Cameras;
		}

		public void ResetCamera()
		{
			GameplayCamera.fieldOfView = Is16By9 ? 50 : 65;
			GameplayCamera.nearClipPlane = 1;
			GameplayCamera.farClipPlane = 10000;
			SkyInputLabel.localPosition = new Vector3(Is16By9 ? -7.1f : -6.5f, 0.1f, 0);
			GameplayCamera.transform.position = new Vector3(0, 9, Is16By9 ? 9 : 8);
			GameplayCamera.transform.LookAt(new Vector3(0, -5.5f, -20), new Vector3(0, 1, 0));
			IsReset = true;
		}

		private void Update()
		{
			if (UpdateEditorCamera()) return;
			UpdateCameraPosition();
			UpdateCameraTilt();
		}

		public bool EditorCamera { get; set; }
		public Vector3 EditorCameraPosition { get; set; }
		public Vector3 EditorCameraRotation { get; set; }
		private bool UpdateEditorCamera()
		{
			if (EditorCamera)
			{
				GameplayCamera.transform.localPosition = EditorCameraPosition;
				GameplayCamera.transform.localRotation = Quaternion.Euler(EditorCameraRotation);
				return true;
			}
			return false;
		}

		private void UpdateCameraPosition()
		{
			int currentTiming = ArcGameplayManager.Instance.Timing;
			int offset = ArcAudioManager.Instance.AudioOffset;
			currentTiming -= offset;
			for (int i = 0; i < Cameras.Count; ++i)
			{
				ArcCamera c = Cameras[i];
				if (c.Timing > currentTiming) break;
				c.Update(currentTiming);
				if (c.CameraType == Chart.CameraEaseType.Reset)
				{
					for (int r = 0; r < i; ++r)
					{
						ArcCamera cr = Cameras[r];
						cr.Update(c.Timing);
					}
				}
			}
			Vector3 position = ResetPosition;
			Vector3 rotation = ResetRotation;
			IsReset = true;
			foreach (var c in Cameras)
			{
				if (c.Timing > currentTiming) break;
				IsReset = c.CameraType == Chart.CameraEaseType.Reset;
				if (IsReset)
				{
					position = ResetPosition;
					rotation = ResetRotation;
				}
				position += new Vector3(-c.Move.x, c.Move.y, c.Move.z) * c.Percent / 100;
				rotation += new Vector3(-c.Rotate.y, -c.Rotate.x, c.Rotate.z) * c.Percent;
			}
			GameplayCamera.transform.localPosition = position;
			GameplayCamera.transform.localRotation = Quaternion.Euler(0, 0, rotation.z) * Quaternion.Euler(rotation.x, rotation.y, 0);
		}
		private void UpdateCameraTilt()
		{
			if (!IsReset)
			{
				CurrentTilt = 0;
				return;
			}
			float currentArcPos = ArcGameplayManager.Instance.Auto && ArcGameplayManager.Instance.IsPlaying ? -ArcArcManager.Instance.ArcJudgePos : 0;
			float pos = Mathf.Clamp(currentArcPos / 4.25f, -1, 1) * 0.05f;
			float delta = pos - CurrentTilt;
			if (Mathf.Abs(delta) >= 0.001f)
			{
				float speed = 6f;
				CurrentTilt = CurrentTilt + speed * delta * Time.deltaTime;
			}
			else
			{
				CurrentTilt = pos;
			}
			GameplayCamera.transform.LookAt(new Vector3(0, -5.5f + 4.5f * EnwidenPrecent, -20f + 4.5f * EnwidenPrecent), new Vector3(CurrentTilt, 1 - CurrentTilt, 0));
		}
	}
}

