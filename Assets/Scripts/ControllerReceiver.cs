using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using PDollarGestureRecognizer;
using System.IO;

public class ControllerReceiver : MonoBehaviour {
	NetworkServerSimple srv = null;
	int receiverPort = 2581;

	public static bool hasGyro = false;
	public static bool hasAccel = false;
	public static bool hasVib = false;
	public static bool hasCompass = false;


	static Dictionary<string, float> inputFloat = new Dictionary<string, float> ();
	static Dictionary<string, Vector2> inputVector2 = new Dictionary<string, Vector2> ();
	static Dictionary<string, Vector3> inputVector3 = new Dictionary<string, Vector3> ();
	static Dictionary<string, Quaternion> inputQuaternion = new Dictionary<string, Quaternion> ();
	static Dictionary<string, Result> inputGesture = new Dictionary<string, Result> ();
	static  Dictionary<string, bool> inputBool = new Dictionary<string, bool> ();

	public static Result EmptyGesture = new Result(){GestureClass = null, Score = 0};
    
    private PDollarGestureRecognizer.Gesture[] trainingSet;

	public Text info = null;

    public class NetMsgId {
        public const short hasGyro = 100;
        public const short hasAccel = 101;
        public const short hasVib = 102;
        public const short inputFloat = 103;
        public const short inputVector2 = 104;
        public const short inputVector3 = 105;
        public const short inputQuaternion = 106;
        public const short inputGesture = 107;
        public const short inputBool = 108;
        public const short vibrate = 109;
        public const short inputButton = 110;
        public const short inputVector2Array = 111;
        public const short hasCompass = 112;
    }

	// Use this for initialization
	void Start () {
		NetworkReader read;
		srv = new NetworkServerSimple ();
		ConnectionConfig config = new ConnectionConfig ();
		config.FragmentSize = 8;
		config.PacketSize = 256;
		config.MaxSentMessageQueueSize = 4;
		config.MaxCombinedReliableMessageSize = 4;
		config.WebSocketReceiveBufferMaxSize = 4;
		config.AddChannel (QosType.ReliableSequenced);
		config.AddChannel (QosType.Unreliable);
		srv.Configure (config, 1);
		srv.Listen (receiverPort);

		NetworkClient cli = new NetworkClient ();
		srv.RegisterHandler (NetMsgId.hasGyro, (msg) => hasGyro = msg.reader.ReadBoolean ());
		srv.RegisterHandler (NetMsgId.hasVib, (msg) => hasVib = msg.reader.ReadBoolean ());
		srv.RegisterHandler (NetMsgId.hasAccel, (msg) => hasAccel = msg.reader.ReadBoolean ());
		srv.RegisterHandler (NetMsgId.hasCompass, (msg) => hasCompass = msg.reader.ReadBoolean ());
		srv.RegisterHandler (NetMsgId.inputFloat, InputFloat);
		srv.RegisterHandler (NetMsgId.inputVector2, InputVector2);
		srv.RegisterHandler (NetMsgId.inputVector3, InputVector3);
		srv.RegisterHandler (NetMsgId.inputQuaternion, InputQuaternion);
		srv.RegisterHandler (NetMsgId.inputGesture,InputGesture);
        srv.RegisterHandler (NetMsgId.inputBool,InputBool);

		StartCoroutine ("WaitForDisconnection");
        
        List<PDollarGestureRecognizer.Gesture> trainingSetList = new List<PDollarGestureRecognizer.Gesture>();
        //Load pre-made gestures
        TextAsset[] gesturesXml = Resources.LoadAll<TextAsset>("GestureSet/10-stylus-MEDIUM/");
        foreach (TextAsset gestureXml in gesturesXml)
            trainingSetList.Add(GestureIO.ReadGestureFromXML(gestureXml.text));

        //Load user custom gestures
        string[] filePaths = Directory.GetFiles(Application.persistentDataPath, "*.xml");
        foreach (string filePath in filePaths)
            trainingSetList.Add(GestureIO.ReadGestureFromFile(filePath));
        trainingSet = trainingSetList.ToArray ();
	}

	public IEnumerator WaitForConnection(){
		while (srv.connections.Count == 0) {
			yield return new WaitForEndOfFrame ();
		}
		if (srv.connections.Count > 0) {
			info.text = "Local IP = " + Network.player.ipAddress + "\nConnected.";
			StartCoroutine ("WaitForDisconnection");
		}
	}

	public IEnumerator WaitForDisconnection(){
		while (srv.connections.Count > 0) {
			yield return new WaitForEndOfFrame ();
		}
		if (srv.connections.Count == 0) {
			info.text = "Local IP = " + Network.player.ipAddress + "\nNot connected.";
			StartCoroutine ("WaitForConnection");
		}
	}

	public void InputFloat(NetworkMessage msg){
		string name = msg.reader.ReadString ();
		float value = (float)msg.reader.ReadDouble ();
		if (inputFloat.ContainsKey (name))
			inputFloat [name] = value;
		else
			inputFloat.Add (name, value);
	}
	public void InputVector2(NetworkMessage msg){
		string name = msg.reader.ReadString ();
		Vector2 value = msg.reader.ReadVector2 ();
		if (inputVector2.ContainsKey (name))
			inputVector2 [name] = value;
		else
			inputVector2.Add (name, value);
	}
	public void InputVector3(NetworkMessage msg){
		string name = msg.reader.ReadString ();
		Vector3 value = msg.reader.ReadVector3 ();
		if (inputVector3.ContainsKey (name))
			inputVector3 [name] = value;
		else
			inputVector3.Add (name, value);
	}
	public void InputQuaternion(NetworkMessage msg){
		string name = msg.reader.ReadString ();
		Quaternion value = msg.reader.ReadQuaternion ();
		if (inputQuaternion.ContainsKey (name))
			inputQuaternion [name] = value;
		else
			inputQuaternion.Add (name, value);
	}
	public void InputGesture(NetworkMessage msg){
		string name = msg.reader.ReadString ();
		int length = msg.reader.ReadInt32 ();
		Point[] points = new Point[length];
		for (int a = 0; a < length; a++) {
			Vector2 v = msg.reader.ReadVector2 ();
			points [a] = new Point (v.x, v.y, msg.reader.ReadInt32());
		}
		PDollarGestureRecognizer.Gesture candidate = new PDollarGestureRecognizer.Gesture (points);
		Result gestureResult = PointCloudRecognizer.Classify (candidate, trainingSet);

		if (inputGesture.ContainsKey (name))
			inputGesture [name] = gestureResult;
		else
			inputGesture.Add (name, gestureResult);
	}
	public void InputBool(NetworkMessage msg){
		string name = msg.reader.ReadString ();
		bool value = msg.reader.ReadBoolean ();
		if (inputBool.ContainsKey (name))
			inputBool [name] = value;
		else
			inputBool.Add (name, value);
	}

	public static bool GetFloat(string name, ref float ret){
		if (inputFloat.ContainsKey (name)) {
			ret = inputFloat [name];
			return true;
		}else
		return false;
	}

	public static float GetFloat(string name){
		if (inputFloat.ContainsKey (name))
			return inputFloat [name];
		else
			return 0;
	}

	public static bool GetVector2(string name, ref Vector2 ret){
		if (inputVector2.ContainsKey (name)) {
			ret = inputVector2 [name];
			return true;
		} else
			return false;
	}

	public static Vector2 GetVector2(string name){
		if (inputVector2.ContainsKey (name))
			return inputVector2 [name];
		else
			return Vector2.zero;
	}

	public static bool GetVector3(string name, ref Vector3 ret){
		if (inputVector3.ContainsKey (name)) {
			ret = inputVector3 [name];
			return true;
		} else
			return false;
	}

	public static Vector3 GetVector3(string name){
		if (inputVector3.ContainsKey (name))
			return inputVector3 [name];
		else
			return Vector3.zero;
	}

	public static bool GetQuaternion(string name, ref Quaternion ret){
		if (inputQuaternion.ContainsKey (name)) {
			ret = inputQuaternion [name];
			return true;
		} else
			return false;
	}

	public static Quaternion GetQuaternion(string name){
		if (inputQuaternion.ContainsKey (name))
			return inputQuaternion [name];
		else
			return Quaternion.identity;
	}

	public static bool GetGesture (string name, ref Result ret){
		if (inputGesture.ContainsKey (name)) {
			ret = inputGesture [name];
			return true;
		} else
			return false;
	}

	public static Result GetGesture(string name){
		if (inputGesture.ContainsKey (name))
			return inputGesture [name];
		else
			return EmptyGesture;
	}
	public static bool GetBool (string name, ref bool ret){
		if (inputBool.ContainsKey (name)) {
			ret = inputBool [name];
			return true;
		} else
			return false;
	}

	public static bool GetBool(string name){
		if (inputBool.ContainsKey (name))
			return inputBool [name];
		else
			return false;
	}

	
	// Update is called once per frame
	void Update () {
		srv.Update ();
	}
}
