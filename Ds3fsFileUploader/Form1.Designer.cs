namespace Ds3fsFileUploader
{
    partial class FrmMain
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            openFileDialog1      = new System.Windows.Forms.OpenFileDialog();
            btChoiceFolder       = new System.Windows.Forms.Button();
            folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            label1               = new System.Windows.Forms.Label();
            tb_SourceFolder      = new System.Windows.Forms.TextBox();
            label2               = new System.Windows.Forms.Label();
            tb_Destination       = new System.Windows.Forms.TextBox();
            btGetFileList        = new System.Windows.Forms.Button();
            progressBar1         = new System.Windows.Forms.ProgressBar();
            label3               = new System.Windows.Forms.Label();
            label4               = new System.Windows.Forms.Label();
            tb_BaseUrlApi        = new System.Windows.Forms.TextBox();
            tb_BucketName        = new System.Windows.Forms.TextBox();
            label5               = new System.Windows.Forms.Label();
            tb_BaseUrlKeycloak   = new System.Windows.Forms.TextBox();
            label6               = new System.Windows.Forms.Label();
            tb_Realm             = new System.Windows.Forms.TextBox();
            label7               = new System.Windows.Forms.Label();
            tb_UserName          = new System.Windows.Forms.TextBox();
            label8               = new System.Windows.Forms.Label();
            tb_Password          = new System.Windows.Forms.TextBox();
            label9               = new System.Windows.Forms.Label();
            tb_ClientId          = new System.Windows.Forms.TextBox();
            label10              = new System.Windows.Forms.Label();
            tb_GrantType         = new System.Windows.Forms.TextBox();
            label11              = new System.Windows.Forms.Label();
            tb_ClientSecret      = new System.Windows.Forms.TextBox();
            label12              = new System.Windows.Forms.Label();
            label13              = new System.Windows.Forms.Label();
            groupBoxCopyProcess  = new System.Windows.Forms.GroupBox();
            label16              = new System.Windows.Forms.Label();
            label17              = new System.Windows.Forms.Label();
            btSaveSettings       = new System.Windows.Forms.Button();
            pnlFileSlots         = new System.Windows.Forms.FlowLayoutPanel();
            SuspendLayout();
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // btChoiceFolder
            // 
            btChoiceFolder.Location                =  new System.Drawing.Point(228, 242);
            btChoiceFolder.Margin                  =  new System.Windows.Forms.Padding(2, 1, 2, 1);
            btChoiceFolder.Name                    =  "btChoiceFolder";
            btChoiceFolder.Size                    =  new System.Drawing.Size(98, 23);
            btChoiceFolder.TabIndex                =  0;
            btChoiceFolder.Text                    =  "Выбор папки";
            btChoiceFolder.UseVisualStyleBackColor =  true;
            btChoiceFolder.Click                   += btChoiceFolder_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(22, 227);
            label1.Margin   = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name     = "label1";
            label1.Size     = new System.Drawing.Size(64, 15);
            label1.TabIndex = 1;
            label1.Text     = "Источник:";
            // 
            // tb_SourceFolder
            // 
            tb_SourceFolder.BackColor = System.Drawing.SystemColors.Window;
            tb_SourceFolder.Enabled   = false;
            tb_SourceFolder.Location  = new System.Drawing.Point(22, 243);
            tb_SourceFolder.Margin    = new System.Windows.Forms.Padding(2, 1, 2, 1);
            tb_SourceFolder.Name      = "tb_SourceFolder";
            tb_SourceFolder.ReadOnly  = true;
            tb_SourceFolder.Size      = new System.Drawing.Size(200, 23);
            tb_SourceFolder.TabIndex  = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(139, 376);
            label2.Margin   = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label2.Name     = "label2";
            label2.Size     = new System.Drawing.Size(176, 15);
            label2.TabIndex = 3;
            label2.Text     = "Куда В Бакете (folder1/folder2/)";
            // 
            // tb_Destination
            // 
            tb_Destination.Location = new System.Drawing.Point(139, 394);
            tb_Destination.Margin   = new System.Windows.Forms.Padding(2, 1, 2, 1);
            tb_Destination.Name     = "tb_Destination";
            tb_Destination.Size     = new System.Drawing.Size(183, 23);
            tb_Destination.TabIndex = 4;
            // 
            // btGetFileList
            // 
            btGetFileList.BackColor               =  System.Drawing.Color.LightGreen;
            btGetFileList.Enabled                 =  false;
            btGetFileList.Location                =  new System.Drawing.Point(337, 377);
            btGetFileList.Margin                  =  new System.Windows.Forms.Padding(2, 1, 2, 1);
            btGetFileList.Name                    =  "btGetFileList";
            btGetFileList.Size                    =  new System.Drawing.Size(112, 57);
            btGetFileList.TabIndex                =  5;
            btGetFileList.Text                    =  "Копировать в FS";
            btGetFileList.UseVisualStyleBackColor =  false;
            btGetFileList.Click                   += btGetFileList_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(22, 332);
            label4.Margin   = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label4.Name     = "label4";
            label4.Size     = new System.Drawing.Size(225, 15);
            label4.TabIndex = 10;
            label4.Text     = "URL api ФХ (http://localhost:7076/api/v1)";
            // 
            // tb_BaseUrlApi
            // 
            tb_BaseUrlApi.Location = new System.Drawing.Point(21, 350);
            tb_BaseUrlApi.Name     = "tb_BaseUrlApi";
            tb_BaseUrlApi.Size     = new System.Drawing.Size(301, 23);
            tb_BaseUrlApi.TabIndex = 11;
            // 
            // tb_BucketName
            // 
            tb_BucketName.Location = new System.Drawing.Point(20, 394);
            tb_BucketName.Name     = "tb_BucketName";
            tb_BucketName.Size     = new System.Drawing.Size(114, 23);
            tb_BucketName.TabIndex = 13;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(21, 376);
            label5.Margin   = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label5.Name     = "label5";
            label5.Size     = new System.Drawing.Size(98, 15);
            label5.TabIndex = 12;
            label5.Text     = "Название Бакета";
            // 
            // tb_BaseUrlKeycloak
            // 
            tb_BaseUrlKeycloak.Location = new System.Drawing.Point(22, 54);
            tb_BaseUrlKeycloak.Name     = "tb_BaseUrlKeycloak";
            tb_BaseUrlKeycloak.Size     = new System.Drawing.Size(200, 23);
            tb_BaseUrlKeycloak.TabIndex = 15;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(22, 36);
            label6.Margin   = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label6.Name     = "label6";
            label6.Size     = new System.Drawing.Size(199, 15);
            label6.TabIndex = 14;
            label6.Text     = "URL Keycloak (http://localhost:8087)";
            // 
            // tb_Realm
            // 
            tb_Realm.Location = new System.Drawing.Point(227, 54);
            tb_Realm.Name     = "tb_Realm";
            tb_Realm.Size     = new System.Drawing.Size(95, 23);
            tb_Realm.TabIndex = 17;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(227, 36);
            label7.Margin   = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label7.Name     = "label7";
            label7.Size     = new System.Drawing.Size(40, 15);
            label7.TabIndex = 16;
            label7.Text     = "Realm";
            // 
            // tb_UserName
            // 
            tb_UserName.Location = new System.Drawing.Point(21, 98);
            tb_UserName.Name     = "tb_UserName";
            tb_UserName.Size     = new System.Drawing.Size(151, 23);
            tb_UserName.TabIndex = 19;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(22, 80);
            label8.Margin   = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label8.Name     = "label8";
            label8.Size     = new System.Drawing.Size(62, 15);
            label8.TabIndex = 18;
            label8.Text     = "UserName";
            // 
            // tb_Password
            // 
            tb_Password.Location = new System.Drawing.Point(177, 98);
            tb_Password.Name     = "tb_Password";
            tb_Password.Size     = new System.Drawing.Size(145, 23);
            tb_Password.TabIndex = 21;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(177, 80);
            label9.Margin   = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label9.Name     = "label9";
            label9.Size     = new System.Drawing.Size(57, 15);
            label9.TabIndex = 20;
            label9.Text     = "Password";
            // 
            // tb_ClientId
            // 
            tb_ClientId.Location = new System.Drawing.Point(21, 142);
            tb_ClientId.Name     = "tb_ClientId";
            tb_ClientId.Size     = new System.Drawing.Size(91, 23);
            tb_ClientId.TabIndex = 23;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(21, 124);
            label10.Margin   = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label10.Name     = "label10";
            label10.Size     = new System.Drawing.Size(51, 15);
            label10.TabIndex = 22;
            label10.Text     = "Client Id";
            // 
            // tb_GrantType
            // 
            tb_GrantType.Location = new System.Drawing.Point(117, 142);
            tb_GrantType.Name     = "tb_GrantType";
            tb_GrantType.Size     = new System.Drawing.Size(91, 23);
            tb_GrantType.TabIndex = 25;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(117, 124);
            label11.Margin   = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label11.Name     = "label11";
            label11.Size     = new System.Drawing.Size(63, 15);
            label11.TabIndex = 24;
            label11.Text     = "Grant Type";
            // 
            // tb_ClientSecret
            // 
            tb_ClientSecret.Location = new System.Drawing.Point(213, 142);
            tb_ClientSecret.Name     = "tb_ClientSecret";
            tb_ClientSecret.Size     = new System.Drawing.Size(109, 23);
            tb_ClientSecret.TabIndex = 27;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new System.Drawing.Point(213, 124);
            label12.Margin   = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label12.Name     = "label12";
            label12.Size     = new System.Drawing.Size(73, 15);
            label12.TabIndex = 26;
            label12.Text     = "Client Secret";
            // 
            // label13
            // 
            label13.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            label13.Location    = new System.Drawing.Point(12, 9);
            label13.Name        = "label13";
            label13.Size        = new System.Drawing.Size(320, 178);
            label13.TabIndex    = 0;
            label13.Text        = "Настройки Авторизации";
            // 
            // groupBoxCopyProcess
            // 
            groupBoxCopyProcess.Controls.Add(label3);
            groupBoxCopyProcess.Controls.Add(progressBar1);
            groupBoxCopyProcess.Controls.Add(pnlFileSlots);
            groupBoxCopyProcess.Location = new System.Drawing.Point(11, 470);
            groupBoxCopyProcess.Name = "groupBoxCopyProcess";
            groupBoxCopyProcess.Size = new System.Drawing.Size(438, 300);
            groupBoxCopyProcess.TabIndex = 28;
            groupBoxCopyProcess.Text = "Процесс копирования:";
            groupBoxCopyProcess.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            // 
            // progressBar1
            // 
            progressBar1.Location = new System.Drawing.Point(21, 20);
            progressBar1.Margin   = new System.Windows.Forms.Padding(2, 1, 2, 1);
            progressBar1.Name     = "progressBar1";
            progressBar1.Size     = new System.Drawing.Size(395, 22);
            progressBar1.Step     = 1;
            progressBar1.TabIndex = 8;
            progressBar1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(22, 5);
            label3.Margin   = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label3.Name     = "label3";
            label3.Size     = new System.Drawing.Size(38, 15);
            label3.TabIndex = 9;
            label3.Text     = "label3";
            label3.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            // 
            // pnlFileSlots
            // 
            pnlFileSlots.AutoScroll = true;
            pnlFileSlots.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            pnlFileSlots.Location = new System.Drawing.Point(21, 48);
            pnlFileSlots.Name = "pnlFileSlots";
            pnlFileSlots.Size = new System.Drawing.Size(395, 240);
            pnlFileSlots.TabIndex = 30;
            pnlFileSlots.WrapContents = false;
            pnlFileSlots.Dock = DockStyle.Bottom;
            pnlFileSlots.Padding = new Padding(5);
            // 
            // btSaveSettings
            // 
            btSaveSettings.Location                =  new System.Drawing.Point(337, 308);
            btSaveSettings.Name                    =  "btSaveSettings";
            btSaveSettings.Size                    =  new System.Drawing.Size(112, 65);
            btSaveSettings.TabIndex                =  29;
            btSaveSettings.Text                    =  "Сохранить настройки";
            btSaveSettings.UseVisualStyleBackColor =  true;
            btSaveSettings.Click                   += btSaveSettings_Click;
            // 
            // label16
            // 
            label16.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            label16.Location    = new System.Drawing.Point(12, 203);
            label16.Name        = "label16";
            label16.Size        = new System.Drawing.Size(321, 92);
            label16.TabIndex    = 32;
            label16.Text        = "Откуда:";
            // 
            // label17
            // 
            label17.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            label17.Location    = new System.Drawing.Point(11, 308);
            label17.Name        = "label17";
            label17.Size        = new System.Drawing.Size(321, 126);
            label17.TabIndex    = 33;
            label17.Text        = "Куда:";
            // 
            // labelElapsedTime
            // 
            labelElapsedTime          = new System.Windows.Forms.Label();
            labelElapsedTime.AutoSize = true;
            labelElapsedTime.Location = new System.Drawing.Point(337, 435);
            labelElapsedTime.Name     = "labelElapsedTime";
            labelElapsedTime.Size     = new System.Drawing.Size(49, 15);
            labelElapsedTime.TabIndex = 34;
            labelElapsedTime.Text     = "00:00:00";
            // 
            // timerElapsed
            // 
            timerElapsed          = new System.Windows.Forms.Timer();
            timerElapsed.Interval = 1000;
            timerElapsed.Tick += timerElapsed_Tick;
            // 
            // FrmMain
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(457, 630);
            Controls.Add(tb_ClientSecret);
            Controls.Add(label12);
            Controls.Add(tb_GrantType);
            Controls.Add(label11);
            Controls.Add(tb_ClientId);
            Controls.Add(label10);
            Controls.Add(tb_Password);
            Controls.Add(label9);
            Controls.Add(tb_UserName);
            Controls.Add(label8);
            Controls.Add(tb_Realm);
            Controls.Add(label7);
            Controls.Add(tb_BaseUrlKeycloak);
            Controls.Add(label6);
            Controls.Add(tb_BucketName);
            Controls.Add(label5);
            Controls.Add(tb_BaseUrlApi);
            Controls.Add(label4);
            Controls.Add(btGetFileList);
            Controls.Add(tb_Destination);
            Controls.Add(label2);
            Controls.Add(tb_SourceFolder);
            Controls.Add(label1);
            Controls.Add(btChoiceFolder);
            Controls.Add(label13);
            Controls.Add(btSaveSettings);
            Controls.Add(groupBoxCopyProcess);
            Controls.Add(label16);
            Controls.Add(label17);
            Controls.Add(labelElapsedTime);
            Margin      = new System.Windows.Forms.Padding(2, 1, 2, 1);
            MaximizeBox = false;
            Text        = "Копирование папки в FS";
            ResumeLayout(false);
            PerformLayout();
        }

        private void InitializeEventHandlers()
        {
            tb_BaseUrlApi.TextChanged      += (s, e) => UpdateUrls();
            tb_BucketName.TextChanged      += (s, e) => UpdateUrls();
            tb_BaseUrlKeycloak.TextChanged += (s, e) => UpdateUrls();
            tb_Realm.TextChanged           += (s, e) => UpdateUrls();
        }

        private System.Windows.Forms.Label label17;

        private System.Windows.Forms.Label label16;

        private System.Windows.Forms.GroupBox groupBoxCopyProcess;

        private System.Windows.Forms.Label label13;

        private System.Windows.Forms.TextBox tb_ClientId;
        private System.Windows.Forms.Label   label10;
        private System.Windows.Forms.TextBox tb_GrantType;
        private System.Windows.Forms.Label   label11;
        private System.Windows.Forms.TextBox tb_ClientSecret;
        private System.Windows.Forms.Label   label12;

        private System.Windows.Forms.TextBox tb_UserName;
        private System.Windows.Forms.Label   label8;
        private System.Windows.Forms.TextBox tb_Password;
        private System.Windows.Forms.Label   label9;

        private System.Windows.Forms.TextBox tb_Realm;
        private System.Windows.Forms.Label   label7;

        private System.Windows.Forms.TextBox tb_BaseUrlKeycloak;
        private System.Windows.Forms.Label   label6;

        private System.Windows.Forms.TextBox tb_BucketName;
        private System.Windows.Forms.Label   label5;

        private System.Windows.Forms.Label   label4;
        private System.Windows.Forms.TextBox tb_BaseUrlApi;

        #endregion

        private OpenFileDialog                   openFileDialog1;
        private System.Windows.Forms.Button      btChoiceFolder;
        private FolderBrowserDialog              folderBrowserDialog1;
        private System.Windows.Forms.Label       label1;
        private System.Windows.Forms.TextBox     tb_SourceFolder;
        private System.Windows.Forms.Label       label2;
        private System.Windows.Forms.TextBox     tb_Destination;
        private System.Windows.Forms.Button      btGetFileList;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label       label3;
        private System.Windows.Forms.Label       labelElapsedTime;
        private System.Windows.Forms.Timer       timerElapsed;
        
        private System.Windows.Forms.Button      btSaveSettings;
        private FlowLayoutPanel                  pnlFileSlots;

        private void UpdateStartStopButton(bool isRunning)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(UpdateStartStopButton), isRunning);
                return;
            }

            if (isRunning)
            {
                // Режим "Остановить"
                btGetFileList.Text      = "Остановить";
                btGetFileList.BackColor = Color.LightCoral;
                btGetFileList.ForeColor = Color.Black;
            }
            else
            {
                // Режим "Запустить"
                btGetFileList.Text      = "Копировать в FS";
                btGetFileList.BackColor = Color.LightGreen;
                btGetFileList.ForeColor = Color.Black;
            }

            btGetFileList.Refresh();
        }
    }
}
