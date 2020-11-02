
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;


namespace KinectProjectWork
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 
    using System.Windows;
    using Microsoft.Kinect.Wpf.Controls;

    public partial class App : Application
    {
        internal KinectRegion KinectRegion { get; set; }
    }
}
