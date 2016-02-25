using System;
using System.Collections.Generic;

/// <summary>
/// Class based key value pair for use in inspector lists and arrays.
/// </summary>
[System.Serializable]
public abstract class InspectorKeyValuePair<K, V> {
	public K key;
	public V value;

	public InspectorKeyValuePair() {}

	public InspectorKeyValuePair(K key, V value) {
		this.key = key;
		this.value = value;
	}

	public KeyValuePair<K, V> ToKeyValuePair() {
		return new KeyValuePair<K, V>(key, value);
	}
}
