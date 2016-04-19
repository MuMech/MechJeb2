using UnityEngine;

#if !UNITY_3_5
namespace Smooth.Slinq.Test {
#endif
	
	public class OrderBySlinq : MonoBehaviour {
		private void Update() {
			for (int i = 0; i < SlinqTest.loops; ++i) {
				SlinqTest.tuples1.Slinq().OrderBy(SlinqTest.to_1).FirstOrDefault();
			}
		}
	}

#if !UNITY_3_5
}
#endif
