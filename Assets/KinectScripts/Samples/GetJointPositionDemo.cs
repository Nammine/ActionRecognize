using UnityEngine;
using System.Collections;
//using Windows.Kinect;

public class GetJointPositionDemo : MonoBehaviour 
{
	// the joint we want to track
	public KinectInterop.JointType joint = KinectInterop.JointType.HandRight;

	// joint position at the moment, in Kinect coordinates
	public Vector3 outputPosition;


	void Update () 
	{
		KinectManager manager = KinectManager.Instance;

		if(manager && manager.IsInitialized())
		{
			if(manager.IsUserDetected())
			{
				long userId = manager.GetPrimaryUserID();

				if(manager.IsJointTracked(userId, (int)joint))
				{
					outputPosition = manager.GetJointPosition(userId, (int)joint);
				}
			}
		}
	}
}
