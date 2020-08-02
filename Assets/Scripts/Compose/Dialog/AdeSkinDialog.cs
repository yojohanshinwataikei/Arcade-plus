using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Arcade.Gameplay;

namespace Arcade.Compose.Dialog
{
	public class SkinPreference
	{
		public string SelectedSide;
		public string SelectedTheme;
		public string SelectedNote;
		public string SelectedBackground;
		public bool IsExternalBackground;
		[JsonIgnore]
		public Side SkinSide
		{
			get
			{
				if (SelectedSide == "Light")
				{
					return Side.Light;
				}
				else if (SelectedSide == "Conflict")
				{
					return Side.Conflict;
				}
				SelectedSide = "Light";
				return Side.Light;
			}
			set
			{
				SelectedSide = value == Side.Light ? "Light" : "Conflict";
			}
		}
	}

	public class AdeSkinDialog : MonoBehaviour
	{
		public static AdeSkinDialog Instance { get; private set; }

		public GameObject BackgroundOptionPrefab;

		public Dropdown SideDropdown;
		public Dropdown ThemeDropdown;
		public Dropdown NoteDropdown;

		public RectTransform InternalBackgroundContainer;
		public RectTransform ExternalBackgroundContainer;

		public Text ExternalBackgroundLabel;

		private Dictionary<string, int> ThemeIds;
		private Dictionary<string, int> NoteIds;
		private Dictionary<string, AdeSkinBackgroundOption> InternalBackgroundButtons;
		private Dictionary<string, AdeSkinBackgroundOption> ExternalBackgroundButtons;

		public string PreferencesSavePath
		{
			get
			{
				return ArcadeComposeManager.ArcadePersistentFolder + "/Skin.json";
			}
		}

		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			SideDropdown.onValueChanged.AddListener(SelectSide);
			NoteDropdown.onValueChanged.AddListener(SelectNote);
			ThemeDropdown.onValueChanged.AddListener(SelectTheme);
			LoadPreferences();
			LoadSkinOptions();
			VerifySkinOptions();
			LoadExternalBackgroundOptions();
			VerifyExternalBackgroundOptions();
			Initialize();
			Debug.Log($"skin:{JsonConvert.SerializeObject(preference)}");
		}

		private SkinPreference preference = new SkinPreference();


		public void OpenExternalBackgroundFolder()
		{
			if (!Directory.Exists(AdeSkinHost.Instance.ExternalBackgroundFolderPath))
			{
				try
				{
					Directory.CreateDirectory(AdeSkinHost.Instance.ExternalBackgroundFolderPath);
				}
				catch (IOException e)
				{
					Debug.LogWarning($"Cannot load external background, the background path is not a directory:{e}");
					return;
				}
			}
			Util.Shell.FileBrowser.OpenExplorer(AdeSkinHost.Instance.ExternalBackgroundFolderPath);
		}

		private void LoadSkinOptions()
		{
			ThemeDropdown.ClearOptions();
			List<string> themes = new List<string>(AdeSkinHost.Instance.skinData.ThemeDatas.Keys);
			ThemeIds = new Dictionary<string, int>();
			for (int i = 0; i < themes.Count; i++)
			{
				ThemeIds.Add(themes[i], i);
			}
			ThemeDropdown.AddOptions(themes);

			NoteDropdown.ClearOptions();
			List<string> notes = new List<string>(AdeSkinHost.Instance.skinData.NoteDatas.Keys);
			NoteIds = new Dictionary<string, int>();
			for (int i = 0; i < notes.Count; i++)
			{
				NoteIds.Add(notes[i], i);
			}
			NoteDropdown.AddOptions(notes);

			if (InternalBackgroundButtons != null)
			{
				foreach (AdeSkinBackgroundOption option in InternalBackgroundButtons.Values)
				{
					Destroy(option.gameObject);
				}
			}
			InternalBackgroundButtons = new Dictionary<string, AdeSkinBackgroundOption>();
			foreach (string bg in AdeSkinHost.Instance.skinData.BackgroundDatas.Keys)
			{
				AdeSkinBackgroundOption option = Instantiate(BackgroundOptionPrefab, InternalBackgroundContainer).GetComponent<AdeSkinBackgroundOption>();
				option.Initialize(bg, false, AdeSkinHost.Instance.skinData.BackgroundDatas[bg].background.value);
				InternalBackgroundButtons.Add(bg, option);
			}
		}

		private void LoadExternalBackgroundOptions()
		{
			if (ExternalBackgroundButtons != null)
			{
				foreach (AdeSkinBackgroundOption option in ExternalBackgroundButtons.Values)
				{
					Destroy(option.gameObject);
				}
			}
			ExternalBackgroundButtons = new Dictionary<string, AdeSkinBackgroundOption>();
			foreach (string bg in AdeSkinHost.Instance.ExternalBackgrounds.Keys)
			{
				AdeSkinBackgroundOption option = Instantiate(BackgroundOptionPrefab, ExternalBackgroundContainer).GetComponent<AdeSkinBackgroundOption>();
				option.Initialize(bg, true, AdeSkinHost.Instance.ExternalBackgrounds[bg].value);
				ExternalBackgroundButtons.Add(bg, option);
			}
			ExternalBackgroundLabel.text = AdeSkinHost.Instance.ExternalBackgrounds.Count > 0 ? "自定义背景" : "没有可用的自定义背景";
		}

		private void VerifySkinOptions()
		{
			if (!preference.IsExternalBackground)
			{
				if (preference.SelectedBackground == null || !AdeSkinHost.Instance.skinData.BackgroundDatas.ContainsKey(preference.SelectedBackground))
				{
					preference.SelectedBackground = AdeSkinHost.Instance.skinData.DefaultBackground;
				}
				AdeSkinHost.BackgroundData BgData = AdeSkinHost.Instance.skinData.BackgroundDatas[preference.SelectedBackground];
				if (BgData.side != null)
				{
					preference.SkinSide = BgData.side.Value;
				}
				if (BgData.theme != null)
				{
					preference.SelectedTheme = BgData.theme;
				}
			}
			if (preference.SelectedTheme == null || !AdeSkinHost.Instance.skinData.ThemeDatas.ContainsKey(preference.SelectedTheme))
			{
				preference.SelectedTheme = AdeSkinHost.Instance.skinData.DefaultThemeData;
			}
			if (preference.SelectedNote == null || !AdeSkinHost.Instance.skinData.NoteDatas.ContainsKey(preference.SelectedNote))
			{
				preference.SelectedNote = AdeSkinHost.Instance.skinData.DefaultNoteData;
			}
		}


		private void VerifyExternalBackgroundOptions()
		{
			if (preference.IsExternalBackground)
			{
				if (!AdeSkinHost.Instance.ExternalBackgrounds.ContainsKey(preference.SelectedBackground))
				{
					preference.IsExternalBackground = false;
					preference.SelectedBackground = AdeSkinHost.Instance.skinData.DefaultBackground;
					AdeSkinHost.BackgroundData BgData = AdeSkinHost.Instance.skinData.BackgroundDatas[preference.SelectedBackground];
					if (BgData.side != null)
					{
						preference.SkinSide = BgData.side.Value;
					}
					if (BgData.theme != null)
					{
						preference.SelectedTheme = BgData.theme;
					}
				}
			}
		}

		private void Initialize()
		{
			SideDropdown.SetValueWithoutNotify(preference.SkinSide == Side.Light ? 0 : 1);
			ThemeDropdown.SetValueWithoutNotify(ThemeIds[preference.SelectedTheme]);
			NoteDropdown.SetValueWithoutNotify(NoteIds[preference.SelectedNote]);
			CurrentBackgroundOption.SetSelected(true);
			if (!preference.IsExternalBackground)
			{
				AdeSkinHost.BackgroundData BgData = AdeSkinHost.Instance.skinData.BackgroundDatas[preference.SelectedBackground];
				SideDropdown.interactable = BgData.side == null;
				ThemeDropdown.interactable = BgData.theme == null;
			}
			else
			{
				SideDropdown.interactable = true;
				ThemeDropdown.interactable = true;
			}
			ApplySimpleSkin();
			ApplyBackground();
			ApplyNoteSideSkin();
			ApplyThemeSideSkin();
		}

		private void LoadPreferences()
		{
			try
			{
				if (File.Exists(PreferencesSavePath))
				{
					PlayerPrefs.SetString("AdeSkinDialog", File.ReadAllText(PreferencesSavePath));
					File.Delete(PreferencesSavePath);
				}
				preference = JsonConvert.DeserializeObject<SkinPreference>(PlayerPrefs.GetString("AdeSkinDialog", ""));
				if (preference == null) preference = new SkinPreference();
			}
			catch (Exception Ex)
			{
				preference = new SkinPreference();
				Debug.Log(Ex);
			}
		}

		public void SelectBackground(string name, bool external)
		{
			CurrentBackgroundOption.SetSelected(false);
			preference.IsExternalBackground = external;
			preference.SelectedBackground = name;
			ApplyBackground();
			CurrentBackgroundOption.SetSelected(true);
			if (!preference.IsExternalBackground)
			{
				AdeSkinHost.BackgroundData BgData = AdeSkinHost.Instance.skinData.BackgroundDatas[preference.SelectedBackground];
				if (BgData.side != null)
				{
					preference.SkinSide = BgData.side.Value;
					SideDropdown.SetValueWithoutNotify(preference.SkinSide == Side.Light ? 0 : 1);
				}
				SideDropdown.interactable = BgData.side == null;
				if (BgData.theme != null)
				{
					preference.SelectedTheme = BgData.theme;
					ThemeDropdown.SetValueWithoutNotify(ThemeIds[preference.SelectedTheme]);
				}
				ThemeDropdown.interactable = BgData.theme == null;
				if (BgData.side != null)
				{
					ApplyNoteSideSkin();
				}
				if (BgData.side != null || BgData.theme != null)
				{
					ApplyThemeSideSkin();
				}
			}
			else
			{
				SideDropdown.interactable = true;
				ThemeDropdown.interactable = true;
			}
		}

		public void SelectSide(int id)
		{
			Side side = id == 0 ? Side.Light : Side.Conflict;
			preference.SkinSide = side;
			ApplyThemeSideSkin();
			ApplyNoteSideSkin();
		}

		public void SelectTheme(int id)
		{
			string theme = ThemeDropdown.options[id].text;
			preference.SelectedTheme = theme;
			ApplyThemeSideSkin();
		}

		public void SelectNote(int id)
		{
			string note = NoteDropdown.options[id].text;
			preference.SelectedNote = note;
			ApplyNoteSideSkin();
		}

		public void ReloadBackgroundFolder(){
			CurrentBackgroundOption.SetSelected(false);
			AdeSkinHost.Instance.LoadExternalBackground();
			LoadExternalBackgroundOptions();
			VerifyExternalBackgroundOptions();
			Initialize();
		}

			public void ReloadSkinFolder(){
			CurrentBackgroundOption.SetSelected(false);
			AdeSkinHost.Instance.LoadSkinDatas();
			LoadSkinOptions();
			VerifySkinOptions();
			Initialize();
		}

		private AdeSkinBackgroundOption CurrentBackgroundOption => (preference.IsExternalBackground ? ExternalBackgroundButtons : InternalBackgroundButtons)[preference.SelectedBackground];

		private void ApplyBackground()
		{
			if (preference.IsExternalBackground)
			{
				ArcSkinManager.Instance.SetBackground(AdeSkinHost.Instance.ExternalBackgrounds[preference.SelectedBackground].value);
			}
			else
			{
				ArcSkinManager.Instance.SetBackground(AdeSkinHost.Instance.skinData.BackgroundDatas[preference.SelectedBackground].background.value);
			}
		}
		private void ApplySimpleSkin()
		{
			ArcSkinManager.Instance.SetSimpleSkin(AdeSkinHost.Instance.skinData);
		}
		private void ApplyNoteSideSkin()
		{
			AdeSkinHost.WithSideData<AdeSkinHost.NoteSideData> note = AdeSkinHost.Instance.skinData.NoteDatas[preference.SelectedNote];
			if (preference.SkinSide == Side.Light)
			{
				ArcSkinManager.Instance.SetNoteSideSkin(note.Light);
			}
			else
			{
				ArcSkinManager.Instance.SetNoteSideSkin(note.Conflict);
			}
		}
		private void ApplyThemeSideSkin()
		{
			AdeSkinHost.WithSideData<AdeSkinHost.ThemeSideData> theme = AdeSkinHost.Instance.skinData.ThemeDatas[preference.SelectedTheme];
			if (preference.SkinSide == Side.Light)
			{
				ArcSkinManager.Instance.SetThemeSideSkin(theme.Light);
			}
			else
			{
				ArcSkinManager.Instance.SetThemeSideSkin(theme.Conflict);
			}
		}

		public void SavePreferences()
		{
			PlayerPrefs.SetString("AdeSkinDialog", JsonConvert.SerializeObject(preference));
		}
		private void OnApplicationQuit()
		{
			SavePreferences();
		}
	}
}
