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
        private static int Port = 5103;
        private static readonly string oscCmd = "/limbo/cmd";

        OscMessage msg;
        private static IPEndPoint sourceEndPoint = new IPEndPoint(IPAddress.Loopback, Port);

        SerialPort serial = new SerialPort();

        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            statusIndicator.Fill = new SolidColorBrush(Colors.Red);

            sourceEndPoint.Address = IPAddress.Parse("192.168.0.114");
            sourceEndPoint.Port = Convert.ToInt32(12345);
            oscCmdServer = new OscServer(TransportType.Udp, IPAddress.Loopback, Port);
            oscCmdServer.FilterRegisteredMethods = false;
            //oscCmdServer.RegisterMethod(oscCmd);
            oscCmdServer.RegisterMethod(AliveMethod);
            oscCmdServer.RegisterMethod(TestMethod);
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
                ConsoleBlock.Text = string.Format("\nBundle Received [{0}:{1}]: Nested Bundles: {2} Nested Messages: {3}", bundle.SourceEndPoint.Address, bundle.TimeStamp, bundle.Bundles.Count, bundle.Messages.Count); // 해당 소스
                statusIndicator.Fill = new SolidColorBrush(Colors.Green);
            }));
            //ConsoleBlock.Text = string.Format("\nBundle Received [{0}:{1}]: Nested Bundles: {2} Nested Messages: {3}", bundle.SourceEndPoint.Address, bundle.TimeStamp, bundle.Bundles.Count, bundle.Messages.Count);
            //Console.WriteLine(string.Format("\nBundle Received [{0}:{1}]: Nested Bundles: {2} Nested Messages: {3}", bundle.SourceEndPoint.Address, bundle.TimeStamp, bundle.Bundles.Count, bundle.Messages.Count));
            //Console.WriteLine("Total Bundles Received: {0}", sBundlesReceivedCount);
            
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
            msg = new OscMessage(sourceEndPoint, oscCmd);
            msg.Append((float)1.0);
            msg.Append((float)4.0);
            msg.Send(sourceEndPoint);

        }
	
    }
}
