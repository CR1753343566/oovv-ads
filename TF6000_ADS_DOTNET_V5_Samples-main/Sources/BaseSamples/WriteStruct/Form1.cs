using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using TwinCAT.Ads;

namespace S13_WriteStruct
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button btnWrite;
		private System.Windows.Forms.TextBox tbInt;
		private System.Windows.Forms.TextBox tbDint;
		private System.Windows.Forms.TextBox tbByte;
		private System.Windows.Forms.TextBox tbLReal;
		private System.Windows.Forms.TextBox tbReal;

		private System.ComponentModel.Container components = null;

		private uint hVar;
		private AdsClient tcClient;

		public Form1()
		{
			InitializeComponent();			
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

        /// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
        {
            tbInt = new TextBox();
            tbDint = new TextBox();
            tbByte = new TextBox();
            tbLReal = new TextBox();
            tbReal = new TextBox();
            groupBox1 = new GroupBox();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            btnWrite = new Button();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // tbInt
            // 
            tbInt.Location = new Point(86, 39);
            tbInt.Name = "tbInt";
            tbInt.Size = new Size(120, 23);
            tbInt.TabIndex = 0;
            tbInt.Text = "1000";
            // 
            // tbDint
            // 
            tbDint.Location = new Point(86, 79);
            tbDint.Name = "tbDint";
            tbDint.Size = new Size(120, 23);
            tbDint.TabIndex = 1;
            tbDint.Text = "10000";
            // 
            // tbByte
            // 
            tbByte.Location = new Point(86, 118);
            tbByte.Name = "tbByte";
            tbByte.Size = new Size(120, 23);
            tbByte.TabIndex = 2;
            tbByte.Text = "100";
            // 
            // tbLReal
            // 
            tbLReal.Location = new Point(86, 158);
            tbLReal.Name = "tbLReal";
            tbLReal.Size = new Size(120, 23);
            tbLReal.TabIndex = 3;
            tbLReal.Text = "3,145";
            // 
            // tbReal
            // 
            tbReal.Location = new Point(86, 197);
            tbReal.Name = "tbReal";
            tbReal.Size = new Size(120, 23);
            tbReal.TabIndex = 4;
            tbReal.Text = "3,14";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(tbByte);
            groupBox1.Controls.Add(tbDint);
            groupBox1.Controls.Add(tbInt);
            groupBox1.Controls.Add(tbLReal);
            groupBox1.Controls.Add(tbReal);
            groupBox1.Location = new Point(19, 10);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(231, 246);
            groupBox1.TabIndex = 5;
            groupBox1.TabStop = false;
            groupBox1.Text = "PLCStruct";
            // 
            // label5
            // 
            label5.Location = new Point(10, 197);
            label5.Name = "label5";
            label5.Size = new Size(57, 28);
            label5.TabIndex = 9;
            label5.Text = "realVal :";
            // 
            // label4
            // 
            label4.Location = new Point(10, 158);
            label4.Name = "label4";
            label4.Size = new Size(57, 28);
            label4.TabIndex = 8;
            label4.Text = "lrealVal :";
            // 
            // label3
            // 
            label3.Location = new Point(10, 118);
            label3.Name = "label3";
            label3.Size = new Size(57, 28);
            label3.TabIndex = 7;
            label3.Text = "byteVal :";
            // 
            // label2
            // 
            label2.Location = new Point(10, 79);
            label2.Name = "label2";
            label2.Size = new Size(57, 28);
            label2.TabIndex = 6;
            label2.Text = "dintVal :";
            // 
            // label1
            // 
            label1.Location = new Point(10, 39);
            label1.Name = "label1";
            label1.Size = new Size(57, 28);
            label1.TabIndex = 5;
            label1.Text = "intVal :";
            // 
            // btnWrite
            // 
            btnWrite.Location = new Point(19, 266);
            btnWrite.Name = "btnWrite";
            btnWrite.Size = new Size(231, 29);
            btnWrite.TabIndex = 6;
            btnWrite.Text = "Write";
            btnWrite.Click += btnWrite_Click;
            // 
            // Form1
            // 
            AutoScaleBaseSize = new Size(6, 16);
            ClientSize = new Size(409, 496);
            Controls.Add(btnWrite);
            Controls.Add(groupBox1);
            Name = "Form1";
            Text = "Sample13";
            Closing += Form1_Closing;
            Load += Form1_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);

        }

        /// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new Form1());
		}

        private void Form1_Load(object sender, System.EventArgs e)
		{
			try
			{
                tcClient = new AdsClient();
                tcClient.Connect(851);	
				hVar = tcClient.CreateVariableHandle("MAIN.PLCStruct");
			}
			catch(Exception err)
			{
				MessageBox.Show(err.Message);
			}
		}

		private void btnWrite_Click(object sender, System.EventArgs e)
		{
			MemoryStream dataStream = new MemoryStream(32);
			BinaryWriter binWrite = new BinaryWriter(dataStream);

			dataStream.Position = 0;
			try
			{
				// Adjust datastream.position for 8 byte-alignment

                binWrite.Write(short.Parse(tbInt.Text));
                dataStream.Position = 4;
				binWrite.Write(int.Parse(tbDint.Text));
                dataStream.Position = 8;
				binWrite.Write(byte.Parse(tbByte.Text));
                dataStream.Position = 16;
				binWrite.Write(double.Parse(tbLReal.Text));
                dataStream.Position = 24;
				binWrite.Write(float.Parse(tbReal.Text));

				tcClient.Write(hVar,dataStream.GetBuffer().AsMemory());
			}
			catch( Exception err)
			{
				MessageBox.Show(err.Message);
			}
		}

		private void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			//Resourcen wieder freigeben
			try
			{
				tcClient.DeleteVariableHandle(hVar);					
			}
			catch(Exception err)
			{
				MessageBox.Show(err.Message);
			}
			tcClient.Dispose();	
		}
    }
}
