using UnityEngine;

namespace Arcade.Gameplay
{
	public class ArcAudioManager : MonoBehaviour
	{
		public static ArcAudioManager Instance { get; private set; }
		private void Awake()
		{
			Instance = this;
		}

		public AudioSource Source;
		public float Timing
		{
			get
			{
				return Source.time;
			}
			set
			{
				Source.time = value;
			}
		}
		public AudioClip Clip
		{
			get
			{
				return Source.clip;
			}
			set
			{
				Source.clip = value;
			}
		}
		public int AudioOffset { get; set; }

		public void Load(AudioClip clip, int offset)
		{
			AudioOffset = offset;
			Clip = clip;
			Clip.LoadAudioData();
		}
		public void Play()
		{
			Source.Play();
		}
		public void Pause()
		{
			Source.Pause();
		}
	}
}