using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using Smooth.Collections;
using Smooth.Compare;

using EventType=Smooth.Compare.EventType;

#if !UNITY_3_5
namespace Smooth.Compare.Utilities {
#endif
	/// <summary>
	/// Simple utility that listens to finder events and logs all requests for unregistered comparers when the component is destroyed.
	/// 
	/// This can be useful when running an application meant for a non-JIT platform in a JIT-enabled simulator so you can test many code paths, then track down and fix any potential comparer issues.
	/// </summary>
	public class LogUnregisteredOnDestroy : MonoBehaviour {
		public bool destroyOnLoad;

		// Note: We don't use the registered comparer for Type because we want to add our event listener ASAP
		protected HashSet<Type> comparers = new HashSet<Type>();
		protected HashSet<Type> equalityComparers = new HashSet<Type>();

		protected void Awake() {
			if (!destroyOnLoad) {
				DontDestroyOnLoad(gameObject);
			}

			Finder.OnEvent.Handle += HandleFinderEvent;
		}
		
		protected void OnDestroy() {
			Finder.OnEvent.Handle -= HandleFinderEvent;

			if (comparers.Count > 0 || equalityComparers.Count > 0) {
				var sb = new StringBuilder();

				if (comparers.Count > 0) {
					sb.Append("Unregistered ").Append(ComparerType.Comparer.ToStringCached()).AppendLine("s :");
					foreach (var type in comparers) {
						sb.AppendLine(type.FullName);
					}
				}

				if (equalityComparers.Count > 0) {
					if (sb.Length > 0) {
						sb.AppendLine();
					}
					sb.Append("Unregistered ").Append(ComparerType.EqualityComparer.ToStringCached()).AppendLine("s :");
					foreach (var type in equalityComparers) {
						sb.AppendLine(type.FullName);
					}
				}

				Debug.Log(sb.ToString());
			}
		}

		protected virtual void HandleFinderEvent(ComparerType comparerType, EventType eventType, Type type) {
			if (eventType == EventType.FindUnregistered && type.IsValueType) {
				switch (comparerType) {
				case ComparerType.Comparer:
					comparers.Add(type);
					break;
				case ComparerType.EqualityComparer:
					equalityComparers.Add(type);
					break;
				default:
					break;
				}
			}
		}
	}
#if !UNITY_3_5
}
#endif
