﻿using LightLexer;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace LightParser
{
	public enum ExitCode
	{
		Continue,
		Exit,
	}

	public interface IModel<TReturnType>
	{
		IParameter<TReturnType> this[ReadOnlySpan<char> name] { get; }

		void Set(ReadOnlySpan<char> name, IParameter<TReturnType> parameter);

		bool TryGet(ReadOnlySpan<char> name, out IParameter<TReturnType> parameter);

		IEnumerable<IParameter<TReturnType>> GetEnumerable();
	}

	public interface IParameter<TReturnType>
	{
		TReturnType GetType();

		bool TryGet<T>(out IEnumerable<T> value);

		bool TryGet<T>(out T value);

		bool TryGet(ReadOnlySpan<char> name, out IParameter<TReturnType> parameter);

		bool TrySet<T>(T value);
	}

	public interface IState<TNodeType, TEngineStates> where TNodeType : Enum where TEngineStates : Enum
	{
		ExitCode OnStep(ref Parser<TNodeType, TEngineStates> parsedTemplate, ref AbstractSyntaxTree<TNodeType> ast, ref Token token);
	}

	public ref struct AbstractSyntaxTree<T> where T : Enum
	{
		public ref Node<T> CurrentNode => ref nodeTree[currIdx];

		public ref Node<T> CurrentRoot => ref nodeTree[currRoot.Peek()];

		public readonly int CurrentIdx => currIdx;

		Span<Node<T>> nodeTree;
		RefStack<int> currRoot;

		int currIdx = 0;

		public AbstractSyntaxTree(Span<Node<T>> nodeTree)
		{
			this.nodeTree = nodeTree;
			currRoot = new RefStack<int>(64);
			currRoot.Push(0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetMiddle(int idx)
		{
			nodeTree[idx].middle = currIdx;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetRight(int idx)
		{
			nodeTree[idx].right = currIdx;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetLeft(int idx)
		{
			nodeTree[idx].left = currIdx;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<Node<T>> GetTree()
		{
			return nodeTree.Slice(0, currIdx);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Advance()
		{
			return currIdx++;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetStackDepth()
		{
			return currRoot.Count;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetRoot()
		{
			return currRoot.Peek();
		}

		public void PushRoot()
		{
			currRoot.Push(currIdx);
		}

		public void PopRoot()
		{
			if (currRoot.Count == 1)
				throw new Exception("Root stack must never be empty.");

			currRoot.Pop();
		}
	}

	public struct Node<T> where T : Enum
	{
		public T nodeType;
		public Token token;

		public int right;
		public int middle;
		public int left;

		public Node(T nodeType, Token token, int right, int middle, int left)
		{
			this.nodeType = nodeType;
			this.token = token;
			this.right = right;
			this.middle = middle;
			this.left = left;
		}

		public static void Create(ref Node<T> node, T nodeType, Token token = default, int right = -1, int middle = -1, int left = -1)
		{
			node = new()
			{
				nodeType = nodeType,
				token = token,
				right = right,
				middle = middle,
				left = left
			};
		}
	}

	public ref struct TemplateContext<TNodeType, TReturnType> where TNodeType : Enum
	{
		public ReadOnlySpan<char> txt;
		public ReadOnlySpan<Node<TNodeType>> nodes;
		public ReadOnlySpan<TReturnType> returnTypes;
	};

	public ref struct TypeResolver<TNodeType, TReturnType> where TNodeType : Enum where TReturnType : Enum
	{
		public delegate TReturnType TypeResolveMethod(TNodeType type, TReturnType right, TReturnType middle, TReturnType left);

		TypeResolveMethod typeResolver;

		public TypeResolver(TypeResolveMethod typeResolver)
		{
			this.typeResolver = typeResolver;
		}

		public Span<TReturnType> ResolveTypes(int root, ReadOnlySpan<Node<TNodeType>> nodes, Span<TReturnType> types)
		{
			ComputeType(root, nodes, types);
			return types.Slice(0, nodes.Length);
		}

		TReturnType ComputeType(int root, ReadOnlySpan<Node<TNodeType>> nodes, Span<TReturnType> types)
		{
			if (root == -1)
				return default;

			ref readonly Node<TNodeType> node = ref nodes[root];

			TReturnType right = ComputeType(node.right, nodes, types);
			TReturnType middle = ComputeType(node.middle, nodes, types);
			TReturnType left = ComputeType(node.left, nodes, types);

			types[root] = typeResolver(node.nodeType, right, middle, left);
			return types[root];
		}
	}

	public ref struct Parser<TNodeType, TEngineState> where TNodeType : Enum where TEngineState : Enum
	{
		Dictionary<TEngineState, IState<TNodeType, TEngineState>> stateDict;

		TokenEnumerable.Enumerator enumerator;

		public Parser(Dictionary<TEngineState, IState<TNodeType, TEngineState>> stateDict, TokenEnumerable tokens)
		{
			this.stateDict = stateDict;
			this.enumerator = tokens.GetEnumerator();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public AbstractSyntaxTree<TNodeType> GetAst(Span<Node<TNodeType>> nodeTree)
		{
			return new(nodeTree);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CalculateAst(ref AbstractSyntaxTree<TNodeType> ast, TEngineState initialState)
		{
			CalculateAst(initialState, ref ast);
		}

		bool CalculateAst(TEngineState engineState, ref AbstractSyntaxTree<TNodeType> ast)
		{
			ref Token token = ref enumerator.Current;

			while (true)
			{
				if (!enumerator.MoveNext())
					return true;

				var code = stateDict[engineState].OnStep(ref this, ref ast, ref token);
				if (code == ExitCode.Exit)
					return false;
			}
		}

		public void Transition(TEngineState newState, ref AbstractSyntaxTree<TNodeType> ast, bool repeatToken = false)
		{
			ref Token token = ref enumerator.Current;

			if (repeatToken)
			{
				var code = stateDict[newState].OnStep(ref this, ref ast, ref token);
				if (code == ExitCode.Exit)
					return;
			}

			CalculateAst(newState, ref ast);
		}

		public bool Consume()
		{
			return enumerator.MoveNext();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ExitCode PopState()
		{
			return ExitCode.Exit;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ExitCode Continue()
		{
			return ExitCode.Continue;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsEnd()
		 => enumerator.IsEnd();
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

		public bool TryGet(TType type, TMask rightMask, TMask middleMask, TMask leftMask, out TAction action)
		{
			for (int i = 0; i < Count; i++)
			{
				(TType t, TMask right, TMask middle, TMask left) = maskBuff[i];

				if (t.Equals(type) && right.HasFlag(rightMask) && middle.HasFlag(middleMask) && left.HasFlag(leftMask))
				{
					action = methodBuff[i];
					return true;
				}
			}

			action = default;
			return false;
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

	public class ModelStack<TReturnType>
	{
		List<IModel<TReturnType>> stack = new List<IModel<TReturnType>>();

		public void Push(IModel<TReturnType> model)
		{
			stack.Add(model);
		}

		public void Pop()
		{
			stack.RemoveAt(stack.Count - 1);
		}

		public IModel<TReturnType> Peek()
		{
			return stack[stack.Count - 1];
		}

		public IModel<TReturnType> PeekBottom()
		{
			return stack[0];
		}

		public bool TryGet(ReadOnlySpan<char> name, out IParameter<TReturnType> parameter)
		{
			for (int i = stack.Count - 1; i >= 0; i--)
			{
				if (stack[i].TryGet(name, out parameter))
					return true;
			}

			parameter = default;
			return false;
		}
	}
}
