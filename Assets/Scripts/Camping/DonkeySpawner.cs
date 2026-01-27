using Common;
using UnityEngine;

namespace Camping
{
    public class DonkeySpawner : SingletonMonoBehavior<DonkeySpawner>
    {
        public GameObject Donkey;
        
        public void SpawnDonkey()
        {
            GameObject.Instantiate(Donkey, PartyMovement.Instance.transform.position + new Vector3(1, 0f, 1) * Random.Range(1, 2), Quaternion.identity);
        }
    }
}