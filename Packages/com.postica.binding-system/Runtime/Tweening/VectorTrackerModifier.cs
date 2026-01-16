using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Tweening
{
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class Vector3DampedTrackerModifier : DampedTrackerModifier<Vector3>
    {
        protected override Vector3 Add(Vector3 a, Vector3 b) => a + b;
        protected override Vector3 Subtract(Vector3 a, Vector3 b) => a - b;
        protected override Vector3 Multiply(Vector3 a, float b) => a * b;

        protected override bool Equals(Vector3 a, Vector3 b, float epsilon)
        {
            // Use a tolerance for floating-point comparison
            return a.IsApprox(b, epsilon);
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class Vector2DampedTrackerModifier : DampedTrackerModifier<Vector2>
    {
        protected override Vector2 Add(Vector2 a, Vector2 b) => a + b;
        protected override Vector2 Subtract(Vector2 a, Vector2 b) => a - b;
        protected override Vector2 Multiply(Vector2 a, float b) => a * b;

        
        protected override bool Equals(Vector2 a, Vector2 b, float epsilon)
        {
            // Use a tolerance for floating-point comparison
            return a.IsApprox(b, epsilon);
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class Vector4DampedTrackerModifier : DampedTrackerModifier<Vector4>
    {
        protected override Vector4 Add(Vector4 a, Vector4 b) => a + b;
        protected override Vector4 Subtract(Vector4 a, Vector4 b) => a - b;
        protected override Vector4 Multiply(Vector4 a, float b) => a * b;

        protected override bool Equals(Vector4 a, Vector4 b, float epsilon)
        {
            // Use a tolerance for floating-point comparison
            return a.IsApprox(b, epsilon);
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class Vector2IntDampedTrackerModifier : DampedTrackerModifier<Vector2Int>
    {
        protected override Vector2Int Add(Vector2Int a, Vector2Int b) => a + b;
        protected override Vector2Int Subtract(Vector2Int a, Vector2Int b) => a - b;
        protected override Vector2Int Multiply(Vector2Int a, float b) => new(Mathf.RoundToInt(a.x * b), Mathf.RoundToInt(a.y * b));

    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class Vector3IntDampedTrackerModifier : DampedTrackerModifier<Vector3Int>
    {
        protected override Vector3Int Add(Vector3Int a, Vector3Int b) => a + b;
        protected override Vector3Int Subtract(Vector3Int a, Vector3Int b) => a - b;
        protected override Vector3Int Multiply(Vector3Int a, float b) => new((int)(a.x * b), (int)(a.y * b), (int)(a.z * b));

    }
}