using UnityEngine;

#if !UNITY_3_5
namespace Smooth.Slinq.Test {
#endif
	
	public class DistinctSlinq : MonoBehaviour {
		private void Update() {
			for (int i = 0; i < SlinqTest.loops; ++i) {
				SlinqTest.tuples1.Slinq().Distinct(SlinqTest.eq_1).Count();
			}
		}
	}

#if !UNITY_3_5
}
#endif
