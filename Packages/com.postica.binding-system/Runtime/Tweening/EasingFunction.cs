using System;
using UnityEngine;

namespace Postica.BindingSystem.Tweening
{
    public class EasingFunction
    {
        public string Name { get; }
        public Func<float, float> Function { get; protected set; }
        public EaseType EaseType { get; }
        public FunctionType FunctionType { get; }
        public float? ConvergenceTime { get; set; }

        public EasingFunction(string name, EaseType easeType, FunctionType functionType)
        {
            Name = name;
            EaseType = easeType;
            FunctionType = functionType;
            Function = TweenSystem.GetEaseFunction(functionType, easeType);
        }
        
        public EasingFunction(string name, Func<float, float> function)
        {
            Name = name;
            Function = function;
            EaseType = EaseType.EaseIn;
            FunctionType = FunctionType.Custom;
        }
        
        public EasingFunction(EaseType easeType, FunctionType functionType)
        {
            Name = easeType.ToString() + functionType;
            EaseType = easeType;
            FunctionType = functionType;
            Function = TweenSystem.GetEaseFunction(functionType, easeType);
        }
        
        public float Evaluate(float t)
        {
            return Function(t);
        }

        public (float min, float max) GetMinMax(float xMin, float xMax, float dt)
        {
            var min = Evaluate(xMin);
            var max = Evaluate(xMax);
            for (float x = xMin; x < xMax; x += dt)
            {
                var value = Evaluate(x);
                if (value < min) min = value;
                if (value > max) max = value;
            }
            
            return (min, max);
        }
        
        public float GetConvergencePoint(float xMin, float xMax, float dt, float threshold = 0)
        {
            var lastValue = Evaluate(xMax);
            
            if (threshold <= 0)
            {
                threshold = Mathf.Epsilon;
            }
            
            for (float x = xMax - dt; x >= xMin; x -= dt)
            {
                var value = Evaluate(x);
                if (Mathf.Abs(value - lastValue) >= threshold)
                {
                    return x;
                }
            
                lastValue = value;
            }

            return xMin;
        }
    }
    
    public class DeltaEasingFunction : EasingFunction
    {
        public delegate float DeltaFunction(float dt);

        private DeltaFunction _deltaFunction;
        private Action _onRestart;
        private float _lastValue;
        
        private float ComputeFull(float t)
        {
            if (_lastValue > t)
            {
                _lastValue = t;
                _onRestart?.Invoke();
                
                return _deltaFunction(0);
            }
            
            var dt = t - _lastValue;
            _lastValue = t;
            
            return _deltaFunction(dt);
        }
        
        public DeltaEasingFunction(string name, DeltaFunction deltaFunction, Action onRestart = null)
            : base(name, null)
        {
            Function = ComputeFull;
            _deltaFunction = deltaFunction;
            _onRestart = onRestart;
        }
    }
}