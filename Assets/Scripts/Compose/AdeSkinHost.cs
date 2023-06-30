using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Arcade.Util.Loader;
using Newtonsoft.Json;
using UnityEngine;

namespace Arcade.Compose
{
	public enum Side
	{
		Light, Conflict, Colorless
	}

	// This class store all skin related data read from skin folder
	public class AdeSkinHost : MonoBehaviour
	{
		// Note: this class is only used to read Default Skin Data from the scene/prefab file, so that we can edit it in the editor
		// Unity has a strict limitation for serializable scripts in the editor, see https://docs.unity3d.com/Manual/script-Serialization.html
		[Serializable]
		public class RawSkinDefaults
		{
			public Sprite SongInfo;
			public Sprite ProgressGlow;
			public Sprite UnknownCover;
			public Sprite DifficultyPast;
			public Sprite DifficultyPresent;
			public Sprite DifficultyFuture;
			public Sprite DifficultyBeyond;
			public Sprite Pause;
			public Sprite PausePressed;
			public Sprite Play;
			public Sprite PlayPressed;
			public Sprite DialogTop;
			public Sprite DialogBackground;
			public Sprite ButtonSingle;
			public Sprite ButtonSinglePressed;
			public Sprite ButtonSingleDisabled;
			public Sprite ButtonDualLeft;
			public Sprite ButtonDualLeftPressed;
			public Sprite ButtonDualLeftDisabled;
			public Sprite ButtonDualRight;
			public Sprite ButtonDualRightPressed;
			public Sprite ButtonDualRightDisabled;
			public Sprite ShutterLeft;
			public Sprite ShutterRight;
			public AudioClip ShutterOpen;
			public AudioClip ShutterClose;
			public AudioClip TapSound;
			public AudioClip ArcSound;
			public Sprite TutorialBanner;
			public Sprite BackgroundDarken;
			public Sprite SkyInputLabel;
			public Sprite SkyInputLine;
			public Sprite TrackLaneDivider;
			public Sprite LaneHit;
			public Texture2D ParticleArc;
			public Texture2D ParticleSfxTap;
			public Sprite ArcCap;
			public Sprite ArcTapShadow;
			public Sprite HeightIndicator;
			public Texture2D ArcBody;
			public Texture2D ArcBodyHighlight;
			public Sprite TapNoteLight;
			public Sprite TapNoteConflict;
			public Texture2D ArcTapLight;
			public Texture2D ArcTapConflict;
			public Texture2D SfxArcTapNoteLight;
			public Texture2D SfxArcTapNoteConflict;
			public Texture2D SfxArcTapCoreLight;
			public Texture2D SfxArcTapCoreConflict;
			public Mesh SfxArcTapModel;
			public Color ConnectionLineColorLight;
			public Color ConnectionLineColorConflict;
			public Sprite HoldNoteLight;
			public Sprite HoldNoteConflict;
			public Sprite HoldNoteHighlightLight;
			public Sprite HoldNoteHighlightConflict;
			public Color ArcRedLow;
			public Color ArcBlueLow;
			public Color ArcGreenLow;
			public Color ArcRedHigh;
			public Color ArcBlueHigh;
			public Color ArcGreenHigh;
			public Color ArcVoid;
			public Sprite TrackLight;
			public Sprite TrackConflict;
			public Sprite TrackExtraLight;
			public Sprite TrackExtraConflict;
			public Sprite CriticalLine;
			public Sprite CriticalLineExtraLight;
			public Sprite CriticalLineExtraConflict;
			public Color ComboTextColorLight;
			public Color ComboTextColorConflict;
			public Texture2D ParticleNote;
			public Color ParticleArcStartColor;
			public Color ParticleArcEndColor;
			public Sprite BackgroundLight;
			public Sprite BackgroundConflict;
		}
		// For debug propose, any thing in the SkinDatas should be able to be printed as a string
		// If the value do not contained the needed infomation, we attach a label to it showing the source of the asset
		public struct Labelled<T>
		{
			public string label;
			public T value;
		}
		// The "Assets/DefaultSkin" folder is a standard skin folder without the config files
		public class SkinDatas
		{
			public Labelled<Sprite> SongInfo;
			public Labelled<Sprite> ProgressGlow;
			public Labelled<Sprite> UnknownCover;
			public Labelled<Sprite> DifficultyPast;
			public Labelled<Sprite> DifficultyPresent;
			public Labelled<Sprite> DifficultyFuture;
			public Labelled<Sprite> DifficultyBeyond;
			public Labelled<Sprite> Pause;
			public Labelled<Sprite> PausePressed;
			public Labelled<Sprite> Play;
			public Labelled<Sprite> PlayPressed;
			public Labelled<Sprite> DialogTop;
			public Labelled<Sprite> DialogBackground;
			public Labelled<Sprite> ButtonSingle;
			public Labelled<Sprite> ButtonSinglePressed;
			public Labelled<Sprite> ButtonSingleDisabled;
			public Labelled<Sprite> ButtonDualLeft;
			public Labelled<Sprite> ButtonDualLeftPressed;
			public Labelled<Sprite> ButtonDualLeftDisabled;
			public Labelled<Sprite> ButtonDualRight;
			public Labelled<Sprite> ButtonDualRightPressed;
			public Labelled<Sprite> ButtonDualRightDisabled;
			public Labelled<Sprite> ShutterLeft;
			public Labelled<Sprite> ShutterRight;
			public Labelled<AudioClip> ShutterOpen;
			public Labelled<AudioClip> ShutterClose;
			public Labelled<AudioClip> TapSound;
			public Labelled<AudioClip> ArcSound;
			public Labelled<Sprite> TutorialBanner;
			public Labelled<Sprite> BackgroundDarken;
			public Labelled<Sprite> SkyInputLabel;
			public Labelled<Sprite> SkyInputLine;
			public Labelled<Sprite> TrackLaneDivider;
			public Labelled<Sprite> LaneHit;
			public Labelled<Texture2D> ParticleSfxTap;
			public Labelled<Texture2D> ParticleArc;
			public Labelled<Sprite> ArcCap;
			public Labelled<Sprite> ArcTapShadow;
			public Labelled<Sprite> HeightIndicator;
			public Labelled<Texture2D> ArcBody;
			public Labelled<Texture2D> ArcBodyHighlight;
			public Labelled<Mesh> SfxArcTapModel;
			public string DefaultNoteData;
			public Dictionary<string, WithSideData<NoteSideData>> NoteDatas;
			public string DefaultThemeData;
			public Dictionary<string, WithSideData<ThemeSideData>> ThemeDatas;
			public string DefaultBackground;
			public Dictionary<string, BackgroundData> BackgroundDatas;
		}
		//The data of one note type in one side
		public class NoteSideData
		{
			public Labelled<Sprite> TapNote;
			public Labelled<Texture2D> ArcTap;
			public Labelled<Texture2D> SfxArcTapNote;
			public Labelled<Texture2D> SfxArcTapCore;
			public Color ConnectionLineColor;
			public Labelled<Sprite> HoldNote;
			public Labelled<Sprite> HoldNoteHighlight;
			public Color ArcRedLow;
			public Color ArcBlueLow;
			public Color ArcGreenLow;
			public Color ArcRedHigh;
			public Color ArcBlueHigh;
			public Color ArcGreenHigh;
			public Color ArcVoid;
		};
		//The data of onetheme in one side
		public class ThemeSideData
		{
			public Labelled<Sprite> Track;
			public Labelled<Sprite> TrackExtra;
			public Labelled<Sprite> CriticalLine;
			public Labelled<Sprite> CriticalLineExtra;
			public Color ComboTextColor;
			public Labelled<Texture2D> ParticleNote;
			public Color ParticleArcStartColor;
			public Color ParticleArcEndColor;
		}
		public class BackgroundData
		{
			public Labelled<Sprite> background;
			public Side? side;
			public string theme;
		}
		public class WithSideData<T>
		{
			public T Light;
			public T Conflict;
			public T Colorless;
		};
		[Serializable]
		public class SkinDataSpec
		{
			public CollectionDataSpec<NoteSideDataSpec> note;
			public CollectionDataSpec<ThemeSideDataSpec> theme;
			public BackgroundDataSpec[] background;
		}
		[Serializable]
		public class NoteSideDataSpec
		{
			public string tapnote;
			public string holdnote;
			[JsonPropertyAttribute("holdnote-highlight")]
			public string holdnoteHighlight;
			public string arctap;
			[JsonPropertyAttribute("sfx-arctap-note")]
			public string sfxArctapNote;
			[JsonPropertyAttribute("sfx-arctap-core")]
			public string sfxArctapCore;
			[JsonPropertyAttribute("connection-line-color")]
			public string connectionLineColor;
			[JsonPropertyAttribute("arc-red-low")]
			public string arcRedLow;
			[JsonPropertyAttribute("arc-blue-low")]
			public string arcBlueLow;
			[JsonPropertyAttribute("arc-green-low")]
			public string arcGreenLow;
			[JsonPropertyAttribute("arc-red-high")]
			public string arcRedHigh;
			[JsonPropertyAttribute("arc-blue-high")]
			public string arcBlueHigh;
			[JsonPropertyAttribute("arc-green-high")]
			public string arcGreenHigh;
			[JsonPropertyAttribute("arc-void")]
			public string arcVoid;
		}
		[Serializable]
		public class ThemeSideDataSpec
		{
			public string track;
			[JsonPropertyAttribute("track-extra")]
			public string trackExtra;
			[JsonPropertyAttribute("track-critical-line")]
			public string trackCriticalLine;
			[JsonPropertyAttribute("track-critical-line-extra")]
			public string trackCriticalLineExtra;
			[JsonPropertyAttribute("combo-text-color")]
			public string comboTextColor;
			[JsonPropertyAttribute("particle-note")]
			public string particleNote;
			[JsonPropertyAttribute("particle-arc-start-color")]
			public string particleArcStartColor;
			[JsonPropertyAttribute("particle-arc-end-color")]
			public string particleArcEndColor;
		}
		[Serializable]
		public class WithSideDataSpec<T>
		{
			public string name;
			public T light;
			public T conflict;
			public T colorless;
		};
		[Serializable]
		public class CollectionDataSpec<T>
		{
			[JsonPropertyAttribute("default-name")]
			public string defaultName;
			public T light;
			public T conflict;
			public T colorless;
			public List<WithSideDataSpec<T>> additional;
		}
		[Serializable]
		public class BackgroundDataSpec
		{
			public string name;
			public string file;
			public string side;
			public string theme;
		}

		public static AdeSkinHost Instance { get; private set; }
		private List<UnityEngine.Object> externalSkinDataObjects = new List<UnityEngine.Object>();
		private List<UnityEngine.Object> externalBackgroundDataObjects = new List<UnityEngine.Object>();
		public RawSkinDefaults rawDefaultData;
		public SkinDatas skinData;
		public Dictionary<string, Labelled<Sprite>> ExternalBackgrounds;
		public string SkinFolderPath
		{
			get
			{
				// Note: We do not hase the executable path available, so we use the dataPath
				// but relationship between executable path and data path can be different in different platform.
				return Path.Combine(new DirectoryInfo(Application.dataPath).Parent.FullName, "Skin");
			}
		}

		public string ExternalBackgroundFolderPath
		{
			get
			{
				// Note: Same as above
				return Path.Combine(new DirectoryInfo(Application.dataPath).Parent.FullName, "Background");
			}
		}
		private void Awake()
		{
			Instance = this;
		}

		private void Start()
		{
			LoadSkinDatas();
			LoadExternalBackground();
		}

		private void OnDestroy()
		{
			DestroyExternalResources(externalSkinDataObjects);
			DestroyExternalResources(externalBackgroundDataObjects);
		}

		// Note: the user generated assets will not gc by the csharp run time, so we should Destroy them manually
		// In our implementation, all skin assets are considered owned and managed by this class, the other references are only weak ones
		// However, after reloading of the skin, existed reference to the old assets should be replaced with the new ones to prevent crashing
		private void DestroyExternalResources(List<UnityEngine.Object> list)
		{
			if (list != null)
			{
				foreach (UnityEngine.Object obj in list)
				{
					Destroy(obj);
				}
				list.Clear();
			}
		}

		#region SkinLoading
		// Note: the LoadSkinDatas is a long synchronized method, so the application will freeze during this processing
		// However, If we use the network library in unity to load things, we will handle the complex coroutine programming,
		// and converting between uri and filepath.
		// Maybe the best solution is running this function in another thread, but the technical detail is unknown.
		// TODO: It will be better to add something indicate the loading.
		public void LoadSkinDatas()
		{
			this.skinData = null;
			DestroyExternalResources(externalSkinDataObjects);
			SkinDatas skinData = new SkinDatas();

			skinData.SongInfo = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "SongInfo.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.SongInfo);
			skinData.ProgressGlow = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "ProgressGlow.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.ProgressGlow);
			skinData.UnknownCover = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "UnknownCover.jpg"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.UnknownCover);

			skinData.DifficultyPast = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Difficulties", "Past.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.DifficultyPast);
			skinData.DifficultyPresent = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Difficulties", "Present.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.DifficultyPresent);
			skinData.DifficultyFuture = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Difficulties", "Future.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.DifficultyFuture);
			skinData.DifficultyBeyond = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Difficulties", "Beyond.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.DifficultyBeyond);

			skinData.Pause = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "PlayPause", "Pause.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.Pause);
			skinData.PausePressed = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "PlayPause", "PausePressed.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.PausePressed);
			skinData.Play = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "PlayPause", "Play.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.Play);
			skinData.PlayPressed = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "PlayPause", "PlayPressed.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.PlayPressed);

			skinData.DialogTop = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Dialog", "DialogTop.png"), (path) =>
			{
				return Load9SliceSprite(path, new Vector4(36, 0, 40, 0), externalSkinDataObjects);
			}, rawDefaultData.DialogTop);
			skinData.DialogBackground = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Dialog", "DialogBackground.png"), (path) =>
			{
				return Load9SliceSprite(path, new Vector4(36, 60, 40, 0), externalSkinDataObjects);
			}, rawDefaultData.DialogBackground);

			skinData.ButtonSingle = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Dialog", "ButtonSingle.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.ButtonSingle);
			skinData.ButtonSinglePressed = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Dialog", "ButtonSinglePressed.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.ButtonSinglePressed);
			skinData.ButtonSingleDisabled = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Dialog", "ButtonSingleDisabled.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.ButtonSingleDisabled);

			skinData.ButtonDualLeft = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Dialog", "ButtonDualLeft.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.ButtonDualLeft);
			skinData.ButtonDualLeftPressed = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Dialog", "ButtonDualLeftPressed.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.ButtonDualLeftPressed);
			skinData.ButtonDualLeftDisabled = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Dialog", "ButtonDualLeftDisabled.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.ButtonDualLeftDisabled);

			skinData.ButtonDualRight = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Dialog", "ButtonDualRight.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.ButtonDualRight);
			skinData.ButtonDualRightPressed = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Dialog", "ButtonDualRightPressed.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.ButtonDualRightPressed);
			skinData.ButtonDualRightDisabled = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Dialog", "ButtonDualRightDisabled.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.ButtonDualRightDisabled);

			skinData.ShutterLeft = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Shutter", "ShutterLeft.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.ShutterLeft);
			skinData.ShutterRight = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "UI", "Shutter", "ShutterRight.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.ShutterRight);
			skinData.ShutterOpen = LoadLabelled<AudioClip>(Path.Combine(SkinFolderPath, "Sound", "ShutterOpen.wav"), (path) =>
			{
				return LoadWavAudioClip(path, externalSkinDataObjects);
			}, rawDefaultData.ShutterOpen);
			skinData.ShutterClose = LoadLabelled<AudioClip>(Path.Combine(SkinFolderPath, "Sound", "ShutterClose.wav"), (path) =>
			{
				return LoadWavAudioClip(path, externalSkinDataObjects);
			}, rawDefaultData.ShutterClose);

			skinData.TapSound = LoadLabelled<AudioClip>(Path.Combine(SkinFolderPath, "Sound", "Tap.wav"), (path) =>
			{
				return LoadWavAudioClip(path, externalSkinDataObjects);
			}, rawDefaultData.TapSound);
			skinData.ArcSound = LoadLabelled<AudioClip>(Path.Combine(SkinFolderPath, "Sound", "Arc.wav"), (path) =>
			{
				return LoadWavAudioClip(path, externalSkinDataObjects);
			}, rawDefaultData.ArcSound);

			skinData.TutorialBanner = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "Tutorial.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.TutorialBanner);
			skinData.BackgroundDarken = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "BackgroundDarken.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.TutorialBanner);

			skinData.SkyInputLabel = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "SkyInput", "SkyInputLabel.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.SkyInputLabel);
			skinData.SkyInputLine = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "SkyInput", "SkyInputLine.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.SkyInputLine);

			skinData.TrackLaneDivider = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "Track", "TrackLaneDivider.png"), (path) =>
			{
				return LoadPivotSprite(path, new Vector2(0.5f, 0.0f), externalSkinDataObjects);
			}, rawDefaultData.TrackLaneDivider);
			skinData.LaneHit = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "Track", "LaneHit.png"), (path) =>
			{
				return LoadPivotSprite(path, new Vector2(0.0f, 0.0f), externalSkinDataObjects);
			}, rawDefaultData.LaneHit);

			skinData.ParticleSfxTap = LoadLabelled<Texture2D>(Path.Combine(SkinFolderPath, "Playfield", "Particle", "ParticleSfxTap.png"), (path) =>
			{
				return LoadTexture2D(path, externalSkinDataObjects);
			}, rawDefaultData.ParticleSfxTap);

			skinData.ParticleArc = LoadLabelled<Texture2D>(Path.Combine(SkinFolderPath, "Playfield", "Particle", "ParticleArc.png"), (path) =>
			{
				return LoadTexture2D(path, externalSkinDataObjects);
			}, rawDefaultData.ParticleArc);

			skinData.ArcCap = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "Note", "ArcCap.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.ArcCap);

			skinData.ArcTapShadow = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "Note", "ArcTapShadow.png"), (path) =>
			{
				return LoadNormalSprite(path, externalSkinDataObjects);
			}, rawDefaultData.ArcTapShadow);

			skinData.HeightIndicator = LoadLabelled<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "Note", "HeightIndicator.png"), (path) =>
			{
				return LoadPivotSprite(path, new Vector2(0.5f, 0.0f), externalSkinDataObjects);
			}, rawDefaultData.HeightIndicator);

			skinData.ArcBody = LoadLabelled<Texture2D>(Path.Combine(SkinFolderPath, "Playfield", "Note", "ArcBody", "ArcBody.png"), (path) =>
			{
				return LoadTexture2D(path, externalSkinDataObjects);
			}, rawDefaultData.ArcBody);
			skinData.ArcBodyHighlight = LoadLabelled<Texture2D>(Path.Combine(SkinFolderPath, "Playfield", "Note", "ArcBody", "ArcBodyHighlight.png"), (path) =>
			{
				return LoadTexture2D(path, externalSkinDataObjects);
			}, rawDefaultData.ArcBodyHighlight);

			skinData.SfxArcTapModel = LoadLabelled<Mesh>(Path.Combine(SkinFolderPath, "Playfield", "Note", "SfxArcTap", "SfxArcTap.obj"), (path) =>
			{
				return LoadObjMesh(path, externalSkinDataObjects);
			}, rawDefaultData.SfxArcTapModel);

			string specData = "";
			try
			{
				specData = File.ReadAllText(Path.Combine(SkinFolderPath, "Arcade-editor-skin.json"));
			}
			catch
			{

			}

			SkinDataSpec spec = JsonConvert.DeserializeObject<SkinDataSpec>(specData, new JsonSerializerSettings
			{
				Error = (sender, error) =>
				{
					Debug.LogWarning($"Error when parsing skin spec, related fields will be ignored:\n{error.ErrorContext.Error.Message}");
					error.ErrorContext.Handled = true;
				}
			});
			Debug.Log($"spec:{JsonConvert.SerializeObject(spec)}");

			LoadNoteDatas(skinData, spec?.note);
			LoadThemeDatas(skinData, spec?.theme);
			LoadBackgroundDatas(skinData, spec?.background);

			this.skinData = skinData;
			return;
		}

		public void LoadExternalBackground()
		{
			DestroyExternalResources(externalBackgroundDataObjects);
			ExternalBackgrounds = new Dictionary<string, Labelled<Sprite>>();
			if (!Directory.Exists(ExternalBackgroundFolderPath))
			{
				try
				{
					Directory.CreateDirectory(ExternalBackgroundFolderPath);
				}
				catch (IOException e)
				{
					Debug.LogWarning($"Cannot load external background, the background path is not a directory:{e}");
					return;
				}
			}
			foreach (string filepath in Directory.GetFiles(ExternalBackgroundFolderPath, "*.jpg"))
			{
				Sprite background = LoadNormalSprite(filepath, externalBackgroundDataObjects);
				if (background != null)
				{
					string id = Path.GetFileNameWithoutExtension(filepath);
					if (ExternalBackgrounds.ContainsKey(id))
					{
						Debug.LogWarning($"Duplicated background id \"{id}\" will be ignored");
					}
					else
					{
						ExternalBackgrounds.Add(id, new Labelled<Sprite> { value = background, label = filepath });
					}
				}
			}
		}

		private void LoadBackgroundDatas(SkinDatas skinData, BackgroundDataSpec[] spec)
		{
			skinData.BackgroundDatas = new Dictionary<string, BackgroundData>();
			if (spec != null)
			{
				foreach (BackgroundDataSpec backgroundDataSpec in spec)
				{
					LoadBackgroundData(skinData, backgroundDataSpec);
				}
			}
			if (skinData.DefaultBackground == null)
			{
				skinData.DefaultBackground = "\"DefaultLight\"";
				skinData.BackgroundDatas.Add("\"DefaultLight\"", new BackgroundData()
				{
					background = new Labelled<Sprite>() { value = rawDefaultData.BackgroundLight, label = "<internal:light>" },
					side = Side.Light,
					theme = skinData.DefaultThemeData
				});
				skinData.BackgroundDatas.Add("\"DefaultConflict\"", new BackgroundData()
				{
					background = new Labelled<Sprite>() { value = rawDefaultData.BackgroundConflict, label = "<internal:conflict>" },
					side = Side.Conflict,
					theme = skinData.DefaultThemeData
				});
				skinData.BackgroundDatas.Add("\"DefaultColorless\"", new BackgroundData()
				{
					background = new Labelled<Sprite>() { value = rawDefaultData.BackgroundLight, label = "<internal:light>" },
					side = Side.Colorless,
					theme = skinData.DefaultThemeData
				});
			}
		}

		private void LoadBackgroundData(SkinDatas skinData, BackgroundDataSpec spec)
		{
			if (spec == null)
			{
				return;
			}
			if (spec.name == null)
			{
				Debug.LogWarning($"BackgroundDataSpec without a name will be ignored. spec:\n{JsonConvert.SerializeObject(spec)}");
				return;
			}
			if (skinData.BackgroundDatas.ContainsKey(spec.name))
			{
				Debug.LogWarning($"BackgroundDataSpec with a duplicated name will be ignored. spec:\n{JsonConvert.SerializeObject(spec)}");
				return;
			}
			string file = spec.file ?? spec.name;
			string path = Path.Combine(SkinFolderPath, "Playfield", "Background", file + ".jpg");
			Sprite background = LoadNormalSprite(path, externalSkinDataObjects);
			if (background == null)
			{
				Debug.LogWarning($"Background not found, ignored the BackgroundDataSpec. spec:\n{JsonConvert.SerializeObject(spec)}");
				return;
			}
			BackgroundData data = new BackgroundData();
			data.background = new Labelled<Sprite> { value = background, label = path };
			data.side = null;
			if (spec.side == "light")
			{
				data.side = Side.Light;
			}
			else if (spec.side == "conflict")
			{
				data.side = Side.Conflict;
			}
			else if (spec.side == "colorless")
			{
				data.side = Side.Colorless;
			}
			data.theme = null;
			if(spec.theme!=null){
				if (skinData.ThemeDatas.ContainsKey(spec.theme))
				{
					data.theme = spec.theme;
				}
			}
			skinData.BackgroundDatas.Add(spec.name, data);
			if (skinData.DefaultBackground == null)
			{
				skinData.DefaultBackground = spec.name;
			}
		}

		private void LoadNoteDatas(SkinDatas skinData, CollectionDataSpec<NoteSideDataSpec> spec)
		{
			skinData.NoteDatas = new Dictionary<string, WithSideData<NoteSideData>>();

			WithSideData<NoteSideData> internalDefaultNoteData = new WithSideData<NoteSideData>();
			internalDefaultNoteData.Light = new NoteSideData();
			internalDefaultNoteData.Conflict = new NoteSideData();
			internalDefaultNoteData.Light.TapNote = new Labelled<Sprite> { value = rawDefaultData.TapNoteLight, label = "<internal:light>" };
			internalDefaultNoteData.Conflict.TapNote = new Labelled<Sprite> { value = rawDefaultData.TapNoteConflict, label = "<internal:conflict>" };
			internalDefaultNoteData.Light.HoldNote = new Labelled<Sprite> { value = rawDefaultData.HoldNoteLight, label = "<internal:light>" };
			internalDefaultNoteData.Conflict.HoldNote = new Labelled<Sprite> { value = rawDefaultData.HoldNoteConflict, label = "<internal:conflict>" };
			internalDefaultNoteData.Light.HoldNoteHighlight = new Labelled<Sprite> { value = rawDefaultData.HoldNoteHighlightLight, label = "<internal:light>" };
			internalDefaultNoteData.Conflict.HoldNoteHighlight = new Labelled<Sprite> { value = rawDefaultData.HoldNoteHighlightConflict, label = "<internal:conflict>" };
			internalDefaultNoteData.Light.ArcTap = new Labelled<Texture2D> { value = rawDefaultData.ArcTapLight, label = "<internal:light>" };
			internalDefaultNoteData.Conflict.ArcTap = new Labelled<Texture2D> { value = rawDefaultData.ArcTapConflict, label = "<internal:conflict>" };
			internalDefaultNoteData.Light.SfxArcTapNote = new Labelled<Texture2D> { value = rawDefaultData.SfxArcTapNoteLight, label = "<internal:light>" };
			internalDefaultNoteData.Conflict.SfxArcTapNote = new Labelled<Texture2D> { value = rawDefaultData.SfxArcTapNoteConflict, label = "<internal:conflict>" };
			internalDefaultNoteData.Light.SfxArcTapCore = new Labelled<Texture2D> { value = rawDefaultData.SfxArcTapCoreLight, label = "<internal:light>" };
			internalDefaultNoteData.Conflict.SfxArcTapCore = new Labelled<Texture2D> { value = rawDefaultData.SfxArcTapCoreConflict, label = "<internal:conflict>" };
			internalDefaultNoteData.Light.ConnectionLineColor = rawDefaultData.ConnectionLineColorLight;
			internalDefaultNoteData.Conflict.ConnectionLineColor = rawDefaultData.ConnectionLineColorConflict;
			internalDefaultNoteData.Light.ArcRedLow = rawDefaultData.ArcRedLow;
			internalDefaultNoteData.Conflict.ArcRedLow = rawDefaultData.ArcRedLow;
			internalDefaultNoteData.Light.ArcBlueLow = rawDefaultData.ArcBlueLow;
			internalDefaultNoteData.Conflict.ArcBlueLow = rawDefaultData.ArcBlueLow;
			internalDefaultNoteData.Light.ArcGreenLow = rawDefaultData.ArcGreenLow;
			internalDefaultNoteData.Conflict.ArcGreenLow = rawDefaultData.ArcGreenLow;
			internalDefaultNoteData.Light.ArcRedHigh = rawDefaultData.ArcRedHigh;
			internalDefaultNoteData.Conflict.ArcRedHigh = rawDefaultData.ArcRedHigh;
			internalDefaultNoteData.Light.ArcBlueHigh = rawDefaultData.ArcBlueHigh;
			internalDefaultNoteData.Conflict.ArcBlueHigh = rawDefaultData.ArcBlueHigh;
			internalDefaultNoteData.Light.ArcGreenHigh = rawDefaultData.ArcGreenHigh;
			internalDefaultNoteData.Conflict.ArcGreenHigh = rawDefaultData.ArcGreenHigh;
			internalDefaultNoteData.Light.ArcVoid = rawDefaultData.ArcVoid;
			internalDefaultNoteData.Conflict.ArcVoid = rawDefaultData.ArcVoid;
			internalDefaultNoteData.Colorless = internalDefaultNoteData.Light;

			LoadCollectionData<NoteSideData, NoteSideDataSpec>(internalDefaultNoteData, skinData.NoteDatas, (name) => { skinData.DefaultNoteData = name; }, spec, LoadNoteSideData);
		}
		private void LoadThemeDatas(SkinDatas skinData, CollectionDataSpec<ThemeSideDataSpec> spec)
		{
			skinData.ThemeDatas = new Dictionary<string, WithSideData<ThemeSideData>>();

			WithSideData<ThemeSideData> internalDefaultThemeData = new WithSideData<ThemeSideData>();
			internalDefaultThemeData.Light = new ThemeSideData();
			internalDefaultThemeData.Conflict = new ThemeSideData();
			internalDefaultThemeData.Light.Track = new Labelled<Sprite> { value = rawDefaultData.TrackLight, label = "<internal:light>" };
			internalDefaultThemeData.Conflict.Track = new Labelled<Sprite> { value = rawDefaultData.TrackConflict, label = "<internal:conflict>" };
			internalDefaultThemeData.Light.TrackExtra = new Labelled<Sprite> { value = rawDefaultData.TrackExtraLight, label = "<internal:light>" };
			internalDefaultThemeData.Conflict.TrackExtra = new Labelled<Sprite> { value = rawDefaultData.TrackExtraConflict, label = "<internal:conflict>" };
			internalDefaultThemeData.Light.CriticalLine = new Labelled<Sprite> { value = rawDefaultData.CriticalLine, label = "<internal>" };
			internalDefaultThemeData.Conflict.CriticalLine = new Labelled<Sprite> { value = rawDefaultData.CriticalLine, label = "<internal>" };
			internalDefaultThemeData.Light.CriticalLineExtra = new Labelled<Sprite> { value = rawDefaultData.CriticalLineExtraLight, label = "<internal:light>" };
			internalDefaultThemeData.Conflict.CriticalLineExtra = new Labelled<Sprite> { value = rawDefaultData.CriticalLineExtraConflict, label = "<internal:conflict>" };
			internalDefaultThemeData.Light.ComboTextColor = rawDefaultData.ComboTextColorLight;
			internalDefaultThemeData.Conflict.ComboTextColor = rawDefaultData.ComboTextColorConflict;
			internalDefaultThemeData.Light.ParticleNote = new Labelled<Texture2D> { value = rawDefaultData.ParticleNote, label = "<internal>" };
			internalDefaultThemeData.Conflict.ParticleNote = new Labelled<Texture2D> { value = rawDefaultData.ParticleNote, label = "<internal>" };
			internalDefaultThemeData.Light.ParticleArcStartColor = rawDefaultData.ParticleArcStartColor;
			internalDefaultThemeData.Conflict.ParticleArcStartColor = rawDefaultData.ParticleArcStartColor;
			internalDefaultThemeData.Light.ParticleArcEndColor = rawDefaultData.ParticleArcEndColor;
			internalDefaultThemeData.Conflict.ParticleArcEndColor = rawDefaultData.ParticleArcEndColor;
			internalDefaultThemeData.Colorless = internalDefaultThemeData.Light;
			LoadCollectionData<ThemeSideData, ThemeSideDataSpec>(internalDefaultThemeData, skinData.ThemeDatas, (name) => { skinData.DefaultThemeData = name; }, spec, LoadThemeSideData);
		}
		private delegate void NameSetter(string name);
		private delegate T SideDataLoader<T, S>(S spec, T fallback);
		private void LoadCollectionData<T, S>(WithSideData<T> internalDefault, Dictionary<string, WithSideData<T>> datas, NameSetter defaultNameSetter, CollectionDataSpec<S> spec, SideDataLoader<T, S> sideDataLoader)
		{
			string internalDefaultName = "\"Default\"";
			if (spec == null)
			{
				defaultNameSetter(internalDefaultName);
				datas.Add(internalDefaultName, internalDefault);
				return;
			}
			string defaultName = spec.defaultName ?? internalDefaultName;
			defaultNameSetter(defaultName);
			WithSideDataSpec<S> defaultSpec = new WithSideDataSpec<S>();
			defaultSpec.name = defaultName;
			defaultSpec.light = spec.light;
			defaultSpec.conflict = spec.conflict;
			defaultSpec.colorless = spec.colorless;
			LoadWithSideData<T, S>(datas, defaultSpec, internalDefault, sideDataLoader);
			WithSideData<T> defaultData = datas[defaultName];
			if (spec.additional != null)
			{
				foreach (WithSideDataSpec<S> additionalSpec in spec.additional)
				{
					LoadWithSideData<T, S>(datas, additionalSpec, defaultData, sideDataLoader);
				}
			}
		}

		private void LoadWithSideData<T, S>(Dictionary<string, WithSideData<T>> datas, WithSideDataSpec<S> spec, WithSideData<T> fallback, SideDataLoader<T, S> sideDataLoader)
		{
			if (spec == null)
			{
				return;
			}
			if (spec.name == null)
			{
				Debug.LogWarning($"WithSideDataSpec without a name will be ignored. spec:\n{JsonConvert.SerializeObject(spec)}");
				return;
			}
			if (datas.ContainsKey(spec.name))
			{
				Debug.LogWarning($"WithSideDataSpec with a duplicated name will be ignored. spec:\n{JsonConvert.SerializeObject(spec)}");
				return;
			}
			WithSideData<T> sideData = new WithSideData<T>();
			sideData.Light = sideDataLoader(spec.light, fallback.Light);
			sideData.Conflict = sideDataLoader(spec.conflict, fallback.Conflict);
			sideData.Colorless = sideDataLoader(spec.colorless, fallback.Colorless);
			datas.Add(spec.name, sideData);
		}
		private NoteSideData LoadNoteSideData(NoteSideDataSpec spec, NoteSideData fallback)
		{
			if (spec == null)
			{
				return fallback;
			}
			NoteSideData noteSideData = new NoteSideData();

			noteSideData.TapNote = LoadLabelledWithFallback<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "Note", "TapNote"), spec.tapnote, ".png", (path) =>
			{
				return LoadPivotSprite(path, new Vector2(0.5f, 0.0f), externalSkinDataObjects);
			}, fallback.TapNote);
			noteSideData.HoldNote = LoadLabelledWithFallback<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "Note", "HoldNote"), spec.holdnote, ".png", (path) =>
			{
				return LoadPivotSprite(path, new Vector2(0.5f, 0.0f), externalSkinDataObjects);
			}, fallback.HoldNote);
			noteSideData.HoldNoteHighlight = LoadLabelledWithFallback<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "Note", "HoldNote"), spec.holdnoteHighlight, ".png", (path) =>
			{
				return LoadPivotSprite(path, new Vector2(0.5f, 0.0f), externalSkinDataObjects);
			}, fallback.HoldNoteHighlight);
			noteSideData.ArcTap = LoadLabelledWithFallback<Texture2D>(Path.Combine(SkinFolderPath, "Playfield", "Note", "ArcTap"), spec.arctap, ".png", (path) =>
			{
				return LoadTexture2D(path, externalSkinDataObjects);
			}, fallback.ArcTap);
			noteSideData.SfxArcTapNote = LoadLabelledWithFallback<Texture2D>(Path.Combine(SkinFolderPath, "Playfield", "Note", "SfxArcTap"), spec.sfxArctapNote, ".jpg", (path) =>
			{
				return LoadTexture2D(path, externalSkinDataObjects);
			}, fallback.SfxArcTapNote);
			noteSideData.SfxArcTapCore = LoadLabelledWithFallback<Texture2D>(Path.Combine(SkinFolderPath, "Playfield", "Note", "SfxArcTap"), spec.sfxArctapCore, ".jpg", (path) =>
			{
				return LoadTexture2D(path, externalSkinDataObjects);
			}, fallback.SfxArcTapCore);
			noteSideData.ConnectionLineColor = LoadColor(spec.connectionLineColor, fallback.ConnectionLineColor);
			noteSideData.ArcRedLow = LoadColor(spec.arcRedLow, fallback.ArcRedLow);
			noteSideData.ArcBlueLow = LoadColor(spec.arcBlueLow, fallback.ArcBlueLow);
			noteSideData.ArcGreenLow = LoadColor(spec.arcGreenLow, fallback.ArcGreenLow);
			noteSideData.ArcRedHigh = LoadColor(spec.arcRedHigh, fallback.ArcRedHigh);
			noteSideData.ArcBlueHigh = LoadColor(spec.arcBlueHigh, fallback.ArcBlueHigh);
			noteSideData.ArcGreenHigh = LoadColor(spec.arcGreenHigh, fallback.ArcGreenHigh);
			noteSideData.ArcVoid = LoadColor(spec.arcVoid, fallback.ArcVoid);

			return noteSideData;
		}
		private ThemeSideData LoadThemeSideData(ThemeSideDataSpec spec, ThemeSideData fallback)
		{

			if (spec == null)
			{
				return fallback;
			}
			ThemeSideData themeSideData = new ThemeSideData();

			themeSideData.Track = LoadLabelledWithFallback<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "Track", "TrackBase"), spec.track, ".png", (path) =>
			{
				return LoadPivotFullRectSprite(path, new Vector2(0.5f, 0.0f), externalSkinDataObjects);
			}, fallback.Track);
			themeSideData.TrackExtra = LoadLabelledWithFallback<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "Track", "TrackExtra"), spec.trackExtra, ".png", (path) =>
			{
				return LoadPivotFullRectSprite(path, new Vector2(0.5f, 0.0f), externalSkinDataObjects);
			}, fallback.TrackExtra);
			themeSideData.CriticalLine = LoadLabelledWithFallback<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "Track", "CriticalLine"), spec.trackCriticalLine, ".png", (path) =>
			{
				return LoadPivotSprite(path, new Vector2(0.5f, 0.0f), externalSkinDataObjects);
			}, fallback.CriticalLine);
			themeSideData.CriticalLineExtra = LoadLabelledWithFallback<Sprite>(Path.Combine(SkinFolderPath, "Playfield", "Track", "CriticalLine"), spec.trackCriticalLineExtra, ".png", (path) =>
			{
				return LoadPivotSprite(path, new Vector2(0.5f, 0.0f), externalSkinDataObjects);
			}, fallback.CriticalLineExtra);
			themeSideData.ComboTextColor = LoadColor(spec.comboTextColor, fallback.ComboTextColor);
			themeSideData.ParticleNote = LoadLabelledWithFallback<Texture2D>(Path.Combine(SkinFolderPath, "Playfield", "Particle", "ParticleNote"), spec.particleNote, ".png", (path) =>
			{
				return LoadTexture2D(path, externalSkinDataObjects);
			}, fallback.ParticleNote);
			themeSideData.ParticleArcStartColor = LoadColor(spec.particleArcStartColor, fallback.ParticleArcStartColor);
			themeSideData.ParticleArcEndColor = LoadColor(spec.particleArcEndColor, fallback.ParticleArcEndColor);
			return themeSideData;
		}

		private delegate T Loader<T>(string path);
		private Labelled<T> LoadLabelledWithFallback<T>(string folder, string name, string extension, Loader<T> loader, Labelled<T> fallback)
		{
			if (name == null)
			{
				return fallback;
			}
			else
			{
				string path = Path.Combine(folder, name + extension);
				T loaded = loader(path);
				if (loaded == null)
				{
					return fallback;
				}
				else
				{
					return new Labelled<T> { label = path, value = loaded };

				}
			}
		}
		private Labelled<T> LoadLabelled<T>(string path, Loader<T> loader, T defaultValue, string defaultLabel = "<internal>")
		{
			T loaded = loader(path);
			if (loaded == null)
			{
				return new Labelled<T> { label = defaultLabel, value = defaultValue };
			}
			else
			{
				return new Labelled<T> { label = path, value = loaded };
			}
		}
		private Color LoadColor(string color, Color fallback)
		{
			if (color == null)
			{
				return fallback;
			}
			else
			{
				Match match = Regex.Match(color, "^#([0-9a-fA-F]{2})([0-9a-fA-F]{2})([0-9a-fA-F]{2})$");
				if (match.Success)
				{
					byte red = Byte.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
					byte green = Byte.Parse(match.Groups[2].Value, NumberStyles.HexNumber);
					byte blue = Byte.Parse(match.Groups[3].Value, NumberStyles.HexNumber);
					return new Color32(red, green, blue, 255);
				}
				else
				{
					return fallback;
				}
			}
		}
		private Texture2D LoadTexture2D(string path, List<UnityEngine.Object> resourceList)
		{
			Texture2D texture = Loader.LoadTexture2D(path);
			if (texture != null)
			{
				resourceList.Add(texture);
			}
			return texture;
		}

		private Sprite LoadNormalSprite(string path, List<UnityEngine.Object> resourceList)
		{
			return LoadPivotSprite(path, new Vector2(0.5f, 0.5f), resourceList);
		}
		private Sprite LoadPivotSprite(string path, Vector2 pivot, List<UnityEngine.Object> resourceList)
		{
			Texture2D texture = LoadTexture2D(path, resourceList);
			if (texture == null)
			{
				return null;
			}
			Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot);
			resourceList.Add(sprite);
			sprite.name = path;
			return sprite;
		}
		private Sprite LoadPivotFullRectSprite(string path, Vector2 pivot, List<UnityEngine.Object> resourceList)
		{
			Texture2D texture = LoadTexture2D(path, resourceList);
			if (texture == null)
			{
				return null;
			}
			Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, 100, 1, SpriteMeshType.FullRect);
			resourceList.Add(sprite);
			sprite.name = path;
			return sprite;
		}
		private Sprite Load9SliceSprite(string path, Vector4 border, List<UnityEngine.Object> resourceList)
		{
			Texture2D texture = LoadTexture2D(path, resourceList);
			if (texture == null)
			{
				return null;
			}
			Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100, 1, SpriteMeshType.FullRect, border);
			resourceList.Add(sprite);
			sprite.name = path;
			return sprite;
		}

		private AudioClip LoadWavAudioClip(string path, List<UnityEngine.Object> resourceList)
		{
			AudioClip clip = Loader.LoadWavOrMp3AudioFile(path);
			if (clip != null)
			{
				resourceList.Add(clip);
				clip.name=path;
			}
			return clip;
		}

		private Mesh LoadObjMesh(string path, List<UnityEngine.Object> resourceList)
		{
			Mesh mesh = Loader.LoadObjMesh(path);
			if (mesh != null)
			{
				resourceList.Add(mesh);
				mesh.name=path;
			}
			return mesh;
		}
		#endregion
	}
}
