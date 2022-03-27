
using System;
using System.Collections.Generic;
using UnityEngine;


class UIX : MonoBehaviour {

	public static GameObject Add(string path, bool zero = true, GameObject parent = null) {
		var resource = Resources.Load<GameObject>(path);

		GameObject go;

		try {
			go = zero ? Instantiate<GameObject>(resource, Vector3.zero, Quaternion.identity) : Instantiate(resource);
		} catch (Exception e) {
			Debug.LogError("Failed to Instantiate " + path);
			throw e;
		}

		if (parent != null) {
			go.transform.SetParent(parent.transform, !zero);
		}

		return go;
	}

	public static UnityEngine.Sprite LoadSprite(string path) {
		return Resources.Load<UnityEngine.Sprite>(path);
	}

	public static void Empty(Transform transform) {
		foreach (Transform child in transform) UnityEngine.Object.Destroy(child.gameObject);
	}

	static Dictionary<string, Color> cachedColors = new Dictionary<string, Color>();

	public static Color GetColor(string hex) {
		if (cachedColors.ContainsKey(hex)) return cachedColors[hex];
		Color color;
		ColorUtility.TryParseHtmlString(hex, out color);
		cachedColors[hex] = color;
		return color;
	}
}