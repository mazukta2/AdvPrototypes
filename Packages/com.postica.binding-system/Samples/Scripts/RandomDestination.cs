using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class RandomDestination : MonoBehaviour
    {
        [Range(0f, 20f)]
        public float maxNextPointDistance = 5;
        [Range(0f, 20f)]
        public float maxSpeed = 5f;

        private Vector3 _nextDestination;

        private void Start()
        {
            _nextDestination = transform.position;
        }

        void Update()
        {
            var currentPosition = transform.position;
            if(Vector3.Distance(_nextDestination, currentPosition) < 0.01f)
            {
                var delta = new Vector3(
                        Random.Range(-maxNextPointDistance, maxNextPointDistance),
                        Random.Range(-maxNextPointDistance, maxNextPointDistance),
                        Random.Range(-maxNextPointDistance, maxNextPointDistance)
                    );
                _nextDestination = currentPosition + delta;
            }

            var speed = Random.Range(0, maxSpeed);
            transform.position = Vector3.MoveTowards(currentPosition, _nextDestination, speed * Time.deltaTime);
        }
    }
}
