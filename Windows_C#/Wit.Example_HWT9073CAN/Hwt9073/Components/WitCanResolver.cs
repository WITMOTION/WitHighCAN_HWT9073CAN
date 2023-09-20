using System;
using System.Collections.Generic;
using System.Linq;
using Wit.SDK.Device.Device.Device.DKey;
using Wit.SDK.Modular.Sensor.Device;
using Wit.SDK.Modular.Sensor.Modular.ProtocolResolver.Interface;
using Wit.SDK.Modular.Sensor.Modular.Resolver.Utils;
using Wit.SDK.Utils;

namespace Wit.SDK.Modular.Sensor.Modular.ProtocolResolver.Roles
{

    /**
     * 维特CAN协议解析器
     */
    public class WitCanResolver : IProtocolResolver
    {
        /// <summary>
        /// 接收的原始数据
        /// </summary>
        private string ActiveStringDataBuffer = "";

        /// <summary>
        /// 主动接收的byte缓存
        /// </summary>
        private List<byte> ActiveByteDataBuffer = new List<byte>();

        /// <summary>
        /// 临时Byte
        /// </summary>
        private byte[] ActiveByteTemp = new byte[1000];

        /// <summary>
        /// 临时数据Byte
        /// </summary>
        private byte[] DataByteTemp = new byte[1000];


        /// <summary>
        /// 处理被动接收的数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="deviceModel"></param>
        public override void OnReceiveData(DeviceModel deviceModel, byte[] data)
        {
            // 设备地址
            string addrStr = deviceModel.GetAddr();
            // 没有设备地址就不接收
            if (string.IsNullOrEmpty(addrStr)) return;
            // 设备地址
            int addr = int.Parse(addrStr);
            ActiveByteDataBuffer.AddRange(data);
 

            // 开头需要是AT
            while (ActiveByteDataBuffer.Count > 2
                && ( ActiveByteDataBuffer[0] != 0x41 
                || ActiveByteDataBuffer[1] != 0x54))
            {
                ActiveByteDataBuffer.RemoveAt(0);
            }

            // 41 54 0A 00 00 00 08 55 51 28 01 28 FF 22 F8 0D 0A
            while (ActiveByteDataBuffer.Count >= 17)
            {
                ActiveByteTemp = ActiveByteDataBuffer.GetRange(0, 17).ToArray();
                ActiveByteDataBuffer.RemoveRange(0, 17);

                if (ActiveByteTemp[15] != 0x0d || ActiveByteTemp[16] != 0x0a) {
                    break;
                }

                // 获得CANid
                int frameId = (int)((uint)ActiveByteTemp[2] << 24 | (uint)ActiveByteTemp[3] << 16 | (uint)ActiveByteTemp[4] << 8 | (uint)ActiveByteTemp[5] << 0);
                bool bExt = (frameId & 0x04) == 0x04;
                bool bRTR = (frameId & 0x02) == 0x02;
                int deviceId;
                if (bExt)
                {
                    deviceId = (frameId >> 3 + 24 & 0x1f) + (frameId >> 3 + 16 & 0xff) + (frameId >> 3 + 8 & 0xff) + (frameId >> 3 + 0 & 0xff);
                }
                else
                {
                    deviceId = (frameId >> 3 + 18 + 8 & 0x07) + (frameId >> 3 + 18 & 0xff);
                }

                // 如果数据id等于设备id
                if (deviceId == addr)
                {
                    short[] Pack = new short[3];
                    Pack[0] = BitConverter.ToInt16(ActiveByteTemp, 9);
                    Pack[1] = BitConverter.ToInt16(ActiveByteTemp, 11);
                    Pack[2] = BitConverter.ToInt16(ActiveByteTemp, 13);
                    // 标识位
                    string Identify = ActiveByteTemp[8].ToString("X");
                    deviceModel.SetDeviceData(new ShortKey(Identify + "_0"), Pack[0]);
                    deviceModel.SetDeviceData(new ShortKey(Identify + "_1"), Pack[1]);
                    deviceModel.SetDeviceData(new ShortKey(Identify + "_2"), Pack[2]);
                }
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="outData"></param>
        /// <param name="deviceModel"></param>
        public override void OnReadData(DeviceModel deviceModel, byte[] outData, int delay = -1)
        {

            delay = AutoDelayUtils.GetAutoDelay(delay, deviceModel);

            // 设备地址
            string addrStr = deviceModel.GetAddr();
            // 没有设备地址就不发送
            if (string.IsNullOrEmpty(addrStr)) return;
            // 获得要发送的命令
            outData = GetSendCommand(int.Parse(addrStr), 0, 0, outData);
            byte[] returnData;
            deviceModel.SendData(outData, out returnData, true, delay);
            if (outData != null && returnData.Length >= 11 && outData[9] == 0x27)
            {
                // 查找返回的数据
                returnData = findReturnData(outData, returnData);
                if (returnData != null && returnData.Length == 17)
                {
                    int reg = outData[11] << 8 | outData[10];
                    byte[] regData = returnData.Skip(9).Take(6).ToArray();
                    for (int j = 0; regData != null && j < regData.Length - 1; j += 2)
                    {
                        string key = string.Format("{0:X2}", reg++);
                        short value = (short)(regData[j + 1] << 8 | regData[j]);
                        deviceModel.SetDeviceData(new ShortKey(key), value);
                    }
                }
            }
        }

        /// <summary>
        /// 获得要发送的命令
        /// </summary>
        /// <param name="hexFrameId">帧ID(10进制)</param>
        /// <param name="frameFormat">帧格式 0=标准 1=扩展</param>
        /// <param name="frmaeType">帧类型 0=数据 ; 1=远程</param>
        /// <param name="datas">数据</param>
        public byte[] GetSendCommand(int FrameId, int frameFormat, int frmaeType, byte[] dataByte)
        {
            FrameId = frameFormat == 1 ? FrameId << 3 | 0x04 : FrameId <<= 21;
            if (frmaeType == 1) { FrameId |= 0x02; }
            int iLength = dataByte.Length;
            List<byte> sendByte = new List<byte>();
            for (int i = 0; i < 8; i = i + 8)
            {
                int iL = 8;
                if (iLength - i < 8) iL = iLength - i;
                byte[] byteTemp = new byte[iL + 2 + 4 + 1 + 2];
                byteTemp[0] = (byte)'A';
                byteTemp[1] = (byte)'T';
                for (int j = 0; j < 4; j++)
                    byteTemp[2 + j] = (byte)(FrameId >> (3 - j) * 8 & 0xff);
                byteTemp[6] = (byte)iL;
                for (int j = 0; j < iL; j++)
                    byteTemp[7 + j] = dataByte[i + j];
                byteTemp[7 + iL] = (byte)'\r';
                byteTemp[8 + iL] = (byte)'\n';
                sendByte.Clear();
                for (int n = 0; n < 9 + iL; n++) { sendByte.Add(byteTemp[n]); }
            }
            return sendByte.ToArray();
        }



        /// <summary>
        /// 查找返回的数据
        /// </summary>
        /// <param name="returnData"></param>
        /// <returns></returns>
        public byte[] findReturnData(byte[] sendData, byte[] returnData)
        {

            //41 54 0A 00 00 00 08 55 5F 00 00 00 00 00 00 0D 0A 传感器返回数据格式
            byte[] tempArr = new byte[0];
            for (int i = 0; i < returnData.Length; i++)
            {
                tempArr = returnData.Skip(i).Take(17).ToArray(); ;

                if (tempArr.Length == 17 &&
                    tempArr[0] == sendData[0] &&
                    tempArr[1] == sendData[1] &&
                    tempArr[2] == sendData[2] &&
                    tempArr[3] == sendData[3] &&
                    tempArr[4] == sendData[4] &&
                    tempArr[5] == sendData[5] &&
                    tempArr[7] == 0x55 &&
                    tempArr[8] == 0x5F
                    )
                {
                    return tempArr;
                }
            }
            return null;
        }
    }
}
