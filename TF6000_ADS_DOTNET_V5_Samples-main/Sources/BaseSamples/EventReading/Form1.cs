using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using TwinCAT.Ads;
using System.Threading;
using TwinCAT.TypeSystem;
using System.Text;
using System.Buffers.Binary;
using System.Threading.Tasks;
using TwinCAT.Ads.TypeSystem;
using TwinCAT;
using System.Collections.Generic;

namespace S03_EventReading
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public class Form1 : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox tbInt;
        private System.Windows.Forms.TextBox tbDint;
        private System.Windows.Forms.TextBox tbSint;
        private System.Windows.Forms.TextBox tbLreal;
        private System.Windows.Forms.TextBox tbReal;
        private System.Windows.Forms.TextBox tbString;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        private AdsClient _client;
        private uint[] hConnect;
        //private AdsStream dataStream;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox tbBool;
        //private AdsBinaryReader binRead;

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tbInt = new TextBox();
            tbDint = new TextBox();
            tbSint = new TextBox();
            tbLreal = new TextBox();
            tbReal = new TextBox();
            tbString = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            label9 = new Label();
            label10 = new Label();
            label11 = new Label();
            tbBool = new TextBox();
            SuspendLayout();
            // 
            // tbInt
            // 
            tbInt.Location = new Point(125, 59);
            tbInt.Name = "tbInt";
            tbInt.Size = new Size(367, 23);
            tbInt.TabIndex = 0;
            // 
            // tbDint
            // 
            tbDint.Location = new Point(125, 98);
            tbDint.Name = "tbDint";
            tbDint.Size = new Size(367, 23);
            tbDint.TabIndex = 1;
            // 
            // tbSint
            // 
            tbSint.Location = new Point(125, 138);
            tbSint.Name = "tbSint";
            tbSint.Size = new Size(367, 23);
            tbSint.TabIndex = 2;
            // 
            // tbLreal
            // 
            tbLreal.Location = new Point(125, 177);
            tbLreal.Name = "tbLreal";
            tbLreal.Size = new Size(367, 23);
            tbLreal.TabIndex = 3;
            // 
            // tbReal
            // 
            tbReal.Location = new Point(125, 217);
            tbReal.Name = "tbReal";
            tbReal.Size = new Size(367, 23);
            tbReal.TabIndex = 4;
            // 
            // tbString
            // 
            tbString.Location = new Point(125, 256);
            tbString.Name = "tbString";
            tbString.Size = new Size(367, 23);
            tbString.TabIndex = 5;
            // 
            // label1
            // 
            label1.Location = new Point(10, 59);
            label1.Name = "label1";
            label1.Size = new Size(105, 28);
            label1.TabIndex = 6;
            label1.Text = "MAIN.intVal :";
            // 
            // label2
            // 
            label2.Location = new Point(10, 98);
            label2.Name = "label2";
            label2.Size = new Size(105, 29);
            label2.TabIndex = 7;
            label2.Text = "MAIN.dintVal :";
            // 
            // label3
            // 
            label3.Location = new Point(10, 138);
            label3.Name = "label3";
            label3.Size = new Size(105, 28);
            label3.TabIndex = 8;
            label3.Text = "MAIN.sintVal :";
            // 
            // label4
            // 
            label4.Location = new Point(10, 177);
            label4.Name = "label4";
            label4.Size = new Size(105, 29);
            label4.TabIndex = 9;
            label4.Text = "MAIN.lrealVal :";
            // 
            // label5
            // 
            label5.Location = new Point(10, 217);
            label5.Name = "label5";
            label5.Size = new Size(105, 28);
            label5.TabIndex = 10;
            label5.Text = "MAIN.realVal :";
            // 
            // label6
            // 
            label6.Location = new Point(10, 256);
            label6.Name = "label6";
            label6.Size = new Size(105, 28);
            label6.TabIndex = 11;
            label6.Text = "MAIN.stringVal :";
            // 
            // label7
            // 
            label7.Location = new Point(10, 177);
            label7.Name = "label7";
            label7.Size = new Size(86, 29);
            label7.TabIndex = 9;
            label7.Text = "label4";
            // 
            // label8
            // 
            label8.Location = new Point(10, 138);
            label8.Name = "label8";
            label8.Size = new Size(86, 28);
            label8.TabIndex = 8;
            label8.Text = "label3";
            // 
            // label9
            // 
            label9.Location = new Point(10, 59);
            label9.Name = "label9";
            label9.Size = new Size(86, 28);
            label9.TabIndex = 6;
            label9.Text = "label1";
            // 
            // label10
            // 
            label10.Location = new Point(10, 98);
            label10.Name = "label10";
            label10.Size = new Size(86, 29);
            label10.TabIndex = 7;
            label10.Text = "label2";
            // 
            // label11
            // 
            label11.Location = new Point(10, 20);
            label11.Name = "label11";
            label11.Size = new Size(105, 28);
            label11.TabIndex = 13;
            label11.Text = "MAIN.boolVal :";
            // 
            // tbBool
            // 
            tbBool.Location = new Point(125, 20);
            tbBool.Name = "tbBool";
            tbBool.Size = new Size(367, 23);
            tbBool.TabIndex = 12;
            // 
            // Form1
            // 
            AutoScaleBaseSize = new Size(6, 16);
            ClientSize = new Size(680, 494);
            Controls.Add(label11);
            Controls.Add(tbBool);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(tbString);
            Controls.Add(tbReal);
            Controls.Add(tbLreal);
            Controls.Add(tbSint);
            Controls.Add(tbDint);
            Controls.Add(tbInt);
            Controls.Add(label7);
            Controls.Add(label8);
            Controls.Add(label9);
            Controls.Add(label10);
            Name = "Form1";
            Text = "Form1";
            Closing += Form1_Closing;
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();

        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.Run(new Form1());
        }

        private void RegisterNotifications()
        {
            using (AdsClient client = new AdsClient())
            {
                // Add the Notification event handler
                client. AdsNotification += Client_AdsNotification;

                // Connect to target
                client.Connect(AmsNetId.Local, 851);
                uint notificationHandle = 0;

                try
                {
                    // Notification to a DINT Type (UINT32)
                    // Check for change every 200 ms

                    int size = sizeof(UInt32);
                    //byte[] notificationBuffer = new byte[sizeof(UInt32)];
                    
                    notificationHandle = client.AddDeviceNotification("MAIN.nCounter", size, new NotificationSettings(AdsTransMode.OnChange, 200, 0), null);
                    Thread.Sleep(5000); // Sleep the main thread to get some (asynchronous Notifications)
                }
                finally
                {
                    // Unregister the Event / Handle
                    client.DeleteDeviceNotification(notificationHandle);
                    client.AdsNotification -= Client_AdsNotification;
                }
            }
        }

        private void Client_AdsNotification(object sender, AdsNotificationEventArgs e)
        {
            // Or here we know about UDINT type --> can be marshalled as UINT32
            uint nCounter = BinaryPrimitives.ReadUInt32LittleEndian(e.Data.Span);

            // If Synchronization is needed (e.g. in Windows.Forms or WPF applications)
            // we could synchronize via SynchronizationContext into the UI Thread

            /*SynchronizationContext syncContext = SynchronizationContext.Current;
              _context.Post(status => someLabel.Text = nCounter.ToString(), null); // Non-blocking post */
        }

        private async Task RegisterNotificationsAsync()
        {
            CancellationToken cancel = CancellationToken.None;

            using (AdsClient client = new AdsClient())
            {
                // Add the Notification event handler
                client.AdsNotification += Client_AdsNotification2;

                // Connect to target
                client.Connect(AmsNetId.Local, 851);
                uint notificationHandle = 0;

                // Notification to a DINT Type (UINT32)
                // Check for change every 200 ms

                int size = sizeof(UInt32);

                ResultHandle result = await client.AddDeviceNotificationAsync("MAIN.nCounter", size, new NotificationSettings(AdsTransMode.OnChange, 200, 0), null, cancel);

                if (result.Succeeded)
                {
                    notificationHandle = result.Handle;
                    await Task.Delay(5000); // Wait asynchronously without blocking the UI Thread.
                                            // Unregister the Event / Handle
                    ResultAds result2 = await client.DeleteDeviceNotificationAsync(notificationHandle, cancel);
                }
                client.AdsNotification -= Client_AdsNotification2;
            }        
        }

        private void Client_AdsNotification2(object sender, AdsNotificationEventArgs e)
        {
            // Or here we know about UDINT type --> can be marshalled as UINT32
            uint nCounter = BinaryPrimitives.ReadUInt32LittleEndian(e.Data.Span);

            // If Synchronization is needed (e.g. in Windows.Forms or WPF applications)
            // we could synchronize via SynchronizationContext into the UI Thread

            /*SynchronizationContext syncContext = SynchronizationContext.Current;
              _context.Post(status => someLabel.Text = nCounter.ToString(), null); // Non-blocking post */
        }

        private async Task RegisterSumNotificationsAsync()
        {
            CancellationToken cancel = CancellationToken.None;

            using (AdsClient client = new AdsClient())
            {
                // Add the Notification event handler
                client.AdsSumNotification += Client_SumNotification;

                // Connect to target
                client.Connect(AmsNetId.Local, 851);
                uint notificationHandle = 0;

                // Notification to a DINT Type (UINT32)
                // Check for change every 200 ms

                ResultHandle result = await client.AddDeviceNotificationAsync("MAIN.nCounter", sizeof(UInt32), new NotificationSettings(AdsTransMode.OnChange, 200, 0), null, cancel);

                if (result.Succeeded)
                {
                    notificationHandle = result.Handle;
                    await Task.Delay(5000); // Wait asynchronously without blocking the UI Thread.
                                            // Unregister the Event / Handle
                    ResultAds result2 = await client.DeleteDeviceNotificationAsync(notificationHandle, cancel);
                }
                client.AdsNotification -= Client_AdsNotification2;
            }
        }

        private void Client_SumNotification(object sender, AdsSumNotificationEventArgs e)
        {
            // Timestamp of the Notification List
            DateTimeOffset dateTime = e.TimeStamp;

            // List of Raw ADS Notifications
            IList<Notification> notifications = e.Notifications;

            foreach(Notification notification in notifications)
            {
                // Notifications can be handled more efficiently, because they occur togeterh
                // handler and can be transformed/synchronized in one step compared to AdsClient.AdsNotifcation events.
            }
        }


        private void SymbolValueChanged()
        {
            using (AdsClient client = new AdsClient())
            {
                // Connect to target
                client.Connect(AmsNetId.Local, 851);
                Symbol symbol = null;

                try
                {
                    ISymbolLoader loader = SymbolLoaderFactory.Create(client, SymbolLoaderSettings.Default);
                    // DINT Type (UINT32)
                    symbol = (Symbol)loader.Symbols["MAIN.nCounter"];

                    // Set the Notification Settings of the Symbol if NotificationSettings.Default is not appropriate
                    // Check for change every 500 ms
                    symbol.NotificationSettings = new NotificationSettings(AdsTransMode.OnChange, 500, 0);

                    symbol.ValueChanged += Symbol_ValueChanged; // Registers the notification
                    Thread.Sleep(5000); // Sleep the main thread to get some (asynchronous Notifications)
                }
                finally
                {
                    // Unregister the Event and the underlying Handle
                    symbol.ValueChanged -= Symbol_ValueChanged; // Unregisters the notification
                }
            }
        }

        private void Symbol_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            Symbol symbol = (Symbol)e.Symbol;

            // Object Value can be cast to int automatically, because it is an Primitive Value (DINT --> Int32).
            // The Symbol information is used internally to cast the value to its appropriate .NET Type.
            int iVal = (int)e.Value;

            // If Synchronization is needed (e.g. in Windows.Forms or WPF applications)
            // we could synchronize via SynchronizationContext into the UI Thread
            
            /*SynchronizationContext syncContext = SynchronizationContext.Current;
              _context.Post(status => someLabel.Text = iVal.ToString(), null); // Non-blocking post */
        }

        SynchronizationContext _context = null;

        private void Form1_Load(object sender, System.EventArgs e)
        {
            // Get the WindowsFormSynchronizationContext.
            _context = SynchronizationContext.Current;

            // Create AdsClient instance
            _client = new AdsClient(); // Don't forget to dispose when finish using

            // Connection to Port 851 on the local system
            _client.Connect(851);
            hConnect = new uint[7];

            try
            {
                hConnect[0] = _client.AddDeviceNotification("MAIN.boolVal", 1,
                    new NotificationSettings(AdsTransMode.OnChange, 100, 0), tbBool);
                hConnect[1] = _client.AddDeviceNotification("MAIN.intVal", 2,
                    new NotificationSettings(AdsTransMode.OnChange, 100, 0), tbInt);
                hConnect[2] = _client.AddDeviceNotification("MAIN.dintVal", 4,
                    new NotificationSettings(AdsTransMode.OnChange, 100, 0), tbDint);
                hConnect[3] = _client.AddDeviceNotification("MAIN.sintVal", 1,
                    new NotificationSettings(AdsTransMode.OnChange, 100, 0), tbSint);
                hConnect[4] = _client.AddDeviceNotification("MAIN.lrealVal", 8,
                    new NotificationSettings(AdsTransMode.OnChange, 100, 0), tbLreal);
                hConnect[5] = _client.AddDeviceNotification("MAIN.realVal", 4,
                    new NotificationSettings(AdsTransMode.OnChange, 100, 0), tbReal);
                hConnect[6] = _client.AddDeviceNotification("MAIN.stringVal", 13,
                    new NotificationSettings(AdsTransMode.OnChange, 100, 0), tbString);

                _client.AdsNotification += new EventHandler<AdsNotificationEventArgs>(OnNotification);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

		private void OnNotification(object sender, AdsNotificationEventArgs e)
		{
            // The Notification appears in Background Thread
            DateTimeOffset time = e.TimeStamp;
            ReadOnlyMemory<byte> memory = e.Data;
            
			string strValue = "";

            if (e.Handle == hConnect[0])
                strValue = BitConverter.ToBoolean(memory.ToArray(), 0).ToString();
            else if (e.Handle == hConnect[1])
                strValue = BitConverter.ToInt16(memory.ToArray(), 0).ToString();
            else if (e.Handle == hConnect[2])
                strValue = BitConverter.ToInt32(memory.ToArray(), 0).ToString();
            else if (e.Handle == hConnect[3])
            {
                byte[] data = memory.ToArray();
                strValue = ((sbyte)data[0]).ToString();
            }
            else if (e.Handle == hConnect[4])
                strValue = BitConverter.ToDouble(memory.ToArray(), 0).ToString();
            else if (e.Handle == hConnect[5])
                strValue = BitConverter.ToSingle(memory.ToArray(), 0).ToString();
            else if (e.Handle == hConnect[6])
            {
                //strValue = new String(binRead.ReadChars(13));
                PrimitiveTypeMarshaler converter = new PrimitiveTypeMarshaler(StringMarshaler.DefaultEncoding);
                converter.Unmarshal(memory.Span, out strValue);
            }

            // Determine the TextBox
            TextBox textBox = (TextBox)e.UserData;
            string text = string.Format("DateTime: {0},{1}ms; {2}", time, time.Millisecond, strValue);

            // Synchronization to UI Thread.
            this.Invoke(new Action(() => textBox.Text = text));
		}

        private void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			try
			{
                if (_client != null)
                {
                    // Removing Notifications
                    _client.AdsNotification -= new EventHandler<AdsNotificationEventArgs>(OnNotification);
                    // Disposing the Client.
                    _client.Dispose();
                    _client = null;
                }
			}
			catch(Exception err)
			{
				MessageBox.Show(err.Message);
			}			
		}
    }
}
