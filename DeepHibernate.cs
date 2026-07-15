using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Threading;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

[assembly: System.Reflection.AssemblyTitle("DeepHibernate")]
[assembly: System.Reflection.AssemblyDescription("极致休眠")]
[assembly: System.Reflection.AssemblyVersion("2.1.0.0")]

namespace DeepHibernate
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    class TempInfo
    {
        public double CPU { get; set; }
        public double? GPU { get; set; }
    }

    class MainForm : Form
    {
        private CheckBox chkUSBSuspend;
        private CheckBox chkDeviceWake;
        private CheckBox chkWOL;
        private CheckBox chkTaskWake;
        private TrackBar trackTemp;
        private NumericUpDown numTemp;
        private Label lblCPU;
        private Label lblGPU;
        private Label lblStatus;
        private Button btnHibernate;
        private BackgroundWorker coolDownWorker;
        private System.Windows.Forms.Timer tempTimer;

        private Computer lhmComputer;
        private bool lhmAvailable = false;

        private int targetTemp = 35;
        private bool isCooling = false;
        private bool syncingTrack = false;

        public MainForm()
        {
            InitializeComponent();
            InitLibreHardwareMonitor();
            ReadTemperatureAsync();
        }

        private void InitLibreHardwareMonitor()
        {
            try
            {
                lhmComputer = new Computer();
                lhmComputer.IsCpuEnabled = true;
                lhmComputer.IsGpuEnabled = true;
                lhmComputer.Open();
                lhmAvailable = true;
            }
            catch
            {
                lhmAvailable = false;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "DeepHibernate — 极致休眠";
            this.Size = new Size(500, 460);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Icon = Icon.ExtractAssociatedIcon(
                System.Reflection.Assembly.GetExecutingAssembly().Location);

            int y = 12;
            int leftMargin = 20;
            Font baseFont = new Font("Microsoft YaHei", 9.5f);

            // Title
            Label lblTitle = new Label();
            lblTitle.Text = "DeepHibernate — 极致休眠";
            lblTitle.Font = new Font("Microsoft YaHei", 13, FontStyle.Bold);
            lblTitle.Location = new Point(leftMargin, y);
            lblTitle.Size = new Size(440, 28);
            this.Controls.Add(lblTitle);
            y += 30;

            // Description
            Label lblDesc = new Label();
            lblDesc.Text = "休眠前执行电源优化，降温达标后进入 S4 休眠";
            lblDesc.Font = new Font("Microsoft YaHei", 8.5f);
            lblDesc.ForeColor = Color.Gray;
            lblDesc.Location = new Point(leftMargin + 2, y);
            lblDesc.Size = new Size(440, 18);
            this.Controls.Add(lblDesc);
            y += 25;

            // Four checkboxes
            chkUSBSuspend = new CheckBox();
            chkUSBSuspend.Text = "关闭 USB 选择性暂停";
            chkUSBSuspend.Checked = true;
            chkUSBSuspend.Location = new Point(leftMargin, y);
            chkUSBSuspend.Size = new Size(440, 22);
            chkUSBSuspend.Font = baseFont;
            this.Controls.Add(chkUSBSuspend);
            y += 26;

            chkDeviceWake = new CheckBox();
            chkDeviceWake.Text = "禁用设备唤醒权限（键盘 / 鼠标等无法唤醒）";
            chkDeviceWake.Checked = true;
            chkDeviceWake.Location = new Point(leftMargin, y);
            chkDeviceWake.Size = new Size(440, 22);
            chkDeviceWake.Font = baseFont;
            this.Controls.Add(chkDeviceWake);
            y += 26;

            chkWOL = new CheckBox();
            chkWOL.Text = "关闭网卡 Wake-on-LAN（默认保留 WOL）";
            chkWOL.Checked = false;
            chkWOL.Location = new Point(leftMargin, y);
            chkWOL.Size = new Size(440, 22);
            chkWOL.Font = baseFont;
            this.Controls.Add(chkWOL);
            y += 26;

            chkTaskWake = new CheckBox();
            chkTaskWake.Text = "禁用计划任务唤醒";
            chkTaskWake.Checked = true;
            chkTaskWake.Location = new Point(leftMargin, y);
            chkTaskWake.Size = new Size(440, 22);
            chkTaskWake.Font = baseFont;
            this.Controls.Add(chkTaskWake);
            y += 30;

            // Separator
            Label sep = new Label();
            sep.BorderStyle = BorderStyle.Fixed3D;
            sep.Height = 2;
            sep.Location = new Point(leftMargin, y);
            sep.Size = new Size(440, 2);
            this.Controls.Add(sep);
            y += 12;

            // Target temperature row
            Label lblTempTitle = new Label();
            lblTempTitle.Text = "目标温度";
            lblTempTitle.Font = new Font("Microsoft YaHei", 9.5f, FontStyle.Bold);
            lblTempTitle.Location = new Point(leftMargin, y + 1);
            lblTempTitle.Size = new Size(65, 20);
            this.Controls.Add(lblTempTitle);

            trackTemp = new TrackBar();
            trackTemp.Minimum = 20;
            trackTemp.Maximum = 80;
            trackTemp.Value = 35;
            trackTemp.TickFrequency = 5;
            trackTemp.AutoSize = false;
            trackTemp.Height = 30;
            trackTemp.Location = new Point(leftMargin + 65, y - 2);
            trackTemp.Size = new Size(280, 30);
            trackTemp.Scroll += TrackTemp_Scroll;
            this.Controls.Add(trackTemp);

            numTemp = new NumericUpDown();
            numTemp.Minimum = 20;
            numTemp.Maximum = 80;
            numTemp.Value = 35;
            numTemp.Location = new Point(leftMargin + 355, y);
            numTemp.Size = new Size(50, 22);
            numTemp.Font = baseFont;
            numTemp.TextAlign = HorizontalAlignment.Center;
            numTemp.ValueChanged += NumTemp_ValueChanged;
            this.Controls.Add(numTemp);

            Label lblDeg = new Label();
            lblDeg.Text = "°C";
            lblDeg.Font = baseFont;
            lblDeg.Location = new Point(leftMargin + 408, y + 1);
            lblDeg.Size = new Size(30, 20);
            this.Controls.Add(lblDeg);
            y += 36;

            // CPU Temperature
            lblCPU = new Label();
            lblCPU.Text = "CPU 温度：-- °C";
            lblCPU.Font = new Font("Microsoft YaHei", 10);
            lblCPU.Location = new Point(leftMargin, y);
            lblCPU.Size = new Size(440, 22);
            this.Controls.Add(lblCPU);
            y += 24;

            // GPU Temperature
            lblGPU = new Label();
            lblGPU.Text = "GPU 温度：-- °C";
            lblGPU.Font = new Font("Microsoft YaHei", 10);
            lblGPU.Location = new Point(leftMargin, y);
            lblGPU.Size = new Size(440, 22);
            this.Controls.Add(lblGPU);
            y += 28;

            // Status
            lblStatus = new Label();
            lblStatus.Text = "就绪";
            lblStatus.Font = new Font("Microsoft YaHei", 9);
            lblStatus.ForeColor = Color.DimGray;
            lblStatus.Location = new Point(leftMargin, y);
            lblStatus.Size = new Size(440, 20);
            this.Controls.Add(lblStatus);
            y += 30;

            // Button
            btnHibernate = new Button();
            btnHibernate.Text = "进入极致休眠";
            btnHibernate.Font = new Font("Microsoft YaHei", 11, FontStyle.Bold);
            btnHibernate.Location = new Point((440 - 260) / 2 + leftMargin, y);
            btnHibernate.Size = new Size(260, 38);
            btnHibernate.Click += BtnHibernate_Click;
            this.Controls.Add(btnHibernate);
            y += 48;

            // Version
            Label lblVersion = new Label();
            lblVersion.Text = "v2.1";
            lblVersion.Font = new Font("Microsoft YaHei", 8);
            lblVersion.ForeColor = Color.LightGray;
            lblVersion.Location = new Point(leftMargin, y);
            lblVersion.Size = new Size(100, 14);
            this.Controls.Add(lblVersion);

            // BackgroundWorker
            coolDownWorker = new BackgroundWorker();
            coolDownWorker.WorkerSupportsCancellation = true;
            coolDownWorker.WorkerReportsProgress = true;
            coolDownWorker.DoWork += CoolDownWorker_DoWork;
            coolDownWorker.ProgressChanged += CoolDownWorker_ProgressChanged;
            coolDownWorker.RunWorkerCompleted += CoolDownWorker_Completed;

            // Temperature refresh timer (2 second interval)
            tempTimer = new System.Windows.Forms.Timer();
            tempTimer.Interval = 2000;
            tempTimer.Tick += TempTimer_Tick;
            tempTimer.Start();
        }

        private void TempTimer_Tick(object sender, EventArgs e)
        {
            // Read temps in background thread to avoid UI freeze
            ThreadPool.QueueUserWorkItem(_ =>
            {
                double cpu = ReadCPUTemperature();
                double? gpu = ReadGPUTemperature();
                this.BeginInvoke((Action)(() =>
                {
                    UpdateTempLabels(cpu, gpu);
                }));
            });
        }

        private void TrackTemp_Scroll(object sender, EventArgs e)
        {
            if (syncingTrack) return;
            syncingTrack = true;
            numTemp.Value = trackTemp.Value;
            syncingTrack = false;
        }

        private void NumTemp_ValueChanged(object sender, EventArgs e)
        {
            if (syncingTrack) return;
            syncingTrack = true;
            if ((int)numTemp.Value != trackTemp.Value)
                trackTemp.Value = (int)numTemp.Value;
            syncingTrack = false;
        }

        private void BtnHibernate_Click(object sender, EventArgs e)
        {
            if (isCooling) return;

            targetTemp = (int)numTemp.Value;
            isCooling = true;
            btnHibernate.Enabled = false;
            trackTemp.Enabled = false;
            numTemp.Enabled = false;
            chkUSBSuspend.Enabled = false;
            chkDeviceWake.Enabled = false;
            chkWOL.Enabled = false;
            chkTaskWake.Enabled = false;

            // Apply power policies in background
            ApplyPowerPolicies();

            // Switch to High Performance for active cooling
            SetHighPerformance();

            lblStatus.Text = string.Format("正在降温... 等待温度降至 {0}°C", targetTemp);
            lblStatus.ForeColor = Color.FromArgb(220, 120, 0);

            coolDownWorker.RunWorkerAsync();
        }

        private void ApplyPowerPolicies()
        {
            try
            {
                // USB selective suspend
                if (chkUSBSuspend.Checked)
                {
                    RunCommand("powercfg",
                        "/SETACVALUEINDEX SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0");
                }

                // Device wake permissions
                if (chkDeviceWake.Checked)
                {
                    Process p = new Process();
                    p.StartInfo.FileName = "powercfg";
                    p.StartInfo.Arguments = "-devicequery wake_armed";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.Start();
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(5000);

                    string[] devices = output.Split(
                        new string[] { "\r\n", "\n" },
                        StringSplitOptions.RemoveEmptyEntries);
                    foreach (string device in devices)
                    {
                        string trimmed = device.Trim();
                        if (trimmed.Length > 0)
                        {
                            RunCommand("powercfg",
                                string.Format("-devicedisablewake \"{0}\"", trimmed));
                        }
                    }
                }

                // WOL
                if (chkWOL.Checked)
                {
                    Process p = new Process();
                    p.StartInfo.FileName = "wmic";
                    p.StartInfo.Arguments =
                        "path Win32_NetworkAdapter where \"NetEnabled=true\" call EnableWakeUp False";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.Start();
                    p.WaitForExit(5000);
                }

                // Task wake timers
                if (chkTaskWake.Checked)
                {
                    RunCommand("powercfg", "-waketimers disable");
                }
            }
            catch { }
        }

        private void SetHighPerformance()
        {
            try
            {
                RunCommand("powercfg",
                    "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
            }
            catch { }
        }

        private void RunCommand(string fileName, string arguments)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = fileName;
                p.StartInfo.Arguments = arguments;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                p.WaitForExit(8000);
            }
            catch { }
        }

        private void ReadTemperatureAsync()
        {
            // Read initial temperature on startup (non-blocking)
            Thread t = new Thread(() =>
            {
                double cpu = ReadCPUTemperature();
                double? gpu = ReadGPUTemperature();
                this.BeginInvoke((Action)(() =>
                {
                    UpdateTempLabels(cpu, gpu);
                }));
            });
            t.IsBackground = true;
            t.Start();
        }

        private void CoolDownWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            DateTime start = DateTime.Now;
            TimeSpan timeout = TimeSpan.FromMinutes(10);

            while (!worker.CancellationPending)
            {
                // Check timeout
                TimeSpan elapsed = DateTime.Now - start;
                if (elapsed > timeout)
                {
                    e.Result = "timeout";
                    return;
                }

                double cpuTemp = ReadCPUTemperature();
                double? gpuTemp = ReadGPUTemperature();

                // Report temps to UI
                TempInfo info = new TempInfo();
                info.CPU = cpuTemp;
                info.GPU = gpuTemp;
                worker.ReportProgress((int)elapsed.TotalSeconds, info);

                // Check if CPU temp is valid and at or below target
                if (cpuTemp > 0 && cpuTemp <= targetTemp)
                {
                    e.Result = "reached";
                    return;
                }

                Thread.Sleep(2000);
            }
        }

        private void CoolDownWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            TempInfo info = e.UserState as TempInfo;
            if (info == null) return;

            UpdateTempLabels(info.CPU, info.GPU);

            int elapsedSec = e.ProgressPercentage;
            int min = elapsedSec / 60;
            int sec = elapsedSec % 60;
            lblStatus.Text = string.Format(
                "降温中 [{0}:{1:D2}] — 目标 {2}°C",
                min, sec, targetTemp);
            lblStatus.ForeColor = Color.FromArgb(220, 120, 0);
        }

        private void CoolDownWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            string result = e.Result as string;

            if (result == "reached")
            {
                lblStatus.Text = string.Format("温度已降至 {0}°C，正在进入 S4 休眠...", targetTemp);
                lblStatus.ForeColor = Color.Green;
            }
            else if (result == "timeout")
            {
                lblStatus.Text = string.Format(
                    "降温超时（10分钟），当前温度未达 {0}°C，直接进入休眠...", targetTemp);
                lblStatus.ForeColor = Color.Red;
            }

            // Enter S4 hibernate
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "shutdown";
                p.StartInfo.Arguments = "/h /f";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
            }
            catch
            {
                lblStatus.Text = "休眠命令执行失败，请手动执行 shutdown /h /f";
                lblStatus.ForeColor = Color.Red;
                isCooling = false;
                btnHibernate.Enabled = true;
            }
        }

        private void UpdateTempLabels(double cpuTemp, double? gpuTemp)
        {
            if (cpuTemp > 0)
            {
                SetTempLabel(lblCPU, "CPU 温度", cpuTemp);
            }
            else
            {
                lblCPU.Text = "CPU 温度：N/A";
                lblCPU.ForeColor = Color.Gray;
            }

            if (gpuTemp.HasValue && gpuTemp.Value > 0)
            {
                SetTempLabel(lblGPU, "GPU 温度", gpuTemp.Value);
            }
            else
            {
                lblGPU.Text = "GPU 温度：N/A";
                lblGPU.ForeColor = Color.Gray;
            }
        }

        private void SetTempLabel(Label lbl, string prefix, double temp)
        {
            lbl.Text = string.Format("{0}：{1:F0} °C", prefix, temp);

            if (temp > 80)
                lbl.ForeColor = Color.FromArgb(220, 30, 0);  // Hot red
            else if (temp > 60)
                lbl.ForeColor = Color.FromArgb(220, 120, 0); // Warm orange
            else if (temp > targetTemp)
                lbl.ForeColor = Color.FromArgb(200, 140, 0); // Mild yellow-orange
            else
                lbl.ForeColor = Color.FromArgb(0, 160, 60);  // Cool green
        }

        // =====================================================
        // CPU Temperature: LHM → WMI MSAcpi → N/A
        // =====================================================
        private double ReadCPUTemperature()
        {
            // Priority 1: LibreHardwareMonitorLib (most reliable, all CPUs)
            if (lhmAvailable && lhmComputer != null)
            {
                try
                {
                    double? lhmTemp = ReadCPUTempFromLHM();
                    if (lhmTemp.HasValue && lhmTemp.Value > 0)
                        return lhmTemp.Value;
                }
                catch { }
            }

            // Priority 2: WMI MSAcpi_ThermalZoneTemperature (fallback)
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    @"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        object tempObj = obj["CurrentTemperature"];
                        if (tempObj != null)
                        {
                            double kelvinTenths = Convert.ToDouble(tempObj);
                            double celsius = (kelvinTenths / 10.0) - 273.15;
                            // Sanity check: valid CPU temps are 0-125°C
                            if (celsius >= 0 && celsius <= 125)
                                return celsius;
                        }
                    }
                }
            }
            catch { }

            return 0;
        }

        private double? ReadCPUTempFromLHM()
        {
            if (lhmComputer == null) return null;

            foreach (var hardware in lhmComputer.Hardware)
            {
                try { hardware.Update(); } catch { continue; }

                // CPU hardware type
                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    // Prefer "CPU Package" or "Core (Tctl/Tdie)" — the package-level sensor
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature &&
                            sensor.Value.HasValue &&
                            sensor.Value > 0)
                        {
                            string name = sensor.Name.ToLower();
                            if (name.Contains("package") || name.Contains("tdie") ||
                                name.Contains("tctl") || name.Contains("cpu"))
                            {
                                return sensor.Value.Value;
                            }
                        }
                    }

                    // Fallback: any temperature sensor on CPU hardware
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature &&
                            sensor.Value.HasValue &&
                            sensor.Value > 0)
                        {
                            return sensor.Value.Value;
                        }
                    }
                }
            }

            return null;
        }

        // =====================================================
        // GPU Temperature: LHM → nvidia-smi → N/A
        // =====================================================
        private double? ReadGPUTemperature()
        {
            // Priority 1: LibreHardwareMonitorLib (all GPUs: NVIDIA/AMD/Intel)
            if (lhmAvailable && lhmComputer != null)
            {
                try
                {
                    double? lhmTemp = ReadGPUTempFromLHM();
                    if (lhmTemp.HasValue && lhmTemp.Value > 0)
                        return lhmTemp.Value;
                }
                catch { }
            }

            // Priority 2: nvidia-smi (NVIDIA dGPU fallback)
            try
            {
                string nvidiaPath = null;
                string[] candidates = new string[] {
                    @"C:\Windows\System32\nvidia-smi.exe",
                    @"C:\Program Files\NVIDIA Corporation\NVSMI\nvidia-smi.exe"
                };
                foreach (string path in candidates)
                {
                    if (System.IO.File.Exists(path))
                    {
                        nvidiaPath = path;
                        break;
                    }
                }
                if (nvidiaPath == null)
                {
                    // Try without full path
                    nvidiaPath = "nvidia-smi.exe";
                }

                Process p = new Process();
                p.StartInfo.FileName = nvidiaPath;
                p.StartInfo.Arguments =
                    "--query-gpu=temperature.gpu --format=csv,noheader,nounits";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                string output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(3000);

                if (p.ExitCode == 0 && output.Length > 0)
                {
                    double temp;
                    if (double.TryParse(output, out temp) && temp > 0)
                        return temp;
                }
            }
            catch { }

            return null;
        }

        private double? ReadGPUTempFromLHM()
        {
            if (lhmComputer == null) return null;

            foreach (var hardware in lhmComputer.Hardware)
            {
                try { hardware.Update(); } catch { continue; }

                // GPU hardware types: NvidiaGpu, AmdGpu, IntelGpu
                if (hardware.HardwareType == HardwareType.GpuNvidia ||
                    hardware.HardwareType == HardwareType.GpuAmd ||
                    hardware.HardwareType == HardwareType.GpuIntel)
                {
                    // Prefer "GPU Core" temperature sensor
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature &&
                            sensor.Value.HasValue &&
                            sensor.Value > 0)
                        {
                            string name = sensor.Name.ToLower();
                            if (name.Contains("core") || name.Contains("gpu"))
                            {
                                return sensor.Value.Value;
                            }
                        }
                    }

                    // Fallback: any temperature sensor on GPU hardware
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature &&
                            sensor.Value.HasValue &&
                            sensor.Value > 0)
                        {
                            return sensor.Value.Value;
                        }
                    }
                }
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (tempTimer != null)
                {
                    tempTimer.Stop();
                    tempTimer.Dispose();
                }
                if (lhmComputer != null)
                {
                    try { lhmComputer.Close(); } catch { }
                }
            }
            base.Dispose(disposing);
        }
    }
}
