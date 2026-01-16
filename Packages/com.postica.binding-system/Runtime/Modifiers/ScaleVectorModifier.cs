using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Modifiers
{
    public static class ScaleVectorModifiers
    {
        public static void RegisterAll()
        {
            ModifiersFactory.Register<ScaleVector2Modifier>();
            ModifiersFactory.Register<ScaleVector3Modifier>();
            ModifiersFactory.Register<ScaleVector4Modifier>();
            ModifiersFactory.Register<ScaleVector2IntModifier>();
            ModifiersFactory.Register<ScaleVector3IntModifier>();
        }
    }
    
    [Serializable]
    internal sealed class ScaleVector2Modifier : ScaleVectorModifier<Vector2>, IDynamicComponent
    {
        [Tooltip("The value to multiply.")]
        public ReadOnlyBind<Vector2> scaleBy = Vector2.one.Bind();
        protected override Vector2 Modify(Vector2 value) => Vector2.Scale(value, scaleBy.Value);
        protected override Vector2 InverseModify(Vector2 output) => Vector2.Scale(output, scaleBy.Value.Inverse());

        public bool IsDynamic => scaleBy.IsBound;
    }
    
    [Serializable]
    internal sealed class ScaleVector3Modifier : ScaleVectorModifier<Vector3>, IDynamicComponent
    {
        [Tooltip("The value to multiply.")]
        public ReadOnlyBind<Vector3> scaleBy = Vector3.one.Bind();
        protected override Vector3 Modify(Vector3 value) => Vector3.Scale(value, scaleBy.Value);
        protected override Vector3 InverseModify(Vector3 output) => Vector3.Scale(output, scaleBy.Value.Inverse());

        public bool IsDynamic => scaleBy.IsBound;
    }
    
    [Serializable]
    internal sealed class ScaleVector4Modifier : ScaleVectorModifier<Vector4>, IDynamicComponent
    {
        [Tooltip("The value to multiply.")]
        public ReadOnlyBind<Vector4> scaleBy = Vector4.one.Bind();
        protected override Vector4 Modify(Vector4 value) => Vector4.Scale(value, scaleBy.Value);
        protected override Vector4 InverseModify(Vector4 output) => Vector4.Scale(output, scaleBy.Value.Inverse());

        public bool IsDynamic => scaleBy.IsBound;
    }
    
    [Serializable]
    internal sealed class ScaleVector2IntModifier : ScaleVectorModifier<Vector2Int>, IDynamicComponent
    {
        [Tooltip("The value to multiply.")]
        public ReadOnlyBind<Vector2Int> scaleBy = Vector2Int.one.Bind();
        protected override Vector2Int Modify(Vector2Int value) => Vector2Int.Scale(value, scaleBy.Value);
        protected override Vector2Int InverseModify(Vector2Int output) => Vector2Int.Scale(output, scaleBy.Value.Inverse());

        public bool IsDynamic => scaleBy.IsBound;
    }
    
    [Serializable]
    internal sealed class ScaleVector3IntModifier : ScaleVectorModifier<Vector3Int>, IDynamicComponent
    {
        [Tooltip("The value to multiply.")]
        public ReadOnlyBind<Vector3Int> scaleBy = Vector3Int.one.Bind();
        protected override Vector3Int Modify(Vector3Int value) => Vector3Int.Scale(value, scaleBy.Value);
        protected override Vector3Int InverseModify(Vector3Int output) => Vector3Int.Scale(output, scaleBy.Value.Inverse());

        public bool IsDynamic => scaleBy.IsBound;
    }
    
    /// <summary>
    /// A modifier which multiplies each component of a <typeparamref name="T"/>.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [TypeIcon("_bsicons/modifiers/multiply")]
    [TypeDescription("Multiplies each component of the vector by another vector.")]
    public abstract class ScaleVectorModifier<T> : BaseModifier<T>
    {
        ///<inheritdoc/>
        public override string Id { get; } = $"Scale {typeof(T).Name}";

        ///<inheritdoc/>
        public override string ShortDataDescription => "";
    }
}