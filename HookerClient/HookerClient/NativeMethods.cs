/********************************** Module Header **********************************\
 * Module Name:  NativeMethods.cs
 * Project:      CSMailslotClient
 * Copyright (c) Microsoft Corporation.
 * 
 * 
 * Native API Signatures and Types 
 * 
 * This source is subject to the Microsoft Public License.
 * See http://www.microsoft.com/en-us/openness/licenses.aspx#MPL.
 * All other rights reserved.
 * 
 * THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
 * EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
 * WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\***********************************************************************************/

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace CSMailslotClient
{
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        /// <summary>
        /// Desired Access of File/Device
        /// </summary>
        [Flags]
        internal enum FileDesiredAccess : uint
        {
            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            GENERIC_ALL = 0x10000000
        }

        /// <summary>
        /// File share mode
        /// </summary>
        [Flags]
        internal enum FileShareMode : uint
        {
            Zero = 0x00000000,  // No sharing
            FILE_SHARE_DELETE = 0x00000004,
            FILE_SHARE_READ = 0x00000001,
            FILE_SHARE_WRITE = 0x00000002
        }

        /// <summary>
        /// File Creation Disposition
        /// </summary>
        internal enum FileCreationDisposition : uint
        {
            CREATE_NEW = 1,
            CREATE_ALWAYS = 2,
            OPEN_EXISTING = 3,
            OPEN_ALWAYS = 4,
            TRUNCATE_EXISTING = 5
        }


        /// <summary>
        /// Represents a wrapper class for a mailslot handle. 
        /// </summary>
        [SecurityCritical(SecurityCriticalScope.Everything),
        HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true),
        SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal sealed class SafeMailslotHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeMailslotHandle()
                : base(true)
            {
            }

            public SafeMailslotHandle(IntPtr preexistingHandle, bool ownsHandle)
                : base(ownsHandle)
            {
                base.SetHandle(preexistingHandle);
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success),
            DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CloseHandle(IntPtr handle);

            protected override bool ReleaseHandle()
            {
                return CloseHandle(base.handle);
            }
        }



        /// <summary>
        /// Creates or opens a file, directory, physical disk, volume, console 
        /// buffer, tape drive, communications resource, mailslot, or named pipe.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file or device to be created or opened.
        /// </param>
        /// <param name="desiredAccess">
        /// The requested access to the file or device, which can be summarized 
        /// as read, write, both or neither (zero).
        /// </param>
        /// <param name="shareMode">
        /// The requested sharing mode of the file or device, which can be read, 
        /// write, both, delete, all of these, or none (refer to the following 
        /// table). 
        /// </param>
        /// <param name="securityAttributes">
        /// A SECURITY_ATTRIBUTES object that contains two separate but related 
        /// data members: an optional security descriptor, and a Boolean value 
        /// that determines whether the returned handle can be inherited by 
        /// child processes.
        /// </param>
        /// <param name="creationDisposition">
        /// An action to take on a file or device that exists or does not exist.
        /// </param>
        /// <param name="flagsAndAttributes">
        /// The file or device attributes and flags.
        /// </param>
        /// <param name="hTemplateFile">Handle to a template file.</param>
        /// <returns>
        /// If the function succeeds, the return value is an open handle to the 
        /// specified file, device, named pipe, or mail slot.
        /// If the function fails, the return value is an invalid handle.
        /// </returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeMailslotHandle CreateFile(string fileName,
            FileDesiredAccess desiredAccess, FileShareMode shareMode,
            IntPtr securityAttributes,
            FileCreationDisposition creationDisposition,
            int flagsAndAttributes, IntPtr hTemplateFile);


        /// <summary>
        /// Writes data to the specified file or input/output (I/O) device.
        /// </summary>
        /// <param name="handle">
        /// A handle to the file or I/O device (for example, a file, file stream,
        /// physical disk, volume, console buffer, tape drive, socket, 
        /// communications resource, mailslot, or pipe). 
        /// </param>
        /// <param name="bytes">
        /// A buffer containing the data to be written to the file or device.
        /// </param>
        /// <param name="numBytesToWrite">
        /// The number of bytes to be written to the file or device.
        /// </param>
        /// <param name="numBytesWritten">
        /// The number of bytes written when using a synchronous IO.
        /// </param>
        /// <param name="overlapped">
        /// A pointer to an OVERLAPPED structure is required if the file was 
        /// opened with FILE_FLAG_OVERLAPPED.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is true. If the function 
        /// fails, or is completing asynchronously, the return value is false.
        /// </returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteFile(SafeMailslotHandle handle,
            byte[] bytes, int numBytesToWrite, out int numBytesWritten,
            IntPtr overlapped);
        /*aggiunte mie per vedere se esiste mailslot*/

        [StructLayout(LayoutKind.Sequential)]
        internal class SECURITY_ATTRIBUTES
        {
            public int nLength;
            public SafeLocalMemHandle lpSecurityDescriptor;
            public bool bInheritHandle;
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeMailslotHandle CreateMailslot(string mailslotName,
            uint nMaxMessageSize, int lReadTimeout,
            SECURITY_ATTRIBUTES securityAttributes);

        /// <summary>
        /// Represents a wrapper class for a local memory pointer. 
        /// </summary>
        [SuppressUnmanagedCodeSecurity,
        HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
        internal sealed class SafeLocalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeLocalMemHandle()
                : base(true)
            {
            }

            public SafeLocalMemHandle(IntPtr preexistingHandle, bool ownsHandle)
                : base(ownsHandle)
            {
                base.SetHandle(preexistingHandle);
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success),
            DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr LocalFree(IntPtr hMem);

            protected override bool ReleaseHandle()
            {
                return (LocalFree(base.handle) == IntPtr.Zero);
            }
        }


        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
            string sddlSecurityDescriptor, int sddlRevision,
            out SafeLocalMemHandle pSecurityDescriptor,
            IntPtr securityDescriptorSize);


         }
}

