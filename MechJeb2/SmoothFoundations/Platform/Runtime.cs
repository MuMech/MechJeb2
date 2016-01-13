using UnityEngine;
using System;

namespace Smooth.Platform {

	/// <summary>
	/// Helper class that provides information about the target platform.
	/// </summary>
	public static class Runtime {

		/// <summary>
		/// The target runtime platform.
		/// </summary>
		public static readonly RuntimePlatform platform = Application.platform;

		/// <summary>
		/// The base platform of the target runtime.
		/// </summary>
		public static readonly BasePlatform basePlatform = platform.ToBasePlatform();

		/// <summary>
		/// True if the base platform supports JIT compilation; otherwise false.
		/// </summary>
		public static readonly bool hasJit = basePlatform.HasJit();

		/// <summary>
		/// True if the base platform does not support JIT compilation; otherwise false.
		/// </summary>
		public static readonly bool noJit = !hasJit;

	}
}