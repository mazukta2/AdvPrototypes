using Postica.BindingSystem.Accessors;
using Postica.BindingSystem.Reflection;
using Postica.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        void RequestRefresh(PropertyData data)
        {
            if (_isUIToolkit)
            {
                ApplyChanges(data);
            }
            else
            {
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        void ExecuteAction(PropertyData data, Action action, bool autoApply = true)
        {
            if (_isUIToolkit)
            {
                action?.Invoke();
                if (autoApply)
                {
                    ApplyChanges(data);
                }
            }
            else
            {
                data.preRenderAction = action;
            }
        }

        private void UpdateWriteConverter(PropertyData data, bool logExceptions = true)
        {
            if (data.writeConverter.HasChanged() && !data.hasError)
            {
                var (fromType, toType) = GetTypeMapping(data, logExceptions);
                if (toType != null && fromType != null && !fromType.IsAssignableFrom(toType))
                {
                    data.writeConverter = new ConverterHandler(toType, fromType, data, data.properties.writeConverter, false, _isUIToolkit);
                    RequestRefresh(data);
                }
            }
        }

        private void UpdateReadConverter(PropertyData data, bool logExceptions = true)
        {
            if (data.readConverter.HasChanged() && !data.hasError)
            {
                var (fromType, toType) = GetTypeMapping(data, logExceptions);
                if (toType != null && fromType != null && !toType.IsAssignableFrom(fromType))
                {
                    data.readConverter = new ConverterHandler(fromType, toType, data, data.properties.readConverter, true, _isUIToolkit);
                    RequestRefresh(data);
                }
            }
        }

        private void SetTargetValue(Object newTarget, PropertyData data, out bool isValid, bool logErrorsInConsole = true, bool silent = false)
        {
            var objType = data.sourcePersistedType;
            if (newTarget is Component newC && newC.GetType() != objType && objType != null)
            {
                if (objType == typeof(GameObject))
                {
                    data.properties.target.objectReferenceValue = newC.gameObject;
                }
                else if (typeof(Component).IsAssignableFrom(objType))
                {
                    var component = newC.gameObject.GetComponent(objType);
                    data.properties.target.objectReferenceValue = component;
                }
            }
            else if (newTarget is GameObject newGo
                && newGo.GetType() != objType
                && objType != null
                && typeof(Component).IsAssignableFrom(objType))
            {
                var component = newGo.GetComponent(objType);
                data.properties.target.objectReferenceValue = component ? component : newTarget;
            }
            else
            {
                data.properties.target.objectReferenceValue = newTarget;
            }

            isValid = false;

            if (ValidatePath(data.properties.target.objectReferenceValue, data, data.properties.path.stringValue, logErrorsInConsole))
            {
                data.prevValue = null;
                isValid = true;
            }

            if (string.IsNullOrEmpty(data.prevValue))
            {
                data.formattedValue = null;
            }

            if (!silent && !data.firstRun)
            {
                data.onChanged?.Invoke();
            }
        }

        private (Type from, Type to) GetTypeMapping(PropertyData data, bool logExceptions = true)
        {
            if (data.isSelfReference)
            {
                return (data.sourceCurrentType, data.bindType);
            }

            ref var properties = ref data.properties;

            if(string.IsNullOrEmpty(properties.path.stringValue)
             || string.IsNullOrEmpty(properties.type.stringValue))
            {
                return default;
            }
            try
            {
                var fromType =  AccessorsFactory.GetTypeAtPath(data.sourceCurrentType, properties.path.stringValue);
                var toType = data.bindType;
                return (fromType, toType);
            }
            catch (Exception ex)
            {
                if (logExceptions)
                {
                    Debug.LogException(ex);
                }
            }
            return default;
        }

        private bool ValidatePath(Object obj, PropertyData data, bool logErrorsInConsole = true)
        {
            return ValidatePath(obj, data, data.properties.path.stringValue, logErrorsInConsole);
        }
        
        private bool ValidatePath(Object obj, PropertyData data, string path, bool logErrorsInConsole = true)
        {
            if (!obj)
            {
                return false;
            }
            try
            {
                var (canRead, canWrite) = (false, false);
                if (data.isSelfReference)
                {
                    (canRead, canWrite) = (true, false);
                }
                else if (!string.IsNullOrEmpty(path))
                {
                    var accessor = AccessorsFactory.GetAccessor(obj,
                                                                path
                                                                //,
                                                                //data.parameters.parameters.GetValues(),
                                                                //data.parameters.mainParamIndex
                                                                );
                    (canRead, canWrite) = (accessor.CanRead, accessor.CanWrite);
                }
                else
                {
                    return true;
                }
                switch (data.properties.BindMode)
                {
                    case BindMode.Write:
                        if (canWrite)
                        {
                            return true;
                        }
                        data.hasError = true;
                        data.errorClass = Errors.Classes.BindMode;
                        data.errorMessage = "This path contains a readonly data";
                        break;
                    case BindMode.Read:
                        if (canRead)
                        {
                            return true;
                        }
                        data.hasError = true;
                        data.errorClass = Errors.Classes.BindMode;
                        data.errorMessage = "This path contains a non-readable data";
                        break;
                    case BindMode.ReadWrite:
                        if (!canWrite)
                        {
                            data.hasError = true;
                            data.errorClass = Errors.Classes.BindMode;
                            data.errorMessage = "This path contains a readonly data";
                            break;
                        }
                        if (!canRead)
                        {
                            data.hasError = true;
                            data.errorClass = Errors.Classes.BindMode;
                            data.errorMessage = "This path contains a non-readable data";
                        }
                        return true;
                }
                return false;
            }
            catch (ArgumentException aex)
            {
                if (data.invalidPath != aex.ParamName)
                {
                    var target = data.properties.target.objectReferenceValue;
                    var typeName = data.sourcePersistedType?.Name ?? string.Empty;
                    var showType = !target || target.GetType().Name != typeName;
                    data.invalidPath = aex.ParamName;
                    if (showType && path.StartsWith(data.invalidPath))
                    {
                        data.errorMessage = $"Cannot retrieve {typeName} from {target.name}";
                        data.errorClass = Errors.Classes.MissingComponent;
                    }
                    else
                    {
                        data.errorMessage = $"Path is not valid for {target.name}";
                        data.errorClass = Errors.Classes.Path;
                    }
                    if (logErrorsInConsole)
                    {
                        Debug.LogException(aex, obj);
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                data.errorClass = Errors.Classes.Path;
                if (logErrorsInConsole)
                {
                    Debug.LogException(ex, obj);
                }
                return false;
            }
        }

        private static IDictionary<TKey, TValue> ToDictionaryNotUnique<TKey, TValue>(IEnumerable<TValue> list, Func<TValue, TKey> keyFunctor)
        {
            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
            foreach(var item in list)
            {
                dictionary[keyFunctor(item)] = item;
            }
            return dictionary;
        }

        private static bool TryGetGameObject(Object obj, out GameObject go)
        {
            if(obj is GameObject igo)
            {
                go = igo;
                return true;
            }
            if(obj is Component c)
            {
                go = c.gameObject;
                return true;
            }

            go = null;
            return false;
        }

        private static Object GetSameTypeObject(Object obj, Object sample)
        {
            if(TryGetGameObject(obj, out var go1) 
                && sample is Component c
                && go1.TryGetComponent(c.GetType(), out var resultComponent))
            {
                return resultComponent;
            }

            return obj;
        }

        private string NicifyPath(string path)
        {
            // Switch to arrows
            return string.Join("→", path.Replace(AccessorPath.ArrayPrefix, "[index]", StringComparison.Ordinal)
                                        .Split('/').Select(p => StringUtility.NicifyName(p)).ToArray());
        }

        private string TransformPath(Object obj, string path)
        {
            if (path.StartsWith(AccessorPath.ProviderPrefix))
            {
                // Most probably it is a providers path
                return path;
            }

            if (obj)
            {
                path = path.RemoveAtStart(obj.GetType().Name + SmartDropdown.Separator);
            }

            return path.TrimStart(SmartDropdown.Separator);
        }

        private string InverseTransformPath(Object obj, string path)
        {
            return obj ? $"{obj.GetType().Name}/{path}" : path;
        }

        private static string GetFriendlyName(Type type)
        {
            if (type == typeof(float)) return "Float";

            return type.UserFriendlyName(true);
        }

        private GUIContent FitString(GUIStyle style, float controlWidth, PropertyData data, string value, bool forceRefit)
        {
            // TODO: Beware of property values as those can be from providers (thus the id instead of the path)

            if (string.IsNullOrEmpty(value) && string.IsNullOrEmpty(data.formattedValue))
            {
                if (!data.isSelfReference)
                {
                    data.formattedValue = data.properties.target.objectReferenceValue
                                        ? $" <color={Styles.helpColor}><i>Select a binding path...</i></color>"
                                        : $" <color={Styles.helpColor}><i>Provide a source object...</i></color>";
                    contents.formattedPath.text = data.formattedValue;
                    contents.formattedPath.tooltip = string.Empty;
                }
                else
                {
                    data.formattedValue = "<b>Self</b>";
                    contents.formattedPath.text = data.formattedValue;
                    contents.formattedPath.tooltip = "The object reference will be used as value";
                }
                data.prevValue = value;

                return contents.formattedPath;
            }

            if (forceRefit || data.prevValue != value || !string.IsNullOrEmpty(data.invalidPath) || data.shouldRefitPath)
            {
                data.shouldRefitPath = false;

                contents.formattedPath.tooltip = string.Empty;

                data.prevValue = value;
                if (!data.firstRun)
                {
                    data.hasError = false;
                }

                char separator = '/';
                
                bool invalidType = false;
                bool showType = false;
                var typeName = data.sourcePersistedType?.Name ?? string.Empty;

                // Check if it comes from a provider
                if (!string.IsNullOrEmpty(value) 
                    && AccessorPath.TryGetProviderId(value, out var providerId, out var cleanId))
                {
                    var providers = BindTypesCache.GetAllAccessorProviders();
                    var separatorString = new string(separator, 1);
                    if(providers.TryGetValue(providerId, out var provider) && provider.TryConvertIdToPath(cleanId, separatorString, out var nicePath))
                    {
                        value = providerId + '/' + nicePath;
                    }
                }

                if (!string.IsNullOrEmpty(data.invalidPath))
                {
                    data.hasError = true;
                    var target = data.properties.target.objectReferenceValue;
                    showType = !target || target.GetType().Name != typeName;
                    invalidType = showType && value.StartsWith(data.invalidPath);
                    if (invalidType)
                    {
                        data.errorMessage = $"Cannot retrieve {typeName} from {target.name}";
                    }
                    else
                    {
                        data.errorMessage = $"Path is not valid for {target.name}";
                        value = value.Replace(data.invalidPath, $"#%{data.invalidPath}%#", StringComparison.Ordinal);
                    }
                }

                if (ReflectionFactory.CurrentOptions.UseNiceNames)
                {
                    value = NicifyPath(value);
                    // Switch to arrows
                    separator = '→';
                }

                var lastSeparatorIndex = value.LastIndexOf(separator) + 1;
                if (lastSeparatorIndex > 0)
                {
                    _tempContent.text = value;
                    var textLength = style.CalcSize(_tempContent).x;
                    var exceededRatio = 1 - Mathf.Abs(controlWidth / textLength);
                    var textColor = style.normal.textColor;
                    var colorString = UnityEngine.ColorUtility.ToHtmlStringRGBA(textColor.WithAlpha(0.75f));
                    var textSize = style.fontSize - 1;

                    var startIndex = exceededRatio > 0 ? (int)Mathf.Min(lastSeparatorIndex, value.Length * exceededRatio) + 3 : 0;
                    var prefix = exceededRatio > 0 ? "..." : "";
                    var lessImportantPart = value.Substring(startIndex, lastSeparatorIndex - startIndex);
                    var moreImportantPart = value.Substring(lastSeparatorIndex);
                    var formattedValue = $"<size={textSize}><color=#{colorString}>{prefix}{lessImportantPart}</color></size><b>{moreImportantPart}</b>";

                    data.formattedValue = formattedValue;
                    contents.formattedPath.tooltip = value.Replace("#%", "", StringComparison.Ordinal).Replace("%#", "", StringComparison.Ordinal);
                }
                else
                {
                    data.formattedValue = value;
                    contents.formattedPath.tooltip = string.Empty;
                }

                if (!string.IsNullOrEmpty(data.invalidPath))
                {
                    var emphasizer = " ";
                    data.invalidPath = null;
                    if (invalidType)
                    {
                        data.formattedValue = $"<color={Styles.errorColor}><b>[{typeName}]</b></color>{emphasizer}" + data.formattedValue;
                    }
                    else
                    {
                        var finalValue = data.formattedValue.Replace("#%", $"<color={Styles.errorColor}><b>", StringComparison.Ordinal);
                        finalValue = finalValue.Replace("%#", "</b></color>", StringComparison.Ordinal);
                        data.formattedValue = showType 
                                            ? $"<color={Styles.warnColor}><b>[{typeName}]</b></color>{emphasizer}" + finalValue
                                            : finalValue;
                    }
                }
            }

            contents.formattedPath.text = data.formattedValue;
            return contents.formattedPath;
        }

        private string FitString(PropertyData data, string value, bool forceRefit)
        {
            // TODO: Beware of property values as those can be from providers (thus the id instead of the path)

            if (string.IsNullOrEmpty(value) && string.IsNullOrEmpty(data.formattedValue))
            {
                if (!data.isSelfReference)
                {
                    data.formattedValue = data.properties.target.objectReferenceValue
                                        ? $" <color={Styles.helpColor}><i>Select a binding path...</i></color>"
                                        : $" <color={Styles.helpColor}><i>Provide a source object...</i></color>";
                    contents.formattedPath.text = data.formattedValue;
                    contents.formattedPath.tooltip = string.Empty;
                }
                else
                {
                    data.formattedValue = "<b>Self</b>";
                    contents.formattedPath.text = data.formattedValue;
                    contents.formattedPath.tooltip = "The object reference will be used as value";
                }
                data.prevValue = value;

                return contents.formattedPath.text;
            }

            if (forceRefit || data.prevValue != value || !string.IsNullOrEmpty(data.invalidPath) || data.shouldRefitPath)
            {
                data.shouldRefitPath = false;

                contents.formattedPath.tooltip = string.Empty;

                data.prevValue = value;
                if (!data.firstRun)
                {
                    data.hasError = false;
                }

                char separator = '/';

                bool invalidType = false;
                bool showType = false;
                var typeName = data.sourcePersistedType?.Name ?? string.Empty;

                // Check if it comes from a provider
                if (!string.IsNullOrEmpty(value)
                    && AccessorPath.TryGetProviderId(value, out var providerId, out var cleanId))
                {
                    var providers = BindTypesCache.GetAllAccessorProviders();
                    var separatorString = new string(separator, 1);
                    if (providers.TryGetValue(providerId, out var provider) && provider.TryConvertIdToPath(cleanId, separatorString, out var nicePath))
                    {
                        value = providerId + '/' + nicePath;
                    }
                }

                if (!string.IsNullOrEmpty(data.invalidPath))
                {
                    data.hasError = true;
                    var target = data.properties.target.objectReferenceValue;
                    showType = !target || target.GetType().Name != typeName;
                    invalidType = showType && value.StartsWith(data.invalidPath);
                    if (invalidType)
                    {
                        data.errorMessage = $"Cannot retrieve {typeName} from {target.name}";
                    }
                    else
                    {
                        data.errorMessage = $"Path is not valid for {target.name}";
                        value = value.Replace(data.invalidPath, $"#%{data.invalidPath}%#", StringComparison.Ordinal);
                    }
                }

                if (ReflectionFactory.CurrentOptions.UseNiceNames)
                {
                    value = NicifyPath(value);
                    // Switch to arrows
                    separator = '→';
                }

                var lastSeparatorIndex = value.LastIndexOf(separator) + 1;
                if (lastSeparatorIndex > 0)
                {
                    _tempContent.text = value;

                    var lessImportantPart = value[..lastSeparatorIndex];
                    var moreImportantPart = value[lastSeparatorIndex..];
                    var formattedValue = $"{lessImportantPart}<b>{moreImportantPart}</b>";

                    data.formattedValue = formattedValue;
                    contents.formattedPath.tooltip = value.Replace("#%", "", StringComparison.Ordinal).Replace("%#", "", StringComparison.Ordinal);
                }
                else
                {
                    data.formattedValue = value;
                    contents.formattedPath.tooltip = string.Empty;
                }

                if (!string.IsNullOrEmpty(data.invalidPath))
                {
                    var emphasizer = " ";
                    data.invalidPath = null;
                    if (invalidType)
                    {
                        data.formattedValue = $"<color={Styles.errorColor}><b>[{typeName}]</b></color>{emphasizer}" + data.formattedValue;
                    }
                    else
                    {
                        var finalValue = data.formattedValue.Replace("#%", $"<color={Styles.errorColor}><b>", StringComparison.Ordinal);
                        finalValue = finalValue.Replace("%#", "</b></color>", StringComparison.Ordinal);
                        data.formattedValue = showType
                                            ? $"<color={Styles.warnColor}><b>[{typeName}]</b></color>{emphasizer}" + finalValue
                                            : finalValue;
                    }
                }
            }

            contents.formattedPath.text = data.formattedValue;
            return contents.formattedPath.text;
        }
    }
}