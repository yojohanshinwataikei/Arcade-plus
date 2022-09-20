using System;
using System.Collections;
using UnityEngine;
using OBSWebsocketDotNet;
using Arcade.Gameplay;
using System.Threading.Tasks;
using Arcade.Compose.Dialog;

namespace Arcade.Compose.Feature
{
	public class AdeObsManager : MonoBehaviour
	{
		private OBSWebsocket obs = new OBSWebsocket();
		public static AdeObsManager Instance { get; private set; }

		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			Connect();
		}
		private void OnDestroy()
		{
			if (obs.IsConnected) obs.Disconnect();
		}

		public void Record()
		{
			if (!obs.IsConnected)
			{
				AdeSingleDialog.Instance.Show("无法连接到OBS Studio\n请确认其已加载obs-websocket插件\n端口为4455，没有启用密码\n并启动了OBS Studio", "无法使用快速录制功能", "重试", Connect);
				return;
			}
			if (AdeProjectManager.Instance.CurrentProjectMetadata == null)
			{
				AdeSingleDialog.Instance.Show("未加载谱面", "错误");
				return;
			}
			if (recording != null) StopCoroutine(recording);
			recording = StartCoroutine(RecordCoroutine());
		}
		public void ForceClose()
		{
			if (recording != null)
			{
				StopCoroutine(recording);
				if (obs.IsConnected) obs.StopRecord();
				AdeSingleDialog.Instance.Show("录制中断", "提示");
				recording = null;
			}
		}
		private void Connect()
		{
			Task.Run(() => obs.Connect("ws://localhost:4455", null));
		}

		private Coroutine recording = null;
		private IEnumerator RecordCoroutine()
		{
			try
			{
				if (obs.IsConnected) obs.StartRecord();
			}
			catch (Exception)
			{
				AdeSingleDialog.Instance.Show("启动录制时发生异常\nOBS是否处于录制状态？", "错误");
				yield break;
			}
			yield return new WaitForSeconds(0.5f);
			ArcadeComposeManager.Instance.Play();
			ArcGameplayManager.Instance.PlayDelayed();
			yield return new WaitForSeconds(ArcAudioManager.Instance.Clip.length + 3);
			yield return new WaitForSeconds(0.5f);
			if (obs.IsConnected) obs.StopRecord();
			recording = null;
		}
	}
}
