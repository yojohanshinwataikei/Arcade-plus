using System;
using System.Collections;
using UnityEngine;
using OBSWebsocketDotNet;
using Arcade.Gameplay;
using System.Threading.Tasks;
using Arcade.Compose.Dialog;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.UI;

namespace Arcade.Compose.Feature
{
	[Serializable]
	public class OBSPreferences
	{
		public string ip = "127.0.0.1";
		public int port = 4455;
		public bool usePassword = false;
		public string password = "";
	}
	public class AdeObsManager : MonoBehaviour
	{
		private OBSWebsocket obs = new OBSWebsocket();
		public static AdeObsManager Instance { get; private set; }
		private OBSPreferences preference;

		public string PreferencesSavePath
		{
			get
			{
				return ArcadeComposeManager.ArcadePersistentFolder + "/OBS.json";
			}
		}
		public InputField ObsServerIpInput;
		public InputField ObsServerPortInput;
		public InputField ObsServerPasswordInput;
		public Toggle ObsServerUsePasswordToggle;
		public Text ConnectButtonLabel;
		public Button StartRecordButton;
		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			LoadPreferences();
			UpdatePreferencesInput();
			ObsServerIpInput.onEndEdit.AddListener(OnOBSServerIpChange);
			ObsServerPortInput.onEndEdit.AddListener(OnOBSServerPortChange);
			ObsServerPasswordInput.onEndEdit.AddListener(OnOBSServerPasswordChange);
			ObsServerUsePasswordToggle.onValueChanged.AddListener(OnOBSServerUsePasswordChange);
		}

        private void UpdatePreferencesInput()
        {
            ObsServerIpInput.SetTextWithoutNotify(preference.ip);
            ObsServerPortInput.SetTextWithoutNotify(preference.port.ToString());
            ObsServerPasswordInput.SetTextWithoutNotify(preference.password);
            ObsServerUsePasswordToggle.SetIsOnWithoutNotify(preference.usePassword);
			UpdateFieldsState();
        }

        private void LoadPreferences()
		{
			try
			{
				if (File.Exists(PreferencesSavePath))
				{
					PlayerPrefs.SetString("AdeOBSDialog", File.ReadAllText(PreferencesSavePath));
					File.Delete(PreferencesSavePath);
				}
				preference = JsonConvert.DeserializeObject<OBSPreferences>(PlayerPrefs.GetString("AdeSkinDialog", ""));
				if (preference == null) preference = new OBSPreferences();
			}
			catch (Exception Ex)
			{
				preference = new OBSPreferences();
				Debug.Log(Ex);
			}
		}

		public void SavePreferences()
		{
			PlayerPrefs.SetString("AdeOBSDialog", JsonConvert.SerializeObject(preference));
		}
		private void OnApplicationQuit()
		{
			SavePreferences();
		}

		public void OpenOBSWebsite()
		{
			Application.OpenURL("https://obsproject.com/");
		}

		public void OnOBSServerIpChange(string text)
		{
			preference.ip = ObsServerIpInput.text;
		}

		public void OnOBSServerPortChange(string text)
		{
			try
			{
				preference.port = int.Parse(ObsServerPortInput.text);
			}
			catch (Exception)
			{
				ObsServerPortInput.SetTextWithoutNotify(preference.port.ToString());
			}
		}

		public void OnOBSServerPasswordChange(string text)
		{
			preference.password = ObsServerPasswordInput.text;
		}

		public void OnOBSServerUsePasswordChange(bool value)
		{
			preference.usePassword = ObsServerUsePasswordToggle.isOn;
			UpdateFieldsState();
		}

		public void UpdateFieldsState()
		{
			ObsServerPasswordInput.interactable = preference.usePassword;
		}

		public void ForceClose()
		{
		}
	}
}
