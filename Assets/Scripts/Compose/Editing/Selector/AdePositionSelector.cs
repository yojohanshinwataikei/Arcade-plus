using System;
using UnityEngine;
using Arcade.Gameplay.Chart;
using Arcade.Gameplay;
using UnityEngine.InputSystem;

namespace Arcade.Compose.Editing
{
	public class AdePositionSelector : MonoBehaviour
    {
        public static AdePositionSelector Instance { get; private set; }
        private void Awake()
        {
            Instance = this;
        }

        public bool Enable { get; private set; }

        private ArcArc note;
        private Action<Vector2> currentSetter;
        private CursorMode lastMode = CursorMode.Horizontal;

        private void Update()
        {
            if (!Enable) return;
            UpdatePosition();
        }
        public void ModifyNote(ArcArc note, Action<Vector2> setter)
        {
            Enable = true;
            this.note = note;
            currentSetter = setter;
            lastMode = AdeCursorManager.Instance.Mode;
            AdeCursorManager.Instance.Mode = CursorMode.Vertical;
            AdeCursorManager.Instance.EnableVerticalPanel = true;
        }
        private void UpdatePosition()
        {
            if (!AdeCursorManager.Instance.IsVerticalHit) return;
            Vector3 pos = AdeCursorManager.Instance.AttachedVerticalPoint;
            currentSetter?.Invoke(new Vector2(ArcAlgorithm.WorldXToArc(pos.x), ArcAlgorithm.WorldYToArc(pos.y)));
            if (Mouse.current.leftButton.wasPressedThisFrame) EndModify();
        }
        public void EndModify()
        {
            EndOfFrame.Instance.Listeners.AddListener(() =>
            {
                Enable = false;
                note = null;
                currentSetter = null;
                AdeCursorManager.Instance.Mode = lastMode;
                AdeCursorManager.Instance.EnableVerticalPanel = false;
            });
        }
    }
}
