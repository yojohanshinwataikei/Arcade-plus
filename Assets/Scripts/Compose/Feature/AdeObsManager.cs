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
using OBSWebsocketDotNet.Communication;
using System.Threading.Channels;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;
using System.Threading;
using Cysharp.Threading.Tasks;
using Arcade.Util.UniTaskHelper;
using System.Globalization;

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
		public Button ConnectionButton;
		public Text ConnectionButtonLabel;
		public AdeDualDialog OBSDialog;

		private bool hasOngoingConnectionTask = false;
		private bool obsIsRecording = false;
		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			obs.Connected += this.OnConnect;
			obs.Disconnected += this.OnDisconnect;
			obs.RecordStateChanged += this.OnRecordStateChanged;
			LoadPreferences();
			UpdatePreferencesInput();
			ObsServerIpInput.onEndEdit.AddListener(OnOBSServerIpChange);
			ObsServerPortInput.onEndEdit.AddListener(OnOBSServerPortChange);
			ObsServerPasswordInput.onEndEdit.AddListener(OnOBSServerPasswordChange);
			ObsServerUsePasswordToggle.onValueChanged.AddListener(OnOBSServerUsePasswordChange);
			ArcGameplayManager.Instance.OnChartLoad.AddListener(UpdateFieldsState);
		}

		private void UpdatePreferencesInput()
		{
			ObsServerIpInput.SetTextWithoutNotify(preference.ip);
			ObsServerPortInput.SetTextWithoutNotify(preference.port.ToString(CultureInfo.InvariantCulture));
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
				preference = JsonConvert.DeserializeObject<OBSPreferences>(PlayerPrefs.GetString("AdeOBSDialog", ""));
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
			Debug.Log("saving obs setting when exit...");
			SavePreferences();
			Debug.Log("saved obs setting when exit");
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
				preference.port = int.Parse(ObsServerPortInput.text, CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				ObsServerPortInput.SetTextWithoutNotify(preference.port.ToString(CultureInfo.InvariantCulture));
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
			if (obs.IsConnected || hasOngoingConnectionTask)
			{
				ObsServerIpInput.interactable = false;
				ObsServerPortInput.interactable = false;
				ObsServerPasswordInput.interactable = false;
				ObsServerUsePasswordToggle.interactable = false;
			}
			else
			{
				ObsServerIpInput.interactable = true;
				ObsServerPortInput.interactable = true;
				ObsServerPasswordInput.interactable = preference.usePassword;
				ObsServerUsePasswordToggle.interactable = true;
			}
			ConnectionButton.interactable = !hasOngoingConnectionTask;
			if (hasOngoingConnectionTask)
			{
				if (obs.IsConnected)
				{
					ConnectionButtonLabel.text = "正在断开 OBS……";
				}
				else
				{
					ConnectionButtonLabel.text = "正在连接 OBS……";
				}
			}
			else
			{
				if (obs.IsConnected)
				{
					ConnectionButtonLabel.text = "断开 OBS";
				}
				else
				{
					ConnectionButtonLabel.text = "连接到 OBS";
				}
			}
			OBSDialog.RightButton.interactable = obs.IsConnected && ArcGameplayManager.Instance.IsLoaded && !obsIsRecording && RecordingCancellation == null;
		}

		// OBS event handlers are not executed in main unity thread,
		// so move them to main unity thread by a channel
		System.Threading.Channels.Channel<Action> eventActionChannel = System.Threading.Channels.Channel.CreateUnbounded<Action>();

		public void Update()
		{
			while (true)
			{
				Action action;
				if (eventActionChannel.Reader.TryRead(out action))
				{
					action();
				}
				else
				{
					break;
				}
			}
		}

		public void OnConnect(object sender, EventArgs e)
		{
			eventActionChannel.Writer.TryWrite(() =>
			{
				hasOngoingConnectionTask = false;
				obsIsRecording = obs.GetRecordStatus().IsRecording;
				UpdateFieldsState();
				AdeToast.Instance.Show($"OBS 已连接");
			});
		}

		public void OnDisconnect(object sender, ObsDisconnectionInfo e)
		{
			eventActionChannel.Writer.TryWrite(() =>
			{
				hasOngoingConnectionTask = false;
				obsIsRecording = false;
				StopRecord();
				UpdateFieldsState();
				AdeToast.Instance.Show($"OBS 连接已断开");
			});
		}
		public void OnRecordStateChanged(object sender, RecordStateChangedEventArgs e)
		{
			eventActionChannel.Writer.TryWrite(() =>
			{
				obsIsRecording = e.OutputState.State == OutputState.OBS_WEBSOCKET_OUTPUT_STARTED || e.OutputState.State == OutputState.OBS_WEBSOCKET_OUTPUT_STARTING;
				Debug.Log($"[====]{e.OutputState.State}");
				if (!obsIsRecording)
				{
					StopRecord();
				}
				UpdateFieldsState();
			});
		}

		public void OnConnectionButton()
		{
			if (hasOngoingConnectionTask)
			{
				return;
			}
			hasOngoingConnectionTask = true;
			if (obs.IsConnected)
			{
				obs.Disconnect();
			}
			else
			{
				UriBuilder uriBuilder = new UriBuilder();
				uriBuilder.Scheme = "ws";
				uriBuilder.Host = preference.ip;
				uriBuilder.Port = preference.port;
				obs.ConnectAsync(uriBuilder.ToString(), preference.usePassword ? preference.password : null);
			}
			UpdateFieldsState();
		}

		public CancellationTokenSource RecordingCancellation;

		private async UniTask Record(CancellationToken cancellationToken)
		{
			try
			{
				obs.StartRecord();
				await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);
				ArcadeComposeManager.Instance.Play();
				ArcGameplayManager.Instance.PlayDelayed();
				await UniTask.Delay(TimeSpan.FromSeconds(ArcAudioManager.Instance.Clip.length + 3f), cancellationToken: cancellationToken);
				await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);
			}
			finally
			{
				if (obs.IsConnected) obs.StopRecord();
				if (RecordingCancellation?.Token == cancellationToken)
				{
					RecordingCancellation = null;
				}
				ArcadeComposeManager.Instance.Pause();
				ArcGameplayManager.Instance.Pause();
				AdeToast.Instance.Show($"OBS 快速录制已结束");
				UpdateFieldsState();
			}
		}

		public void StartRecord()
		{
			OBSDialog.Close();
			UpdateFieldsState();
			RecordingCancellation = new CancellationTokenSource();
			Record(RecordingCancellation.Token).WithExceptionLogger().Forget();
		}

		private void StopRecord()
		{
			Debug.Log("[====]Stop");
			RecordingCancellation?.Cancel();
			RecordingCancellation = null;
		}

		public void ForceStopRecording()
		{
			if (RecordingCancellation != null)
			{
				AdeBasicSingleDialogContent.Instance.Show("OBS 快速录制被手动中断", "提示");
				StopRecord();
			}
		}
	}
}
