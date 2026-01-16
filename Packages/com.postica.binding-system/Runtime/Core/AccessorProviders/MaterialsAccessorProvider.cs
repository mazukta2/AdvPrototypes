using Postica.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Postica.BindingSystem.Accessors
{
    [HideMember]
    public class MaterialsAccessorProvider : ComponentAccessorProvider<Renderer>
    {
        /// <inheritdoc/>
        public override string Id => "Materials";

        /// <inheritdoc/>
        public override IEnumerable<AccessorPath> GetAvailablePaths(Renderer source)
        {
            List<AccessorPath> paths = new List<AccessorPath>();
            var materials = Application.isPlaying ? source.materials : source.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                BuildPaths(paths, materials[i], i);
            }

            return paths;
        }

        private void BuildPaths(List<AccessorPath> paths, Material material, int materialIndex)
        {
            if(material == null) { return; }

            var materialName = material.name.Replace(" (Clone)", "");

            var materialPath = new AccessorPath(this,
                                                null,
                                                $"{materialIndex}:{materialName}",
                                                BindMode.Read,
                                                typeof(Material),
                                                material,
                                                materialName,
                                                true,
                                                material.shader.name
                                                );
            paths.Add(materialPath);

            var keywords = material.shaderKeywords;
            for (int i = 0; i < keywords.Length; i++)
            {
                var name = keywords[i];
                var type = typeof(bool);
                paths.Add(new AccessorPath(this,
                                           materialPath,
                                           $"{materialIndex}:{materialName}:{name}",
                                           BindMode.ReadWrite,
                                           type,
                                           GetValue(material, type, name),
                                           $"{materialName}/{StringUtility.NicifyName(name)}",
                                           true));
            }

            var shader = material.shader;
            var propertiesCount = shader.GetPropertyCount();
            for (int i = 0; i < propertiesCount; i++)
            {
                var name = shader.GetPropertyName(i);
                var propertyType = shader.GetPropertyType(i);
                Type type = null;
                var isSealed = true;
                switch (propertyType)
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        type = typeof(Color);
                        isSealed = false;
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                        type = typeof(Vector4);
                        isSealed = false;
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                        type = typeof(float);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Range:
                        type = typeof(float);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Texture:
                        type = typeof(Texture);
                        isSealed = false;
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Int:
                        type = typeof(int);
                        break;
                }

                paths.Add(new AccessorPath(this,
                                           materialPath,
                                           $"{materialIndex}:{materialName}:{name}:{propertyType}",
                                           BindMode.ReadWrite,
                                           type,
                                           GetValue(material, type, name),
                                           $"{materialName}/{StringUtility.NicifyName(name)}",
                                           isSealed));
            }
        }

        private object GetValue(Material material, Type type, string name)
        {
            if (typeof(Texture).IsAssignableFrom(type))
            {
                return material.GetTexture(name);
            }
            if(type == typeof(Color))
            {
                return material.GetColor(name);
            }
            return null;
        }

        /// <inheritdoc/>
        protected override IAccessor GetAccessor(string pathId)
        {
            var idSplit = pathId.Split(':');
            if (idSplit.Length < 2 || idSplit.Length > 4)
            { 
                return null;
            }

            var materialIndex = int.Parse(idSplit[0]);
            var materialName = idSplit[1];

            if (idSplit.Length == 2)
            {
                return new MaterialAccessor(materialName, materialIndex);
            }

            var propertyName = idSplit[2];
            var propertyId = Shader.PropertyToID(propertyName);

            if(idSplit.Length == 3)
            {
                // Most probably a keyword
                return new MaterialBooleanAccessor()
                {
                    materialIndex = materialIndex,
                    materialName = materialName,
                    propertyId = propertyId,
                    propertyName = propertyName
                };
            }

            var propertyType = (UnityEngine.Rendering.ShaderPropertyType)Enum.Parse(typeof(UnityEngine.Rendering.ShaderPropertyType), idSplit[3]);
            switch (propertyType)
            {
                case UnityEngine.Rendering.ShaderPropertyType.Color:
                    return new MaterialColorAccessor()
                    {
                        materialIndex = materialIndex,
                        materialName = materialName,
                        propertyId = propertyId,
                        propertyName = propertyName
                    };
                case UnityEngine.Rendering.ShaderPropertyType.Float:
                    return new MaterialFloatAccessor()
                    {
                        materialIndex = materialIndex,
                        materialName = materialName,
                        propertyId = propertyId,
                        propertyName = propertyName
                    };
                case UnityEngine.Rendering.ShaderPropertyType.Int:
                    return new MaterialIntAccessor()
                    {
                        materialIndex = materialIndex,
                        materialName = materialName,
                        propertyId = propertyId,
                        propertyName = propertyName
                    };
                case UnityEngine.Rendering.ShaderPropertyType.Range:
                    return new MaterialFloatAccessor()
                    {
                        materialIndex = materialIndex,
                        materialName = materialName,
                        propertyId = propertyId,
                        propertyName = propertyName
                    };
                case UnityEngine.Rendering.ShaderPropertyType.Texture:
                    return new MaterialTextureAccessor()
                    {
                        materialIndex = materialIndex,
                        materialName = materialName,
                        propertyId = propertyId,
                        propertyName = propertyName
                    };
                case UnityEngine.Rendering.ShaderPropertyType.Vector:
                    return new MaterialVector4Accessor()
                    {
                        materialIndex = materialIndex,
                        materialName = materialName,
                        propertyId = propertyId,
                        propertyName = propertyName
                    };
            }

            return null;
        }

        /// <inheritdoc/>
        public override bool TryConvertIdToPath(string id, string separator, out string path)
        {
            var idSplit = id.Split(':');
            if(idSplit.Length < 2 || idSplit.Length > 4) 
            {
                path = null;
                return false; 
            }

            if(idSplit.Length == 2)
            {
                path = idSplit[1];
                return true;
            }

            if (idSplit.Length == 3)
            {
                path = idSplit[1] + separator + idSplit[2];
                return true;
            }

            var index = idSplit[3].IndexOf(separator);
            path = index < 0 
                ? idSplit[1] + separator + idSplit[2] 
                : idSplit[1] + separator + idSplit[2] + separator + idSplit[3].Substring(index + 1);
            return true;
        }

        #region [  MATERIAL ACCESSORS  ]

        private sealed class MaterialBooleanAccessor : MaterialPropertyAccessor<MaterialBooleanAccessor, bool>
        {
            public override bool GetValue(Material material) => material.IsKeywordEnabled(propertyName);

            public override void SetValue(Material material, in bool value) 
            {
                if (value) { material.EnableKeyword(propertyName); }
                else { material.DisableKeyword(propertyName); }
            }
        }

        private sealed class MaterialMatrixAccessor : MaterialPropertyAccessor<MaterialMatrixAccessor, Matrix4x4>
        {
            public override Matrix4x4 GetValue(Material material) => material.GetMatrix(propertyId);

            public override void SetValue(Material material, in Matrix4x4 value) => material.SetMatrix(propertyId, value);
        }

        private sealed class MaterialVector4Accessor : MaterialPropertyAccessor<MaterialVector4Accessor, Vector4>
        {
            public override Vector4 GetValue(Material material) => material.GetVector(propertyId);

            public override void SetValue(Material material, in Vector4 value) => material.SetVector(propertyId, value);
        }

        private sealed class MaterialTextureAccessor : MaterialPropertyAccessor<MaterialTextureAccessor, Texture>
        {
            public override Texture GetValue(Material material) => material.GetTexture(propertyId);

            public override void SetValue(Material material, in Texture value) => material.SetTexture(propertyId, value);
        }

        private sealed class MaterialIntAccessor : MaterialPropertyAccessor<MaterialIntAccessor, int>
        {
            public override int GetValue(Material material) => material.GetInt(propertyId);

            public override void SetValue(Material material, in int value) => material.SetInt(propertyId, value);
        }

        private sealed class MaterialFloatAccessor : MaterialPropertyAccessor<MaterialFloatAccessor, float>
        {
            public override float GetValue(Material material) => material.GetFloat(propertyId);

            public override void SetValue(Material material, in float value) => material.SetFloat(propertyId, value);
        }

        private sealed class MaterialColorAccessor : MaterialPropertyAccessor<MaterialColorAccessor, Color>
        {
            public override Color GetValue(Material material) => material.GetColor(propertyId);

            public override void SetValue(Material material, in Color value) => material.SetColor(propertyId, value);
        }

        private abstract class MaterialPropertyAccessor<TP, T> : BaseAccessor<Renderer, T> where TP : MaterialPropertyAccessor<TP, T>, new()
        {
            public int propertyId;
            public string propertyName;
            public string materialName;
            public int materialIndex;

            public override bool CanRead => true;

            public override bool CanWrite => true;

            public override IAccessor Duplicate()
            {
                return new TP()
                {
                    propertyId = propertyId,
                    propertyName = propertyName,
                    materialName = materialName,
                    materialIndex = materialIndex
                };
            }

            public sealed override T GetValue(Renderer target)
            {
                return GetValue(GetMaterial(target));
            }

            public sealed override void SetValue(ref Renderer target, in T value)
            {
                SetValue(GetMaterial(target), value);
            }

            private Material GetMaterial(Renderer target)
            {
                var materials = Application.isPlaying ? target.materials : target.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && materials[i].name.Replace(" (Clone)", "").Replace(" (Instance)", "") == materialName)
                    {
                        return materials[i];
                    }
                }

                return materials[Mathf.Min(materialIndex, materials.Length - 1)];
            }

            public abstract T GetValue(Material material);
            public abstract void SetValue(Material material, in T value);
        }

        private sealed class MaterialAccessor : BaseAccessor<Renderer, Material>
        {
            private readonly string _materialName;
            private readonly int _materialIndex;

            public MaterialAccessor(string materialName, int materialIndex)
            {
                _materialName = materialName;
                _materialIndex = materialIndex;
            }

            public override bool CanRead => true;

            public override bool CanWrite => false;

            public override IAccessor Duplicate()
            {
                return new MaterialAccessor(_materialName, _materialIndex);
            }

            public override Material GetValue(Renderer target)
            {
                var materials = Application.isPlaying ? target.materials : target.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if(materials[i] != null && materials[i].name.Replace(" (Clone)", "") == _materialName)
                    {
                        return materials[i];
                    }
                }

                return materials[Mathf.Min(_materialIndex, materials.Length - 1)];
            }

            public override void SetValue(ref Renderer target, in Material value)
            {
                // Nothing for now
            }
        }

        #endregion
    }
}
