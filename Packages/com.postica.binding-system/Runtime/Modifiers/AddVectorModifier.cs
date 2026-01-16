using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Modifiers
{
    public static class AddVectorModifiers
    {
        public static void RegisterAll()
        {
            ModifiersFactory.Register<AddVector2Modifier>();
            ModifiersFactory.Register<AddVector3Modifier>();
            ModifiersFactory.Register<AddVector4Modifier>();
            ModifiersFactory.Register<AddVector2IntModifier>();
            ModifiersFactory.Register<AddVector3IntModifier>();
        }
    }
    
    [Serializable]
    internal sealed class AddVector2Modifier : AddVectorModifier<Vector2>
    {
        protected override Vector2 Modify(Vector2 value) => value + add.Value;
        protected override Vector2 InverseModify(Vector2 output) => output - add.Value;
    }
    
    [Serializable]
    internal sealed class AddVector3Modifier : AddVectorModifier<Vector3>
    {
        protected override Vector3 Modify(Vector3 value) => value + add.Value;
        protected override Vector3 InverseModify(Vector3 output) => output - add.Value;
    }
    
    [Serializable]
    internal sealed class AddVector4Modifier : AddVectorModifier<Vector4>
    {
        protected override Vector4 Modify(Vector4 value) => value + add.Value;
        protected override Vector4 InverseModify(Vector4 output) => output - add.Value;
    }
    
    [Serializable]
    internal sealed class AddVector2IntModifier : AddVectorModifier<Vector2Int>
    {
        protected override Vector2Int Modify(Vector2Int value) => value + add.Value;
        protected override Vector2Int InverseModify(Vector2Int output) => output - add.Value;
    }
    
    [Serializable]
    internal sealed class AddVector3IntModifier : AddVectorModifier<Vector3Int>
    {
        protected override Vector3Int Modify(Vector3Int value) => value + add.Value;
        protected override Vector3Int InverseModify(Vector3Int output) => output - add.Value;
    }
    
    /// <summary>
    /// A modifier which adds each component of a <typeparamref name="T"/>.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [TypeIcon("_bsicons/modifiers/add")]
    [TypeDescription("Sums each component of the vector.")]
    public abstract class AddVectorModifier<T> : BaseModifier<T>, IDynamicComponent
    {
        [Tooltip("The additive value to apply.")]
        public ReadOnlyBind<T> add = default(T).Bind();
        
        ///<inheritdoc/>
        public override string Id { get; } = $"Add {typeof(T).Name}";

        ///<inheritdoc/>
        public override string ShortDataDescription => "";

        ///<inheritdoc/>
        public bool IsDynamic => add.IsBound;
    }
}