using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class WASDInputProvider : MonoBehaviour
    {
        [Header("Moves")]
        [SerializeField] private ReadOnlyBind<bool> _forward;
        [SerializeField] private ReadOnlyBind<bool> _backward;
        [SerializeField] private ReadOnlyBind<bool> _left;
        [SerializeField] private ReadOnlyBind<bool> _right;
        [SerializeField] private ReadOnlyBind<bool> _jump;

        [Header("Configuration")] 
        [SerializeField] private ReadOnlyBind<float> _verticalAxisAcceleration = 5f.Bind();
        [SerializeField] private ReadOnlyBind<float> _horizontalAxisAcceleration = 5f.Bind();
        [SerializeField] private ReadOnlyBind<float> _jumpAcceleration = 5f.Bind();
        [SerializeField] private ReadOnlyBind<bool> _jumpOnRelease = true.Bind();
        
        [Header("Output")]
        public Bind<Vector2> movement;
        public Bind<float> verticalAxis;
        public Bind<float> horizontalAxis;
        public Bind<float> jumpPowerAccumulator;
        public Bind<float> jumpValue;

        private float _axisV;
        private float _axisH;
        private float _jumpAccumulator;
        private bool _isJumpAccumulating;
        private float _resetJumpValueTime;
        private float _resetJumpValueDuration = 0.3f;
        
        private void Update()
        {
            ApplyAxisValues();

            ApplyJumpValues();
        }

        private void ApplyJumpValues()
        {
            if(Time.time > _resetJumpValueTime)
            {
                jumpValue.Value = 0;
            }
            
            if (_isJumpAccumulating)
            {
                if (_jump)
                {
                    if (jumpPowerAccumulator.CanRead)
                    {
                        _jumpAccumulator = jumpPowerAccumulator.Value;
                    }
                    _jumpAccumulator += Mathf.Clamp01(_jumpAcceleration.Value * Time.deltaTime);
                    jumpPowerAccumulator.Value = _jumpAccumulator;
                    return;
                }
                
                _isJumpAccumulating = false;
                jumpValue.Value = _jumpAccumulator;
                _resetJumpValueTime = Time.time + _resetJumpValueDuration;
                _jumpAccumulator = 0;
                jumpPowerAccumulator.Value = 0;
                return;
            }
            
            if (!_jump)
            {
                return;
            }

            if (!_jumpOnRelease)
            {
                jumpValue.Value = 1;
                _resetJumpValueTime = Time.time + _resetJumpValueDuration;
                return;
            }

            _isJumpAccumulating = true;
            _jumpAccumulator = 0;
            jumpPowerAccumulator.Value = 0;
        }

        private void ApplyAxisValues()
        {
            var verticalTarget = 0f;
            var horizontalTarget = 0f;
            if (_forward)
            {
                verticalTarget = 1;
            }
            else if (_backward)
            {
                verticalTarget = -1;
            }
            if (_right)
            {
                horizontalTarget = 1;
            }
            else if (_left)
            {
                horizontalTarget = -1;
            }

            if (verticalAxis.CanRead)
            {
                _axisV = verticalAxis.Value;
            }
            if (horizontalAxis.CanRead)
            {
                _axisH = horizontalAxis.Value;
            }
            
            _axisV = Mathf.MoveTowards(_axisV, verticalTarget, _verticalAxisAcceleration.Value * Time.deltaTime);
            _axisH = Mathf.MoveTowards(_axisH, horizontalTarget, _horizontalAxisAcceleration.Value * Time.deltaTime);

            verticalAxis.Value = _axisV;
            horizontalAxis.Value = _axisH;
            movement.Value = new Vector2(_axisH, _axisV);
        }
    }
}
