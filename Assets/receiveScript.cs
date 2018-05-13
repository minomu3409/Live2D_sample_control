using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Net;
using System.Net.Sockets;
using System.Threading;

using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;

public class receiveScript : MonoBehaviour {
	// Cubism Parameters
	private const int FaceAngleX = 0;
	private const int FaceAngleY = 1;
	private const int FaceAngleZ = 2;
	private const int Cheek = 3;
	private const int EyeLOpen = 4;
	private const int EyeLSmile = 5;
	private const int EyeROpen = 6;
	private const int EyeRSmile = 7;
	private const int EyeBallX = 8;
	private const int EyeBallY = 9;

	private const int MouthForm = 12;
	private const int MouthOpenY = 13;
	private const int BodyAngleX = 14;
	private const int BodyAngleY = 15;
	private const int BodyAngleZ = 16;

	private float lastEyeLOpen = 0;
	private float lastEyeROpen = 0;
	private float lastFaceAngleX = 0;
	private float lastBodyAngleX = 0;
	private float lastFaceAngleY = 0;
	private float lastBodyAngleY = 0;
	private float lastFaceAngleZ = 0;
	private float lastBodyAngleZ = 0;
	private float lastEyeSight = 0;

	private bool bodyAngleInit = true;
	private float bodyPivotX;
	private const float bodyPivotY = 480f;

	private static float eyeSightRecv = 0.5f;

	struct point2f
	{
		public float x;
		public float y;
	}

	// モデルを動かす用
	private CubismModel _model;
	private float _t;

	// 通信用パラメータ
	int PORT = 50001;
	static UdpClient udp;
	Thread thread;
	const int maxPartsNum = 68;
	static point2f[] points = new point2f[68+1];

	// Use this for initialization
	void Start () {
		_model = this.FindCubismModel ();

		udp = new UdpClient(PORT);
		udp.Client.ReceiveTimeout = 0xFFFFFFF;
		thread = new Thread(new ThreadStart(ThreadMethod));
		thread.Start(); 
	}
	
	// Update is called once per frame
	void Update () {
//		IPEndPoint remoteEP = null;
//		byte[] buff = udp.Receive(ref remoteEP);
//		string text = "";
//		for (int i = 0; i < 68; i++) {
//			unsafe{
//				fixed(byte* buffP = &buff[8*i]) 
//				{
//					int pointX = *(int*)&buffP[0];
//					int pointY = *(int*)&buffP[4];
//					text += i + "X: " + pointX + " " + i + "Y: " + pointY + " ";
//				}
//			}
//		}
////		Debug.Log (text);
	}

	void LateUpdate () {
		if (points [0].x != 0) {
			float value;
			CubismParameter parameter;

			// 体の回転 Z
			if (bodyAngleInit) {
				bodyPivotX = points [8].x;
				bodyAngleInit = false;
			}
			value = 180f/Mathf.PI*Mathf.Atan2(bodyPivotY - points [8].y, bodyPivotX - points [8].x);
			value = value - 90f;
			value = Mathf.Clamp((value - lastBodyAngleZ) * 0.1f + lastBodyAngleZ, -10f, 10f);	// 後で使うのでクランプしておく
			parameter = _model.Parameters [BodyAngleZ];
			parameter.Value = value;
			lastBodyAngleZ = value;
//			Debug.Log("BodyAngleZ: "+value);

			// 顔の回転 Z
			float noseAngle = 180f/Mathf.PI*Mathf.Atan2(points [33].y - points [27].y, points [33].x - points [27].x);
			float mouthAngle = 180f/Mathf.PI*Mathf.Atan2(points [35].y - points [31].y, points [35].x - points [31].x);
			value = (noseAngle - 90f + mouthAngle) / 2f;
			value = value / 20f * 30f - lastBodyAngleZ;
			value = (value - lastFaceAngleZ) * 0.2f + lastFaceAngleZ;
			parameter = _model.Parameters [FaceAngleZ];
			parameter.Value = value;
			lastFaceAngleZ = value;
			//			Debug.Log("FaceAngleZ: "+value);

			// 座標の回転を入れるんだったらここ

			// 顔＆体の回転 X
			float faceX = (points [27].x - points [0].x) / (points [16].x - points [0].x);
			value = (faceX - 0.5f) / 0.25f * 30f;
			value = (value - lastFaceAngleX) * 0.2f + lastFaceAngleX;
			parameter = _model.Parameters [FaceAngleX];
			parameter.Value = value;
			lastFaceAngleX = value;
			//			Debug.Log("FaceAngleX: "+value);
			parameter = _model.Parameters [BodyAngleX];
			value = (faceX - 0.5f) / 0.25f * 10f;
			value = (value - lastBodyAngleX) * 0.05f + lastBodyAngleX;
			parameter.Value = value;
			lastBodyAngleX = value;
			//			Debug.Log("BodyAngleX: "+value);

			// 顔＆体の回転 Y
			float faceY = (points [8].y - points [33].y) / (points [8].y - points [27].y);
			value = (faceY - 0.6f) / 0.05f * 30f;
			value = (value - lastFaceAngleY) * 0.2f + lastFaceAngleY;
			parameter = _model.Parameters [FaceAngleY];
			parameter.Value = value;
			lastFaceAngleY = value;
			//			Debug.Log("FaceAngleY: "+value);
			parameter = _model.Parameters [BodyAngleY];
			value = (faceY - 0.6f) / 0.05f * 10f;
			value = (value - lastBodyAngleY) * 0.05f + lastBodyAngleY;
			parameter.Value = value;
			lastBodyAngleY = value;
			//			Debug.Log("BodyAngleY: "+value);

			//目の開き具合を算出
			float eyeLOpen = Mathf.Abs (points [43].y - points [47].y) / (Mathf.Abs (points [42].x - points [45].x));
			float eyeROpen = Mathf.Abs (points [38].y - points [40].y) / (Mathf.Abs (points [39].x - points [36].x));
			eyeLOpen = (eyeLOpen - 0.2f) * 11f;
			eyeROpen = (eyeROpen - 0.2f) * 11f;
//			eyeLOpen = Mathf.Abs (eyeLOpen - 0.2f) * 11f;
//			eyeROpen = Mathf.Abs (eyeROpen - 0.2f) * 11f;
			if (!((eyeLOpen > 0.5f && eyeROpen < 0.1f) || (eyeROpen > 0.5f && eyeLOpen < 0.1f))) {
				eyeLOpen = eyeROpen = (eyeLOpen + eyeROpen) / 2f;
			}
			eyeLOpen = (eyeLOpen - lastEyeLOpen) * 0.2f + lastEyeLOpen;
			eyeROpen = (eyeROpen - lastEyeROpen) * 0.2f + lastEyeROpen;
			parameter = _model.Parameters [EyeLOpen];
			parameter.Value = eyeLOpen;
			parameter = _model.Parameters [EyeROpen];
			parameter.Value = eyeROpen;
			lastEyeLOpen = eyeLOpen;
			lastEyeROpen = eyeROpen;
			//		Debug.Log("EyeLOpen: "+eyeLOpen+" EyeROpen: "+eyeROpen);

			//口の開き具合を算出
			float mouthCloseThres = 10f;
			value = Mathf.Abs (points [62].y - points [66].y);
			if (value < mouthCloseThres)
				value = 0;
			value = (value / (Mathf.Abs (points [51].y - points [62].y) + Mathf.Abs (points [66].y - points [57].y)));
			parameter = _model.Parameters [MouthOpenY];
			parameter.Value = value;
//		Debug.Log("MouthOpenY: "+value);

			//笑っているかどうかを算出
			float laughYValue = (points [62].y - Mathf.Abs (points [48].y - points [54].y) / 2f) / (Mathf.Abs (points [51].y - points [62].y) + Mathf.Abs (points [66].y - points [57].y));
//			float laughXValue = (Mathf.Abs (points [48].x - points [54].x)) / (Mathf.Abs (points [51].y - points [62].y) + Mathf.Abs (points [66].y - points [57].y));
			float laughXValue = (Mathf.Abs (points [31].x - points [35].x)) / (Mathf.Abs (points [51].y - points [62].y) + Mathf.Abs (points [66].y - points [57].y));
			float eyeLaugh = 0;
			float cheek = -0.5f;
			float mouthForm = 1f;
			if (laughXValue > 2.0f) {	// 笑顔
				mouthForm = 1f;
				eyeLaugh = Mathf.Abs(laughXValue - 2.0f) / 0.5f;
				cheek += eyeLaugh / 2f;
			}
			if (laughYValue < 14f) {	// 真顔
				mouthForm=0.2f;
				eyeLaugh = 0;
			}
			parameter = _model.Parameters [MouthForm];
			parameter.Value = mouthForm;
			parameter = _model.Parameters [EyeLSmile];
			parameter.Value = eyeLaugh;
			parameter = _model.Parameters [EyeRSmile];
			parameter.Value = eyeLaugh;
			parameter = _model.Parameters [Cheek];
			parameter.Value = cheek;
//			Debug.Log("laughYValue: "+laughYValue+" laughXValue: "+laughXValue);

			// 目線の設定
//			Debug.Log("eyeSightRecv: "+eyeSightRecv);
			if (eyeSightRecv != 0) {
				value = (eyeSightRecv - 0.5f) / 0.25f;
				value = (value - lastEyeSight) * 0.4f + lastEyeSight;
				parameter = _model.Parameters [EyeBallX];
				parameter.Value = value;
				lastEyeSight = value;
//				Debug.Log ("EyeBallX: " + value);
			}

		}
		// sample
//		_t += (Time.deltaTime * 1f);
//		var value = Mathf.Sin (_t * 2f * Mathf.PI / 1f) * 0.6f + 0.6f;
//		var parameter = _model.Parameters [4];
//		parameter.Value = value;
	}

	void OnApplicationQuit()
	{
		thread.Abort();
	}

	private static void ThreadMethod()
	{
		int counter = 0;
		while(true)
		{
			IPEndPoint remoteEP = null;
			byte[] buff = udp.Receive (ref remoteEP);
			string text = "";
			if (counter % 2 == 0) {		// 二回に一回バッファに格納する（約15fps）
				for (int i = 0; i < maxPartsNum+1; i++) {
					unsafe {
						fixed(byte* buffP = &buff[8*i]) {
							points [i].x = (float)*(int*)&buffP [0];
							points [i].y = (float)*(int*)&buffP [4];
							text += i + "X: " + points [i].x + " " + i + "Y: " + points [i].y + " ";
//						int pointX = *(int*)&buffP[0];
//						int pointY = *(int*)&buffP[4];
//						text += i + "X: " + pointX + " " + i + "Y: " + pointY + " ";

							// 目線データ
							eyeSightRecv = *(float*)buffP;
						}
					}
				}
			}
			counter++;
//			Debug.Log (text);
		}
	} 
}
