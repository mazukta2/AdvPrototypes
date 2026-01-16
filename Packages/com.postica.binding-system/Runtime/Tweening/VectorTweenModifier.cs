using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Tweening
{
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class Vector3EaseModifier : EaseModifier<Vector3>
    {
        protected override Vector3 Lerp(Vector3 from, Vector3 to, float progress)
        {
            return Vector3.LerpUnclamped(from, to, progress);
        }
        
        protected override float GetLerpPoint(Vector3 actual, Vector3 from, Vector3 to)
        {
            var toFromMagnitude = (to - from).magnitude;
            if (Mathf.Approximately(toFromMagnitude, 0))
            {
                return 0;
            }
            
            var toActualMagnitude = (actual - from).magnitude;
            var sign = Mathf.Sign(Vector3.Dot(to - from, actual - from));

            return sign * toActualMagnitude / toFromMagnitude;
        }

        protected override bool Equals(Vector3 a, Vector3 b)
        {
            // Use a tolerance for floating-point comparison
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z);
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class Vector2EaseModifier : EaseModifier<Vector2>
    {
        protected override Vector2 Lerp(Vector2 from, Vector2 to, float progress)
        {
            return Vector2.LerpUnclamped(from, to, progress);
        }
        
        protected override float GetLerpPoint(Vector2 actual, Vector2 from, Vector2 to)
        {
            var toFromMagnitude = (to - from).magnitude;
            if (Mathf.Approximately(toFromMagnitude, 0))
            {
                return 0;
            }
            
            var toActualMagnitude = (actual - from).magnitude;
            var sign = Mathf.Sign(Vector2.Dot(to - from, actual - from));

            return sign * toActualMagnitude / toFromMagnitude;
        }
        
        protected override bool Equals(Vector2 a, Vector2 b)
        {
            // Use a tolerance for floating-point comparison
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class Vector4EaseModifier : EaseModifier<Vector4>
    {
        protected override Vector4 Lerp(Vector4 from, Vector4 to, float progress)
        {
            return Vector4.LerpUnclamped(from, to, progress);
        }
        
        protected override float GetLerpPoint(Vector4 actual, Vector4 from, Vector4 to)
        {
            var toFromMagnitude = (to - from).magnitude;
            if (Mathf.Approximately(toFromMagnitude, 0))
            {
                return 0;
            }
            
            var toActualMagnitude = (actual - from).magnitude;
            var sign = Mathf.Sign(Vector4.Dot(to - from, actual - from));

            return sign * toActualMagnitude / toFromMagnitude;
        }
        
        protected override bool Equals(Vector4 a, Vector4 b)
        {
            // Use a tolerance for floating-point comparison
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) &&
                   Mathf.Approximately(a.z, b.z) && Mathf.Approximately(a.w, b.w);
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class Vector2IntEaseModifier : EaseModifier<Vector2Int>
    {
        protected override Vector2Int Lerp(Vector2Int from, Vector2Int to, float progress)
        {
            return new Vector2Int(Mathf.FloorToInt(Mathf.Lerp(from.x, to.x, progress)),
                Mathf.FloorToInt(Mathf.Lerp(from.y, to.y, progress)));
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class Vector3IntEaseModifier : EaseModifier<Vector3Int>
    {
        protected override Vector3Int Lerp(Vector3Int from, Vector3Int to, float progress)
        {
            return new Vector3Int(Mathf.FloorToInt(Mathf.Lerp(from.x, to.x, progress)),
                Mathf.FloorToInt(Mathf.Lerp(from.y, to.y, progress)),
                Mathf.FloorToInt(Mathf.Lerp(from.z, to.z, progress)));
        }
    }
}