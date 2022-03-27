using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleAction : MonoBehaviour {

	[SerializeField]
	Button btnAction;
	
	[SerializeField]
	Outline outline;

	[SerializeField]
	public string code = "?";


	private void Start() {
		btnAction.onClick.AddListener(OnBtnClick);
	}

	void OnBtnClick() {
		Battle.Get().SetAction(this);
	}

	public void SetSelected(bool flag) {
		outline.enabled = flag;
	}

}