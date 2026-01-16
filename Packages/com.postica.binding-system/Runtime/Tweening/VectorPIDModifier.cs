using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Tweening
{
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class Vector3PIDModifier : PIDModifier<Vector3>
    {
        public Vector3PIDModifier()
        {
            P_I_D = new Data()
            {
                useLimits = true.Bind(),
                min = (-Vector3.one).Bind(),
                max = Vector3.one.Bind(),
            };
        }
        protected override Vector3 Add(Vector3 a, Vector3 b) => a + b;
        protected override Vector3 Subtract(Vector3 a, Vector3 b) => a - b;
        protected override Vector3 Multiply(Vector3 a, float b) => a * b;
        protected override Vector3 Divide(Vector3 a, float b) => a / b;
        protected override Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max)
        {
            return new Vector3(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y),
                Mathf.Clamp(value.z, min.z, max.z)
            );
        }
        
        protected override Vector3 Normalize(Vector3 value, Vector3 min, Vector3 max)
        {
            return new Vector3(
                Mathf.InverseLerp(min.x, max.x, value.x),
                Mathf.InverseLerp(min.y, max.y, value.y),
                Mathf.InverseLerp(min.z, max.z, value.z)
            );
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class Vector2PIDModifier : PIDModifier<Vector2>
    {
        public Vector2PIDModifier()
        {
            P_I_D = new Data()
            {
                useLimits = true.Bind(),
                min = (-Vector2.one).Bind(),
                max = Vector2.one.Bind(),
            };
        }
        protected override Vector2 Add(Vector2 a, Vector2 b) => a + b;
        protected override Vector2 Subtract(Vector2 a, Vector2 b) => a - b;
        protected override Vector2 Multiply(Vector2 a, float b) => a * b;
        protected override Vector2 Divide(Vector2 a, float b) => a / b;
        protected override Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max)
        {
            return new Vector2(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y)
            );
        }
        
        protected override Vector2 Normalize(Vector2 value, Vector2 min, Vector2 max)
        {
            return new Vector2(
                Mathf.InverseLerp(min.x, max.x, value.x),
                Mathf.InverseLerp(min.y, max.y, value.y)
            );
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class Vector4PIDModifier : PIDModifier<Vector4>
    {
        public Vector4PIDModifier()
        {
            P_I_D = new Data()
            {
                useLimits = true.Bind(),
                min = (-Vector4.one).Bind(),
                max = Vector4.one.Bind(),
            };
        }
        protected override Vector4 Add(Vector4 a, Vector4 b) => a + b;
        protected override Vector4 Subtract(Vector4 a, Vector4 b) => a - b;
        protected override Vector4 Multiply(Vector4 a, float b) => a * b;
        protected override Vector4 Divide(Vector4 a, float b) => a / b;
        protected override Vector4 Clamp(Vector4 value, Vector4 min, Vector4 max)
        {
            return new Vector4(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y),
                Mathf.Clamp(value.z, min.z, max.z),
                Mathf.Clamp(value.w, min.w, max.w)
            );
        }
        
        protected override Vector4 Normalize(Vector4 value, Vector4 min, Vector4 max)
        {
            return new Vector4(
                Mathf.InverseLerp(min.x, max.x, value.x),
                Mathf.InverseLerp(min.y, max.y, value.y),
                Mathf.InverseLerp(min.z, max.z, value.z),
                Mathf.InverseLerp(min.w, max.w, value.w)
            );
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class Vector2IntPIDModifier : PIDModifier<Vector2Int>
    {
        protected override Vector2Int Add(Vector2Int a, Vector2Int b) => a + b;
        protected override Vector2Int Subtract(Vector2Int a, Vector2Int b) => a - b;
        protected override Vector2Int Multiply(Vector2Int a, float b) => new Vector2Int(Mathf.RoundToInt(a.x * b), Mathf.RoundToInt(a.y * b));
        protected override Vector2Int Divide(Vector2Int a, float b) => new Vector2Int(Mathf.RoundToInt(a.x / b), Mathf.RoundToInt(a.y / b));
        protected override Vector2Int Clamp(Vector2Int value, Vector2Int min, Vector2Int max)
        {
            return new Vector2Int(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y)
            );
        }
        
        protected override Vector2Int Normalize(Vector2Int value, Vector2Int min, Vector2Int max)
        {
            return new Vector2Int(
                Mathf.RoundToInt(Mathf.InverseLerp(min.x, max.x, value.x)),
                Mathf.RoundToInt(Mathf.InverseLerp(min.y, max.y, value.y))
            );
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class Vector3IntPIDModifier : PIDModifier<Vector3Int>
    {
        protected override Vector3Int Add(Vector3Int a, Vector3Int b) => a + b;
        protected override Vector3Int Subtract(Vector3Int a, Vector3Int b) => a - b;
        protected override Vector3Int Multiply(Vector3Int a, float b) => new Vector3Int(Mathf.RoundToInt(a.x * b), Mathf.RoundToInt(a.y * b), Mathf.RoundToInt(a.z * b));
        protected override Vector3Int Divide(Vector3Int a, float b) => new Vector3Int(Mathf.RoundToInt(a.x / b), Mathf.RoundToInt(a.y / b), Mathf.RoundToInt(a.z / b));
        protected override Vector3Int Clamp(Vector3Int value, Vector3Int min, Vector3Int max)
        {
            return new Vector3Int(
                Mathf.Clamp(value.x, min.x, max.x),
                Mathf.Clamp(value.y, min.y, max.y),
                Mathf.Clamp(value.z, min.z, max.z)
            );
        }
        
        protected override Vector3Int Normalize(Vector3Int value, Vector3Int min, Vector3Int max)
        {
            return new Vector3Int(
                Mathf.RoundToInt(Mathf.InverseLerp(min.x, max.x, value.x)),
                Mathf.RoundToInt(Mathf.InverseLerp(min.y, max.y, value.y)),
                Mathf.RoundToInt(Mathf.InverseLerp(min.z, max.z, value.z))
            );
        }
    }
}