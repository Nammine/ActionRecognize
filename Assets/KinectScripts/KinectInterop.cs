using UnityEngine;
//using Windows.Kinect;

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;
using System.IO;


public class KinectInterop
{
	// constants一些常量（不改变的）
	public static class Constants
	{
//		public const int BodyCount = 6;
		public const int JointCount = 25;//可检测关节25出

		public const float MinTimeBetweenSameGestures = 0.0f;//~
		public const float PoseCompleteDuration = 1.0f;//~
		public const float ClickMaxDistance = 0.05f;//~
		public const float ClickStayDuration = 1.5f;//~
	}
	
	/// Data structures for interfacing C# with the native wrapper

    [Flags]//复合枚举
    public enum FrameSource : uint
    {
		TypeNone = 0x0,
        TypeColor = 0x1,//彩色
        TypeInfrared = 0x2,//红外
        TypeDepth = 0x8,//深度
        TypeBodyIndex = 0x10,//body index
        TypeBody = 0x20,//body
        TypeAudio = 0x40//声音
    }
	
    public enum JointType : int
    {
		SpineBase = 0,//脊髓底部
		SpineMid = 1,//脊髓中部
        Neck = 2,//脖子
        Head = 3,//头
        ShoulderLeft = 4,
        ElbowLeft = 5,//左肘
        WristLeft = 6,//左手腕
        HandLeft = 7,//左手
        ShoulderRight = 8,
        ElbowRight = 9,
        WristRight = 10,
        HandRight = 11,
        HipLeft = 12,//左胯
        KneeLeft = 13,
        AnkleLeft = 14,
        FootLeft = 15,
        HipRight = 16,
        KneeRight = 17,
        AnkleRight = 18,
        FootRight = 19,
        SpineShoulder = 20,//不知道 
        HandTipLeft = 21,
        ThumbLeft = 22,
        HandTipRight = 23,
        ThumbRight = 24
		//Count = 25
    }

    public static readonly Vector3[] JointBaseDir =
    {
        Vector3.zero,
        Vector3.up,
        Vector3.up,
        Vector3.up,
        Vector3.left,
        Vector3.left,
        Vector3.left,
        Vector3.left,
        Vector3.right,
        Vector3.right,
        Vector3.right,
        Vector3.right,
        Vector3.down,
        Vector3.down,
        Vector3.down,
        Vector3.forward,
        Vector3.down,
        Vector3.down,
        Vector3.down,
        Vector3.forward,
        Vector3.up,
        Vector3.left,
        Vector3.forward,
        Vector3.right,
        Vector3.forward
    };

    public enum TrackingState
    {
        NotTracked = 0,
        Inferred = 1,
        Tracked = 2
    }

	public enum HandState
    {
        Unknown = 0,
        NotTracked = 1,
        Open = 2,
        Closed = 3,
        Lasso = 4
    }
	
	public enum TrackingConfidence
    {
        Low = 0,
        High = 1
    }

//    [Flags]
//    public enum ClippedEdges
//    {
//        None = 0,
//        Right = 1,
//        Left = 2,
//        Top = 4,
//        Bottom = 8
//    }


	public class SensorData
	{
		public DepthSensorInterface sensorInterface;

		public int bodyCount;
		public int jointCount;

		public int colorImageWidth;
		public int colorImageHeight;

		public byte[] colorImage;
		public long lastColorFrameTime = 0;

		public int depthImageWidth;
		public int depthImageHeight;

		public ushort[] depthImage;
		public long lastDepthFrameTime = 0;

		public ushort[] infraredImage;
		public long lastInfraredFrameTime = 0;

		public byte[] bodyIndexImage;
		public long lastBodyIndexFrameTime = 0;
	}

	public struct SmoothParameters
	{
		public float smoothing;
		public float correction;
		public float prediction;
		public float jitterRadius;//抖动频率
		public float maxDeviationRadius;
	}
	
	public struct JointData
    {
		// parameters filled in by the sensor interface
		public JointType jointType;
    	public TrackingState trackingState;
    	public Vector3 kinectPos;
    	public Vector3 position;
		public Quaternion orientation;  // deprecated

		// KM calculated parameters

		public Vector3 direction;
		public Quaternion normalRotation;
		public Quaternion mirroredRotation;
    }
	
	public struct BodyData
    {
		// parameters filled in by the sensor interface
        public Int64 liTrackingID;
        public Vector3 position;
		public Quaternion orientation;  // deprecated

		public JointData[] joint;

		// KM calculated parameters
		public Quaternion normalRotation;
		public Quaternion mirroredRotation;
		
		public Vector3 hipsDirection;
		public Vector3 shouldersDirection;
		public float bodyTurnAngle;

		public Vector3 leftThumbDirection;
		public Vector3 leftHandDirection;
		public Vector3 leftThumbForward;
		public float leftThumbAngle;

		public Vector3 rightThumbDirection;
		public Vector3 rightHandDirection;
		public Vector3 rightThumbForward;
		public float rightThumbAngle;

		//public Vector3 leftLegDirection;
		//public Vector3 leftFootDirection;
		//public Vector3 rightLegDirection;
		//public Vector3 rightFootDirection;

		public HandState leftHandState;
		public TrackingConfidence leftHandConfidence;
		public HandState rightHandState;
		public TrackingConfidence rightHandConfidence;
		
        public uint dwClippedEdges;
        public short bIsTracked;
		public short bIsRestricted;
    }
	
    public struct BodyFrameData
    {
        public Int64 liRelativeTime;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.Struct)]
        public BodyData[] bodyData;
        public UnityEngine.Vector4 floorClipPlane;
		
		public BodyFrameData(int bodyCount, int jointCount)
		{
			liRelativeTime = 0;
			floorClipPlane = UnityEngine.Vector4.zero;

			bodyData = new BodyData[bodyCount];
			
			for(int i = 0; i < bodyCount; i++)
			{
				bodyData[i].joint = new JointData[jointCount];
			}
		}
    }
	

	// initializes the available sensor interfaces
	public static List<DepthSensorInterface> InitSensorInterfaces(ref bool bNeedRestart)
	{
		List<DepthSensorInterface> listInterfaces = new List<DepthSensorInterface>();

		var typeInterface = typeof(DepthSensorInterface);
		Type[] typesAvailable = typeInterface.Assembly.GetTypes();

		foreach(Type type in typesAvailable)
		{
			if(typeInterface.IsAssignableFrom(type) && type != typeInterface)
			{
				DepthSensorInterface sensorInt = null;

				try 
				{
					sensorInt = (DepthSensorInterface)Activator.CreateInstance(type);

					bool bIntNeedRestart = false;
					if(sensorInt.InitSensorInterface(ref bIntNeedRestart))
					{
						bNeedRestart |= bIntNeedRestart;
					}
					else
					{
						sensorInt.FreeSensorInterface();
						sensorInt = null;
						continue;
					}

					if(sensorInt.GetSensorsCount() <= 0)
					{
						sensorInt.FreeSensorInterface();
						sensorInt = null;
					}
				} 
				catch (Exception) 
				{
					if(sensorInt != null)
					{
						try 
						{
							sensorInt.FreeSensorInterface();
						}
						catch (Exception) 
						{
							// do nothing
						}
						finally
						{
							sensorInt = null;
						}
					}
				}

				if(sensorInt != null)
				{
					listInterfaces.Add(sensorInt);
				}
			}
		}

		return listInterfaces;
	}

	// opens the default sensor and needed readers
	public static SensorData OpenDefaultSensor(List<DepthSensorInterface> listInterfaces, 
	                                           FrameSource dwFlags, float sensorAngle, bool bUseMultiSource)
	{
		SensorData sensorData = null;
		if(listInterfaces == null)
			return sensorData;

		foreach(DepthSensorInterface sensorInt in listInterfaces)
		{
			try 
			{
				if(sensorData == null)
				{
					sensorData = sensorInt.OpenDefaultSensor(dwFlags, sensorAngle, bUseMultiSource);

					if(sensorData != null)
					{
						sensorData.sensorInterface = sensorInt;
						Debug.Log("Interface used: " + sensorInt.GetType().Name);
					}
				}
				else
				{
					sensorInt.FreeSensorInterface();
				}
			} 
			catch (Exception ex) 
			{
				Debug.LogError("Initialization of sensor failed.");
				Debug.LogError(ex.ToString());

				try 
				{
					sensorInt.FreeSensorInterface();
				} 
				catch (Exception) 
				{
					// do nothing
				}
			}
		}

		return sensorData;
	}

	// closes opened readers and closes the sensor
	public static void CloseSensor(SensorData sensorData)
	{
		if(sensorData != null && sensorData.sensorInterface != null)
		{
			sensorData.sensorInterface.CloseSensor(sensorData);
		}
	}

	// invoked periodically to update sensor data, if needed
	public static bool UpdateSensorData(SensorData sensorData)
	{
		bool bResult = false;

		if(sensorData.sensorInterface != null)
		{
			bResult = sensorData.sensorInterface.UpdateSensorData(sensorData);
		}

		return bResult;
	}
	
	// returns the mirror joint of the given joint
	public static JointType GetMirrorJoint(JointType joint)
	{
		switch(joint)
		{
			case JointType.ShoulderLeft:
				return JointType.ShoulderRight;
	        case JointType.ElbowLeft:
				return JointType.ElbowRight;
	        case JointType.WristLeft:
				return JointType.WristRight;
	        case JointType.HandLeft:
				return JointType.HandRight;
					
	        case JointType.ShoulderRight:
				return JointType.ShoulderLeft;
	        case JointType.ElbowRight:
				return JointType.ElbowLeft;
	        case JointType.WristRight:
				return JointType.WristLeft;
	        case JointType.HandRight:
				return JointType.HandLeft;
					
	        case JointType.HipLeft:
				return JointType.HipRight;
	        case JointType.KneeLeft:
				return JointType.KneeRight;
	        case JointType.AnkleLeft:
				return JointType.AnkleRight;
	        case JointType.FootLeft:
				return JointType.FootRight;
					
	        case JointType.HipRight:
				return JointType.HipLeft;
	        case JointType.KneeRight:
				return JointType.KneeLeft;
	        case JointType.AnkleRight:
				return JointType.AnkleLeft;
	        case JointType.FootRight:
				return JointType.FootLeft;
					
	        case JointType.HandTipLeft:
				return JointType.HandTipRight;
	        case JointType.ThumbLeft:
				return JointType.ThumbRight;
			
	        case JointType.HandTipRight:
				return JointType.HandTipLeft;
	        case JointType.ThumbRight:
				return JointType.ThumbLeft;
		}
	
		return joint;
	}

	// gets new multi source frame
	public static bool GetMultiSourceFrame(SensorData sensorData)
	{
		bool bResult = false;

		if(sensorData.sensorInterface != null)
		{
			bResult = sensorData.sensorInterface.GetMultiSourceFrame(sensorData);
		}

		return bResult;
	}

	// frees last multi source frame
	public static void FreeMultiSourceFrame(SensorData sensorData)
	{
		if(sensorData.sensorInterface != null)
		{
			sensorData.sensorInterface.FreeMultiSourceFrame(sensorData);
		}
	}

	// Polls for new skeleton data
	public static bool PollBodyFrame(SensorData sensorData, ref BodyFrameData bodyFrame, ref Matrix4x4 kinectToWorld)
	{
		bool bNewFrame = false;

		if(sensorData.sensorInterface != null)
		{
			bNewFrame = sensorData.sensorInterface.PollBodyFrame(sensorData, ref bodyFrame, ref kinectToWorld);

			if(bNewFrame)
			{
				for(int i = 0; i < sensorData.bodyCount; i++)
				{
					if(bodyFrame.bodyData[i].bIsTracked != 0)
					{
						// calculate joint directions
						for(int j = 0; j < sensorData.jointCount; j++)
						{
							if(j == 0)
							{
								bodyFrame.bodyData[i].joint[j].direction = Vector3.zero;
							}
							else
							{
								int jParent = (int)sensorData.sensorInterface.GetParentJoint(bodyFrame.bodyData[i].joint[j].jointType);
								
								if(bodyFrame.bodyData[i].joint[j].trackingState != TrackingState.NotTracked && 
								   bodyFrame.bodyData[i].joint[jParent].trackingState != TrackingState.NotTracked)
								{
									bodyFrame.bodyData[i].joint[j].direction = 
										bodyFrame.bodyData[i].joint[j].position - bodyFrame.bodyData[i].joint[jParent].position;
								}
							}
						}
					}

				}
			}
		}
		
		return bNewFrame;
	}

	// Polls for new color frame data
	public static bool PollColorFrame(SensorData sensorData)
	{
		bool bNewFrame = false;

		if(sensorData.sensorInterface != null)
		{
			bNewFrame = sensorData.sensorInterface.PollColorFrame(sensorData);
		}

		return bNewFrame;
	}

	// Polls for new depth frame data
	public static bool PollDepthFrame(SensorData sensorData)
	{
		bool bNewFrame = false;

		if(sensorData.sensorInterface != null)
		{
			bNewFrame = sensorData.sensorInterface.PollDepthFrame(sensorData);
		}

		return bNewFrame;
	}

	// Polls for new infrared frame data
	public static bool PollInfraredFrame(SensorData sensorData)
	{
		bool bNewFrame = false;

		if(sensorData.sensorInterface != null)
		{
			bNewFrame = sensorData.sensorInterface.PollInfraredFrame(sensorData);
		}

		return bNewFrame;
	}

	// returns depth frame coordinates for the given 3d Kinect-space point
	public static Vector2 MapSpacePointToDepthCoords(SensorData sensorData, Vector3 kinectPos)
	{
		Vector2 vPoint = Vector2.zero;

		if(sensorData.sensorInterface != null)
		{
			vPoint = sensorData.sensorInterface.MapSpacePointToDepthCoords(sensorData, kinectPos);
		}

		return vPoint;
	}

	// returns 3d coordinates for the given depth-map point
	public static Vector3 MapDepthPointToSpaceCoords(SensorData sensorData, Vector2 depthPos, ushort depthVal)
	{
		Vector3 vPoint = Vector3.zero;

		if(sensorData.sensorInterface != null)
		{
			vPoint = sensorData.sensorInterface.MapDepthPointToSpaceCoords(sensorData, depthPos, depthVal);
		}

		return vPoint;
	}

	// returns color-map coordinates for the given depth point
	public static Vector2 MapDepthPointToColorCoords(SensorData sensorData, Vector2 depthPos, ushort depthVal)
	{
		Vector2 vPoint = Vector2.zero;

		if(sensorData.sensorInterface != null)
		{
			vPoint = sensorData.sensorInterface.MapDepthPointToColorCoords(sensorData, depthPos, depthVal);
		}

		return vPoint;
	}

	// estimates color-map coordinates for the current depth frame
	public static bool MapDepthFrameToColorCoords(SensorData sensorData, ref Vector2[] vColorCoords)
	{
		bool bResult = false;

		if(sensorData.sensorInterface != null)
		{
			bResult = sensorData.sensorInterface.MapDepthFrameToColorCoords(sensorData, ref vColorCoords);
		}

		return bResult;
	}

	// Copy a resource file to the target
	public static bool CopyResourceFile(string targetFilePath, string resFileName, ref bool bOneCopied, ref bool bAllCopied)
	{
		TextAsset textRes = Resources.Load(resFileName, typeof(TextAsset)) as TextAsset;
		if(textRes == null)
		{
			bOneCopied = false;
			bAllCopied = false;
			
			return false;
		}
		
		FileInfo targetFile = new FileInfo(targetFilePath);
		if(!targetFile.Directory.Exists)
		{
			targetFile.Directory.Create();
		}
		
		if(!targetFile.Exists || targetFile.Length !=  textRes.bytes.Length)
		{
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream (targetFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write(textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bool bFileCopied = File.Exists(targetFilePath);
				
				bOneCopied = bOneCopied || bFileCopied;
				bAllCopied = bAllCopied && bFileCopied;
				
				return bFileCopied;
			}
		}
		
		return false;
	}
	
}