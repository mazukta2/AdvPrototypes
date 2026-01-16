#if (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX)
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace MonoHook
{
    public static unsafe class HookUtils
    {
        static bool jit_write_protect_supported;
        private static readonly long _Pagesize;


        static HookUtils()
        {
            try
            {
                jit_write_protect_supported = pthread_jit_write_protect_supported_np() != 0;
            }
            catch
            {
                // ignored
            }

            PropertyInfo systemPageSize = typeof(Environment).GetProperty("SystemPageSize");
            if (systemPageSize == null)
            {
                throw new NotSupportedException("Unsupported runtime");
            }

            _Pagesize = (int)systemPageSize.GetValue(null, Array.Empty<object>());
        }

        public static void MemCpy(void* pDst, void* pSrc, int len)
        {
            byte* pDst_ = (byte*)pDst;
            byte* pSrc_ = (byte*)pSrc;

            for (int i = 0; i < len; i++)
                *pDst_++ = *pSrc_++;
        }

        public static void MemCpy_Jit(void* pDst, byte[] src)
        {
            if (!jit_write_protect_supported)
            {
                fixed(void * pSrc = &src[0])
                {
                    MemCpy(pDst, pSrc, src.Length);
                }

                return;
            }

            fixed(void * p = &src[0])
            {
                memcpy_jit(new IntPtr(pDst), new IntPtr(p), src.Length);
            }
        }

        /// <summary>
        /// Set flags of address to `read write execute`
        /// </summary>
        public static void SetAddrFlagsToRWX(IntPtr ptr, int size) { }

        public static void FlushICache(void* code, int size)
        {
            IntPtr codeStart = new IntPtr(code);
            IntPtr codeEnd = new IntPtr((byte*)code + size);
            __clear_cache(codeStart, codeEnd);
        }

        public static KeyValuePair<long, long> GetPageAlignedAddr(long code, int size)
        {
            long pagesize = _Pagesize;
            long startPage = (code) & ~(pagesize - 1);
            long endPage = (code + size + pagesize - 1) & ~(pagesize - 1);
            return new KeyValuePair<long, long>(startPage, endPage);
        }


        const int PRINT_SPLIT = 4;
        const int PRINT_COL_SIZE = PRINT_SPLIT * 4;
        public static string HexToString(void* ptr, int size, int offset = 0)
        {
            Func<IntPtr, string> formatAddr = (IntPtr addr__) => IntPtr.Size == 4 ? $"0x{(uint)addr__:x}" : $"0x{(ulong)addr__:x}";

            byte* addr = (byte*)ptr;

            StringBuilder sb = new StringBuilder(1024);
            sb.AppendLine($"addr:{formatAddr(new IntPtr(addr))}");

            addr += offset;
            size += Math.Abs(offset);

            int count = 0;
            var stillAppend = true;
            while (stillAppend)
            {
                sb.Append($"\r\n{formatAddr(new IntPtr(addr + count))}: ");
                for (int i = 1; i < PRINT_COL_SIZE + 1; i++)
                {
                    if (count >= size)
                    {
                        stillAppend = false;
                        break;
                    }

                    sb.Append($"{*(addr + count):x2}");
                    if (i % PRINT_SPLIT == 0)
                        sb.Append(" ");

                    count++;
                }
            }
            
            return sb.ToString();
        }

        [DllImport("pthread", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern int pthread_jit_write_protect_supported_np();
        
        [DllImport("pthread", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern int pthread_jit_write_protect_np(int enable);

        [DllImport("hookOSXlib", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr memcpy_jit(IntPtr dst, IntPtr src, int len);
        
        [DllImport("libc")]
        private static extern int __clear_cache(IntPtr begin, IntPtr end);
    }
}

#endif