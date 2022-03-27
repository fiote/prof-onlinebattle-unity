using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePlayer : MonoBehaviour {

	[SerializeField]
	Image imgPlayer, imgBg;

	[SerializeField]
	TMP_Text txtUsername, txtHP;

	[SerializeField]
	Outline outline;

	public RawPlayer player;
	bool stunned = false;
	int hp = 0;

	public void SetAction(bool flag, string code) {
		outline.enabled = flag;
		if (code != "") {
			imgPlayer.sprite = UIX.LoadSprite("Images/" + code);
		}
	}

	public void SetAction(string code) {
		outline.enabled = false;
		if (code == "" && hp == 0) return;
		imgPlayer.sprite = UIX.LoadSprite("Images/" + (code != "" ? code : "idle"));
	}

	public void SetHP(int value) {
		hp = value;
		txtHP.text = hp + "/3";
		if (hp == 0) imgPlayer.sprite = UIX.LoadSprite("Images/tombstone");
	}

	public void SetStunned(bool flag) {
		if (hp == 0) return;
		stunned = flag;
		imgPlayer.sprite = UIX.LoadSprite("Images/" + (flag ? "stunned" : "stable"));
	}

	internal void SetPlayer(RawPlayer player) {
		this.player = player;
		txtUsername.text = player.username;
		SetAction(false, "");
		SetMainPlayer(player.username == Battle.Get().myusername);
	}

	void SetMainPlayer(bool flag) {
		imgBg.color = flag ? UIX.GetColor("#0C0080") : UIX.GetColor("#353535");
	}
}