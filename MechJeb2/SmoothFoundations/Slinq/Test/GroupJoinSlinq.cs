using UnityEngine;

#if !UNITY_3_5
namespace Smooth.Slinq.Test {
#endif
	
	public class GroupJoinSlinq : MonoBehaviour {
		private void Update() {
			for (int i = 0; i < SlinqTest.loops; ++i) {
				SlinqTest.tuples1.Slinq().GroupJoin(SlinqTest.tuples2.Slinq(), SlinqTest.to_1, SlinqTest.to_1, (a, bs) => bs.Count()).Count();
			}
		}
	}

#if !UNITY_3_5
}
#endif
