using Microsoft.Kinect;
using Microsoft.Kinect.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectProjectWork
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //---------------------------------------------------------------//
        //---------------------------------------------------------------//

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;


        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        //---------------------------------------------------------------//
        //---------------------------------------------------------------//

        /// <summary>
        /// Used for closing the application after quit button is activated
        /// </summary>
        private int counter = 10;

        // Dispatcher t for other buttons and t2 for quit button and t3 for progressBar of other buttons, t4 for progressBar of quit button. dt for current DateTime
        System.Windows.Threading.DispatcherTimer t, t2, t3, t4, t5, t6;
        DateTime dt;

        /// <summary>
        /// if false, normal operation. if true, quit operation.
        /// </summary>
        private bool quitValue = false;


        /// <summary>
        /// Declaring a new cursor
        /// </summary>
        //private Cursor newCursor;

        /// <summary>
        /// The index of the current image.
        /// </summary>
        private int indexField = 1;

        //Check the button over which the mouse is.
        Border v = new Border();
        Button btn = new Button();
        private string s;
        ProgressBar pBar = new ProgressBar();

        /// <summary>
        /// Event implementing INotifyPropertyChanged interface.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The paths of the picture files.
        /// </summary>
        private string[] picturePaths = CreatePicturePaths("");

        private string[] backPicturePaths = CreateBackgroundPicturePaths();


        public MainWindow()
        {
            this.Index = 0;
            this.PreviousPicture = this.LoadPicture(this.Index - 1);
            this.Picture = this.LoadPicture(this.Index);
            this.NextPicture = this.LoadPicture(this.Index + 1);
            this.backgroundPic = this.LoadBackgroundPicture(this.backIndex);



            //For displaying a folders images after some seconds
            t = new System.Windows.Threading.DispatcherTimer();
            t2 = new System.Windows.Threading.DispatcherTimer();
            t3 = new System.Windows.Threading.DispatcherTimer();
            t4 = new System.Windows.Threading.DispatcherTimer();
            t5 = new System.Windows.Threading.DispatcherTimer();
            t6 = new System.Windows.Threading.DispatcherTimer();
            t.Tick += new EventHandler(t_Tick);
            t2.Tick += new EventHandler(t2_tick);
            t3.Tick += new EventHandler(t3_tick);
            t4.Tick += new EventHandler(t4_tick);
            t5.Tick += new EventHandler(t5_tick);
            t6.Tick += new EventHandler(t6_tick);

            InitializeComponent();
            KinectRegion.SetKinectRegion(this, kinectRegion);

            App app = ((App)Application.Current);
            app.KinectRegion = kinectRegion;

            // Use the default sensor
            this.kinectRegion.KinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectRegion.KinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectRegion.KinectSensor.DepthFrameSource.FrameDescription;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectRegion.KinectSensor.BodyFrameSource.OpenReader();

            // open the sensor
            this.kinectRegion.KinectSensor.Open();

            scrollViewer1.Visibility = System.Windows.Visibility.Hidden;
            scrollViewer2.Visibility = System.Windows.Visibility.Hidden;

        }


        //------------------------------------------------------------//
        //------------------------------------------------------------//

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine("Hey!");
            if (this.bodyFrameReader != null)
            {
                //Debug.WriteLine("Hey!");
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectRegion.KinectSensor != null)
            {
                this.kinectRegion.KinectSensor.Close();
                this.kinectRegion.KinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                    foreach (Body body in this.bodies)
                    {
                        if (body.IsTracked)
                        {
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            bool isLeftSwipe = leftSwipe(body, JointType.HandLeft);
                            bool isRightSwipe = rightSwipe(body, JointType.HandRight);


                        if (isLeftSwipe) {
                            Debug.WriteLine("Left swipe performed!");
                            Index--;
                            // Setup corresponding picture if pictures are available.
                            this.NextPicture = this.Picture;
                            this.Picture = this.PreviousPicture;
                            this.PreviousPicture = LoadPicture(Index - 1);

                            // Notify world of change to Index and Picture.
                            if (this.PropertyChanged != null)
                            {
                                this.PropertyChanged(this, new PropertyChangedEventArgs("PreviousPicture"));
                                this.PropertyChanged(this, new PropertyChangedEventArgs("Picture"));
                                this.PropertyChanged(this, new PropertyChangedEventArgs("NextPicture"));
                            }
                        }
                            
                        if (isRightSwipe) {
                            Debug.WriteLine("Right swipe performed!");
                            Index++;

                            // Setup corresponding picture if pictures are available.
                            this.PreviousPicture = this.Picture;
                            this.Picture = this.NextPicture;
                            this.NextPicture = LoadPicture(Index + 1);

                            // Notify world of change to Index and Picture.
                            if (this.PropertyChanged != null)
                            {
                                this.PropertyChanged(this, new PropertyChangedEventArgs("PreviousPicture"));
                                this.PropertyChanged(this, new PropertyChangedEventArgs("Picture"));
                                this.PropertyChanged(this, new PropertyChangedEventArgs("NextPicture"));
                            }
                        }
                            
                    }
                    }

              }
        }

        public static int rightBoolCounter = 0;

        private bool rightSwipe(Body body, JointType jointType)
        {
            var shoulderSpine = body.Joints[JointType.SpineShoulder];
            var hand = body.Joints[jointType];
            var rightShoulder = body.Joints[JointType.ShoulderRight];

            bool isDetected = hand.Position.Y > shoulderSpine.Position.Y && (hand.Position.X - rightShoulder.Position.X) > 0.3;

            if (isDetected == true)
            {
                rightBoolCounter += 1;
                if (rightBoolCounter > 1)
                {
                    //Debug.WriteLine("Hola!" + "    " + rightBoolCounter + "\n");
                    return false;
                    //Debug.WriteLine("Hand Try 1!   " + (hand.Position.Y < head.Position.Y) + "\n");
                    //Debug.WriteLine("Hand Try 2!   " + (hand.Position.X > leftShoulder.Position.X) + "\n");
                }
                else
                    return true;
            }
            else {
                if (shoulderSpine.Position.Y > hand.Position.Y || shoulderSpine.Position.X > hand.Position.X)
                {
                    rightBoolCounter = 0;
                    //Debug.WriteLine("Entering...boolcounter=0" + "    " + (hand.Position.Y - shoulderSpine.Position.Y) + "\n");
                }
                return false;
                //Debug.WriteLine((hand.Position.X > shoulderSpine.Position.X) + "\n");
            }
        }

        public static int leftBoolCounter = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <param name="jointType"></param>
        /// <returns></returns>
        private bool leftSwipe(Body body, JointType jointType)
        {
            var shoulderSpine = body.Joints[JointType.SpineShoulder];
            var hand = body.Joints[jointType];
            var leftShoulder = body.Joints[JointType.ShoulderLeft];

            bool isDetected = hand.Position.Y > shoulderSpine.Position.Y && (leftShoulder.Position.X - hand.Position.X) > 0.3;

            if (isDetected == true)
            {
                leftBoolCounter += 1;
                if (leftBoolCounter > 1)
                {
                    //Debug.WriteLine("Hola!" + "    " + leftBoolCounter + "\n");
                    return false;
                    //Debug.WriteLine("Hand Try 1!   " + (hand.Position.Y < head.Position.Y) + "\n");
                    //Debug.WriteLine("Hand Try 2!   " + (hand.Position.X > leftShoulder.Position.X) + "\n");
                }
                else
                    return true;
            }
            else {
                if (shoulderSpine.Position.Y > hand.Position.Y || hand.Position.X > shoulderSpine.Position.X)
                {
                    leftBoolCounter = 0;
                    //Debug.WriteLine("Entering...boolcounter=0" + "    " + (hand.Position.Y - shoulderSpine.Position.Y) + "\n");
                }
                return false;
                //Debug.WriteLine((hand.Position.X > shoulderSpine.Position.X) + "\n");
            }
        }

        //-----------------------------------------------------------//
        //-----------------------------------------------------------//

        /// <summary>
        /// Gets or sets the index number of the image to be shown.
        /// </summary>
        public int Index
        {
            get
            {
                return this.indexField;
            }

            set
            {
                if (this.indexField != value)
                {
                    this.indexField = value;

                    // Notify world of change to Index and Picture.
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Index"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the index number of the image to be shown
        /// </summary>
        public int backIndex
        {
            get
            {
                return this.indexField;
            }

            set
            {
                if (this.indexField != value)
                {
                    this.indexField = value;

                    // Notify world of change to Index and Picture.
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Index"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the previous image displayed.
        /// </summary>
        public BitmapImage PreviousPicture { get; private set; }

        /// <summary>
        /// Gets the current image to be displayed.
        /// </summary>
        public BitmapImage Picture { get; private set; }

        /// <summary>
        /// Gets the next image displayed.
        /// </summary>
        public BitmapImage NextPicture { get; private set; }

        /// <summary>
        /// Gets the next background iamge to be displayed
        /// </summary>
        public BitmapImage backgroundPic { get; private set; }

        #region Code for creating picture paths
        /// <summary>
        /// Get list of files to display as pictures.
        /// </summary>
        /// <param name="folderName">if empty, entire 'Images' directory is searched. Otherwise, the specified directory is searched</param>
        /// <returns>Path to pictures</returns>
        private static string[] CreatePicturePaths(string folderName)
        {
            var list = new List<string>();
            var commonPicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var newPath = commonPicturesPath + "\\Images\\" + folderName;
            list.AddRange(Directory.GetFiles(newPath, "*.jpg", SearchOption.AllDirectories));
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.png", SearchOption.AllDirectories));

            }
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.tif", SearchOption.AllDirectories));
            }
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.gif", SearchOption.AllDirectories));
            }
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.bmp", SearchOption.AllDirectories));
            }

            return list.ToArray();
        }
        #endregion

        #region Code for triggering `folder change` event if mouse stays on button for 2 seconds (5 seconds for Quit button)
        private void onMouseEnter(object sender, MouseEventArgs e)
        {
            if (!quitValue)
            {
                //this.Cursor = Cursors.Hand;
                dt = DateTime.Now;
                v = (Border)sender;
                s = v.Name;
                switch (s)
                {
                    case "AllImages":
                        pBar = (ProgressBar)allBar;
                        break;
                    case "Cars":
                        pBar = (ProgressBar)carsBar;
                        break;
                    case "Football":
                        pBar = (ProgressBar)footballBar;
                        break;
                    case "DC":
                        pBar = (ProgressBar)dcBar;
                        break;
                    case "Dogs":
                        pBar = (ProgressBar)dogsBar;
                        break;
                    case "back":
                        pBar = (ProgressBar)backBar;
                        this.backIndex++;
                        break;
                }
                pBar.Visibility = System.Windows.Visibility.Visible;
                t.Interval = new TimeSpan(0, 0, 1);
                t.IsEnabled = true;
                t3.Interval = new TimeSpan(0, 0, 0, 0, 32);
                t3.IsEnabled = true;
            }
        }

        private void onMouseLeave(object sender, MouseEventArgs e)
        {
            if (!quitValue)
            {
                pBar.Visibility = System.Windows.Visibility.Collapsed;
                pBar.Value = 0;
                switch (s)
                {
                    case "AllImages":
                        allLabel.Foreground = Brushes.Black;
                        break;
                    case "Cars":
                        carsLabel.Foreground = Brushes.Black;
                        break;
                    case "Football":
                        footballLabel.Foreground = Brushes.Black;
                        break;
                    case "DC":
                        dcLabel.Foreground = Brushes.Black;
                        break;
                    case "Dogs":
                        dogsLabel.Foreground = Brushes.Black;
                        break;
                    case "back":
                        backLabel.Foreground = Brushes.Black;
                        break;
                }
                t.IsEnabled = false;
                t3.IsEnabled = false;
            }
        }

        void t_Tick(object sender, EventArgs e)
        {

            if ((DateTime.Now - dt).Seconds >= 2)
            {
                switch (s)
                {
                    case "AllImages":
                        picturePaths = CreatePicturePaths("");
                        break;
                    case "Cars":
                        picturePaths = CreatePicturePaths("Cars");
                        break;
                    case "Football":
                        picturePaths = CreatePicturePaths("Football");
                        break;
                    case "DC":
                        picturePaths = CreatePicturePaths("DC");
                        break;
                    case "Dogs":
                        picturePaths = CreatePicturePaths("Dogs");
                        break;
                    case "back":
                        backPicturePaths = CreateBackgroundPicturePaths();
                        break;
                }

                if (s != "back")
                {
                    Index = 0;
                    this.PreviousPicture = this.LoadPicture(this.Index - 1);
                    this.Picture = this.LoadPicture(this.Index);
                    this.NextPicture = this.LoadPicture(this.Index + 1);

                    // Notify world of change to Index and Picture.
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("PreviousPicture"));
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Picture"));
                        this.PropertyChanged(this, new PropertyChangedEventArgs("NextPicture"));
                    }
                }
                else if (s == "back")
                {
                    this.backgroundPic = this.LoadBackgroundPicture(this.backIndex);
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("backgroundPic"));
                    }
                }
                pBar.Visibility = System.Windows.Visibility.Collapsed;
                pBar.Value = 0;
                switch (s)
                {
                    case "AllImages":
                        pBar = (ProgressBar)allBar;
                        allLabel.Foreground = Brushes.White;
                        break;
                    case "Cars":
                        pBar = (ProgressBar)carsBar;
                        carsLabel.Foreground = Brushes.White;
                        break;
                    case "Football":
                        pBar = (ProgressBar)footballBar;
                        footballLabel.Foreground = Brushes.White;
                        break;
                    case "DC":
                        pBar = (ProgressBar)dcBar;
                        dcLabel.Foreground = Brushes.White;
                        break;
                    case "Dogs":
                        pBar = (ProgressBar)dogsBar;
                        dogsLabel.Foreground = Brushes.White;
                        break;
                    case "back":
                        pBar = (ProgressBar)backBar;
                        backLabel.Foreground = Brushes.White;
                        break;

                }

            }

        }


        private void onMouseEnterQ(object sender, MouseEventArgs e)
        {
            if (!quitValue)
            {
                //this.Cursor = Cursors.Hand;
                dt = DateTime.Now;
                pBar = (ProgressBar)quitBar;
                t2.Interval = new TimeSpan(0, 0, 1);
                t2.IsEnabled = true;
                t4.Interval = new TimeSpan(0, 0, 0, 0, 94);
                t4.IsEnabled = true;
                pBar.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void onMouseLeaveQ(object sender, MouseEventArgs e)
        {
            //this.Cursor = newCursor;
            if (!quitValue)
            {
                t2.IsEnabled = false;
                t4.IsEnabled = false;
                pBar.Visibility = System.Windows.Visibility.Collapsed;
                pBar.Value = 0;
            }
        }

        private void t2_tick(object sender, EventArgs e)
        {
            if ((DateTime.Now - dt).Seconds >= 5)
            {
                quitValue = true;
                this.backgroundPic = this.LoadBackgroundPicture(0);
                this.PropertyChanged(this, new PropertyChangedEventArgs("backgroundPic"));
                window.back.Opacity = 0;
                window.Quit.Opacity = 0;
                window.Dogs.Opacity = 0;
                window.Cars.Opacity = 0;
                window.DC.Opacity = 0;
                window.Football.Opacity = 0;
                window.current.Opacity = 0;
                window.AllImages.Opacity = 0;
                window.allBar.Visibility = Visibility.Hidden;
                window.quitBar.Visibility = Visibility.Hidden;
                window.dogsBar.Visibility = Visibility.Hidden;
                window.carsBar.Visibility = Visibility.Hidden;
                window.dcBar.Visibility = Visibility.Hidden;
                window.footballBar.Visibility = Visibility.Hidden;
                window.backBar.Visibility = Visibility.Hidden;
                window.Text7.Opacity = 1;
                window.Text8.Opacity = 1;
                t5.Interval = new TimeSpan(0, 0, 0, 0, 1000);
                t5.IsEnabled = true;
                t.IsEnabled = false;
                t2.IsEnabled = false;
                t3.IsEnabled = false;
                t4.IsEnabled = false;
            }
        }

        private void t3_tick(object sender, EventArgs e)
        {
            pBar.Value += 2;
        }

        private void t4_tick(object sender, EventArgs e)
        {
            pBar.Value += 2;
        }

        private void t5_tick(object sender, EventArgs e)
        {

            window.Text8.Text = " " + counter + " seconds.";
            counter--;
            if (counter == -2)
                t6_tick(sender, e);
        }

        private void t6_tick(object sender, EventArgs e)
        {
            window.Close();
        }

        #endregion

        #region Handling Button Clicks

        private void quitApplication(object sender, RoutedEventArgs e)
        {
            quitValue = true;
            this.backgroundPic = this.LoadBackgroundPicture(0);
            this.PropertyChanged(this, new PropertyChangedEventArgs("backgroundPic"));
            window.back.Opacity = 0;
            window.Quit.Opacity = 0;
            window.Dogs.Opacity = 0;
            window.Cars.Opacity = 0;
            window.DC.Opacity = 0;
            window.Football.Opacity = 0;
            window.current.Opacity = 0;
            window.AllImages.Opacity = 0;
            window.allBar.Visibility = Visibility.Hidden;
            window.quitBar.Visibility = Visibility.Hidden;
            window.dogsBar.Visibility = Visibility.Hidden;
            window.carsBar.Visibility = Visibility.Hidden;
            window.dcBar.Visibility = Visibility.Hidden;
            window.footballBar.Visibility = Visibility.Hidden;
            window.backBar.Visibility = Visibility.Hidden;
            window.Text7.Opacity = 1;
            window.Text8.Opacity = 1;
            t5.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            t5.IsEnabled = true;
            t.IsEnabled = false;
            t2.IsEnabled = false;
            t3.IsEnabled = false;
            t4.IsEnabled = false;
        }

        private void changeBackground(object sender, RoutedEventArgs e)
        {
            backPicturePaths = CreateBackgroundPicturePaths();
            backIndex++;
            this.backgroundPic = this.LoadBackgroundPicture(this.backIndex);
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs("backgroundPic"));
            }
        }
    
        private void displayImages(object sender, RoutedEventArgs e)
        {
            btn = (Button) sender;
            s = btn.Name;
            switch (s)
            {
                case "allLabel":
                    picturePaths = CreatePicturePaths("");
                    break;
                case "carsLabel":
                    picturePaths = CreatePicturePaths("Cars");
                    break;
                case "footballLabel":
                    picturePaths = CreatePicturePaths("Football");
                    break;
                case "dcLabel":
                    picturePaths = CreatePicturePaths("DC");
                    break;
                case "dogsLabel":
                    picturePaths = CreatePicturePaths("Dogs");
                    break;
            }
            Index = 0;
            this.PreviousPicture = this.LoadPicture(this.Index - 1);
            this.Picture = this.LoadPicture(this.Index);
            this.NextPicture = this.LoadPicture(this.Index + 1);

            // Notify world of change to Index and Picture.
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs("PreviousPicture"));
                this.PropertyChanged(this, new PropertyChangedEventArgs("Picture"));
                this.PropertyChanged(this, new PropertyChangedEventArgs("NextPicture"));
            }
        }

        #endregion



        #region Code for Loading picture
        /// <summary>
        /// Load the picture with the given index.
        /// </summary>
        /// <param name="index">The index to use.</param>
        /// <returns>Corresponding image.</returns>
        private BitmapImage LoadPicture(int index)
        {
            BitmapImage value;

            if (this.picturePaths.Length != 0)
            {
                var actualIndex = index % this.picturePaths.Length;
                if (actualIndex < 0)
                {
                    actualIndex += this.picturePaths.Length;
                }

                Debug.Assert(0 <= actualIndex, "Index used will be non-negative");
                Debug.Assert(actualIndex < this.picturePaths.Length, "Index is within bounds of path array");

                try
                {
                    value = new BitmapImage(new Uri(this.picturePaths[actualIndex]));

                }
                catch (NotSupportedException)
                {
                    value = null;
                }
            }
            else
            {
                value = null;
            }

            return value;
        }
        #endregion

        #region Create Background Pictures path. Load Background pictures

        /// <summary>
        /// Get list of files to display as pictures.
        /// </summary>
        /// <returns>Path to pictures</returns>
        private static string[] CreateBackgroundPicturePaths()
        {
            var list = new List<string>();
            var commonPicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var newPath = commonPicturesPath + "\\Background";
            list.AddRange(Directory.GetFiles(newPath, "*.jpg", SearchOption.AllDirectories));
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.png", SearchOption.AllDirectories));

            }
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.tif", SearchOption.AllDirectories));
            }
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.gif", SearchOption.AllDirectories));
            }
            if (list.Count == 0)
            {
                list.AddRange(Directory.GetFiles(newPath, "*.bmp", SearchOption.AllDirectories));
            }

            return list.ToArray();
        }

        /// <summary>
        /// Load the picture with the given index.
        /// </summary>
        /// <param name="index">The index to use.</param>
        /// <returns>Corresponding image.</returns>
        private BitmapImage LoadBackgroundPicture(int index)
        {
            BitmapImage value;

            if (this.backPicturePaths.Length != 0)
            {
                var actualIndex = index % this.backPicturePaths.Length;
                if (actualIndex < 0)
                {
                    actualIndex += this.backPicturePaths.Length;
                }

                Debug.Assert(0 <= actualIndex, "Index used will be non-negative");
                Debug.Assert(actualIndex < this.backPicturePaths.Length, "Index is within bounds of path array");

                try
                {
                    value = new BitmapImage(new Uri(this.backPicturePaths[actualIndex]));

                }
                catch (NotSupportedException)
                {
                    value = null;
                }
            }
            else
            {
                value = null;
            }

            return value;
        }

        #endregion
    }
}
