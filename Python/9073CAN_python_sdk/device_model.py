# coding:UTF-8
import threading
import time
import serial
import struct
from serial import SerialException


# 串口配置 Serial Port Configuration
class SerialConfig:
    # 串口号
    portName = ''

    # 波特率
    baud = 2000000

    # Can波特率
    canBaud = 250000


# 设备实例 Device instance
class DeviceModel:
    # region 属性 attribute
    # 设备名称 deviceName
    deviceName = "我的设备"

    # 设备数据字典 Device Data Dictionary
    deviceData = {}

    # 设备是否开启
    isOpen = False

    # 串口 Serial port
    serialPort = None

    # 串口配置 Serial Port Configuration
    serialConfig = SerialConfig()

    # 临时数组 Temporary array
    TempBytes = []

    # 起始寄存器 Start register
    statReg = None

    # 模式(默认AT指令模式)  AT mode
    isAT = True

    # can id及数据模式 Can ID and data mode
    canid = []
    # endregion

    def __init__(self, deviceName, portName, baud, canBaud):
        print("初始化设备模型")
        # 设备名称（自定义） Device Name
        self.deviceName = deviceName
        # 串口号 Serial port number
        self.serialConfig.portName = portName
        # 串口波特率 baud
        self.serialConfig.baud = baud
        # 串口CAN波特率 Can baud
        self.serialConfig.canBaud = canBaud * 1000

    # region 获取设备数据 Obtain device data
    # 设置设备数据 Set device data
    def set(self, key, value):
        # 将设备数据存到键值 Saving device data to key values
        self.deviceData[key] = value

    # 获得设备数据 Obtain device data
    def get(self, key):
        # 从键值中获取数据，没有则返回None Obtaining data from key values
        if key in self.deviceData:
            return self.deviceData[key]
        else:
            return None

    # 删除设备数据 Delete device data
    def remove(self, key):
        # 删除设备键值
        del self.deviceData[key]
    # endregion

    # 打开设备 open Device
    def openDevice(self):
        # 先关闭端口 Turn off the device first
        self.closeDevice()
        try:
            self.serialPort = serial.Serial(self.serialConfig.portName, self.serialConfig.baud, timeout=0.5)
            self.isOpen = True
            print("{}已打开".format(self.serialConfig.portName))

            # 设置USB-CAN模块CAN波特率 Set USB-CAN module CAN baud rate
            self.sendData("AT+CG\r\n".encode())
            time.sleep(0.1)
            self.sendData("AT+CAN_BAUD={}\r\n".format(self.serialConfig.canBaud).encode())
            print("设置CAN波特率为{}".format(self.serialConfig.canBaud))
            time.sleep(0.1)
            self.sendData("AT+AT\r\n".encode())

            # 开启一个线程持续监听串口数据 Start a thread to continuously listen to serial port data
            t = threading.Thread(target=self.readDataTh, args=("Data-Received-Thread", 10,))
            t.start()
            print("设备打开成功")
        except SerialException:
            print("打开" + self.serialConfig.portName + "失败")

    # 监听串口数据线程 Listening to serial data threads
    def readDataTh(self, threadName, delay):
        print("启动" + threadName)
        while True:
            # 如果串口打开了
            if self.isOpen:
                try:
                    tLen = self.serialPort.inWaiting()
                    if tLen > 0:
                        data = self.serialPort.read(tLen)
                        self.onDataReceived(data)
                except Exception as ex:
                    print(ex)
            else:
                time.sleep(0.1)
                print("串口未打开")
                break

    # 关闭设备  close Device
    def closeDevice(self):
        if self.serialPort is not None:
            self.serialPort.close()
            print("端口关闭了")
        self.isOpen = False
        print("设备关闭了")

    # region 数据解析 data analysis
    # 串口数据处理  Serial port data processing
    def onDataReceived(self, data):
        tempdata = bytes.fromhex(data.hex())
        # AT指令模式  AT mode
        if self.isAT:
            for val in tempdata:
                self.TempBytes.append(val)
                if len(self.TempBytes) > 7:
                    # AT
                    if not ((self.TempBytes[0] == 0x41) and (self.TempBytes[1] == 0x54)):
                        del self.TempBytes[0]
                        continue
                    tLen = len(self.TempBytes)
                    # 长度验证 Length verification
                    if tLen == self.TempBytes[6] + 9:
                        if not (self.TempBytes[15] == 0x0D and self.TempBytes[16] == 0x0A):
                            del self.TempBytes[0]
                            continue
                        # 协议头解析 Protocol header parsing
                        self.processProtocol(self.TempBytes[2:6])
                        # 数据解析 Data parsing
                        self.processData(self.TempBytes[7:15])
                        self.TempBytes.clear()
        # 透传模式 Transparent mode
        else:
            for val in tempdata:
                self.TempBytes.append(val)
                if self.TempBytes[0] != 0x55:
                    del self.TempBytes[0]
                    continue
                tLen = len(self.TempBytes)
                if tLen == 8:
                    self.processData(self.TempBytes)
                    self.set("canmode_1", "透传")
                    self.set("canmode_2", " ")
                    self.TempBytes.clear()

    # CANID解析
    def processProtocol(self, Bytes):
        self.canid = Bytes
        bytes_data = bytearray(Bytes)
        # 拿到二进制序列 Get the binary sequence
        binary_data = bin(struct.unpack("!I", bytes_data)[0])[2:].zfill(32)
        if binary_data[30] == '0':
            self.set("canmode_1", "数据帧")
        else:
            self.set("canmode_1", "远程帧")
        if binary_data[29] == '0':
            self.set("canmode_2", "标准")
            can_id = int(binary_data[:11], 2)
            self.set("CanID", can_id)
        else:
            self.set("canmode_2", "拓展")
            can_id = int(binary_data[:29], 2)
            self.set("CanID", can_id)

    # 数据解析 data analysis
    def processData(self, Bytes):
        # 时间 Time
        if Bytes[1] == 0x50:
            pass
        # 加速度 Acceleration
        elif Bytes[1] == 0x51:
            Ax = self.getSignInt16(Bytes[3] << 8 | Bytes[2]) / 32768 * 16
            Ay = self.getSignInt16(Bytes[5] << 8 | Bytes[4]) / 32768 * 16
            Az = self.getSignInt16(Bytes[7] << 8 | Bytes[6]) / 32768 * 16
            self.set("AccX", round(Ax, 3))
            self.set("AccY", round(Ay, 3))
            self.set("AccZ", round(Az, 3))
        # 角速度 Angular velocity
        elif Bytes[1] == 0x52:
            Gx = self.getSignInt16(Bytes[3] << 8 | Bytes[2]) / 32768 * 2000
            Gy = self.getSignInt16(Bytes[5] << 8 | Bytes[4]) / 32768 * 2000
            Gz = self.getSignInt16(Bytes[7] << 8 | Bytes[6]) / 32768 * 2000
            self.set("AsX", round(Gx, 3))
            self.set("AsY", round(Gy, 3))
            self.set("AsZ", round(Gz, 3))
        # 角度 Angle
        elif Bytes[1] == 0x53:
            if Bytes[2] == 0x01:
                AngX = self.getSignInt32(Bytes[7] << 24 | Bytes[6] << 16 | Bytes[5] << 8 | Bytes[4]) / 1000
                self.set("AngX", round(AngX, 3))
            elif Bytes[2] == 0x02:
                AngY = self.getSignInt32(Bytes[7] << 24 | Bytes[6] << 16 | Bytes[5] << 8 | Bytes[4]) / 1000
                self.set("AngY", round(AngY, 3))
            elif Bytes[2] == 0x03:
                AngZ = self.getSignInt32(Bytes[7] << 24 | Bytes[6] << 16 | Bytes[5] << 8 | Bytes[4]) / 1000
                self.set("AngZ", round(AngZ, 3))
        # 磁场 Magnetic field
        elif Bytes[1] == 0x54:
            Hx = self.getSignInt16(Bytes[3] << 8 | Bytes[2]) * 13 / 1000
            Hy = self.getSignInt16(Bytes[5] << 8 | Bytes[4]) * 13 / 1000
            Hz = self.getSignInt16(Bytes[7] << 8 | Bytes[6]) * 13 / 1000
            self.set("HX", round(Hx, 3))
            self.set("HY", round(Hy, 3))
            self.set("HZ", round(Hz, 3))
        # 读取回传 Read callback
        elif Bytes[1] == 0x5F:
            value = self.getSignInt16(Bytes[3] << 8 | Bytes[2])
            self.set(str(self.statReg), value)
            print("读取数据返回  reg:{}   value:{} ".format(self.statReg, value))
        else:
            pass

    # 获得int16有符号数 Obtain int16 signed number
    def getSignInt16(self, num):
        if num >= pow(2, 15):
            num -= pow(2, 16)
        return num

    # 获得int32有符号数 Obtain int32 signed number
    def getSignInt32(self, num):
        if num >= pow(2, 31):
            num -= pow(2, 32)
        return num
    # endregion

    # 发送串口数据 Sending serial port data
    def sendData(self, data):
        try:
            self.serialPort.write(data)
        except Exception as ex:
            print(ex)

    # 读取寄存器 read register
    def readReg(self, regAddr):
        # 从指令中获取起始寄存器 （处理回传数据需要用到） Get start register from instruction
        self.statReg = regAddr
        # 封装读取指令并向串口发送数据 Encapsulate read instructions and send data to the serial port
        self.sendData(self.get_readBytes(regAddr))

    # 写入寄存器 Write Register
    def writeReg(self, regAddr, sValue):
        # 解锁 unlock
        self.unlock()
        # 延迟100ms Delay 100ms
        time.sleep(0.1)
        # 封装写入指令并向串口发送数据
        self.sendData(self.get_writeBytes(regAddr, sValue))
        # 延迟100ms Delay 100ms
        time.sleep(0.1)
        # 保存 save
        self.save()

    # 读取指令封装 Read instruction encapsulation
    def get_readBytes(self, regAddr):
        # 初始化
        tempBytes = [None] * 14
        # 设备modbus地址
        tempBytes[0] = 0x41
        # 读取功能码
        tempBytes[1] = 0x54
        # 寄存器高8位
        tempBytes[2] = self.canid[0]
        # 寄存器低8位
        tempBytes[3] = self.canid[1]
        # 读取寄存器个数高8位
        tempBytes[4] = self.canid[2]
        # 读取寄存器个数低8位
        tempBytes[5] = self.canid[3]
        tempBytes[6] = 5
        tempBytes[7] = 0xFF
        tempBytes[8] = 0xAA
        tempBytes[9] = 0x27
        tempBytes[10] = regAddr & 0xff
        tempBytes[11] = regAddr >> 8
        tempBytes[12] = 0x0D
        tempBytes[13] = 0x0A
        return tempBytes

    # 写入指令封装 Write instruction encapsulation
    def get_writeBytes(self, regAddr, rValue):
        # 初始化
        tempBytes = [None] * 14
        # 设备modbus地址
        tempBytes[0] = 0x41
        # 读取功能码
        tempBytes[1] = 0x54
        # 寄存器高8位
        tempBytes[2] = self.canid[0]
        # 寄存器低8位
        tempBytes[3] = self.canid[1]
        # 读取寄存器个数高8位
        tempBytes[4] = self.canid[2]
        # 读取寄存器个数低8位
        tempBytes[5] = self.canid[3]
        tempBytes[6] = 5
        tempBytes[7] = 0xFF
        tempBytes[8] = 0xAA
        tempBytes[9] = regAddr
        tempBytes[10] = rValue & 0xff
        tempBytes[11] = rValue >> 8
        tempBytes[12] = 0x0D
        tempBytes[13] = 0x0A
        return tempBytes

    # 解锁
    def unlock(self):
        cmd = self.get_writeBytes(0x69, 0xb588)
        self.sendData(cmd)

    # 保存
    def save(self):
        cmd = self.get_writeBytes(0x00, 0x0000)
        self.sendData(cmd)

    # 设置AT模式 Set AT mode
    def setAT(self):
        cmd = "AT+AT\r\n"
        self.sendData(cmd.encode())
        self.isAT = True

    # 设置透传模式 Set Transparent mode
    def setET(self):
        cmd = "AT+ET\r\n"
        self.sendData(cmd.encode())
        self.isAT = False
