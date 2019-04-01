
namespace Kinect
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Forms;
    using System.Threading;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Collections.Generic;
    using Microsoft.Kinect;
    //sing Accord.Video.FFMPEG;
    using System.Windows.Controls;
    using System.Windows.Markup;
     

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private KinectSensor kinectSensor = null;
        static System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();
        int alarmCounter = 0;
        string cfilename = "";
        string dfilename = "";
        int control = 0;
        private ColorFrameReader colorFrameReader = null;
        private DepthFrameReader depthFrameReader = null;
        private WriteableBitmap colorBitmap = null;
        private WriteableBitmap depthBitmap = null;
        private const int MapDepthToByte = 8000 / 256;
        private byte[] depthPixels = null;
        private FrameDescription depthFrameDescription = null;
        private FrameDescription colorFrameDescription = null;
        private string FolderLoc = "";
        private bool record = false;
        private static CountdownEvent complete = new CountdownEvent(1);
       // private static VideoFileWriter ColorWriter = new VideoFileWriter();
       // private static VideoFileWriter DepthWriter = new VideoFileWriter();
        private int cnumber = 0;
        private int dnumber = 0;
        readonly string path = Directory.GetCurrentDirectory();
        private MultiSourceFrameReader multiSourceFrameReader=null;

        private string statusText = null;
        public MainWindow()
        {
            this.kinectSensor = KinectSensor.GetDefault();
            //this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
            //this.depthFrameReader = this.kinectSensor.DepthFrameSource.OpenReader();
            this.multiSourceFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);
            this.multiSourceFrameReader.MultiSourceFrameArrived += this.MultiSourceFrameReader_MultiSourceFrameArrived;
            //this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
            //this.depthFrameReader.FrameArrived += this.Reader_DepthFrameArrived;
            this.colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            this.depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            this.depthPixels = new byte[this.depthFrameDescription.Width * this.depthFrameDescription.Height];
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.depthBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Gray8, null);
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;
            this.kinectSensor.Open();
            this.DataContext = this;
            this.record = false;

            this.InitializeComponent();
            CountDown.Content = alarmCounter.ToString();
            myTimer.Tick += new EventHandler(TimerEventProcessor);
            myTimer.Interval = 1000;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        public ImageSource ColorSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        public ImageSource DepthSource
        {
            get
            {
                return this.depthBitmap;
            }
        }


        void CreateThumbnail(string filename, BitmapSource image1)
        {
            
            if (filename != string.Empty)
            {
                using (FileStream stream = new FileStream(filename, FileMode.Create))
                {
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image1));
                    encoder.Save(stream);
                }
            }
        }

        private void MultiSourceFrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            bool depthFrameProcessed = false;
            if (e.FrameReference == null) return;

            MultiSourceFrame reference = e.FrameReference.AcquireFrame();

            if (reference == null) return;

            //        if (reference.ColorFrameReference != null && reference.DepthFrameReference != null)
            //         {   
            
            using (ColorFrame colorFrame = reference.ColorFrameReference.AcquireFrame())
            using (DepthFrame depthFrame = reference.DepthFrameReference.AcquireFrame())
            if(colorFrame != null && depthFrame != null)
            { 
                using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                using (KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                {

                    this.colorBitmap.Lock();

                    // verify data and write the new color frame data to the display bitmap
                    if ((this.colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (this.colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                    {
                        colorFrame.CopyConvertedFrameDataToIntPtr(
                            this.colorBitmap.BackBuffer,
                            (uint)(this.colorFrameDescription.Width * this.colorFrameDescription.Height * 4),
                            ColorImageFormat.Bgra);

                        this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                    }

                    colorBitmap.Unlock();
                    //Debug.Print(colorBitmap.Width.ToString());
                    if (record)
                    {
                        //ThreadPool.QueueUserWorkItem(new WaitCallback(colorRecord), colorBitmap);
                        //ThreadPool.QueueUserWorkItem(new WaitCallback(colorRecord), BitmapFromWriteableBitmap(colorBitmap));

                        string filename1 = FolderPath.Text + "\\" + Filename.Text + "_" + uterance_id.Text + "_color" + "\\" + cnumber + ".jpg";
                        CreateThumbnail(filename1, colorBitmap.Clone()); //ThreadPool.QueueUserWorkItem(save => CreateThumbnail(filename1, colorBitmap.Clone()));
                        cnumber++;
                    }

                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer

                    // verify data and write the color data to the display bitmap
                    if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                        (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                    {
                        // Note: In order to see the full range of depth (including the less reliable far field depth)
                        // we are setting maxDepth to the extreme potential depth threshold
                        ushort maxDepth = ushort.MaxValue;

                        // If you wish to filter by reliable depth distance, uncomment the following line:
                        //// maxDepth = depthFrame.DepthMaxReliableDistance

                        this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                        depthFrameProcessed = true;
                    }

                    if (depthFrameProcessed)
                    {
                        this.RenderDepthPixels();
                    }
                }
            }

        }

        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        private void RenderDepthPixels()
        {
            //Debug.Print(BitmapFromWriteableBitmap(this.depthBitmap).Height.ToString());
            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                this.depthPixels,
                this.depthBitmap.PixelWidth,
                0);
            if (record)
            {
                //DepthWriter.WriteVideoFrame(BitmapFromWriteableBitmap(depthBitmap));
                //ThreadPool.QueueUserWorkItem(new WaitCallback(depthRecord), BitmapFromWriteableBitmap(depthBitmap));
                string filename2 = FolderPath.Text + "\\" + Filename.Text + "_" + uterance_id.Text + "_depth" + "\\" + dnumber + ".jpg";

                CreateThumbnail(filename2, depthBitmap.Clone());//ThreadPool.QueueUserWorkItem(save => CreateThumbnail(filename2, depthBitmap.Clone()));
                dnumber++;
            }

        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text

        }

        void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
        {
            alarmCounter += 1;
            CountDown.Content = alarmCounter.ToString();
            //myTimer.Enabled = true;
            if (alarmCounter==5)
            {
                
                myTimer.Stop();
                alarmCounter = 0;
                //myTimer.Enabled = false;
                System.IO.Directory.CreateDirectory(cfilename);
                System.IO.Directory.CreateDirectory(dfilename);
                CountDown.Content = alarmCounter.ToString();
                //myTimer.Dispose();
                record = true;
                

            }
            
        }

        void RecordBtn_Click(object sender, RoutedEventArgs e)
        {
            cfilename = FolderPath.Text + "\\" + Filename.Text + "_" + uterance_id.Text + "_color";
            dfilename = FolderPath.Text + "\\" + Filename.Text + "_" + uterance_id.Text + "_depth";
            //string path = Directory.GetCurrentDirectory();
            //System.Windows.Forms.MessageBox.Show(path, "Error: file exist",
            //MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (this.record == false && control==0)
            {
                if (!Directory.Exists(cfilename) && !Directory.Exists(dfilename)&& !string.IsNullOrWhiteSpace(Filename.Text)&& !string.IsNullOrWhiteSpace(uterance_id.Text) && !string.IsNullOrWhiteSpace(FolderPath.Text))
                {

                    control = 1;
                    RecordBtn.Content = "STOP";
                    myTimer.Enabled = true;
                                     

                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("please change the Signer ID or the Utterance ID, this ones already exist or the fields are empty", "Error: file exist or fields are empty",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if (this.record == true)
            {

                this.record = false;
                control = 0;
                alarmCounter = 0;
                CountDown.Content = alarmCounter.ToString();                
                RecordBtn.Content = "RECORD";
                complete.Signal();
                complete.Wait();
                cnumber = 0;
                dnumber = 0;
                string cfilepath = FolderPath.Text + "\\" + Filename.Text + "_" + uterance_id.Text + "_color";
                string dfilepath = FolderPath.Text + "\\" + Filename.Text + "_" + uterance_id.Text + "_depth";

                FFMPEGVideoRecord(cfilepath, "_color");
                FFMPEGVideoRecord(dfilepath, "_depth");

                complete.Reset();
                
            }

            if (alarmCounter > 0 && alarmCounter < 5 && this.record == false && control==1)
            {
                control = 0;
            //    myTimer.Stop();
                myTimer.Enabled = false;
                alarmCounter = 0;
                CountDown.Content = alarmCounter.ToString();
                RecordBtn.Content = "RECORD";
            }
        }


        private void FFMPEGVideoRecord(string filepath,string type)
        {
            string strCmdText;
            string filename = Filename.Text;
            //string path = Directory.GetCurrentDirectory();
            strCmdText = @"/C "+path+"\\ffmpeg\\bin\\ffmpeg -f image2 -r 30 -i %d.jpg -r 30 "+ filename+"_"+ uterance_id.Text +type+ ".avi & del *.jpg";
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            //startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.WorkingDirectory = @"" + filepath;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = strCmdText;
            process.StartInfo = startInfo;
            process.Start();

        }

        private Bitmap BitmapFromWriteableBitmap(WriteableBitmap writeBmp)
        {
            Bitmap bmp;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource)writeBmp));
                enc.Save(outStream);
                bmp = new System.Drawing.Bitmap(outStream);
            }
            return bmp;
        }

        private static void colorRecord(object wb)
        {
            complete.AddCount(1);
                
            //ColorWriter.WriteVideoFrame((System.Drawing.Bitmap)wb);
            complete.Signal();
        }
        private static void depthRecord(object wb)
        {
            complete.AddCount(1);
            //DepthWriter.WriteVideoFrame((System.Drawing.Bitmap)wb);
            complete.Signal();
        }

        private void Select_Folder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Select Folder";
            fbd.ShowNewFolderButton = true;
            if (fbd.ShowDialog().ToString().Equals("OK"))
            {
                FolderPath.Text = fbd.SelectedPath;
                this.FolderLoc = fbd.SelectedPath;
            }

        }




        private static WriteableBitmap ResizeWritableBitmap(WriteableBitmap wBitmap, int reqWidth, int reqHeight)
        {
            int Stride = wBitmap.PixelWidth * ((wBitmap.Format.BitsPerPixel + 7) / 8);
            int NumPixels = Stride * wBitmap.PixelHeight;
            ushort[] ArrayOfPixels = new ushort[NumPixels];


            wBitmap.CopyPixels(ArrayOfPixels, Stride, 0);

            int OriWidth = (int)wBitmap.PixelWidth;
            int OriHeight = (int)wBitmap.PixelHeight;

            double nXFactor = (double)OriWidth / (double)reqWidth;
            double nYFactor = (double)OriHeight / (double)reqHeight;

            double fraction_x, fraction_y, one_minus_x, one_minus_y;
            int ceil_x, ceil_y, floor_x, floor_y;

            ushort pix1, pix2, pix3, pix4;
            int nStride = reqWidth * ((wBitmap.Format.BitsPerPixel + 7) / 8);
            int nNumPixels = reqWidth * reqHeight;
            ushort[] newArrayOfPixels = new ushort[nNumPixels];
            for (int y = 0; y < reqHeight; y++)
            {
                for (int x = 0; x < reqWidth; x++)
                {
                    // Setup
                    floor_x = (int)Math.Floor(x * nXFactor);
                    floor_y = (int)Math.Floor(y * nYFactor);

                    ceil_x = floor_x + 1;
                    if (ceil_x >= OriWidth) ceil_x = floor_x;

                    ceil_y = floor_y + 1;
                    if (ceil_y >= OriHeight) ceil_y = floor_y;

                    fraction_x = x * nXFactor - floor_x;
                    fraction_y = y * nYFactor - floor_y;

                    one_minus_x = 1.0 - fraction_x;
                    one_minus_y = 1.0 - fraction_y;

                    pix1 = ArrayOfPixels[floor_x + floor_y * OriWidth];
                    pix2 = ArrayOfPixels[ceil_x + floor_y * OriWidth];
                    pix3 = ArrayOfPixels[floor_x + ceil_y * OriWidth];
                    pix4 = ArrayOfPixels[ceil_x + ceil_y * OriWidth];

                    ushort g1 = (ushort)(one_minus_x * pix1 + fraction_x * pix2);
                    ushort g2 = (ushort)(one_minus_x * pix3 + fraction_x * pix4);
                    ushort g = (ushort)(one_minus_y * (double)(g1) + fraction_y * (double)(g2));
                    newArrayOfPixels[y * reqWidth + x] = g;
                }
            }
            WriteableBitmap newWBitmap = new WriteableBitmap(reqWidth, reqHeight, 96, 96, PixelFormats.Gray16, null);
            Int32Rect Imagerect = new Int32Rect(0, 0, reqWidth, reqHeight);
            int newStride = reqWidth * ((PixelFormats.Gray16.BitsPerPixel + 7) / 8);
            newWBitmap.WritePixels(Imagerect, newArrayOfPixels, newStride, 0);
            return newWBitmap;
        }

    }
}
