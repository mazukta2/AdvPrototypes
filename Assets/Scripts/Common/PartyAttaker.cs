using UnityEngine;

namespace Common
{
    public class PartyAttaker : SingletonMonoBehavior<PartyAttaker>
    {
        public float Radius = 10f;
        public float Cooldown = 1f;
        public GameObject BulletPrefab;
        public float CurrentCooldown = 0f;
        public float Damage = 10;

        public void Update()
        {
            foreach (var enemy in Enemy.List)
            {
                if (Vector3.Distance(enemy.transform.position, transform.position) < Radius)
                {
                    if (CurrentCooldown <= 0f)
                    {
                        CurrentCooldown = Cooldown;
                        var bulletGo = GameObject.Instantiate(BulletPrefab, transform.position, Quaternion.identity);
                        bulletGo.GetComponent<Bullet>().SetTarget(enemy.gameObject, Damage);
                    }
                    else
                    {
                        CurrentCooldown -= Time.deltaTime;
                    }
                }
            }
        }
        
        
        public void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(this.transform.position, Radius);
        }
    }
}