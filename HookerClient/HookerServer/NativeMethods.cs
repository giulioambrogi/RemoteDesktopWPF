/********************************** Module Header **********************************\
 * Module Name:  NativeMethods.cs
 * Project:      CSMailslotServer
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

namespace CSMailslotServer
{
    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        /// <summary>
        /// Mailslot waits forever for a message 
        /// </summary>
        internal const int MAILSLOT_WAIT_FOREVER = -1;

        /// <summary>
        /// There is no next message
        /// </summary>
        internal const int MAILSLOT_NO_MESSAGE = -1;


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
        /// The SECURITY_ATTRIBUTES structure contains the security descriptor for 
        /// an object and specifies whether the handle retrieved by specifying 
        /// this structure is inheritable. This structure provides security 
        /// settings for objects created by various functions, such as CreateFile, 
        /// CreateNamedPipe, CreateProcess, RegCreateKeyEx, or RegSaveKeyEx.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal class SECURITY_ATTRIBUTES
        {
            public int nLength;
            public SafeLocalMemHandle lpSecurityDescriptor;
            public bool bInheritHandle;
        }


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



        /// <summary>
        /// Creates an instance of a mailslot and returns a handle for subsequent 
        /// operations.
        /// </summary>
        /// <param name="mailslotName">Mailslot name</param>
        /// <param name="nMaxMessageSize">
        /// The maximum size of a single message
        /// </param>
        /// <param name="lReadTimeout">
        /// The time a read operation can wait for a message.
        /// </param>
        /// <param name="securityAttributes">Security attributes</param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the server 
        /// end of a mailslot instance.
        /// </returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeMailslotHandle CreateMailslot(string mailslotName,
            uint nMaxMessageSize, int lReadTimeout,
            SECURITY_ATTRIBUTES securityAttributes);


        /// <summary>
        /// Retrieves information about the specified mailslot.
        /// </summary>
        /// <param name="hMailslot">A handle to a mailslot</param>
        /// <param name="lpMaxMessageSize">
        /// The maximum message size, in bytes, allowed for this mailslot.
        /// </param>
        /// <param name="lpNextSize">
        /// The size of the next message in bytes.
        /// </param>
        /// <param name="lpMessageCount">
        /// The total number of messages waiting to be read.
        /// </param>
        /// <param name="lpReadTimeout">
        /// The amount of time, in milliseconds, a read operation can wait for a 
        /// message to be written to the mailslot before a time-out occurs. 
        /// </param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMailslotInfo(SafeMailslotHandle hMailslot,
            IntPtr lpMaxMessageSize, out int lpNextSize, out int lpMessageCount,
            IntPtr lpReadTimeout);


        /// <summary>
        /// Reads data from the specified file or input/output (I/O) device.
        /// </summary>
        /// <param name="handle">
        /// A handle to the device (for example, a file, file stream, physical 
        /// disk, volume, console buffer, tape drive, socket, communications 
        /// resource, mailslot, or pipe).
        /// </param>
        /// <param name="bytes">
        /// A buffer that receives the data read from a file or device.
        /// </param>
        /// <param name="numBytesToRead">
        /// The maximum number of bytes to be read.
        /// </param>
        /// <param name="numBytesRead">
        /// The number of bytes read when using a synchronous IO.
        /// </param>
        /// <param name="overlapped">
        /// A pointer to an OVERLAPPED structure if the file was opened with 
        /// FILE_FLAG_OVERLAPPED.
        /// </param> 
        /// <returns>
        /// If the function succeeds, the return value is true. If the function 
        /// fails, or is completing asynchronously, the return value is false.
        /// </returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadFile(SafeMailslotHandle handle,
            byte[] bytes, int numBytesToRead, out int numBytesRead,
            IntPtr overlapped);


        /// <summary>
        /// The ConvertStringSecurityDescriptorToSecurityDescriptor function 
        /// converts a string-format security descriptor into a valid, 
        /// functional security descriptor.
        /// </summary>
        /// <param name="sddlSecurityDescriptor">
        /// A string containing the string-format security descriptor (SDDL) 
        /// to convert.
        /// </param>
        /// <param name="sddlRevision">
        /// The revision level of the sddlSecurityDescriptor string. 
        /// Currently this value must be 1.
        /// </param>
        /// <param name="pSecurityDescriptor">
        /// A pointer to a variable that receives a pointer to the converted 
        /// security descriptor.
        /// </param>
        /// <param name="securityDescriptorSize">
        /// A pointer to a variable that receives the size, in bytes, of the 
        /// converted security descriptor. This parameter can be IntPtr.Zero.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is true.
        /// </returns>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
            string sddlSecurityDescriptor, int sddlRevision,
            out SafeLocalMemHandle pSecurityDescriptor,
            IntPtr securityDescriptorSize);

    }
}
