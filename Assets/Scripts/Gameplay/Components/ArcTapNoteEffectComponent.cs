using System.Collections;
using Arcade.Util.Pooling;
using UnityEngine;
using UnityEngine.VFX;

namespace Arcade.Gameplay
{
	public class ArcTapNoteEffectComponent : MonoBehaviour,IPoolable
	{
		public bool Available { get; private set; } = true;
		[UnityEngine.Serialization.FormerlySerializedAs("Effect")]
		public ParticleSystem LegacyEffect;
		public VisualEffect Effect;

		public void PlayAt(Vector2 pos)
		{
			Available = false;
			transform.position = pos;
			Effect.enabled=true;
			Effect.Play();
			StartCoroutine(WaitForEnd());
		}
		IEnumerator WaitForEnd()
		{
			yield return new WaitForSeconds(0.5f);
			Effect.Reinit();
			Effect.Stop();
			Effect.enabled=false;
			yield return null;
			Available = true;
		}
		public void SetTexture(Texture2D texture){
			Effect.SetTexture("Texture",texture);
		}
	}
}
