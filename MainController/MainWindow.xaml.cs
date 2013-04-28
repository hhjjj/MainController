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
using Bespoke.Common;
using Bespoke.Common.Osc;

using System.Windows.Threading;

using System.IO.Ports;

using ExceptionEventArgs = Bespoke.Common.ExceptionEventArgs;

namespace MainController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        OscServer oscCmdServer;
        private static int Port = 8000;
        private static readonly string oscCmd = "/kinect/1";

        OscMessage msg;

        private static IPEndPoint kinectFrontIP = new IPEndPoint(IPAddress.Loopback, Port);
        private static IPEndPoint kinectBackIP = new IPEndPoint(IPAddress.Loopback, Port);
        private static IPEndPoint limboDisplayIP = new IPEndPoint(IPAddress.Loopback, Port);
        private static IPEndPoint imageServerIP = new IPEndPoint(IPAddress.Loopback, Port);
        private static IPEndPoint limboIP = new IPEndPoint(IPAddress.Loopback, Port);
        private static IPEndPoint ipadIP = new IPEndPoint(IPAddress.Loopback, Port);


        SerialPort serial = new SerialPort();
        string serialRxData;
        FlowDocument mcFlowDoc = new FlowDocument();
        Paragraph para = new Paragraph();

        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            comms_connect.Content = "Connect";

            portInput.Text = Port.ToString();
            kinectFrontIPInput.Text = IPAddress.Loopback.ToString();
            kinectBackIPInput.Text = IPAddress.Loopback.ToString();   
            limboViewerIPInput.Text = IPAddress.Loopback.ToString();       
            imageServerIPInput.Text= IPAddress.Loopback.ToString();  
            limboStandIPInput.Text  = IPAddress.Loopback.ToString();
            ipadIPInput.Text = IPAddress.Loopback.ToString();        




            statusIndicator.Fill = new SolidColorBrush(Colors.Red);

            //sourceEndPoint.Address = IPAddress.Parse("192.168.0.114");
            //sourceEndPoint.Port = Convert.ToInt32(12345);
            oscCmdServer = new OscServer(TransportType.Udp, IPAddress.Loopback, Port);
            oscCmdServer.FilterRegisteredMethods = false;
            //oscCmdServer.RegisterMethod(oscCmd);
            oscCmdServer.RegisterMethod(oscCmd);
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
           
            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                statusIndicator.Fill = new SolidColorBrush(Colors.Green);
            }));
            Console.WriteLine(string.Format("\nBundle Received [{0}:{1}]: Nested Bundles: {2} Nested Messages: {3}", bundle.SourceEndPoint.Address, bundle.TimeStamp, bundle.Bundles.Count, bundle.Messages.Count));
            Console.WriteLine("Total Bundles Received: {0}", sBundlesReceivedCount);
            
        }

        private static void oscCmdServer_MessageReceived(object sender, OscMessageReceivedEventArgs e)
		{
            sMessagesReceivedCount++;

            OscMessage message = e.Message;

            Console.WriteLine(string.Format("\nMessage Received [{0}]: {1}", message.SourceEndPoint.Address, message.Address));
            Console.WriteLine(string.Format("Message contains {0} objects.", message.Data.Count));

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
        private static readonly string AliveMethod = "/osctest/alive";
        private static readonly string TestMethod = "/osctest/test";

        private static int sBundlesReceivedCount;
        private static int sMessagesReceivedCount;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //msg = new OscMessage(sourceEndPoint, oscCmd);
            //msg.Append((float)1.0);
            //msg.Append((float)4.0);
            //msg.Send(sourceEndPoint);

        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            msg = new OscMessage(kinectFrontIP, oscCmd);
            msg.Append("Stop");
            msg.Send(kinectFrontIP);
        }

        private void IPSetButton_Click(object sender, RoutedEventArgs e)
        {
            Port = Convert.ToInt32(portInput.Text);
            kinectFrontIP.Address = IPAddress.Parse(kinectFrontIPInput.Text);
            kinectFrontIP.Port = Port;
            kinectBackIP.Address = IPAddress.Parse(kinectBackIPInput.Text);
            kinectBackIP.Port = Port;
            limboDisplayIP.Address = IPAddress.Parse(limboViewerIPInput.Text);
            limboDisplayIP.Port = Port;
            imageServerIP.Address = IPAddress.Parse(imageServerIPInput.Text);
            imageServerIP.Port = Port;
            limboIP.Address = IPAddress.Parse(limboStandIPInput.Text);
            limboIP.Port = Port;
            ipadIP.Address = IPAddress.Parse(ipadIPInput.Text);
            ipadIP.Port = Port;

            

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
            serialRxData = serial.ReadExisting();
            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(showRxData), serialRxData);
        }
        private void showRxData(string text)
        {
            // Assign the value of the recieved_data to the RichTextBox.
            para.Inlines.Add(text);
            mcFlowDoc.Blocks.Add(para);
            commData.Document = mcFlowDoc;
        }


      
    }
}
