using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace Arcade.Compose.Dialog
{
	[Serializable]
	public class SoundPreferences
	{
		public float Chart = 0.7f;
		public float Effect = 0f;
	}
	public class AdeSoundDialog : MonoBehaviour
	{
		public static AdeSoundDialog Instance { get; private set; }

		public GameObject View;
		public InputField ChartAudio, EffectAudio;
		public AudioSource ChartSource, EffectSource;
		private SoundPreferences preferences;
		public string PreferencesSavePath
		{
			get
			{
				return ArcadeComposeManager.ArcadePersistentFolder + "/Sound.json";
			}
		}

		private void Awake()
		{
			Instance = this;
		}

		private void Start()
		{
			Load();
		}
		private void OnDestroy()
		{
			Save();
		}
		private void Load()
		{
			try
			{
				if (File.Exists(PreferencesSavePath))
				{
					PlayerPrefs.SetString("AdeSoundDialog", File.ReadAllText(PreferencesSavePath));
					File.Delete(PreferencesSavePath);
				}
				preferences = JsonConvert.DeserializeObject<SoundPreferences>(PlayerPrefs.GetString("AdeSoundDialog", ""));
				if (preferences == null) preferences = new SoundPreferences();
			}
			catch (Exception)
			{
				preferences = new SoundPreferences();
			}
			finally
			{
				ChartAudio.text = preferences.Chart.ToString();
				EffectAudio.text = preferences.Effect.ToString();
				ChartSource.volume = preferences.Chart;
				EffectSource.volume = preferences.Effect;
			}
		}
		private void Save()
		{
			PlayerPrefs.SetString("AdeSoundDialog", JsonConvert.SerializeObject(preferences));
		}

		public void OnChartAudioChange()
		{
			try
			{
				float val = float.Parse(ChartAudio.text);
				val = Mathf.Clamp(val, 0, 1);
				preferences.Chart = val;
				ChartSource.volume = val;
				Save();
			}
			catch (Exception)
			{
				ChartAudio.text = preferences.Chart.ToString();
			}
		}
		public void OnEffectAudioChange()
		{
			try
			{
				float val = float.Parse(EffectAudio.text);
				val = Mathf.Clamp(val, 0, 1);
				preferences.Effect = val;
				EffectSource.volume = val;
				Save();
			}
			catch (Exception)
			{
				EffectAudio.text = preferences.Effect.ToString();
			}
		}

		public void SwitchStatus()
		{
			View.SetActive(!View.activeSelf);
		}
	}
}
