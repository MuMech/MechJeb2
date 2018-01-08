using System;
using System.Collections.Generic;

namespace MuMech
{
	public interface IMechJebModuleScriptActionContainer
	{
		int getRecursiveCount();
		List<MechJebModuleScriptAction> getRecursiveActionsList();
		List<MechJebModuleScriptActionsList> getActionsListsObjects();
	}
}

