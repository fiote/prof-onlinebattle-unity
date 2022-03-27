using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Client : Singleton<Client> {

	WebSocket websocket;
	readonly string serverHost = "127.0.0.1";
	readonly int serverPort = 3000;

	public MutableVar<string> status = new MutableVar<string>();
	public MutableVar<bool> connected = new MutableVar<bool>(false);

	int cbid = 0;

	Dictionary<string, Action<WsEvent>> bindings = new Dictionary<string, Action<WsEvent>> {
	};

	public void On(string channel, Action<WsEvent> action) {
		bindings[channel] = action;
	}

	// ===== STATIC GET =============================================

	static Client _this;

	public static Client Get() {
		return _this;
	}

	// ===== START ==================================================

	void Start() {
		Debug.Log("NetworkClient.Start()");
		_this = this;
		Setup();
	}
	// ===== SETUP ==================================================

	void Setup() {
		Debug.Log("NetworkClient.Setup()");
		InitSocket();
		SetupCoreEvents();
		SetupInternalEvents();
		SetupChannelEvents();
		Connect();
	}

	void InitSocket() {
		websocket = new WebSocket("ws://" + serverHost + ":" + serverPort);
		status.Set("init");
	}

	async void Connect() {
		// waiting for messages
		status.Set("connecting");
		await websocket.Connect();
	}

	void SetupCoreEvents() {
		websocket.OnOpen += OnOpen;
		websocket.OnClose += OnClose;

		websocket.OnMessage += (bytes) => {
			var message = System.Text.Encoding.UTF8.GetString(bytes);
			var feed = new WsEvent(message);
			if (feed.channel == "callback") FireCallback(feed.cbid, feed.data);
		};
	}

	int iRetryCount = 0;
	readonly int iRetryMax = 99;

	void OnOpen() {
		Debug.Log("NetworkClient.OnOpen()");
		status.Set("open");
		connected.Set(true);
	}

	void OnClose(WebSocketCloseCode code) {
		Debug.Log("NetworkClient.OnClose()");

		if (code != WebSocketCloseCode.Normal) Debug.LogError($"Client.OnClose({code})");

		iRetryCount++;
		
		status.Set("closed");
		connected.Set(false);

		if (iRetryCount > iRetryMax) {
			Application.Quit();
			return;
		}

		if (code == WebSocketCloseCode.Abnormal) {
			Setup();
		}
	}

	async public Task WaitOpen(bool inner = false) {
		while (!connected.Get()) {
			await Task.Delay(TimeSpan.FromMilliseconds(50));
		}
	}

	// ===== INTERNAL ===============================================

	void SetupInternalEvents() {
		websocket.OnMessage += (bytes) => {
			var message = System.Text.Encoding.UTF8.GetString(bytes);
			var feed = new WsEvent(message);
			if (feed.channel != "internal") return;
			if (feed.evtype == "ping") {
				var data = new Hashtable();
				data["evtype"] = "ping";
				EmitBack(feed.cbid, data);
			}
		};

	}

	public void ForceDisconnect(string reason) {
		DestroyConnection();
	}

	public void DestroyConnection() {
		Destroy(gameObject);
	}

	void SetupChannelEvents() {
		websocket.OnMessage += (bytes) => {
			var message = System.Text.Encoding.UTF8.GetString(bytes);
			var feed = new WsEvent(message);
			if (bindings.ContainsKey(feed.channel)) {
				bindings[feed.channel].Invoke(feed);
			} else {
				if (feed.channel == "callback") return;
				if (feed.channel == "internal") return;
				throw new Exception("CHANNEL " + feed.channel + " NOT REGISTERED");
			}
		};
	}

	List<string> stackMsgs = new List<string>();

	List<CallbackData> stackCallbacks = new List<CallbackData>();
	void ProcessStackMsgs() {
		var stack = stackMsgs;
		stackMsgs.Clear();
		foreach (var message in stack) EmitMessage(message);
	}

	async void EmitMessage(string message) {
		if (status.Get() != "open") {
			stackMsgs.Add(message);
			return;
		}
		ProcessStackMsgs();
		await websocket.SendText(message);
	}

	public void EmitBack(int cbid, Hashtable data) {
		Hashtable package = new Hashtable();
		package["channel"] = "callback";
		package["cbid"] = cbid;
		package["data"] = data;
		var message = JsonConvert.SerializeObject(package);
		EmitMessage(message);
	}

	public static string Stringify(Hashtable data) {
		return JsonConvert.SerializeObject(data);
	}

	public async Task<JToken> Emit(string channel, Hashtable data, Action<JToken> action = null) {
		Hashtable package = new Hashtable();
		package["channel"] = channel;
		package["data"] = data;

		var cb = new CallbackData(cbid);
		stackCallbacks.Add(cb);
		package["cbid"] = cbid;
		cbid++;

		if (action != null) cb.OnResolved(action);

		var message = Stringify(package);
		EmitMessage(message);

		while (!cb.IsResolved()) await Task.Delay(TimeSpan.FromMilliseconds(50));

		var result = cb.GetResult();
		cb.Destroy();

		return result;
	}


	void FireCallback(int cbid, JToken data) {
		var cb = stackCallbacks.Find(x => x.IsId(cbid));
		if (cb != null) {
			cb.SetResult(data);
			stackCallbacks.Remove(cb);
		}
	}

	// ===== INNER METHODS ==========================================

	void Update() {
#if !UNITY_WEBGL || UNITY_EDITOR
		websocket?.DispatchMessageQueue();
#endif
	}

	private async void OnApplicationQuit() {
		await websocket?.Close();
	}
}

[Serializable]
class CallbackData {

	int cbid;
	JToken result;
	MutableVar<bool> resolved = new MutableVar<bool>(false);

	public CallbackData(int cbid) {
		this.cbid = cbid;
	}

	public bool IsId(int check) {
		return cbid == check;
	}

	public bool IsResolved() {
		return resolved.Get();
	}

	public void SetResult(JToken value) {
		result = value;
		resolved.Set(true);
	}

	public void OnResolved(Action<JToken> action) {
		resolved.OnSet((value, before) => { action(value); });
	}

	public JToken GetResult() {
		return result;
	}

	public void Destroy() {
		result = null;
		resolved.Destroy();
	}
}
