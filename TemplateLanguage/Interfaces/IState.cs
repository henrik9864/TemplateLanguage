using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokhenizer;

namespace TemplateLanguage
{
	internal interface IState
	{
		ExitCode OnStep(ref ParsedTemplate parsedTemplate, ref AbstractSyntaxTree ast, ref Token token);
	}
}
