namespace S12_ReadArray
{
    public partial class Form1
{
        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lbArray = new System.Windows.Forms.ListBox();
            btnRead = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // lbArray
            // 
            lbArray.ItemHeight = 17;
            lbArray.Location = new System.Drawing.Point(19, 10);
            lbArray.Name = "lbArray";
            lbArray.Size = new System.Drawing.Size(173, 242);
            lbArray.TabIndex = 0;
            lbArray.SelectedIndexChanged += lbArray_SelectedIndexChanged;
            // 
            // btnRead
            // 
            btnRead.Location = new System.Drawing.Point(19, 295);
            btnRead.Name = "btnRead";
            btnRead.Size = new System.Drawing.Size(173, 28);
            btnRead.TabIndex = 1;
            btnRead.Text = "Read";
            btnRead.Click += btnRead_Click;
            // 
            // Form1
            // 
            AutoScaleBaseSize = new System.Drawing.Size(6, 16);
            ClientSize = new System.Drawing.Size(447, 491);
            Controls.Add(btnRead);
            Controls.Add(lbArray);
            Name = "Form1";
            Text = "Sample12";
            Load += Form1_Load;
            ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.Button btnRead;
    private System.ComponentModel.Container components = null;
    private System.Windows.Forms.ListBox lbArray;
}
}
