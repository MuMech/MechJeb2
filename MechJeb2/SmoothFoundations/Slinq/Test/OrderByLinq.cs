using UnityEngine;
using System.Linq;

#if !UNITY_3_5
namespace Smooth.Slinq.Test {
#endif
	
	public class OrderByLinq : MonoBehaviour {
		private void Update() {
			for (int i = 0; i < SlinqTest.loops; ++i) {
				SlinqTest.tuples1.OrderBy(SlinqTest.to_1f).FirstOrDefault();
			}
		}
	}

#if !UNITY_3_5
}
#endif
