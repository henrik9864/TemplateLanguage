using System.Buffers;

namespace TemplateLanguage
{
	public ref struct RefStack<T>
	{
		public readonly int Count => currIdx;

		IMemoryOwner<T> buff;
		int currIdx;

        public RefStack(int capacity)
        {
			this.buff = MemoryPool<T>.Shared.Rent(capacity);
			this.currIdx = 0;
        }

		public void Push(in T item)
		{
            buff.Memory.Span[currIdx++] = item;
		}

		public T Pop()
		{
			return buff.Memory.Span[--currIdx];
		}

		public ref T Peek(int offset = 0)
		{
			return ref buff.Memory.Span[currIdx + offset - 1];
		}

		public bool TryPeek(int offset, out T val)
		{
			if (currIdx + offset - 1 < buff.Memory.Length)
			{
				val = default;
				return false;
			}

			val = Peek(offset);
			return true;
		}

		public ReadOnlySpan<T> AsSpan()
		{
			return buff.Memory.Span.Slice(0, currIdx);
		}

		public void Clear()
		{
			buff.Memory.Span.Clear();
			currIdx = 0;
		}

		public void Dispose()
		{
			buff.Dispose();
		}
    }
}