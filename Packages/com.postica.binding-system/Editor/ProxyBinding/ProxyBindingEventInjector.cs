using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using MonoHook;
using Postica.BindingSystem.Reflection;
using Postica.Common;
using Postica.Common.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.ProxyBinding
{
    internal static class ProxyBindingEventInjector
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            EditorApplication.update += HookToEventDrawer;
        }

        private static void HookToEventDrawer()
        {
            EditorApplication.update -= HookToEventDrawer;
            
            var originalMethod = typeof(UnityEventDrawer).GetMethod("GeneratePopUpForType", BindingFlags.Static | BindingFlags.NonPublic);
            var replacementMethod = typeof(ProxyBindingEventInjector).GetMethod(nameof(GeneratePopUpForType), BindingFlags.NonPublic | BindingFlags.Static);
            var baseReplacementMethod = typeof(ProxyBindingEventInjector).GetMethod(nameof(BaseGeneratePopUpForType), BindingFlags.NonPublic | BindingFlags.Static);
            
            var hook = new MethodHook(originalMethod, replacementMethod, baseReplacementMethod);
            hook.Install();

            AssemblyReloadEvents.beforeAssemblyReload += hook.Uninstall;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void GeneratePopUpForType(
            GenericMenu menu,
            Object target,
            string targetName,
            SerializedProperty listener,
            Type[] delegateArgumentsTypes)
        {
            if (target is IBindProxyProvider and Component)
            {
                return;
            }
            
            var eventData = new EventData(listener);
            
            IBindProxyProvider bindings;
            if(target.TryGetGameObject(out var go))
            {
                bindings = go.GetComponent<IBindProxyProvider>();
            }
            else
            {
                var path = AssetDatabase.GetAssetPath(target);
                bindings = AssetDatabase.LoadAllAssetsAtPath(path).FirstOrDefault(a => a is IBindProxyProvider) as IBindProxyProvider;
                // This is to avoid a strange bug where assets commands will not load
                if (target is IBindProxyProvider targetProxy 
                    && eventData.methodName == "UpdateProxy"
                    && targetProxy.TryGetProxy(eventData.argument, out var proxy, out _)
                    && proxy.Source)
                {
                    target = proxy.Source;
                    targetName = target.GetType().Name;
                }
            }
            
            if (bindings is not Object bindingsObject)
            {
                BaseGeneratePopUpForType(menu, target, targetName, listener, delegateArgumentsTypes);
                return;
            }

            if (!bindings.TryGetProxiesInTree(target, "", out var proxies)
                || proxies == null
                || proxies.Count == 0)
            {
                BaseGeneratePopUpForType(menu, target, targetName, listener, delegateArgumentsTypes);
                return;
            }

            var activeBindings = proxies.Where(p => p.proxy.IsBound);
            
            if(!activeBindings.Any())
            {
                BaseGeneratePopUpForType(menu, target, targetName, listener, delegateArgumentsTypes);
                return;
            }
            
            var updateMethod = bindings.GetType().GetMethod("UpdateProxy", BindingFlags.Instance | BindingFlags.Public);
            if (updateMethod == null)
            {
                throw new Exception("Method UpdateProxy not found in IBindProxyProvider");
            }

            menu.AddSeparator(targetName + "/Update Bindings");
            foreach (var (proxy, index) in activeBindings)
            {
                // Need to nicify the path first
                var pieces = proxy.Path.Split('/', '.');
                var path = string.Join('\u2192', pieces.Select(p => p.NiceName()));
                var typename = proxy.ValueType?.UserFriendlyName();
                var proxyId = bindings.GetProxyId(proxy);
                var isChecked = eventData.target == bindingsObject && eventData.methodName == updateMethod.Name && eventData.argument == proxyId; 
                menu.AddItem(new GUIContent($"{targetName}/{typename} {path}"), isChecked, () =>
                {
                    eventData.Assign(bindingsObject, updateMethod, proxyId);
                });
            }
            
            menu.AddSeparator(targetName + "/");
            if (delegateArgumentsTypes.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent(targetName + "/Static Parameters"));
            }
            BaseGeneratePopUpForType(menu, target, targetName, listener, delegateArgumentsTypes);
        }

        private struct EventData
        {
            public readonly SerializedProperty listener;

            public Object target
            {
                get => listener.FindPropertyRelative("m_Target").objectReferenceValue;
                set => listener.FindPropertyRelative("m_Target").objectReferenceValue = value;
            }

            public string targetAssemblyTypeName
            {
                get => listener.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue;
                set => listener.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = value;
            }

            public string methodName
            {
                get => listener.FindPropertyRelative("m_MethodName").stringValue;
                set => listener.FindPropertyRelative("m_MethodName").stringValue = value;
            }

            public PersistentListenerMode mode
            {
                get => (PersistentListenerMode)listener.FindPropertyRelative("m_Mode").enumValueIndex;
                set => listener.FindPropertyRelative("m_Mode").enumValueIndex = (int)value;
            }
            
            public string argument
            {
                get => listener.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue;
                set => listener.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue = value;
            }
            
            public EventData(SerializedProperty listener)
            {
                this.listener = listener;
            }

            public void Assign(Object newTarget, MethodInfo method, string newArgument)
            {
                target = newTarget;
                targetAssemblyTypeName = method.DeclaringType.AssemblyQualifiedName;
                methodName = method.Name;
                mode = PersistentListenerMode.String;
                argument = newArgument;
                listener.serializedObject.ApplyModifiedProperties();
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static void BaseGeneratePopUpForType(
            GenericMenu menu,
            Object target,
            string targetName,
            SerializedProperty listener,
            Type[] delegateArgumentsTypes)
        {
            Debug.Log(BindSystem.DebugPrefix + "If you see this message, hook system failed! Contact developer.");
        }
    }
}