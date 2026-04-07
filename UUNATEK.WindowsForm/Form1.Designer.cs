namespace UUNATEK.WindowsForm
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            grpConnection = new GroupBox();
            lblComPort = new Label();
            txtComPort = new TextBox();
            lblBaudRate = new Label();
            txtBaudRate = new TextBox();
            btnConnect = new Button();
            grpStatus = new GroupBox();
            lblIsOpen = new Label();
            lblPortName = new Label();
            lblIsPrinting = new Label();
            btnGetStatus = new Button();
            tabControl = new TabControl();
            tabBasicPrint = new TabPage();
            grpSimulation = new GroupBox();
            lblPaper = new Label();
            cboPaper = new ComboBox();
            btnBrowse = new Button();
            lblFileName = new Label();
            picSimulation = new PictureBox();
            grpPrintSettings = new GroupBox();
            lblXPosition = new Label();
            txtXPosition = new TextBox();
            lblYPosition = new Label();
            txtYPosition = new TextBox();
            lblScale = new Label();
            nudScale = new NumericUpDown();
            lblRotation = new Label();
            nudRotation = new NumericUpDown();
            chkInvertX = new CheckBox();
            chkInvertY = new CheckBox();
            btnPrint = new Button();
            btnGenerateGCode = new Button();
            txtGCodePreview = new TextBox();
            lblResult = new Label();
            tabPrintWithApproval = new TabPage();
            grpWorkflowFiles = new GroupBox();
            lblPaperImage = new Label();
            txtPaperImagePath = new TextBox();
            btnBrowsePaperImage = new Button();
            lblSignatureSvg = new Label();
            txtSignatureSvgPath = new TextBox();
            btnBrowseSignatureSvg = new Button();
            grpWorkflowPreviews = new GroupBox();
            lblPaperPreview = new Label();
            picPaperPreview = new PictureBox();
            lblSignaturePreview = new Label();
            picSignaturePreview = new PictureBox();
            grpWorkflowSettings = new GroupBox();
            chkShouldApprove = new CheckBox();
            btnTestPrintWithApproval = new Button();
            lblWorkflowStatus = new Label();
            lblRequestIdLabel = new Label();
            lblRequestIdValue = new Label();
            txtLastResult = new TextBox();
            tabRequestHistory = new TabPage();
            grpLogFilters = new GroupBox();
            lblLogCount = new Label();
            nudLogCount = new NumericUpDown();
            btnRefreshLogs = new Button();
            grpLogList = new GroupBox();
            lstRequestLogs = new ListBox();
            grpLogDetails = new GroupBox();
            lblLogRequestIdLabel = new Label();
            lblLogRequestIdValue = new Label();
            lblLogStatusLabel = new Label();
            lblLogStatusValue = new Label();
            lblLogCreatedAtLabel = new Label();
            lblLogCreatedAtValue = new Label();
            lblLogUpdatedAtLabel = new Label();
            lblLogUpdatedAtValue = new Label();
            lblLogCompletedAtLabel = new Label();
            lblLogCompletedAtValue = new Label();
            lblLogApprovalResponseLabel = new Label();
            lblLogApprovalResponseValue = new Label();
            lblLogErrorMessageLabel = new Label();
            lblLogErrorMessageValue = new Label();
            lblStatusTransitionsLabel = new Label();
            lblStatusTransitionsValue = new Label();
            openFileDialog = new OpenFileDialog();
            grpConnection.SuspendLayout();
            grpStatus.SuspendLayout();
            tabControl.SuspendLayout();
            tabBasicPrint.SuspendLayout();
            grpSimulation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picSimulation).BeginInit();
            grpPrintSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudScale).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudRotation).BeginInit();
            tabPrintWithApproval.SuspendLayout();
            grpWorkflowFiles.SuspendLayout();
            grpWorkflowPreviews.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picPaperPreview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picSignaturePreview).BeginInit();
            grpWorkflowSettings.SuspendLayout();
            tabRequestHistory.SuspendLayout();
            grpLogFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudLogCount).BeginInit();
            grpLogList.SuspendLayout();
            grpLogDetails.SuspendLayout();
            SuspendLayout();
            
            grpConnection.Controls.Add(lblComPort);
            grpConnection.Controls.Add(txtComPort);
            grpConnection.Controls.Add(lblBaudRate);
            grpConnection.Controls.Add(txtBaudRate);
            grpConnection.Controls.Add(btnConnect);
            grpConnection.Dock = DockStyle.Top;
            grpConnection.Location = new Point(0, 0);
            grpConnection.Name = "grpConnection";
            grpConnection.Padding = new Padding(10);
            grpConnection.Size = new Size(1024, 65);
            grpConnection.TabIndex = 0;
            grpConnection.TabStop = false;
            grpConnection.Text = "Serial Connection";
            
            lblComPort.AutoSize = true;
            lblComPort.Location = new Point(15, 28);
            lblComPort.Name = "lblComPort";
            lblComPort.Size = new Size(63, 15);
            lblComPort.TabIndex = 0;
            lblComPort.Text = "COM Port:";
            
            txtComPort.Location = new Point(85, 25);
            txtComPort.Name = "txtComPort";
            txtComPort.Size = new Size(80, 23);
            txtComPort.TabIndex = 1;
            txtComPort.Text = "COM5";
            
            lblBaudRate.AutoSize = true;
            lblBaudRate.Location = new Point(180, 28);
            lblBaudRate.Name = "lblBaudRate";
            lblBaudRate.Size = new Size(63, 15);
            lblBaudRate.TabIndex = 2;
            lblBaudRate.Text = "Baud Rate:";
            
            txtBaudRate.Location = new Point(255, 25);
            txtBaudRate.Name = "txtBaudRate";
            txtBaudRate.Size = new Size(80, 23);
            txtBaudRate.TabIndex = 3;
            txtBaudRate.Text = "250000";
            
            btnConnect.Location = new Point(350, 23);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(100, 28);
            btnConnect.TabIndex = 4;
            btnConnect.Text = "Connect";
            btnConnect.Click += btnConnect_Click;
            
            grpStatus.Dock = DockStyle.Top;
            grpStatus.Controls.Add(lblIsOpen);
            grpStatus.Controls.Add(lblPortName);
            grpStatus.Controls.Add(lblIsPrinting);
            grpStatus.Controls.Add(btnGetStatus);
            grpStatus.Location = new Point(0, 65);
            grpStatus.Name = "grpStatus";
            grpStatus.Padding = new Padding(10);
            grpStatus.Size = new Size(1024, 55);
            grpStatus.TabIndex = 1;
            grpStatus.TabStop = false;
            grpStatus.Text = "Printer Status";
            
            lblIsOpen.AutoSize = true;
            lblIsOpen.Location = new Point(15, 25);
            lblIsOpen.Name = "lblIsOpen";
            lblIsOpen.Size = new Size(60, 15);
            lblIsOpen.TabIndex = 0;
            lblIsOpen.Text = "IsOpen: --";
            
            lblPortName.AutoSize = true;
            lblPortName.Location = new Point(150, 25);
            lblPortName.Name = "lblPortName";
            lblPortName.Size = new Size(45, 15);
            lblPortName.TabIndex = 1;
            lblPortName.Text = "Port: --";
            
            lblIsPrinting.AutoSize = true;
            lblIsPrinting.Location = new Point(300, 25);
            lblIsPrinting.Name = "lblIsPrinting";
            lblIsPrinting.Size = new Size(65, 15);
            lblIsPrinting.TabIndex = 2;
            lblIsPrinting.Text = "Printing: --";
            
            btnGetStatus.Location = new Point(460, 20);
            btnGetStatus.Name = "btnGetStatus";
            btnGetStatus.Size = new Size(80, 28);
            btnGetStatus.TabIndex = 3;
            btnGetStatus.Text = "Refresh";
            btnGetStatus.Click += btnGetStatus_Click;
            
            tabControl.Controls.Add(tabBasicPrint);
            tabControl.Controls.Add(tabPrintWithApproval);
            tabControl.Controls.Add(tabRequestHistory);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 120);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1024, 580);
            tabControl.TabIndex = 2;
            
            tabBasicPrint.Controls.Add(grpSimulation);
            tabBasicPrint.Controls.Add(grpPrintSettings);
            tabBasicPrint.Location = new Point(4, 24);
            tabBasicPrint.Name = "tabBasicPrint";
            tabBasicPrint.Padding = new Padding(3);
            tabBasicPrint.Size = new Size(1016, 552);
            tabBasicPrint.TabIndex = 0;
            tabBasicPrint.Text = "Basic Print";
            tabBasicPrint.UseVisualStyleBackColor = true;
            
            grpSimulation.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grpSimulation.Controls.Add(lblPaper);
            grpSimulation.Controls.Add(cboPaper);
            grpSimulation.Controls.Add(btnBrowse);
            grpSimulation.Controls.Add(lblFileName);
            grpSimulation.Controls.Add(picSimulation);
            grpSimulation.Location = new Point(10, 10);
            grpSimulation.Name = "grpSimulation";
            grpSimulation.Padding = new Padding(10);
            grpSimulation.Size = new Size(680, 535);
            grpSimulation.TabIndex = 0;
            grpSimulation.TabStop = false;
            grpSimulation.Text = "Simulation";
            
            lblPaper.AutoSize = true;
            lblPaper.Location = new Point(15, 28);
            lblPaper.Name = "lblPaper";
            lblPaper.Size = new Size(40, 15);
            lblPaper.TabIndex = 0;
            lblPaper.Text = "Paper:";
            
            cboPaper.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPaper.Location = new Point(60, 25);
            cboPaper.Name = "cboPaper";
            cboPaper.Size = new Size(130, 23);
            cboPaper.TabIndex = 1;
            cboPaper.SelectedIndexChanged += OnSettingChanged;
            
            btnBrowse.Location = new Point(210, 24);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(90, 25);
            btnBrowse.TabIndex = 2;
            btnBrowse.Text = "Browse...";
            btnBrowse.Click += btnBrowse_Click;
            
            lblFileName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblFileName.AutoEllipsis = true;
            lblFileName.Location = new Point(310, 28);
            lblFileName.Name = "lblFileName";
            lblFileName.Size = new Size(355, 15);
            lblFileName.TabIndex = 3;
            lblFileName.Text = "No file selected";
            
            picSimulation.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            picSimulation.BackColor = Color.LightGray;
            picSimulation.BorderStyle = BorderStyle.FixedSingle;
            picSimulation.Location = new Point(15, 55);
            picSimulation.Name = "picSimulation";
            picSimulation.Size = new Size(650, 465);
            picSimulation.SizeMode = PictureBoxSizeMode.Zoom;
            picSimulation.TabIndex = 4;
            picSimulation.TabStop = false;
            picSimulation.Resize += OnSimulationResize;
            
            grpPrintSettings.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            grpPrintSettings.Controls.Add(lblXPosition);
            grpPrintSettings.Controls.Add(txtXPosition);
            grpPrintSettings.Controls.Add(lblYPosition);
            grpPrintSettings.Controls.Add(txtYPosition);
            grpPrintSettings.Controls.Add(lblScale);
            grpPrintSettings.Controls.Add(nudScale);
            grpPrintSettings.Controls.Add(lblRotation);
            grpPrintSettings.Controls.Add(nudRotation);
            grpPrintSettings.Controls.Add(chkInvertX);
            grpPrintSettings.Controls.Add(chkInvertY);
            grpPrintSettings.Controls.Add(txtGCodePreview);
            grpPrintSettings.Controls.Add(btnGenerateGCode);
            grpPrintSettings.Controls.Add(btnPrint);
            grpPrintSettings.Controls.Add(lblResult);
            grpPrintSettings.Location = new Point(700, 10);
            grpPrintSettings.Name = "grpPrintSettings";
            grpPrintSettings.Size = new Size(305, 535);
            grpPrintSettings.TabIndex = 1;
            grpPrintSettings.TabStop = false;
            grpPrintSettings.Text = "Print Settings";
            
            lblXPosition.AutoSize = true;
            lblXPosition.Location = new Point(15, 33);
            lblXPosition.Name = "lblXPosition";
            lblXPosition.Size = new Size(63, 15);
            lblXPosition.TabIndex = 0;
            lblXPosition.Text = "X Position:";
            
            txtXPosition.Location = new Point(95, 30);
            txtXPosition.Name = "txtXPosition";
            txtXPosition.Size = new Size(195, 23);
            txtXPosition.TabIndex = 1;
            txtXPosition.Text = "50mm";
            txtXPosition.TextChanged += OnSettingChanged;
            
            lblYPosition.AutoSize = true;
            lblYPosition.Location = new Point(15, 68);
            lblYPosition.Name = "lblYPosition";
            lblYPosition.Size = new Size(63, 15);
            lblYPosition.TabIndex = 2;
            lblYPosition.Text = "Y Position:";
            
            txtYPosition.Location = new Point(95, 65);
            txtYPosition.Name = "txtYPosition";
            txtYPosition.Size = new Size(195, 23);
            txtYPosition.TabIndex = 3;
            txtYPosition.Text = "50mm";
            txtYPosition.TextChanged += OnSettingChanged;
            
            lblScale.AutoSize = true;
            lblScale.Location = new Point(15, 103);
            lblScale.Name = "lblScale";
            lblScale.Size = new Size(37, 15);
            lblScale.TabIndex = 4;
            lblScale.Text = "Scale:";
            
            nudScale.Location = new Point(95, 100);
            nudScale.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudScale.Name = "nudScale";
            nudScale.Size = new Size(195, 23);
            nudScale.TabIndex = 5;
            nudScale.Value = new decimal(new int[] { 1, 0, 0, 0 });
            nudScale.ValueChanged += OnSettingChanged;
            
            lblRotation.AutoSize = true;
            lblRotation.Location = new Point(15, 138);
            lblRotation.Name = "lblRotation";
            lblRotation.Size = new Size(55, 15);
            lblRotation.TabIndex = 6;
            lblRotation.Text = "Rotation:";
            
            nudRotation.Location = new Point(95, 135);
            nudRotation.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
            nudRotation.Name = "nudRotation";
            nudRotation.Size = new Size(195, 23);
            nudRotation.TabIndex = 7;
            nudRotation.ValueChanged += OnSettingChanged;
            
            chkInvertX.AutoSize = true;
            chkInvertX.Location = new Point(95, 175);
            chkInvertX.Name = "chkInvertX";
            chkInvertX.Size = new Size(69, 19);
            chkInvertX.TabIndex = 8;
            chkInvertX.Text = "Invert X";
            chkInvertX.CheckedChanged += OnSettingChanged;
            
            chkInvertY.AutoSize = true;
            chkInvertY.Checked = true;
            chkInvertY.CheckState = CheckState.Checked;
            chkInvertY.Location = new Point(195, 175);
            chkInvertY.Name = "chkInvertY";
            chkInvertY.Size = new Size(69, 19);
            chkInvertY.TabIndex = 9;
            chkInvertY.Text = "Invert Y";
            chkInvertY.CheckedChanged += OnSettingChanged;
            
            txtGCodePreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtGCodePreview.Location = new Point(15, 205);
            txtGCodePreview.Multiline = true;
            txtGCodePreview.Name = "txtGCodePreview";
            txtGCodePreview.ReadOnly = true;
            txtGCodePreview.ScrollBars = ScrollBars.Vertical;
            txtGCodePreview.Font = new Font("Consolas", 8F);
            txtGCodePreview.BackColor = Color.White;
            txtGCodePreview.Size = new Size(275, 225);
            txtGCodePreview.TabIndex = 13;
            
            btnGenerateGCode.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnGenerateGCode.Location = new Point(15, 440);
            btnGenerateGCode.Name = "btnGenerateGCode";
            btnGenerateGCode.Size = new Size(275, 30);
            btnGenerateGCode.TabIndex = 12;
            btnGenerateGCode.Text = "Generate G-code";
            btnGenerateGCode.Click += btnGenerateGCode_Click;
            
            btnPrint.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnPrint.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnPrint.Location = new Point(15, 478);
            btnPrint.Name = "btnPrint";
            btnPrint.Size = new Size(275, 35);
            btnPrint.TabIndex = 10;
            btnPrint.Text = "Print";
            btnPrint.Click += btnPrint_Click;
            
            lblResult.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblResult.AutoEllipsis = true;
            lblResult.Location = new Point(15, 516);
            lblResult.Name = "lblResult";
            lblResult.Size = new Size(275, 17);
            lblResult.TabIndex = 11;
            
            tabPrintWithApproval.Controls.Add(grpWorkflowFiles);
            tabPrintWithApproval.Controls.Add(grpWorkflowPreviews);
            tabPrintWithApproval.Controls.Add(grpWorkflowSettings);
            tabPrintWithApproval.Location = new Point(4, 24);
            tabPrintWithApproval.Name = "tabPrintWithApproval";
            tabPrintWithApproval.Padding = new Padding(3);
            tabPrintWithApproval.Size = new Size(1016, 552);
            tabPrintWithApproval.TabIndex = 1;
            tabPrintWithApproval.Text = "Print with Approval";
            tabPrintWithApproval.UseVisualStyleBackColor = true;
            
            grpWorkflowFiles.Location = new Point(10, 10);
            grpWorkflowFiles.Name = "grpWorkflowFiles";
            grpWorkflowFiles.Size = new Size(995, 100);
            grpWorkflowFiles.TabIndex = 0;
            grpWorkflowFiles.TabStop = false;
            grpWorkflowFiles.Text = "Input Files";
            
            lblPaperImage.AutoSize = true;
            lblPaperImage.Location = new Point(15, 30);
            lblPaperImage.Name = "lblPaperImage";
            lblPaperImage.Size = new Size(78, 15);
            lblPaperImage.TabIndex = 0;
            lblPaperImage.Text = "Paper Image:";
            
            txtPaperImagePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtPaperImagePath.Location = new Point(100, 27);
            txtPaperImagePath.Name = "txtPaperImagePath";
            txtPaperImagePath.ReadOnly = true;
            txtPaperImagePath.Size = new Size(765, 23);
            txtPaperImagePath.TabIndex = 1;
            
            btnBrowsePaperImage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowsePaperImage.Location = new Point(875, 26);
            btnBrowsePaperImage.Name = "btnBrowsePaperImage";
            btnBrowsePaperImage.Size = new Size(105, 25);
            btnBrowsePaperImage.TabIndex = 2;
            btnBrowsePaperImage.Text = "Browse...";
            btnBrowsePaperImage.Click += btnBrowsePaperImage_Click;
            
            lblSignatureSvg.AutoSize = true;
            lblSignatureSvg.Location = new Point(15, 65);
            lblSignatureSvg.Name = "lblSignatureSvg";
            lblSignatureSvg.Size = new Size(82, 15);
            lblSignatureSvg.TabIndex = 3;
            lblSignatureSvg.Text = "Signature SVG:";
            
            txtSignatureSvgPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSignatureSvgPath.Location = new Point(100, 62);
            txtSignatureSvgPath.Name = "txtSignatureSvgPath";
            txtSignatureSvgPath.ReadOnly = true;
            txtSignatureSvgPath.Size = new Size(765, 23);
            txtSignatureSvgPath.TabIndex = 4;
            
            btnBrowseSignatureSvg.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBrowseSignatureSvg.Location = new Point(875, 61);
            btnBrowseSignatureSvg.Name = "btnBrowseSignatureSvg";
            btnBrowseSignatureSvg.Size = new Size(105, 25);
            btnBrowseSignatureSvg.TabIndex = 5;
            btnBrowseSignatureSvg.Text = "Browse...";
            btnBrowseSignatureSvg.Click += btnBrowseSignatureSvg_Click;
            
            grpWorkflowFiles.Controls.Add(lblPaperImage);
            grpWorkflowFiles.Controls.Add(txtPaperImagePath);
            grpWorkflowFiles.Controls.Add(btnBrowsePaperImage);
            grpWorkflowFiles.Controls.Add(lblSignatureSvg);
            grpWorkflowFiles.Controls.Add(txtSignatureSvgPath);
            grpWorkflowFiles.Controls.Add(btnBrowseSignatureSvg);
            
            grpWorkflowPreviews.Location = new Point(10, 120);
            grpWorkflowPreviews.Name = "grpWorkflowPreviews";
            grpWorkflowPreviews.Size = new Size(470, 425);
            grpWorkflowPreviews.TabIndex = 1;
            grpWorkflowPreviews.TabStop = false;
            grpWorkflowPreviews.Text = "Preview";
            
            lblPaperPreview.AutoSize = true;
            lblPaperPreview.Location = new Point(15, 25);
            lblPaperPreview.Name = "lblPaperPreview";
            lblPaperPreview.Size = new Size(84, 15);
            lblPaperPreview.TabIndex = 0;
            lblPaperPreview.Text = "Paper Preview:";
            
            picPaperPreview.BackColor = Color.LightGray;
            picPaperPreview.BorderStyle = BorderStyle.FixedSingle;
            picPaperPreview.Location = new Point(15, 45);
            picPaperPreview.Name = "picPaperPreview";
            picPaperPreview.Size = new Size(440, 165);
            picPaperPreview.SizeMode = PictureBoxSizeMode.Zoom;
            picPaperPreview.TabIndex = 1;
            picPaperPreview.TabStop = false;
            
            lblSignaturePreview.AutoSize = true;
            lblSignaturePreview.Location = new Point(15, 220);
            lblSignaturePreview.Name = "lblSignaturePreview";
            lblSignaturePreview.Size = new Size(106, 15);
            lblSignaturePreview.TabIndex = 2;
            lblSignaturePreview.Text = "Signature Preview:";
            
            picSignaturePreview.BackColor = Color.White;
            picSignaturePreview.BorderStyle = BorderStyle.FixedSingle;
            picSignaturePreview.Location = new Point(15, 240);
            picSignaturePreview.Name = "picSignaturePreview";
            picSignaturePreview.Size = new Size(440, 165);
            picSignaturePreview.SizeMode = PictureBoxSizeMode.Zoom;
            picSignaturePreview.TabIndex = 3;
            picSignaturePreview.TabStop = false;
            
            grpWorkflowPreviews.Controls.Add(lblPaperPreview);
            grpWorkflowPreviews.Controls.Add(picPaperPreview);
            grpWorkflowPreviews.Controls.Add(lblSignaturePreview);
            grpWorkflowPreviews.Controls.Add(picSignaturePreview);
            
            grpWorkflowSettings.Location = new Point(490, 120);
            grpWorkflowSettings.Name = "grpWorkflowSettings";
            grpWorkflowSettings.Size = new Size(515, 425);
            grpWorkflowSettings.TabIndex = 2;
            grpWorkflowSettings.TabStop = false;
            grpWorkflowSettings.Text = "Test Workflow";
            
            chkShouldApprove.AutoSize = true;
            chkShouldApprove.Checked = true;
            chkShouldApprove.CheckState = CheckState.Checked;
            chkShouldApprove.Location = new Point(20, 30);
            chkShouldApprove.Name = "chkShouldApprove";
            chkShouldApprove.Size = new Size(251, 19);
            chkShouldApprove.TabIndex = 0;
            chkShouldApprove.Text = "Approve Request (unchecked = void print)";
            
            btnTestPrintWithApproval.Location = new Point(20, 60);
            btnTestPrintWithApproval.Name = "btnTestPrintWithApproval";
            btnTestPrintWithApproval.Size = new Size(475, 35);
            btnTestPrintWithApproval.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnTestPrintWithApproval.TabIndex = 1;
            btnTestPrintWithApproval.Text = "Test Print with Approval";
            btnTestPrintWithApproval.Click += btnTestPrintWithApproval_Click;
            
            lblWorkflowStatus.AutoSize = true;
            lblWorkflowStatus.Location = new Point(20, 110);
            lblWorkflowStatus.Name = "lblWorkflowStatus";
            lblWorkflowStatus.Size = new Size(45, 15);
            lblWorkflowStatus.TabIndex = 2;
            lblWorkflowStatus.Text = "Status: ";
            
            lblRequestIdLabel.AutoSize = true;
            lblRequestIdLabel.Location = new Point(20, 135);
            lblRequestIdLabel.Name = "lblRequestIdLabel";
            lblRequestIdLabel.Size = new Size(70, 15);
            lblRequestIdLabel.TabIndex = 3;
            lblRequestIdLabel.Text = "Request ID:";
            
            lblRequestIdValue.AutoSize = true;
            lblRequestIdValue.Location = new Point(95, 135);
            lblRequestIdValue.Name = "lblRequestIdValue";
            lblRequestIdValue.Size = new Size(17, 15);
            lblRequestIdValue.TabIndex = 4;
            lblRequestIdValue.Text = "--";
            
            txtLastResult.Location = new Point(20, 165);
            txtLastResult.Multiline = true;
            txtLastResult.Name = "txtLastResult";
            txtLastResult.ReadOnly = true;
            txtLastResult.ScrollBars = ScrollBars.Vertical;
            txtLastResult.Size = new Size(475, 245);
            txtLastResult.Font = new Font("Consolas", 8.5F);
            txtLastResult.TabIndex = 5;
            
            grpWorkflowSettings.Controls.Add(chkShouldApprove);
            grpWorkflowSettings.Controls.Add(btnTestPrintWithApproval);
            grpWorkflowSettings.Controls.Add(lblWorkflowStatus);
            grpWorkflowSettings.Controls.Add(lblRequestIdLabel);
            grpWorkflowSettings.Controls.Add(lblRequestIdValue);
            grpWorkflowSettings.Controls.Add(txtLastResult);
            
            tabRequestHistory.Controls.Add(grpLogFilters);
            tabRequestHistory.Controls.Add(grpLogList);
            tabRequestHistory.Controls.Add(grpLogDetails);
            tabRequestHistory.Location = new Point(4, 24);
            tabRequestHistory.Name = "tabRequestHistory";
            tabRequestHistory.Padding = new Padding(3);
            tabRequestHistory.Size = new Size(1016, 552);
            tabRequestHistory.TabIndex = 2;
            tabRequestHistory.Text = "Request History";
            tabRequestHistory.UseVisualStyleBackColor = true;
            
            grpLogFilters.Location = new Point(10, 10);
            grpLogFilters.Name = "grpLogFilters";
            grpLogFilters.Size = new Size(995, 60);
            grpLogFilters.TabIndex = 0;
            grpLogFilters.TabStop = false;
            grpLogFilters.Text = "Filters";
            
            lblLogCount.AutoSize = true;
            lblLogCount.Location = new Point(15, 28);
            lblLogCount.Name = "lblLogCount";
            lblLogCount.Size = new Size(104, 15);
            lblLogCount.TabIndex = 0;
            lblLogCount.Text = "Number of Logs:";
            
            nudLogCount.Location = new Point(125, 25);
            nudLogCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudLogCount.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            nudLogCount.Name = "nudLogCount";
            nudLogCount.Size = new Size(80, 23);
            nudLogCount.TabIndex = 1;
            nudLogCount.Value = new decimal(new int[] { 20, 0, 0, 0 });
            
            btnRefreshLogs.Location = new Point(220, 24);
            btnRefreshLogs.Name = "btnRefreshLogs";
            btnRefreshLogs.Size = new Size(100, 25);
            btnRefreshLogs.TabIndex = 2;
            btnRefreshLogs.Text = "Refresh";
            btnRefreshLogs.Click += btnRefreshLogs_Click;
            
            grpLogFilters.Controls.Add(lblLogCount);
            grpLogFilters.Controls.Add(nudLogCount);
            grpLogFilters.Controls.Add(btnRefreshLogs);
            
            grpLogList.Location = new Point(10, 80);
            grpLogList.Name = "grpLogList";
            grpLogList.Size = new Size(450, 465);
            grpLogList.TabIndex = 1;
            grpLogList.TabStop = false;
            grpLogList.Text = "Recent Requests";
            
            lstRequestLogs.Dock = DockStyle.Fill;
            lstRequestLogs.Font = new Font("Consolas", 8.5F);
            lstRequestLogs.Location = new Point(3, 19);
            lstRequestLogs.Name = "lstRequestLogs";
            lstRequestLogs.Size = new Size(444, 443);
            lstRequestLogs.TabIndex = 0;
            lstRequestLogs.SelectedIndexChanged += lstRequestLogs_SelectedIndexChanged;
            
            grpLogList.Controls.Add(lstRequestLogs);
            
            grpLogDetails.Location = new Point(470, 80);
            grpLogDetails.Name = "grpLogDetails";
            grpLogDetails.Size = new Size(535, 465);
            grpLogDetails.TabIndex = 2;
            grpLogDetails.TabStop = false;
            grpLogDetails.Text = "Request Details";
            
            lblLogRequestIdLabel.AutoSize = true;
            lblLogRequestIdLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLogRequestIdLabel.Location = new Point(15, 30);
            lblLogRequestIdLabel.Name = "lblLogRequestIdLabel";
            lblLogRequestIdLabel.Size = new Size(74, 15);
            lblLogRequestIdLabel.TabIndex = 0;
            lblLogRequestIdLabel.Text = "Request ID:";
            
            lblLogRequestIdValue.AutoSize = true;
            lblLogRequestIdValue.Location = new Point(130, 30);
            lblLogRequestIdValue.Name = "lblLogRequestIdValue";
            lblLogRequestIdValue.Size = new Size(17, 15);
            lblLogRequestIdValue.TabIndex = 1;
            lblLogRequestIdValue.Text = "--";
            
            lblLogStatusLabel.AutoSize = true;
            lblLogStatusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLogStatusLabel.Location = new Point(15, 60);
            lblLogStatusLabel.Name = "lblLogStatusLabel";
            lblLogStatusLabel.Size = new Size(46, 15);
            lblLogStatusLabel.TabIndex = 2;
            lblLogStatusLabel.Text = "Status:";
            
            lblLogStatusValue.AutoSize = true;
            lblLogStatusValue.Location = new Point(130, 60);
            lblLogStatusValue.Name = "lblLogStatusValue";
            lblLogStatusValue.Size = new Size(17, 15);
            lblLogStatusValue.TabIndex = 3;
            lblLogStatusValue.Text = "--";
            
            lblLogCreatedAtLabel.AutoSize = true;
            lblLogCreatedAtLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLogCreatedAtLabel.Location = new Point(15, 90);
            lblLogCreatedAtLabel.Name = "lblLogCreatedAtLabel";
            lblLogCreatedAtLabel.Size = new Size(71, 15);
            lblLogCreatedAtLabel.TabIndex = 4;
            lblLogCreatedAtLabel.Text = "Created At:";
            
            lblLogCreatedAtValue.AutoSize = true;
            lblLogCreatedAtValue.Location = new Point(130, 90);
            lblLogCreatedAtValue.Name = "lblLogCreatedAtValue";
            lblLogCreatedAtValue.Size = new Size(17, 15);
            lblLogCreatedAtValue.TabIndex = 5;
            lblLogCreatedAtValue.Text = "--";
            
            lblLogUpdatedAtLabel.AutoSize = true;
            lblLogUpdatedAtLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLogUpdatedAtLabel.Location = new Point(15, 120);
            lblLogUpdatedAtLabel.Name = "lblLogUpdatedAtLabel";
            lblLogUpdatedAtLabel.Size = new Size(75, 15);
            lblLogUpdatedAtLabel.TabIndex = 6;
            lblLogUpdatedAtLabel.Text = "Updated At:";
            
            lblLogUpdatedAtValue.AutoSize = true;
            lblLogUpdatedAtValue.Location = new Point(130, 120);
            lblLogUpdatedAtValue.Name = "lblLogUpdatedAtValue";
            lblLogUpdatedAtValue.Size = new Size(17, 15);
            lblLogUpdatedAtValue.TabIndex = 7;
            lblLogUpdatedAtValue.Text = "--";
            
            lblLogCompletedAtLabel.AutoSize = true;
            lblLogCompletedAtLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLogCompletedAtLabel.Location = new Point(15, 150);
            lblLogCompletedAtLabel.Name = "lblLogCompletedAtLabel";
            lblLogCompletedAtLabel.Size = new Size(90, 15);
            lblLogCompletedAtLabel.TabIndex = 8;
            lblLogCompletedAtLabel.Text = "Completed At:";
            
            lblLogCompletedAtValue.AutoSize = true;
            lblLogCompletedAtValue.Location = new Point(130, 150);
            lblLogCompletedAtValue.Name = "lblLogCompletedAtValue";
            lblLogCompletedAtValue.Size = new Size(17, 15);
            lblLogCompletedAtValue.TabIndex = 9;
            lblLogCompletedAtValue.Text = "--";
            
            lblLogApprovalResponseLabel.AutoSize = true;
            lblLogApprovalResponseLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLogApprovalResponseLabel.Location = new Point(15, 180);
            lblLogApprovalResponseLabel.Name = "lblLogApprovalResponseLabel";
            lblLogApprovalResponseLabel.Size = new Size(121, 15);
            lblLogApprovalResponseLabel.TabIndex = 10;
            lblLogApprovalResponseLabel.Text = "Approval Response:";
            
            lblLogApprovalResponseValue.AutoSize = true;
            lblLogApprovalResponseValue.Location = new Point(15, 200);
            lblLogApprovalResponseValue.MaximumSize = new Size(505, 0);
            lblLogApprovalResponseValue.Name = "lblLogApprovalResponseValue";
            lblLogApprovalResponseValue.Size = new Size(17, 15);
            lblLogApprovalResponseValue.TabIndex = 11;
            lblLogApprovalResponseValue.Text = "--";
            
            lblLogErrorMessageLabel.AutoSize = true;
            lblLogErrorMessageLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLogErrorMessageLabel.Location = new Point(15, 240);
            lblLogErrorMessageLabel.Name = "lblLogErrorMessageLabel";
            lblLogErrorMessageLabel.Size = new Size(91, 15);
            lblLogErrorMessageLabel.TabIndex = 12;
            lblLogErrorMessageLabel.Text = "Error Message:";
            
            lblLogErrorMessageValue.AutoSize = true;
            lblLogErrorMessageValue.Location = new Point(15, 260);
            lblLogErrorMessageValue.MaximumSize = new Size(505, 0);
            lblLogErrorMessageValue.Name = "lblLogErrorMessageValue";
            lblLogErrorMessageValue.Size = new Size(17, 15);
            lblLogErrorMessageValue.TabIndex = 13;
            lblLogErrorMessageValue.Text = "--";
            
            lblStatusTransitionsLabel.AutoSize = true;
            lblStatusTransitionsLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblStatusTransitionsLabel.Location = new Point(15, 300);
            lblStatusTransitionsLabel.Name = "lblStatusTransitionsLabel";
            lblStatusTransitionsLabel.Size = new Size(115, 15);
            lblStatusTransitionsLabel.TabIndex = 14;
            lblStatusTransitionsLabel.Text = "Status Transitions:";
            
            lblStatusTransitionsValue.AutoSize = true;
            lblStatusTransitionsValue.Location = new Point(15, 320);
            lblStatusTransitionsValue.MaximumSize = new Size(505, 0);
            lblStatusTransitionsValue.Name = "lblStatusTransitionsValue";
            lblStatusTransitionsValue.Size = new Size(17, 15);
            lblStatusTransitionsValue.TabIndex = 15;
            lblStatusTransitionsValue.Text = "--";
            lblStatusTransitionsValue.Font = new Font("Consolas", 8.5F);
            
            grpLogDetails.Controls.Add(lblLogRequestIdLabel);
            grpLogDetails.Controls.Add(lblLogRequestIdValue);
            grpLogDetails.Controls.Add(lblLogStatusLabel);
            grpLogDetails.Controls.Add(lblLogStatusValue);
            grpLogDetails.Controls.Add(lblLogCreatedAtLabel);
            grpLogDetails.Controls.Add(lblLogCreatedAtValue);
            grpLogDetails.Controls.Add(lblLogUpdatedAtLabel);
            grpLogDetails.Controls.Add(lblLogUpdatedAtValue);
            grpLogDetails.Controls.Add(lblLogCompletedAtLabel);
            grpLogDetails.Controls.Add(lblLogCompletedAtValue);
            grpLogDetails.Controls.Add(lblLogApprovalResponseLabel);
            grpLogDetails.Controls.Add(lblLogApprovalResponseValue);
            grpLogDetails.Controls.Add(lblLogErrorMessageLabel);
            grpLogDetails.Controls.Add(lblLogErrorMessageValue);
            grpLogDetails.Controls.Add(lblStatusTransitionsLabel);
            grpLogDetails.Controls.Add(lblStatusTransitionsValue);
            
            openFileDialog.Filter = "SVG Files (*.svg)|*.svg|All Files (*.*)|*.*";
            openFileDialog.Title = "Select an SVG File";
            
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1024, 700);
            Controls.Add(tabControl);
            Controls.Add(grpStatus);
            Controls.Add(grpConnection);
            MinimumSize = new Size(900, 650);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "UunaTek Printer - Test Interface";
            grpConnection.ResumeLayout(false);
            grpConnection.PerformLayout();
            grpStatus.ResumeLayout(false);
            grpStatus.PerformLayout();
            tabControl.ResumeLayout(false);
            tabBasicPrint.ResumeLayout(false);
            grpSimulation.ResumeLayout(false);
            grpSimulation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picSimulation).EndInit();
            grpPrintSettings.ResumeLayout(false);
            grpPrintSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudScale).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudRotation).EndInit();
            tabPrintWithApproval.ResumeLayout(false);
            grpWorkflowFiles.ResumeLayout(false);
            grpWorkflowFiles.PerformLayout();
            grpWorkflowPreviews.ResumeLayout(false);
            grpWorkflowPreviews.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picPaperPreview).EndInit();
            ((System.ComponentModel.ISupportInitialize)picSignaturePreview).EndInit();
            grpWorkflowSettings.ResumeLayout(false);
            grpWorkflowSettings.PerformLayout();
            tabRequestHistory.ResumeLayout(false);
            grpLogFilters.ResumeLayout(false);
            grpLogFilters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudLogCount).EndInit();
            grpLogList.ResumeLayout(false);
            grpLogDetails.ResumeLayout(false);
            grpLogDetails.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox grpConnection;
        private Label lblComPort;
        private TextBox txtComPort;
        private Label lblBaudRate;
        private TextBox txtBaudRate;
        private Button btnConnect;

        private GroupBox grpStatus;
        private Label lblIsOpen;
        private Label lblPortName;
        private Label lblIsPrinting;
        private Button btnGetStatus;

        private TabControl tabControl;
        private TabPage tabBasicPrint;
        private TabPage tabPrintWithApproval;
        private TabPage tabRequestHistory;

        private GroupBox grpSimulation;
        private Label lblPaper;
        private ComboBox cboPaper;
        private Button btnBrowse;
        private Label lblFileName;
        private PictureBox picSimulation;

        private GroupBox grpPrintSettings;
        private Label lblXPosition;
        private TextBox txtXPosition;
        private Label lblYPosition;
        private TextBox txtYPosition;
        private Label lblScale;
        private NumericUpDown nudScale;
        private Label lblRotation;
        private NumericUpDown nudRotation;
        private CheckBox chkInvertX;
        private CheckBox chkInvertY;

        private Button btnGenerateGCode;
        private Button btnPrint;
        private Label lblResult;
        private TextBox txtGCodePreview;

        private GroupBox grpWorkflowFiles;
        private Label lblPaperImage;
        private TextBox txtPaperImagePath;
        private Button btnBrowsePaperImage;
        private Label lblSignatureSvg;
        private TextBox txtSignatureSvgPath;
        private Button btnBrowseSignatureSvg;

        private GroupBox grpWorkflowPreviews;
        private Label lblPaperPreview;
        private PictureBox picPaperPreview;
        private Label lblSignaturePreview;
        private PictureBox picSignaturePreview;

        private GroupBox grpWorkflowSettings;
        private CheckBox chkShouldApprove;
        private Button btnTestPrintWithApproval;
        private Label lblWorkflowStatus;
        private Label lblRequestIdLabel;
        private Label lblRequestIdValue;
        private TextBox txtLastResult;

        private GroupBox grpLogFilters;
        private Label lblLogCount;
        private NumericUpDown nudLogCount;
        private Button btnRefreshLogs;

        private GroupBox grpLogList;
        private ListBox lstRequestLogs;

        private GroupBox grpLogDetails;
        private Label lblLogRequestIdLabel;
        private Label lblLogRequestIdValue;
        private Label lblLogStatusLabel;
        private Label lblLogStatusValue;
        private Label lblLogCreatedAtLabel;
        private Label lblLogCreatedAtValue;
        private Label lblLogUpdatedAtLabel;
        private Label lblLogUpdatedAtValue;
        private Label lblLogCompletedAtLabel;
        private Label lblLogCompletedAtValue;
        private Label lblLogApprovalResponseLabel;
        private Label lblLogApprovalResponseValue;
        private Label lblLogErrorMessageLabel;
        private Label lblLogErrorMessageValue;
        private Label lblStatusTransitionsLabel;
        private Label lblStatusTransitionsValue;

        private OpenFileDialog openFileDialog;
    }
}
