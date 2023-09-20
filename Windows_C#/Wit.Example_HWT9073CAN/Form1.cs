using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Wit.SDK.Device.Device.Device.DKey;
using Wit.SDK.Modular.Sensor.Modular.DataProcessor.Constant;
using Wit.SDK.Modular.WitSensorApi.Modular.HWT9073;

namespace Wit.Example_HWT9073CAN
{
    /// <summary>
    /// 程序主窗口
    /// 说明：
    /// 1.本程序是维特智能开发的HWT9073CAN九轴传感器示例程序
    /// 2.适用示例程序前请咨询技术支持,询问本示例程序是否支持您的传感器
    /// 3.使用前请了解传感器的通信协议
    /// 4.本程序只有一个窗口,所有逻辑都在这里
    /// 
    /// Program main window
    /// Description:
    /// 1. This program is the HWT9073CAN 9-axis sensor sample program developed by WitMotion
    /// 2. Please consult technical support before applying the sample program and ask whether the sample program supports your sensor
    /// 3. Understand the communication protocol of the sensor before using it
    /// 4. This program only has one window, all the logic is here
    /// </summary>


    /// <summary>
    /// HWT9073CAN
    /// </summary>
    public partial class Form1 : Form
    {
        private HWT9073CAN HWT9073CAN { get; set; } = new HWT9073CAN();

        /// <summary>
        /// HWT9073CAN支持的波特率
        /// Supported baud rate
        /// </summary>
        private List<int> SupportBaudRateList { get; set; } = new List<int>() { 230400, 2000000 };

        /// <summary>
        /// HWT9073CAN支持的CAN波特率
        /// Supported can baud rate
        /// </summary>
        private List<int> SupportCanRateList { get; set; } = new List<int>() { 100000, 125000, 200000, 250000, 500000, 800000, 1000000 };

        /// <summary>
        /// 控制自动刷新数据线程是否工作
        /// Controls whether the auto flush data thread works
        /// </summary>
        public bool EnableRefreshDataTh { get; private set; }

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 窗体加载时
        /// Form load time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            // 加载串口号到下拉框里   Loading serial port number
            portComboBox_MouseDown(null, null);

            // 加载波特率下拉框 Load baud rate
            for (int i = 0; i < SupportBaudRateList.Count; i++)
            {
                baudComboBox.Items.Add(SupportBaudRateList[i]);
            }

            // 加载CAN波特率下拉框 Load can baud rate
            for (int i = 0; i < SupportCanRateList.Count; i++)
            {
                CANcomboBox.Items.Add(SupportCanRateList[i]);
            }

            // 默认选中230400    Default 230400
            baudComboBox.SelectedItem = 230400;
            // 默认选中250K    Default 250K
            CANcomboBox.SelectedItem = 250000;

            // 启动刷新数据线程 Start refreshing data thread
            Thread thread = new Thread(RefreshDataTh);
            thread.IsBackground = true;
            EnableRefreshDataTh = true;
            thread.Start();
        }

        /// <summary>
        /// 窗体关闭时
        /// Form close time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 关闭刷新数据线程  Close the refresh data thread
            EnableRefreshDataTh = false;
            // 关闭串口 Close serial port
            closeButton_Click(null, null);
        }

        /// <summary>
        /// 鼠标移动到串口号下拉框里时
        /// The mouse moves to the serial port number drop-down box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void portComboBox_MouseDown(object sender, MouseEventArgs e)
        {
            portComboBox.Items.Clear();
            string[] portNameList = SerialPort.GetPortNames();

            for (int i = 0; i < portNameList.Length; i++)
            {
                portComboBox.Items.Add(portNameList[i]);
            }
        }

        /// <summary>
        /// 打开设备
        /// Turn on the device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openButton_Click(object sender, EventArgs e)
        {
            // 获得连接的串口号和波特率 Obtain the serial port number and baud rate for the connection
            string portName;
            int baudrate;
            string canrate;
            byte CANId;
            try
            {
                portName = (string)portComboBox.SelectedItem;
                baudrate = (int)baudComboBox.SelectedItem;
                canrate = CANcomboBox.SelectedItem.ToString();
                CANId = byte.Parse(ModbustextBox.Text.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            // 不重复打开    Open without repeating
            if (HWT9073CAN.IsOpen())
            {
                return;
            }

            // 打开设备 Turn on the device
            try
            {
                HWT9073CAN.SetPortName(portName);
                HWT9073CAN.SetBaudrate(baudrate);
                HWT9073CAN.SetCANId(CANId);
                HWT9073CAN.SetCanRate(canrate);

                HWT9073CAN.Open();
                // 实现记录数据事件 Implement logging data events
                HWT9073CAN.OnRecord += HWT9073CAN_OnRecord;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// 当传感器数据刷新时会调用这里，您可以在这里记录数据
        /// This is called when the sensor data is refreshed, so you can record the data here
        /// </summary>
        /// <param name="HWT9073CAN"></param>
        private void HWT9073CAN_OnRecord(HWT9073CAN HWT9073CAN)
        {
            string text = GetDeviceData(HWT9073CAN);
            Debug.WriteLine(text);
        }

        /// <summary>
        /// 关闭设备
        /// Shut down device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 如果已经打开了设备就关闭设备   Turn off the device if it is already on
                if (HWT9073CAN.IsOpen())
                {
                    HWT9073CAN.OnRecord -= HWT9073CAN_OnRecord;
                    HWT9073CAN.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// 刷新数据线程
        /// Refresh data thread
        /// </summary>
        private void RefreshDataTh()
        {
            while (EnableRefreshDataTh)
            {
                Thread.Sleep(100);
                if (HWT9073CAN.IsOpen())
                {
                    dataRichTextBox.Invoke(new Action(() =>
                    {
                        dataRichTextBox.Text = GetDeviceData(HWT9073CAN);
                    }));
                }

            }
        }

        /// <summary>
        /// 获得设备的数据
        /// Get the device's data
        /// </summary>
        private string GetDeviceData(HWT9073CAN HWT9073CAN)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(HWT9073CAN.GetDeviceName()).Append("\n");
            // 加速度 ACC
            builder.Append("AccX").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.AccX)).Append("g \t");
            builder.Append("AccY").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.AccY)).Append("g \t");
            builder.Append("AccZ").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.AccZ)).Append("g \n");
            // 角速度 Angular velocity
            builder.Append("GyroX").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.AsX)).Append("°/s \t");
            builder.Append("GyroY").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.AsY)).Append("°/s \t");
            builder.Append("GyroZ").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.AsZ)).Append("°/s \n");
            // 角度   Angle
            builder.Append("AngleX").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.AngleX)).Append("° \t");
            builder.Append("AngleY").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.AngleY)).Append("° \t");
            builder.Append("AngleZ").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.AngleZ)).Append("° \n");
            // 磁场   Mag
            builder.Append("MagX").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.HX)).Append("uT \t");
            builder.Append("MagY").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.HY)).Append("uT \t");
            builder.Append("MagZ").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.HZ)).Append("uT \n");
            // 经纬度  Longitude and latitude
            builder.Append("Lon").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.Lon)).Append("′ \t");
            builder.Append("Lat").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.Lat)).Append("′ \n");
            // 端口号  Port
            builder.Append("D0").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.D0)).Append("\t");
            builder.Append("D1").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.D1)).Append("\t");
            builder.Append("D2").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.D2)).Append("\t");
            builder.Append("D3").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.D3)).Append("\n");
            // 四元数  Quaternion
            builder.Append("Q0").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.Q0)).Append("\t");
            builder.Append("Q1").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.Q1)).Append("\t");
            builder.Append("Q2").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.Q2)).Append("\t");
            builder.Append("Q3").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.Q3)).Append("\n");
            // 气压   Barometric
            builder.Append("P").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.Q1)).Append("Pa \t");
            builder.Append("H").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.Q2)).Append("m \t");
            // 温度   Temp
            builder.Append("T").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.T)).Append("℃ \n");
            // 版本号  Version
            builder.Append("VersionNumber").Append(":").Append(HWT9073CAN.GetDeviceData(WitSensorKey.VersionNumber)).Append("\n");
            return builder.ToString();
        }

        /// <summary>
        /// 加计校准
        /// Acceleration calibration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void appliedCalibrationButton_Click(object sender, EventArgs e)
        {
            if (HWT9073CAN.IsOpen() == false)
            {
                return;
            }

            try
            {
                // 解锁寄存器并发送命令   Unlock the register and send the command
                HWT9073CAN.UnlockReg();
                HWT9073CAN.AppliedCalibration();
                // 下面两行与上面等价,推荐使用上面的    Equivalent to above
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x69, 0x88, 0xb5 });
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x01, 0x01, 0x00 });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 读取03寄存器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void readReg03Button_Click(object sender, EventArgs e)
        {
            if (HWT9073CAN.IsOpen() == false)
            {
                return;
            }
            try
            {
                // 等待时长 Waiting time
                int waitTime = 150;
                // 发送读取命令，并且等待传感器返回数据，如果没读上来可以将 waitTime 延长，或者多读几次
                //Send a read command and wait for the sensor to return data. If it is not read, the waitTime can be extended or read several more times
                HWT9073CAN.SendReadReg(0x03, waitTime);
                // 下面这行和上面等价推荐使用上面的 Equivalent to above
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x27, 0x03, 0x00 }, waitTime);

                short? reg03Value = HWT9073CAN.GetDeviceData(new ShortKey("03"));
                MessageBox.Show($"寄存器03值为 : {reg03Value}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 设置回传速率10Hz
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void returnRate10_Click(object sender, EventArgs e)
        {
            if (HWT9073CAN.IsOpen() == false)
            {
                return;
            }
            try
            {
                // 解锁寄存器并发送命令   Unlock the register and send the command
                HWT9073CAN.UnlockReg();
                HWT9073CAN.SetReturnRate(0x06);
                // 下面两行与上面等价,推荐使用上面的    Equivalent to above
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x69, 0x88, 0xb5 });
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x03, 0x06, 0x00 });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 设置回传速率50Hz
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void returnRate50_Click(object sender, EventArgs e)
        {
            if (HWT9073CAN.IsOpen() == false)
            {
                return;
            }
            try
            {
                // 解锁寄存器并发送命令   Unlock the register and send the command
                HWT9073CAN.UnlockReg();
                HWT9073CAN.SetReturnRate(0x08);
                // 下面两行与上面等价,推荐使用上面的    Equivalent to above
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x69, 0x88, 0xb5 });
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x03, 0x08, 0x00 });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 设置带宽20Hz
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bandWidth20_Click(object sender, EventArgs e)
        {
            if (HWT9073CAN.IsOpen() == false)
            {
                return;
            }
            try
            {
                // 解锁寄存器并发送命令   Unlock the register and send the command
                HWT9073CAN.UnlockReg();
                HWT9073CAN.SetBandWidth(0x04);
                // 下面两行与上面等价,推荐使用上面的    Equivalent to above
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x69, 0x88, 0xb5 });
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x1F, 0x04, 0x00 });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 设置带宽256Hz
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bandWidth256_Click(object sender, EventArgs e)
        {
            if (HWT9073CAN.IsOpen() == false)
            {
                return;
            }
            try
            {
                // 解锁寄存器并发送命令   Unlock the register and send the command
                HWT9073CAN.UnlockReg();
                HWT9073CAN.SetBandWidth(0x00);
                // 下面两行与上面等价,推荐使用上面的    Equivalent to above
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x69, 0x88, 0xb5 });
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x1F, 0x00, 0x00 });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 设置设备地址为50
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetAddrBtn_Click(object sender, EventArgs e)
        {
            if (HWT9073CAN.IsOpen() == false)
            {
                return;
            }
            try
            {
                // 解锁寄存器并发送命令   Unlock the register and send the command
                HWT9073CAN.UnlockReg();
                byte CANId = byte.Parse(ModbustextBox.Text.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                HWT9073CAN.SetCANId(CANId);
                // 下面两行与上面等价,推荐使用上面的    Equivalent to above
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x69, 0x88, 0xb5 });
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x1A, 0x50, 0x00, });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 开始磁场校准
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startFieldCalibrationButton_Click(object sender, EventArgs e)
        {
            if (HWT9073CAN.IsOpen() == false)
            {
                return;
            }
            try
            {
                // 解锁寄存器并发送命令   Unlock the register and send the command
                HWT9073CAN.UnlockReg();
                HWT9073CAN.StartFieldCalibration();
                // 下面两行与上面等价,推荐使用上面的    Equivalent to above
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x69, 0x88, 0xb5 });
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x01, 0x07, 0x00 });
                MessageBox.Show("开始磁场校准,请绕传感器XYZ三轴各转一圈,转完以后点击【结束磁场校准】");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 结束磁场校准
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void endFieldCalibrationButton_Click(object sender, EventArgs e)
        {
            if (HWT9073CAN.IsOpen() == false)
            {
                return;
            }
            try
            {
                // 解锁寄存器并发送命令   Unlock the register and send the command
                HWT9073CAN.UnlockReg();
                HWT9073CAN.EndFieldCalibration();
                // 下面两行与上面等价,推荐使用上面的    Equivalent to above
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x69, 0x88, 0xb5 });
                //HWT9073CAN.SendProtocolData(new byte[] { 0xff, 0xaa, 0x01, 0x00, 0x00 });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
