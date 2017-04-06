using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;

using System.Media;

namespace W7_AngleBasedMatching
{   //Sound files taken from http://freesound.org
    //Viking Ship from http://millionthvector.blogspot.hk/2014/02/new-free-spirites-top-down-ships.html
    //Stone sprite from http://vignette2.wikia.nocookie.net/herebemonsters/images/4/43/Thunderous-Rock-Sprite.png/revision/latest?cb=20140330101444

    //Core code from HongBo Fu Week 8's Lecture 
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Initialize ship location
        int shipX = 384;
        int shipY = 500;

        
        private KinectSensor sensor;

        private Skeleton[] skeletons = null;
        private JointType[] bones = { 
                                      // torso 
                                      JointType.Head, JointType.ShoulderCenter,
                                      JointType.ShoulderCenter, JointType.ShoulderLeft,
                                      JointType.ShoulderCenter, JointType.ShoulderRight,
                                      JointType.ShoulderCenter, JointType.Spine, 
                                      JointType.Spine, JointType.HipCenter,
                                      JointType.HipCenter, JointType.HipLeft, 
                                      JointType.HipCenter, JointType.HipRight,
                                      // left arm 
                                      JointType.ShoulderLeft, JointType.ElbowLeft,
                                      JointType.ElbowLeft, JointType.WristLeft,
                                      JointType.WristLeft, JointType.HandLeft,
                                      // right arm 
                                      JointType.ShoulderRight, JointType.ElbowRight,
                                      JointType.ElbowRight, JointType.WristRight,
                                      JointType.WristRight, JointType.HandRight,
                                      // left leg
                                      JointType.HipLeft, JointType.KneeLeft,
                                      JointType.KneeLeft, JointType.AnkleLeft,
                                      JointType.AnkleLeft, JointType.FootLeft,
                                      // right leg
                                      JointType.HipRight, JointType.KneeRight,
                                      JointType.KneeRight, JointType.AnkleRight,
                                      JointType.AnkleRight, JointType.FootRight,
                                    };

        private DrawingGroup drawingGroup; // Drawing group for skeleton rendering output
        private DrawingImage drawingImg; // Drawing image that we will display

        private BitmapImage rockImg;
        private BitmapImage shipImg;
        private BitmapImage shipLImg;
        private BitmapImage shipRImg;
        private BitmapImage waveImg;

        private bool shipMovingLeft = false;
        private bool shipMovingRight = false;

        private Wave[] Waves = new Wave[20];
        private Rock[] RockBlocks = new Rock[10];
        private Rect[] r_rocks = new Rect[10];
        private Rect[] r_waves = new Rect[20];
        private Rect r_ship = new Rect();

        private int Distance_Remaining = 900;
        private int Time_Elapsed = 0;

        private bool collided = false;
        private bool gameEnded = false;

        private int Time_restart = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PopulatePoseLibrary(); // new 

            if (KinectSensor.KinectSensors.Count == 0)
            {   
                MessageBox.Show("No Kinects detected", "Depth Sensor Basics");
                Application.Current.Shutdown();
            }
            else
            {
                sensor = KinectSensor.KinectSensors[0];
                if (sensor == null)
                {
                    MessageBox.Show("Kinect is not ready to use", "Depth Sensor Basics");
                    Application.Current.Shutdown();
                }
            }

            //initialize ship transform

            // skeleton stream 
            sensor.SkeletonStream.Enable();
            sensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(sensor_SkeletonFrameReady);
            skeletons = new Skeleton[sensor.SkeletonStream.FrameSkeletonArrayLength];

            // Create the drawing group we'll use for drawing
            drawingGroup = new DrawingGroup();
            // Create an image source that we can use in our image control
            drawingImg = new DrawingImage(drawingGroup);
            // Display the drawing using our image control
            skeletonImg.Source = drawingImg;
            // prevent drawing outside of our render area
            drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, 640, 480));



            for (int i = 0; i < 10; i++)
            {
                RockBlocks[i] = new Rock();
                r_rocks[i] = new Rect(RockBlocks[i].x, RockBlocks[i].y, 64, 64);

                //Initialize Rock position
                r_rocks[i].X = rand.Next((640 - 64));
                if (i > 0)
                {
                    for (int j = 0; j < i; j++)
                    {
                        while (!(r_rocks[j].X <= r_rocks[i].X - 64) && !((r_rocks[j].X) > (r_rocks[i].X + 64)))
                        {
                            r_rocks[j].X = rand.Next((640 - 64));

                        }
                    }
                }
            }

            for (int i = 0; i < 20; i++)
            {
                Waves[i] = new Wave();
                r_waves[i] = new Rect(Waves[i].x, Waves[i].y, 16, 64);

                //Initialize Rock position
                r_waves[i].X = rand.Next((640 - 16));
                if (i > 0)
                {
                    for (int j = 0; j < i; j++)
                    {
                        while (!(r_waves[j].X <= r_waves[i].X - 16) && !((r_waves[j].X) > (r_waves[i].X + 16)))
                        {
                            r_waves[j].X = rand.Next((640 - 16));

                        }
                    }
                }
            }

                r_ship = new Rect(320, 360, 30, 100);
            LoadImages();
            // start the kinect
            sensor.Start();
        }
        private void LoadImages()
        {
            rockImg = new BitmapImage(new Uri("Image/Rock.png", UriKind.Relative));
            shipImg = new BitmapImage(new Uri("Image/vikingship.png", UriKind.Relative));
            shipLImg = new BitmapImage(new Uri("Image/vikingshipl.png", UriKind.Relative));
            shipRImg = new BitmapImage(new Uri("Image/vikingshipr.png", UriKind.Relative));
            waveImg = new BitmapImage(new Uri("Image/wave.png", UriKind.Relative));
        }

        private void sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (DrawingContext dc = this.drawingGroup.Open()) // clear the drawing
            {

                // draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, 640, 480));
                DrawWaves(dc);
                DrawRocks(dc);
                DrawShip(dc);
                statusTxt.Text = "No Skeleton Detected";
                Distance.Text = "Distance remaining: " + Distance_Remaining + " m";
                if(collided == false && Distance_Remaining > 0) //check distanec remaining 
                Distance_Remaining -= 1;

                Time_Elapsed += 1;
                Time.Text = "Time: " + Time_Elapsed / 30 + " s";


                if (Distance_Remaining == 0)
                {
                    if (gameEnded == false)
                    {
                        soundPlayer.Play();
                    }
                    Distance.Text = "You WIN!!";
                    gameEnded = true;
                    Time_restart +=1;
                    Time.Text = "Game restart in " + (5 - Time_restart / 30) + " s";
                    if(Time_restart >= 150)
                    {
                        Distance_Remaining = 900;
                        Time_restart = 0;
                        Time_Elapsed = 0;
                        gameEnded = false;
                    
                    }

                }

                using (SkeletonFrame frame = e.OpenSkeletonFrame())
                {
                    if (frame != null)
                    {
                        frame.CopySkeletonDataTo(skeletons);

                        // Add your code below 

                        // Find the closest skeleton 
                        Skeleton skeleton = GetPrimarySkeleton(skeletons);

                        if (skeleton == null) return;

                        statusTxt.Text = "Skeleton Detected";
                        DrawSkeleton(skeleton, dc, Brushes.GreenYellow, new Pen(Brushes.DarkGreen, 6));

                        PoseMatching(skeleton);
                    }
                }
            }
        }

        private Skeleton GetPrimarySkeleton(Skeleton[] skeletons)
        {
            Skeleton skeleton = null;

            if (skeletons != null)
            {
                //Find the closest skeleton       
                for (int i = 0; i < skeletons.Length; i++)
                {
                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        if (skeleton == null) skeleton = skeletons[i];
                        else if (skeleton.Position.Z > skeletons[i].Position.Z)
                            skeleton = skeletons[i];
                    }
                }
            }

            return skeleton;
        }
        private void DrawSkeleton(Skeleton skeleton, DrawingContext dc, Brush jointBrush, Pen bonePen)
        {
            for (int i = 0; i < bones.Length; i += 2)
                DrawBone(skeleton, dc, bones[i], bones[i + 1], bonePen);

            // Render joints
            foreach (Joint j in skeleton.Joints)
            {
                if (j.TrackingState == JointTrackingState.NotTracked) continue;

                dc.DrawEllipse(jointBrush, null, SkeletonPointToScreenPoint(j.Position), 5, 5);
            }
        }

        private void DrawWaves(DrawingContext dc)
        {
            for (int i = 0; i < 5; i++)
            {
                //first 5 waves
                r_waves[i].Y += 8;
                if (r_waves[i].Y >= 640)
                {
                    r_waves[i].X = rand.Next(640 - 16);
                    r_waves[i].Y = -64;
                }
            }
            if (Time_Elapsed > 30)
            {
                for (int i = 5; i < 10; i++) 
                {
                    r_waves[i].Y += 8;
                    if (r_waves[i].Y >= 640)
                    {
                        r_waves[i].X = rand.Next(640 - 16);
                        r_waves[i].Y = -64;
                    }
                                  }
            }

            if (Time_Elapsed > 60)
            {
                for (int i = 10; i < 15; i++)
                {
                    r_waves[i].Y += 8;
                    if (r_waves[i].Y >= 640)
                    {
                        r_waves[i].X = rand.Next(640 - 16);
                        r_waves[i].Y = -64;
                    }
                }
            }
            if (Time_Elapsed > 90)
            {
                for (int i = 15; i < 20; i++)
                {
                    r_waves[i].Y += 8;
                    if (r_waves[i].Y >= 640)
                    {
                        r_waves[i].X = rand.Next(640 - 16);
                        r_waves[i].Y = -64;
                    }
                }
            }
            for (int i = 0; i < r_waves.Length; i++) {
                dc.DrawImage(waveImg, r_waves[i]);
            }
        }

        private void DrawRocks(DrawingContext dc)
        {   //Check if rocks overlap another rock

            for (int i = 0; i < r_rocks.Length; i++)
            {//aabb code from https://developer.mozilla.org/en-US/docs/Games/Techniques/2D_collision_detection
                if (!collided && (r_ship.X < r_rocks[i].X + r_rocks[i].Width && r_ship.X + r_ship.Width > r_rocks[i].X && r_ship.Y < r_rocks[i].Y + r_rocks[i].Height && r_ship.Y + r_ship.Height > r_rocks[i].Y))
                {
                    r_rocks[i].Y += 0;
                    collided = true;      
                }

                else if(collided == false && gameEnded == false)
                {
                    r_rocks[i].Y += 4;
                }
                else if (gameEnded == true)
                {
                    r_rocks[i].Y = 0;
                }

                if (r_rocks[i].Y > 600 - r_rocks[i].Height)
                {
                    r_rocks[i].Y = 0;
                    r_rocks[i].X = rand.Next((640 - 64));
                }
                dc.DrawImage(rockImg, r_rocks[i]);
            }   
        }
        private void DrawShip(DrawingContext dc)
        {
            if (shipMovingLeft == true)
                dc.DrawImage(shipLImg, r_ship);
            else if (shipMovingRight == true)
                dc.DrawImage(shipRImg, r_ship);
            else
                dc.DrawImage(shipImg, r_ship);  
        }
        private void DrawBone(Skeleton skeleton, DrawingContext dc, JointType jointType0, JointType jointType1, Pen bonePen)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked) return;

            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred) return;

            //dc.DrawLine(new Pen(Brushes.Red, 5),

            // class exercise 8 
            // check if this bone is used for pose matching or not
            int angleIndex = -1;
            for (int i = 0; i < poseLib[0].Angles.Length; i++)
            {
                if ((poseLib[0].Angles[i].StartJoint == jointType0 &&
                    poseLib[0].Angles[i].EndJoint == jointType1) ||
                    (poseLib[0].Angles[i].StartJoint == jointType1 &&
                    poseLib[0].Angles[i].EndJoint == jointType0))
                {
                    angleIndex = i;
                    break; // found 
                }
            }

            for (int i = 0; i < poseLib[1].Angles.Length; i++)
            {
                if ((poseLib[1].Angles[i].StartJoint == jointType0 &&
                    poseLib[1].Angles[i].EndJoint == jointType1) ||
                    (poseLib[1].Angles[i].StartJoint == jointType1 &&
                    poseLib[1].Angles[i].EndJoint == jointType0))
                {
                    angleIndex = i;
                    break; // found 
                }
            }


            if (angleIndex != -1) // used for pose matching 
            {
                if (poseLib[0].Angles[angleIndex].matched) // matched   
                    dc.DrawLine(new Pen(Brushes.Yellow, 10),
                    SkeletonPointToScreenPoint(joint0.Position),
                    SkeletonPointToScreenPoint(joint1.Position));
                else if (poseLib[1].Angles[angleIndex].matched) // matched 
                    dc.DrawLine(new Pen(Brushes.Blue, 10),
                    SkeletonPointToScreenPoint(joint0.Position),
                    SkeletonPointToScreenPoint(joint1.Position));
                else // not matched
                    dc.DrawLine(new Pen(Brushes.Red, 10),
                    SkeletonPointToScreenPoint(joint0.Position),
                    SkeletonPointToScreenPoint(joint1.Position));
            }

            else
                dc.DrawLine(bonePen,
                    SkeletonPointToScreenPoint(joint0.Position),
                    SkeletonPointToScreenPoint(joint1.Position));
        }

        private Point SkeletonPointToScreenPoint(SkeletonPoint sp)
        {
            ColorImagePoint pt = sensor.CoordinateMapper.MapSkeletonPointToColorPoint(
                sp, ColorImageFormat.RgbResolution640x480Fps30);
            return new Point(pt.X, pt.Y);
        }

        // ------------------------------------------------------------
        private float GetAngle(Skeleton s, JointType js, JointType je)
        {
            Point sp = SkeletonPointToScreenPoint(s.Joints[js].Position);
            Point ep = SkeletonPointToScreenPoint(s.Joints[je].Position);

            float angle = (float)(
                Math.Atan2(ep.Y - sp.Y, ep.X - sp.X) * 180 / Math.PI);

            angle = (angle + 360) % 360; // --> [0, 360)

            return angle;
        }

        private void PoseMatching(Skeleton skeleton)
        {
            //statusTxt.Text = String.Format("Angle: {0:0.0}",
            //            GetAngle(skeleton,
            //        JointType.ElbowRight, JointType.WristRight));

            //if (isMatched(skeleton, targetPose))
            //    statusTxt.Text = targetPose.Title + ": Matched";
            //else
            //    statusTxt.Text = targetPose.Title + ": Not Matched";              

            // class exercise 7
            if (isMatched(skeleton, poseLib[0]))
            {
                statusTxt.Text = poseLib[0].Title + ": Matched";
                //                targetPoseIndex = rand.Next(poseLib.Length); // next random pose 
                rowingSound.PlayLooping();
                MoveShip(0);
                shipMovingLeft = true;
                //                
            }
            else
            {
                statusTxt.Text = poseLib[0].Title + ": Not Matched";
                shipMovingLeft = false;
                //                soundPlaying = false;
            }
            if (isMatched(skeleton, poseLib[1]))
            {
                statusTxt_Copy.Text = poseLib[1].Title + ": Matched";
                //                targetPoseIndex = rand.Next(poseLib.Length); // next random pose 

                rowingSound.PlayLooping();
                MoveShip(1);
                shipMovingRight = true;
                //                soundPlaying = true;
            }
            else
            {
                statusTxt_Copy.Text = poseLib[1].Title + ": Not Matched";
                shipMovingRight = false;
                //               soundPlaying = false;
            }
        }

        Boolean isMatched(Skeleton skeleton, Pose pose)
        {
            // class exercise 4
            //if (Math.Abs(pose.Angles[0].Angle -
            //    GetAngle(skeleton, pose.Angles[0].StartJoint, pose.Angles[0].EndJoint)) < pose.Angles[0].Threshold)
            //    return true;
            //else
            //    return false;

            // class exercise 6
            //for (int i = 0; i < pose.Angles.Length; i++)
            //{
            //    // as long as one bone's angle is not matched we consider the pose is not matched
            //    if (AngularDifference(pose.Angles[i].Angle, GetAngle(skeleton, pose.Angles[i].StartJoint, pose.Angles[i].EndJoint)) > pose.Angles[i].Threshold)
            //        return false; 
            //}
            //return true; 

            Boolean pose_matched = true;

            for (int i = 0; i < pose.Angles.Length; i++)
            {
                // as long as one bone's angle is not matched we consider the pose is not matched
                if (AngularDifference(pose.Angles[i].Angle, GetAngle(skeleton, pose.Angles[i].StartJoint, pose.Angles[i].EndJoint)) > pose.Angles[i].Threshold)
                {
                    pose.Angles[i].matched = false; // class exercise 8 
                    pose_matched = false;
                }
                else
                    pose.Angles[i].matched = true; // class exercise 8 
            }
            return pose_matched;
        }

        private float AngularDifference(float a1, float a2)
        {
            float abs_diff = Math.Abs(a1 - a2);
            return Math.Min(abs_diff, 360 - abs_diff);
        }

        private Pose targetPose = new Pose();

        // class exercise 7
        private Pose[] poseLib;
        private Random rand = new Random();
        private int targetPoseIndex = 0;
        private float matchingThreshold = 20;

        private SoundPlayer soundPlayer = new SoundPlayer("ding.wav");
        private SoundPlayer rowingSound = new SoundPlayer("rowing.wav");
        //        private Boolean soundPlaying = false;

        private void PopulatePoseLibrary() // initialized in Window_Loaded
        {
            // initialize the targetPose below 
            targetPose.Title = "T-pose";

            // class exercise 5: 
            PoseAngle[] angles = new PoseAngle[4];
            angles[0] = new PoseAngle(JointType.ElbowRight, JointType.WristRight, 0, 20);
            angles[1] = new PoseAngle(JointType.ShoulderRight, JointType.ElbowRight, 0, 20);
            angles[2] = new PoseAngle(JointType.ElbowLeft, JointType.WristLeft, 180, 20);
            angles[3] = new PoseAngle(JointType.ShoulderLeft, JointType.ElbowLeft, 180, 20);

            targetPose.Angles = angles;

            // class exercise 7
            poseLib = new Pose[2];

            poseLib[0] = new Pose();
            poseLib[0].Title = "SteerLeft";
            angles = new PoseAngle[4];
            angles[0] = new PoseAngle(JointType.ShoulderLeft, JointType.ElbowLeft, 45, matchingThreshold);
            angles[1] = new PoseAngle(JointType.ElbowLeft, JointType.WristLeft, 45, matchingThreshold);
            angles[2] = new PoseAngle(JointType.ShoulderRight, JointType.ElbowRight, 45, matchingThreshold);
            angles[3] = new PoseAngle(JointType.ElbowRight, JointType.WristRight, 45, matchingThreshold);
            poseLib[0].Angles = angles;

            poseLib[1] = new Pose();
            poseLib[1].Title = "SteerRight";
            angles = new PoseAngle[4];
            angles[0] = new PoseAngle(JointType.ShoulderLeft, JointType.ElbowLeft, 135, matchingThreshold);
            angles[1] = new PoseAngle(JointType.ElbowLeft, JointType.WristLeft, 135, matchingThreshold);
            angles[2] = new PoseAngle(JointType.ShoulderRight, JointType.ElbowRight, 135, matchingThreshold);
            angles[3] = new PoseAngle(JointType.ElbowRight, JointType.WristRight, 135, matchingThreshold);
            poseLib[1].Angles = angles;

            //Pose 1 - Both Hands Up
            /*poseLib[0] = new Pose();
            poseLib[0].Title = "Arms Up";
            angles = new PoseAngle[4];
            angles[0] = new PoseAngle(JointType.ShoulderLeft, JointType.ElbowLeft, 180, matchingThreshold);
            angles[1] = new PoseAngle(JointType.ElbowLeft, JointType.WristLeft, 270, matchingThreshold);
            angles[2] = new PoseAngle(JointType.ShoulderRight, JointType.ElbowRight, 0, matchingThreshold);
            angles[3] = new PoseAngle(JointType.ElbowRight, JointType.WristRight, 270, matchingThreshold);
            poseLib[0].Angles = angles;

            //Pose 2 - Both Hands Down
            poseLib[1] = new Pose();
            poseLib[1].Title = "Arms Down";
            angles = new PoseAngle[4];
            angles[0] = new PoseAngle(JointType.ShoulderLeft, JointType.ElbowLeft, 180, matchingThreshold);
            angles[1] = new PoseAngle(JointType.ElbowLeft, JointType.WristLeft, 90, matchingThreshold);
            angles[2] = new PoseAngle(JointType.ShoulderRight, JointType.ElbowRight, 0, matchingThreshold);
            angles[3] = new PoseAngle(JointType.ElbowRight, JointType.WristRight, 90, matchingThreshold);
            poseLib[1].Angles = angles;

            //Pose 3 - Left Up and Right Down
            poseLib[2] = new Pose();
            poseLib[2].Title = "Left Up and Right Down";
            angles = new PoseAngle[4];
            angles[0] = new PoseAngle(JointType.ShoulderLeft, JointType.ElbowLeft, 180, matchingThreshold);
            angles[1] = new PoseAngle(JointType.ElbowLeft, JointType.WristLeft, 270, matchingThreshold);
            angles[2] = new PoseAngle(JointType.ShoulderRight, JointType.ElbowRight, 0, matchingThreshold);
            angles[3] = new PoseAngle(JointType.ElbowRight, JointType.WristRight, 90, matchingThreshold);
            poseLib[2].Angles = angles;

            //Pose 4 - Right Up and Left Down
            poseLib[3] = new Pose();
            poseLib[3].Title = "Right Up and Left Down";
            angles = new PoseAngle[4];
            angles[0] = new PoseAngle(JointType.ShoulderLeft, JointType.ElbowLeft, 180, matchingThreshold);
            angles[1] = new PoseAngle(JointType.ElbowLeft, JointType.WristLeft, 90, matchingThreshold);
            angles[2] = new PoseAngle(JointType.ShoulderRight, JointType.ElbowRight, 0, matchingThreshold);
            angles[3] = new PoseAngle(JointType.ElbowRight, JointType.WristRight, 270, matchingThreshold);
            poseLib[3].Angles = angles;
        */
            //            targetPoseIndex = rand.Next(poseLib.Length);

        }

        void MoveShip(int direction) //direction : 0 = left 1 = right
        {
            int speed = 40;
            if (gameEnded == false)
            {
                if (direction == 0 && r_ship.X >= 40)
                {
                    r_ship.X -= speed;


                }
                else if (direction == 1 && r_ship.X <= 560)
                {
                    r_ship.X += speed;
                }

                if (collided == true)
                {
                    bool noCollision = false;
                    for (int i = 0; i < r_rocks.Length; i++)
                    {
                        if ((r_ship.X < r_rocks[i].X + r_rocks[i].Width && r_ship.X + r_ship.Width > r_rocks[i].X && r_ship.Y < r_rocks[i].Y + r_rocks[i].Height && r_ship.Y + r_ship.Height > r_rocks[i].Y))
                            break;
                        noCollision = true;
                    }
                    if (noCollision == true)
                        collided = false;
                }
            }
        }
    }
}
