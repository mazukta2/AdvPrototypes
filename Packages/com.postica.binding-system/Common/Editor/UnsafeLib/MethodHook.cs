/*
 Desc: A tool that can hook Mono methods at runtime, allowing you to override their functionality without modifying UnityEditor.dll and other files
 Author: Misaka Mikoto
 Github: https://github.com/Misaka-Mikoto-Tech/MonoHook
 */

using DotNetDetour;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace MonoHook
{
    /// <summary>
    /// Hook class, used to hook a C# method
    /// </summary>
    public unsafe class MethodHook
    {
        public string tag;
        public bool isHooked { get; private set; }
        public bool isPlayModeHook { get; private set; }

        public MethodBase targetMethod { get; private set; }       // The target method to be hooked
        public MethodBase replacementMethod { get; private set; }  // The replacement method after being hooked
        public MethodBase proxyMethod { get; private set; }        // The proxy method for the target method (can call the original method after being hooked)

        private IntPtr _targetPtr;                  // The address pointer after the target method is JIT-compiled
        private IntPtr _replacementPtr;
        private IntPtr _proxyPtr;
        
        private Delegate targetDelegate;
        private Delegate replacementDelegate;
        private Delegate proxyDelegate;

        private CodePatcher _codePatcher;
        
        private readonly object _lock = new object();

        /// <summary>
        /// Creates a Hook
        /// </summary>
        /// <param name="targetMethod">The target method to be replaced</param>
        /// <param name="replacementMethod">The prepared replacement method</param>
        /// <param name="proxyMethod">If the original target method still needs to be called, it can be done through this method. If not needed, it can be null</param>
        public MethodHook(MethodBase targetMethod, MethodBase replacementMethod, MethodBase proxyMethod, string data = "")
        {
            this.targetMethod       = targetMethod;
            this.replacementMethod  = replacementMethod;
            this.proxyMethod        = proxyMethod;
            this.tag = data;

            CheckMethod();
            
            #if UNITY_EDITOR
            targetDelegate = ToDelegate(targetMethod);
            replacementDelegate = ToDelegate(replacementMethod);
            if (proxyMethod != null)
            {
                proxyDelegate = ToDelegate(proxyMethod);
            }
            #endif
        }

        private static Delegate ToDelegate(MethodBase method)
        {
            try
            {
                if (method is not MethodInfo targetMethod)
                {
                    return null;
                }
                var targetMethodParameters = targetMethod.GetParameters();
                var delegateType = targetMethod.ReturnType == typeof(void)
                    ? Expression.GetActionType(targetMethodParameters.Select(p => p.ParameterType).ToArray())
                    : Expression.GetFuncType(targetMethodParameters.Select(p => p.ParameterType)
                        .Append(targetMethod.ReturnType).ToArray());
                return Delegate.CreateDelegate(delegateType, targetMethod);
            }
#if ENABLE_HOOK_DEBUG
            catch (Exception e)
            {
                Debug.LogError($"Failed to create delegate for method {targetMethod.Name}: {e}");
#else
            catch
            {
#endif
                return null;
            }
        }

        public void Install()
        {
            if (LDasm.IsiOS()) // iOS does not support modifying the code region page
                return;

            if (isHooked)
                return;

            lock (_lock)
            {
                DoInstall();
            }

#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= Uninstall;
            AssemblyReloadEvents.beforeAssemblyReload += Uninstall;
#endif
            
            isPlayModeHook = Application.isPlaying;
        }

        public void Uninstall()
        {
            if (!isHooked)
                return;

            lock (_lock)
            {
                _codePatcher.RemovePatch();
            }

            isHooked = false;
            HookPool.RemoveHooker(targetMethod);
        }

        #region [  Private Methods  ]
        private void DoInstall()
        {
            if (targetMethod == null || replacementMethod == null)
                throw new Exception("None of methods targetMethod or replacementMethod can be null");

            HookPool.AddHook(targetMethod, this);

            if (_codePatcher == null)
            {
                if (GetFunctionAddr())
                {
#if ENABLE_HOOK_DEBUG
                    UnityEngine.Debug.Log($"Original [{targetMethod.DeclaringType.Name}.{targetMethod.Name}]: {HookUtils.HexToString(_targetPtr.ToPointer(), 64, -16)}");
                    UnityEngine.Debug.Log($"Original [{replacementMethod.DeclaringType.Name}.{replacementMethod.Name}]: {HookUtils.HexToString(_replacementPtr.ToPointer(), 64, -16)}");
                    if(proxyMethod != null)
                        UnityEngine.Debug.Log($"Original [{proxyMethod.DeclaringType.Name}.{proxyMethod.Name}]: {HookUtils.HexToString(_proxyPtr.ToPointer(), 64, -16)}");
#endif

                    CreateCodePatcher();
                    _codePatcher.ApplyPatch();

#if ENABLE_HOOK_DEBUG
                    UnityEngine.Debug.Log($"New [{targetMethod.DeclaringType.Name}.{targetMethod.Name}]: {HookUtils.HexToString(_targetPtr.ToPointer(), 64, -16)}");
                    UnityEngine.Debug.Log($"New [{replacementMethod.DeclaringType.Name}.{replacementMethod.Name}]: {HookUtils.HexToString(_replacementPtr.ToPointer(), 64, -16)}");
                    if(proxyMethod != null)
                        UnityEngine.Debug.Log($"New [{proxyMethod.DeclaringType.Name}.{proxyMethod.Name}]: {HookUtils.HexToString(_proxyPtr.ToPointer(), 64, -16)}");
#endif
                }
            }

            isHooked = true;
        }

        private void CheckMethod()
        {
            if (targetMethod == null || replacementMethod == null)
                throw new Exception("MethodHook:targetMethod and replacementMethod and proxyMethod can not be null");

            string methodName = $"{targetMethod.DeclaringType.Name}.{targetMethod.Name}";
            if (targetMethod.IsAbstract)
                throw new Exception($"WARNING: you can not hook abstract method [{methodName}]");

#if UNITY_EDITOR
            int minMethodBodySize = 10;

            {
                if ((targetMethod.MethodImplementationFlags & MethodImplAttributes.InternalCall) != MethodImplAttributes.InternalCall)
                {
                    int codeSize = targetMethod.GetMethodBody().GetILAsByteArray().Length; // GetMethodBody can not call on il2cpp
                    if (codeSize < minMethodBodySize)
                    {
#if ENABLE_HOOK_DEBUG
                        Debug.LogWarning(
                            $"WARNING: you can not hook method [{methodName}], cause its method body is too short({codeSize}), will random crash on IL2CPP release mode");
#endif
                    }
                }
            }

            if(proxyMethod != null)
            {
                methodName = $"{proxyMethod.DeclaringType.Name}.{proxyMethod.Name}";
                int codeSize = proxyMethod.GetMethodBody().GetILAsByteArray().Length;
                if (codeSize < minMethodBodySize)
                {
#if ENABLE_HOOK_DEBUG
                    UnityEngine.Debug.LogWarning(
                        $"WARNING: size of method body[{methodName}] is too short({codeSize}), will random crash on IL2CPP release mode, please fill some dummy code inside");
#endif
                }

                if ((proxyMethod.MethodImplementationFlags & MethodImplAttributes.NoOptimization) !=
                    MethodImplAttributes.NoOptimization)
                {
                    throw new Exception(
                        $"WARNING: method [{methodName}] must has a Attribute `MethodImpl(MethodImplOptions.NoOptimization)` to prevent code call to this optimized by compiler(pass args by shared stack)");
                }
            }
#endif
        }

        private void CreateCodePatcher()
        {
            long addrOffset = Math.Abs(_targetPtr.ToInt64() - _proxyPtr.ToInt64());

            if (_proxyPtr != IntPtr.Zero)
            {
                addrOffset = Math.Max(addrOffset, Math.Abs(_targetPtr.ToInt64() - _proxyPtr.ToInt64()));
            }

            if (LDasm.IsARM())
            {
                if (IntPtr.Size == 8)
                {
                    if (addrOffset < ((1 << 26) - 1))
                    {
#if ENABLE_HOOK_DEBUG
                        Debug.Log("Apply CodePatcher_arm64_near");
#endif
                        _codePatcher = new CodePatcher_arm64_near(_targetPtr, _replacementPtr, _proxyPtr);
                    }
                    // 4GB of instruction space
                    else if (addrOffset < 0xFFFFFFFF)
                    {
#if ENABLE_HOOK_DEBUG
                        Debug.Log($"Apply CodePatcher_arm64_far: {addrOffset}");
#endif
                        _codePatcher = new CodePatcher_arm64_far(_targetPtr, _replacementPtr, _proxyPtr);
                    }
                    else
                    {
                        throw new Exception(
                            "Address of target method and replacement method are too far, can not hook");
                    }
                }
                else if (addrOffset < ((1 << 25) - 1))
                {
                    _codePatcher = new CodePatcher_arm32_near(_targetPtr, _replacementPtr, _proxyPtr);
                }
                else if (addrOffset < ((1 << 27) - 1))
                {
                    _codePatcher = new CodePatcher_arm32_far(_targetPtr, _replacementPtr, _proxyPtr);
                }
                else throw new Exception("Address of target method and replacement method are too far, can not hook");
            }
            else
            {
                if (IntPtr.Size == 8)
                {
                    if(addrOffset < 0x7fffffff) // 2G
                    {
                        _codePatcher = new CodePatcher_x64_near(_targetPtr, _replacementPtr, _proxyPtr);
                    }
                    else
                    {
                        _codePatcher = new CodePatcher_x64_far(_targetPtr, _replacementPtr, _proxyPtr);
                    }
                }
                else
                {
                    _codePatcher = new CodePatcher_x86(_targetPtr, _replacementPtr, _proxyPtr);
                }
            }
        }

        /// <summary>
        /// Get the native code address of the corresponding JIT-ed function
        /// </summary>
        private bool GetFunctionAddr()
        {
            _targetPtr = GetFunctionAddr(targetMethod);
            _replacementPtr = GetFunctionAddr(replacementMethod);
            _proxyPtr = GetFunctionAddr(proxyMethod);

            if (_targetPtr == IntPtr.Zero || _replacementPtr == IntPtr.Zero) return false;

            if (proxyMethod != null && _proxyPtr == null) return false;

            if(_replacementPtr == _targetPtr)
            {
                throw new Exception($"The addresses of target method {targetMethod.Name} and replacement method {replacementMethod.Name} can not be the same");
            }

            if (LDasm.IsThumb(_targetPtr) || LDasm.IsThumb(_replacementPtr))
            {
                throw new Exception("System doesn't support thumb arch");
            }

            return true;
        }
        
        /// <summary>
        /// Get the method instruction address
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private IntPtr GetFunctionAddr(MethodBase method)
        {
            if (method == null)
            {
                return IntPtr.Zero;
            }

            RuntimeHelpers.PrepareMethod(method.MethodHandle);
            return method.MethodHandle.GetFunctionPointer();
        }

        #endregion
    }

}
