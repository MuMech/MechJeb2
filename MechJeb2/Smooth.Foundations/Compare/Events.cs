using System;

namespace Smooth.Compare {
	public enum ComparerType {
		None = 0,
		Comparer,
		EqualityComparer,
	}

	public enum EventType {
		None = 0,
		Registered,
		AlreadyRegistered,
		FindRegistered,
		FindUnregistered,
		EfficientDefault,
		InefficientDefault,
		InvalidDefault,
		CustomJit,
		FactoryJit,
	}

	public static class EventExtensions {
		public static string ToStringCached(this ComparerType comparerType) {
			switch (comparerType) {
			case ComparerType.Comparer:
				return "Sort Order Comparer";
			case ComparerType.EqualityComparer:
				return "Equality Comparer";
			default:
				return "Unknown";
			}
		}
	}
}