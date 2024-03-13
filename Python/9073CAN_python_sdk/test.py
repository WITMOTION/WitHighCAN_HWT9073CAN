import time

import device_model

"""
    高精度CAN传感器示例 Example
"""


def updateData(device):
    print(
        "ID:{}  {}{}  AccX:{}  AccY:{}  AccZ:{}  AsX:{}  AsY:{}  AsZ:{}  AngX:{}  AngY:{}  AngZ:{}  Hx:{}  Hy:{}  Hy:{}"
        .format(device.get("CanID"), device.get("canmode_2"), device.get("canmode_1"), device.get("AccX"),
                device.get("AccY"),
                device.get("AccZ"), device.get("AsX"), device.get("AsY"), device.get("AsZ"), device.get("AngX"),
                device.get("AngY"), device.get("AngZ"), device.get("HX"), device.get("HY"), device.get("HZ")))


if __name__ == "__main__":
    # 拿到设备模型 Get the device model
    # DeviceModel("设备名称可以自定义", "COM口号", "串口波特率", "CAN波特率(单位K)")
    device = device_model.DeviceModel("测试设备", "COM5", 115200, 250, updateData)
    # 开启设备 Turn on the device
    device.openDevice()

    # 读取回传速率
    # device.readReg(0x03)
    # time.sleep(1)

    # 设置20hz回传速率
    # device.writeReg(0x03, 7)

    # 设置透传模式
    # device.setET()
