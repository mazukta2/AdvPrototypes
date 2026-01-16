using System.Text.RegularExpressions;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.ProxyBinding
{
    internal static class ProxyBindingFixer
    {
        private static bool _initialized;
        private static Regex _regex = new Regex(@"bindings\.Array\.data\[(\d+)\]", RegexOptions.Compiled);
        
        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            
            ProxyBindings.MakeDirty -= PersistProxies;
            ProxyBindings.MakeDirty += PersistProxies;
            ProxyBindingsAsset.MakeDirty -= PersistProxies;
            ProxyBindingsAsset.MakeDirty += PersistProxies;
        }

        private static void PersistProxies(IBindProxyProvider provider)
        {
            if(provider is not Object obj)
            {
                return;
            }

            using var serObj = new SerializedObject(obj);
            serObj.Update();
            var serProp = serObj.FindProperty("bindings");
            for (int i = 0; i < serProp.arraySize; i++)
            {
                var element = serProp.GetArrayElementAtIndex(i);
                var ppathProperty = element.FindPropertyRelative("_bindData._ppath");
                if(ppathProperty == null)
                {
                    continue;
                }
                
                ppathProperty.stringValue = FixPath(i, ppathProperty.stringValue);
            }
            
            serObj.ApplyModifiedProperties();
        }

        private static string FixPath(int i, string path)
        {
            var replacement = "bindings.Array.data[" + i + "]";
            return _regex.Replace(path, replacement);
        }
    }
}