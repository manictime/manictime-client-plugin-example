using System;
using System.Runtime.InteropServices;

namespace Plugins.Outlook
{
    public class MarshalEx
    {
        [DllImport("oleaut32.dll", PreserveSig = false)]
        static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        [DllImport("ole32.dll")]
        static extern int CLSIDFromProgID(
            [MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid pclsid);

        public static object GetActiveObject(string progId)
        {
            CLSIDFromProgID(progId, out var classId);
            GetActiveObject(ref classId, default, out var activeObjectReference);

            return activeObjectReference;
        }
    }
}