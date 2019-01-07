﻿using System;
using System.Runtime.InteropServices;

namespace Gee.External.Capstone {
    /// <summary>
    ///     Native Capstone.
    /// </summary>
    internal static class NativeCapstone {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate IntPtr Callback(IntPtr pBinaryCode, IntPtr binaryCodeSize, IntPtr invalidOffset, IntPtr pState);

        /// <summary>
        ///     Magic Instruction Architecture Details Field Offset.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Represents the offset, in bytes, of <c>NativeInstructionDetail.X86|Arm64|...</c>. In the Capstone
        ///         API, those fields are defined by a nested anonymous union defined by <c>cs_detail</c>. A
        ///         poor-man's analysis of <c>cs_detail</c> has indicated that all fields defined by it are aligned
        ///         along 8 byte boundaries. As such, poor-man's pointer arithmetic has concluded that
        ///         <c>NativeInstructionDetail.X86|Arm64|...</c> are accessible at this offset.
        ///     </para>
        ///     <para>
        ///         It seems the .NET Marshaller marshals <c>cs_detail</c> to <c>NativeInstructionDetail</c>
        ///         perfectly except for <c>NativeInstructionDetail.X86|Arm64|...</c>! Those fields are always set to
        ///         garbage data, indicating the .NET Marshaller is marshaling them from incorrect memory locations.
        ///         We've no idea why! As such, <c>NativeInstructionDetail.X86|Arm64|...</c> are not defined by the
        ///         Capstone.NET API and are instead read manually from this offset.
        ///     </para>
        /// </remarks>
        private const int MagicInstructionArchitectureDetailsFieldOffset = 80;

        /// <summary>
        ///     Create a Disassembler.
        /// </summary>
        /// <param name="disassembleArchitecture">
        ///     The hardware architecture for the disassembler to use.
        /// </param>
        /// <param name="disassembleMode">
        ///     The hardware mode for the disassembler to use.
        /// </param>
        /// <returns>
        ///     A disassembler handle.
        /// </returns>
        /// <exception cref="Gee.External.Capstone.CapstoneException">
        ///     Thrown if a disassembler could not be created.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     Thrown if the disassemble architecture is invalid, or if the disassemble mode is invalid or
        ///     unsupported by the disassemble architecture.
        /// </exception>
        /// <exception cref="System.OutOfMemoryException">
        ///     Thrown if sufficient memory cannot be allocated to perform the operation as a rare indication that the
        ///     system is under heavy load.
        /// </exception>
        internal static NativeDisassemblerHandle CreateDisassembler(DisassembleArchitecture disassembleArchitecture, NativeDisassembleMode disassembleMode) {
            var pDisassembler = IntPtr.Zero;
            var resultCode = NativeCapstoneImport.CreateDisassembler(disassembleArchitecture, disassembleMode, ref pDisassembler);
            if (resultCode != NativeCapstoneResultCode.Ok) {
                if (resultCode == NativeCapstoneResultCode.UninitializedMemoryManagement) {
                    const string detailMessage = "Memory Management is uninitialized.";
                    throw new CapstoneException(detailMessage);
                }
                else if (resultCode == NativeCapstoneResultCode.UnsupportedArchitecture) {
                    var detailMessage = $"A disassemble architecture ({disassembleArchitecture}) is invalid.";
                    throw new ArgumentException(detailMessage, nameof(disassembleArchitecture));
                }
                else if (resultCode == NativeCapstoneResultCode.UnsupportedDissembleMode) {
                    var detailMessage = $"A disassemble mode ({disassembleMode}) is invalid.";
                    throw new ArgumentException(detailMessage, nameof(disassembleMode));
                }
                else if (resultCode == NativeCapstoneResultCode.OutOfMemory) {
                    const string detailMessage = "Sufficient memory could not be allocated.";
                    throw new OutOfMemoryException(detailMessage);
                }
                else {
                    const string detailMessage = "A disassembler could not be created.";
                    throw new CapstoneException(detailMessage);
                }
            }

            var hDisassembler = new NativeDisassemblerHandle(pDisassembler);
            return hDisassembler;
        }

        /// <summary>
        ///     Create an Instruction..
        /// </summary>
        /// <param name="hDisassembler">
        ///     A disassembler handle.
        /// </param>
        /// <returns>
        ///     An instruction handle.
        /// </returns>
        /// <exception cref="System.ObjectDisposedException">
        ///     Thrown if the disassembler handle is disposed.
        /// </exception>
        public static NativeInstructionHandle CreateInstruction(NativeDisassemblerHandle hDisassembler) {
            // ...
            //
            // Throws an exception if the operation fails.
            var pInstruction = NativeCapstoneImport.CreateInstruction(hDisassembler);

            var hInstruction = new NativeInstructionHandle(pInstruction);
            return hInstruction;
        }

        /// <summary>
        ///     Get an Instruction's Accessed Registers.
        /// </summary>
        /// <param name="hDisassembler">
        ///     A disassembler handle.
        /// </param>
        /// <param name="hInstruction">
        ///     An instruction handle.
        /// </param>
        /// <returns>
        ///     A 2-tuple, where the first item is an array of the instruction's read registers and the second item is
        ///     an array of the instruction's written registers.
        /// </returns>
        /// <exception cref="Gee.External.Capstone.CapstoneException">
        ///     Thrown if the instruction's accessed registers could not be retrieved.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     Thrown if the disassembler handle is invalid.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        ///     Thrown if the instruction was disassembled when Instruction Details Mode was disabled, or if the
        ///     instruction was disassembled when Skip Data Mode was enabled.
        /// </exception>
        /// <exception cref="System.NotSupportedException">
        ///     Thrown if Diet Mode is enabled, or if the disassembler's hardware architecture does not support the
        ///     operation.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">
        ///     Thrown if the disassembler handle is disposed, or if the instruction handle is disposed.
        /// </exception>
        public static Tuple<short[], short[]> GetAccessedRegisters(NativeDisassemblerHandle hDisassembler, NativeInstructionHandle hInstruction) {
            // ...
            //
            // Throws an exception if the operation fails.
            var readRegisters = new short[64];
            byte readRegistersCount = 0;
            var writtenRegisters = new short[64];
            byte writtenRegistersCount = 0;
            var resultCode = NativeCapstoneImport.GetAccessedRegisters(hDisassembler, hInstruction, readRegisters, ref readRegistersCount, writtenRegisters, ref writtenRegistersCount);
            if (resultCode != NativeCapstoneResultCode.Ok) {
                if ((int) resultCode == -1) {
                    // ...
                    //
                    // For some reason, the Capstone API will return a <c>-1</c>, instead of a defined error code, if
                    // the disassembler handle is invalid.
                    var detailMessage = $"A disassembler handle ({nameof(hDisassembler)}) is invalid.";
                    throw new ArgumentException(detailMessage, nameof(hDisassembler));
                }
                else if (resultCode == NativeCapstoneResultCode.UnsupportedArchitecture) {
                    const string detailMessage = "A disassembler's hardware architecture is not supported.";
                    throw new NotSupportedException(detailMessage);
                }
                else if (resultCode == NativeCapstoneResultCode.UnSupportedDietModeOperation) {
                    const string detailMessage = "An operation is not supported when diet mode is enabled.";
                    throw new NotSupportedException(detailMessage);
                }
                else if (resultCode == NativeCapstoneResultCode.UnsupportedInstructionDetail) {
                    const string detailMessage = "An operation is not supported when instruction details are disabled.";
                    throw new InvalidOperationException(detailMessage);
                }
                else if (resultCode == NativeCapstoneResultCode.UnsupportedSkipDataModeOperation) {
                    const string detailMessage = "An operation is not supported when skip-data mode is enabled.";
                    throw new InvalidOperationException(detailMessage);
                }
                else {
                    const string detailMessage = "An instruction's accessed registers could not be retrieved.";
                    throw new CapstoneException(detailMessage);
                }
            }

            var newReadRegisters = new short[readRegistersCount];
            var newWrittenRegisters = new short[writtenRegistersCount];
            Array.Copy(readRegisters, newReadRegisters, newReadRegisters.Length);
            Array.Copy(writtenRegisters, newWrittenRegisters, newWrittenRegisters.Length);

            var tuple = Tuple.Create(newReadRegisters, newWrittenRegisters);
            return tuple;
        }

        /// <summary>
        ///     Get an Instruction.
        /// </summary>
        /// <param name="hInstruction">
        ///     An instruction handle.
        /// </param>
        /// <returns>
        ///     An instruction.
        /// </returns>
        public static NativeInstruction GetInstruction(NativeInstructionHandle hInstruction) {
            var pInstruction = hInstruction.DangerousAddRefAndGetHandle();
            try {
                // ...
                //
                // Throws an exception if the operation fails.
                var instruction = MarshalExtension.PtrToStructure<NativeInstruction>(pInstruction);
                return instruction;
            }
            finally {
                hInstruction.DangerousRelease();
            }
        }

        /// <summary>
        ///     Get an Instruction's Details.
        /// </summary>
        /// <param name="hInstruction">
        ///     An instruction handle.
        /// </param>
        /// <returns>
        ///     The instruction's details. A null reference indicates the instruction was disassembled without
        ///     details.
        /// </returns>
        public static NativeInstructionDetail? GetInstructionDetail(NativeInstructionHandle hInstruction) {
            var pInstruction = hInstruction.DangerousAddRefAndGetHandle();
            try {
                // ...
                //
                // First, we calculate the memory address of the <c>NativeInstruction.Details</c> field, which is
                // always relative to the memory address of its defining <c>NativeInstruction</c> structure. This is
                // NOT the actual memory address of the instruction's details.
                var instructionDetailOffset = Marshal.OffsetOf(typeof(NativeInstruction), nameof(NativeInstruction.Details));
                var pInstructionDetail = (IntPtr) ((long) pInstruction + (long) instructionDetailOffset);

                // ...
                //
                // Second, we read the value of the <c>NativeInstruction.Details</c> field, which IS the actual memory
                // address of the instruction's details. If the value is not equal to <c>IntPtr.Zero</c>, that indicates
                // the instruction was disassembled with details.
                var ppInstructionDetail = Marshal.ReadIntPtr(pInstructionDetail);
                NativeInstructionDetail? instructionDetail = null;
                if (ppInstructionDetail != IntPtr.Zero) {
                    instructionDetail = MarshalExtension.PtrToStructure<NativeInstructionDetail>(ppInstructionDetail);
                }

                return instructionDetail;
            }
            finally {
                hInstruction.DangerousRelease();
            }
        }

        /// <summary>
        ///     Get an Instruction's Details.
        /// </summary>
        /// <typeparam name="TInstructionDetail">
        ///     The type of the instruction's details.
        /// </typeparam>
        /// <param name="hInstruction">
        ///     An instruction handle.
        /// </param>
        /// <returns>
        ///     The instruction's details. A null reference indicates the instruction was disassembled without
        ///     details.
        /// </returns>
        public static TInstructionDetail? GetInstructionDetail<TInstructionDetail>(NativeInstructionHandle hInstruction) where TInstructionDetail : struct {
            var pInstruction = hInstruction.DangerousAddRefAndGetHandle();
            try {
                // ...
                //
                // First, we calculate the memory address of the <c>NativeInstruction.Details</c> field, which is
                // always relative to the memory address of its defining <c>NativeInstruction</c> structure. This is
                // NOT the actual memory address of the instruction's details.
                var instructionDetailOffset = Marshal.OffsetOf(typeof(NativeInstruction), nameof(NativeInstruction.Details));
                var pInstructionDetail = (IntPtr) ((long) pInstruction + (long) instructionDetailOffset);

                // ...
                //
                // Second, we read the value of the <c>NativeInstruction.Details</c> field, which IS the actual memory
                // address of the instruction's details. If the value is not equal to <c>IntPtr.Zero</c>, that indicates
                // the instruction was disassembled with details.
                var ppInstructionDetail = Marshal.ReadIntPtr(pInstructionDetail);
                TInstructionDetail? instructionDetail = null;
                if (ppInstructionDetail != IntPtr.Zero) {
                    // ...
                    //
                    // Fourth, we calculate the memory address of the instruction's architecture specific details,
                    // which is always relative to the memory address of the instruction's details.
                    var pArchInstructionDetail = ppInstructionDetail + NativeCapstone.MagicInstructionArchitectureDetailsFieldOffset;
                    instructionDetail = (TInstructionDetail) Marshal.PtrToStructure(pArchInstructionDetail, typeof(TInstructionDetail));
                }

                return instructionDetail;
            }
            finally {
                hInstruction.DangerousRelease();
            }
        }

        /// <summary>
        ///     Get an Instruction's Details.
        /// </summary>
        /// <param name="instruction">
        ///     An instruction.
        /// </param>
        /// <returns>
        ///     The instruction's details. A null reference indicates the instruction was disassembled without
        ///     details.
        /// </returns>
        public static NativeInstructionDetail? GetInstructionDetail(ref NativeInstruction instruction) {
            NativeInstructionDetail? instructionDetails = null;
            if (instruction.Details != IntPtr.Zero) {
                // ...
                //
                // Throws an exception if the operation fails.
                var pInstructionDetails = instruction.Details;
                instructionDetails = MarshalExtension.PtrToStructure<NativeInstructionDetail>(pInstructionDetails);
            }

            return instructionDetails;
        }

        /// <summary>
        ///     Get an Instruction's Architecture Specific Details.
        /// </summary>
        /// <typeparam name="TInstructionDetails">
        ///     The type of the instruction's architecture specific details.
        /// </typeparam>
        /// <param name="instruction">
        ///     An instruction.
        /// </param>
        /// <returns>
        ///     The instruction's architecture specific details. A null reference indicates the instruction was
        ///     disassembled without its details. 
        /// </returns>
        public static TInstructionDetails? GetInstructionDetail<TInstructionDetails>(ref NativeInstruction instruction) where TInstructionDetails : struct {
            TInstructionDetails? instructionDetails = null;
            if (instruction.Details != IntPtr.Zero) {
                // ...
                //
                // Throws an exception if the operation fails.
                var pInstructionDetails = instruction.Details + NativeCapstone.MagicInstructionArchitectureDetailsFieldOffset;
                instructionDetails = MarshalExtension.PtrToStructure<TInstructionDetails>(pInstructionDetails);
            }

            return instructionDetails;
        }

        /// <summary>
        ///     Get an Instruction Group's Name.
        /// </summary>
        /// <param name="hDisassembler">
        ///     A disassembler handle.
        /// </param>
        /// <param name="instructionGroupId">
        ///     An instruction group's unique identifier.
        /// </param>
        /// <returns>
        ///     The instruction group's name. A null reference if the disassembler handle is invalid, or if the
        ///     instruction group's unique identifier is invalid.
        /// </returns>
        /// <exception cref="System.ObjectDisposedException">
        ///     Thrown if the disassembler handle is disposed.
        /// </exception>
        public static unsafe string GetInstructionGroupName(NativeDisassemblerHandle hDisassembler, int instructionGroupId) {
            // ...
            //
            // Throws an exception if the operation fails.
            string instructionGroupName = null;
            var pInstructionGroupName = NativeCapstoneImport.GetInstructionGroupName(hDisassembler, instructionGroupId);
            if (pInstructionGroupName != IntPtr.Zero) {
                instructionGroupName = new string((sbyte*) pInstructionGroupName);
            }

            return instructionGroupName;
        }

        /// <summary>
        ///     Get a Register's Name.
        /// </summary>
        /// <param name="hDisassembler">
        ///     A disassembler handle.
        /// </param>
        /// <param name="registerId">
        ///     A register unique identifier.
        /// </param>
        /// <returns>
        ///     The register's name. A null reference if the disassembler handle is invalid, or if the register unique
        ///     identifier is invalid.
        /// </returns>
        /// <exception cref="System.ObjectDisposedException">
        ///     Thrown if the disassembler handle is disposed.
        /// </exception>
        public static unsafe string GetRegisterName(NativeDisassemblerHandle hDisassembler, int registerId) {
            // ...
            //
            // Throws an exception if the operation fails.
            string registerName = null;
            var pRegisterName = NativeCapstoneImport.GetRegisterName(hDisassembler, registerId);
            if (pRegisterName != IntPtr.Zero) {
                registerName = new string((sbyte*) pRegisterName);
            }

            return registerName;
        }

        /// <summary>
        ///     Disassemble Binary Code Iteratively.
        /// </summary>
        /// <param name="hDisassembler">
        ///     A disassembler handle.
        /// </param>
        /// <param name="binaryCode">
        ///     A buffer indicating the binary code to disassemble.
        /// </param>
        /// <param name="binaryCodeOffset">
        ///     The index of the instruction to disassemble in the binary code buffer . If the instruction is
        ///     disassembled successfully, this value will be updated to reflect the index of the next instruction to
        ///     disassemble in the binary code buffer. If the updated value is less than the length of the binary code
        ///     buffer, you can safely invoke this method with the updated value to disassemble the next instruction.
        /// </param>
        /// <param name="address">
        ///     The address of the instruction. If the instruction is disassembled successfully, this value will be
        ///     updated to reflect the address of the next instruction to disassemble in the binary code buffer.
        /// </param>
        /// <param name="hInstruction">
        ///     An instruction handle.
        /// </param>
        /// <returns>
        ///     A boolean true if an instruction was disassembled successfully. A boolean false otherwise.
        /// </returns>
        /// <exception cref="System.ObjectDisposedException">
        ///     Thrown if the disassembler handle is disposed, or if the instruction handle is disposed.
        /// </exception>
        internal static bool Iterate(NativeDisassemblerHandle hDisassembler, byte[] binaryCode, ref int binaryCodeOffset, ref long address, NativeInstructionHandle hInstruction) {
            var hBinaryCode = GCHandle.Alloc(binaryCode, GCHandleType.Pinned);
            try {
                // ...
                //
                // First, we increment the pointer to the binary code buffer to the point to the address of the
                // instruction we want to disassemble.
                var pBinaryCode = hBinaryCode.AddrOfPinnedObject() + binaryCodeOffset;

                // ...
                //
                // Second, we calculate the size of the binary code buffer by decrementing the offset we incremented
                // by in the previous step.
                var binaryCodeSize = (IntPtr) binaryCode.Length - binaryCodeOffset;

                // ...
                //
                // Third, we save the address of the binary code buffer we will disassemble, so that we can later
                // compute a new offset, and disassemble the binary code. If an instruction was disassembled
                // successfully, the pointer to the binary code, the binary code size, and the starting address will
                // be updated by the Capstone API to reflect the address of the next instruction to disassemble in the
                // binary code buffer.
                //
                // Throws an exception if the operation fails.
                var initialPBinaryCode = pBinaryCode;
                var isDisassembled = NativeCapstoneImport.Iterate(hDisassembler, ref pBinaryCode, ref binaryCodeSize, ref address, hInstruction);
                if (isDisassembled) {
                    // ...
                    //
                    // Fourth, we compute a new offset to indicate to the caller the next instruction to disassemble
                    // in the binary code buffer.
                    binaryCodeOffset += (int) ((long) pBinaryCode - (long) initialPBinaryCode);
                }

                return isDisassembled;
            }
            finally {
                if (hBinaryCode.IsAllocated) {
                    hBinaryCode.Free();
                }
            }
        }

        /// <summary>
        ///     Query an Option.
        /// </summary>
        /// <param name="queryOption">
        ///     An option to query.
        /// </param>
        /// <returns>
        ///     A boolean true if the option is supported. A boolean false otherwise.
        /// </returns>
        public static bool Query(NativeQueryOption queryOption) {
            var isSupported = NativeCapstoneImport.Query(queryOption);
            return isSupported;
        }

        /// <summary>
        ///     Set a Disassembler Option.
        /// </summary>
        /// <param name="hDisassembler">
        ///     A disassembler handle.
        /// </param>
        /// <param name="optionType">
        ///     A type of option to set.
        /// </param>
        /// <param name="optionValue">
        ///     A value to set the option to.
        /// </param>
        /// <exception cref="Gee.External.Capstone.CapstoneException">
        ///     Thrown if the option could not be set.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     Thrown if the disassembler handle is invalid, or if the option is invalid.
        /// </exception>
        /// <exception cref="System.NotSupportedException">
        ///     Thrown if the option is equal to <see cref="NativeDisassemblerOptionType.SetSkipDataConfig" />.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">
        ///     Thrown if the disassembler handle is disposed.
        /// </exception>
        public static void SetDisassemblerOption(NativeDisassemblerHandle hDisassembler, NativeDisassemblerOptionType optionType, NativeDisassemblerOptionValue optionValue) {
            if (optionType == NativeDisassemblerOptionType.SetSkipDataConfig) {
                var detailMessage = $"A disassembler option ({optionType}) is unsupported.";
                throw new NotSupportedException(detailMessage);
            }

            // ...
            //
            // Throws an exception if the operation fails.
            var resultCode = NativeCapstoneImport.SetDisassemblerOption(hDisassembler, optionType, (IntPtr) optionValue);
            if (resultCode != NativeCapstoneResultCode.Ok) {
                if (resultCode == NativeCapstoneResultCode.InvalidHandle2) {
                    var detailMessage = $"A disassembler handle ({nameof(hDisassembler)}) is invalid.";
                    throw new ArgumentException(detailMessage, nameof(hDisassembler));
                }
                else if (resultCode == NativeCapstoneResultCode.InvalidOption) {
                    var detailMessage = $"An option ({nameof(optionType)}) is invalid.";
                    throw new ArgumentException(detailMessage, nameof(optionType));
                }
                else {
                    var detailMessage = $"A disassembler option ({optionType}) could not be set.";
                    throw new CapstoneException(detailMessage);
                }
            }
        }

        /// <summary>
        ///     Set Disassembler Instruction Mnemonic Option.
        /// </summary>
        /// <param name="hDisassembler">
        ///     A disassembler handle.
        /// </param>
        /// <param name="optionValue">
        ///     A value to set the instruction mnemonic option to.
        /// </param>
        /// <exception cref="Gee.External.Capstone.CapstoneException">
        ///     Thrown if the instruction mnemonic option could not be set.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     Thrown if the disassembler handle is invalid.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">
        ///     Thrown if the disassembler handle is disposed.
        /// </exception>
        public static void SetInstructionMnemonicOption(NativeDisassemblerHandle hDisassembler, ref NativeInstructionMnemonicOptionValue optionValue) {
            var pOptionValue = IntPtr.Zero;
            try {
                pOptionValue = MarshalExtension.AllocHGlobal<NativeInstructionMnemonicOptionValue>();
                Marshal.StructureToPtr(optionValue, pOptionValue, false);

                // ...
                //
                // Throws an exception if the operation fails.
                const NativeDisassemblerOptionType optionType = NativeDisassemblerOptionType.SetMnemonic;
                var resultCode = NativeCapstoneImport.SetDisassemblerOption(hDisassembler, optionType, pOptionValue);
                if (resultCode != NativeCapstoneResultCode.Ok) {
                    if (resultCode == NativeCapstoneResultCode.InvalidHandle2) {
                        var detailMessage = $"A disassembler handle ({nameof(hDisassembler)}) is invalid.";
                        throw new ArgumentException(detailMessage, nameof(hDisassembler));
                    }
                    else {
                        var detailMessage = $"A disassembler option ({optionType}) could not be set.";
                        throw new CapstoneException(detailMessage);
                    }
                }
            }
            finally {
                if (pOptionValue != IntPtr.Zero) {
                    Marshal.FreeHGlobal(pOptionValue);
                }
            }
        }

        public static void SetSkipDataOption(NativeDisassemblerHandle hDisassembler, ref NativeSkipDataOptionValue optionValue) {
            var pOptionValue = IntPtr.Zero;
            try {
                pOptionValue = MarshalExtension.AllocHGlobal<NativeSkipDataOptionValue>();
                Marshal.StructureToPtr(optionValue, pOptionValue, false);

                // ...
                //
                // Throws an exception if the operation fails.
                const NativeDisassemblerOptionType optionType = NativeDisassemblerOptionType.SetSkipDataConfig;
                var resultCode = NativeCapstoneImport.SetDisassemblerOption(hDisassembler, optionType, pOptionValue);
            }
            finally {
                if (pOptionValue != IntPtr.Zero) {
                    Marshal.FreeHGlobal(pOptionValue);
                }
            }
        }
    }
}