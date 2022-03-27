using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Battle : MonoBehaviour {

	[SerializeField]
	GameObject goTeam1, goTeam2, goControl, goResult, goMatches, goEndGame;

	[SerializeField]
	TMP_Text txtTimer, txtEndGame;

	[SerializeField]
	Button btnReturnLobby;

	static Battle _this;

	Client client;
	bool canSetAction = true;
	public string myusername = "";

	public static Battle Get() {
		return _this;
	}

	private void Start() {
		_this = this;

		Debug.Log("BATTLE.START");
		client = Client.Get();

		if (!client.connected.Get()) {
			SceneManager.LoadScene("Lobby");
			return;
		}

		UnsetActions();

		btnReturnLobby.onClick.AddListener(() => {
			SceneManager.LoadScene("Lobby");
		});

		client.On("battle", ProcessBattleEvent);
		RequestData();
	}

	Dictionary<string, BattlePlayer> bPlayers = new Dictionary<string, BattlePlayer>();

	public List<RawPlayer> team1, team2;

	async void RequestData() {
		Debug.Log("RequestData()");
		var data = new Hashtable();
		data["ev"] = "get-data";
		var result = await client.Emit("battle", data);

		bPlayers.Clear();

		var bdata = result.ToObject<WsBattleData>();
		myusername = bdata.username;

		team1 = bdata.team1;
		team2 = bdata.team2;

		PlotTeam(goTeam1, bdata.team1);
		PlotTeam(goTeam2, bdata.team2);

		StartTurn(bdata.timer);
	}

	void PlotTeam(GameObject goTeam, List<RawPlayer> players) {
		var goList = goTeam.transform.Find("Players");
		UIX.Empty(goList.transform);

		players.ForEach(player => {
			var go = UIX.Add("Prefabs/Player", true, goList.gameObject);
			var bp = go.GetComponent<BattlePlayer>();
			bp.SetPlayer(player);
			bPlayers[player.username] = bp;
		});
	}

	public void UnsetActions() {
		var actions = gameObject.transform.Find("Control/Actions").GetComponentsInChildren<BattleAction>();
		foreach (var act in actions) act.SetSelected(false);
	}

	public async void SetAction(BattleAction action) {
		if (!canSetAction) return;

		UnsetActions();
		action.SetSelected(true);

		var data = new Hashtable();
		data["ev"] = "set-action";
		data["code"] = action.code;

		await client.Emit("battle", data);
	}


	int timer;
	float dtTimer;

	void StartTimer(int timer) {
		dtTimer = 0f;
		this.timer = timer;
		txtTimer.text = timer + "s";
	}

	void TickTimer() {
		timer--;
		txtTimer.text = timer + "s";
	}

	void StartTurn(int timer) {
		StartTimer(timer);
		goResult.SetActive(false);
		goControl.SetActive(true);
		UnsetActions();		
		foreach(var bp in bPlayers) bp.Value.SetAction(false, "");
		canSetAction = true;
	}

	void ProcessBattleEvent(WsEvent wsevent) {
		var ev = wsevent.data["ev"].ToString();

		if (ev == "start-turn") {
			var timer = wsevent.data["timer"].ToObject<int>();
			var stunned = wsevent.data["stunned"].ToObject<bool>();
			var hp = wsevent.data["hp"].ToObject<int>();
			bPlayers[myusername].SetHP(hp);
			bPlayers[myusername].SetStunned(stunned);
			StartTurn(timer);
			canSetAction = !stunned && hp > 0;
		}

		if (ev == "set-actions") {
			var actions = wsevent.data["actions"].ToObject<List<WsPlayerAction>>();
			foreach(var act in actions) bPlayers[act.username].SetAction(act.flag, act.action);
		}

		if (ev == "lock-action") {
			canSetAction = false;
		}

		if (ev == "turn-result") {
			var turnlog = wsevent.data["log"].ToObject<List<WsTurnLog>>();
			ShowTurnResult(turnlog);
		}

		if (ev == "end-game") {
			goEndGame.SetActive(true);
			txtEndGame.text = wsevent.data["result"].ToString();
		}
	}

	void ShowTurnResult(List<WsTurnLog> logs) {
		goResult.SetActive(true);
		UIX.Empty(goMatches.transform);

		foreach(var bp in bPlayers.Values) bp.SetStunned(false);

		var match = new BattleMatch();

		logs.ForEach(log => {
			if (log.type == "match") {
				var go = UIX.Add("Prefabs/Match", true, goMatches);
				match = go.GetComponent<BattleMatch>();
				match.SetPlayers(log.p1, log.p2, true);
			}

			if (log.type == "damage") {
				var player = bPlayers[log.player];
				player.SetHP((int)log.newhp);
				match.AddResult(log.player + " perdeu " + log.amount + " HP.");
			}

			if (log.type == "stun") {
				var player = bPlayers[log.player];
				player.SetStunned(true);
				match.AddResult(log.player + " foi paralizado.");
			}

			if (log.type == "update") {
				match.SetPlayers(log.p1, log.p2, false);
			}
		});
	}

	private void Update() {
		if (timer > 0) {
			dtTimer += Time.deltaTime;
			if (dtTimer >= 1f) {
				dtTimer -= 1f;
				TickTimer();
			}
		}
	}
}
class WsPlayerAction {
	public string username, action;
	public bool flag;
}


class WsBattleData {
	public bool status;
	public int timer;
	public string username;
	public List<RawPlayer> team1, team2;
}

public class RawPlayer {
	public string username, action;
	public bool stunned;
	public int? hp;
}

public class WsTurnLog {
	public string type, player;
	public RawPlayer p1, p2;
	public int? amount, newhp;
}