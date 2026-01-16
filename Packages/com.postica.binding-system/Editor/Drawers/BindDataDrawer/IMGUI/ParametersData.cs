using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Postica.Common;

namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        internal struct ParameterData
        {
            public string name;
            public Type type;
            public object valueProvider;
            public GUIContent content;
            public float height;
        }

        internal struct Parameters
        {
            public ParameterData[] array;

            public readonly BindDataParameter[] parameters;
            public readonly int mainParamIndex;
            private readonly MemberInfo _member;
            private readonly PropertyData _data;

            public Parameters(PropertyData data)
            {
                array = Array.Empty<ParameterData>();
                parameters = Array.Empty<BindDataParameter>();
                mainParamIndex = -1;
                _member = null;
                _data = null;

                var purePath = data.properties.path.stringValue;
                if (string.IsNullOrEmpty(purePath))
                {
                    return;
                }
                
                var lastIndexOf = purePath.LastIndexOf(']');
                if(lastIndexOf < 0)
                {
                    lastIndexOf = purePath.LastIndexOf(')');
                }
            
                if (lastIndexOf < 5)
                {
                    // Avoid the case of Providers or Arrays
                    data.parameters = default;
                    return;
                }
            
                var path = purePath.Substring(0, lastIndexOf + 1);
                
                var member = AccessorsFactory.GetMemberAtPath(
                    data.properties.target.objectReferenceValue.GetType(),
                    path);
                
                var parametersProperty = data.properties.parameters;
                _member = member;
                _data = data;
                parameters = parametersProperty.GetValue() as BindDataParameter[];
                mainParamIndex = data.properties.mainParameterIndex.intValue;

                (string name, Type type)[] pars = null;
                ParameterInfo[] paramInfos = null;
                Type memberType = null;

                switch (member)
                {
                    case FieldInfo info:
                        memberType = info.FieldType;
                        break;
                    case PropertyInfo info:
                        paramInfos = info.GetIndexParameters();
                        memberType = info.PropertyType;
                        break;
                    case MethodInfo info:
                        paramInfos = info.GetParameters();
                        memberType = info.ReturnType;
                        break;
                    case Type info:
                        memberType = info;
                        break;
                }

                if(paramInfos?.Length > 0)
                {
                    pars = paramInfos.Select(p => (p.Name, p.ParameterType)).ToArray();
                }
                else if(memberType?.IsArray == true)
                {
                    pars = new (string, Type)[memberType.GetArrayRank()];
                    if(pars.Length == 1)
                    {
                        pars[0] = ("Index", typeof(int));
                    }
                    else if (pars.Length > 1)
                    {
                        for (int i = 0; i < pars.Length; i++)
                        {
                            pars[i] = ("Index " + (i + 1), typeof(int));
                        }
                    }
                }
                else
                {
                    array = Array.Empty<ParameterData>();
                    return;
                }

                array = new ParameterData[pars.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    var bindParam_i = parameters[i];
                    var parameter_i = pars[i];

                    array[i].content = new GUIContent(StringUtility.NicifyName(parameter_i.name));
                    array[i].valueProvider = bindParam_i;
                    array[i].name = parameter_i.name;
                    array[i].type = parameter_i.type;
                }
            }

            public bool HaveChanged() => _data?.isValid != true || _member == null || _data.properties.parameters?.arraySize != array?.Length;

            public bool IsValid() => _data?.properties.parameters != null && _data.properties.parameters?.arraySize == array?.Length;

            public void Reset(bool logReset)
            {
                if (_data == null || _data.properties.parameters == null) { return; }

                _data.properties.parameters.arraySize = 0;
#if BS_DEBUG
                if (logReset)
                {
                    Debug.LogWarning($"Invalid parameters state at {_data.properties.path.stringValue}. Resetting parameters...");
                }
#endif
            }

            public float GetHeight()
            {
                if (_data?.properties.parameters == null) { return 0; }
                if (_data?.properties.path.isExpanded != true) { return 0; }

                var parametersProperty = _data.properties.parameters;

                float totalHeight = 0;
                var size = parametersProperty.arraySize;

                if (size != array.Length)
                {
                    // Invalid state...
                    return totalHeight;
                }

                for (int i = 0; i < size; i++)
                {
                    var property_i = parametersProperty.GetArrayElementAtIndex(i);

                    var propertyHeight = EditorGUI.GetPropertyHeight(property_i, array[i].content, true);

                    array[i].height = propertyHeight;

                    totalHeight += propertyHeight;
                }

                return totalHeight;
            }
        }
    }
}