using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Deckbuilding
{
    public class Message : MonoBehaviour
    {
        public TextMeshProUGUI Text;
        private Vector3 _worldPosition;

        public void Set(Vector3 worldPosition, string message)
        {
            Text.text = message;
            _worldPosition = worldPosition;
        }

        public void OnEnable()
        {
            StartCoroutine(FadeInFadeOut());
        }

        protected void Update()
        {
            var screenPoint = Camera.main.WorldToScreenPoint(_worldPosition);
            transform.position = screenPoint;
        }

        private IEnumerator FadeInFadeOut()
        {
            var color = Text.color;
            color.a = 0;
            Text.color = color;
            while (color.a < 1)
            {
                color.a += Time.deltaTime;
                Text.color = color;
                yield return null;
            }
            
            yield return new WaitForSeconds(4);
            
            while (color.a > 0)
            {
                color.a -= Time.deltaTime;
                Text.color = color;
                yield return null;
            }
            
            Destroy(gameObject);
        }
    }
}