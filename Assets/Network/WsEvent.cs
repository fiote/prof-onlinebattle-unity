using Newtonsoft.Json.Linq;

public class WsEvent {

	public string channel, evtype;
	public int cbid;
	public JToken data;

	public WsEvent(string message) {
		var feed = JObject.Parse(message);
		if (feed["cbid"] != null) cbid = int.Parse(feed["cbid"].ToString());
		channel = feed["channel"].ToString();
		data = feed["data"];
		if (data != null && data["evtype"] != null) evtype = data["evtype"].ToString();
	}
}

public class WsGenericData {
	public bool status;
	public bool silent;
	public string error;
	public JToken data;

	override public string ToString() {
		return $"WsGenericData(status={status}, error={error}, data={data})";
	}
}