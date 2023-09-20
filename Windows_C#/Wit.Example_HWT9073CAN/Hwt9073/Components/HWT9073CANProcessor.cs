using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Wit.SDK.Modular.Sensor.Device;
using Wit.SDK.Modular.Sensor.Modular.DataProcessor.Constant;
using Wit.SDK.Modular.Sensor.Modular.DataProcessor.Interface;
using Wit.SDK.Modular.Sensor.Modular.DataProcessor.Utils;
using Wit.SDK.Modular.Sensor.Utils;
using Wit.SDK.Sensor.Device.Constant;

namespace Wit.SDK.Modular.Sensor.Modular.DataProcessor.Roles
{
    /// <summary>
    /// HWT9073-CAN协议数据处理器 
    /// </summary>
    public class HWT9073CANProcessor : IDataProcessor
    {
        
        /// <summary>
        /// 记录key值切换器
        /// </summary>
        private RecordKeySwitch RecordKeySwitch = new RecordKeySwitch();

        /// <summary>
        /// 记录触发器列表
        /// </summary>
        public List<string> UpdateKeys = new List<string> { "50_0", "51_0", "52_0", "54_0" };

        public DeviceModel DeviceModel { get; private set; }
        /// <summary>
        /// 读取必要参数线程
        /// </summary>
        private Thread ReadParamThread = null;

        public override void OnOpen(DeviceModel deviceModel)
        {
            // 找设备的CAN波特率
            DeviceModel = deviceModel;

            // 设置tll转can
            SetTTLCAN(deviceModel);

            // 传入刷新数据的key值
            RecordKeySwitch.Open(deviceModel, UpdateKeys);
            deviceModel.OnKeyUpdate -= DeviceModel_OnKeyUpdate;
            deviceModel.OnKeyUpdate += DeviceModel_OnKeyUpdate;

            // 自动读取数据
            this.DeviceModel = deviceModel;
            ReadParamThread = new Thread(ReadDataThread) { IsBackground = true };
            ReadParamThread.Start();
        }

        /// <summary>
        /// 自动读取数据线程
        /// </summary>
        private void ReadDataThread()
        {
            try
            {
                while (true)
                {
                    if (ReadMagType(DeviceModel)  
                    && ReadVersionNumberReg(DeviceModel)
                    && ReadSerialNumberReg(DeviceModel))
                    {
                        return;
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception)
            {

                ;
            }

        }

        /// <summary>
        /// 解算角度
        /// </summary>
        /// <param name="deviceModel"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void DeviceModel_OnKeyUpdate(DeviceModel deviceModel, string key, object value)
        {
            // 当角度第三包key刷新时来计算角度
            if ("53_2".Equals(key))
            {
                var reg53_0 = deviceModel.GetDeviceData("53_0");
                var reg53_1 = deviceModel.GetDeviceData("53_1");
                var reg53_2 = deviceModel.GetDeviceData("53_2");

                // 解算角度
                if (string.IsNullOrEmpty(reg53_0) == false &&
                    string.IsNullOrEmpty(reg53_1) == false &&
                    string.IsNullOrEmpty(reg53_2) == false)
                {
                    int hReg;
                    int lReg;

                    if (int.Parse(reg53_0) == 1)
                    {
                        hReg = (ushort)short.Parse(reg53_2);
                        lReg = (ushort)short.Parse(reg53_1);
                        double AngleX = (hReg << 16 | lReg) / 1000.0;
                        deviceModel.SetDeviceData(WitSensorKey.AngleX, AngleX);
                    }
                    else if (int.Parse(reg53_0) == 2)
                    {
                        hReg = (ushort)int.Parse(reg53_2);
                        lReg = (ushort)int.Parse(reg53_1);                      
                        double AngleY = (hReg << 16 | lReg) / 1000.0;
                        deviceModel.SetDeviceData(WitSensorKey.AngleY, AngleY);
                    }
                    else if (int.Parse(reg53_0) == 3)
                    {
                        hReg = (ushort)int.Parse(reg53_2);
                        lReg = (ushort)int.Parse(reg53_1);  
                        double AngleZ = (hReg << 16 | lReg) / 1000.0;
                        deviceModel.SetDeviceData(WitSensorKey.AngleZ, AngleZ);
                    }
                }
            }
        }

        /// <summary>
        /// 销毁时
        /// </summary>
        public override void OnClose()
        {
            DeviceModel.OnKeyUpdate -= DeviceModel_OnKeyUpdate;
            // 关闭key值切换器
            RecordKeySwitch.Close();
            if (ReadParamThread != null)
            {
                try
                {
                    ReadParamThread.Abort();
                }
                catch (Exception ex)
                {

                    ;
                }
            }
        }


        /// <summary>
        /// 设置TTL-CAN模块
        /// </summary>
        /// <param name="deviceModel"></param>
        private void SetTTLCAN(DeviceModel deviceModel)
        {
            byte[] returnData;
            // 设置CAN端波特率
            deviceModel.SendData(Encoding.Default.GetBytes("AT+CG\r\n"), out returnData);
            Thread.Sleep(50);

            int canBand = 0;

            string canb = deviceModel.GetDeviceData(InnerKeys.CanBaud);

            if (canb!=null&&int.TryParse(canb,out canBand))
            {
                deviceModel.SendData(Encoding.Default.GetBytes($"AT+CAN_BAUD={canBand}\r\n"), out returnData);
                Thread.Sleep(50);
            }
            // 切换到AT指令模式
            deviceModel.SendData(Encoding.Default.GetBytes("AT+AT\r\n"), out returnData);
        }

        /// <summary>
        /// 数据更新时
        /// </summary>
        /// <param name="deviceModel"></param>
        public override void OnUpdate(DeviceModel deviceModel)
        {
            var reg50_0 = deviceModel.GetDeviceData("50_0");
            var reg50_1 = deviceModel.GetDeviceData("50_1");
            var reg50_2 = deviceModel.GetDeviceData("50_2");

            var reg51_0 = deviceModel.GetDeviceData("51_0");
            var reg51_1 = deviceModel.GetDeviceData("51_1");
            var reg51_2 = deviceModel.GetDeviceData("51_2");

            var reg52_0 = deviceModel.GetDeviceData("52_0");
            var reg52_1 = deviceModel.GetDeviceData("52_1");
            var reg52_2 = deviceModel.GetDeviceData("52_2");

            var reg54_0 = deviceModel.GetDeviceData("54_0");
            var reg54_1 = deviceModel.GetDeviceData("54_1");
            var reg54_2 = deviceModel.GetDeviceData("54_2");

            var reg72 = deviceModel.GetDeviceData("72");

            // 版本号
            var reg2e = deviceModel.GetDeviceData("2E");// 版本号
            var reg2f = deviceModel.GetDeviceData("2F");// 版本号

            // 如果有版本号
            if (string.IsNullOrEmpty(reg2e) == false &&
                string.IsNullOrEmpty(reg2f) == false)
            {
                var reg2eValue = (ushort)short.Parse(reg2e);
                var vbytes = BitConverter.GetBytes((ushort)short.Parse(reg2e)).Concat(BitConverter.GetBytes((ushort)short.Parse(reg2f))).ToArray();
                UInt32 tempVerSion = BitConverter.ToUInt32(vbytes, 0);
                string sbinary = Convert.ToString(tempVerSion, 2);
                sbinary = ("").PadLeft((32 - sbinary.Length), '0') + sbinary;
                if (sbinary.StartsWith("1"))//新版本号
                {
                    string tempNewVS = Convert.ToUInt32(sbinary.Substring(4 - 3, 14 + 3), 2).ToString();
                    tempNewVS += "." + Convert.ToUInt32(sbinary.Substring(18, 6), 2);
                    tempNewVS += "." + Convert.ToUInt32(sbinary.Substring(24), 2);
                    deviceModel.SetDeviceData(WitSensorKey.VersionNumber, tempNewVS);
                }
                else
                {
                    deviceModel.SetDeviceData(WitSensorKey.VersionNumber, reg2eValue.ToString());
                }
            }

            // 序列号
            var reg7f = deviceModel.GetDeviceData("7F");// 序列号
            var reg80 = deviceModel.GetDeviceData("80");// 序列号
            var reg81 = deviceModel.GetDeviceData("81");// 序列号
            var reg82 = deviceModel.GetDeviceData("82");// 序列号
            var reg83 = deviceModel.GetDeviceData("83");// 序列号
            var reg84 = deviceModel.GetDeviceData("84");// 序列号
            if (string.IsNullOrEmpty(reg7f) == false &&
                string.IsNullOrEmpty(reg80) == false &&
                string.IsNullOrEmpty(reg81) == false &&
                string.IsNullOrEmpty(reg82) == false &&
                string.IsNullOrEmpty(reg83) == false &&
                string.IsNullOrEmpty(reg84) == false)
            {
                var sbytes = BitConverter.GetBytes(short.Parse(reg7f))
                    .Concat(BitConverter.GetBytes(short.Parse(reg80)))
                    .Concat(BitConverter.GetBytes(short.Parse(reg81)))
                    .Concat(BitConverter.GetBytes(short.Parse(reg82)))
                    .Concat(BitConverter.GetBytes(short.Parse(reg83)))
                    .Concat(BitConverter.GetBytes(short.Parse(reg84)))
                    .ToArray();
                string sn = Encoding.Default.GetString(sbytes);
                deviceModel.SetDeviceData(WitSensorKey.SerialNumber, sn);
            }

            // 解算片上时间
            // 解算加速度
            if (!string.IsNullOrEmpty(reg50_0) && !string.IsNullOrEmpty(reg50_1) && !string.IsNullOrEmpty(reg50_2))
            {
                // 解算数据,并且保存到设备数据里
                var yy = 2000 + (byte)int.Parse(reg50_0);
                var MM = (byte)(int.Parse(reg50_0) >> 8);
                var dd = (byte)int.Parse(reg50_1);
                var hh = (byte)(int.Parse(reg50_1) >> 8);
                var mm = (byte)int.Parse(reg50_2);
                var ss = (byte)(int.Parse(reg50_2) >> 8);
                var ms = (0).ToString("000");// int.Parse(reg50_3);

                deviceModel.SetDeviceData(WitSensorKey.ChipTime, $"{yy}-{MM}-{dd} {hh}:{mm}:{ss}.{ms}");
            }

            // 解算加速度
            if (!string.IsNullOrEmpty(reg51_0) && !string.IsNullOrEmpty(reg51_1) && !string.IsNullOrEmpty(reg51_2))
            {
                var AX = Math.Round(double.Parse(reg51_0) / 32768.0 * 16, 3);
                var AY = Math.Round(double.Parse(reg51_1) / 32768.0 * 16, 3);
                var AZ = Math.Round(double.Parse(reg51_2) / 32768.0 * 16, 3);
                deviceModel.SetDeviceData(WitSensorKey.AccX, AX);
                deviceModel.SetDeviceData(WitSensorKey.AccY, AY);
                deviceModel.SetDeviceData(WitSensorKey.AccZ, AZ);
            }

            // 解算角速度
            if (!string.IsNullOrEmpty(reg52_0) && !string.IsNullOrEmpty(reg52_1) && !string.IsNullOrEmpty(reg52_2))
            {
                var WX = Math.Round(double.Parse(reg52_0) / 32768.0 * 2000, 3);
                var WY = Math.Round(double.Parse(reg52_1) / 32768.0 * 2000, 3);
                var WZ = Math.Round(double.Parse(reg52_2) / 32768.0 * 2000, 3);
                deviceModel.SetDeviceData(WitSensorKey.AsX, WX);
                deviceModel.SetDeviceData(WitSensorKey.AsY, WY);
                deviceModel.SetDeviceData(WitSensorKey.AsZ, WZ);
            }

            // 解算磁场
            if (!string.IsNullOrEmpty(reg54_0) && !string.IsNullOrEmpty(reg54_1) && !string.IsNullOrEmpty(reg54_2) && !string.IsNullOrEmpty(reg72))
            {
                short type = short.Parse(reg72);
                deviceModel.SetDeviceData(WitSensorKey.HX, GetMagData(short.Parse(reg72), double.Parse(reg54_0)));
                deviceModel.SetDeviceData(WitSensorKey.HY, GetMagData(short.Parse(reg72), double.Parse(reg54_1)));
                deviceModel.SetDeviceData(WitSensorKey.HZ, GetMagData(short.Parse(reg72), double.Parse(reg54_2)));
                deviceModel.SetDeviceData(WitSensorKey.HM, Math.Round(Math.Sqrt(Math.Pow(DipSensorMagHelper.GetMagToUt(type, double.Parse(reg54_0)), 2) + 
                                                           Math.Pow(DipSensorMagHelper.GetMagToUt(type, double.Parse(reg54_1)), 2) + 
                                                           Math.Pow(DipSensorMagHelper.GetMagToUt(type, double.Parse(reg54_2)), 2)), 2));
            }
        }

        /// <summary>
        /// 异步读取磁场类型寄存器
        /// </summary>
        private bool ReadMagType(DeviceModel deviceModel)
        {
            bool bRET = false;
            if (deviceModel.GetDeviceData("72") == null)
            {
                // 读取72磁场类型寄存器,后面解析磁场的时候要用到
                deviceModel.AsyncReadData(new byte[] { 0xff, 0xaa, 0x27, 0x72, 0x00 }, () => { });
                Thread.Sleep(20);
            }
            else
            {
                bRET = true;
            }
            return bRET;
        }

        /// <summary>
        /// 异步读取版本号
        /// </summary>
        /// <param name="deviceModel"></param>
        private bool ReadVersionNumberReg(DeviceModel deviceModel)
        {
            bool bRET = false;
            if (deviceModel.GetDeviceData("2E") == null && deviceModel.GetDeviceData("2F") == null)
            {
                // 读版本号
                deviceModel.AsyncReadData(new byte[] { 0xff, 0xaa, 0x27, 0x2E, 0x00 }, () => { });
                Thread.Sleep(20);
            }
            else
            {
                bRET = true;
            }
            return bRET;
        }


        /// <summary>
        /// 读取序列号寄存器
        /// </summary>
        /// <param name="deviceModel"></param>
        private bool ReadSerialNumberReg(DeviceModel deviceModel)
        {
            bool bRET = false;
            // 读序列号
            if (deviceModel.GetDeviceData("7F") == null && deviceModel.GetDeviceData("82") == null)
            {
                // 读序列号
                deviceModel.ReadData(new byte[] { 0xff, 0xaa, 0x27, 0x7F, 0x00 });
                deviceModel.ReadData(new byte[] { 0xff, 0xaa, 0x27, 0x7F + 3, 0x00 });
            }
            else
            {
                bRET = true;
            }
            return bRET;
        }

        /// <summary>
        /// 磁场转换标准单位uT
        /// </summary>
        /// <param name="magData"></param>
        /// <returns></returns>
        public static double GetMagData(short reg72, double magData)
        {
            return DipSensorMagHelper.GetMagToUt(reg72, magData);
        }
    }

}
