using UnityEngine;

#if !UNITY_3_5
namespace Smooth.Slinq.Test {
#endif
	
	public class JoinSlinq : MonoBehaviour {
		private void Update() {
			for (int i = 0; i < SlinqTest.loops; ++i) {
				SlinqTest.tuples1.Slinq().Join(SlinqTest.tuples2.Slinq(), SlinqTest.to_1, SlinqTest.to_1, (a, b) => 0).Count();
			}
		}
	}

#if !UNITY_3_5
}
#endif
