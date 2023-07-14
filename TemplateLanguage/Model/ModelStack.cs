namespace TemplateLanguage
{
	public class ModelStack
	{
		List<IModel> stack = new List<IModel>();

		public void Push(IModel model)
		{
			stack.Add(model);
		}

		public void Pop()
		{
			stack.RemoveAt(stack.Count - 1);
		}

		public IModel Peek()
		{
			return stack[stack.Count - 1];
		}

		public bool TryGet(ReadOnlySpan<char> name, out IParameter parameter)
		{
			for (int i = 0; i < stack.Count; i++)
			{
				if (stack[i].TryGet(name, out parameter))
					return true;
			}

			parameter = default;
			return false;
		}
	}
}
