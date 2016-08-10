using UnityEngine;
using System.Collections;

public class LoadMainLevel : MonoBehaviour 
{
	private bool levelLoaded = false;
	
	
	void Update() 
	{
		KinectManager manager = KinectManager.Instance;
		
		if(!levelLoaded && manager && KinectManager.IsKinectInitialized())
		{
			levelLoaded = true;
			Application.LoadLevel(1);
		}
	}
	
}
