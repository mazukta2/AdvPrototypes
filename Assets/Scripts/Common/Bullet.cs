using System;
using UnityEngine;

namespace Common
{
    public class Bullet : MonoBehaviour
    {

        public float Damage = 1;
        public float Speed;
        public GameObject Target;
        public void SetTarget(GameObject instanceGameObject, float damage)
        {
            Target = instanceGameObject;
            Damage = damage;
        }

        protected void Update()
        {
            if (Target != null)
            {
                transform.position = Vector3.MoveTowards(transform.position, Target.transform.position, Time.deltaTime * Speed);

                if (Vector3.Distance(transform.position, Target.transform.position) < 0.1f)
                {
                    var enemy = Target.gameObject.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.Hit(Damage);
                        Destroy(gameObject);
                        return;
                    }

                    var player = Target.gameObject.GetComponent<PartyHealth>();
                    if (player != null)
                    {
                        player.Hit(Damage);
                        Destroy(gameObject);
                        return;
                    }
                }
            }
        }
    }
}