using UnityEngine;

#if !UNITY_3_5
namespace Smooth.Slinq.Test {
#endif
	
	public class WhereTakeSelectAggregateSlinq : MonoBehaviour {
		private void Update() {
			for (int i = 0; i < SlinqTest.loops; ++i) {
				SlinqTest.tuples1.Slinq().Where(t => true).Take(int.MaxValue).Select(t => t).Aggregate(0, (acc, t) => 0);
			}
		}
	}

#if !UNITY_3_5
}
#endif
