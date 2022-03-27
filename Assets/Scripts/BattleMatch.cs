using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BattleMatch : MonoBehaviour {

    [SerializeField]
    BattlePlayer p1, p2;

    [SerializeField]
    TMP_Text txtResult;


    // Start is called before the first frame update
    void Start() {
        
    }

    public void SetPlayers(RawPlayer a, RawPlayer b, bool reset = false) {
        if (reset) txtResult.text = "";
        if (Battle.Get().team1.Exists(p => p.username == a.username)) {
            SetP1(a);
            SetP2(b);
        } else {
            SetP1(b);
            SetP2(a);
        }
    }

    public void SetP1(RawPlayer player) {
        SetP(p1, player);
    }

    public void SetP2(RawPlayer player) {
        SetP(p2, player);
    }

    void SetP(BattlePlayer p, RawPlayer player) {
        p.SetPlayer(player);
        p.SetStunned(player.stunned);
        p.SetHP((int)player.hp);
        p.SetAction(player.action);

    }

    public void SetHP(string username, int newhp) {
        if (p1.player.username == username) p1.SetHP(newhp);
        if (p2.player.username == username) p2.SetHP(newhp);
    }

    public void AddResult(string part) {
        txtResult.text += part + "\n";
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
