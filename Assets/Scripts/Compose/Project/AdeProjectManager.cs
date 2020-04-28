using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Arcade.Compose.Dialog;
using Arcade.Compose.Feature;
using Arcade.Compose.UI;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using Newtonsoft.Json;
using Arcade.Util.Mp3Converter;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Arcade.Compose.Command;

namespace Arcade.Compose
{
	[Serializable]
	public class AdeChartDifficulty
	{
		public string Rating;
		public string ChartPath;
		public string Designer;
	}
	[Serializable]
	public class ArcadeProject
	{
		public string Title;
		public string Artist;
		public float BaseBpm;
		public string AudioPath;
		public AdeChartDifficulty[] Difficulties = new AdeChartDifficulty[3];

		public int LastWorkingDifficulty = 2;
		public int LastWorkingTiming;

		[JsonIgnore]
		public AudioClip AudioClip;
		[JsonIgnore]
		public Texture2D Cover;
		[JsonIgnore]
		public Sprite CoverSprite;
	}

	public class AdeProjectManager : MonoBehaviour
	{
		public static AdeProjectManager Instance { get; private set; }

		public string CurrentProjectFolder { get; set; }
		public ArcadeProject CurrentProject { get; set; }
		public int CurrentDifficulty { get; set; } = 2;

		public Sprite DefaultCover;
		public Image CoverImage;
		public Image[] DifficultyImages;
		public InputField Name, Composer, Diff, BaseBpm, AudioOffset;
		public Text OpenLabel;
		public Text SaveMode;

		public Color EnableColor, DisableColor;
		public Image FileWatchEnableImage;

		private FileSystemWatcher watcher = new FileSystemWatcher();
		private bool shouldReload = false;

		public string ProjectFilePath
		{
			get
			{
				return $"{CurrentProjectFolder}/Arcade/Project.arcade";
			}
		}

		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			watcher.NotifyFilter = NotifyFilters.LastWrite;
			watcher.Changed += OnWatcherChanged;

			watcher.EnableRaisingEvents = true;
			StartCoroutine(AutosaveCoroutine());
		}
		private void Update()
		{
			if (shouldReload)
			{
				ReloadChart(CurrentDifficulty);
				ArcGameplayManager.Instance.Timing = ArcGameplayManager.Instance.Chart.LastEventTiming - 500;
				shouldReload = false;
			}
			if (Input.GetKey(KeyCode.LeftControl))
			{
				if (Input.GetKeyDown(KeyCode.S))
				{
					SaveProject();
				}
			}
		}

		private void InitializeProject(string folder)
		{
			ArcadeProject p = new ArcadeProject();
			File.WriteAllText(ProjectFilePath, JsonConvert.SerializeObject(p));
		}
		private void CreateDirectories(string folder)
		{
			string[] directories = new string[] { $"{folder}/Arcade", $"{folder}/Arcade/Autosave", $"{folder}/Arcade/Converting", $"{folder}/Arcade/Backup" };
			foreach (var s in directories) if (!Directory.Exists(s)) Directory.CreateDirectory(s);
		}

		public void CleanProject()
		{
			if (CurrentProject == null) return;
			if (CurrentProject.AudioClip != null)
			{
				Destroy(CurrentProject.AudioClip);
				CurrentProject.AudioClip = null;
			}
			if (CurrentProject.Cover != null)
			{
				Destroy(CurrentProject.Cover);
				CurrentProject.Cover = null;
			}
			if (CurrentProject.CoverSprite != null)
			{
				Destroy(CurrentProject.CoverSprite);
				CurrentProject.CoverSprite = null;
			}
			foreach (Image i in DifficultyImages) i.color = new Color(1f, 1f, 1f, 0.6f);
			CoverImage.sprite = DefaultCover;
			Name.text = "";
			Composer.text = "";
			Diff.text = "";
			Name.interactable = false;
			Composer.interactable = false;
			Diff.interactable = false;
			AdeTimingSlider.Instance.Enable = false;
			OpenLabel.color = new Color(1, 1, 1, 1);
			BaseBpm.interactable = false;
			AudioOffset.interactable = false;
			watcher.EnableRaisingEvents = false;
			FileWatchEnableImage.color = DisableColor;


			ArcGameplayManager.Instance.Clean();
		}
		public void OpenProject()
		{
			try
			{
				string folder = Util.Windows.Dialog.OpenFolderDialog(
					"选择您的 Arcaea 自制谱文件夹 (包含 0/1/2.aff, base.mp3/ogg/wav, base.jpg)");
				if (folder == null) return;
				CleanProject();
				CreateDirectories(folder);
				CurrentProjectFolder = folder;
				if (!File.Exists(ProjectFilePath)) InitializeProject(folder);
				try
				{
					CurrentProject = JsonConvert.DeserializeObject<ArcadeProject>(File.ReadAllText(ProjectFilePath));
				}
				catch (Exception Ex)
				{
					AdeSingleDialog.Instance.Show(Ex.Message, "读取错误");
					CurrentProject = new ArcadeProject();
				}

				StartCoroutine(LoadingCoroutine());
			}
			catch (Exception Ex)
			{
				AdeSingleDialog.Instance.Show(Ex.Message, "读取错误");
				CurrentProject = null;
				CurrentProjectFolder = null;
			}
		}
		public void SaveProject()
		{
			if (CurrentProject == null || CurrentProjectFolder == null) return;
			if (ArcGameplayManager.Instance.Chart == null) return;
			CurrentProject.LastWorkingDifficulty = CurrentDifficulty;
			CurrentProject.LastWorkingTiming = ArcGameplayManager.Instance.Timing;
			File.WriteAllText(ProjectFilePath, JsonConvert.SerializeObject(CurrentProject));
			string path = CurrentProjectFolder + $"/{CurrentDifficulty}.aff";
			string backupPath = CurrentProjectFolder + $"/Arcade/Backup/{CurrentDifficulty}_{DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")}.aff";
			File.Copy(path, backupPath);
			FileStream fs = new FileStream(path, FileMode.Create);
			try
			{
				ArcGameplayManager.Instance.Chart.Serialize(fs, ArcadeComposeManager.Instance.ArcadePreference.ChartSortMode);
			}
			catch (Exception Ex)
			{
				AdeSingleDialog.Instance.Show(Ex.Message + "\n" + Ex.ToString(), "保存错误");
			}
			AdeToast.Instance.Show("谱面已保存至\n" + path + "\n原文件已备份至\n" + backupPath);
			fs.Close();
		}

		private IEnumerator AutosaveCoroutine()
		{
			while (true)
			{
				yield return new WaitForSeconds(30f);
				if (CurrentProject == null || CurrentProjectFolder == null) continue;
				if (ArcGameplayManager.Instance.Chart == null) continue;
				string backupPath = CurrentProjectFolder + $"/Arcade/Autosave/{CurrentDifficulty}_{DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")}.aff";
				FileStream fs = new FileStream(backupPath, FileMode.Create);
				try
				{
					ArcGameplayManager.Instance.Chart.Serialize(fs, ArcadeComposeManager.Instance.ArcadePreference.ChartSortMode);
				}
				catch (Exception Ex)
				{
					AdeSingleDialog.Instance.Show(Ex.Message + "\n" + Ex.ToString(), "自动保存错误");
				}
				fs.Close();
			}
		}

		private IEnumerator LoadChartCoroutine(int index, bool shutter)
		{
			if (CurrentProject == null || CurrentProjectFolder == null || CurrentProject.AudioClip == null)
			{
				yield break;
			}

			if (!File.Exists($"{CurrentProjectFolder}/{index}.aff"))
			{
				File.WriteAllText($"{CurrentProjectFolder}/{index}.aff", "AudioOffset:0\n-\ntiming(0,100.00,4.00);");
			}

			if (shutter) yield return AdeShutterManager.Instance.CloseCoroutine();
			ArcadeComposeManager.Instance.Pause();
			AdeObsManager.Instance.ForceClose();
			CommandManager.Instance.Clear();

			Aff.ArcaeaAffReader reader = null;
			try
			{
				reader = new Aff.ArcaeaAffReader($"{CurrentProjectFolder}/{index}.aff");
			}
			catch (Aff.ArcaeaAffFormatException Ex)
			{
				AdeSingleDialog.Instance.Show(Ex.Message, "谱面格式错误");
				reader = null;
			}
			catch (Exception Ex)
			{
				AdeSingleDialog.Instance.Show(Ex.Message, "谱面读取错误");
				reader = null;
			}
			if (reader == null)
			{
				if (shutter) yield return AdeShutterManager.Instance.OpenCoroutine();
				yield break;
			}
			ArcGameplayManager.Instance.Load(new Gameplay.Chart.ArcChart(reader), CurrentProject.AudioClip);
			CurrentDifficulty = index;

			Diff.text = CurrentProject.Difficulties[CurrentDifficulty] == null ? "" : CurrentProject.Difficulties[CurrentDifficulty].Rating;
			foreach (Image i in DifficultyImages) i.color = new Color(1f, 1f, 1f, 0.6f);
			DifficultyImages[index].color = new Color(1, 1, 1, 1);

			AudioOffset.interactable = true;
			AudioOffset.text = ArcAudioManager.Instance.AudioOffset.ToString();

			watcher.Path = CurrentProjectFolder;
			watcher.Filter = $"{index}.aff";

			yield return null;

			ArcArcManager.Instance.Rebuild();

			ArcGameplayManager.Instance.Timing = CurrentProject.LastWorkingTiming;

			if (shutter) yield return AdeShutterManager.Instance.OpenCoroutine();
		}
		private IEnumerator LoadCoverCoroutine()
		{
			if (!File.Exists($"{CurrentProjectFolder}/base.jpg"))
			{
				CoverImage.sprite = DefaultCover;
				yield break;
			}
			string path = $"{CurrentProjectFolder}/base.jpg";
			using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(Uri.EscapeUriString("file:///" + path.Replace("\\", "/"))))
			{
				yield return req.SendWebRequest();
				if (!string.IsNullOrWhiteSpace(req.error))
				{
					CoverImage.sprite = DefaultCover;
					yield break;
				}
				CurrentProject.Cover = DownloadHandlerTexture.GetContent(req);
				CurrentProject.CoverSprite = Sprite.Create(CurrentProject.Cover, new Rect(0, 0, CurrentProject.Cover.width, CurrentProject.Cover.height), new Vector2(0.5f, 0.5f));
				CoverImage.sprite = CurrentProject.CoverSprite;
			}
		}
		private IEnumerator LoadMusicCoroutine()
		{
			string[] searchPaths = new string[] { $"{CurrentProjectFolder}/Arcade/Converting/base.wav", $"{CurrentProjectFolder}/base.wav", $"{CurrentProjectFolder}/base.ogg" };
			string path = null;
			foreach (var s in searchPaths)
			{
				if (File.Exists(s)) path = s;
			}
			if (path == null)
			{
				if (File.Exists($"{CurrentProjectFolder}/base.mp3"))
				{
					Task converting = Task.Run(() => Mp3Converter.Mp3ToWav($"{CurrentProjectFolder}/base.mp3", $"{CurrentProjectFolder}/Arcade/Converting/base.wav"));
					while (!converting.IsCompleted) yield return null;
					if (converting.Status == TaskStatus.RanToCompletion)
					{
						path = $"{CurrentProjectFolder}/Arcade/Converting/base.wav";
					}
				}
			}
			if (path == null)
			{
				AdeSingleDialog.Instance.Show(
				   "没有找到音乐或音乐格式不正确",
					"谱面格式错误");
				yield break;
			}
			using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(Uri.EscapeUriString("file:///" + path.Replace("\\", "/")), path.EndsWith("wav") ? AudioType.WAV : AudioType.OGGVORBIS))
			{
				yield return req.SendWebRequest();
				if (!string.IsNullOrWhiteSpace(req.error))
				{
					yield break;
				}
				CurrentProject.AudioClip = DownloadHandlerAudioClip.GetContent(req);
				AdeTimingSlider.Instance.Enable = true;
				AdeTimingSlider.Instance.Length = (int)(CurrentProject.AudioClip.length * 1000);
			}
		}
		private IEnumerator LoadingCoroutine()
		{
			yield return AdeShutterManager.Instance.CloseCoroutine();

			Name.text = CurrentProject.Title;
			Composer.text = CurrentProject.Artist;
			Diff.text = "";
			Name.interactable = true;
			Composer.interactable = true;
			Diff.interactable = true;
			OpenLabel.color = new Color(0, 0, 0, 0);

			ArcTimingManager.Instance.BaseBpm = CurrentProject.BaseBpm == 0 ? 100 : CurrentProject.BaseBpm;
			BaseBpm.interactable = true;
			BaseBpm.text = ArcTimingManager.Instance.BaseBpm.ToString();

			watcher.EnableRaisingEvents = false;
			FileWatchEnableImage.color = DisableColor;

			yield return LoadCoverCoroutine();
			yield return LoadMusicCoroutine();
			yield return LoadChartCoroutine(CurrentProject.LastWorkingDifficulty, false);

			yield return AdeShutterManager.Instance.OpenCoroutine();
		}
		public void ReloadChart(int index)
		{
			StartCoroutine(LoadChartCoroutine(index, true));
		}

		public void SetDefaultCover(Sprite cover){
			if(CoverImage.sprite==DefaultCover){
				CoverImage.sprite=cover;
			}
			DefaultCover=cover;
		}

		public void OnComposerEdited()
		{
			if (CurrentProject == null) return;
			CurrentProject.Artist = Composer.text;
		}
		public void OnNameEdited()
		{
			if (CurrentProject == null) return;
			CurrentProject.Title = Name.text;
		}
		public void OnDiffEdited()
		{
			if (CurrentProject == null) return;
			if (CurrentDifficulty < 0 || CurrentDifficulty > 2) return;
			if (CurrentProject.Difficulties[CurrentDifficulty] == null)
				CurrentProject.Difficulties[CurrentDifficulty] = new AdeChartDifficulty();
			CurrentProject.Difficulties[CurrentDifficulty].Rating = Diff.text;
		}
		public void OnBaseBpmEdited()
		{
			float value;
			bool result = float.TryParse(BaseBpm.text, out value);
			if (result)
			{
				if (value <= 0) value = 100;
				if (CurrentProject != null) CurrentProject.BaseBpm = value;
				ArcTimingManager.Instance.BaseBpm = value;
				File.WriteAllText(ProjectFilePath, JsonConvert.SerializeObject(CurrentProject));
				ArcArcManager.Instance.Rebuild();
				BaseBpm.text = value.ToString();
			}
		}
		public void OnAudioOffsetEdited()
		{
			int value;
			bool result = int.TryParse(AudioOffset.text, out value);
			if (result)
			{
				ArcAudioManager.Instance.AudioOffset = value;
				AudioOffset.text = value.ToString();
			}
		}
		public void OnFileWatchClicked()
		{
			if (CurrentProject != null && CurrentDifficulty != -1)
			{
				watcher.EnableRaisingEvents = !watcher.EnableRaisingEvents;
				FileWatchEnableImage.color = watcher.EnableRaisingEvents ? EnableColor : DisableColor;
			}
		}

		private void OnWatcherChanged(object sender, FileSystemEventArgs e)
		{
			DateTime begin = DateTime.Now;
			FileStream fs = null;

		retry:
			try
			{
				fs = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.None);
			}
			catch (IOException)
			{
				if (DateTime.Now - begin < TimeSpan.FromSeconds(3))
					goto retry;
				else return;
			}

			fs?.Close();

			shouldReload = true;
		}

		public void OnSaveModeClicked()
		{
			ArcadeComposeManager.Instance.ArcadePreference.ChartSortMode = ArcadeComposeManager.Instance.ArcadePreference.ChartSortMode == ChartSortMode.Timing ? ChartSortMode.Type : ChartSortMode.Timing;
			SaveMode.text = ArcadeComposeManager.Instance.ArcadePreference.ChartSortMode == ChartSortMode.Timing ? "按时间" : "按类型";
		}
		public void OnOpenFolder()
		{
			if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProjectFolder)) return;
			Util.Windows.Dialog.OpenExplorer(CurrentProjectFolder);
		}

		public void OnApplicationQuit()
		{
			SaveProject();
		}

	}
}
