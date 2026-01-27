using System;
using Camping;
using UnityEngine;
using UnityEngine.UI;
using Input = UnityEngine.Windows.Input;

namespace Common
{
    public class Enemy : ListMonoBehavior<Enemy>
    {
        public float Health = 50f;
        public float MaxHealth = 50f;
        public float Radius = 10f;
        public float Cooldown = 1f;
        public GameObject BulletPrefab;
        public float CurrentCooldown = 0f;
        public float Damage = 10;
        public Image HealthBar;
        public int Gold = 2;

        public void Update()
        {
            if (Vector3.Distance(PartyHealth.Instance.transform.position, transform.position) < Radius)
            {
                if (CurrentCooldown <= 0f)
                {
                    CurrentCooldown = Cooldown;
                    var bulletGo = GameObject.Instantiate(BulletPrefab, transform.position, Quaternion.identity);
                    bulletGo.GetComponent<Bullet>().SetTarget(PartyHealth.Instance.gameObject, Damage);
                }
                else
                {
                    CurrentCooldown -= Time.deltaTime;
                }
            }
        }
        
        
        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(this.transform.position, Radius);
        }

        public void Hit(float damage)
        {
            Health -= damage;
            HealthBar.fillAmount = Health / MaxHealth;
            if (Health <= 0)
            {
                PartyGold.Add(Gold);
                Destroy(this.gameObject);
            }
        }
    }
}