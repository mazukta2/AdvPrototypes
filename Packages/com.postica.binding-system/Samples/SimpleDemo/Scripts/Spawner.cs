using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject sample;
    public float timeToLive = 5f;
    
    public void Spawn()
    {
        var spawned = Instantiate(sample, transform.position, Quaternion.identity);
        spawned.SetActive(true);
        Destroy(spawned, timeToLive);
    }
}
