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
		void OnEnter(ref ParsedTemplate parsedTemplate, ref ParsedTemplate.State state);

		void OnExit(ref ParsedTemplate parsedTemplate, ref ParsedTemplate.State state);

		void OnStep(ref ParsedTemplate parsedTemplate, ref ParsedTemplate.State state);
	}
}
