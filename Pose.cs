using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Kinect;

namespace W7_AngleBasedMatching
{
    public class PoseAngle
    {
        public JointType StartJoint;
        public JointType EndJoint;
        public float Angle;
        public float Threshold;

        public Boolean matched = false; // class exercise 8 

        public PoseAngle(JointType sj, JointType ej, float a, float t)
        {
            StartJoint = sj;
            EndJoint = ej;
            Angle = a;
            Threshold = t;
        }
    }

    public class Pose
    {
        public string Title;
        public PoseAngle[] Angles; 
    }
}
