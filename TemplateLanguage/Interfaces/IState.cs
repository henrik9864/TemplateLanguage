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
		void OnEnterAbove(ref ParsedTemplate parsedTemplate, ref ParsedTemplate.State state, EngineState prevState);

		void OnEnterBelow(ref ParsedTemplate parsedTemplate, ref ParsedTemplate.State state, EngineState prevState);

		void OnExitAbove(ref ParsedTemplate parsedTemplate, ref ParsedTemplate.State state, EngineState newState);

		void OnExitBelow(ref ParsedTemplate parsedTemplate, ref ParsedTemplate.State state, EngineState newState);

		void OnStep(ref ParsedTemplate parsedTemplate, ref ParsedTemplate.State state);
	}
}
