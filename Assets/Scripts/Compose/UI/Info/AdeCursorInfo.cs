using System.Collections;
using System.Collections.Generic;
using Arcade.Compose.Editing;
using Arcade.Compose.Operation;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using UnityEngine;
using UnityEngine.UI;

namespace Arcade.Compose
{
	public class AdeCursorInfo : MonoBehaviour
	{
        private bool enableInfo;

		public Text InfoText;
		public GameObject InfoGameObject;
		public bool EnableInfo
		{
			get
			{
				return enableInfo;
			}
			set
			{
				if (enableInfo != value)
				{
					InfoGameObject.SetActive(value);
					enableInfo = value;
				}
			}
		}
		// Start is called before the first frame update
		void Start()
		{

		}

		void Update()
		{
            AdeCursorManager cursor=AdeCursorManager.Instance;
			EnableInfo = cursor.WallEnabled || cursor.TrackEnabled;
			string content = string.Empty;
			if (!EnableInfo) return;
			content += $"音乐时间: {cursor.AttachedTiming + ArcAudioManager.Instance.AudioOffset}\n";
			content += $"谱面时间: {cursor.AttachedTiming}";
			if (cursor.WallEnabled)
			{
				Vector3 pos = cursor.AttachedWallPoint;
				content += $"\n坐标: ({ArcAlgorithm.WorldXToArc(pos.x).ToString("f2")},{ArcAlgorithm.WorldYToArc(pos.y).ToString("f2")})";
			}
			if (AdeClickToCreate.Instance.Enable && AdeClickToCreate.Instance.Mode != ClickToCreateMode.Idle)
			{
				content += $"\n点立得: {AdeClickToCreate.Instance.Mode.ToString()}";
				if (AdeClickToCreate.Instance.Mode == ClickToCreateMode.Arc)
				{
					content += $"\n{AdeClickToCreate.Instance.CurrentArcColor}/{AdeClickToCreate.Instance.CurrentArcIsVoid}/{AdeClickToCreate.Instance.CurrentArcType}";
				}
			}
			if (AdeSelectNoteOperation.Instance.RangeSelectPosition != null)
			{
				content += $"\n段落选择起点: {AdeSelectNoteOperation.Instance.RangeSelectPosition}";
			}
			if (AdeSelectionManager.Instance.SelectedNotes.Count == 1 && AdeSelectionManager.Instance.SelectedNotes[0] is ArcArc)
			{
				ArcArc arc = AdeSelectionManager.Instance.SelectedNotes[0] as ArcArc;
				float p = (cursor.AttachedTiming - arc.Timing) / (arc.EndTiming - arc.Timing);
				if (p >= 0 && p <= 1)
				{
					float x = ArcAlgorithm.X(arc.XStart, arc.XEnd, p, arc.LineType);
					float y = ArcAlgorithm.Y(arc.YStart, arc.YEnd, p, arc.LineType);
					content += $"\nArc: {(p * 100).ToString("f2")}%, {x.ToString("f2")}, {y.ToString("f2")}";
				}
			}
			InfoText.text = content;
		}
	}
}