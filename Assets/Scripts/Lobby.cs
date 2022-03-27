using Newtonsoft.Json.Linq;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Lobby : MonoBehaviour {

	Client client;
	
	[SerializeField]
	Button btnEnterBattle, btnLeaveBattle;
	
	[SerializeField]
	TMP_Text txtEnterBattle, txtErroLogin;
	
	[SerializeField]
	TMP_InputField inputUsername;

	private void Start() {
		client = Client.Get();

		btnEnterBattle.onClick.AddListener(ClickEnterBattle);
		btnLeaveBattle.onClick.AddListener(ClickLeaveBattle);
		client.On("lobby", ProcessLobbyEvent);

		GetLobby();
	}

	void ProcessLobbyEvent(WsEvent wsevent) {
		Debug.Log("ProcessLobbyEvent()");
		var ev = wsevent.data["ev"].ToString();

		if (ev == "battlesize") {
			var size = wsevent.data["size"].ToObject<int>();
			txtEnterBattle.text = $"Entrar Batalha {size}/6";
		}

		if (ev == "go-battle") {
			SceneManager.LoadScene("Battle");
		}
	}

	void GetLobby() {
		var data = new Hashtable();
		data["ev"] = "get-lobby";
		_ = client.Emit("lobby", data);
	}

	async void ClickEnterBattle() {
		if (!client.connected.Get()) return;

		SetOnQueue(true);
		txtErroLogin.text = "";

		var data = new Hashtable();
		data["ev"] = "enter-battle";
		data["username"] = inputUsername.text;
		var result = await client.Emit("lobby", data);

		var status = result["status"].ToObject<bool>();

		if (!status) {
			var error = result["error"].ToString();
			txtErroLogin.text = error;
			SetOnQueue(false);
		}
	}

	void SetOnQueue(bool flag) {
		inputUsername.enabled = !flag;
		btnEnterBattle.enabled = !flag;
		btnLeaveBattle.gameObject.SetActive(flag);
	}

	async void ClickLeaveBattle() {
		if (!client.connected.Get()) return;

		var data = new Hashtable();
		data["ev"] = "leave-battle";
		var result = await client.Emit("lobby", data);

		SetOnQueue(false);
	}
}
