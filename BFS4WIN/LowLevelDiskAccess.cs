﻿using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Management;
using System.Threading.Tasks;
using System.Collections;

namespace BFS4WIN
{
    // routines taken from https://code.msdn.microsoft.com/windowsapps/CCS-LABS-C-Low-Level-Disk-91676ca9
    class LowLevelDiskAccess
    {
        #region "API CALLS" 

        public enum EMoveMethod : uint
        {
            Begin = 0,
            Current = 1,
            End = 2
        }

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint SetFilePointer(
            [In] SafeFileHandle hFile,
            [In] long lDistanceToMove,
            [Out] out int lpDistanceToMoveHigh,
            [In] EMoveMethod dwMoveMethod);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess,
          uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition,
          uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32", SetLastError = true)]
        internal extern static int ReadFile(SafeFileHandle handle, byte[] bytes,
           int numBytesToRead, out int numBytesRead, IntPtr overlapped_MustBeZero);

        [DllImport("kernel32", SetLastError = true)]
        internal extern static int WriteFile(SafeFileHandle handle, byte[] bytes,
           int numBytesToRead, out int numBytesRead, IntPtr overlapped_MustBeZero);

        #endregion


        /// <summary> 
        /// Returns the Sector from the drive at the specified location 
        /// </summary> 
        /// <param name="drive"> 
        /// The drive to have a sector read 
        /// </param> 
        /// <param name="sector"> 
        /// The sector number to read. 
        /// </param> 
        /// <param name="bytesPerSector"></param> 
        /// <returns></returns> 
        public byte[] ReadSector(string drive, double sector, int bytesPerSector)
        {
            short FILE_ATTRIBUTE_NORMAL = 0x80;
            short INVALID_HANDLE_VALUE = -1;
            uint GENERIC_READ = 0x80000000;
            uint GENERIC_WRITE = 0x40000000;
            uint CREATE_NEW = 1;
            uint CREATE_ALWAYS = 2;
            uint OPEN_EXISTING = 3;

            SafeFileHandle handleValue = CreateFile(drive, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (handleValue.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            double sec = sector * bytesPerSector;

            int size = int.Parse(bytesPerSector.ToString());
            byte[] buf = new byte[size];
            int read = 0;
            int moveToHigh;
            SetFilePointer(handleValue, int.Parse(sec.ToString()), out moveToHigh, EMoveMethod.Begin);
            ReadFile(handleValue, buf, size, out read, IntPtr.Zero);
            handleValue.Close();
            return buf;
        }

        public void WriteSector(string drive, double sector, int bytesPerSector, byte[] data)
        {
            short FILE_ATTRIBUTE_NORMAL = 0x80;
            short INVALID_HANDLE_VALUE = -1;
            uint GENERIC_READ = 0x80000000;
            uint GENERIC_WRITE = 0x40000000;
            uint CREATE_NEW = 1;
            uint CREATE_ALWAYS = 2;
            uint OPEN_EXISTING = 3;

            SafeFileHandle handleValue = CreateFile(drive, GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (handleValue.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            double sec = sector * bytesPerSector;

            int size = int.Parse(bytesPerSector.ToString());
            byte[] buf = new byte[size];
            int write= 0;
            int moveToHigh;
            SetFilePointer(handleValue, int.Parse(sec.ToString()), out moveToHigh, EMoveMethod.Begin);
            WriteFile(handleValue, data, size, out write, IntPtr.Zero);
            handleValue.Close();
            //return buf;
        }

        #region "WMI LOW LEVEL COMMANDS" 

        /// <summary> 
        /// Returns the number of bytes that the drive sectors contain. 
        /// </summary> 
        /// <param name="drive"> 
        /// Int: The drive number to scan. 
        /// </param> 
        /// <returns> 
        /// Int: The number of bytes the sector contains. 
        /// </returns> 
        public UInt32 BytesPerSector(int drive)
        {
            int driveCounter = 0;
            try
            {
               ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_DiskDrive");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if (driveCounter == drive)
                    {
                        var t = queryObj["BytesPerSector"];
                        return UInt32.Parse(t.ToString());

                    }
                    driveCounter++;
                }
            }
            catch (ManagementException)
            {
                return 0;
            }
            return 0;
        }

        /// <summary> 
        /// Returns a list of physical drive IDs 
        /// </summary> 
        /// <returns> 
        /// ArrayList: Device IDs of all connected physical hard drives 
        ///  </returns> 
        public ArrayList GetDriveList()
        {
            ArrayList drivelist = new ArrayList();

            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_DiskDrive");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    drivelist.Add(queryObj["DeviceID"].ToString());
                }
            }
            catch (ManagementException)
            {
                return null;
            }
            return drivelist;
        }

        /// <summary> 
        /// Returns the total sectors on the specified drive 
        /// </summary> 
        /// <param name="drive"> 
        /// int: The drive to be queried. 
        /// </param> 
        /// <returns> 
        /// int: Returns the total number of sectors 
        /// </returns> 
        public UInt64 GetTotalSectors(int drive)
        {
            int driveCount = 0;
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_DiskDrive");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if (driveCount == drive)
                    {
                        var t = queryObj["TotalSectors"];
                        return UInt64.Parse(t.ToString());

                    }
                    driveCount++;
                }
            }
            catch (ManagementException)
            {
                return 0;
            }
            return 0;
        }

        /// <summary> 
        /// Returns the caption of the drive. 
        /// </summary> 
        /// <param name="drive"> 
        /// The drive to be queried. 
        /// </param> 
        /// <returns> 
        /// string: drive caption
        /// </returns> 
        public string GetCaption(int drive)
        {
            int driveCount = 0;
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_DiskDrive");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if (driveCount == drive)
                    {
                        var t = queryObj["Caption"];
                        return t.ToString();
                    }
                    driveCount++;
                }
            }
            catch (ManagementException)
            {
                return "";
            }
            return "";
        }        
        #endregion
    }
}