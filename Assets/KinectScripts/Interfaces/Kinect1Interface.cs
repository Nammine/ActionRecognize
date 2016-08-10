using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;

public class Kinect1Interface : DepthSensorInterface 
{
	public static class Constants
	{
		public const int NuiSkeletonCount = 6;

		public const NuiImageResolution ColorImageResolution = NuiImageResolution.resolution640x480;
		public const NuiImageResolution DepthImageResolution = NuiImageResolution.resolution640x480;
		
		public const bool IsNearMode = false;
	}

	// Structs and constants for interfacing C# with the Kinect.dll 

	[Flags]
	public enum NuiInitializeFlags : uint
	{
		UsesNone = 0,
		UsesAudio = 0x10000000,
		UsesDepthAndPlayerIndex = 0x00000001,
		UsesColor = 0x00000002,
		UsesSkeleton = 0x00000008,
		UsesDepth = 0x00000020,
		UsesHighQualityColor = 0x00000040
	}
	
	public enum NuiErrorCodes : uint
	{
		FrameNoData = 0x83010001,
		StreamNotEnabled = 0x83010002,
		ImageStreamInUse = 0x83010003,
		FrameLimitExceeded = 0x83010004,
		FeatureNotInitialized = 0x83010005,
		DeviceNotGenuine = 0x83010006,
		InsufficientBandwidth = 0x83010007,
		DeviceNotSupported = 0x83010008,
		DeviceInUse = 0x83010009,
		
		DatabaseNotFound = 0x8301000D,
		DatabaseVersionMismatch = 0x8301000E,
		HardwareFeatureUnavailable = 0x8301000F,
		
		DeviceNotConnected = 0x83010014,
		DeviceNotReady = 0x83010015,
		SkeletalEngineBusy = 0x830100AA,
		DeviceNotPowered = 0x8301027F,
	}
	
	public enum NuiSkeletonPositionIndex : int
	{
		HipCenter = 0,
		Spine,
		ShoulderCenter,
		Head,
		ShoulderLeft,
		ElbowLeft,
		WristLeft,
		HandLeft,
		ShoulderRight,
		ElbowRight,
		WristRight,
		HandRight,
		HipLeft,
		KneeLeft,
		AnkleLeft,
		FootLeft,
		HipRight,
		KneeRight,
		AnkleRight,
		FootRight,
		Count
	}
	
	public enum NuiSkeletonPositionTrackingState
	{
		NotTracked = 0,
		Inferred,
		Tracked
	}
	
	public enum NuiSkeletonTrackingState
	{
		NotTracked = 0,
		PositionOnly,
		SkeletonTracked
	}
	
	public enum NuiImageType
	{
		DepthAndPlayerIndex = 0,	// USHORT
		Color,						// RGB32 data
		ColorYUV,					// YUY2 stream from camera h/w, but converted to RGB32 before user getting it.
		ColorRawYUV,				// YUY2 stream from camera h/w.
		Depth						// USHORT
	}
	
	public enum NuiImageResolution
	{
		resolutionInvalid = -1,
		resolution80x60 = 0,
		resolution320x240 = 1,
		resolution640x480 = 2,
		resolution1280x960 = 3     // for hires color only
	}

	public enum NuiImageStreamFlags
	{
		None = 0x00000000,
		SupressNoFrameData = 0x0001000,
		EnableNearMode = 0x00020000,
		TooFarIsNonZero = 0x0004000
	}
	
	public struct NuiSkeletonData
	{
		public NuiSkeletonTrackingState eTrackingState;
		public uint dwTrackingID;
		public uint dwEnrollmentIndex_NotUsed;
		public uint dwUserIndex;
		public Vector4 Position;
		[MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 20, ArraySubType = UnmanagedType.Struct)]
		public Vector4[] SkeletonPositions;
		[MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 20, ArraySubType = UnmanagedType.Struct)]
		public NuiSkeletonPositionTrackingState[] eSkeletonPositionTrackingState;
		public uint dwQualityFlags;
	}
	
	public struct NuiSkeletonFrame
	{
		public long liTimeStamp;
		public uint dwFrameNumber;
		public uint dwFlags;
		public Vector4 vFloorClipPlane;
		public Vector4 vNormalToGravity;
		[MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.Struct)]
		public NuiSkeletonData[] SkeletonData;
	}
	
	public struct NuiTransformSmoothParameters
	{
		public float fSmoothing;
		public float fCorrection;
		public float fPrediction;
		public float fJitterRadius;
		public float fMaxDeviationRadius;
	}
	
	public struct NuiSkeletonBoneRotation
	{
		public Matrix4x4 rotationMatrix;
		public Quaternion rotationQuaternion;
	}
	
	public struct NuiSkeletonBoneOrientation
	{
		public NuiSkeletonPositionIndex endJoint;

		public NuiSkeletonPositionIndex startJoint;
		public NuiSkeletonBoneRotation hierarchicalRotation;
		public NuiSkeletonBoneRotation absoluteRotation;
	}
	
	public struct NuiImageViewArea
	{
		public int eDigitalZoom;
		public int lCenterX;
		public int lCenterY;
	}
	
	public class NuiImageBuffer
	{
		public int m_Width;
		public int m_Height;
		public int m_BytesPerPixel;
		public IntPtr m_pBuffer;
	}
	
	public struct NuiImageFrame
	{
		public Int64 liTimeStamp;
		public uint dwFrameNumber;
		public NuiImageType eImageType;
		public NuiImageResolution eResolution;
		//[MarshalAsAttribute(UnmanagedType.Interface)]
		public IntPtr pFrameTexture;
		public uint dwFrameFlags_NotUsed;
		public NuiImageViewArea ViewArea_NotUsed;
	}
	
	public struct NuiLockedRect
	{
		public int pitch;
		public int size;
		//[MarshalAsAttribute(UnmanagedType.U8)] 
		public IntPtr pBits; 
		
	}

	public enum NuiHandpointerState : uint
	{
		None = 0,
		Tracked = 1,
		Active = 2,
		Interactive = 4,
		Pressed = 8,
		PrimaryForUser = 0x10
	}
	
	public enum InteractionHandEventType : int
	{
		None = 0,
		Grip = 1,
		Release = 2
	}
	

	// private interface data

	private KinectInterop.FrameSource sourceFlags;

	//private IntPtr colorStreamHandle;
	//private IntPtr depthStreamHandle;

	private NuiSkeletonFrame skeletonFrame;
	private NuiTransformSmoothParameters smoothParameters;

	private NuiImageViewArea pcViewArea = new NuiImageViewArea 
	{
		eDigitalZoom = 0,
		lCenterX = 0,
		lCenterY = 0
	};
	
	// exported wrapper functions

	[DllImport(@"Kinect10.dll")]
	private static extern int NuiGetSensorCount(out int pCount);

	[DllImport(@"Kinect10.dll")]
	private static extern int NuiTransformSmooth(ref NuiSkeletonFrame pSkeletonFrame, ref NuiTransformSmoothParameters pSmoothingParams);

	[DllImport(@"Kinect10.dll")]
	private static extern int NuiImageGetColorPixelCoordinatesFromDepthPixelAtResolution(NuiImageResolution eColorResolution, NuiImageResolution eDepthResolution, ref NuiImageViewArea pcViewArea, int lDepthX, int lDepthY, ushort sDepthValue, out int plColorX, out int plColorY);
	

	[DllImportAttribute(@"KinectUnityWrapper.dll")]
	private static extern int InitKinectSensor(NuiInitializeFlags dwFlags, bool bEnableEvents, int iColorResolution, int iDepthResolution, bool bNearMode);
	
	[DllImportAttribute(@"KinectUnityWrapper.dll")]
	private static extern void ShutdownKinectSensor();
	
	[DllImportAttribute(@"KinectUnityWrapper.dll")]
	private static extern int SetKinectElevationAngle(int sensorAngle);
	
	[DllImportAttribute(@"KinectUnityWrapper.dll")]
	private static extern int GetKinectElevationAngle();
	
	[DllImportAttribute(@"KinectUnityWrapper.dll")]
	private static extern int UpdateKinectSensor();
	
	[DllImport(@"KinectUnityWrapper.dll")]
	private static extern int GetSkeletonFrameLength();
	
	[DllImport(@"KinectUnityWrapper.dll")]
	private static extern bool GetSkeletonFrameData(ref NuiSkeletonFrame pSkeletonData, ref uint iDataBufLen, bool bNewFrame);
	
	[DllImport(@"KinectUnityWrapper.dll")]
	private static extern int GetNextSkeletonFrame(uint dwWaitMs);

	[DllImport(@"KinectUnityWrapper.dll")]
	private static extern IntPtr GetColorStreamHandle();
	
	[DllImport(@"KinectUnityWrapper.dll")]
	private static extern IntPtr GetDepthStreamHandle();
	
	[DllImport(@"KinectUnityWrapper.dll")]
	private static extern bool GetColorFrameData(IntPtr btVideoBuf, ref uint iVideoBufLen, bool bGetNewFrame);
	
	[DllImport(@"KinectUnityWrapper.dll")]
	private static extern bool GetDepthFrameData(IntPtr shDepthBuf, ref uint iDepthBufLen, bool bGetNewFrame);
	
	[DllImport(@"KinectUnityWrapper.dll")]
	private static extern bool GetInfraredFrameData(IntPtr shInfraredBuf, ref uint iInfraredBufLen, bool bGetNewFrame);


	[DllImport(@"KinectUnityWrapper")]
	private static extern int InitKinectInteraction();
	
	[DllImport(@"KinectUnityWrapper")]
	private static extern void FinishKinectInteraction();
	
	[DllImport( @"KinectUnityWrapper")]
	private static extern uint GetInteractorsCount();
	
	[DllImport( @"KinectUnityWrapper", EntryPoint = "GetInteractorSkeletonTrackingID" )]
	private static extern uint GetSkeletonTrackingID( uint player );
	
	[DllImport( @"KinectUnityWrapper", EntryPoint = "GetInteractorLeftHandState" )]
	private static extern uint GetLeftHandState( uint player );
	
	[DllImport( @"KinectUnityWrapper", EntryPoint = "GetInteractorRightHandState" )]
	private static extern uint GetRightHandState( uint player );
	
	[DllImport( @"KinectUnityWrapper", EntryPoint = "GetInteractorLeftHandEvent" )]
	private static extern InteractionHandEventType GetLeftHandEvent( uint player );
	
	[DllImport( @"KinectUnityWrapper", EntryPoint = "GetInteractorRightHandEvent" )]
	private static extern InteractionHandEventType GetRightHandEvent( uint player );
	


	private string GetNuiErrorString(int hr)
	{
		string message = string.Empty;
		uint uhr = (uint)hr;
		
		switch(uhr)
		{
		case (uint)NuiErrorCodes.FrameNoData:
			message = "Frame contains no data.";
			break;
		case (uint)NuiErrorCodes.StreamNotEnabled:
			message = "Stream is not enabled.";
			break;
		case (uint)NuiErrorCodes.ImageStreamInUse:
			message = "Image stream is already in use.";
			break;
		case (uint)NuiErrorCodes.FrameLimitExceeded:
			message = "Frame limit is exceeded.";
			break;
		case (uint)NuiErrorCodes.FeatureNotInitialized:
			message = "Feature is not initialized.";
			break;
		case (uint)NuiErrorCodes.DeviceNotGenuine:
			message = "Device is not genuine.";
			break;
		case (uint)NuiErrorCodes.InsufficientBandwidth:
			message = "Bandwidth is not sufficient.";
			break;
		case (uint)NuiErrorCodes.DeviceNotSupported:
			message = "Device is not supported (e.g. Kinect for XBox 360).";
			break;
		case (uint)NuiErrorCodes.DeviceInUse:
			message = "Device is already in use.";
			break;
		case (uint)NuiErrorCodes.DatabaseNotFound:
			message = "Database not found.";
			break;
		case (uint)NuiErrorCodes.DatabaseVersionMismatch:
			message = "Database version mismatch.";
			break;
		case (uint)NuiErrorCodes.HardwareFeatureUnavailable:
			message = "Hardware feature is not available.";
			break;
		case (uint)NuiErrorCodes.DeviceNotConnected:
			message = "Device is not connected.";
			break;
		case (uint)NuiErrorCodes.DeviceNotReady:
			message = "Device is not ready.";
			break;
		case (uint)NuiErrorCodes.SkeletalEngineBusy:
			message = "Skeletal engine is busy.";
			break;
		case (uint)NuiErrorCodes.DeviceNotPowered:
			message = "Device is not powered.";
			break;
			
		default:
			message = "hr=0x" + uhr.ToString("X");
			break;
		}
		
		return message;
	}

	private bool NuiImageResolutionToSize(NuiImageResolution res, out int refWidth, out int refHeight)
	{
		switch( res )
		{
			case NuiImageResolution.resolution80x60:
				refWidth = 80;
				refHeight = 60;
				return true;
			case NuiImageResolution.resolution320x240:
				refWidth = 320;
				refHeight = 240;
				return true;
			case NuiImageResolution.resolution640x480:
				refWidth = 640;
				refHeight = 480;
				return true;
			case NuiImageResolution.resolution1280x960:
				refWidth = 1280;
				refHeight = 960;
				return true;
			default:
				refWidth = 0;
				refHeight = 0;
				break;
		}

		return false;
	}
	
	public bool InitSensorInterface (ref bool bNeedRestart)
	{
		bool bOneCopied = false, bAllCopied = true;
		
		KinectInterop.CopyResourceFile("KinectUnityWrapper.dll", "KinectUnityWrapper.dll", ref bOneCopied, ref bAllCopied);
		KinectInterop.CopyResourceFile("KinectInteraction180_32.dll", "KinectInteraction180_32.dll", ref bOneCopied, ref bAllCopied);
		KinectInterop.CopyResourceFile("FaceTrackData.dll", "FaceTrackData.dll", ref bOneCopied, ref bAllCopied);
		KinectInterop.CopyResourceFile("FaceTrackLib.dll", "FaceTrackLib.dll", ref bOneCopied, ref bAllCopied);
		KinectInterop.CopyResourceFile("KinectBackgroundRemoval180_32.dll", "KinectBackgroundRemoval180_32.dll", ref bOneCopied, ref bAllCopied);
		
		KinectInterop.CopyResourceFile("msvcp100d.dll", "msvcp100d.dll", ref bOneCopied, ref bAllCopied);
		KinectInterop.CopyResourceFile("msvcr100d.dll", "msvcr100d.dll", ref bOneCopied, ref bAllCopied);
		
		bNeedRestart = (bOneCopied && bAllCopied);

		return true;
	}

	public void FreeSensorInterface ()
	{
	}

	public int GetSensorsCount ()
	{
		int iSensorCount = 0;
		int hr = NuiGetSensorCount(out iSensorCount);

		if(hr == 0)
			return iSensorCount;
		else
			return 0;
	}

	public KinectInterop.SensorData OpenDefaultSensor (KinectInterop.FrameSource dwFlags, float sensorAngle, bool bUseMultiSource)
	{
		sourceFlags = dwFlags;

		NuiInitializeFlags nuiFlags = // NuiInitializeFlags.UsesNone;
			NuiInitializeFlags.UsesSkeleton | NuiInitializeFlags.UsesDepthAndPlayerIndex;

		if((dwFlags & KinectInterop.FrameSource.TypeBody) != 0)
		{
			nuiFlags |= NuiInitializeFlags.UsesSkeleton;
		}
		
		if((dwFlags & KinectInterop.FrameSource.TypeColor) != 0)
		{
			nuiFlags |= NuiInitializeFlags.UsesColor;
		}
		
		if((dwFlags & KinectInterop.FrameSource.TypeDepth) != 0)
		{
			nuiFlags |= NuiInitializeFlags.UsesDepthAndPlayerIndex;
		}
		
		if((dwFlags & KinectInterop.FrameSource.TypeBodyIndex) != 0)
		{
			nuiFlags |= NuiInitializeFlags.UsesDepthAndPlayerIndex;
		}
		
		if((dwFlags & KinectInterop.FrameSource.TypeInfrared) != 0)
		{
			nuiFlags |= (NuiInitializeFlags.UsesColor | (NuiInitializeFlags)0x8000);
		}
		
		int hr = InitKinectSensor(nuiFlags, true, (int)Constants.ColorImageResolution, (int)Constants.DepthImageResolution, Constants.IsNearMode);

		if(hr == 0)
		{
			// set sensor angle
			SetKinectElevationAngle((int)sensorAngle);

			// initialize Kinect interaction
			hr = InitKinectInteraction();
			if(hr != 0)
			{
				Debug.LogError("Initialization of KinectInteraction failed.");
			}
			
			KinectInterop.SensorData sensorData = new KinectInterop.SensorData();

			sensorData.bodyCount = Constants.NuiSkeletonCount;
			sensorData.jointCount = 20;

			NuiImageResolutionToSize(Constants.ColorImageResolution, out sensorData.colorImageWidth, out sensorData.colorImageHeight);
//			sensorData.colorImageWidth = Constants.ColorImageWidth;
//			sensorData.colorImageHeight = Constants.ColorImageHeight;

			if((dwFlags & KinectInterop.FrameSource.TypeColor) != 0)
			{
				//colorStreamHandle =  GetColorStreamHandle();
				sensorData.colorImage = new byte[sensorData.colorImageWidth * sensorData.colorImageHeight * 4];
			}

			NuiImageResolutionToSize(Constants.DepthImageResolution, out sensorData.depthImageWidth, out sensorData.depthImageHeight);
//			sensorData.depthImageWidth = Constants.DepthImageWidth;
//			sensorData.depthImageHeight = Constants.DepthImageHeight;
			
			if((dwFlags & KinectInterop.FrameSource.TypeDepth) != 0)
			{
				//depthStreamHandle = GetDepthStreamHandle();
				sensorData.depthImage = new ushort[sensorData.depthImageWidth * sensorData.depthImageHeight];
			}
			
			if((dwFlags & KinectInterop.FrameSource.TypeBodyIndex) != 0)
			{
				sensorData.bodyIndexImage = new byte[sensorData.depthImageWidth * sensorData.depthImageHeight];
			}
			
			if((dwFlags & KinectInterop.FrameSource.TypeInfrared) != 0)
			{
				sensorData.infraredImage = new ushort[sensorData.colorImageWidth * sensorData.colorImageHeight];
			}

			if((dwFlags & KinectInterop.FrameSource.TypeBody) != 0)
			{
				skeletonFrame = new NuiSkeletonFrame() 
				{ 
					SkeletonData = new NuiSkeletonData[Constants.NuiSkeletonCount] 
				};
				
				// default values used to pass to smoothing function
				smoothParameters = new NuiTransformSmoothParameters();

				smoothParameters.fSmoothing = 0.5f;
				smoothParameters.fCorrection = 0.5f;
				smoothParameters.fPrediction = 0.5f;
				smoothParameters.fJitterRadius = 0.05f;
				smoothParameters.fMaxDeviationRadius = 0.04f;
			}
			
			return sensorData;
		}
		else
		{
			Debug.LogError("InitKinectSensor failed: " + GetNuiErrorString(hr));
		}

		return null;
	}

	public void CloseSensor (KinectInterop.SensorData sensorData)
	{
		FinishKinectInteraction();
		ShutdownKinectSensor();
	}

	public bool UpdateSensorData (KinectInterop.SensorData sensorData)
	{
		int hr = UpdateKinectSensor();
		return (hr == 0);
	}

	public bool GetMultiSourceFrame (KinectInterop.SensorData sensorData)
	{
		return false;
	}

	public void FreeMultiSourceFrame (KinectInterop.SensorData sensorData)
	{
	}

	private int NuiSkeletonGetNextFrame(uint dwMillisecondsToWait, ref NuiSkeletonFrame pSkeletonFrame)
	{
		if(sourceFlags != KinectInterop.FrameSource.TypeAudio)
		{
			// non-audio sources
			uint iFrameLength = (uint)GetSkeletonFrameLength();
			bool bSuccess = GetSkeletonFrameData(ref pSkeletonFrame, ref iFrameLength, true);
			return bSuccess ? 0 : -1;
		}
		else
		{
			// audio only
			int hr = GetNextSkeletonFrame(dwMillisecondsToWait);

			if(hr == 0)
			{
				uint iFrameLength = (uint)GetSkeletonFrameLength();
				bool bSuccess = GetSkeletonFrameData(ref pSkeletonFrame, ref iFrameLength, true);
				
				return bSuccess ? 0 : -1;
			}
			
			return hr;
		}
	}

	private void GetHandStateAndConf(uint handState, InteractionHandEventType handEvent, 
	                                 ref KinectInterop.HandState refHandState, ref KinectInterop.TrackingConfidence refHandConf)
	{
		bool bHandPrimary = (handState & (uint)NuiHandpointerState.PrimaryForUser) != 0;

		refHandConf = bHandPrimary ? KinectInterop.TrackingConfidence.High : KinectInterop.TrackingConfidence.Low;

		if(bHandPrimary)
		{
			switch(handEvent)
			{
				case InteractionHandEventType.Grip:
					refHandState = KinectInterop.HandState.Closed;
					break;

				case InteractionHandEventType.Release:
				//case InteractionHandEventType.None:
					refHandState = KinectInterop.HandState.Open;
					break;
			}
		}
		else
		{
			refHandState = KinectInterop.HandState.NotTracked;
		}
	}

	public bool PollBodyFrame (KinectInterop.SensorData sensorData, ref KinectInterop.BodyFrameData bodyFrame, ref Matrix4x4 kinectToWorld)
	{
		bool newSkeleton = false;
		
		int hr = NuiSkeletonGetNextFrame(0, ref skeletonFrame);
		if(hr == 0)
		{
			newSkeleton = true;
		}
		
		if(newSkeleton)
		{
			hr = NuiTransformSmooth(ref skeletonFrame, ref smoothParameters);
			if(hr != 0)
			{
				Debug.LogError("Skeleton Data Smoothing failed");
			}

			for(uint i = 0; i < sensorData.bodyCount; i++)
			{
				NuiSkeletonData body = skeletonFrame.SkeletonData[i];
				
				bodyFrame.bodyData[i].bIsTracked = (short)(body.eTrackingState ==  NuiSkeletonTrackingState.SkeletonTracked ? 1 : 0);
				
				if(body.eTrackingState ==  NuiSkeletonTrackingState.SkeletonTracked)
				{
					// transfer body and joints data
					bodyFrame.bodyData[i].liTrackingID = (long)body.dwTrackingID;
					
					for(int j = 0; j < sensorData.jointCount; j++)
					{
						KinectInterop.JointData jointData = bodyFrame.bodyData[i].joint[j];
						
						jointData.jointType = GetJointAtIndex(j);
						jointData.trackingState = (KinectInterop.TrackingState)body.eSkeletonPositionTrackingState[j];
						
						if(jointData.trackingState != KinectInterop.TrackingState.NotTracked)
						{
							jointData.kinectPos = body.SkeletonPositions[j];
							jointData.position = kinectToWorld.MultiplyPoint3x4(jointData.kinectPos);
						}
						
						jointData.orientation = Quaternion.identity;
//							Windows.Kinect.Vector4 vQ = body.JointOrientations[jointData.jointType].Orientation;
//							jointData.orientation = new Quaternion(vQ.X, vQ.Y, vQ.Z, vQ.W);
						
						if(j == 0)
						{
							bodyFrame.bodyData[i].position = jointData.position;
							bodyFrame.bodyData[i].orientation = jointData.orientation;
						}
						
						bodyFrame.bodyData[i].joint[j] = jointData;
					}


					// tranfer hand states
					uint intCount = GetInteractorsCount();

					for(uint intIndex = 0; intIndex < intCount; intIndex++)
					{
						uint skeletonId = GetSkeletonTrackingID(intIndex);

						if(skeletonId == body.dwTrackingID)
						{
							uint leftHandState = GetLeftHandState(intIndex);
							InteractionHandEventType leftHandEvent = GetLeftHandEvent(intIndex);
							
							uint rightHandState = GetRightHandState(intIndex);
							InteractionHandEventType rightHandEvent = GetRightHandEvent(intIndex);
							
							GetHandStateAndConf(leftHandState, leftHandEvent, 
							                    ref bodyFrame.bodyData[i].leftHandState, 
							                    ref bodyFrame.bodyData[i].leftHandConfidence);
							
							GetHandStateAndConf(rightHandState, rightHandEvent, 
							                    ref bodyFrame.bodyData[i].rightHandState, 
							                    ref bodyFrame.bodyData[i].rightHandConfidence);
						}
					}

				}
			}
			
		}
		
		return newSkeleton;
	}

	public bool PollColorFrame (KinectInterop.SensorData sensorData)
	{
		uint videoBufLen = (uint)sensorData.colorImage.Length;
		
		var pColorData = GCHandle.Alloc(sensorData.colorImage, GCHandleType.Pinned);
		bool newColor = GetColorFrameData(pColorData.AddrOfPinnedObject(), ref videoBufLen, true);
		pColorData.Free();
		
		if (newColor)
		{
			for (int i = 0; i < videoBufLen; i += 4)
			{
				byte btTmp = sensorData.colorImage[i];
				sensorData.colorImage[i] = sensorData.colorImage[i + 2];
				sensorData.colorImage[i + 2] = btTmp;
				sensorData.colorImage[i + 3] = 255;
			}
		}

		return newColor;
	}

	public bool PollDepthFrame (KinectInterop.SensorData sensorData)
	{
		uint depthBufLen = (uint)(sensorData.depthImage.Length * sizeof(ushort));
		
		var pDepthData = GCHandle.Alloc(sensorData.depthImage, GCHandleType.Pinned);
		bool newDepth = GetDepthFrameData(pDepthData.AddrOfPinnedObject(), ref depthBufLen, true);
		pDepthData.Free();

		if(newDepth)
		{
			uint depthLen = (uint)sensorData.depthImage.Length;

			for (int i = 0; i < depthLen; i++)
			{
				if((sensorData.depthImage[i] & 7) != 0)
					sensorData.bodyIndexImage[i] = (byte)((sensorData.depthImage[i] & 7) - 1);
				else
					sensorData.bodyIndexImage[i] = 255;

				sensorData.depthImage[i] = (ushort)(sensorData.depthImage[i] >> 3);
			}
		}

		return newDepth;
	}

	public bool PollInfraredFrame (KinectInterop.SensorData sensorData)
	{
		uint infraredBufLen = (uint)(sensorData.infraredImage.Length * sizeof(ushort));
		
		var pInfraredData = GCHandle.Alloc(sensorData.infraredImage, GCHandleType.Pinned);
		bool newInfrared = GetInfraredFrameData(pInfraredData.AddrOfPinnedObject(), ref infraredBufLen, true);
		pInfraredData.Free();
		
		return newInfrared;
	}

	public void FixJointOrientations(KinectInterop.SensorData sensorData, ref KinectInterop.BodyData bodyData)
	{
		// fix the hips-to-spine tilt (it is about 40 degrees to the back)
		int hipsIndex = (int)KinectInterop.JointType.SpineBase;

		Quaternion quat = bodyData.joint[hipsIndex].normalRotation;
		//quat *= Quaternion.Euler(40f, 0f, 0f);
		bodyData.joint[hipsIndex].normalRotation = quat;

		Vector3 mirroredAngles = quat.eulerAngles;
		mirroredAngles.y = -mirroredAngles.y;
		mirroredAngles.z = -mirroredAngles.z;
		bodyData.joint[hipsIndex].mirroredRotation = Quaternion.Euler(mirroredAngles);

		bodyData.normalRotation = bodyData.joint[hipsIndex].normalRotation;
		bodyData.mirroredRotation = bodyData.joint[hipsIndex].mirroredRotation;
	}
	
	private static void NuiTransformSkeletonToDepthImage(Vector3 vPoint, out float pfDepthX, out float pfDepthY, out float pfDepthZ)
	{
		if (vPoint.z > float.Epsilon)
		{
			pfDepthX = 0.5f + ((vPoint.x * 285.63f) / (vPoint.z * 320f));
			pfDepthY = 0.5f - ((vPoint.y * 285.63f) / (vPoint.z * 240f));
			pfDepthZ = vPoint.z * 1000f;
		}
		else
		{
			pfDepthX = 0f;
			pfDepthY = 0f;
			pfDepthZ = 0f;
		}
	}
	
	public Vector2 MapSpacePointToDepthCoords (KinectInterop.SensorData sensorData, Vector3 spacePos)
	{
		float fDepthX, fDepthY, fDepthZ;
		NuiTransformSkeletonToDepthImage(spacePos, out fDepthX, out fDepthY, out fDepthZ);
		
		Vector3 point = new Vector3();
		point.x = (int)((fDepthX * sensorData.depthImageWidth) + 0.5f);
		point.y = (int)((fDepthY * sensorData.depthImageHeight) + 0.5f);
		point.z = (int)(fDepthZ + 0.5f);
		
		return point;
	}

	public Vector3 MapDepthPointToSpaceCoords (KinectInterop.SensorData sensorData, Vector2 depthPos, ushort depthVal)
	{
		throw new System.NotImplementedException ();
	}

	public Vector2 MapDepthPointToColorCoords (KinectInterop.SensorData sensorData, Vector2 depthPos, ushort depthVal)
	{
		int cx, cy;
		NuiImageGetColorPixelCoordinatesFromDepthPixelAtResolution(
			Constants.ColorImageResolution,
			Constants.DepthImageResolution,
			ref pcViewArea,
			(int)depthPos.x, (int)depthPos.y, (ushort)(depthVal << 3),
			out cx, out cy);
		
		return new Vector2(cx, cy);
	}

	public bool MapDepthFrameToColorCoords (KinectInterop.SensorData sensorData, ref Vector2[] vColorCoords)
	{
		return false;
	}

	// returns the index of the given joint in joint's array or -1 if joint is not applicable
	public int GetJointIndex(KinectInterop.JointType joint)
	{
		switch(joint)
		{
			case KinectInterop.JointType.SpineBase:
				return (int)NuiSkeletonPositionIndex.HipCenter;
			case KinectInterop.JointType.SpineMid:
				return (int)NuiSkeletonPositionIndex.Spine;
			case KinectInterop.JointType.Neck:
				return (int)NuiSkeletonPositionIndex.ShoulderCenter;
			case KinectInterop.JointType.Head:
				return (int)NuiSkeletonPositionIndex.Head;
				
			case KinectInterop.JointType.ShoulderLeft:
				return (int)NuiSkeletonPositionIndex.ShoulderLeft;
			case KinectInterop.JointType.ElbowLeft:
				return (int)NuiSkeletonPositionIndex.ElbowLeft;
			case KinectInterop.JointType.WristLeft:
				return (int)NuiSkeletonPositionIndex.WristLeft;
			case KinectInterop.JointType.HandLeft:
				return (int)NuiSkeletonPositionIndex.HandLeft;
				
			case KinectInterop.JointType.ShoulderRight:
				return (int)NuiSkeletonPositionIndex.ShoulderRight;
			case KinectInterop.JointType.ElbowRight:
				return (int)NuiSkeletonPositionIndex.ElbowRight;
			case KinectInterop.JointType.WristRight:
				return (int)NuiSkeletonPositionIndex.WristRight;
			case KinectInterop.JointType.HandRight:
				return (int)NuiSkeletonPositionIndex.HandRight;
				
			case KinectInterop.JointType.HipLeft:
				return (int)NuiSkeletonPositionIndex.HipLeft;
			case KinectInterop.JointType.KneeLeft:
				return (int)NuiSkeletonPositionIndex.KneeLeft;
			case KinectInterop.JointType.AnkleLeft:
				return (int)NuiSkeletonPositionIndex.AnkleLeft;
			case KinectInterop.JointType.FootLeft:
				return (int)NuiSkeletonPositionIndex.FootLeft;
				
			case KinectInterop.JointType.HipRight:
				return (int)NuiSkeletonPositionIndex.HipRight;
			case KinectInterop.JointType.KneeRight:
				return (int)NuiSkeletonPositionIndex.KneeRight;
			case KinectInterop.JointType.AnkleRight:
				return (int)NuiSkeletonPositionIndex.AnkleRight;
			case KinectInterop.JointType.FootRight:
				return (int)NuiSkeletonPositionIndex.FootRight;
		}
		
		return -1;
	}

	// returns the joint at given index
	public KinectInterop.JointType GetJointAtIndex(int index)
	{
		switch(index)
		{
		case (int)NuiSkeletonPositionIndex.HipCenter:
			return KinectInterop.JointType.SpineBase;
		case (int)NuiSkeletonPositionIndex.Spine:
			return KinectInterop.JointType.SpineMid;
		case (int)NuiSkeletonPositionIndex.ShoulderCenter:
			return KinectInterop.JointType.Neck;
		case (int)NuiSkeletonPositionIndex.Head:
			return KinectInterop.JointType.Head;
			
		case (int)NuiSkeletonPositionIndex.ShoulderLeft:
			return KinectInterop.JointType.ShoulderLeft;
		case (int)NuiSkeletonPositionIndex.ElbowLeft:
			return KinectInterop.JointType.ElbowLeft;
		case (int)NuiSkeletonPositionIndex.WristLeft:
			return KinectInterop.JointType.WristLeft;
		case (int)NuiSkeletonPositionIndex.HandLeft:
			return KinectInterop.JointType.HandLeft;
			
		case (int)NuiSkeletonPositionIndex.ShoulderRight:
			return KinectInterop.JointType.ShoulderRight;
		case (int)NuiSkeletonPositionIndex.ElbowRight:
			return KinectInterop.JointType.ElbowRight;
		case (int)NuiSkeletonPositionIndex.WristRight:
			return KinectInterop.JointType.WristRight;
		case (int)NuiSkeletonPositionIndex.HandRight:
			return KinectInterop.JointType.HandRight;
			
		case (int)NuiSkeletonPositionIndex.HipLeft:
			return KinectInterop.JointType.HipLeft;
		case (int)NuiSkeletonPositionIndex.KneeLeft:
			return KinectInterop.JointType.KneeLeft;
		case (int)NuiSkeletonPositionIndex.AnkleLeft:
			return KinectInterop.JointType.AnkleLeft;
		case (int)NuiSkeletonPositionIndex.FootLeft:
			return KinectInterop.JointType.FootLeft;
			
		case (int)NuiSkeletonPositionIndex.HipRight:
			return KinectInterop.JointType.HipRight;
		case (int)NuiSkeletonPositionIndex.KneeRight:
			return KinectInterop.JointType.KneeRight;
		case (int)NuiSkeletonPositionIndex.AnkleRight:
			return KinectInterop.JointType.AnkleRight;
		case (int)NuiSkeletonPositionIndex.FootRight:
			return KinectInterop.JointType.FootRight;
		}
		
		return (KinectInterop.JointType)(-1);
	}

	// returns the parent joint of the given joint
	public KinectInterop.JointType GetParentJoint(KinectInterop.JointType joint)
	{
		switch(joint)
		{
			case KinectInterop.JointType.SpineBase:
				return KinectInterop.JointType.SpineBase;
				
			case KinectInterop.JointType.ShoulderLeft:
			case KinectInterop.JointType.ShoulderRight:
				return KinectInterop.JointType.Neck;
				
			case KinectInterop.JointType.HipLeft:
			case KinectInterop.JointType.HipRight:
				return KinectInterop.JointType.SpineBase;
		}
		
		return (KinectInterop.JointType)((int)joint - 1);
	}
	
	// returns the next joint in the hierarchy, as to the given joint
	public KinectInterop.JointType GetNextJoint(KinectInterop.JointType joint)
	{
		switch(joint)
		{
			case KinectInterop.JointType.SpineBase:
				return KinectInterop.JointType.SpineMid;
			case KinectInterop.JointType.SpineMid:
				return KinectInterop.JointType.Neck;
			case KinectInterop.JointType.Neck:
				return KinectInterop.JointType.Head;
				
			case KinectInterop.JointType.ShoulderLeft:
				return KinectInterop.JointType.ElbowLeft;
			case KinectInterop.JointType.ElbowLeft:
				return KinectInterop.JointType.WristLeft;
			case KinectInterop.JointType.WristLeft:
				return KinectInterop.JointType.HandLeft;
				
			case KinectInterop.JointType.ShoulderRight:
				return KinectInterop.JointType.ElbowRight;
			case KinectInterop.JointType.ElbowRight:
				return KinectInterop.JointType.WristRight;
			case KinectInterop.JointType.WristRight:
				return KinectInterop.JointType.HandRight;
				
			case KinectInterop.JointType.HipLeft:
				return KinectInterop.JointType.KneeLeft;
			case KinectInterop.JointType.KneeLeft:
				return KinectInterop.JointType.AnkleLeft;
			case KinectInterop.JointType.AnkleLeft:
				return KinectInterop.JointType.FootLeft;
				
			case KinectInterop.JointType.HipRight:
				return KinectInterop.JointType.KneeRight;
			case KinectInterop.JointType.KneeRight:
				return KinectInterop.JointType.AnkleRight;
			case KinectInterop.JointType.AnkleRight:
				return KinectInterop.JointType.FootRight;
		}
		
		return joint;  // in case of end joint - Head, HandLeft, HandRight, FootLeft, FootRight
	}
	
}
