using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Arcade.Compose.UI;

namespace Arcade.Compose.Dialog
{
	[Serializable]
	public class SoundPreferences
	{
		public float Chart = 0.7f;
		public float Effect = 0f;
	}
	public class AdeSoundDialogContent : AdeDialogContent<AdeSingleDialog>
	{
		public static AdeSoundDialogContent Instance { get; private set; }

		public AdeNumberInputWithSlider ChartAudioInput, EffectAudioInput;
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
			ChartAudioInput.onValueEdited += OnChartAudioChange;
			EffectAudioInput.onValueEdited += OnEffectAudioChange;
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
				ChartAudioInput.SetValueWithoutNotify(preferences.Chart);
				EffectAudioInput.SetValueWithoutNotify(preferences.Effect);
				ChartSource.volume = preferences.Chart;
				EffectSource.volume = preferences.Effect;
			}
		}
		private void Save()
		{
			PlayerPrefs.SetString("AdeSoundDialog", JsonConvert.SerializeObject(preferences));
		}

		public void OnChartAudioChange(float val)
		{
			val = Mathf.Clamp(val, 0, 1);
			preferences.Chart = val;
			ChartSource.volume = val;
			Save();
			ChartAudioInput.SetValueWithoutNotify(preferences.Chart);
		}
		public void OnEffectAudioChange(float val)
		{
			val = Mathf.Clamp(val, 0, 1);
			preferences.Effect = val;
			EffectSource.volume = val;
			Save();
			EffectAudioInput.SetValueWithoutNotify(preferences.Effect);
		}
	}
}
