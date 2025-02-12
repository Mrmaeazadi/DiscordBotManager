namespace BotManager
{
    partial class RenameForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            mainPanel = new Panel();
            okButton = new Button();
            cancelButton = new Button();
            newNameTextBox = new TextBox();
            newNameLabel = new Label();
            mainPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainPanel
            // 
            mainPanel.BackColor = Color.FromArgb(14, 14, 14);
            mainPanel.Controls.Add(okButton);
            mainPanel.Controls.Add(cancelButton);
            mainPanel.Controls.Add(newNameTextBox);
            mainPanel.Controls.Add(newNameLabel);
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.Location = new Point(0, 0);
            mainPanel.Name = "mainPanel";
            mainPanel.Size = new Size(316, 186);
            mainPanel.TabIndex = 0;
            mainPanel.MouseDown += MainPanel_MouseDown;
            mainPanel.MouseMove += MainPanel_MouseMove;
            // 
            // okButton
            // 
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(50, 108);
            okButton.Name = "okButton";
            okButton.Size = new Size(75, 23);
            okButton.TabIndex = 3;
            okButton.Text = "تایید";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += OkButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(194, 108);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(75, 23);
            cancelButton.TabIndex = 2;
            cancelButton.Text = "لغو";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += CancelButton_Click;
            // 
            // newNameTextBox
            // 
            newNameTextBox.Location = new Point(12, 39);
            newNameTextBox.Name = "newNameTextBox";
            newNameTextBox.Size = new Size(292, 23);
            newNameTextBox.TabIndex = 1;
            // 
            // newNameLabel
            // 
            newNameLabel.AutoSize = true;
            newNameLabel.Font = new Font("Vazirmatn", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            newNameLabel.ForeColor = Color.White;
            newNameLabel.Location = new Point(86, 9);
            newNameLabel.Name = "newNameLabel";
            newNameLabel.RightToLeft = RightToLeft.Yes;
            newNameLabel.Size = new Size(146, 27);
            newNameLabel.TabIndex = 0;
            newNameLabel.Text = "نام جدید را وارد کنید:";
            // 
            // RenameForm
            // 
            AcceptButton = okButton;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(316, 186);
            ControlBox = false;
            Controls.Add(mainPanel);
            FormBorderStyle = FormBorderStyle.None;
            Name = "RenameForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "NewRenameForm";
            TopMost = true;
            mainPanel.ResumeLayout(false);
            mainPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel mainPanel;
        private Button okButton;
        private Button cancelButton;
        private TextBox newNameTextBox;
        private Label newNameLabel;
    }
}