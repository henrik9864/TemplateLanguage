using System.Runtime.CompilerServices;

namespace Tokhenizer
{
    public unsafe struct Token
    {
        public readonly Range range;
        fixed uint data[8];

        internal Token(Range range) : this()
        {
            this.range = range;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get<T>(int slot) where T : unmanaged
        {
#if ERROR_CHECK
        if (slot < 0 || slot >= 8)
            throw new IndexOutOfRangeException();

        if (sizeof(T) != sizeof(uint))
            throw new Exception();
#endif

            return ref Unsafe.As<uint, T>(ref data[slot]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFlag<T>(int slot, in T value) where T : unmanaged
        {
#if ERROR_CHECK
            if (slot < 0 || slot >= 8)
                throw new IndexOutOfRangeException();

            if (sizeof(T) != sizeof(uint))
                throw new Exception();
#endif

            data[slot] |= Unsafe.As<T, uint>(ref Unsafe.AsRef(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveFlag<T>(int slot, in T value) where T : unmanaged
        {
#if ERROR_CHECK
            if (slot < 0 || slot >= 8)
                throw new IndexOutOfRangeException();

            if (sizeof(T) != sizeof(uint))
                throw new Exception();
#endif

            data[slot] = data[slot] & ~Unsafe.As<T, uint>(ref Unsafe.AsRef(value));
        }

        public ReadOnlySpan<char> GetSpan(in ReadOnlySpan<char> text)
        {
            return text[range];
        }
    }
}