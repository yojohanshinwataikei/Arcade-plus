using System;
using System.Collections;
using System.IO;
using System.Linq;
using Arcade.Compose.Dialog;
using Arcade.Compose.Feature;
using Arcade.Compose.UI;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Arcade.Compose.Command;
using Arcade.Util.Loader;

namespace Arcade.Compose
{
	[Serializable]
	public class AdeChartDifficultyMetadata
	{
		public string Rating;
	}
	[Serializable]
	public class ArcadeProjectMetadata
	{
		public string Title;
		public string Artist;
		public float BaseBpm;
		public AdeChartDifficultyMetadata[] Difficulties = new AdeChartDifficultyMetadata[4];

		public int LastWorkingDifficulty = 2;
		public int LastWorkingTiming;

	}

	public class AdeProjectManager : MonoBehaviour
	{
		public static AdeProjectManager Instance { get; private set; }

		public string CurrentProjectFolder { get; set; }
		public ArcadeProjectMetadata CurrentProjectMetadata { get; set; }
		public int CurrentDifficulty { get; set; } = 2;

		public Sprite DefaultCover;
		public Image CoverImage;
		public Image[] DifficultyImages;
		public InputField Name, Composer, Diff, BaseBpm, AudioOffset;
		public Button CurrentTimingGroup;
		public Text OpenLabel;
		public Text SaveMode;

		public Color EnableColor, DisableColor;
		public Image FileWatchEnableImage;

		public CanvasGroup TutorialCanvasGroup;
		public Text TutorialText;

		private AudioClip AudioClip;
		private Texture2D Cover;
		private Sprite CoverSprite;

		private Coroutine loadingCoroutine;
		private FileSystemWatcher watcher = new FileSystemWatcher();
		private bool shouldReload = false;

		public string ProjectArcadeFolder
		{
			get
			{
				return Path.Combine(CurrentProjectFolder, "Arcade");
			}
		}

		public string ProjectMetadataFilePath
		{
			get
			{
				return Path.Combine(ProjectArcadeFolder, "Project.arcade");
			}
		}
		public string ProjectAutosaveFolder
		{
			get
			{
				return Path.Combine(ProjectArcadeFolder, "Autosave");
			}
		}
		public string ProjectBackupFolder
		{
			get
			{
				return Path.Combine(ProjectArcadeFolder, "Backup");
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
			watcher.EnableRaisingEvents = false;
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

		private void CreateArcadeDirectories(string folder)
		{
			string[] directories = new string[] {
					ProjectArcadeFolder,
					ProjectAutosaveFolder,
				 	ProjectBackupFolder,
				};
			foreach (var s in directories) if (!Directory.Exists(s)) Directory.CreateDirectory(s);
		}

		public void CleanProject()
		{
			if (CurrentProjectMetadata == null) return;
			if (AudioClip != null)
			{
				Destroy(AudioClip);
				AudioClip = null;
			}
			if (Cover != null)
			{
				Destroy(Cover);
				Cover = null;
			}
			if (CoverSprite != null)
			{
				Destroy(CoverSprite);
				CoverSprite = null;
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
			CurrentTimingGroup.interactable = false;
			watcher.EnableRaisingEvents = false;
			FileWatchEnableImage.color = DisableColor;
			ArcGameplayManager.Instance.Clean();
		}
		public void OpenProject()
		{
			if (loadingCoroutine != null)
			{
				return;
			}
			try
			{
				string folder = Util.Shell.FileBrowser.OpenFolderDialog("选择您的 Arcaea 自制谱文件夹 (包含 0/1/2.aff, base.mp3/ogg/wav, base.jpg)");
				if (folder == null) return;
				loadingCoroutine = StartCoroutine(LoadProjectCoroutine(folder));
			}
			catch (Exception Ex)
			{
				AdeSingleDialog.Instance.Show(Ex.Message, "读取错误");
				Debug.Log(Ex);
				CurrentProjectMetadata = null;
				CurrentProjectFolder = null;
			}
		}
		public void SaveProject()
		{
			if (CurrentProjectMetadata == null || CurrentProjectFolder == null) return;
			if (ArcGameplayManager.Instance.Chart == null) return;
			CurrentProjectMetadata.LastWorkingDifficulty = CurrentDifficulty;
			CurrentProjectMetadata.LastWorkingTiming = ArcGameplayManager.Instance.Timing;
			File.WriteAllText(ProjectMetadataFilePath, JsonConvert.SerializeObject(CurrentProjectMetadata));
			string path = Path.Combine(CurrentProjectFolder, $"{CurrentDifficulty}.aff");
			string backupPath = Path.Combine(ProjectBackupFolder, $"{CurrentDifficulty}_{DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")}.aff");
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
				if (CurrentProjectMetadata == null || CurrentProjectFolder == null) continue;
				if (ArcGameplayManager.Instance.Chart == null) continue;
				string backupPath = Path.Combine(ProjectAutosaveFolder, $"{CurrentDifficulty}_{DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")}.aff");
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

		private IEnumerator LoadChartCoroutine(int difficulty)
		{
			yield return AdeShutterManager.Instance.CloseCoroutine();
			LoadChart(difficulty);
			if (!ArcGameplayManager.Instance.IsLoaded)
			{
				SetTutorialMessage("无法加载谱面文件，请修正谱面文件格式或删除谱面文件后重新打开谱面文件夹");
			}
			yield return AdeShutterManager.Instance.OpenCoroutine();
			loadingCoroutine = null;
		}

		private IEnumerator LoadProjectCoroutine(string folder)
		{
			yield return AdeShutterManager.Instance.CloseCoroutine();
			LoadProject(folder);
			yield return AdeShutterManager.Instance.OpenCoroutine();
			loadingCoroutine = null;
		}
		public void ReloadChart(int index)
		{
			if (loadingCoroutine != null)
			{
				return;
			}
			if (CurrentProjectMetadata == null)
			{
				return;
			}
			loadingCoroutine = StartCoroutine(LoadChartCoroutine(index));
		}

		public void SetDefaultCover(Sprite cover)
		{
			if (CoverImage.sprite == DefaultCover)
			{
				CoverImage.sprite = cover;
			}
			DefaultCover = cover;
		}

		private void LoadProject(string folder)
		{
			CleanProject();
			watcher.EnableRaisingEvents = false;
			FileWatchEnableImage.color = DisableColor;
			CurrentProjectFolder = folder;
			CreateArcadeDirectories(folder);
			try
			{
				LoadMetadata();
			}
			catch (Exception Ex)
			{
				AdeSingleDialog.Instance.Show(Ex.Message, "元信息读取错误");
				SetTutorialMessage("无法加载工程元信息，请删除谱面文件夹下的 Arcade 文件夹后重新打开谱面文件夹");
				return;
			}
			LoadCover();
			LoadAudio();
			if (AudioClip == null)
			{
				SetTutorialMessage("无法加载音频文件，请确认音频文件存在且格式正确后重新打开谱面文件夹");
				return;
			}
			LoadChart(CurrentProjectMetadata.LastWorkingDifficulty);
			if (!ArcGameplayManager.Instance.IsLoaded)
			{
				SetTutorialMessage("无法加载谱面文件，请修正谱面文件格式或删除谱面文件后重新打开谱面文件夹");
				return;
			}
			SetTutorialMessage(null);
		}

		private void LoadMetadata()
		{
			try
			{
				CurrentProjectMetadata = JsonConvert.DeserializeObject<ArcadeProjectMetadata>(File.ReadAllText(ProjectMetadataFilePath));
			}
			catch
			{
				CurrentProjectMetadata = new ArcadeProjectMetadata();
				File.WriteAllText(ProjectMetadataFilePath, JsonConvert.SerializeObject(CurrentProjectMetadata));
			}
			if (CurrentProjectMetadata.Difficulties.Length < 4)
			{
				var Difficulties = new AdeChartDifficultyMetadata[4];
				CurrentProjectMetadata.Difficulties.CopyTo(Difficulties, 0);
				CurrentProjectMetadata.Difficulties = Difficulties;
			}

			Name.text = CurrentProjectMetadata.Title;
			Composer.text = CurrentProjectMetadata.Artist;
			Diff.text = "";
			Name.interactable = true;
			Composer.interactable = true;
			Diff.interactable = true;
			OpenLabel.color = new Color(0, 0, 0, 0);

			ArcTimingManager.Instance.BaseBpm = CurrentProjectMetadata.BaseBpm == 0 ? 100 : CurrentProjectMetadata.BaseBpm;
			BaseBpm.interactable = true;
			BaseBpm.text = ArcTimingManager.Instance.BaseBpm.ToString();
		}

		private void LoadCover()
		{
			string coverPath = Path.Combine(CurrentProjectFolder, "base.jpg");
			Texture2D texture = Loader.LoadTexture2D(coverPath);
			if (texture == null)
			{

				CoverImage.sprite = DefaultCover;
				return;
			}
			Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
			sprite.name = coverPath;
			Cover = texture;
			CoverSprite = sprite;
			CoverImage.sprite = CoverSprite;
		}
		private void LoadAudio()
		{
			string[] files = new string[]{
				Path.Combine(CurrentProjectFolder, "base.ogg"),
				Path.Combine(CurrentProjectFolder, "base.mp3"),
				Path.Combine(CurrentProjectFolder, "base.wav"),
				};
			foreach (string audioPath in files)
			{
				AudioClip clip = Loader.LoadAudioFile(audioPath);
				if (clip != null)
				{
					AudioClip = clip;
					AdeTimingSlider.Instance.Enable = true;
					AdeTimingSlider.Instance.Length = (int)(AudioClip.length * 1000);
					return;
				}
			}
		}

		private void LoadChart(int difficulty)
		{
			if (CurrentProjectMetadata == null || CurrentProjectFolder == null || AudioClip == null)
			{
				return;
			}
			string chartPath = Path.Combine(CurrentProjectFolder, $"{difficulty}.aff");
			if (!File.Exists(chartPath))
			{
				File.WriteAllText(chartPath, "AudioOffset:0\n-\ntiming(0,100.00,4.00);");
			}
			ArcadeComposeManager.Instance.Pause();
			AdeObsManager.Instance.ForceClose();
			CommandManager.Instance.Clear();
			Aff.RawAffChart raw = null;
			try
			{
				raw = Aff.ArcaeaFileFormat.ParseFromPath(chartPath);
			}
			catch (Exception Ex)
			{
				Debug.LogWarning(Ex);
				AdeSingleDialog.Instance.Show(Ex.Message, "谱面读取错误");
			}
			Gameplay.Chart.ArcChart chart = null;
			if (raw != null)
			{
				if (raw.error.Count > 0)
				{
					AdeSingleDialog.Instance.Show($"格式问题：\n{string.Join("\n", raw.error.Select(s => $"- {s}"))}", "谱面解析失败");
				}
				else
				{
					if (raw.warning.Count > 0)
					{
						AdeSingleDialog.Instance.Show($"Arcade-Plus 检测到谱面存在问题，并对谱面进行了自动修复\n谱面在存在的问题：\n{string.Join("\n", raw.warning.Select(s => $"- {s}"))}", "谱面解析出现问题");
					}
					chart = new Gameplay.Chart.ArcChart(raw);
				}
			}
			ArcGameplayManager.Instance.Load(chart, AudioClip);
			CurrentDifficulty = difficulty;

			Diff.text = CurrentProjectMetadata.Difficulties[CurrentDifficulty] == null ? "" : CurrentProjectMetadata.Difficulties[CurrentDifficulty].Rating;
			foreach (Image i in DifficultyImages) i.color = new Color(1f, 1f, 1f, 0.6f);
			DifficultyImages[difficulty].color = new Color(1, 1, 1, 1);

			AudioOffset.interactable = true;
			CurrentTimingGroup.interactable = true;
			AudioOffset.text = ArcAudioManager.Instance.AudioOffset.ToString();

			watcher.Path = CurrentProjectFolder;
			watcher.Filter = $"{difficulty}.aff";
			ArcGameplayManager.Instance.Timing = CurrentProjectMetadata.LastWorkingTiming;
		}

		private void SetTutorialMessage(string message)
		{
			if (message == null)
			{
				TutorialCanvasGroup.alpha = 0;
				return;
			}
			TutorialCanvasGroup.alpha = 1;
			TutorialText.text = message;
		}

		public void OnComposerEdited()
		{
			if (CurrentProjectMetadata == null) return;
			CurrentProjectMetadata.Artist = Composer.text;
		}
		public void OnNameEdited()
		{
			if (CurrentProjectMetadata == null) return;
			CurrentProjectMetadata.Title = Name.text;
		}
		public void OnDiffEdited()
		{
			if (CurrentProjectMetadata == null) return;
			if (CurrentDifficulty < 0 || CurrentDifficulty > 3) return;
			if (CurrentProjectMetadata.Difficulties[CurrentDifficulty] == null)
				CurrentProjectMetadata.Difficulties[CurrentDifficulty] = new AdeChartDifficultyMetadata();
			CurrentProjectMetadata.Difficulties[CurrentDifficulty].Rating = Diff.text;
		}
		public void OnBaseBpmEdited()
		{
			float value;
			bool result = float.TryParse(BaseBpm.text, out value);
			if (result)
			{
				if (value <= 0) value = 100;
				if (CurrentProjectMetadata != null) CurrentProjectMetadata.BaseBpm = value;
				ArcTimingManager.Instance.BaseBpm = value;
				File.WriteAllText(ProjectMetadataFilePath, JsonConvert.SerializeObject(CurrentProjectMetadata));
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
			if (CurrentProjectMetadata != null && CurrentDifficulty != -1)
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
			if (CurrentProjectMetadata == null || string.IsNullOrWhiteSpace(CurrentProjectFolder)) return;
			Util.Shell.FileBrowser.OpenExplorer(CurrentProjectFolder);
		}

		public void OnApplicationQuit()
		{
			SaveProject();
		}
	}
}
