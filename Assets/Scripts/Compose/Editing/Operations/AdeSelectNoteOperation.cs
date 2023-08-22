using System.Collections;
using System.Collections.Generic;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using Arcade.Util.UnityExtension;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcade.Compose.Operation
{
    public class AdeSelectNoteOperation : AdeOperation
    {
        public static AdeSelectNoteOperation Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private float? rangeSelectPosition = null;
        public float? RangeSelectPosition { get => rangeSelectPosition; }

        public override AdeOperationResult TryExecuteOperation()
        {
            if (!AdeInputManager.Instance.Inputs.RangeSelection.IsPressed())
            {
                rangeSelectPosition = null;
            }

            if (!AdeGameplayContentInputHandler.InputActive)
            {
                return false;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                //range selection shortcut
                if (AdeInputManager.Instance.Inputs.RangeSelection.IsPressed())
                {
                    if (rangeSelectPosition == null)
                    {
                        rangeSelectPosition = AdeCursorManager.Instance.AttachedTiming;
                    }
                    else
                    {
                        if (!AdeInputManager.Instance.Inputs.MultipleSelection.IsPressed())
                        {
                            AdeSelectionManager.Instance.DeselectAllNotes();
                        }
                        AdeSelectionManager.Instance.RangeSelectNote(rangeSelectPosition.Value, AdeCursorManager.Instance.AttachedTiming);
                        rangeSelectPosition = null;
                        return true;
                    }
                }

                Ray ray = AdeCursorManager.Instance.GameplayCamera.MousePositionToRay();

                RaycastHit[] hits = Physics.RaycastAll(ray, 120, 1 << 9);
                ArcNote n = null;
                float distance = float.MaxValue;
                foreach (var h in hits)
                {
                    ArcNote t = ArcGameplayManager.Instance.FindNoteByRaycastHit(h);
                    if (t != null)
                    {
                        if (h.distance < distance)
                        {
                            distance = h.distance;
                            n = t;
                        }
                    }
                }
                if (n != null)
                {
                    if (!AdeInputManager.Instance.Inputs.MultipleSelection.IsPressed())
                    {
                        AdeSelectionManager.Instance.DeselectAllNotes();
                        AdeSelectionManager.Instance.SelectNote(n);
                        return true;
                    }
                    else
                    {
                        if (AdeSelectionManager.Instance.SelectedNotes.Contains(n)) AdeSelectionManager.Instance.DeselectNote(n);
                        else AdeSelectionManager.Instance.SelectNote(n);
                        return true;
                    }
                }
                else
                {
                    if (!AdeInputManager.Instance.Inputs.MultipleSelection.IsPressed())
                    {
                        AdeSelectionManager.Instance.DeselectAllNotes();
                        return true;
                    }
                }
            }
            return false;
        }
        public override void Reset()
        {
            rangeSelectPosition = null;
        }
    }
}
