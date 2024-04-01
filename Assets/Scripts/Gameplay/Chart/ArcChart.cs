using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Arcade.Aff;
using Arcade.Util.Misc;
using UnityEngine;

namespace Arcade.Gameplay.Chart
{
	public enum ChartSortMode
	{
		Timing,
		Type
	}
	public class ArcChart
	{
		public int AudioOffset;
		public float TimingPointDensityFactor;
		public Dictionary<string, string> AdditionalMetadata = new Dictionary<string, string>();
		public List<ArcTap> Taps = new List<ArcTap>();
		public List<ArcHold> Holds = new List<ArcHold>();
		public List<ArcTiming> Timings = new List<ArcTiming>();
		public List<ArcArc> Arcs = new List<ArcArc>();
		public List<ArcCamera> Cameras = new List<ArcCamera>();
		public List<ArcSceneControl> SceneControl = new List<ArcSceneControl>();
		public List<ArcTimingGroup> TimingGroups = new List<ArcTimingGroup>();
		public int LastEventTiming = 0;

		public ArcChart(RawAffChart raw)
		{
			AudioOffset = raw.AudioOffset;
			TimingPointDensityFactor = raw.TimingPointDensityFactor;
			AdditionalMetadata = raw.additionalMetadata;
			foreach (var item in raw.items)
			{
				addRawItem(item, null);
			}
		}

		void addRawItem(IRawAffItem item, ArcTimingGroup timingGroup)
		{
			if (item is RawAffTiming)
			{
				if (timingGroup == null)
				{
					Timings.Add(new ArcTiming(item as RawAffTiming));
				}
				else
				{
					timingGroup.Timings.Add(new ArcTiming(item as RawAffTiming));
				}
			}
			else if (item is RawAffTap)
			{
				Taps.Add(new ArcTap(item as RawAffTap, timingGroup));
			}
			else if (item is RawAffHold)
			{
				Holds.Add(new ArcHold(item as RawAffHold, timingGroup));
			}
			else if (item is RawAffArc)
			{
				Arcs.Add(new ArcArc(item as RawAffArc, timingGroup));
			}
			else if (item is RawAffCamera)
			{
				Cameras.Add(new ArcCamera(item as RawAffCamera, timingGroup));
			}
			else if (item is RawAffSceneControl)
			{
				SceneControl.Add(new ArcSceneControl(item as RawAffSceneControl, timingGroup));
			}
			else if (item is RawAffTimingGroup)
			{
				ArcTimingGroup arcTimingGroup = new ArcTimingGroup()
				{
					Id = TimingGroups.Count + 1,
				};
				arcTimingGroup.ApplyAttributes((item as RawAffTimingGroup).Attributes);
				TimingGroups.Add(arcTimingGroup);
				foreach (var nestedItem in (item as RawAffTimingGroup).Items)
				{
					addRawItem(nestedItem, arcTimingGroup);
				}
			}
		}

		public void Serialize(Stream stream, ChartSortMode mode = ChartSortMode.Timing)
		{
			List<ArcEvent> events = new List<ArcEvent>();
			events.AddRange(Timings);
			events.AddRange(Cameras);
			events.AddRange(SceneControl);
			events.AddRange(Taps);
			events.AddRange(Holds);
			events.AddRange(Arcs);

			List<ArcEvent> mainEvents = new List<ArcEvent>();
			Dictionary<ArcTimingGroup, List<ArcEvent>> timingGroupEvents = new Dictionary<ArcTimingGroup, List<ArcEvent>>();
			foreach (var timingGroup in TimingGroups)
			{
				timingGroupEvents.Add(timingGroup, new List<ArcEvent>());
				timingGroupEvents[timingGroup].AddRange(timingGroup.Timings);
			}

			foreach (var arcEvent in events)
			{
				if (arcEvent is IHasTimingGroup)
				{
					ArcTimingGroup timingGroup = (arcEvent as IHasTimingGroup).TimingGroup;
					if (timingGroup != null)
					{
						timingGroupEvents[timingGroup].Add(arcEvent);
						continue;
					}
				}
				mainEvents.Add(arcEvent);
			}

			mainEvents = SortedEvent(mainEvents, mode);
			foreach (var timingGroup in TimingGroups)
			{
				timingGroupEvents[timingGroup] = SortedEvent(timingGroupEvents[timingGroup], mode);
			}

			RawAffChart raw = new RawAffChart();
			raw.AudioOffset = ArcAudioManager.Instance.AudioOffset;
			raw.TimingPointDensityFactor = TimingPointDensityFactor;
			raw.additionalMetadata = AdditionalMetadata;
			foreach (var e in mainEvents)
			{
				if (e is IIntoRawItem)
				{
					var rawItem = (e as IIntoRawItem).IntoRawItem();
					raw.items.Add(rawItem);
				}
			}
			foreach (var timingGroup in TimingGroups)
			{
				var timingGroupItem = new RawAffTimingGroup()
				{
					Items = new List<IRawAffNestableItem>(),
					Attributes = timingGroup.Attributes
				};
				foreach (var e in timingGroupEvents[timingGroup])
				{
					if (e is IIntoRawItem)
					{
						var rawItem = (e as IIntoRawItem).IntoRawItem() as IRawAffNestableItem;
						timingGroupItem.Items.Add(rawItem);
					}

				}
				raw.items.Add(timingGroupItem);
			}
			ArcaeaFileFormat.DumpToStream(stream, raw);
		}

		static List<ArcEvent> SortedEvent(List<ArcEvent> events, ChartSortMode mode)
		{
			switch (mode)
			{
				case ChartSortMode.Timing:
					return events.OrderBy(arcEvent => arcEvent.Timing)
						.ThenBy(arcEvent => (arcEvent is ArcTiming ? 1 : arcEvent is ArcTap ? 2 : arcEvent is ArcHold ? 3 : arcEvent is ArcArc ? 4 : arcEvent is ArcCamera ? 5 : arcEvent is ArcSceneControl ? 6 : 7))
						.ToList();
				case ChartSortMode.Type:
					return events.OrderBy(arcEvent => (arcEvent is ArcTiming ? 1 : arcEvent is ArcTap ? 2 : arcEvent is ArcHold ? 3 : arcEvent is ArcArc ? 4 : arcEvent is ArcCamera ? 5 : arcEvent is ArcSceneControl ? 6 : 7))
						.ThenBy(arcEvent => arcEvent.Timing)
						.ToList();
			}
			return events;
		}
	}
	public interface ISelectable
	{
		bool Selected { get; set; }
	}
	public interface IHasTimingGroup
	{
		ArcTimingGroup TimingGroup { get; }
	}
	public static class ArcChartExtension
	{
		public static bool NoInput(this IHasTimingGroup hasTimingGroup)
		{
			return hasTimingGroup.TimingGroup?.NoInput ?? false;
		}
		public static bool GroupHide(this IHasTimingGroup hasTimingGroup)
		{
			return hasTimingGroup.TimingGroup?.GroupHide ?? false;
		}
		public static Vector3 FallDirection(this IHasTimingGroup hasTimingGroup)
		{
			int angleX = hasTimingGroup.TimingGroup?.AngleX ?? 0;
			int angleY = hasTimingGroup.TimingGroup?.AngleY ?? 0;
			Vector3 direction = new Vector3(0, 0, 1);
			direction = Quaternion.AngleAxis((float)(angleX) / 10, new Vector3(1, 0, 0)) * direction;
			direction = Quaternion.AngleAxis((float)(angleY) / 10, new Vector3(0, -1, 0)) * direction;
			return direction;
		}
		public static bool FadingHolds(this IHasTimingGroup hasTimingGroup)
		{
			return hasTimingGroup.TimingGroup?.FadingHolds ?? false;
		}
	}
	public interface ISetableTimingGroup
	{
		ArcTimingGroup TimingGroup { set; }
	}
	public enum ArcLineType
	{
		B,
		S,
		Si,
		So,
		SiSi,
		SiSo,
		SoSi,
		SoSo
	}
	public enum CameraEaseType
	{
		L,
		Qi,
		Qo,
		Reset,
		S
	}
	public enum SceneControlType
	{
		TrackHide,
		TrackShow,
		HideGroup,
		EnwidenCamera,
		EnwidenLanes,
		Unknown,
	}
	public abstract class ArcEvent
	{
		public int Timing;
		public virtual void Assign(ArcEvent newValues)
		{
			Timing = newValues.Timing;
		}
		public abstract ArcEvent Clone();
	}
	public class ArcSceneControl : ArcEvent, IIntoRawItem, IHasTimingGroup, ISetableTimingGroup
	{

		public ArcSceneControl()
		{
		}
		public ArcSceneControl(RawAffSceneControl rawAffSceneControl, ArcTimingGroup timingGroup)
		{
			Timing = rawAffSceneControl.Timing;
			RawType = rawAffSceneControl.Type;
			RawParams = rawAffSceneControl.Params;
			if (RawType == "trackhide" && RawParams.Count == 0)
			{
				Type = SceneControlType.TrackHide;
			}
			else if (RawType == "trackshow" && RawParams.Count == 0)
			{
				Type = SceneControlType.TrackShow;
			}
			else if (RawType == "hidegroup" && RawParams.Count == 2)
			{
				if ((RawParams[0] is RawAffFloat) && (RawParams[1] is RawAffInt))
				{
					Type = SceneControlType.HideGroup;
					Enable = (RawParams[1] as RawAffInt).data > 0;
				}
			}
			else if (RawType == "enwidencamera" && RawParams.Count == 2)
			{
				if ((RawParams[0] is RawAffFloat) && (RawParams[1] is RawAffInt))
				{
					Type = SceneControlType.EnwidenCamera;
					Duration = (RawParams[0] as RawAffFloat).data;
					Enable = (RawParams[1] as RawAffInt).data > 0;
				}
			}
			else if (RawType == "enwidenlanes" && RawParams.Count == 2)
			{
				if ((RawParams[0] is RawAffFloat) && (RawParams[1] is RawAffInt))
				{
					Type = SceneControlType.EnwidenLanes;
					Duration = (RawParams[0] as RawAffFloat).data;
					Enable = (RawParams[1] as RawAffInt).data > 0;
				}
			}
			TimingGroup = timingGroup;
		}
		public IRawAffItem IntoRawItem()
		{
			var item = new RawAffSceneControl()
			{
				Timing = Timing,
				Type = "unknown",
				Params = new List<IRawAffValue>(),
			};
			if (Type == SceneControlType.TrackHide)
			{
				item.Type = "trackhide";
			}
			else if (Type == SceneControlType.TrackShow)
			{
				item.Type = "trackshow";
			}
			else if (Type == SceneControlType.HideGroup)
			{
				item.Type = "hidegroup";
				item.Params = new List<IRawAffValue>{
					new RawAffFloat{data=0},
					new RawAffInt{data=Enable?1:0},
				};
			}
			else if (Type == SceneControlType.EnwidenCamera)
			{
				item.Type = "enwidencamera";
				item.Params = new List<IRawAffValue>{
					new RawAffFloat{data=Duration},
					new RawAffInt{data=Enable?1:0},
				};
			}
			else if (Type == SceneControlType.EnwidenLanes)
			{
				item.Type = "enwidenlanes";
				item.Params = new List<IRawAffValue>{
					new RawAffFloat{data=Duration},
					new RawAffInt{data=Enable?1:0},
				};
			}
			else
			{
				item.Type = RawType;
				item.Params = RawParams;
			}
			return item;
		}
		public SceneControlType Type = SceneControlType.Unknown;
		public bool Enable;
		public float Duration;
		public ArcTimingGroup TimingGroup { get; set; }
		public string RawType;
		public List<IRawAffValue> RawParams;

		public override ArcEvent Clone()
		{
			throw new NotImplementedException();
		}
	}
	public abstract class ArcNote : ArcEvent, ISelectable
	{
		// This indicated if the sound effect or tap particle of the note is played
		public bool Judged;
		public float Position;

		protected GameObject instance;
		public virtual GameObject Instance
		{
			get
			{
				return instance;
			}
			set
			{
				if (instance != null) Destroy();
				instance = value;
				transform = instance.transform;
				spriteRenderer = instance.GetComponent<SpriteRenderer>();
				meshRenderer = instance.GetComponent<MeshRenderer>();
			}
		}

		protected bool enable;
		public virtual bool Enable
		{
			get
			{
				return enable;
			}
			set
			{
				if (enable != value)
				{
					enable = value;
					if (spriteRenderer != null) spriteRenderer.enabled = value;
					if (meshRenderer != null) meshRenderer.enabled = value;
				}
			}
		}
		public abstract bool Selected { get; set; }

		public Transform transform;
		public SpriteRenderer spriteRenderer;
		public MeshRenderer meshRenderer;

		public abstract void Instantiate();
		public virtual void Destroy()
		{
			if (instance != null) UnityEngine.Object.Destroy(instance);
			instance = null;
			transform = null;
			spriteRenderer = null;
			meshRenderer = null;
		}
	}
	public abstract class ArcLongNote : ArcNote
	{
		public int EndTiming;
		public override void Assign(ArcEvent newValues)
		{
			base.Assign(newValues);
			ArcLongNote n = newValues as ArcLongNote;
			EndTiming = n.EndTiming;
		}

		// This indicate if long note particle present
		public bool Judging;

		// This two should be replaced by judged
		public bool ShouldPlayAudio;
		public bool AudioPlayed;

		// The times where combo increase
		public List<int> JudgeTimings = new List<int>();

	}
	public class ArcTap : ArcNote, IIntoRawItem, IHasTimingGroup, ISetableTimingGroup
	{
		public int Track;
		public ArcTimingGroup TimingGroup { get; set; }

		private bool selected;
		private float currentAlpha;
		private int alphaShaderId;
		private MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
		public Dictionary<ArcArcTap, LineRenderer> ConnectionLines = new Dictionary<ArcArcTap, LineRenderer>();
		private BoxCollider boxCollider;

		public ArcTap()
		{
		}
		public ArcTap(RawAffTap rawAffTap, ArcTimingGroup timingGroup)
		{
			Timing = rawAffTap.Timing;
			Track = rawAffTap.Track;
			TimingGroup = timingGroup;
		}

		public IRawAffItem IntoRawItem()
		{
			return new RawAffTap()
			{
				Timing = Timing,
				Track = Track,
			};
		}

		public float Alpha
		{
			get
			{
				return currentAlpha;
			}
			set
			{
				if (currentAlpha != value)
				{
					spriteRenderer.GetPropertyBlock(propertyBlock);
					propertyBlock.SetFloat(alphaShaderId, value);
					spriteRenderer.SetPropertyBlock(propertyBlock);
					foreach (var l in ConnectionLines.Values) l.startColor = l.endColor = new Color(l.endColor.r, l.endColor.g, l.endColor.b, value * 0.8f);
					currentAlpha = value;
				}
			}
		}
		public override bool Enable
		{
			get
			{
				return base.Enable;
			}
			set
			{
				if (enable != value)
				{
					base.Enable = value;
					boxCollider.enabled = value;
					foreach (var l in ConnectionLines.Values) l.enabled = value;
				}
			}
		}
		public override bool Selected
		{
			get
			{
				return selected;
			}
			set
			{
				if (selected != value)
				{
					spriteRenderer.renderingLayerMask = MaskUtil.SetMask(spriteRenderer.renderingLayerMask, ArcGameplayManager.Instance.SelectionLayerMask, value);
					selected = value;
				}
			}
		}
		public override void Destroy()
		{
			base.Destroy();
			boxCollider = null;
			foreach (var l in ConnectionLines.Values) if (l.gameObject != null) UnityEngine.Object.Destroy(l.gameObject);
			ConnectionLines.Clear();
		}
		public override ArcEvent Clone()
		{
			return new ArcTap()
			{
				Timing = Timing,
				Track = Track,
				TimingGroup = TimingGroup
			};
		}
		public override void Assign(ArcEvent newValues)
		{
			base.Assign(newValues);
			ArcTap n = newValues as ArcTap;
			Track = n.Track;
			TimingGroup = n.TimingGroup;
		}
		public override GameObject Instance
		{
			get
			{
				return base.Instance;
			}
			set
			{
				if (instance != null) Destroy();
				base.Instance = value;
				boxCollider = instance.GetComponent<BoxCollider>();
				alphaShaderId = Shader.PropertyToID("_Alpha");
				Enable = false;
			}
		}
		public override void Instantiate()
		{
			Instance = UnityEngine.Object.Instantiate(ArcTapNoteManager.Instance.TapNotePrefab, ArcTapNoteManager.Instance.NoteLayer);
		}
		public void SetupArcTapConnection()
		{
			foreach (var l in ConnectionLines.Values) UnityEngine.Object.Destroy(l.gameObject);
			ConnectionLines.Clear();
			foreach (var arc in ArcArcManager.Instance.Arcs)
			{
				if (arc.ArcTaps == null) continue;
				if (arc.NoInput()) continue;
				foreach (var arctap in arc.ArcTaps)
				{
					if (Mathf.Abs(arctap.Timing - Timing) <= 1)
					{
						arctap.SetupArcTapConnection();
					}
				}
			}
		}
	}
	public class ArcHold : ArcLongNote, IIntoRawItem, IHasTimingGroup, ISetableTimingGroup
	{
		public int Track;
		public ArcTimingGroup TimingGroup { get; set; }

		private MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

		public void ReloadSkin()
		{
			defaultSprite = ArcHoldNoteManager.Instance.DefaultSprite;
			highlightSprite = ArcHoldNoteManager.Instance.HighlightSprite;
			spriteRenderer.sprite = highlighted ? highlightSprite : defaultSprite;
		}
		public override GameObject Instance
		{
			get
			{
				return base.Instance;
			}
			set
			{
				if (instance != null) Destroy();
				base.Instance = value;
				fromShaderId = Shader.PropertyToID("_From");
				toShaderId = Shader.PropertyToID("_To");
				alphaShaderId = Shader.PropertyToID("_Alpha");
				defaultSprite = ArcHoldNoteManager.Instance.DefaultSprite;
				highlightSprite = ArcHoldNoteManager.Instance.HighlightSprite;
				boxCollider = instance.GetComponent<BoxCollider>();
				CalculateJudgeTimings();
				Enable = false;
				ReloadSkin();
			}
		}
		public override void Destroy()
		{
			base.Destroy();
			boxCollider = null;
			JudgeTimings.Clear();
		}
		public override ArcEvent Clone()
		{
			return new ArcHold()
			{
				Timing = Timing,
				EndTiming = EndTiming,
				Track = Track,
				TimingGroup = TimingGroup,
			};
		}
		public override void Assign(ArcEvent newValues)
		{
			base.Assign(newValues);
			ArcHold n = newValues as ArcHold;
			Track = n.Track;
			TimingGroup = n.TimingGroup;
			CalculateJudgeTimings();
		}
		public override void Instantiate()
		{
			Instance = UnityEngine.Object.Instantiate(ArcHoldNoteManager.Instance.HoldNotePrefab, ArcHoldNoteManager.Instance.NoteLayer);
		}

		public void CalculateJudgeTimings()
		{
			JudgeTimings.Clear();
			int u = 0;
			double bpm = ArcTimingManager.Instance.CalculateBpmByTiming(Timing, TimingGroup);
			bpm = Math.Abs(bpm);
			if (bpm == 0) return;
			double interval = 60000f / bpm / (bpm >= 255 ? 1 : 2) / ArcGameplayManager.Instance.TimingPointDensityFactor;
			int total = (int)((EndTiming - Timing) / interval);
			if ((u ^ 1) >= total)
			{
				JudgeTimings.Add((int)(Timing + (EndTiming - Timing) * 0.5f));
				return;
			}
			int n = u ^ 1;
			int t = Timing;
			while (true)
			{
				t = (int)(Timing + n * interval);
				if (t < EndTiming)
				{
					JudgeTimings.Add(t);
				}
				if (total == ++n) break;
			}
		}
		public override bool Enable
		{
			get
			{
				return base.Enable;
			}
			set
			{
				if (enable != value)
				{
					base.Enable = value;
					boxCollider.enabled = value;
				}
			}
		}
		public float From
		{
			get
			{
				return currentFrom;
			}
			set
			{
				if (currentFrom != value)
				{
					currentFrom = value;
					spriteRenderer.GetPropertyBlock(propertyBlock);
					propertyBlock.SetFloat(fromShaderId, value);
					spriteRenderer.SetPropertyBlock(propertyBlock);
				}
			}
		}
		public float To
		{
			get
			{
				return currentTo;
			}
			set
			{
				if (currentTo != value)
				{
					currentTo = value;
					spriteRenderer.GetPropertyBlock(propertyBlock);
					propertyBlock.SetFloat(toShaderId, value);
					spriteRenderer.SetPropertyBlock(propertyBlock);
				}
			}
		}
		public float Alpha
		{
			get
			{
				return currentAlpha;
			}
			set
			{
				if (currentAlpha != value)
				{
					spriteRenderer.GetPropertyBlock(propertyBlock);
					propertyBlock.SetFloat(alphaShaderId, value);
					spriteRenderer.SetPropertyBlock(propertyBlock);
					currentAlpha = value;
				}
			}
		}
		public override bool Selected
		{
			get
			{
				return selected;
			}
			set
			{
				if (selected != value)
				{
					spriteRenderer.renderingLayerMask = MaskUtil.SetMask(spriteRenderer.renderingLayerMask, ArcGameplayManager.Instance.SelectionLayerMask, value);
					selected = value;
				}
			}
		}
		public bool Highlight
		{
			get
			{
				return highlighted;
			}
			set
			{
				if (highlighted != value)
				{
					highlighted = value;
					spriteRenderer.sprite = value ? highlightSprite : defaultSprite;
				}
			}
		}

		public int FlashCount;
		public BoxCollider boxCollider;

		private bool selected;
		private bool highlighted;
		private int fromShaderId = 0, toShaderId = 0, alphaShaderId = 0;
		private float currentFrom = 0, currentTo = 1, currentAlpha = 1;
		private Sprite defaultSprite, highlightSprite;

		public ArcHold()
		{
		}
		public ArcHold(RawAffHold rawAffHold, ArcTimingGroup timingGroup)
		{
			Timing = rawAffHold.Timing;
			EndTiming = rawAffHold.EndTiming;
			Track = rawAffHold.Track;
			TimingGroup = timingGroup;
		}
		public IRawAffItem IntoRawItem()
		{
			return new RawAffHold()
			{
				Timing = Timing,
				EndTiming = EndTiming,
				Track = Track,
			};
		}
	}
	public class ArcTiming : ArcEvent, IIntoRawItem
	{
		public float Bpm;
		public float BeatsPerLine;

		public ArcTiming()
		{
		}

		public ArcTiming(RawAffTiming rawAffTiming)
		{
			Timing = rawAffTiming.Timing;
			Bpm = rawAffTiming.Bpm;
			BeatsPerLine = rawAffTiming.BeatsPerLine;
		}
		public IRawAffItem IntoRawItem()
		{
			return new RawAffTiming()
			{
				Timing = Timing,
				Bpm = Bpm,
				BeatsPerLine = BeatsPerLine,
			};
		}

		public override ArcEvent Clone()
		{
			return new ArcTiming()
			{
				Timing = Timing,
				Bpm = Bpm,
				BeatsPerLine = BeatsPerLine
			};
		}
		public override void Assign(ArcEvent newValues)
		{
			base.Assign(newValues);
			ArcTiming n = newValues as ArcTiming;
			Bpm = n.Bpm;
			BeatsPerLine = n.BeatsPerLine;
		}
	}
	public class ArcArcTap : ArcNote, IHasTimingGroup
	{
		public ArcArc Arc;
		public ArcTimingGroup TimingGroup { get => Arc.TimingGroup; }

		public bool IsConvertedVariousSizedArctap = false;

		public Transform Model;
		public Transform Shadow;
		public MeshRenderer ModelRenderer;
		public SpriteRenderer ShadowRenderer;
		public MeshCollider ArcTapCollider;
		private MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

		public override bool Enable
		{
			get
			{
				return base.Enable;
			}
			set
			{
				if (enable != value)
				{
					base.Enable = value;
					ModelRenderer.enabled = value;
					ShadowRenderer.enabled = value;
					ArcTapCollider.enabled = value;
				}
			}
		}
		public override GameObject Instance
		{
			get
			{
				return base.Instance;
			}
			set
			{
				if (instance != null) Destroy();
				base.Instance = value;
				ModelRenderer = instance.GetComponentInChildren<MeshRenderer>();
				Model = ModelRenderer.transform;
				ShadowRenderer = instance.GetComponentInChildren<SpriteRenderer>();
				Shadow = ShadowRenderer.transform;
				ModelRenderer.sortingLayerName = "Arc";
				ModelRenderer.sortingOrder = 4;
				alphaShaderId = Shader.PropertyToID("_Alpha");
				ArcTapCollider = instance.GetComponentInChildren<MeshCollider>();
				Enable = false;
			}
		}
		public override void Destroy()
		{
			RemoveArcTapConnection();
			base.Destroy();
			ArcTapCollider = null;
			ModelRenderer = null;
			ShadowRenderer = null;
		}
		public override ArcEvent Clone()
		{
			return new ArcArcTap()
			{
				Timing = Timing,
			};
		}
		public override void Assign(ArcEvent newValues)
		{
			base.Assign(newValues);
		}
		public void Instantiate(ArcArc arc)
		{
			Arc = arc;
			Instance = UnityEngine.Object.Instantiate(
				arc.IsSfx ? ArcArcManager.Instance.SfxArcTapPrefab : ArcArcManager.Instance.ArcTapPrefab, arc.transform
			);

			UpdatePosition();
			UpdateScale();
			SetupArcTapConnection();
		}

		public void UpdatePosition()
		{
			ArcTimingManager timingManager = ArcTimingManager.Instance;
			int offset = ArcAudioManager.Instance.AudioOffset;
			float t = 1f * (Timing - Arc.Timing) / (Arc.EndTiming - Arc.Timing);
			Vector3 parentLocalPosition = new Vector3(0, 0, -timingManager.CalculatePositionByTiming(Arc.Timing + offset, Arc.TimingGroup) / 1000f);
			Vector3 baseLocalPosition = new Vector3();
			if (IsConvertedVariousSizedArctap)
			{
				baseLocalPosition = new Vector3(ArcAlgorithm.ArcXToWorld((Arc.XStart + Arc.XEnd) / 2f), ArcAlgorithm.ArcYToWorld(Arc.YStart) - 0.5f, 0);
			}
			else
			{
				baseLocalPosition = new Vector3(ArcAlgorithm.ArcXToWorld(ArcAlgorithm.X(Arc.XStart, Arc.XEnd, t, Arc.LineType)),
										ArcAlgorithm.ArcYToWorld(ArcAlgorithm.Y(Arc.YStart, Arc.YEnd, t, Arc.LineType)) - 0.5f, 0);
			}
			Vector3 offsetLocalPosition = -timingManager.CalculatePositionByTiming(Timing + offset, Arc.TimingGroup) / 1000f * this.FallDirection();
			Vector3 additionalLocalPosition = new Vector3(0, 0, -0.6f);
			LocalPosition = baseLocalPosition + offsetLocalPosition - parentLocalPosition + additionalLocalPosition;
		}

		public void UpdateScale()
		{
			if (IsConvertedVariousSizedArctap)
			{
				LocalScale = Mathf.Abs(ArcAlgorithm.ArcXToWorld(Arc.XEnd) - ArcAlgorithm.ArcXToWorld(Arc.XStart));
			}
			else
			{
				LocalScale = 4.25f;
			}
		}

		/// <summary>
		/// Please use the overload method.
		/// </summary>
		public override void Instantiate()
		{
			throw new NotImplementedException();
		}

		public void SetupArcTapConnection()
		{
			if (Arc == null || Arc.NoInput() || (Arc.EndTiming - Arc.Timing) == 0) return;
			List<ArcTap> taps = ArcTapNoteManager.Instance.Taps;
			ArcTap[] sameTimeTapNotes = taps.Where((s) => Mathf.Abs(s.Timing - Timing) <= 1).ToArray();
			foreach (ArcTap t in sameTimeTapNotes)
			{
				LineRenderer l = UnityEngine.Object.Instantiate(ArcArcManager.Instance.ConnectionPrefab, t.transform).GetComponent<LineRenderer>();
				float p = 1f * (Timing - Arc.Timing) / (Arc.EndTiming - Arc.Timing);
				Vector3 pos = new Vector3(ArcAlgorithm.ArcXToWorld(ArcAlgorithm.X(Arc.XStart, Arc.XEnd, p, Arc.LineType)),
											 ArcAlgorithm.ArcYToWorld(ArcAlgorithm.Y(Arc.YStart, Arc.YEnd, p, Arc.LineType)) - 0.5f)
											 - new Vector3(ArcArcManager.Instance.Lanes[t.Track], 0);
				l.SetPosition(1, new Vector3(pos.x, 0, pos.y));
				l.startColor = l.endColor = ArcArcManager.Instance.ConnectionColor;
				l.startColor = l.endColor = new Color(l.endColor.r, l.endColor.g, l.endColor.b, t.Alpha * 0.8f);
				l.enabled = t.Enable;
				l.transform.localPosition = new Vector3();

				if (t.ConnectionLines.ContainsKey(this))
				{
					UnityEngine.Object.Destroy(t.ConnectionLines[this].gameObject);
					t.ConnectionLines.Remove(this);
				}

				t.ConnectionLines.Add(this, l);
			}
		}
		public void RemoveArcTapConnection()
		{
			List<ArcTap> taps = ArcTapNoteManager.Instance.Taps;
			ArcTap[] sameTimeTapNotes = taps.Where((s) => Mathf.Abs(s.Timing - Timing) <= 1).ToArray();
			foreach (ArcTap t in sameTimeTapNotes)
			{
				if (t.ConnectionLines.ContainsKey(this))
				{
					UnityEngine.Object.Destroy(t.ConnectionLines[this].gameObject);
					t.ConnectionLines.Remove(this);
				}
			}
		}
		public void Relocate()
		{
			ArcTimingManager timingManager = ArcTimingManager.Instance;
			int offset = ArcAudioManager.Instance.AudioOffset;
			float t = 1f * (Timing - Arc.Timing) / (Arc.EndTiming - Arc.Timing);
			LocalPosition = new Vector3(ArcAlgorithm.ArcXToWorld(ArcAlgorithm.X(Arc.XStart, Arc.XEnd, t, Arc.LineType)),
									  ArcAlgorithm.ArcYToWorld(ArcAlgorithm.Y(Arc.YStart, Arc.YEnd, t, Arc.LineType)) - 0.5f,
									  -timingManager.CalculatePositionByTimingAndStart(Arc.Timing + offset, Timing + offset, Arc.TimingGroup) / 1000f - 0.6f);
			SetupArcTapConnection();
		}

		public bool IsMyself(GameObject gameObject)
		{
			return Model.gameObject.Equals(gameObject);
		}

		public float Alpha
		{
			get
			{
				return currentAlpha;
			}
			set
			{
				if (currentAlpha != value)
				{
					currentAlpha = value;
					ModelRenderer.GetPropertyBlock(propertyBlock);
					propertyBlock.SetFloat(alphaShaderId, value);
					ModelRenderer.SetPropertyBlock(propertyBlock);
					ShadowRenderer.color = new Color(0.49f, 0.49f, 0.49f, 0.7843f * value);
				}
			}
		}
		public Vector3 LocalPosition
		{
			get
			{
				return Model.localPosition;
			}
			set
			{
				Model.localPosition = value;
				Vector3 p = value;
				p.y = 0;
				Shadow.localPosition = p;
			}
		}
		public float LocalScale
		{
			get
			{
				return Model.localScale.x;
			}
			set
			{
				Model.localScale = new Vector3(value, Model.localScale.y, Model.localScale.z);
				Shadow.localScale = new Vector3(value / 4.25f, 1f, 1f);
			}
		}
		public override bool Selected
		{
			get
			{
				return selected;
			}

			set
			{
				if (selected != value)
				{
					ModelRenderer.renderingLayerMask = MaskUtil.SetMask(ModelRenderer.renderingLayerMask, ArcGameplayManager.Instance.SelectionLayerMask, value);
					selected = value;
				}
			}
		}

		private bool selected;
		private float currentAlpha = 1f;
		private int alphaShaderId = 0;

		public ArcArcTap()
		{
		}
		public ArcArcTap(RawAffArctap arctap)
		{
			Timing = arctap.Timing;
		}
	}
	public class ArcArc : ArcLongNote, IIntoRawItem, IHasTimingGroup, ISetableTimingGroup
	{

		public ArcTimingGroup TimingGroup { get; set; }
		public float XStart;
		public float XEnd;
		public ArcLineType LineType;
		public float YStart;
		public float YEnd;
		public int Color;
		public string Effect = "none";
		public bool IsVoid;
		public List<ArcArcTap> ArcTaps = new List<ArcArcTap>();

		public ArcArcTap ConvertedVariousSizedArctap = null;

		public bool IsVariousSizedArctap
		{
			get
			{
				if (Color == 3 && Timing == EndTiming && Math.Abs(YStart - YEnd) < float.Epsilon && !IsVoid)
				{
					return true;
				}
				return false;
			}
		}

		public override ArcEvent Clone()
		{
			ArcArc arc = new ArcArc()
			{
				Timing = Timing,
				EndTiming = EndTiming,
				XStart = XStart,
				XEnd = XEnd,
				LineType = LineType,
				YStart = YStart,
				YEnd = YEnd,
				Color = Color,
				Effect = Effect,
				IsVoid = IsVoid,
				TimingGroup = TimingGroup,
			};
			foreach (var arctap in ArcTaps) arc.ArcTaps.Add(arctap.Clone() as ArcArcTap);
			return arc;
		}
		public override void Assign(ArcEvent newValues)
		{
			base.Assign(newValues);
			ArcArc n = newValues as ArcArc;
			XStart = n.XStart;
			XEnd = n.XEnd;
			LineType = n.LineType;
			YStart = n.YStart;
			YEnd = n.YEnd;
			Color = n.Color;
			Effect = n.Effect;
			IsVoid = n.IsVoid;
			TimingGroup = n.TimingGroup;
		}

		public override bool Enable
		{
			get
			{
				return base.Enable;
			}
			set
			{
				if (enable != value)
				{
					base.Enable = value;
					arcRenderer.Enable = value;
					if (!value) foreach (ArcArcTap t in ArcTaps) if (t.Instance != null) t.Enable = value;
				}
			}
		}
		public override bool Selected
		{
			get
			{
				return arcRenderer.Selected;
			}
			set
			{
				arcRenderer.Selected = value;
				if (ConvertedVariousSizedArctap != null)
				{
					ConvertedVariousSizedArctap.Selected = value;
				}
			}
		}
		public override GameObject Instance
		{
			get
			{
				return base.Instance;
			}
			set
			{
				base.Instance = value;
				arcRenderer = instance.GetComponent<ArcArcRenderer>();
				arcRenderer.Arc = this;
				Enable = false;
			}
		}

		public bool IsSfx
		{
			get
			{
				return Effect.EndsWith("_wav");
			}
		}
		public override void Destroy()
		{
			base.Destroy();
			arcRenderer = null;
			ArcGroup.Clear();
			DestroyArcTaps();
		}
		public override void Instantiate()
		{
			Instance = UnityEngine.Object.Instantiate(ArcArcManager.Instance.ArcNotePrefab, ArcArcManager.Instance.ArcLayer);
			InstantiateArcTaps();
		}

		public void CalculateJudgeTimings()
		{
			JudgeTimings.Clear();
			if (IsVoid) return;
			if (EndTiming == Timing) return;
			int u = RenderHead ? 0 : 1;
			double bpm = ArcTimingManager.Instance.CalculateBpmByTiming(Timing, TimingGroup);
			bpm = Math.Abs(bpm);
			if (bpm == 0)
			{
				JudgeTimings.Add(Timing);
				return;
			};
			double interval = 60000f / bpm / (bpm >= 255 ? 1 : 2) / ArcGameplayManager.Instance.TimingPointDensityFactor;
			int total = (int)((EndTiming - Timing) / interval);
			if ((u ^ 1) >= total)
			{
				JudgeTimings.Add((int)(Timing + (EndTiming - Timing) * 0.5f));
				return;
			}
			int n = u ^ 1;
			int t = Timing;
			while (true)
			{
				t = (int)(Timing + n * interval);
				if (t < EndTiming)
				{
					JudgeTimings.Add(t);
				}
				if (total == ++n) break;
			}
		}
		public void Rebuild()
		{
			arcRenderer.Build();
			DestroyArcTaps();
			InstantiateArcTaps();
		}
		public void InstantiateArcTaps()
		{
			foreach (var tap in ArcTaps)
			{
				tap.Instantiate(this);
			}
			if (IsVariousSizedArctap)
			{
				ConvertedVariousSizedArctap = new ArcArcTap
				{
					Timing = Timing,
					Arc = this,
					IsConvertedVariousSizedArctap = true,
				};
				ConvertedVariousSizedArctap.Instantiate(this);
				ConvertedVariousSizedArctap.Selected=Selected;
			}
		}
		public void DestroyArcTaps()
		{
			foreach (var at in ArcTaps)
			{
				at.Destroy();
			}
			ConvertedVariousSizedArctap?.Destroy();
			ConvertedVariousSizedArctap = null;
		}
		public void AddArcTap(ArcArcTap arctap)
		{
			if (arctap.Timing > EndTiming || arctap.Timing < Timing)
			{
				throw new ArgumentOutOfRangeException("ArcTap 时间不在 Arc 范围内");
			}
			ArcTimingManager timingManager = ArcTimingManager.Instance;
			int offset = ArcAudioManager.Instance.AudioOffset;
			arctap.Instantiate(this);
			float t = 1f * (arctap.Timing - Timing) / (EndTiming - Timing);
			arctap.LocalPosition = new Vector3(ArcAlgorithm.ArcXToWorld(ArcAlgorithm.X(XStart, XEnd, t, LineType)),
									  ArcAlgorithm.ArcYToWorld(ArcAlgorithm.Y(YStart, YEnd, t, LineType)) - 0.5f,
									  -timingManager.CalculatePositionByTimingAndStart(Timing + offset, arctap.Timing + offset, TimingGroup) / 1000f - 0.6f);
			ArcTaps.Add(arctap);
		}
		public void RemoveArcTap(ArcArcTap arctap)
		{
			arctap.Destroy();
			ArcTaps.Remove(arctap);
		}

		public bool IsHitMyself(RaycastHit h)
		{
			return arcRenderer.IsHitMyself(h);
		}

		public int FlashCount;
		public float EndPosition;
		public bool Flag;
		public bool RenderHead;
		public List<ArcArc> ArcGroup;
		public ArcArcRenderer arcRenderer;

		public ArcArc()
		{
		}
		public ArcArc(RawAffArc rawAffArc, ArcTimingGroup timingGroup)
		{
			Timing = rawAffArc.Timing;
			EndTiming = rawAffArc.EndTiming;
			XStart = rawAffArc.XStart;
			XEnd = rawAffArc.XEnd;
			LineType = rawAffArc.LineType;
			YStart = rawAffArc.YStart;
			YEnd = rawAffArc.YEnd;
			Color = rawAffArc.Color;
			Effect = rawAffArc.Effect;
			IsVoid = rawAffArc.IsVoid;
			if (rawAffArc.ArcTaps.Count > 0)
			{
				IsVoid = true;
				foreach (var arctap in rawAffArc.ArcTaps)
				{
					ArcTaps.Add(new ArcArcTap(arctap));
				}
			}
			TimingGroup = timingGroup;
		}
		public IRawAffItem IntoRawItem()
		{
			return new RawAffArc()
			{
				Timing = Timing,
				EndTiming = EndTiming,
				XStart = XStart,
				XEnd = XEnd,
				LineType = LineType,
				YStart = YStart,
				YEnd = YEnd,
				Color = Color,
				Effect = Effect,
				IsVoid = IsVoid,
				ArcTaps = ArcTaps.Select((arctap) => new RawAffArctap() { Timing = arctap.Timing, }).ToList(),
			};
		}

	}
	public class ArcCamera : ArcEvent, IIntoRawItem, IHasTimingGroup, ISetableTimingGroup
	{

		public ArcTimingGroup TimingGroup { get; set; }
		public Vector3 Move, Rotate;
		public CameraEaseType CameraType;
		public int Duration;

		public float Percent;

		public ArcCamera()
		{
		}

		public ArcCamera(RawAffCamera rawAffCamera, ArcTimingGroup timingGroup)
		{
			Timing = rawAffCamera.Timing;
			Move = new Vector3(rawAffCamera.MoveX, rawAffCamera.MoveY, rawAffCamera.MoveZ);
			Rotate = new Vector3(rawAffCamera.RotateX, rawAffCamera.RotateY, rawAffCamera.RotateZ);
			CameraType = rawAffCamera.CameraType;
			Duration = rawAffCamera.Duration;
			TimingGroup = timingGroup;
		}
		public IRawAffItem IntoRawItem()
		{
			return new RawAffCamera()
			{
				Timing = Timing,
				MoveX = Move.x,
				MoveY = Move.y,
				MoveZ = Move.z,
				RotateX = Rotate.x,
				RotateY = Rotate.y,
				RotateZ = Rotate.z,
				CameraType = CameraType,
				Duration = Duration,
			};
		}

		public override ArcEvent Clone()
		{
			return new ArcCamera()
			{
				Timing = Timing,
				Duration = Duration,
				CameraType = CameraType,
				Move = Move,
				Percent = Percent,
				Rotate = Rotate
			};
		}
		public override void Assign(ArcEvent newValues)
		{
			base.Assign(newValues);
			ArcCamera n = newValues as ArcCamera;
			Move = n.Move;
			Rotate = n.Rotate;
			CameraType = n.CameraType;
			Duration = n.Duration;
		}

		public void Update(int Timing)
		{
			if (Timing > this.Timing + Duration)
			{
				Percent = 1;
				return;
			}
			else if (Timing <= this.Timing)
			{
				Percent = 0;
				return;
			}
			Percent = Mathf.Clamp((1f * Timing - this.Timing) / Duration, 0, 1);
			switch (CameraType)
			{
				case CameraEaseType.Qi:
					Percent = ArcAlgorithm.Qi(Percent);
					break;
				case CameraEaseType.Qo:
					Percent = ArcAlgorithm.Qo(Percent);
					break;
				case CameraEaseType.S:
					Percent = ArcAlgorithm.S(0, 1, Percent);
					break;
			}
		}
	}

	public class ArcTimingGroup
	{
		public int Id;
		public List<ArcTiming> Timings = new List<ArcTiming>();
		public string Attributes = "";
		public bool NoInput = false;
		public bool FadingHolds = false;
		public int AngleX = 0;
		public int AngleY = 0;
		public bool GroupHide = false;
		public float earliestRenderTime = 0;
		public float latestRenderTime = 0;

		public void ApplyAttributes(string attributes)
		{
			Attributes = attributes;
			var attributeList = attributes.Split('_');
			foreach (var attribute in attributeList)
			{
				if (attribute == "noinput")
				{
					NoInput = true;
					continue;
				}
				if (attribute == "fadingholds")
				{
					FadingHolds = true;
					continue;
				}
				Match matchAngleX = Regex.Match(attribute, "^anglex([0-9]+)$");
				if (matchAngleX.Success)
				{
					bool result = int.TryParse(matchAngleX.Groups[1].Value, out AngleX);
					if (!result)
					{
						Debug.LogWarning($"Timinggroup attribute applying failed:{attribute}");
					}
					continue;
				}
				Match matchAngleY = Regex.Match(attribute, "^angley([0-9]+)$");
				if (matchAngleY.Success)
				{
					bool result = int.TryParse(matchAngleY.Groups[1].Value, out AngleY);
					if (!result)
					{
						Debug.LogWarning($"Timinggroup attribute applying failed:{attribute}");
					}
					continue;
				}
			}
		}
	}
}
