using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
//using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Net;
using System.Net.Sockets;
using Bespoke.Common;
using Bespoke.Common.Osc;

using System.Windows.Threading;

using System.IO.Ports;

using ExceptionEventArgs = Bespoke.Common.ExceptionEventArgs;

using System.Reflection;

namespace MainController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        OscServer oscCmdServer;
        private static int Port = 8000;
        private static readonly string kinectFrontCmd = "/kinect/1";
        private static readonly string kinectBackCmd = "/kinect/2";
        private string iPadMsgAddr;
        private int iPadMsgArg;
        

        OscMessage msg;

        private static IPEndPoint kinectFrontIP = new IPEndPoint(IPAddress.Loopback, Port);
        private static IPEndPoint kinectBackIP = new IPEndPoint(IPAddress.Loopback, Port);
        private static IPEndPoint limboViewerIP = new IPEndPoint(IPAddress.Loopback, Port);
        private static IPEndPoint imageServerIP = new IPEndPoint(IPAddress.Loopback, Port);
        private static IPEndPoint limboStandIP = new IPEndPoint(IPAddress.Loopback, Port);
        private static IPEndPoint ipadIP = new IPEndPoint(IPAddress.Loopback, Port);


        SerialPort serial = new SerialPort();
        string serialRxData;
        FlowDocument mcFlowDoc = new FlowDocument();
        Paragraph para = new Paragraph();

        private bool kinectFrontOff;
        private bool kinectBackOff;

        DispatcherTimer dispatcherTimer;





        // Nike Free Limbo variables


        private const int slideviewScene = 1;
        private const int exerciseScene = 2;
        private const int limboScene = 3;

        private const int kinectFront = 1;
        private const int kinectBack = 2;

        private int currentScene = slideviewScene;

        private int userCount;

        private int standHeight = 3; // 5.0 = 3, 4.0 = 2, 3.0 = 1

        private bool iPadOK = false;
        private bool limboViewerOK = false;
        private bool limboStandOK = false;
        private bool imageServerOK = false;
        private bool kinectFrontOK = false;
        private bool kinectBackOK = false;
        private bool remoteConOK = false;

        StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;




        public MainWindow()
        {
            InitializeComponent();

            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

            

            comms_connect.Content = "Connect";

            Port = MySettings.Default.portSetting;
            kinectFrontIP.Address = IPAddress.Parse(MySettings.Default.kinectFrontIPSetting);
            kinectFrontIP.Port = Port;
            kinectBackIP.Address = IPAddress.Parse(MySettings.Default.kinectBackIPSetting);
            kinectBackIP.Port = Port;
            limboViewerIP.Address = IPAddress.Parse(MySettings.Default.limboViewerIPSetting);
            limboViewerIP.Port = Port;
            imageServerIP.Address = IPAddress.Parse(MySettings.Default.imageServerIPSetting);
            imageServerIP.Port = Port;
            limboStandIP.Address = IPAddress.Parse(MySettings.Default.limboStandIPSetting);
            limboStandIP.Port = Port;
            ipadIP.Address = IPAddress.Parse(MySettings.Default.iPadIPSetting);
            ipadIP.Port = Port;

            iPadMsgAddr = MySettings.Default.iPadMsgAddrSetting;
            iPadMsgArg = MySettings.Default.iPadMsgArgSetting;

            iPadMsgAddrInput.Text = iPadMsgAddr;
            iPadMsgArgInput.Text = iPadMsgArg.ToString();

            portInput.Text = Port.ToString();
            kinectFrontIPInput.Text = MySettings.Default.kinectFrontIPSetting;
            kinectBackIPInput.Text = MySettings.Default.kinectBackIPSetting;
            limboViewerIPInput.Text = MySettings.Default.limboViewerIPSetting;
            imageServerIPInput.Text = MySettings.Default.imageServerIPSetting;
            limboStandIPInput.Text = MySettings.Default.limboStandIPSetting;
            ipadIPInput.Text = MySettings.Default.iPadIPSetting;

            userCount = MySettings.Default.userCountSetting;
            userCountDisplay.Text = userCount.ToString();


            myIPAddrText.Text = LocalIPAddress();






            iPadLED.Fill = new SolidColorBrush(Colors.Gray);
            limboViewerLED.Fill = new SolidColorBrush(Colors.Gray);
            limboStandLED.Fill = new SolidColorBrush(Colors.Gray);
            imageServerLED.Fill = new SolidColorBrush(Colors.Gray);
            kinectFrontLED.Fill = new SolidColorBrush(Colors.Gray);
            kinectBackLED.Fill = new SolidColorBrush(Colors.Gray);
            remoteLED.Fill = new SolidColorBrush(Colors.Gray); 

            //sourceEndPoint.Address = IPAddress.Parse("192.168.0.114");
            //sourceEndPoint.Port = Convert.ToInt32(12345);

            oscCmdServer = new OscServer(TransportType.Udp, IPAddress.Parse(LocalIPAddress()), Port);
            //oscCmdServer = new OscServer(IPAddress.Parse("224.25.26.27"), Port);
            oscCmdServer.FilterRegisteredMethods = false;
            //oscCmdServer.RegisterMethod(oscCmd);
            oscCmdServer.RegisterMethod(kinectFrontCmd);
            oscCmdServer.RegisterMethod(kinectBackCmd);
            //oscCmdServer.RegisterMethod(TestMethod);
            oscCmdServer.BundleReceived += new EventHandler<OscBundleReceivedEventArgs>(oscCmdServer_BundleReceived);
            oscCmdServer.MessageReceived += new EventHandler<OscMessageReceivedEventArgs>(oscCmdServer_MessageReceived);
            oscCmdServer.ReceiveErrored += new EventHandler<ExceptionEventArgs>(oscCmdServer_ReceiveErrored);
            oscCmdServer.ConsumeParsingExceptions = false;
            oscCmdServer.Start();




        }

        private void oscCmdServer_BundleReceived(object sender, OscBundleReceivedEventArgs e)
        {
            sBundlesReceivedCount++;

            OscBundle bundle = e.Bundle;
           
           
            Console.WriteLine(string.Format("\nBundle Received [{0}:{1}]: Nested Bundles: {2} Nested Messages: {3}", bundle.SourceEndPoint.Address, bundle.TimeStamp, bundle.Bundles.Count, bundle.Messages.Count));
            Console.WriteLine("Total Bundles Received: {0}", sBundlesReceivedCount);
            
        }

        private void oscCmdServer_MessageReceived(object sender, OscMessageReceivedEventArgs e)
		{
            sMessagesReceivedCount++;

            OscMessage message = e.Message;

            Console.WriteLine(string.Format("\nMessage Received [{0}]: {1}", message.SourceEndPoint.Address, message.Address));
            Console.WriteLine(string.Format("Message contains {0} objects.", message.Data.Count));

            iPadMsgReceive(message);
            limboViewerMsgReceive(message);
            limboStandMsgReceive(message);
            imageServerMsgReceive(message);
            kinectMsgReceive(kinectFront, message);
            kinectMsgReceive(kinectBack, message);




            if (message.Address == "/ipad")
            {
                if (message.Data[0].ToString() == "ok")
                {
                    iPadOK = true;

                }

            }

            if (message.Address == kinectFrontCmd)
            {
                if (message.Data[0].ToString() == "IN")
                {
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        frontStatus.Fill = new SolidColorBrush(Colors.Red);
                    }));
                }

                if (message.Data[0].ToString() == "OUT")
                {
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        frontStatus.Fill = new SolidColorBrush(Colors.Blue);
                    }));
                    msg = new OscMessage(kinectFrontIP, kinectFrontCmd);
                    msg.Append("STOP");
                    msg.Send(kinectFrontIP);

                    msg = new OscMessage(kinectBackIP, kinectBackCmd);
                    msg.Append("START");
                    msg.Send(kinectBackIP);
                }
            }

            if (message.Address == kinectBackCmd)
            {
                if (message.Data[0].ToString() == "IN")
                {
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        backStatus.Fill = new SolidColorBrush(Colors.Red);
                        msg = new OscMessage(kinectBackIP, kinectBackCmd);
                        msg.Append("STOP");
                        msg.Send(kinectBackIP);

                        msg = new OscMessage(kinectFrontIP, kinectFrontCmd);
                        msg.Append("START");
                        msg.Send(kinectFrontIP);
                    }));
                }

                if (message.Data[0].ToString() == "OUT")
                {
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                    {
                        backStatus.Fill = new SolidColorBrush(Colors.Blue);

                        msg = new OscMessage(kinectBackIP, kinectBackCmd);
                        msg.Append("STOP");
                        msg.Send(kinectBackIP);

                        msg = new OscMessage(kinectFrontIP, kinectFrontCmd);
                        msg.Append("START");
                        msg.Send(kinectFrontIP);
                    }));
                }
                
            }
            for (int i = 0; i < message.Data.Count; i++)
            {
                string dataString;

                if (message.Data[i] == null)
                {
                    dataString = "Nil";
                }
                else
                {
                    dataString = (message.Data[i] is byte[] ? BitConverter.ToString((byte[])message.Data[i]) : message.Data[i].ToString());
                }
                Console.WriteLine(string.Format("[{0}]: {1}", i, dataString));
            }

            Console.WriteLine("Total Messages Received: {0}", sMessagesReceivedCount);
		}

        private static void oscCmdServer_ReceiveErrored(object sender, ExceptionEventArgs e)
        {
            Console.WriteLine("Error during reception of packet: {0}", e.Exception.Message);
        }


        private static int sBundlesReceivedCount;
        private static int sMessagesReceivedCount;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //msg = new OscMessage(sourceEndPoint, oscCmd);
            //msg.Append((float)1.0);
            //msg.Append((float)4.0);
            //msg.Send(sourceEndPoint);

        }


        private void IPSetButton_Click(object sender, RoutedEventArgs e)
        {
            Port = Convert.ToInt32(portInput.Text);
            kinectFrontIP.Address = IPAddress.Parse(kinectFrontIPInput.Text);
            kinectFrontIP.Port = Port;
            kinectBackIP.Address = IPAddress.Parse(kinectBackIPInput.Text);
            kinectBackIP.Port = Port;
            limboViewerIP.Address = IPAddress.Parse(limboViewerIPInput.Text);
            limboViewerIP.Port = Port;
            imageServerIP.Address = IPAddress.Parse(imageServerIPInput.Text);
            imageServerIP.Port = Port;
            limboStandIP.Address = IPAddress.Parse(limboStandIPInput.Text);
            limboStandIP.Port = Port;
            ipadIP.Address = IPAddress.Parse(ipadIPInput.Text);
            ipadIP.Port = Port;

            MySettings.Default.portSetting = Port;
            MySettings.Default.kinectFrontIPSetting = kinectFrontIPInput.Text;
            MySettings.Default.kinectBackIPSetting = kinectBackIPInput.Text;
            MySettings.Default.limboViewerIPSetting = limboViewerIPInput.Text;
            MySettings.Default.imageServerIPSetting = imageServerIPInput.Text;
            MySettings.Default.limboStandIPSetting = limboStandIPInput.Text;
            MySettings.Default.iPadIPSetting = ipadIPInput.Text;
            

        }

        private void comms_connect_Click(object sender, RoutedEventArgs e)
        {
            if ((string)comms_connect.Content == "Connect")
            {
                //Sets up serial port
                serial.PortName = comm_ports.Text;
                serial.BaudRate = Convert.ToInt32(comm_speed.Text);
                serial.Handshake = System.IO.Ports.Handshake.None;
                serial.Parity = Parity.None;
                serial.DataBits = 8;
                serial.StopBits = StopBits.One;
                serial.ReadTimeout = 200;
                serial.WriteTimeout = 50;
                serial.Open();
                comms_connect.Content = "Disconnect";
                serial.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(serialRxHandler);
            }
            else
            {
                serial.Close();
                comms_connect.Content = "Connect";
                
            }
        }

        private delegate void UpdateUiTextDelegate(string text);
        private void serialRxHandler(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            // Collecting the characters received to our 'buffer' (string).
            //serialRxData = serial.ReadExisting();
            serialRxData = serial.ReadLine();
            remtoteMsgReceive(serialRxData);
            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(showRxData), serialRxData);
        }
        private void showRxData(string text)
        {
            // Assign the value of the recieved_data to the RichTextBox.
            para.Inlines.Add(text);
            mcFlowDoc.Blocks.Add(para);
            commData.Document = mcFlowDoc;
        }

        private string LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MySettings.Default.Save();
        }

        private void slideSceneButton_Click(object sender, RoutedEventArgs e)
        {
            msg = new OscMessage(limboViewerIP, "/scene");
            msg.Append((Int32)1);
            msg.Send(limboViewerIP);
            
        }

        private void exerciseSceneButton_Click(object sender, RoutedEventArgs e)
        {
            msg = new OscMessage(limboViewerIP, "/scene");
            msg.Append((Int32)2);
            msg.Send(limboViewerIP);
        }

        private void limboSceneButton_Click(object sender, RoutedEventArgs e)
        {
            msg = new OscMessage(limboViewerIP, "/scene");
            msg.Append((Int32)3);
            msg.Send(limboViewerIP);
        }

        private void successButton_Click(object sender, RoutedEventArgs e)
        {   
            typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(failButton, new object[] { true });
            typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(failButton, new object[] { false });
            failButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            
        }

        private void failButton_Click(object sender, RoutedEventArgs e)
        {
            

        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Console.WriteLine("Comm Test All");
            if (iPadOK == true)
            {
                iPadLED.Fill = new SolidColorBrush(Colors.Green);
            }
            else
            {
                iPadLED.Fill = new SolidColorBrush(Colors.Red);
            }

            if (remoteConOK)
            {
                remoteLED.Fill = new SolidColorBrush(Colors.Green);
            }
            else {
                remoteLED.Fill = new SolidColorBrush(Colors.Red);
            }


            if (standHeight == 3)
            {
                radioButton5.IsChecked = true;
            }
            else if (standHeight == 2)
            {
                radioButton4.IsChecked = true;
            }
            else if (standHeight == 1)
            {
                radioButton3.IsChecked = true;
            }
            TestAllButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            iPadOK = false;
            limboViewerOK = false;
            limboStandOK = false;
            imageServerOK = false;
            kinectFrontOK = false;
            kinectBackOK = false;
            remoteConOK = false;
            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

      

        private void iPadTestButton_Click(object sender, RoutedEventArgs e)
        {
            msg = new OscMessage(ipadIP, iPadMsgAddr);
            msg.Append(iPadMsgArg);
            msg.Send(ipadIP);
        }

        private void iPadMsgAddrInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            iPadMsgAddr = iPadMsgAddrInput.Text;
            MySettings.Default.iPadMsgAddrSetting = iPadMsgAddr;
        }

        private void iPadMsgArgInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            iPadMsgArg = Convert.ToInt32(iPadMsgArgInput.Text);
            MySettings.Default.iPadMsgArgSetting = iPadMsgArg;
        }

        private void pictureButton_Click(object sender, RoutedEventArgs e)
        {

           
            msg = new OscMessage(imageServerIP, "/image/picture");
            msg.Append(userCount);
            msg.Send(imageServerIP);

            msg = new OscMessage(ipadIP, "/ipad/picture");
            msg.Append(userCount);
            msg.Send(ipadIP);
            userCount++;


            MySettings.Default.userCountSetting = userCount-1;
            userCountDisplay.Text = (userCount-1).ToString();
        }

        private void resetUserCountButton_Click(object sender, RoutedEventArgs e)
        {
            userCount = 0;
            MySettings.Default.userCountSetting = 0;
            userCountDisplay.Text = userCount.ToString();
        }

        private void ipadReadyButton_Click(object sender, RoutedEventArgs e)
        {
            iPadNextUserReady();
        }

        private void iPadMsgButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ipadTestButton_Click_1(object sender, RoutedEventArgs e)
        {
            iPadIsAlive();
           
        }



        // iPad Message Send
        private void iPadSendPicture(int _userCount)
        {
            OscMessage msg = new OscMessage(ipadIP, "/ipad/picture");
            msg.Append(_userCount);
            msg.Send(ipadIP);
        }

        private void iPadNextUserReady()
        {
            OscMessage msg = new OscMessage(ipadIP, "/ipad");
            msg.Append("ready");
            msg.Send(ipadIP);
        }

        private void iPadIsAlive()
        {
            OscMessage msg = new OscMessage(ipadIP, "/ipad");
            msg.Append("test");
            msg.Send(ipadIP);
        }

        // iPad Message Receive

        private void iPadMsgReceive(OscMessage message)
        {
            if (message.Address == "/ipad")
            {
                if(message.Data[0].GetType() == typeof(int))
                {
                    if (Convert.ToInt32(message.Data[0]) == 1 || Convert.ToInt32(message.Data[0]) == 2 || Convert.ToInt32(message.Data[0]) == 3)
                    {
                        standHeight = Convert.ToInt32(message.Data[0]);
                        limboStandSetStandHeight(standHeight);
                    }
                }
                if (message.Data[0].ToString() == "exercise")
                {
                    currentScene = exerciseScene;
                    limboViewerSetScene(currentScene);
                    kinectOn(kinectFront);
                    kinectOn(kinectBack);  
                }
                if (message.Data[0].ToString() == "ok")
                {
                    iPadOK = true;
                }

            }

        }

        // Limbo Stand Message Send
        private void limboStandSetStandHeight(int _standHeight)
        {
            OscMessage msg = new OscMessage(limboStandIP, "/stand");
            msg.Append(standHeight);
            msg.Send(limboStandIP);
        }

        private void limboStandIsAlive()
        {
            OscMessage msg = new OscMessage(limboStandIP, "/stand");
            msg.Append("test");
            msg.Send(limboStandIP);
        }

        // Limbo Stand Message Receive
        private void limboStandMsgReceive(OscMessage message)
        {
            if (message.Address == "/stand")
            {
               
                if (message.Data[0].ToString() == "shoot")
                {
                    userCount++;

                    imageServerTakePhoto(userCount);
                    limboViewerCaptureFree(userCount);

                    MySettings.Default.userCountSetting = userCount;
                    userCountDisplay.Text = userCount.ToString();

                }
                if (message.Data[0].ToString() == "ok")
                {
                    limboStandOK = true;
                }

            }
        }


        // Limbo Viewer Message Send
        private void limboViewerSetScene(int sceneNumber)
        {
            OscMessage msg = new OscMessage(limboViewerIP, "/view/scene");
            msg.Append(sceneNumber);
            msg.Send(limboViewerIP);
        }

        private void limboViewerCaptureFree(int _userCount)
        {
            OscMessage msg = new OscMessage(limboViewerIP, "/view/picture");
            msg.Append(_userCount);
            msg.Send(limboViewerIP);
        }

        private void limboViewerSendSuccess(bool successStatus)
        {
            OscMessage msg = new OscMessage(limboViewerIP, "/view");
            if (successStatus)
            {
                msg.Append("success");
            }
            else 
            {
                msg.Append("fail");
            }

            msg.Send(limboViewerIP);
        }

        private void limboViewerGetImageFromServer(int _userCount)
        {
            OscMessage msg = new OscMessage(limboViewerIP, "/view/merge");
            msg.Append(_userCount);
            msg.Send(limboViewerIP);
        }

        private void limboViewerIsAlive()
        {
            OscMessage msg = new OscMessage(limboViewerIP, "/view");
            msg.Append("test");
            msg.Send(limboViewerIP);
        }

        // Limbo Viewer Message Receive
        private void limboViewerMsgReceive(OscMessage message)
        {
            if (message.Address == "/view")
            {
                if (message.Data[0].ToString() == "sync")
                {
                    currentScene = limboScene;
                    limboViewerSetScene(currentScene);
                    kinectOn(kinectFront);
                    kinectOn(kinectBack);
                }

                if (message.Data[0].ToString() == "ok")
                {
                    limboViewerOK = true;
                }
                
            }

            if (message.Address == "/view/image")
            {
                if (message.Data[0].ToString() == "uploaded")
                {
                    imageServerMergeImage();
                }
            }
        }
        
        // Image Server Message Send
        private void imageServerMergeImage()
        {
            OscMessage msg = new OscMessage(imageServerIP, "/image");
            msg.Append("merge");
            msg.Send(imageServerIP);
        }

        private void imageServerTakePhoto(int _userCount)
        {
            OscMessage msg = new OscMessage(imageServerIP, "/image/picture");
            msg.Append(_userCount);
            msg.Send(imageServerIP);
        }

        private void imageServerIsAlive()
        {
            OscMessage msg = new OscMessage(imageServerIP, "/image");
            msg.Append("test");
            msg.Send(imageServerIP);
        }

        // Image Server Message Receive

        private void imageServerMsgReceive(OscMessage message)
        {
            if (message.Address == "/image")
            {
                if (message.Data[0].ToString() == "ok")
                {
                    imageServerOK = true;
                }

            }

            if (message.Address == "/image/merge")
            {
                if (message.Data[0].ToString() == "done")
                {
                    iPadSendPicture(userCount);
                    limboViewerGetImageFromServer(userCount);
                }
            }
        }


        // Kinect Message Send
        private void kinectOn(int kinect)
        {
            string addr = "/kinect/" + kinect.ToString();

            if(kinect == 1)
            {
                OscMessage msg = new OscMessage(kinectFrontIP, addr); ;
                msg.Append("on");
                msg.Send(kinectFrontIP);
            }
            else if (kinect == 2)
            {
                OscMessage msg = new OscMessage(kinectBackIP, addr); ;
                msg.Append("on");
                msg.Send(kinectBackIP);
            }
            else 
            {
                return;
            }
        }

        private void kinectOff(int kinect)
        {
            string addr = "/kinect/" + kinect.ToString();

            if (kinect == 1)
            {
                OscMessage msg = new OscMessage(kinectFrontIP, addr); ;
                msg.Append("off");
                msg.Send(kinectFrontIP);
            }
            else if (kinect == 2)
            {
                OscMessage msg = new OscMessage(kinectBackIP, addr); ;
                msg.Append("off");
                msg.Send(kinectBackIP);
            }
            else
            {
                return;
            }
        }

        private void kinectIsAlive(int kinect)
        {
            string addr = "/kinect/" + kinect.ToString();

            if (kinect == 1)
            {
                OscMessage msg = new OscMessage(kinectFrontIP, addr); ;
                msg.Append("test");
                msg.Send(kinectFrontIP);
            }
            else if (kinect == 2)
            {
                OscMessage msg = new OscMessage(kinectBackIP, addr); ;
                msg.Append("test");
                msg.Send(kinectBackIP);
            }
            else
            {
                return;
            }
        }
        
        // Kinect Message Receive

        private void kinectMsgReceive(int kinect, OscMessage message)
        {
            string addr = "/kinect/" + kinect.ToString();

            if (message.Address == addr)
            {
                if (message.Data[0].ToString() == "ok")
                {
                    if (kinect == 1)
                    {
                        kinectFrontOK = true;
                    }
                    else if (kinect == 2)
                    {
                        kinectBackOK = true;
                    }
                    else
                    {
                        return;
                    }
                }

            }

        }

        // Remote Controller Message Send
        private void remoteIsAlive()
        {
            if (serial.IsOpen)
            { 
                char[] buf = {'t'};
                serial.Write(buf, 0, 1);
            }
        }

        // Remote Controller Message Receive
        private void remtoteMsgReceive(string message)
        {
            if (stringComparer.Equals("ok\r", message))
            {
                remoteConOK = true;
            }
            if (stringComparer.Equals("1\r", message))
            {
                Console.WriteLine("Success");
                
                OscMessage msg = new OscMessage(ipadIP, "/ipad");
                msg.Append("success");
                msg.Send(ipadIP);


                msg = new OscMessage(limboViewerIP, "/view");
                msg.Append("success");
                msg.Send(limboViewerIP);
                

            }

            if (stringComparer.Equals("2\r", message))
            {
                Console.WriteLine("Special Cmd");
            }

            if (stringComparer.Equals("3\r", message))
            {
                Console.WriteLine("Fail");
                OscMessage msg = new OscMessage(ipadIP, "/ipad");
                msg.Append("fail");
                msg.Send(ipadIP);


                msg = new OscMessage(limboViewerIP, "/view");
                msg.Append("fail");
                msg.Send(limboViewerIP);
            }

        }

        private void TestAllButton_Click(object sender, RoutedEventArgs e)
        {
       
            iPadIsAlive();
            limboViewerIsAlive();
            limboStandIsAlive();
            imageServerIsAlive();
            kinectIsAlive(kinectFront);
            kinectIsAlive(kinectBack);
            remoteIsAlive();
        }


        private void UIUpdateAsync()
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                frontStatus.Fill = new SolidColorBrush(Colors.Red);
            }));
        }
    }
}
