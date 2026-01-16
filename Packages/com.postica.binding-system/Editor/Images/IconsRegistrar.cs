using Postica.BindingSystem.Accessors;
using Postica.Common;
using System;
using Postica.BindingSystem.PinningLogic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Postica.BindingSystem
{
    static class IconsRegistrar
    {
        private static bool _initialized;

        internal static void Invalidate() => _initialized = false;
        
        internal static void RegisterBasicTypes()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            ObjectIcon.RegisterOnInitialize(() =>
            {
                ObjectIcon.RegisterIconFor<char>(Icons.CharIcon.MultiplyBy(Color.green));
                ObjectIcon.RegisterIconFor<bool>(Icons.BooleanIcon);
                ObjectIcon.RegisterIconFor<byte>(Icons.ByteIcon);
                ObjectIcon.RegisterIconFor<ushort>(Icons.Int16Icon);
                ObjectIcon.RegisterIconFor<short>(Icons.Int16Icon);
                ObjectIcon.RegisterIconFor<uint>(Icons.Int32Icon);
                ObjectIcon.RegisterIconFor<int>(Icons.Int32Icon);
                ObjectIcon.RegisterIconFor<ulong>(
                    Icons.Int64Icon.MultiplyBy(Color.cyan.Red(0.25f).Green(0.75f).WithAlpha(1f)));
                ObjectIcon.RegisterIconFor<long>(Icons.Int64Icon);
                ObjectIcon.RegisterIconFor<float>(Icons.FloatIcon);
                ObjectIcon.RegisterIconFor<double>(Icons.DoubleIcon);
                ObjectIcon.RegisterIconFor<string>(Icons.StringIcon);
                ObjectIcon.RegisterIconFor<Vector2>(Icons.Vector2Icon);
                ObjectIcon.RegisterIconFor<Vector3>(Icons.Vector3Icon);
                ObjectIcon.RegisterIconFor<Vector4>(Icons.Vector4Icon);
                ObjectIcon.RegisterIconFor<Quaternion>(Icons.QuaternionIcon);
                ObjectIcon.RegisterIconFor<Color>(Icons.ColorIcon);
                ObjectIcon.RegisterIconFor<Scene>(Resources.Load<Texture2D>("_bsicons/types/unity-scene"));
                ObjectIcon.RegisterIconFor<PinnedPath>(Resources.Load<Texture2D>("_bsicons/types/pinned"));

                ObjectIcon.RegisterIconFor<IAccessorProvider>(Icons.ProvidersIcon);

                ObjectIcon.RegisterEnumIcon(Icons.EnumIcon);
                ObjectIcon.RegisterDefaultIcon(Resources.Load<Texture2D>("_bsicons/types/generic"));
            });

            ObjectIcon.RegisterResolver(TryGetBindIcon);
            ObjectIcon.RegisterResolver(TryGetEventIcon);
            ObjectIcon.RegisterResolver(TryGetDefaultModifierIcon);
        }

        private static bool TryGetBindIcon(Type type, out Texture2D icon)
        {
            if (typeof(IBind).IsAssignableFrom(type))
            {
                icon = Icons.BindIcon_Lite_On;
                return true;
            }

            icon = null;
            return false;
        }

        private static bool TryGetEventIcon(Type type, out Texture2D icon)
        {
            if (typeof(UnityEventBase).IsAssignableFrom(type))
            {
                icon = Resources.Load<Texture2D>("_bsicons/types/event");
                return true;
            }

            icon = null;
            return false;
        }

        private static bool TryGetDefaultModifierIcon(Type type, out Texture2D icon)
        {
            if (typeof(IModifier).IsAssignableFrom(type))
            {
                icon = Resources.Load<Texture2D>("_bsicons/types/modifier");
                return true;
            }

            icon = null;
            return false;
        }
    }
}