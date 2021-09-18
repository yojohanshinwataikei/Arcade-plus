using System.Collections;
using Arcade.Gameplay;
using UnityEngine;

namespace Arcade.Compose
{
	public class AdeRangeSelect : MonoBehaviour
	{
		private Coroutine co;

		private void Update()
		{
			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
			{
				RangeSelect();
			}
		}

		public void RangeSelect()
		{
			if (!ArcGameplayManager.Instance.IsLoaded)
			{
				AdeToast.Instance.Show("请先加载谱面");
				return;
			}
			if (co != null)
			{
				StopCoroutine(co);
				AdeCursorManager.Instance.Mode = CursorMode.Idle;
			}
			co = StartCoroutine(RangeSelectCoroutine());
		}

		private IEnumerator RangeSelectCoroutine()
		{
			AdeCursorManager.Instance.Mode = CursorMode.Horizontal;
			AdeCursorManager.Instance.EnableVerticalPanel = true;
			AdeToast.Instance.Show("选择起始时间");
			do
			{
				yield return null;
			}
			while (!Input.GetMouseButtonDown(0) || !AdeCursorManager.Instance.IsHorizontalHit);
			int start = (int)AdeCursorManager.Instance.AttachedTiming;
			AdeToast.Instance.Show("选择结束时间");
			do
			{
				yield return null;
			}
			while (!Input.GetMouseButtonDown(0) || !AdeCursorManager.Instance.IsHorizontalHit);
			int num = (int)AdeCursorManager.Instance.AttachedTiming;
			AdeCursorManager.Instance.Mode = CursorMode.Idle;
			AdeCursorManager.Instance.EnableVerticalPanel = false;
			AdeCursorManager.Instance.SelectNotesInRange(start, num);
			AdeToast.Instance.Show(string.Format("段落选中 [{0}, {1}]", start, num));
			co = null;
		}
	}
}
