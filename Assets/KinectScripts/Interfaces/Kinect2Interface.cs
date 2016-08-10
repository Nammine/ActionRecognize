using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Runtime.InteropServices;

public class Kinect2Interface : DepthSensorInterface
{
	private KinectInterop.FrameSource sensorFlags;
	private KinectSensor kinectSensor;
	private CoordinateMapper coordMapper;
	
	private BodyFrameReader bodyFrameReader;
	private BodyIndexFrameReader bodyIndexFrameReader;
	private ColorFrameReader colorFrameReader;
	private DepthFrameReader depthFrameReader;
	private InfraredFrameReader infraredFrameReader;
	
	private MultiSourceFrameReader multiSourceFrameReader;
	private MultiSourceFrame multiSourceFrame;

	private Body[] bodyData;


	public bool InitSensorInterface (ref bool bNeedRestart)
	{
		bool bOneCopied = false, bAllCopied = true;
		KinectInterop.CopyResourceFile("KinectUnityAddin.dll", "KinectUnityAddin.dll", ref bOneCopied, ref bAllCopied);
		
		bNeedRestart = (bOneCopied && bAllCopied);

		return true;
	}

	public void FreeSensorInterface ()
	{
	}

	public int GetSensorsCount()
	{
		int numSensors = KinectSensor.GetDefault() != null ? 1 : 0;
		return numSensors;
	}

	public KinectInterop.SensorData OpenDefaultSensor (KinectInterop.FrameSource dwFlags, float sensorAngle, bool bUseMultiSource)
	{
		KinectInterop.SensorData sensorData = new KinectInterop.SensorData();
		//sensorFlags = dwFlags;
		
		kinectSensor = KinectSensor.GetDefault();
		if(kinectSensor == null)
			return null;
		
		coordMapper = kinectSensor.CoordinateMapper;

		sensorData.bodyCount = kinectSensor.BodyFrameSource.BodyCount;
		sensorData.jointCount = 25;
		
		if((dwFlags & KinectInterop.FrameSource.TypeBody) != 0)
		{
			if(!bUseMultiSource)
				bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();
			
			bodyData = new Body[sensorData.bodyCount];
		}
		
		var frameDesc = kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
		sensorData.colorImageWidth = frameDesc.Width;
		sensorData.colorImageHeight = frameDesc.Height;
		
		if((dwFlags & KinectInterop.FrameSource.TypeColor) != 0)
		{
			if(!bUseMultiSource)
				colorFrameReader = kinectSensor.ColorFrameSource.OpenReader();
			
			sensorData.colorImage = new byte[frameDesc.BytesPerPixel * frameDesc.LengthInPixels];
		}
		
		sensorData.depthImageWidth = kinectSensor.DepthFrameSource.FrameDescription.Width;
		sensorData.depthImageHeight = kinectSensor.DepthFrameSource.FrameDescription.Height;
		
		if((dwFlags & KinectInterop.FrameSource.TypeDepth) != 0)
		{
			if(!bUseMultiSource)
				depthFrameReader = kinectSensor.DepthFrameSource.OpenReader();
			
			sensorData.depthImage = new ushort[kinectSensor.DepthFrameSource.FrameDescription.LengthInPixels];
		}
		
		if((dwFlags & KinectInterop.FrameSource.TypeBodyIndex) != 0)
		{
			if(!bUseMultiSource)
				bodyIndexFrameReader = kinectSensor.BodyIndexFrameSource.OpenReader();
			
			sensorData.bodyIndexImage = new byte[kinectSensor.BodyIndexFrameSource.FrameDescription.LengthInPixels];
		}
		
		if((dwFlags & KinectInterop.FrameSource.TypeInfrared) != 0)
		{
			if(!bUseMultiSource)
				infraredFrameReader = kinectSensor.InfraredFrameSource.OpenReader();
			
			sensorData.infraredImage = new ushort[kinectSensor.InfraredFrameSource.FrameDescription.LengthInPixels];
		}
		
		if(!kinectSensor.IsOpen)
		{
			kinectSensor.Open();
		}
		
		if(bUseMultiSource && dwFlags != KinectInterop.FrameSource.TypeNone && kinectSensor.IsOpen)
		{
			multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader((FrameSourceTypes)dwFlags);
		}
		
		return sensorData;
	}

	public void CloseSensor (KinectInterop.SensorData sensorData)
	{
		if(coordMapper != null)
		{
			coordMapper = null;
		}
		
		if(bodyFrameReader != null)
		{
			bodyFrameReader.Dispose();
			bodyFrameReader = null;
		}
		
		if(bodyIndexFrameReader != null)
		{
			bodyIndexFrameReader.Dispose();
			bodyIndexFrameReader = null;
		}
		
		if(colorFrameReader != null)
		{
			colorFrameReader.Dispose();
			colorFrameReader = null;
		}
		
		if(depthFrameReader != null)
		{
			depthFrameReader.Dispose();
			depthFrameReader = null;
		}
		
		if(infraredFrameReader != null)
		{
			infraredFrameReader.Dispose();
			infraredFrameReader = null;
		}
		
		if(multiSourceFrameReader != null)
		{
			multiSourceFrameReader.Dispose();
			multiSourceFrameReader = null;
		}
		
		if(kinectSensor != null)
		{
			if (kinectSensor.IsOpen)
			{
				kinectSensor.Close();
			}
			
			kinectSensor = null;
		}
	}

	public bool UpdateSensorData (KinectInterop.SensorData sensorData)
	{
		return true;
	}

	public bool GetMultiSourceFrame (KinectInterop.SensorData sensorData)
	{
		if(multiSourceFrameReader != null)
		{
			multiSourceFrame = multiSourceFrameReader.AcquireLatestFrame();
			return (multiSourceFrame != null);
		}
		
		return false;
	}

	public void FreeMultiSourceFrame (KinectInterop.SensorData sensorData)
	{
		if(multiSourceFrame != null)
		{
			multiSourceFrame = null;
		}
	}

	public bool PollBodyFrame (KinectInterop.SensorData sensorData, ref KinectInterop.BodyFrameData bodyFrame, ref Matrix4x4 kinectToWorld)
	{
		bool bNewFrame = false;
		
		if((multiSourceFrameReader != null && multiSourceFrame != null) || 
		   bodyFrameReader != null)
		{
			var frame = multiSourceFrame != null ? multiSourceFrame.BodyFrameReference.AcquireFrame() :
				bodyFrameReader.AcquireLatestFrame();
			
			if(frame != null)
			{
				frame.GetAndRefreshBodyData(bodyData);
				bodyFrame.liRelativeTime = frame.RelativeTime.Ticks;
				
				frame.Dispose();
				frame = null;
				
				for(int i = 0; i < sensorData.bodyCount; i++)
				{
					Body body = bodyData[i];
					
					if (body == null)
					{
						bodyFrame.bodyData[i].bIsTracked = 0;
						continue;
					}
					
					bodyFrame.bodyData[i].bIsTracked = (short)(body.IsTracked ? 1 : 0);
					
					if(body.IsTracked)
					{
						// transfer body and joints data
						bodyFrame.bodyData[i].liTrackingID = (long)body.TrackingId;
						
						for(int j = 0; j < sensorData.jointCount; j++)
						{
							Windows.Kinect.Joint joint = body.Joints[(Windows.Kinect.JointType)j];
							KinectInterop.JointData jointData = bodyFrame.bodyData[i].joint[j];
							
							jointData.jointType = (KinectInterop.JointType)j;
							jointData.trackingState = (KinectInterop.TrackingState)joint.TrackingState;

							if((int)joint.TrackingState != (int)TrackingState.NotTracked)
							{
								jointData.kinectPos = new Vector3(joint.Position.X, joint.Position.Y, joint.Position.Z);
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
						bodyFrame.bodyData[i].leftHandState = (KinectInterop.HandState)body.HandLeftState;
						bodyFrame.bodyData[i].leftHandConfidence = (KinectInterop.TrackingConfidence)body.HandLeftConfidence;
						
						bodyFrame.bodyData[i].rightHandState = (KinectInterop.HandState)body.HandRightState;
						bodyFrame.bodyData[i].rightHandConfidence = (KinectInterop.TrackingConfidence)body.HandRightConfidence;
					}
				}
				
				bNewFrame = true;
			}
		}
		
		return bNewFrame;
	}

	public bool PollColorFrame (KinectInterop.SensorData sensorData)
	{
		bool bNewFrame = false;
		
		if((multiSourceFrameReader != null && multiSourceFrame != null) ||
		   colorFrameReader != null) 
		{
			var colorFrame = multiSourceFrame != null ? multiSourceFrame.ColorFrameReference.AcquireFrame() :
				colorFrameReader.AcquireLatestFrame();
			
			if(colorFrame != null)
			{
				var pColorData = GCHandle.Alloc(sensorData.colorImage, GCHandleType.Pinned);
				colorFrame.CopyConvertedFrameDataToIntPtr(pColorData.AddrOfPinnedObject(), (uint)sensorData.colorImage.Length, ColorImageFormat.Rgba);
				pColorData.Free();

				sensorData.lastColorFrameTime = colorFrame.RelativeTime.Ticks;
				
				colorFrame.Dispose();
				colorFrame = null;
				
				bNewFrame = true;
			}
		}
		
		return bNewFrame;
	}

	public bool PollDepthFrame (KinectInterop.SensorData sensorData)
	{
		bool bNewFrame = false;
		
		if((multiSourceFrameReader != null && multiSourceFrame != null) ||
		   depthFrameReader != null)
		{
			var depthFrame = multiSourceFrame != null ? multiSourceFrame.DepthFrameReference.AcquireFrame() :
				depthFrameReader.AcquireLatestFrame();
			
			if(depthFrame != null)
			{
				var pDepthData = GCHandle.Alloc(sensorData.depthImage, GCHandleType.Pinned);
				depthFrame.CopyFrameDataToIntPtr(pDepthData.AddrOfPinnedObject(), (uint)sensorData.depthImage.Length * sizeof(ushort));
				pDepthData.Free();
				
				sensorData.lastDepthFrameTime = depthFrame.RelativeTime.Ticks;
				
				depthFrame.Dispose();
				depthFrame = null;
				
				bNewFrame = true;
			}
			
			if((multiSourceFrameReader != null && multiSourceFrame != null) ||
			   bodyIndexFrameReader != null)
			{
				var bodyIndexFrame = multiSourceFrame != null ? multiSourceFrame.BodyIndexFrameReference.AcquireFrame() :
					bodyIndexFrameReader.AcquireLatestFrame();
				
				if(bodyIndexFrame != null)
				{
					var pBodyIndexData = GCHandle.Alloc(sensorData.bodyIndexImage, GCHandleType.Pinned);
					bodyIndexFrame.CopyFrameDataToIntPtr(pBodyIndexData.AddrOfPinnedObject(), (uint)sensorData.bodyIndexImage.Length);
					pBodyIndexData.Free();
					
					sensorData.lastBodyIndexFrameTime = bodyIndexFrame.RelativeTime.Ticks;
					
					bodyIndexFrame.Dispose();
					bodyIndexFrame = null;
					
					bNewFrame = true;
				}
			}
		}
		
		return bNewFrame;
	}

	public bool PollInfraredFrame (KinectInterop.SensorData sensorData)
	{
		bool bNewFrame = false;
		
		if((multiSourceFrameReader != null && multiSourceFrame != null) ||
		   infraredFrameReader != null)
		{
			var infraredFrame = multiSourceFrame != null ? multiSourceFrame.InfraredFrameReference.AcquireFrame() :
				infraredFrameReader.AcquireLatestFrame();
			
			if(infraredFrame != null)
			{
				var pInfraredData = GCHandle.Alloc(sensorData.infraredImage, GCHandleType.Pinned);
				infraredFrame.CopyFrameDataToIntPtr(pInfraredData.AddrOfPinnedObject(), (uint)sensorData.infraredImage.Length * sizeof(ushort));
				pInfraredData.Free();
				
				sensorData.lastInfraredFrameTime = infraredFrame.RelativeTime.Ticks;
				
				infraredFrame.Dispose();
				infraredFrame = null;
				
				bNewFrame = true;
			}
		}
		
		return bNewFrame;
	}

	public void FixJointOrientations(KinectInterop.SensorData sensorData, ref KinectInterop.BodyData bodyData)
	{
		// no fixes are needed
	}

	public Vector2 MapSpacePointToDepthCoords (KinectInterop.SensorData sensorData, Vector3 spacePos)
	{
		Vector2 vPoint = Vector2.zero;
		
		if(coordMapper != null)
		{
			CameraSpacePoint camPoint = new CameraSpacePoint();
			camPoint.X = spacePos.x;
			camPoint.Y = spacePos.y;
			camPoint.Z = spacePos.z;
			
			CameraSpacePoint[] camPoints = new CameraSpacePoint[1];
			camPoints[0] = camPoint;
			
			DepthSpacePoint[] depthPoints = new DepthSpacePoint[1];
			coordMapper.MapCameraPointsToDepthSpace(camPoints, depthPoints);
			
			DepthSpacePoint depthPoint = depthPoints[0];
			
			if(depthPoint.X >= 0 && depthPoint.X < sensorData.depthImageWidth &&
			   depthPoint.Y >= 0 && depthPoint.Y < sensorData.depthImageHeight)
			{
				vPoint.x = depthPoint.X;
				vPoint.y = depthPoint.Y;
			}
		}
		
		return vPoint;
	}

	public Vector3 MapDepthPointToSpaceCoords (KinectInterop.SensorData sensorData, Vector2 depthPos, ushort depthVal)
	{
		Vector3 vPoint = Vector3.zero;
		
		if(coordMapper != null && depthPos != Vector2.zero)
		{
			DepthSpacePoint depthPoint = new DepthSpacePoint();
			depthPoint.X = depthPos.x;
			depthPoint.Y = depthPos.y;
			
			DepthSpacePoint[] depthPoints = new DepthSpacePoint[1];
			depthPoints[0] = depthPoint;
			
			ushort[] depthVals = new ushort[1];
			depthVals[0] = depthVal;
			
			CameraSpacePoint[] camPoints = new CameraSpacePoint[1];
			coordMapper.MapDepthPointsToCameraSpace(depthPoints, depthVals, camPoints);
			
			CameraSpacePoint camPoint = camPoints[0];
			vPoint.x = camPoint.X;
			vPoint.y = camPoint.Y;
			vPoint.z = camPoint.Z;
		}
		
		return vPoint;
	}

	public Vector2 MapDepthPointToColorCoords (KinectInterop.SensorData sensorData, Vector2 depthPos, ushort depthVal)
	{
		Vector2 vPoint = Vector2.zero;
		
		if(coordMapper != null && depthPos != Vector2.zero)
		{
			DepthSpacePoint depthPoint = new DepthSpacePoint();
			depthPoint.X = depthPos.x;
			depthPoint.Y = depthPos.y;
			
			DepthSpacePoint[] depthPoints = new DepthSpacePoint[1];
			depthPoints[0] = depthPoint;
			
			ushort[] depthVals = new ushort[1];
			depthVals[0] = depthVal;
			
			ColorSpacePoint[] colPoints = new ColorSpacePoint[1];
			coordMapper.MapDepthPointsToColorSpace(depthPoints, depthVals, colPoints);
			
			ColorSpacePoint colPoint = colPoints[0];
			vPoint.x = colPoint.X;
			vPoint.y = colPoint.Y;
		}
		
		return vPoint;
	}

	public bool MapDepthFrameToColorCoords (KinectInterop.SensorData sensorData, ref Vector2[] vColorCoords)
	{
		if(coordMapper != null && sensorData.colorImage != null && sensorData.depthImage != null)
		{
			var pDepthData = GCHandle.Alloc(sensorData.depthImage, GCHandleType.Pinned);
			var pColorCoordinatesData = GCHandle.Alloc(vColorCoords, GCHandleType.Pinned);
			
			coordMapper.MapDepthFrameToColorSpaceUsingIntPtr(
				pDepthData.AddrOfPinnedObject(), 
				sensorData.depthImage.Length * sizeof(ushort),
				pColorCoordinatesData.AddrOfPinnedObject(), 
				(uint)vColorCoords.Length);
			
			pColorCoordinatesData.Free();
			pDepthData.Free();
			
			return true;
		}
		
		return false;
	}

	// returns the index of the given joint in joint's array or -1 if joint is not applicable
	public int GetJointIndex(KinectInterop.JointType joint)
	{
		return (int)joint;
	}
	
	// returns the joint at given index
	public KinectInterop.JointType GetJointAtIndex(int index)
	{
		return (KinectInterop.JointType)(index);
	}
	
	// returns the parent joint of the given joint
	public KinectInterop.JointType GetParentJoint(KinectInterop.JointType joint)
	{
		switch(joint)
		{
			case KinectInterop.JointType.SpineBase:
				return KinectInterop.JointType.SpineBase;
				
			case KinectInterop.JointType.Neck:
				return KinectInterop.JointType.SpineShoulder;
				
			case KinectInterop.JointType.SpineShoulder:
				return KinectInterop.JointType.SpineMid;
				
			case KinectInterop.JointType.ShoulderLeft:
			case KinectInterop.JointType.ShoulderRight:
				return KinectInterop.JointType.SpineShoulder;
				
			case KinectInterop.JointType.HipLeft:
			case KinectInterop.JointType.HipRight:
				return KinectInterop.JointType.SpineBase;
				
			case KinectInterop.JointType.HandTipLeft:
				return KinectInterop.JointType.HandLeft;
				
			case KinectInterop.JointType.ThumbLeft:
				return KinectInterop.JointType.WristLeft;
			
			case KinectInterop.JointType.HandTipRight:
				return KinectInterop.JointType.HandRight;

			case KinectInterop.JointType.ThumbRight:
				return KinectInterop.JointType.WristRight;
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
				return KinectInterop.JointType.SpineShoulder;
			case KinectInterop.JointType.SpineShoulder:
				return KinectInterop.JointType.Neck;
			case KinectInterop.JointType.Neck:
				return KinectInterop.JointType.Head;
				
			case KinectInterop.JointType.ShoulderLeft:
				return KinectInterop.JointType.ElbowLeft;
			case KinectInterop.JointType.ElbowLeft:
				return KinectInterop.JointType.WristLeft;
			case KinectInterop.JointType.WristLeft:
				return KinectInterop.JointType.HandLeft;
			case KinectInterop.JointType.HandLeft:
				return KinectInterop.JointType.HandTipLeft;
				
			case KinectInterop.JointType.ShoulderRight:
				return KinectInterop.JointType.ElbowRight;
			case KinectInterop.JointType.ElbowRight:
				return KinectInterop.JointType.WristRight;
			case KinectInterop.JointType.WristRight:
				return KinectInterop.JointType.HandRight;
			case KinectInterop.JointType.HandRight:
				return KinectInterop.JointType.HandTipRight;
				
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
		
		return joint;  // in case of end joint - Head, HandTipLeft, HandTipRight, FootLeft, FootRight
	}
	
}
