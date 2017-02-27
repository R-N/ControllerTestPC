using UnityEngine;
using System.Collections;

public class BoxController : MonoBehaviour {
	public float speed = 0.5f;
	Transform myTrans = null;
	// Use this for initialization
	void Start () {
		myTrans = transform;
	}
	
	// Update is called once per frame
	void Update () {
		myTrans.rotation = Quaternion.RotateTowards(myTrans.rotation, ControllerReceiver.GetQuaternion("GyroAttitudeRaw"), 90);
		myTrans.position = myTrans.position + new Vector3 (ControllerReceiver.GetVector2("Joystick").x, ControllerReceiver.GetVector2("Joystick").y, 0) * speed * Time.deltaTime;
	}
}
