using System;
using UnityEngine;

namespace Deckbuilding
{
    public class Init : MonoBehaviour
    {
        public GameSettings Settings;
        public void Awake()
        {
            GameSettings.Instance = Settings;
        }
    }
}