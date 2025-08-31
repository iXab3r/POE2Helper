using System.Text;
using CheatCartridge.GameHelper.Natives;
using EyeAuras.Memory;

namespace CheatCartridge.GameHelper.Scaffolding;

internal static class MemoryExtensions
{
    /// <summary>
    ///     Reads the std::bucket into a array.
    /// </summary>
    /// <typeparam name="TValue">value type that the std bucket contains.</typeparam>
    /// <param name="nativeContainer">native object of the std::bucket.</param>
    /// <returns>a array containing all the valid values found in std::bucket.</returns>
    public static TValue[] ReadStdBucket<TValue>(this IMemory memory, StdBucket nativeContainer)
        where TValue : unmanaged
    {
        if (nativeContainer.Data.First == IntPtr.Zero ||
            nativeContainer.Capacity <= 0x00)
        {
            return Array.Empty<TValue>();
        }

        return memory.ReadStdVector<TValue>(nativeContainer.Data);
    }
    
    /// <summary>
    ///     Reads the std::vector into an array.
    /// </summary>
    public static T[] ReadStdVector<T>(this IMemory memory, StdVector nativeContainer)
        where T : unmanaged
    {
        var typeSize = Marshal.SizeOf<T>();
        var length = nativeContainer.Last - nativeContainer.First;
        if (length <= 0 || length % typeSize != 0)
        {
            return Array.Empty<T>();
        }

        return memory.Read<T>(nativeContainer.First, (int)length / typeSize);
    }
    
    /// <summary>
    ///     Reads the std::wstring. String read is in unicode format.
    /// </summary>
    /// <param name="nativeContainer">native object of std::wstring.</param>
    /// <returns>string.</returns>
    public static string ReadStdWString(this IMemory memory, StdWString nativeContainer)
    {
        const int MaxAllowed = 1000;
        if (nativeContainer.Length <= 0 ||
            nativeContainer.Length > MaxAllowed ||
            nativeContainer.Capacity <= 0 ||
            nativeContainer.Capacity > MaxAllowed)
        {
            return string.Empty;
        }

        if (nativeContainer.Capacity <= 8)
        {
            var buffer = BitConverter.GetBytes(nativeContainer.Buffer.ToInt64());
            var ret = Encoding.Unicode.GetString(buffer);
            buffer = BitConverter.GetBytes(nativeContainer.ReservedBytes.ToInt64());
            ret += Encoding.Unicode.GetString(buffer);
            return nativeContainer.Length < ret.Length ? ret[..nativeContainer.Length] : string.Empty;
        }
        else
        {
            var buffer = memory.Read<byte>(nativeContainer.Buffer, nativeContainer.Length * 2);
            return Encoding.Unicode.GetString(buffer);
        }
    }
}