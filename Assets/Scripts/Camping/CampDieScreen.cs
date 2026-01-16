using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Common
{
    public class CampDieScreen : SingletonMonoBehavior<CampDieScreen>
    {
         public GameObject DeathScreen;
        
        private bool _show;
        
        public void Update()
        {
            if (PartyHealth.Instance == null)
                return;

            DeathScreen.SetActive(PartyHealth.Instance.Value <= 0);
        }

        public void RestartLevel()
        {
            SceneManager.LoadScene(0);
        }
    }
}