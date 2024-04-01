using UnityEngine;
using Arcade.Gameplay.Chart;
using UnityEngine.Events;
using Arcade.Gameplay.Events;

namespace Arcade.Gameplay
{
	namespace Events
	{
		public class OnMusicFinishedEvent : UnityEvent
		{

		}
	}

	public class ArcGameplayManager : MonoBehaviour
	{
		public uint SelectionLayerMask;

		public static ArcGameplayManager Instance { get; private set; }

		public ShaderVariantCollection Shaders;
		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			Shaders.WarmUp();
		}

		public bool Auto { get; set; }
		public bool IsPlaying { get; set; }
		private float timing;
		public int Timing
		{
			get
			{
				return Mathf.RoundToInt(timing * 1000);
			}
			set
			{
				timing = value / 1000f;
				ArcAudioManager.Instance.Timing = Mathf.Max(0, timing);
			}
		}
		public int Length { get; private set; }
		public bool IsLoaded
		{
			get
			{
				return Chart != null;
			}
		}

		public UnityEvent OnChartLoad = new UnityEvent();
		public OnMusicFinishedEvent OnMusicFinished = new OnMusicFinishedEvent();
		public ArcChart Chart { get; set; }

		public float TimingPointDensityFactor
		{
			get
			{
				return Chart.TimingPointDensityFactor;
			}
		}

		public bool EnablePlaybackSync = false;

		private double lastDspTime = 0;
		private double deltaDspTime = 0;

		private void Update()
		{
			deltaDspTime = AudioSettings.dspTime - lastDspTime;
			lastDspTime = AudioSettings.dspTime;
			if (IsPlaying)
			{
				float playBackSpeed = ArcAudioManager.Instance.PlayBackSpeed;
				timing += Time.deltaTime * playBackSpeed;
				if(EnablePlaybackSync){
					float t = ArcAudioManager.Instance.Timing;
					if (deltaDspTime > 0f && (timing >= 0 || ArcAudioManager.Instance.Timing > 0))
					{
						float delta = timing - t;
						int bufferLength;
						int numBuffers;
						AudioSettings.GetDSPBufferSize(out bufferLength, out numBuffers);
						float maxDelta = 2 * (float)bufferLength / (float)AudioSettings.outputSampleRate;
						if (Mathf.Abs(delta) > maxDelta)
						{
							timing = t;
						}
					}
				}
			}
			if (Timing > Length)
			{
				OnMusicFinished.Invoke();
				Stop();
			}
		}
		public bool Load(ArcChart chart, AudioClip audio)
		{
			if (audio == null || chart == null) return false;

			Clean();
			Chart = chart;
			Length = (int)(audio.length * 1000);

			ArcCameraManager.Instance.ResetCamera();
			ArcAudioManager.Instance.Load(audio, chart.AudioOffset);
			ArcTimingManager.Instance.Load(chart.Timings, chart.TimingGroups);
			ArcTapNoteManager.Instance.Load(chart.Taps);
			ArcHoldNoteManager.Instance.Load(chart.Holds);
			ArcArcManager.Instance.Load(chart.Arcs);
			ArcCameraManager.Instance.Load(chart.Cameras);
			ArcSceneControlManager.Instance.Load(chart.SceneControl);

			OnChartLoad.Invoke();
			return true;
		}
		public void Clean()
		{
			Timing = 0;
			ArcTimingManager.Instance.Clean();
			ArcTapNoteManager.Instance.Clean();
			ArcHoldNoteManager.Instance.Clean();
			ArcArcManager.Instance.Clean();
			ArcCameraManager.Instance.Clean();
			ArcSceneControlManager.Instance.Clean();
			Chart = null;
			Length = 0;
		}

		public void ResetJudge()
		{
			if (Chart != null)
			{
				foreach (var t in Chart.Arcs) { foreach (var a in t.ArcTaps) { a.Judged = false; } t.Judged = false; t.Judging = false; t.AudioPlayed = false; };
				foreach (var t in Chart.Holds) { t.Judged = false; t.Judging = false; t.AudioPlayed = false; };
				foreach (var t in Chart.Taps) { t.Judged = false; };
			}
		}
		public void PlayDelayed()
		{
			timing = -3f;
			ResetJudge();
			ArcAudioManager.Instance.Source.Stop();
			ArcAudioManager.Instance.Timing = 0;
			ArcAudioManager.Instance.Source.PlayDelayed(3);
			IsPlaying = true;
		}

		public void Play()
		{
			if (timing < 0)
			{
				ArcAudioManager.Instance.Source.Stop();
				ArcAudioManager.Instance.Timing = 0;
				ArcAudioManager.Instance.Source.PlayDelayed(-timing);
			}
			else
			{
				ArcAudioManager.Instance.Timing = timing;
				ArcAudioManager.Instance.Play();
			}
			IsPlaying = true;
		}
		public void Pause()
		{
			ArcAudioManager.Instance.Pause();
			IsPlaying = false;
			ResetJudge();
		}
		public void Stop()
		{
			timing = 0;
			ArcAudioManager.Instance.Pause();
			ArcAudioManager.Instance.Timing = 0;
			IsPlaying = false;
		}

		public ArcNote FindNoteByRaycastHit(RaycastHit h)
		{
			foreach (var tap in Chart.Taps) if (tap.Instance.Equals(h.transform.gameObject)) return tap;
			foreach (var hold in Chart.Holds) if (hold.Instance.Equals(h.transform.gameObject)) return hold;
			foreach (var arc in Chart.Arcs)
			{
				if (arc.IsHitMyself(h)) return arc;
				foreach (var arctap in arc.ArcTaps)
				{
					if (arctap.IsMyself(h.transform.gameObject)) return arctap;
				}
			}
			return null;
		}
	}
}

