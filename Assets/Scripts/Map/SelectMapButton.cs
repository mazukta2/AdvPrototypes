using System;
using Cinemachine;
using Common;
using Deckbuilding;
using UnityEngine;
using UnityEngine.UI;

namespace Map
{
    public class SelectMapButton : SingletonMonoBehavior<SelectMapButton>
    {
        public Button Button;
        public CinemachineVirtualCamera PartyCamera;
        public CinemachineVirtualCamera MapCamera;
        public bool IsMap => MapCamera.gameObject.activeSelf;

        public void Start()
        {
            Button.onClick.AddListener(OnClick);
        }
        
        void OnClick()
        {
            MapCamera.gameObject.SetActive(!MapCamera.gameObject.activeSelf);
        }

        public void Update()
        {
            Button.interactable = !ChangeQuestScreen.Instance.IsOpened() && !ChangePartyScreen.Instance.IsOpened();
        }
    }
}