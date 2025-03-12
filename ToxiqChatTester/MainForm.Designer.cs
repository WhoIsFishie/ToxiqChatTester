namespace ToxiqChatTester
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lstConversations = new ListBox();
            txtChat = new TextBox();
            txtMessage = new TextBox();
            btnSend = new Button();
            lblStatus = new Label();
            btnNewConversation = new Button();
            splitContainer1 = new SplitContainer();
            label1 = new Label();
            panel1 = new Panel();
            button1 = new Button();
            button2 = new Button();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // lstConversations
            // 
            lstConversations.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lstConversations.FormattingEnabled = true;
            lstConversations.ItemHeight = 15;
            lstConversations.Location = new Point(3, 28);
            lstConversations.Name = "lstConversations";
            lstConversations.Size = new Size(194, 379);
            lstConversations.TabIndex = 0;
            lstConversations.SelectedIndexChanged += lstConversations_SelectedIndexChanged;
            // 
            // txtChat
            // 
            txtChat.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtChat.BackColor = SystemColors.Window;
            txtChat.Location = new Point(3, 3);
            txtChat.Multiline = true;
            txtChat.Name = "txtChat";
            txtChat.ReadOnly = true;
            txtChat.ScrollBars = ScrollBars.Vertical;
            txtChat.Size = new Size(493, 367);
            txtChat.TabIndex = 1;
            // 
            // txtMessage
            // 
            txtMessage.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtMessage.Location = new Point(3, 377);
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new Size(411, 23);
            txtMessage.TabIndex = 2;
            // 
            // btnSend
            // 
            btnSend.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSend.Location = new Point(421, 376);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(75, 23);
            btnSend.TabIndex = 3;
            btnSend.Text = "Send";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.Location = new Point(212, 424);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(198, 23);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "Not connected";
            // 
            // btnNewConversation
            // 
            btnNewConversation.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnNewConversation.Location = new Point(12, 424);
            btnNewConversation.Name = "btnNewConversation";
            btnNewConversation.Size = new Size(194, 23);
            btnNewConversation.TabIndex = 5;
            btnNewConversation.Text = "New Conversation";
            btnNewConversation.UseVisualStyleBackColor = true;
            btnNewConversation.Click += btnNewConversation_Click;
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.Location = new Point(12, 12);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(label1);
            splitContainer1.Panel1.Controls.Add(lstConversations);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(panel1);
            splitContainer1.Size = new Size(705, 413);
            splitContainer1.SplitterDistance = 200;
            splitContainer1.TabIndex = 6;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 9);
            label1.Name = "label1";
            label1.Size = new Size(85, 15);
            label1.TabIndex = 6;
            label1.Text = "Conversations:";
            // 
            // panel1
            // 
            panel1.Controls.Add(txtChat);
            panel1.Controls.Add(txtMessage);
            panel1.Controls.Add(btnSend);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(501, 413);
            panel1.TabIndex = 4;
            // 
            // button1
            // 
            button1.Location = new Point(561, 431);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 7;
            button1.Text = "Inspect JWT Token";
            button1.UseVisualStyleBackColor = true;
            button1.Click += OnInspectToken;
            // 
            // button2
            // 
            button2.Location = new Point(642, 431);
            button2.Name = "button2";
            button2.Size = new Size(75, 23);
            button2.TabIndex = 8;
            button2.Text = "Test Connection";
            button2.UseVisualStyleBackColor = true;
            button2.Click += OnTestConnection;
            // 
            // MainForm
            // 
            AcceptButton = btnSend;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(729, 460);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(splitContainer1);
            Controls.Add(lblStatus);
            Controls.Add(btnNewConversation);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Toxiq Chat Tester";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Button button1;
        private Button button2;
    }
}
