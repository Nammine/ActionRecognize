using UnityEngine;
//using Windows.Kinect;

using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.IO;

public class InteractionManager : MonoBehaviour 
{
    public enum HandEventType : int
    {
        None = 0,
        Grip = 1,
        Release = 2
    }

	// Bool to specify whether the GUI hand cursor to be used as on-screen cursor
	public bool useHandCursor = true;
	
	// hand cursor textures
	public Texture gripHandTexture;
	public Texture releaseHandTexture;
	public Texture normalHandTexture;

	// Smooth factor for cursor movement
	public float smoothFactor = 3f;
	
	// Bool to specify whether hand interaction and grips/releases/clicks control the mouse cursor and functionality or not
	public bool controlMouseCursor = false;

	// Bool to specify whether hand grip/release control mouse dragging or not
	public bool controlMouseDrag = false;

	// Bool to specify whether to convert Unity screen coordinates to full screen mouse coordinates
	public bool convertMouseToFullScreen = false;
	
	// GUI-Text object to be used for displaying debug information
	public GameObject debugText;
	
	private Int64 primaryUserID = 0;
	
	private bool isLeftHandPrimary = false;
	private bool isRightHandPrimary = false;
	
	private bool isLeftHandPress = false;
	private bool isRightHandPress = false;
	
	private Vector3 cursorScreenPos = Vector3.zero;
	private bool dragInProgress = false;
	
	private KinectInterop.HandState leftHandState = KinectInterop.HandState.Unknown;
	private KinectInterop.HandState rightHandState = KinectInterop.HandState.Unknown;
	
	private HandEventType leftHandEvent = HandEventType.None;
	private HandEventType lastLeftHandEvent = HandEventType.Release;

	private Vector3 leftHandPos = Vector3.zero;
	private Vector3 leftHandScreenPos = Vector3.zero;
	private Vector3 leftIboxLeftBotBack = Vector3.zero;
	private Vector3 leftIboxRightTopFront = Vector3.zero;
	private bool isleftIboxValid = false;
	private bool isLeftHandInteracting = false;
	private float leftHandInteractingSince = 0f;
	
	private Vector3 lastLeftHandPos = Vector3.zero;
	private float lastLeftHandTime = 0f;
	private bool isLeftHandClick = false;
	private float leftHandClickProgress = 0f;
	
	private HandEventType rightHandEvent = HandEventType.None;
	private HandEventType lastRightHandEvent = HandEventType.Release;

	private Vector3 rightHandPos = Vector3.zero;
	private Vector3 rightHandScreenPos = Vector3.zero;
	private Vector3 rightIboxLeftBotBack = Vector3.zero;
	private Vector3 rightIboxRightTopFront = Vector3.zero;
	private bool isRightIboxValid = false;
	private bool isRightHandInteracting = false;
	private float rightHandInteractingSince = 0f;
	
	private Vector3 lastRightHandPos = Vector3.zero;
	private float lastRightHandTime = 0f;
	private bool isRightHandClick = false;
	private float rightHandClickProgress = 0f;
	
	// Bool to keep track whether Kinect and Interaction library have been initialized
	private bool interactionInited = false;
	
	// The single instance of FacetrackingManager
	private static InteractionManager instance;

	
	// returns the single InteractionManager instance
    public static InteractionManager Instance
    {
        get
        {
            return instance;
        }
    }
	
	// returns true if the InteractionLibrary is initialized, otherwise returns false
	public bool IsInteractionInited()
	{
		return interactionInited;
	}
	
	// returns the user ID (skeleton tracking ID), or 0 if no user is currently tracked
	public Int64 GetUserID()
	{
		return primaryUserID;
	}
	
	// returns the current left hand event (none, grip or release)
	public HandEventType GetLeftHandEvent()
	{
		return leftHandEvent;
	}
	
	// returns the last detected left hand event (none, grip or release)
	public HandEventType GetLastLeftHandEvent()
	{
		return lastLeftHandEvent;
	}
	
	// returns the current screen position of the left hand
	public Vector3 GetLeftHandScreenPos()
	{
		return leftHandScreenPos;
	}
	
	// returns true if the left hand is primary for the user
	public bool IsLeftHandPrimary()
	{
		return isLeftHandPrimary;
	}
	
	// returns true if the left hand is in press-position
	public bool IsLeftHandPress()
	{
		return isLeftHandPress;
	}
	
	// returns true if left hand click is detected, false otherwise
	public bool IsLeftHandClickDetected()
	{
		if(isLeftHandClick)
		{
			isLeftHandClick = false;
			leftHandClickProgress = 0f;
			lastLeftHandPos = Vector3.zero;
			lastLeftHandTime = Time.realtimeSinceStartup;
			
			return true;
		}
		
		return false;
	}

	// returns the left hand click progress [0, 1]
	public float GetLeftHandClickProgress()
	{
		return leftHandClickProgress;
	}
	
	// returns the current valid right hand event (none, grip or release)
	public HandEventType GetRightHandEvent()
	{
		return rightHandEvent;
	}
	
	// returns the last detected right hand event (none, grip or release)
	public HandEventType GetLastRightHandEvent()
	{
		return lastRightHandEvent;
	}
	
	// returns the current screen position of the right hand
	public Vector3 GetRightHandScreenPos()
	{
		return rightHandScreenPos;
	}
	
	// returns true if the right hand is primary for the user
	public bool IsRightHandPrimary()
	{
		return isRightHandPrimary;
	}
	
	// returns true if the right hand is in press-position
	public bool IsRightHandPress()
	{
		return isRightHandPress;
	}
	
	// returns true if right hand click is detected, false otherwise
	public bool IsRightHandClickDetected()
	{
		if(isRightHandClick)
		{
			isRightHandClick = false;
			rightHandClickProgress = 0f;
			lastRightHandPos = Vector3.zero;
			lastRightHandTime = Time.realtimeSinceStartup;
			
			return true;
		}
		
		return false;
	}

	// returns the right hand click progress [0, 1]
	public float GetRightHandClickProgress()
	{
		return rightHandClickProgress;
	}
	
	// returns the current cursor position in normalized coordinates in (x,y,z)
	public Vector3 GetCursorPosition()
	{
		return cursorScreenPos;
	}
	
	//----------------------------------- end of public functions --------------------------------------//
	
	void Start() 
	{
		instance = this;
		interactionInited = true;
	}
	
	void OnApplicationQuit()
	{
		// uninitialize Kinect interaction
		if(interactionInited)
		{
			interactionInited = false;
			instance = null;
		}
	}
	
	void Update () 
	{
		KinectManager kinectManager = KinectManager.Instance;
		
		// update Kinect interaction
		if(kinectManager && kinectManager.IsInitialized())
		{
			primaryUserID = kinectManager.GetPrimaryUserID();
			
			if(primaryUserID != 0)
			{
				HandEventType handEvent = HandEventType.None;
				
				// get the left hand state
				leftHandState = kinectManager.GetLeftHandState(primaryUserID);
				
				// check if the left hand is interacting
				isleftIboxValid = kinectManager.GetLeftHandInteractionBox(primaryUserID, ref leftIboxLeftBotBack, ref leftIboxRightTopFront, isleftIboxValid);
				//bool bLeftHandPrimaryNow = false;
				
				if(isleftIboxValid && //bLeftHandPrimaryNow &&
				   kinectManager.GetJointTrackingState(primaryUserID, (int)KinectInterop.JointType.HandLeft) != KinectInterop.TrackingState.NotTracked)
				{
					leftHandPos = kinectManager.GetJointPosition(primaryUserID, (int)KinectInterop.JointType.HandLeft);

					leftHandScreenPos.x = Mathf.Clamp01((leftHandPos.x - leftIboxLeftBotBack.x) / (leftIboxRightTopFront.x - leftIboxLeftBotBack.x));
					leftHandScreenPos.y = Mathf.Clamp01((leftHandPos.y - leftIboxLeftBotBack.y) / (leftIboxRightTopFront.y - leftIboxLeftBotBack.y));
					leftHandScreenPos.z = Mathf.Clamp01((leftIboxLeftBotBack.z - leftHandPos.z) / (leftIboxLeftBotBack.z - leftIboxRightTopFront.z));
					
					bool wasLeftHandInteracting = isLeftHandInteracting;
					isLeftHandInteracting = (leftHandPos.x >= (leftIboxLeftBotBack.x - 1.0f)) && (leftHandPos.x <= (leftIboxRightTopFront.x + 0.5f)) &&
						(leftHandPos.y >= (leftIboxLeftBotBack.y - 0.1f)) && (leftHandPos.y <= (leftIboxRightTopFront.y + 0.7f)) &&
						(leftIboxLeftBotBack.z >= leftHandPos.z) && (leftIboxRightTopFront.z * 0.8f <= leftHandPos.z);
					//bLeftHandPrimaryNow = isLeftHandInteracting;
					
					if(!wasLeftHandInteracting && isLeftHandInteracting)
					{
						leftHandInteractingSince = Time.realtimeSinceStartup;
					}

					// check for left press
					isLeftHandPress = (leftIboxRightTopFront.z >= leftHandPos.z);
					
					// check for left hand click
					float fClickDist = (leftHandPos - lastLeftHandPos).magnitude;
					if(isLeftHandInteracting && (fClickDist < KinectInterop.Constants.ClickMaxDistance))
					{
						if((Time.realtimeSinceStartup - lastLeftHandTime) >= KinectInterop.Constants.ClickStayDuration)
						{
							if(!isLeftHandClick)
							{
								isLeftHandClick = true;
								leftHandClickProgress = 1f;
								
								if(controlMouseCursor)
								{
									MouseControl.MouseClick();

									isLeftHandClick = false;
									leftHandClickProgress = 0f;
									lastLeftHandPos = Vector3.zero;
									lastLeftHandTime = Time.realtimeSinceStartup;
								}
							}
						}
						else
						{
							leftHandClickProgress = (Time.realtimeSinceStartup - lastLeftHandTime) / KinectInterop.Constants.ClickStayDuration;
						}
					}
					else
					{
						isLeftHandClick = false;
						leftHandClickProgress = 0f;
						lastLeftHandPos = leftHandPos;
						lastLeftHandTime = Time.realtimeSinceStartup;
					}
				}
				else
				{
					isLeftHandInteracting = false;
					isLeftHandPress = false;
				}
				
				// get the right hand state
				rightHandState = kinectManager.GetRightHandState(primaryUserID);

				// check if the right hand is interacting
				isRightIboxValid = kinectManager.GetRightHandInteractionBox(primaryUserID, ref rightIboxLeftBotBack, ref rightIboxRightTopFront, isRightIboxValid);
				//bool bRightHandPrimaryNow = false;
				
				if(isRightIboxValid && //bRightHandPrimaryNow &&
				   kinectManager.GetJointTrackingState(primaryUserID, (int)KinectInterop.JointType.HandRight) != KinectInterop.TrackingState.NotTracked)
				{
					rightHandPos = kinectManager.GetJointPosition(primaryUserID, (int)KinectInterop.JointType.HandRight);

					rightHandScreenPos.x = Mathf.Clamp01((rightHandPos.x - rightIboxLeftBotBack.x) / (rightIboxRightTopFront.x - rightIboxLeftBotBack.x));
					rightHandScreenPos.y = Mathf.Clamp01((rightHandPos.y - rightIboxLeftBotBack.y) / (rightIboxRightTopFront.y - rightIboxLeftBotBack.y));
					rightHandScreenPos.z = Mathf.Clamp01((rightIboxLeftBotBack.z - rightHandPos.z) / (rightIboxLeftBotBack.z - rightIboxRightTopFront.z));
					
					bool wasRightHandInteracting = isRightHandInteracting;
					isRightHandInteracting = (rightHandPos.x >= (rightIboxLeftBotBack.x - 0.5f)) && (rightHandPos.x <= (rightIboxRightTopFront.x + 1.0f)) &&
						(rightHandPos.y >= (rightIboxLeftBotBack.y - 0.1f)) && (rightHandPos.y <= (rightIboxRightTopFront.y + 0.7f)) &&
						(rightIboxLeftBotBack.z >= rightHandPos.z) && (rightIboxRightTopFront.z * 0.8f <= rightHandPos.z);
					//bRightHandPrimaryNow = isRightHandInteracting;
					
					if(!wasRightHandInteracting && isRightHandInteracting)
					{
						rightHandInteractingSince = Time.realtimeSinceStartup;
					}
					
					if(isLeftHandInteracting && isRightHandInteracting)
					{
						if(rightHandInteractingSince <= leftHandInteractingSince)
							isLeftHandInteracting = false;
						else
							isRightHandInteracting = false;
					}
					
					// check for right press
					isRightHandPress = (rightIboxRightTopFront.z >= rightHandPos.z);
					
					// check for right hand click
					float fClickDist = (rightHandPos - lastRightHandPos).magnitude;
					if(isRightHandInteracting && (fClickDist < KinectInterop.Constants.ClickMaxDistance))
					{
						if((Time.realtimeSinceStartup - lastRightHandTime) >= KinectInterop.Constants.ClickStayDuration)
						{
							if(!isRightHandClick)
							{
								isRightHandClick = true;
								rightHandClickProgress = 1f;
								
								if(controlMouseCursor)
								{
									MouseControl.MouseClick();

									isRightHandClick = false;
									rightHandClickProgress = 0f;
									lastRightHandPos = Vector3.zero;
									lastRightHandTime = Time.realtimeSinceStartup;
								}
							}
						}
						else
						{
							rightHandClickProgress = (Time.realtimeSinceStartup - lastRightHandTime) / KinectInterop.Constants.ClickStayDuration;
						}
					}
					else
					{
						isRightHandClick = false;
						rightHandClickProgress = 0f;
						lastRightHandPos = rightHandPos;
						lastRightHandTime = Time.realtimeSinceStartup;
					}
				}
				else
				{
					isRightHandInteracting = false;
					isRightHandPress = false;
				}
				
				// process left hand
				handEvent = HandStateToEvent(leftHandState, lastLeftHandEvent);

				if((isLeftHandInteracting != isLeftHandPrimary) || (isRightHandInteracting != isRightHandPrimary))
				{
					if(controlMouseCursor && dragInProgress)
					{
						MouseControl.MouseRelease();
						dragInProgress = false;
					}
					
					lastLeftHandEvent = HandEventType.Release;
					lastRightHandEvent = HandEventType.Release;
				}
				
				if(controlMouseCursor && (handEvent != lastLeftHandEvent))
				{
					if(controlMouseDrag && !dragInProgress && (handEvent == HandEventType.Grip))
					{
						dragInProgress = true;
						MouseControl.MouseDrag();
					}
					else if(dragInProgress && (handEvent == HandEventType.Release))
					{
						MouseControl.MouseRelease();
						dragInProgress = false;
					}
				}
				
				leftHandEvent = handEvent;
				if(handEvent != HandEventType.None)
				{
					lastLeftHandEvent = handEvent;
				}
				
				// if the hand is primary, set the cursor position
				if(isLeftHandInteracting)
				{
					isLeftHandPrimary = true;

					if((leftHandClickProgress < 0.8f) && !isLeftHandPress)
					{
						cursorScreenPos = Vector3.Lerp(cursorScreenPos, leftHandScreenPos, smoothFactor * Time.deltaTime);
					}
					else
					{
						leftHandScreenPos = cursorScreenPos;
					}

					if(controlMouseCursor && !useHandCursor)
					{
						MouseControl.MouseMove(cursorScreenPos, convertMouseToFullScreen);
					}
				}
				else
				{
					isLeftHandPrimary = false;
				}
				
				// process right hand
				handEvent = HandStateToEvent(rightHandState, lastRightHandEvent);

				if(controlMouseCursor && (handEvent != lastRightHandEvent))
				{
					if(controlMouseDrag && !dragInProgress && (handEvent == HandEventType.Grip))
					{
						dragInProgress = true;
						MouseControl.MouseDrag();
					}
					else if(dragInProgress && (handEvent == HandEventType.Release))
					{
						MouseControl.MouseRelease();
						dragInProgress = false;
					}
				}
				
				rightHandEvent = handEvent;
				if(handEvent != HandEventType.None)
				{
					lastRightHandEvent = handEvent;
				}	
				
				// if the hand is primary, set the cursor position
				if(isRightHandInteracting)
				{
					isRightHandPrimary = true;

					if((rightHandClickProgress < 0.8f) && !isRightHandPress)
					{
						cursorScreenPos = Vector3.Lerp(cursorScreenPos, rightHandScreenPos, smoothFactor * Time.deltaTime);
					}
					else
					{
						rightHandScreenPos = cursorScreenPos;
					}

					if(controlMouseCursor && !useHandCursor)
					{
						MouseControl.MouseMove(cursorScreenPos, convertMouseToFullScreen);
					}
				}
				else
				{
					isRightHandPrimary = false;
				}

			}
			else
			{
				leftHandState = KinectInterop.HandState.NotTracked;
				rightHandState = KinectInterop.HandState.NotTracked;
				
				isLeftHandPrimary = false;
				isRightHandPrimary = false;
				
				isLeftHandPress = false;
				isRightHandPress = false;
				
				leftHandEvent = HandEventType.None;
				rightHandEvent = HandEventType.None;
				
				lastLeftHandEvent = HandEventType.Release;
				lastRightHandEvent = HandEventType.Release;
				
				if(controlMouseCursor && dragInProgress)
				{
					MouseControl.MouseRelease();
					dragInProgress = false;
				}
			}
		}
		
	}
	
	
	// converts hand state to hand event type
	private HandEventType HandStateToEvent(KinectInterop.HandState handState, HandEventType lastEventType)
	{
		switch(handState)
		{
			case KinectInterop.HandState.Open:
				return HandEventType.Release;

			case KinectInterop.HandState.Closed:
			case KinectInterop.HandState.Lasso:
				return HandEventType.Grip;
			
			case KinectInterop.HandState.Unknown:
				return lastEventType;
		}

		return HandEventType.None;
	}
	
	
	void OnGUI()
	{
		if(!interactionInited)
			return;
		
		// display debug information
		if(debugText)
		{
			string sGuiText = string.Empty;

			if(isRightHandPrimary)
			{
				sGuiText = "Cursor: " + cursorScreenPos.ToString();
				
				if(lastRightHandEvent == HandEventType.Grip)
				{
					sGuiText += "  RightGrip";
				}
				else if(lastRightHandEvent == HandEventType.Release)
				{
					sGuiText += "  RightRelease";
				}
				
				if(isRightHandClick)
				{
					sGuiText += "  RightClick";
				}
//				else if(rightHandClickProgress > 0.5f)
//				{
//					sGuiText += String.Format("  {0:F0}%", rightHandClickProgress * 100);
//				}

				if(isRightHandPress)
				{
					sGuiText += "  RightPress";
				}
			}
			
			if(isLeftHandPrimary)
			{
				sGuiText = "Cursor: " + cursorScreenPos.ToString();
				
				if(lastLeftHandEvent == HandEventType.Grip)
				{
					sGuiText += "  LeftGrip";
				}
				else if(lastLeftHandEvent == HandEventType.Release)
				{
					sGuiText += "  LeftRelease";
				}
				
				if(isLeftHandClick)
				{
					sGuiText += "  LeftClick";
				}
//				else if(leftHandClickProgress > 0.5f)
//				{
//					sGuiText += String.Format("  {0:F0}%", leftHandClickProgress * 100);
//				}
			
				if(isLeftHandPress)
				{
					sGuiText += "  LeftPress";
				}
			}

			debugText.GetComponent<GUIText>().text = sGuiText;
		}
		
		// display the cursor status and position
		if(useHandCursor)
		{
			Texture texture = null;
			
			if(isLeftHandPrimary)
			{
				if(lastLeftHandEvent == HandEventType.Grip)
					texture = gripHandTexture;
				else if(lastLeftHandEvent == HandEventType.Release)
					texture = releaseHandTexture;
			}
			else if(isRightHandPrimary)
			{
				if(lastRightHandEvent == HandEventType.Grip)
					texture = gripHandTexture;
				else if(lastRightHandEvent == HandEventType.Release)
					texture = releaseHandTexture;
			}
			
			if(texture == null)
			{
				texture = normalHandTexture;
			}
			
			if(useHandCursor)
			{
//				if(handCursor.guiTexture && texture)
//				{
//					handCursor.guiTexture.texture = texture;
//				}

				if(isLeftHandPrimary || isRightHandPrimary)
				{
					//handCursor.transform.position = cursorScreenPos; // Vector3.Lerp(handCursor.transform.position, cursorScreenPos, 3 * Time.deltaTime);
					Rect rectTexture = new Rect(cursorScreenPos.x * Screen.width - texture.width / 2, (1f - cursorScreenPos.y) * Screen.height - texture.height / 2, 
					                            texture.width, texture.height);
					GUI.DrawTexture(rectTexture, texture);

					if(controlMouseCursor)
					{
						MouseControl.MouseMove(cursorScreenPos, convertMouseToFullScreen);
					}
				}
			}
		}
	}

}
