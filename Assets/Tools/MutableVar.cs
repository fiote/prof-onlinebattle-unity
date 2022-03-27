using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class MutableVar<T> {

	private T value = default;
	private List<Action<T, T>> onChangeListeners = new List<Action<T, T>>();
	private List<Action<T, T>> onSetListeners = new List<Action<T, T>>();

	private List<Action<T, T>> onChangeListenersOnce = new List<Action<T, T>>();
	private List<Action<T, T>> onSetListenersOnce = new List<Action<T, T>>();

	public MutableVar() {
	}

	public MutableVar(T val) {
		Set(val);
	}

	public void Set(T val, bool trigger = true) {
		var oldvalue = value;
		var newvalue = val;
		value = val;
		if (!trigger) return;
		TriggerSet(oldvalue, newvalue);
		var oldnull = (oldvalue == null || oldvalue.Equals(default(T)));
		var newnull = (newvalue == null || newvalue.Equals(default(T)));
		if (oldnull && newnull) return;
		if (oldnull != newnull || !oldvalue.Equals(newvalue)) TriggerChange(oldvalue, newvalue);
	}

	public T Get() {
		return Parse(value);
	}

	public bool Valued() {
		return Get() != null;
	}

	public T Parse(T val) {
		try {
			return (T)Convert.ChangeType(val, typeof(T));
		} catch (InvalidCastException) {
			Debug.LogError("InvalidCastException");
			return default(T);
		}
	}

	public async Task WaitValue(T val) {
		while (!Get().Equals(val)) await Task.Delay(10);
	}

	public void OnChange(Action<T, T> act) {
		onChangeListeners.Add(act);
	}

	public void OnSet(Action<T, T> act) {
		onSetListeners.Add(act);
	}

	public void OnChangeOnce(Action<T, T> act) {
		onChangeListenersOnce.Add(act);
	}

	public void OnSetOnce(Action<T, T> act) {
		onSetListenersOnce.Add(act);
	}

	public void TriggerChange(T oldvalue, T newvalue) {
		foreach (var act in onChangeListeners) act.Invoke(newvalue, oldvalue);
		foreach (var act in onChangeListenersOnce) act.Invoke(newvalue, oldvalue);
		onChangeListenersOnce.Clear();
	}

	public void ForceTrigger() {
		var value = Get();
		TriggerChange(value, value);
	}

	public void TriggerSet(T oldvalue, T newvalue) {
		foreach (var act in onSetListeners) act.Invoke(newvalue, oldvalue);
		foreach (var act in onSetListenersOnce) act.Invoke(newvalue, oldvalue);
		onSetListenersOnce.Clear();
	}

	public void Destroy() {
		onChangeListeners.Clear();
		onSetListeners.Clear();
		onChangeListeners = null;
		onSetListeners = null;
	}
}