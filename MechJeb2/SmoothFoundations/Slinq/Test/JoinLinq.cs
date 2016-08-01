using UnityEngine;
using System.Linq;

#if !UNITY_3_5
namespace Smooth.Slinq.Test {
#endif
	
	public class JoinLinq : MonoBehaviour {
		private void Update() {
			for (int i = 0; i < SlinqTest.loops; ++i) {
				SlinqTest.tuples1.Join(SlinqTest.tuples2, SlinqTest.to_1f, SlinqTest.to_1f, (a, b) => 0).Count();
			}
		}
	}

#if !UNITY_3_5
}
#endif
