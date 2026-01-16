using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Postica.Common.Reflection
{
    /// <summary>
    /// Contains reflection utilities for accessing fields quickly.
    /// </summary>
    internal static class Reflect
    {
        public interface IFastFieldAccessor
        {
            Type FieldType { get; }
        }
        
        public interface IFastFieldAccessor<T> : IFastFieldAccessor
        {
            T GetValue(object source);
            void SetValue(object source, T value);
        }
        
        public interface IFastFieldAccessor<S, T> : IFastFieldAccessor where S : struct
        {
            T GetValue(ref S source);
            void SetValue(ref S source, T value);
        }
        
        [Preserve]
        [StructLayout(LayoutKind.Sequential)]
        private struct InstanceOf<T> where T : class
        {
            public T value;
        }
        
#if !POSTICA_FASTREFLECT_SUPPORTED
        [Preserve]
        [StructLayout(LayoutKind.Sequential)]
        private struct Wrapper
        {
            public object target;
        }
        
        private static unsafe void* GetPointer(this object obj)
        {
            var wrapper = new Wrapper
            {
                target = obj // 1. Copies the reference into the struct
            };

            // 2. Get the address of the STRUCT (which allows AddressOf)
            // 'ptrToWrapper' points to the stack location of the wrapper
            void* ptrToWrapper = UnsafeUtility.AddressOf(ref wrapper);

            // 3. Cast to IntPtr* to read the reference inside
            // Since 'target' is the first field, ptrToWrapper points exactly to the reference
            IntPtr heapAddress = *(IntPtr*)ptrToWrapper;

            return heapAddress.ToPointer();
        }
        
        private static unsafe void* GetStructPointer<S>(this S obj) where S : struct
        {
            // 1. Get the address of the REFERENCE to the struct
            void* addressOfReference = UnsafeUtility.AddressOf(ref obj);

            // 2. Cast it to an IntPtr pointer so we can read the value inside
            IntPtr* ptrToPtr = (IntPtr*)addressOfReference;

            // 3. Dereference it to get the actual Heap address of the object
            return ptrToPtr;
        }
#endif
        
        public sealed class FastFieldClassAccessor<S, T> : IFastFieldAccessor<S, T>, IFastFieldAccessor<T>
            where S : struct
            where T : class
        {
            private readonly int _offset;

            public FastFieldClassAccessor(int offset)
            {
                _offset = offset;
            }
            
            public Type FieldType => typeof(T);
            
            public T GetValue(object source)
            {
                unsafe
                {
                    var classPointer = source.GetPointer();
                    ref var fieldValue = ref UnsafeUtility.AsRef<InstanceOf<T>>((byte*)classPointer + _offset);
                    return fieldValue.value;
                }
            }
            
            public void SetValue(object source, T value)
            {
                unsafe
                {
                    var classPointer = source.GetPointer();
                    ref var fieldValue = ref UnsafeUtility.AsRef<InstanceOf<T>>((byte*)classPointer + _offset);
                    fieldValue.value = value;
                }
            }

            public T GetValue(ref S source)
            {
                unsafe
                {
                    var pointer = source.GetStructPointer();
                    ref var fieldValue = ref UnsafeUtility.AsRef<InstanceOf<T>>((byte*)pointer + _offset);
                    return fieldValue.value;
                }
            }
            
            public void SetValue(ref S source, T value)
            {
                unsafe
                {
                    var pointer = source.GetStructPointer();
                    ref var fieldValue = ref UnsafeUtility.AsRef<InstanceOf<T>>((byte*)pointer + _offset);
                    fieldValue.value = value;
                }
            }
        }
        
        public sealed class FastFieldAccessor<S, T> : IFastFieldAccessor<S, T>, IFastFieldAccessor<T>
            where S : struct
            where T : struct 
        {
            private readonly int _offset;

            public FastFieldAccessor(int offset)
            {
                _offset = offset;
            }
            
            public Type FieldType => typeof(T);
            
            public T GetValue(object source)
            {
                unsafe
                {
                    var classPointer = source.GetPointer();
                    ref var fieldValue = ref UnsafeUtility.AsRef<T>((byte*)classPointer + _offset);
                    return fieldValue;
                }
            }
            
            public void SetValue(object source, T value)
            {
                unsafe
                {
                    var classPointer = source.GetPointer();
                    ref var fieldValue = ref UnsafeUtility.AsRef<T>((byte*)classPointer + _offset);
                    fieldValue = value;
                }
            }

            public T GetValue(ref S source)
            {
                unsafe
                {
                    var pointer = source.GetStructPointer();
                    ref var fieldValue = ref UnsafeUtility.AsRef<T>((byte*)pointer + _offset);
                    return fieldValue;
                }
            }

            public void SetValue(ref S source, T value)
            {
                unsafe
                {
                    var pointer = source.GetStructPointer();
                    ref var fieldValue = ref UnsafeUtility.AsRef<T>((byte*)pointer + _offset);
                    fieldValue = value;
                }
            }
        }
        
        public sealed class FastFieldClassAccessor<T> : IFastFieldAccessor<T> where T : class
        {
            private readonly int _offset;

            public FastFieldClassAccessor(int offset)
            {
                _offset = offset;
            }
            
            public Type FieldType => typeof(T);

            public T GetValue(object source)
            {
                unsafe
                {
                    // var classPointer = UnsafeUtility.PinGCObjectAndGetAddress(source, out var handle);
                    var classPointer = source.GetPointer();
                    ref var fieldValue = ref UnsafeUtility.AsRef<InstanceOf<T>>((byte*)classPointer + _offset);
                    // UnsafeUtility.ReleaseGCObject(handle);
                    return fieldValue.value;
                }
            }
            
            public void SetValue(object source, T value)
            {
                unsafe
                {
                    // var classPointer = UnsafeUtility.PinGCObjectAndGetAddress(source, out var handle);
                    var classPointer = source.GetPointer();
                    ref var fieldValue = ref UnsafeUtility.AsRef<InstanceOf<T>>((byte*)classPointer + _offset);
                    fieldValue.value = value;
                    // UnsafeUtility.ReleaseGCObject(handle);
                }
            }
        }
        
        public sealed class FastFieldAccessor<T> : IFastFieldAccessor<T> where T : struct
        {
            private readonly int _offset;

            public FastFieldAccessor(int offset)
            {
                _offset = offset;
            }
            
            public Type FieldType => typeof(T);

            public T GetValue(object source)
            {
                unsafe
                {
                    // var classPointer = UnsafeUtility.PinGCObjectAndGetAddress(source, out var handle);
                    var classPointer = source.GetPointer();
                    ref var fieldValue = ref UnsafeUtility.AsRef<T>((byte*)classPointer + _offset);
                    // UnsafeUtility.ReleaseGCObject(handle);
                    return fieldValue;
                }
            }
            
            public void SetValue(object source, T value)
            {
                unsafe
                {
                    // var classPointer = UnsafeUtility.PinGCObjectAndGetAddress(source, out var handle);
                    var classPointer = source.GetPointer();
                    ref var fieldValue = ref UnsafeUtility.AsRef<T>((byte*)classPointer + _offset);
                    fieldValue = value;
                    // UnsafeUtility.ReleaseGCObject(handle);
                }
            }
        }
        
        public static bool IsBoxed<T>(T value)
        {
            return 
                (typeof(T) == typeof(object)) &&
                value != null &&
                value.GetType().IsValueType;
        }

        private static Type GetAccessorType(Type sourceType, Type valueType)
        {
            return (sourceType.IsValueType, valueType.IsValueType) switch
            {
                (true, true) => typeof(FastFieldAccessor<,>).MakeGenericType(sourceType, valueType),
                (true, false) => typeof(FastFieldClassAccessor<,>).MakeGenericType(sourceType, valueType),
                (false, true) => typeof(FastFieldAccessor<>).MakeGenericType(valueType),
                (false, false) => typeof(FastFieldClassAccessor<>).MakeGenericType(valueType),
            };
        }
        
        private static (FieldInfo fieldInfo, int offset) GetFieldOffset(Type type, string fieldName)
        {
            var fieldInfo = type.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var offset = UnsafeUtility.GetFieldOffset(fieldInfo);
            return (fieldInfo, offset);
        }
        
        public static IFastFieldAccessor GetFastFieldAccessor(Type sourceType, string fieldName)
        {
            var (fieldInfo, offset) = GetFieldOffset(sourceType, fieldName);
            var accessorType = GetAccessorType(sourceType, fieldInfo.FieldType);
            
            var accessor = (IFastFieldAccessor)Activator.CreateInstance(accessorType, offset);
            return accessor;
        }

        public static IFastFieldAccessor GetFastFieldAccessor(Type sourceType, string fieldName, string fieldName2)
        {
            var (fieldInfo, offset) = GetFieldOffset(sourceType, fieldName);
            var (fieldInfo2, offset2) = GetFieldOffset(fieldInfo.FieldType, fieldName2);
            var accessorType = GetAccessorType(sourceType, fieldInfo2.FieldType);
            
            var accessor = (IFastFieldAccessor)Activator.CreateInstance(accessorType, offset + offset2);
            return accessor;
        }
        
        public static IFastFieldAccessor GetFastFieldAccessor(Type sourceType, string fieldName, string fieldName2, string fieldName3)
        {
            var (fieldInfo, offset) = GetFieldOffset(sourceType, fieldName);
            var (fieldInfo2, offset2) = GetFieldOffset(fieldInfo.FieldType, fieldName2);
            var (fieldInfo3, offset3) = GetFieldOffset(fieldInfo2.FieldType, fieldName3);
            var accessorType = GetAccessorType(sourceType, fieldInfo3.FieldType);
            
            var accessor = (IFastFieldAccessor)Activator.CreateInstance(accessorType, offset + offset2 + offset3);
            return accessor;
        }
        
        public static IFastFieldAccessor GetFastFieldAccessor(Type sourceType, params string[] fieldNames)
        {
            if(fieldNames.Length == 0)
                throw new ArgumentException("Field names must not be empty", nameof(fieldNames));
            
            var offset = 0;
            var innerType = sourceType;
            for (var i = 0; i < fieldNames.Length; i++)
            {
                var (fieldInfo, offset_i) = GetFieldOffset(innerType, fieldNames[i]);
                offset += offset_i;
                innerType = fieldInfo.FieldType;
            }
            
            var accessorType = GetAccessorType(sourceType, innerType);
            
            var accessor = (IFastFieldAccessor)Activator.CreateInstance(accessorType, offset);
            return accessor;
        }

        #region [  CLASS SOURCE RELATED  ]
        
        public static class From<S> where S : class
        {
            public static FastFieldAccessor<T> Get<T>(string fieldName) where T : struct
                => GetFieldFromClass<T>(typeof(S), fieldName);

            public static FastFieldAccessor<T> Get<T>(string fieldName, string fieldName2)
                where T : struct
                => GetFieldFromClass<T>(typeof(S), fieldName, fieldName2);

            public static FastFieldAccessor<T> Get<T>(string fieldName, string fieldName2, string fieldName3)
                where T : struct
                => GetFieldFromClass<T>(typeof(S), fieldName, fieldName2, fieldName3);

            public static FastFieldAccessor<T> Get<T>(params string[] fieldNames)
                where T : struct
                => GetFieldFromClass<T>(typeof(S), fieldNames);

            public static FastFieldClassAccessor<T> GetRef<T>(string fieldName) where T : class
                => GetClassFieldFromClass<T>(typeof(S), fieldName);
            
            public static FastFieldClassAccessor<T> GetRef<T>(string fieldName, string fieldName2) where T : class
                => GetClassFieldFromClass<T>(typeof(S), fieldName, fieldName2);
            
            public static FastFieldClassAccessor<T> GetRef<T>(string fieldName, string fieldName2, string fieldName3) where T : class
                => GetClassFieldFromClass<T>(typeof(S), fieldName, fieldName2, fieldName3);
            
            public static FastFieldClassAccessor<T> GetRef<T>(params string[] fieldNames) where T : class
                => GetClassFieldFromClass<T>(typeof(S), fieldNames);
        }
        
        
        public static FastFieldAccessor<T> GetFieldFromClass<T>(Type sourceType, string fieldName) where T : struct
        {
            var (_, offset) = GetFieldOffset(sourceType, fieldName);
            var accessor = new FastFieldAccessor<T>(offset);
            return accessor;
        }

        public static FastFieldAccessor<T> GetFieldFromClass<T>(Type sourceType, string fieldName, string fieldName2) where T : struct
        {
            var (fieldInfo, offset) = GetFieldOffset(sourceType, fieldName);
            var (_, offset2) = GetFieldOffset(fieldInfo.FieldType, fieldName2);

            var accessor = new FastFieldAccessor<T>(offset + offset2);
            return accessor;
        }

        public static FastFieldAccessor<T> GetFieldFromClass<T>(Type sourceType, string fieldName, string fieldName2,
            string fieldName3) where T: struct
        {
            var (fieldInfo, offset) = GetFieldOffset(sourceType, fieldName);
            var (fieldInfo2, offset2) = GetFieldOffset(fieldInfo.FieldType, fieldName2);
            var (_, offset3) = GetFieldOffset(fieldInfo2.FieldType, fieldName3);

            var accessor = new FastFieldAccessor<T>(offset + offset2 + offset3);
            return accessor;
        }

        public static FastFieldAccessor<T> GetFieldFromClass<T>(Type sourceType, params string[] fieldNames) where T: struct
        {
            var offset = 0;
            for (var i = 0; i < fieldNames.Length; i++)
            {
                var (fieldInfo, offset_i) = GetFieldOffset(sourceType, fieldNames[i]);
                offset += offset_i;
                sourceType = fieldInfo.FieldType;
            }

            var accessor = new FastFieldAccessor<T>(offset);
            return accessor;
        }
        
        public static FastFieldClassAccessor<T> GetClassFieldFromClass<T>(Type sourceType, string fieldName) where T : class
        {
            var (_, offset) = GetFieldOffset(sourceType, fieldName);
            var accessor = new FastFieldClassAccessor<T>(offset);
            return accessor;
        }

        public static FastFieldClassAccessor<T> GetClassFieldFromClass<T>(Type sourceType, string fieldName, string fieldName2) where T : class
        {
            var (fieldInfo, offset) = GetFieldOffset(sourceType, fieldName);
            var (_, offset2) = GetFieldOffset(fieldInfo.FieldType, fieldName2);

            var accessor = new FastFieldClassAccessor<T>(offset + offset2);
            return accessor;
        }

        public static FastFieldClassAccessor<T> GetClassFieldFromClass<T>(Type sourceType, string fieldName, string fieldName2,
            string fieldName3) where T: class
        {
            var (fieldInfo, offset) = GetFieldOffset(sourceType, fieldName);
            var (fieldInfo2, offset2) = GetFieldOffset(fieldInfo.FieldType, fieldName2);
            var (_, offset3) = GetFieldOffset(fieldInfo2.FieldType, fieldName3);

            var accessor = new FastFieldClassAccessor<T>(offset + offset2 + offset3);
            return accessor;
        }

        public static FastFieldClassAccessor<T> GetClassFieldFromClass<T>(Type sourceType, params string[] fieldNames) where T: class
        {
            var offset = 0;
            for (var i = 0; i < fieldNames.Length; i++)
            {
                var (fieldInfo, offset_i) = GetFieldOffset(sourceType, fieldNames[i]);
                offset += offset_i;
                sourceType = fieldInfo.FieldType;
            }

            var accessor = new FastFieldClassAccessor<T>(offset);
            return accessor;
        }
        
        #endregion
        
        
        #region [  STRUCT SOURCE RELATED  ]
        
        public static class FromStruct<S> where S : struct
        {
            public static FastFieldAccessor<S, T> Get<T>(string fieldName) where T : struct
            {
                var (_, offset) = GetFieldOffset(typeof(S), fieldName);
                var accessor = new FastFieldAccessor<S, T>(offset);
                return accessor;
            }

            public static FastFieldAccessor<S, T> Get<T>(string fieldName, string fieldName2)
                where T : struct
            {
                var (fieldInfo, offset) = GetFieldOffset(typeof(S), fieldName);
                var (_, offset2) = GetFieldOffset(fieldInfo.FieldType, fieldName2);

                var accessor = new FastFieldAccessor<S, T>(offset + offset2);
                return accessor;
            }

            public static FastFieldAccessor<S, T> Get<T>(string fieldName, string fieldName2, string fieldName3)
                where T : struct
            {
                var (fieldInfo, offset) = GetFieldOffset(typeof(S), fieldName);
                var (fieldInfo2, offset2) = GetFieldOffset(fieldInfo.FieldType, fieldName2);
                var (_, offset3) = GetFieldOffset(fieldInfo2.FieldType, fieldName3);

                var accessor = new FastFieldAccessor<S, T>(offset + offset2 + offset3);
                return accessor;
            }

            public static FastFieldAccessor<S, T> Get<T>(params string[] fieldNames)
                where T : struct
            {
                var offset = 0;
                var sourceType = typeof(S);
                for (var i = 0; i < fieldNames.Length; i++)
                {
                    var (fieldInfo, offset_i) = GetFieldOffset(sourceType, fieldNames[i]);
                    offset += offset_i;
                    sourceType = fieldInfo.FieldType;
                }

                var accessor = new FastFieldAccessor<S, T>(offset);
                return accessor;
            }

            public static FastFieldClassAccessor<S, T> GetRef<T>(string fieldName) where T : class
            {
                var (_, offset) = GetFieldOffset(typeof(S), fieldName);
                var accessor = new FastFieldClassAccessor<S, T>(offset);
                return accessor;
            }
            
            public static FastFieldClassAccessor<S, T> GetRef<T>(string fieldName, string fieldName2) where T : class
            {
                var (fieldInfo, offset) = GetFieldOffset(typeof(S), fieldName);
                var (_, offset2) = GetFieldOffset(fieldInfo.FieldType, fieldName2);

                var accessor = new FastFieldClassAccessor<S, T>(offset + offset2);
                return accessor;
            }
            
            public static FastFieldClassAccessor<S, T> GetRef<T>(string fieldName, string fieldName2, string fieldName3) where T : class
            {
                var (fieldInfo, offset) = GetFieldOffset(typeof(S), fieldName);
                var (fieldInfo2, offset2) = GetFieldOffset(fieldInfo.FieldType, fieldName2);
                var (_, offset3) = GetFieldOffset(fieldInfo2.FieldType, fieldName3);

                var accessor = new FastFieldClassAccessor<S, T>(offset + offset2 + offset3);
                return accessor;
            }
            
            public static FastFieldClassAccessor<S, T> GetRef<T>(params string[] fieldNames) where T : class
            {
                var offset = 0;
                var sourceType = typeof(S);
                for (var i = 0; i < fieldNames.Length; i++)
                {
                    var (fieldInfo, offset_i) = GetFieldOffset(sourceType, fieldNames[i]);
                    offset += offset_i;
                    sourceType = fieldInfo.FieldType;
                }

                var accessor = new FastFieldClassAccessor<S, T>(offset);
                return accessor;
            }
        }
        
        #endregion
    }
}