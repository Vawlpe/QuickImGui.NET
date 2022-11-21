using System.Runtime.InteropServices;
using System.Text;

namespace QuickImGuiNET.Veldrid;

public class FixedAsciiString : IDisposable
{
    public unsafe FixedAsciiString(string s)
    {
        var byteCount = Encoding.ASCII.GetByteCount(s);
        DataPtr = Marshal.AllocHGlobal(byteCount + 1);
        fixed (char* sPtr = s)
        {
            var end = Encoding.ASCII.GetBytes(sPtr, s.Length, (byte*)DataPtr, byteCount);
            ((byte*)DataPtr)[end] = 0;
        }
    }

    public IntPtr DataPtr { get; }

    public void Dispose()
    {
        Marshal.FreeHGlobal(DataPtr);
    }
}