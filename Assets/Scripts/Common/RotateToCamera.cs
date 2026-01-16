using System;
using UnityEngine;

namespace Common
{
    public class RotateToCamera : MonoBehaviour
    {
        private Camera _camera;

        protected void OnEnable()
        {
            _camera = Camera.main;
        }

        protected void Update()
        {
            transform.LookAt(_camera.transform.position);
        }
    }
}