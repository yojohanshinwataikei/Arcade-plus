using UnityEngine;
using UnityEngine.Events;

namespace Arcade.Compose
{
	public class EndOfFrame : MonoBehaviour
	{
		public static EndOfFrame Instance { get; private set; }
		private void Awake()
		{
			Instance = this;
		}

		[HideInInspector]
		public UnityEvent Listeners = new UnityEvent();

		private void Update()
		{
			Listeners.Invoke();
			Listeners.RemoveAllListeners();
		}
	}
}
