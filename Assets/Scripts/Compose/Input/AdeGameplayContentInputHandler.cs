using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arcade.Compose
{
    public class AdeGameplayContentInputHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static AdeGameplayContentInputHandler Instance { get; private set; }

        private bool active = false;

        public static bool InputActive { get => Instance != null ? Instance.active : false; }
        private void Awake()
        {
            Instance = this;
        }
        public void OnPointerEnter(PointerEventData data)
        {
            Debug.Log("enter");
            active = true;
        }
        public void OnPointerExit(PointerEventData data)
        {
            Debug.Log("exit");
            active = false;
        }
    }
}