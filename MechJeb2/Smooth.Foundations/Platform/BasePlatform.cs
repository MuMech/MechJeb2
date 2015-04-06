using UnityEngine;
using System;

namespace Smooth.Platform {

	/// <summary>
	/// Enumeration representing the base platforms for Unity builds.
	/// </summary>
	public enum BasePlatform {
		None = 0,
		Android = 100,
		Bb10 = 200,
		Flash = 300,
		Ios = 400,
		Linux = 500,
		Metro = 600,
		NaCl = 700,
		Osx = 800,
		Ps3 = 900,
		Tizen = 1000,
		Wii = 1100,
		Windows = 1200,
		Wp8 = 1300,
		Xbox360 = 1400,
	}

	/// <summary>
	/// Extension methods related to the runtime / base platform.
	/// </summary>
	public static class PlatformExtensions {

		/// <summary>
		/// Returns the base platform for the specified runtime platform.
		/// </summary>
		public static BasePlatform ToBasePlatform(this RuntimePlatform runtimePlatform) {
			switch (runtimePlatform) {
			case RuntimePlatform.IPhonePlayer:
				return BasePlatform.Ios;
			case RuntimePlatform.Android:
				return BasePlatform.Android;
			case RuntimePlatform.WindowsEditor:
			case RuntimePlatform.WindowsPlayer:
			case RuntimePlatform.WindowsWebPlayer:
				return BasePlatform.Windows;
			case RuntimePlatform.OSXEditor:
			case RuntimePlatform.OSXPlayer:
			case RuntimePlatform.OSXWebPlayer:
			case RuntimePlatform.OSXDashboardPlayer:
				return BasePlatform.Osx;
			case RuntimePlatform.LinuxPlayer:
				return BasePlatform.Linux;
			case RuntimePlatform.XBOX360:
				return BasePlatform.Xbox360;
			case RuntimePlatform.PS3:
				return BasePlatform.Ps3;
			case RuntimePlatform.FlashPlayer:
				return BasePlatform.Flash;
			case RuntimePlatform.NaCl:
				return BasePlatform.NaCl;
#if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1
			case RuntimePlatform.WP8Player:
				return BasePlatform.Wp8;
			case RuntimePlatform.MetroPlayerX86:
			case RuntimePlatform.MetroPlayerX64:
			case RuntimePlatform.MetroPlayerARM:
				return BasePlatform.Metro;
			case RuntimePlatform.TizenPlayer:
				return BasePlatform.Tizen;
#endif
			default:
				return BasePlatform.None;
			}
		}

		/// <summary>
		/// Returns true if the specified platform supports JIT compilation; otherwise, false.
		/// </summary>
		public static bool HasJit(this RuntimePlatform runtimePlatform) {
			return (
				runtimePlatform != RuntimePlatform.IPhonePlayer &&
				runtimePlatform != RuntimePlatform.PS3 &&
				runtimePlatform != RuntimePlatform.XBOX360);
		}

		/// <summary>
		/// Returns true if the specified platform supports JIT compilation; otherwise, false.
		/// </summary>
		public static bool HasJit(this BasePlatform basePlatform) {
			return (
				basePlatform != BasePlatform.Ios &&
				basePlatform != BasePlatform.Ps3 &&
				basePlatform != BasePlatform.Xbox360 &&
				basePlatform != BasePlatform.Wii);
		}

		/// <summary>
		/// Returns true if the specified platform does not support JIT compilation; otherwise, false.
		/// </summary>
		public static bool NoJit(this RuntimePlatform runtimePlatform) {
			return !HasJit(runtimePlatform);
		}
		
		/// <summary>
		/// Returns true if the specified platform does not support JIT compilation; otherwise, false.
		/// </summary>
		public static bool NoJit(this BasePlatform basePlatform) {
			return !HasJit(basePlatform);
		}
	}
}
