using DotNetDetour;
using System;

namespace MonoHook
{
    /// <summary>
    /// This class is used to patch the code at a specific address with a jump to a new address.
    /// </summary>
    public abstract unsafe class CodePatcher
    {
        public bool isValid { get; protected set; }

        protected void* _pTarget, _pReplace, _pProxy;
        protected int _jmpCodeSize;
        protected byte[] _targetHeaderBackup;

        public CodePatcher(IntPtr target, IntPtr replace, IntPtr proxy, int jmpCodeSize)
        {
            _pTarget = target.ToPointer();
            _pReplace = replace.ToPointer();
            _pProxy = proxy.ToPointer();
            _jmpCodeSize = jmpCodeSize;
        }

        public void ApplyPatch()
        {
            BackupHeader();
            EnableAddrModifiable();
            PatchTargetMethod();
            PatchProxyMethod();
            FlushICache();
        }

        public void RemovePatch()
        {
            if (_targetHeaderBackup == null)
                return;

            EnableAddrModifiable();
            RestoreHeader();
            FlushICache();
        }

        protected void BackupHeader()
        {
            if (_targetHeaderBackup != null)
                return;

            uint requireSize = LDasm.SizeofMinNumByte(_pTarget, _jmpCodeSize);
            _targetHeaderBackup = new byte[requireSize];

            fixed (void* ptr = _targetHeaderBackup)
                HookUtils.MemCpy(ptr, _pTarget, _targetHeaderBackup.Length);
        }

        protected void RestoreHeader()
        {
            if (_targetHeaderBackup == null)
                return;

            HookUtils.MemCpy_Jit(_pTarget, _targetHeaderBackup);
        }

        protected void PatchTargetMethod()
        {
            byte[] buff = GenJmpCode(_pTarget, _pReplace);
            HookUtils.MemCpy_Jit(_pTarget, buff);
        }

        protected void PatchProxyMethod()
        {
            if (_pProxy == null)
                return;

            // copy target's code to proxy
            HookUtils.MemCpy_Jit(_pProxy, _targetHeaderBackup);

            // jmp to target's new position
            long jmpFrom = (long)_pProxy + _targetHeaderBackup.Length;
            long jmpTo = (long)_pTarget + _targetHeaderBackup.Length;

            byte[] buff = GenJmpCode((void*)jmpFrom, (void*)jmpTo);
            HookUtils.MemCpy_Jit((void*)jmpFrom, buff);
        }

        protected void FlushICache()
        {
            HookUtils.FlushICache(_pTarget, _targetHeaderBackup.Length);
            HookUtils.FlushICache(_pProxy, _targetHeaderBackup.Length * 2);
        }

        protected abstract byte[] GenJmpCode(void* jmpFrom, void* jmpTo);

#if ENABLE_HOOK_DEBUG
        protected string PrintAddrs()
        {
            if (IntPtr.Size == 4)
                return $"target:0x{(uint)_pTarget:x}, replace:0x{(uint)_pReplace:x}, proxy:0x{(uint)_pProxy:x}";
            else
                return $"target:0x{(ulong)_pTarget:x}, replace:0x{(ulong)_pReplace:x}, proxy:0x{(ulong)_pProxy:x}";
        }
#endif

        private void EnableAddrModifiable()
        {
            HookUtils.SetAddrFlagsToRWX(new IntPtr(_pTarget), _targetHeaderBackup.Length);
            HookUtils.SetAddrFlagsToRWX(new IntPtr(_pProxy), _targetHeaderBackup.Length + _jmpCodeSize);
        }
    }

    public unsafe class CodePatcher_x86 : CodePatcher
    {
        protected static readonly byte[] s_jmpCode = new byte[] // 5 bytes
        {
            0xE9, 0x00, 0x00, 0x00, 0x00, // jmp $val   ; $val = $dst - $src - 5 
        };

        public CodePatcher_x86(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy,
            s_jmpCode.Length)
        {
        }

        protected override unsafe byte[] GenJmpCode(void* jmpFrom, void* jmpTo)
        {
            byte[] ret = new byte[s_jmpCode.Length];
            int val = (int)jmpTo - (int)jmpFrom - 5;

            fixed (void* p = &ret[0])
            {
                byte* ptr = (byte*)p;
                *ptr = 0xE9;
                int* pOffset = (int*)(ptr + 1);
                *pOffset = val;
            }

            return ret;
        }
    }

    /// <summary>
    /// Jump within 2G in x64
    /// </summary>
    public unsafe class CodePatcher_x64_near : CodePatcher_x86 // x64_near patcher code is same to x86
    {
        public CodePatcher_x64_near(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy)
        {
        }
    }

    /// <summary>
    /// Jump beyond 2G in x64
    /// </summary>
    public unsafe class CodePatcher_x64_far : CodePatcher
    {
        protected static readonly byte[] s_jmpCode = new byte[] // 12 bytes
        {
            // Since rax is used as a return value by functions and not as a parameter, modifying it is safe
            0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // mov rax, <jmpTo>
            0x50, // push rax
            0xC3 // ret
        };

        //protected static readonly byte[] s_jmpCode2 = new byte[] // 14 bytes
        //{
        //    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,       // <jmpTo>
        //    0xFF, 0x25, 0xF2, 0xFF, 0xFF, 0xFF                    // jmp [rip - 0xe]
        //};

        public CodePatcher_x64_far(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy,
            s_jmpCode.Length)
        {
        }

        protected override unsafe byte[] GenJmpCode(void* jmpFrom, void* jmpTo)
        {
            byte[] ret = new byte[s_jmpCode.Length];

            fixed (void* p = &ret[0])
            {
                byte* ptr = (byte*)p;
                *ptr++ = 0x48;
                *ptr++ = 0xB8;
                *(long*)ptr = (long)jmpTo;
                ptr += 8;
                *ptr++ = 0x50;
                *ptr++ = 0xC3;
            }

            return ret;
        }
    }

    public unsafe class CodePatcher_arm32_near : CodePatcher
    {
        private static readonly byte[] s_jmpCode = new byte[] // 4 bytes
        {
            0x00, 0x00, 0x00, 0xEA, // B $val   ; $val = (($dst - $src) / 4 - 2) & 0x1FFFFFF
        };

        public CodePatcher_arm32_near(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy,
            s_jmpCode.Length)
        {
            if (Math.Abs((long)target - (long)replace) >= ((1 << 25) - 1))
                throw new ArgumentException("address offset of target and replace must less than ((1 << 25) - 1)");

#if ENABLE_HOOK_DEBUG
            UnityEngine.Debug.Log($"CodePatcher_arm32_near: {PrintAddrs()}");
#endif
        }

        protected override unsafe byte[] GenJmpCode(void* jmpFrom, void* jmpTo)
        {
            byte[] ret = new byte[s_jmpCode.Length];
            int val = ((int)jmpTo - (int)jmpFrom) / 4 - 2;

            fixed (void* p = &ret[0])
            {
                byte* ptr = (byte*)p;
                *ptr++ = (byte)val;
                *ptr++ = (byte)(val >> 8);
                *ptr++ = (byte)(val >> 16);
                *ptr++ = 0xEA;
            }

            return ret;
        }
    }

    public unsafe class CodePatcher_arm32_far : CodePatcher
    {
        private static readonly byte[] s_jmpCode = new byte[] // 8 bytes
        {
            0x04, 0xF0, 0x1F, 0xE5, // LDR PC, [PC, #-4]
            0x00, 0x00, 0x00, 0x00, // $val
        };

        public CodePatcher_arm32_far(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy,
            s_jmpCode.Length)
        {
            if (Math.Abs((long)target - (long)replace) < ((1 << 25) - 1))
                throw new ArgumentException(
                    "Address offset of target and replace must larger than ((1 << 25) - 1), please use InstructionModifier_arm32_near instead");

#if ENABLE_HOOK_DEBUG
            UnityEngine.Debug.Log($"CodePatcher_arm32_far: {PrintAddrs()}");
#endif
        }

        protected override unsafe byte[] GenJmpCode(void* jmpFrom, void* jmpTo)
        {
            byte[] ret = new byte[s_jmpCode.Length];

            fixed (void* p = &ret[0])
            {
                uint* ptr = (uint*)p;
                *ptr++ = 0xE51FF004;
                *ptr = (uint)jmpTo;
            }

            return ret;
        }
    }

    /// <summary>
    /// Jump within ±128MB range in arm64
    /// </summary>
    public unsafe class CodePatcher_arm64_near : CodePatcher
    {
        private static readonly byte[] s_jmpCode = new byte[] // 4 bytes
        {
            /*
             * from 0x14 to 0x17 is B opcode
             * offset bits is 26
             * https://developer.arm.com/documentation/ddi0596/2021-09/Base-Instructions/B--Branch-
             */
            0x00, 0x00, 0x00, 0x14, //  B $val   ; $val = (($dst - $src)/4) & 7FFFFFF
        };

        public CodePatcher_arm64_near(IntPtr target, IntPtr replace, IntPtr proxy) : base(target, replace, proxy,
            s_jmpCode.Length)
        {
            // if (Math.Abs((long)target - (long)replace) >= ((1 << 26) - 1) * 4)
            if (Math.Abs((long)target - (long)replace) >= ((1 << 26) - 1))
                throw new ArgumentException("Address offset of target and replace must less than (1 << 26) - 1) * 4");

#if ENABLE_HOOK_DEBUG
            UnityEngine.Debug.Log($"CodePatcher_arm64: {PrintAddrs()}");
#endif
        }

        protected override byte[] GenJmpCode(void* jmpFrom, void* jmpTo)
        {
            byte[] ret = new byte[s_jmpCode.Length];
            int val = (int)((long)jmpTo - (long)jmpFrom) / 4;

            fixed (void* p = &ret[0])
            {
                byte* ptr = (byte*)p;
                *ptr++ = (byte)val;
                *ptr++ = (byte)(val >> 8);
                *ptr++ = (byte)(val >> 16);

                byte last = (byte)(val >> 24);
                last &= 0b11;
                last |= 0x14;

                *ptr = last;
            }

            return ret;
        }
    }

    /// <summary>
    /// arm64 far jump
    /// </summary>
    public unsafe class CodePatcher_arm64_far_original : CodePatcher
    {
        private static readonly byte[] s_jmpCode = new byte[] // 20 bytes (too many bytes, too dangerous to use)
        {
            /*
             * ADR: https://developer.arm.com/documentation/ddi0596/2021-09/Base-Instructions/ADR--Form-PC-relative-address-
             * BR: https://developer.arm.com/documentation/ddi0596/2021-09/Base-Instructions/BR--Branch-to-Register-
             */
            0x6A, 0x00, 0x00, 0x10, // ADR X10, #C
            0x4A, 0x01, 0x40, 0xF9, // LDR X10, [X10,#0]
            0x40, 0x01, 0x1F, 0xD6, // BR X10
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 // $dst
        };

        public CodePatcher_arm64_far_original(IntPtr target, IntPtr replace, IntPtr proxy, int jmpCodeSize) : base(
            target, replace, proxy, jmpCodeSize)
        {
        }

        protected override unsafe byte[] GenJmpCode(void* jmpFrom, void* jmpTo)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Jump to any address in ARM64 using an absolute jump
    /// </summary>
    public unsafe class CodePatcher_arm64_far : CodePatcher
    {
        // The total length of the jump code (5 instructions * 4 bytes each)
        private static readonly int s_jmpCodeLength = 4 * 4;

        public CodePatcher_arm64_far(IntPtr target, IntPtr replace, IntPtr proxy)
            : base(target, replace, proxy, s_jmpCodeLength)
        {
            // No offset limitations for far jump
#if ENABLE_HOOK_DEBUG
        UnityEngine.Debug.Log($"CodePatcher_arm64_far: {PrintAddrs()}");
#endif
        }

        protected override byte[] GenJmpCode(void* jmpFrom, void* jmpTo)
        {
            byte[] ret = new byte[16]; // 4 instructions

            ulong pc = (ulong)jmpFrom;
            ulong targetAddr = (ulong)jmpTo;

            // Encode ADRP and ADD instructions
            uint adrp = Encode_ADRP(17, pc, targetAddr);
            uint add = Encode_ADD(17, 17, (uint)(targetAddr & 0xFFF));
            uint br = Encode_BR(17);
            uint nop = 0xD503201F; // NOP instruction

            fixed (byte* p = ret)
            {
                uint* ptr = (uint*)p;
                ptr[0] = adrp;
                ptr[1] = add;
                ptr[2] = br;
                ptr[3] = nop; // Padding to match the size
            }

            // // Apply the same memory protection and cache synchronization as before
            // IntPtr codeStart = new IntPtr(jmpFrom);
            // int length = ret.Length;
            //
            // // Make memory writable and executable
            // MakeMemoryWritableExecutable(codeStart, length);
            //
            // // Write the code into memory
            // Marshal.Copy(ret, 0, codeStart, length);
            //
            // // Flush the instruction cache
            // FlushInstructionCache(codeStart, length);

            return ret;
        }
        
        private static uint Encode_ADRP(int Rd, ulong pc, ulong address)
        {
            long pageDiff = ((long)(address >> 12) - (long)(pc >> 12));
            if (pageDiff < -(1 << 20) || pageDiff > (1 << 20) - 1)
            {
                throw new ArgumentOutOfRangeException("Address out of range for ADRP.");
            }

            uint immhi = (uint)((pageDiff >> 2) & 0x7FFFF) << 5; // Bits 23-5
            uint immlo = (uint)(pageDiff & 0x3) << 29;           // Bits 30-29
            uint opcode = 0x90000000u;                           // ADRP opcode
            uint rd = (uint)(Rd & 0x1F);

            return opcode | immlo | immhi | rd;
        }

        private static uint Encode_ADD(int Rd, int Rn, uint imm12)
        {
            if (imm12 > 0xFFF)
            {
                throw new ArgumentOutOfRangeException("Immediate out of range for ADD.");
            }

            uint opcode = 0x91000000u; // ADD (immediate) opcode
            opcode |= imm12 << 10;     // Bits 21-10: imm12
            opcode |= ((uint)Rn & 0x1F) << 5; // Bits 9-5: Rn
            opcode |= ((uint)Rd & 0x1F);      // Bits 4-0: Rd

            return opcode;
        }
        
        private static uint Encode_BR(int Rn)
        {
            // BR Xn
            uint opcode = 0xD61F0000u;                // Fixed opcode for BR instruction
            uint rn = ((uint)Rn & 0x1F) << 5;         // Bits 9-5: Rn
            return opcode | rn;
        }
    }
}