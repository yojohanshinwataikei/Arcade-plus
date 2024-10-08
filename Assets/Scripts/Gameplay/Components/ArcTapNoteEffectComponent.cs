using System.Collections;
using Arcade.Util.Pooling;
using UnityEngine;
using UnityEngine.VFX;

namespace Arcade.Gameplay
{
	public class ArcTapNoteEffectComponent : MonoBehaviour, IPoolable
	{
		public bool Available { get; private set; } = true;
		public VisualEffect Effect;
		public Texture2D Texture;

		public void PlayAt(Vector3 pos)
		{
			Available = false;
			transform.position = pos;
			Effect.enabled = true;
			Effect.SetTexture("Texture", Texture);
			Effect.Play();
			StartCoroutine(WaitForEnd());
		}
		IEnumerator WaitForEnd()
		{
			yield return new WaitForSeconds(0.5f);
			Effect.Reinit();
			Effect.Stop();
			Effect.enabled = false;
			yield return null;
			Available = true;
		}
		public void SetTexture(Texture2D texture)
		{
			Effect.SetTexture("Texture", texture);
			Texture = texture;
		}
	}
}
