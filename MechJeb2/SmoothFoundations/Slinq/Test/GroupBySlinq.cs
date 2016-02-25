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
	
	public class GroupBySlinq : MonoBehaviour {
		private void Update() {
			for (int i = 0; i < SlinqTest.loops; ++i) {
				SlinqTest.tuples1.Slinq().GroupBy(SlinqTest.to_1).ForEach(g => g.values.Count());
			}
		}
	}

#if !UNITY_3_5
}
#endif
