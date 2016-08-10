using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Filter to correct the joint locations and joint orientations to constraint to the range of viable human motion.
/// </summary>
//using Windows.Kinect;


public class BoneOrientationsConstraint
{
	public enum ConstraintAxis { X = 0, Y = 1, Z = 2 }
	
    // The Joint Constraints.  
    private readonly List<BoneOrientationConstraint> jointConstraints = new List<BoneOrientationConstraint>();

	//private GUIText debugText;
	

    /// Initializes a new instance of the BoneOrientationConstraints class.
    public BoneOrientationsConstraint()
    {
    }

	public void SetDebugText(GUIText debugText)
	{
		//this.debugText = debugText;
	}
	
	// find the bone constraint structure for given joint
	// returns the structure index in the list, or -1 if the bone structure is not found
	private int FindBoneOrientationConstraint(int thisJoint)
	{
		for(int i = 0; i < jointConstraints.Count; i++)
		{
			if(jointConstraints[i].thisJoint == thisJoint)
				return i;
		}
		
		// not found
		return -1;
	}

    // AddJointConstraint - Adds a joint constraint to the system.  
    public void AddBoneOrientationConstraint(int thisJoint, ConstraintAxis axis, float angleMin, float angleMax)
    {
		int index = FindBoneOrientationConstraint(thisJoint);
		
		BoneOrientationConstraint jc = index >= 0 ? jointConstraints[index] : new BoneOrientationConstraint(thisJoint);
		
		if(index < 0)
		{
			index = jointConstraints.Count;
			jointConstraints.Add(jc);
		}
		
		AxisOrientationConstraint constraint = new AxisOrientationConstraint(axis, angleMin, angleMax);
		jc.axisConstrainrs.Add(constraint);
		
		jointConstraints[index] = jc;
     }

    // AddDefaultConstraints - Adds a set of default joint constraints for normal human poses.  
    // This is a reasonable set of constraints for plausible human bio-mechanics.
    public void AddDefaultConstraints()
    {
        // Spine
		AddBoneOrientationConstraint((int)KinectInterop.JointType.SpineMid, ConstraintAxis.X, -5f, 5f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.SpineMid, ConstraintAxis.Y, -5f, 5f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.SpineMid, ConstraintAxis.Z, -5f, 5f);

		// SpineShoulder
		AddBoneOrientationConstraint((int)KinectInterop.JointType.SpineShoulder, ConstraintAxis.X, -10f, 20f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.SpineShoulder, ConstraintAxis.Y, -60f, 60f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.SpineShoulder, ConstraintAxis.Z, -20f, 20f);
		
		// Neck
		AddBoneOrientationConstraint((int)KinectInterop.JointType.Neck, ConstraintAxis.X, -10f, 30f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.Neck, ConstraintAxis.Y, -10f, 10f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.Neck, ConstraintAxis.Z, -30f, 30f);

		// ShoulderLeft, ShoulderRight
		AddBoneOrientationConstraint((int)KinectInterop.JointType.ShoulderLeft, ConstraintAxis.X, -30f, 30f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.ShoulderLeft, ConstraintAxis.Y, -50f, 90f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.ShoulderLeft, ConstraintAxis.Z, -110f, 90f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.ShoulderRight, ConstraintAxis.X, -30f, 30f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.ShoulderRight, ConstraintAxis.Y, -90f, 50f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.ShoulderRight, ConstraintAxis.Z, -110f, 90f);
		
		// ElbowLeft, ElbowRight
		AddBoneOrientationConstraint((int)KinectInterop.JointType.ElbowLeft, ConstraintAxis.X, -90f, 90f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.ElbowLeft, ConstraintAxis.Y, -90f, 90f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.ElbowLeft, ConstraintAxis.Z, -10f, 180f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.ElbowRight, ConstraintAxis.X, -90f, 90f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.ElbowRight, ConstraintAxis.Y, -90f, 90f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.ElbowRight, ConstraintAxis.Z, -10f, 180f);
		
		// WristLeft, WristRight
		AddBoneOrientationConstraint((int)KinectInterop.JointType.WristLeft, ConstraintAxis.X, -90f, 90f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.WristLeft, ConstraintAxis.Y, -60f, 60f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.WristLeft, ConstraintAxis.Z, -60f, 60f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.WristRight, ConstraintAxis.X, -90f, 90f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.WristRight, ConstraintAxis.Y, -60f, 60f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.WristRight, ConstraintAxis.Z, -90f, 90f);

		// HandLeft, HandRight
		AddBoneOrientationConstraint((int)KinectInterop.JointType.HandLeft, ConstraintAxis.X, -10f, 10f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.HandLeft, ConstraintAxis.Y, -30f, 30f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.HandLeft, ConstraintAxis.Z, -30f, 30f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.HandRight, ConstraintAxis.X, -10f, 10f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.HandRight, ConstraintAxis.Y, -30f, 30f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.HandRight, ConstraintAxis.Z, -30f, 30f);
		
		// HipLeft, HipRight
		AddBoneOrientationConstraint((int)KinectInterop.JointType.HipLeft, ConstraintAxis.X, -100f, 60f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.HipLeft, ConstraintAxis.Y, -90f, 90f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.HipLeft, ConstraintAxis.Z, -100f, 30f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.HipRight, ConstraintAxis.X, -100f, 60f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.HipRight, ConstraintAxis.Y, -90f, 90f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.HipRight, ConstraintAxis.Z, -30f, 100f);
		
		// KneeLeft, KneeRight
		AddBoneOrientationConstraint((int)KinectInterop.JointType.KneeLeft, ConstraintAxis.X, 0f, 100f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.KneeLeft, ConstraintAxis.Y, -10f, 10f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.KneeLeft, ConstraintAxis.Z, -45f, 45f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.KneeRight, ConstraintAxis.X, 0f, 100f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.KneeRight, ConstraintAxis.Y, -10f, 10f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.KneeRight, ConstraintAxis.Z, -45f, 45f);
		
		// FootLeft, FootRight
		AddBoneOrientationConstraint((int)KinectInterop.JointType.FootLeft, ConstraintAxis.X, -20f, 20f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.FootLeft, ConstraintAxis.Y, -20f, 20f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.FootLeft, ConstraintAxis.Z, -30f, 30f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.FootRight, ConstraintAxis.X, -20f, 20f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.FootRight, ConstraintAxis.Y, -20f, 20f);
		AddBoneOrientationConstraint((int)KinectInterop.JointType.FootRight, ConstraintAxis.Z, -30f, 30f);
	}

    // ApplyBoneOrientationConstraints and constrain rotations.
	public void Constrain(ref KinectInterop.BodyData bodyData)
    {
		KinectManager manager = KinectManager.Instance;

        for (int i = 0; i < this.jointConstraints.Count; i++)
        {
            BoneOrientationConstraint jc = this.jointConstraints[i];

			if (jc.thisJoint == (int)KinectInterop.JointType.SpineBase || bodyData.joint[jc.thisJoint].normalRotation == Quaternion.identity)
                continue;
			if (bodyData.joint[jc.thisJoint].trackingState == KinectInterop.TrackingState.NotTracked)
				continue;

			int prevJoint = (int)manager.GetParentJoint((KinectInterop.JointType)jc.thisJoint);
			if (bodyData.joint[prevJoint].trackingState == KinectInterop.TrackingState.NotTracked) 
				continue;

			Quaternion rotJointN = bodyData.joint[jc.thisJoint].normalRotation;
			Quaternion rotParentN = bodyData.joint[prevJoint].normalRotation;

			Quaternion rotLocalN = Quaternion.Inverse(rotParentN) * rotJointN;
			Vector3 eulerAnglesN = rotLocalN.eulerAngles;
			
			Quaternion rotJointM = bodyData.joint[jc.thisJoint].mirroredRotation;
			Quaternion rotParentM = bodyData.joint[prevJoint].mirroredRotation;
			
			Quaternion rotLocalM = Quaternion.Inverse(rotParentM) * rotJointM;
			Vector3 eulerAnglesM = rotLocalM.eulerAngles;
			
			bool isConstrained = false;

			for(int a = 0; a < jc.axisConstrainrs.Count; a++)
			{
				AxisOrientationConstraint ac = jc.axisConstrainrs[a];
				
				Quaternion axisRotation = Quaternion.AngleAxis(eulerAnglesN[ac.axis], ac.rotateAround);
				float angleFromMin = Quaternion.Angle(axisRotation, ac.minQuaternion);
				float angleFromMax = Quaternion.Angle(axisRotation, ac.maxQuaternion);
				 
				if(!(angleFromMin <= ac.angleRange && angleFromMax <= ac.angleRange))
				{
					// correct the axis that has fallen out of range.
					if(angleFromMin > angleFromMax)
					{
						eulerAnglesN[ac.axis] = ac.angleMax;
					}
					else
					{
						eulerAnglesN[ac.axis] = ac.angleMin;
					}

					// fix mirrored rotation as well
					if(ac.axis == 0)
					{
						eulerAnglesM[ac.axis] = eulerAnglesN[ac.axis];
					}
					else
					{
						eulerAnglesM[ac.axis] = -eulerAnglesN[ac.axis];
					}
					
					isConstrained = true;
				}
			}
			
			if(isConstrained)
			{
				rotLocalN = Quaternion.Euler(eulerAnglesN);
				rotJointN = rotParentN * rotLocalN;

				rotLocalM = Quaternion.Euler(eulerAnglesM);
				rotJointM = rotParentM * rotLocalM;
				
				// Put it back into the bone directions
				bodyData.joint[jc.thisJoint].normalRotation = rotJointN;
				bodyData.joint[jc.thisJoint].mirroredRotation = rotJointM;
//				dirJoint = constrainedRotation * dirParent;
//				bodyData.joint[jc.thisJoint].direction = dirJoint;
			}
			
        }
    }


    private struct BoneOrientationConstraint
    {
		public int thisJoint;
		public List<AxisOrientationConstraint> axisConstrainrs;
		
		
		public BoneOrientationConstraint(int thisJoint)
        {
			this.thisJoint = thisJoint;
			axisConstrainrs = new List<AxisOrientationConstraint>();
        }
    }
	
	
	private struct AxisOrientationConstraint
	{
		// the axis to rotate around
		public int axis;
		public Vector3 rotateAround;
				
		// min, max and range of allowed angle
		public float angleMin;
		public float angleMax;
		
		public Quaternion minQuaternion;
		public Quaternion maxQuaternion;
		public float angleRange;
				
		
		public AxisOrientationConstraint(ConstraintAxis axis, float angleMin, float angleMax)
		{
			// Set the axis that we will rotate around
			this.axis = (int)axis;
			
			switch(axis)
			{
				case ConstraintAxis.X:
					this.rotateAround = Vector3.right;
					break;
				 
				case ConstraintAxis.Y:
					this.rotateAround = Vector3.up;
					break;
				 
				case ConstraintAxis.Z:
					this.rotateAround = Vector3.forward;
					break;
			
				default:
					this.rotateAround = Vector3.zero;
					break;
			}
			
			// Set the min and max rotations in degrees
			this.angleMin = angleMin;
			this.angleMax = angleMax;
			
				 
			// Set the min and max rotations in quaternion space
			this.minQuaternion = Quaternion.AngleAxis(angleMin, this.rotateAround);
			this.maxQuaternion = Quaternion.AngleAxis(angleMax, this.rotateAround);
			this.angleRange = angleMax - angleMin;
		}
	}
	
}