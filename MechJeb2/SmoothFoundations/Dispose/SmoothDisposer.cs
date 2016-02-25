using UnityEngine;
using System;
using Smooth.Dispose;

public class SmoothDisposer : MonoBehaviour {
	private static SmoothDisposer instance;

	private void Awake() {
		if (instance) {
			Debug.LogWarning("Only one " + GetType().Name + " should exist at a time, instantiated by the " + typeof(DisposalQueue).Name + " class.");
			Destroy(this);
		} else {
			instance = this;
			DontDestroyOnLoad(this);
		}
	}
	
	private void LateUpdate() {
		DisposalQueue.Pulse();
	}
}