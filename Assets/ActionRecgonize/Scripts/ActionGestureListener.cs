using UnityEngine;
//using Windows.Kinect;
using System.Collections;
using System;

public class ActionGestureListener : MonoBehaviour, KinectGestures.GestureListenerInterface
{
	private bool progressDisplayed;
	private KinectManager manager;
	private ChangeState ct;
	private bool if1 = true;
	private bool if2 = true;
	private bool if3 = true;
	private bool if4 = true;

	public void UserDetected(long userId, int userIndex)
	{
		ct = GameObject.FindWithTag ("Person").GetComponent<ChangeState>();
		manager = KinectManager.Instance;
		manager.DetectGesture (userId, KinectGestures.Gestures.TheFirstMove);
		manager.DetectGesture (userId, KinectGestures.Gestures.TheSecondMove);
		manager.DetectGesture (userId, KinectGestures.Gestures.TheThirdMove);
		manager.DetectGesture (userId, KinectGestures.Gestures.TheForthMove);
	}
	
	public void UserLost(long userId, int userIndex)
	{

	}
	
	public void GestureInProgress(long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              float progress, KinectInterop.JointType joint, Vector3 screenPos)
	{
	     return;
	}
	
	public bool GestureCompleted (long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              KinectInterop.JointType joint, Vector3 screenPos)
	{
		if (gesture == KinectGestures.Gestures.TheFirstMove && if1) {
			//Debug.Log ("the first complete");
			if1 = false;
			ct.first = true;
		}
		else if (gesture == KinectGestures.Gestures.TheSecondMove && if2 && !if1) {
			if2 = false;
			ct.second = true;

		}
		else if (gesture == KinectGestures.Gestures.TheThirdMove && if3 && !if2) {
			if3 = false;
			ct.third = true;


		}
		else if (gesture == KinectGestures.Gestures.TheForthMove && if4 && !if3) {
			if4 = false;
			ct.forth = true;

		}
		progressDisplayed = false;
		return true;
	}
	
	public bool GestureCancelled (long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              KinectInterop.JointType joint)
	{
		if(progressDisplayed)
		{
			// clear the progress info
			progressDisplayed = false;
		}
		
		return true;
	}
	
}
