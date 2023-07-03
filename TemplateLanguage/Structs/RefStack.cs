using System.Buffers;
using System.Collections;

namespace TemplateLanguage
{
	public class MethodContainer<TType, TMask, TAction> : IEnumerable<TAction> where TMask : Enum where TType : Enum
	{
		public int Count => maskBuff.Count;

		List<(TType, TMask, TMask, TMask)> maskBuff;
		List<TAction> methodBuff;

		public MethodContainer()
		{
			this.maskBuff = new();
			this.methodBuff = new();
		}

		public void Add(TType type, TMask rightMask, TMask middleMask, TMask leftMask, TAction action)
		{
            maskBuff.Add((type, rightMask, middleMask, leftMask));
			methodBuff.Add(action);
		}

		public void Add(TType type, TMask rightMask, TMask leftMask, TAction action)
		{
			maskBuff.Add((type, rightMask, default, leftMask));
			methodBuff.Add(action);
		}

		public void Add(TType type, TMask rightMask, TAction action)
		{
			maskBuff.Add((type, rightMask, default, default));
			methodBuff.Add(action);
		}

		public void Add(TType type, TAction action)
		{
			maskBuff.Add((type, default, default, default));
			methodBuff.Add(action);
		}

		public TAction Get(TType type, TMask rightMask, TMask middleMask, TMask leftMask)
		{
			for (int i = 0; i < Count; i++)
			{
				(TType t, TMask right, TMask middle, TMask left) = maskBuff[i];

				if (t.Equals(type) && right.HasFlag(rightMask) && middle.HasFlag(middleMask) && left.HasFlag(leftMask))
				{
					return methodBuff[i];
				}
			}

			throw new Exception($"Node not found, Type: {type}, Right: {rightMask}, Middle: {middleMask}, Left: {leftMask}");
		}

		public IEnumerator<TAction> GetEnumerator()
		{
			return methodBuff.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return methodBuff.GetEnumerator();
		}
	}

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