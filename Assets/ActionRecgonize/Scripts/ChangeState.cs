using UnityEngine;
using System.Collections;
using System.IO.Ports;//串口
using System.Threading;
using System;

public class ChangeState : MonoBehaviour {
	public SerialPort _Port;
	public bool first;
	public bool second;
	public bool third;
	public bool forth;
	public GameObject particleSys;
	public Texture img;
	public Texture img1;
	public Texture img2;
	public Texture img3;
	public Texture img4;
	private bool ifSet2 = true;
	public GameObject particleSys2;
	private bool if1 = true;
	private bool if2 = true;
	private bool if3 = true;
	private float nowTime;
	public Transform passClone;
	public Transform completeClone;
	// Use this for initialization
	void Awake () {
		//InitPort();
		//setCom1();
		first = false;
		second = false;
		third = false;
		forth = false;
	}
	// Update is called once per frame
	void Update () {
		if (first && !second) {
			img = img2;
			if (if1) {
				particleSys2.SetActive (true);
				nowTime = Time.time;
				if1 = false;
				createPass();
			} else{
				if(Time.time - nowTime > 1)
				particleSys2.SetActive (false);
			}
			particleSys.SetActive (false);
		} else if (second && !third) {
			//particleSys2.SetActive (false);
			img = img3;
			if (if2) {
				particleSys2.SetActive (true);
				nowTime = Time.time;
				if2 = false;
				createPass();
			} else{
				if(Time.time - nowTime > 1)
					particleSys2.SetActive (false);
			}

			particleSys.SetActive (false);
		} else if (third && !forth) {
			//particleSys2.SetActive (false);
			img = img4;
			if (if3) {
				particleSys2.SetActive (true);
				nowTime = Time.time;
				if3 = false;
				createPass();
			} else{
				if(Time.time - nowTime > 1)
					particleSys2.SetActive (false);
			}
			particleSys.SetActive (false);
		} else if (forth) {
			particleSys.SetActive (true);
			particleSys2.SetActive(false);
			if (ifSet2) {
				//setCom2 ();
				ifSet2 = false;
				createComplete();
			}
		} else {
			particleSys.SetActive(false);
			particleSys2.SetActive(false);
		} 
	}

	void OnGUI()//动作指示文本框
	{
		GUI.skin.label.normal.textColor = Color.red;//(0, 255.0 / 255, 0, 1.0);
		// 后面的color为 RGBA的格式，支持alpha，取值范围为浮点数： 0 - 1.0.
		GUI.skin.label.fontSize = 50;
		//GUI.skin.label.alignment = TextAnchor.UpperCenter;
		GUI.Label(new Rect(0,0,200,200),img); 
	}  

	public void InitPort()//串口初始化
	{
		_Port = new SerialPort("COM5", 9600, Parity.None, 8, StopBits.One);
		_Port.WriteTimeout = 300;
		
		if(!_Port.IsOpen)
			_Port.Open();
		/*   //_Port .DataReceived +=
        }
        catch
        {// (Exception ex){ 
            // MessageBox.
        }
		*/
	}
	public void setCom1()//写串口函数
	{
		if (_Port.IsOpen) {
			Byte[] buf = new Byte[4];
			buf[0] = 0xAF;
			buf[1] = 0xFD;
			buf[2] = 0X01;
			buf[3] = 0xDF;
			_Port.Write (buf,0,4);
			Debug.Log (buf);
		} 
		else {
			Debug .Log ("no");
		}
	}
	
	public void setCom2()//写串口函数
	{
		if (_Port.IsOpen) {
			Byte[] buf = new Byte[4];
			buf[0] = 0xAF;
			buf[1] = 0xFD;
			buf[2] = 0X02;
			buf[3] = 0xDF;
			_Port.Write (buf,0,4);
			Debug.Log (buf);
		} 
		else {
			Debug .Log ("no");
		}
	}
	private void createPass(){
		if(GetComponent<AudioSource>().isPlaying) GetComponent<AudioSource>().Stop();
		Transform playPass = Instantiate (passClone, passClone.position, passClone.rotation) as Transform;
		playPass.SetParent (transform);
		playPass.position = new Vector3 (-3, 0, 3);
		playPass.localScale = new Vector3 (1, 1, 1);
	}
	private void createComplete(){
		if(GetComponent<AudioSource>().isPlaying) GetComponent<AudioSource>().Stop();
		Transform playComplete = Instantiate (completeClone, completeClone.position, completeClone.rotation) as Transform;
		playComplete.SetParent (transform);
		playComplete.position = new Vector3 (-3, 0, 3);
		playComplete.localScale = new Vector3 (1, 1, 1);
	}
}
