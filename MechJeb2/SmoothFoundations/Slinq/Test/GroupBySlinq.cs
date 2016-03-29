using UnityEngine;

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
