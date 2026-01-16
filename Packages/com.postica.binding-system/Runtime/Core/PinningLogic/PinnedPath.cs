using System;
using Postica.Common;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.PinningLogic
{
    [Serializable]
    public struct PinnedPath : IEquatable<PinnedPath>
    {
        [Flags]
        public enum BitFlags
        {
            None = 0,
            PinChildren = 1 << 0,
        }
        
        
        public Object context;
        public string path;
        public string rawPath;
        public SerializedType type;
        public BitFlags flags;
        
        public bool IsRootPath => path == "/";
        
        public bool IsValid => !string.IsNullOrEmpty(path);
        
        public PinnedPath(Object context, string path, SerializedType type)
        {
            this.context = context;
            this.path = MakeRuntimePath(context, path) ;
            this.type = type;
            this.rawPath = path;
            flags = BitFlags.None;
        }
        
        public PinnedPath WithFlags(BitFlags flags)
        {
            this.flags = flags;
            return this;
        }

        private static string MakeRuntimePath(Object context, string path)
        {
            if (path.Length <= 1)
            {
                return path;
            }
            
            var cleanPath = context.GetType().TryMakeUnityRuntimePath(path, out var runtimePath, deepSearch: true) 
                ? runtimePath.Replace(".Array.data", "") 
                : path.Replace(".Array.data", "");

            try
            {
                return AccessorsFactory.NormalizePath(context, cleanPath);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to normalize path '{path}' for object '{context}' of type '{context.GetType()}'.", e);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is PinnedPath other)
            {
                return context == other.context && path == other.path;
            }

            return false;
        }
        
        public override int GetHashCode()
        {
            return context.GetHashCode() ^ path.GetHashCode();
        }

        public bool Equals(PinnedPath other)
        {
            return Equals(context, other.context) && path == other.path;
        }
        
        public static bool operator ==(PinnedPath left, PinnedPath right)
        {
            return left.Equals(right);
        }
        
        public static bool operator !=(PinnedPath left, PinnedPath right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{context} -> {path}";
        }
    }
}
