using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Smooth.Algebraics;
using Smooth.Delegates;
using Smooth.Slinq;

#if !UNITY_3_5
namespace Smooth.Slinq.Test {
#endif
	
	public class GroupByLinq : MonoBehaviour {
		private void Update() {
			for (int i = 0; i < SlinqTest.loops; ++i) {
				foreach (var grouping in SlinqTest.tuples1.GroupBy(SlinqTest.to_1f)) { grouping.Count(); }
			}
		}
	}

#if !UNITY_3_5
}
#endif
