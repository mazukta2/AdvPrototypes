using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class CollisionMousePointer : MonoBehaviour
    {
        [Header("Setup")]
        public ReadOnlyBind<Vector2> mouseScreenPosition;
        public ReadOnlyBind<bool> mouseButtonDown;
        public ReadOnlyBind<Camera> sourceCamera;
        public ReadOnlyBind<LayerMask> collisionLayer = ((LayerMask)(-1)).Bind();
        public ReadOnlyBind<bool> clickOnlyOnRaycastHit = true.Bind();

        [Header("Output")] 
        public Bind<bool> click;
        public Bind<float> clickDuration;
        public Bind<Vector3> pointerPosition;

        private float _clickDuration;
        
        public bool IsClicked { get; private set; }
        
        void Update()
        {
            var cam = sourceCamera.Value;
            if (!cam)
            {
                cam = Camera.main;
            }
            
            // Get the point where the mouse is pointing
            var ray = cam.ScreenPointToRay(mouseScreenPosition.Value);
            if (Physics.Raycast(ray, out var hit, 100, collisionLayer.Value, QueryTriggerInteraction.Ignore))
            {
                pointerPosition.Value = hit.point;
            }
            else if(clickOnlyOnRaycastHit)
            {
                return;
            }
            
            if (mouseButtonDown)
            {
                _clickDuration += Time.deltaTime;
                clickDuration.Value = _clickDuration;
                click.Value = IsClicked = true;
            }
            else
            {
                _clickDuration = 0;
                clickDuration.Value = 0;
                click.Value = IsClicked = false;
            }
        }
    } 
}
