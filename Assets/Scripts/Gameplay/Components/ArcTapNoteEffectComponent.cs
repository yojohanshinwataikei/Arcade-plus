using System.Collections;
using UnityEngine;

namespace Arcade.Gameplay
{
	public class ArcTapNoteEffectComponent : MonoBehaviour
	{
		public bool Available { get; set; } = true;
		public ParticleSystem Effect;

		public void PlayAt(Vector2 pos)
		{
			Available = false;
			transform.position = pos;
			Effect.Play();
			StartCoroutine(WaitForEnd());
		}
		IEnumerator WaitForEnd()
		{
			yield return new WaitForSeconds(0.5f);
			Effect.Stop();
			Effect.Clear();
			yield return null;
			Available = true;
		}
	}
}