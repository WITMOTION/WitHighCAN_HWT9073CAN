
#include "string.h"
#include "Math.h"
#include "windows.h"
#include "Com.h"

#include<stdio.h>
#include<conio.h>
#include "wit_c_sdk.h"

static char s_cDataUpdate = 0;
int iComPort = 4;
int iBaud = 2000000;
int iAddress = 0x50;
void ComRxCallBack(char *p_data, UINT32 uiSize)
{
	for(UINT32 i = 0; i < uiSize; i++)
	{
		WitSerialDataIn(p_data[i]);
	}
	//printf("Serial recv %d\r\n", uiSize);
}
static void AutoScanSensor(void);
static void SensorUartSend(uint8_t *p_data, uint32_t uiSize);
static void CopeSensorData(uint32_t uiReg, uint32_t uiRegNum);
static void DelayMs(uint16_t ms);
void main(void)
{	
	float a[3], w[3], Angle[3], h[3], Temp;
	int iBuff;
	printf("Please enter the serial number:");
	scanf_s("%d",&iComPort);
	
	OpenCOMDevice(iComPort,iBaud);
	WitInit(WIT_PROTOCOL_905x_CAN, 0x50);
	//WitSerialWriteRegister(SensorUartSend);
	WitRegisterCallBack(CopeSensorData);
	WitDelayMsRegister(DelayMs);
	AutoScanSensor();
	while (1)
    {
		//WitReadReg(AX, 16);
        Sleep(500);
		for (int i = 0;i<3;i++)
		{
			a[i] = (float)sReg[AX+i]/32768.0f*16.0f;
			w[i] = (float)sReg[GX+i]/32768.0f*2000.0f;
			iBuff = (((uint32_t)sReg[HRoll + 2 * i]) << 16) | ((uint16_t)sReg[LRoll + 2 * i]);
			Angle[i] = (float)iBuff / 1000.0f;
			h[i] = (float)sReg[HX+i];
		}
		Temp = (float)sReg[TEMP905x] / 100.0f;

		printf("a:%.2f %.2f %.2f\r\n",a[0],a[1],a[2]);
		printf("w:%.2f %.2f %.2f\r\n",w[0],w[1],w[2]);
		printf("Angle:%.1f %.1f %.1f\r\n",Angle[0],Angle[1],Angle[2]);
		printf("h:%.0f %.0f %.0f\r\n",h[0],h[1],h[2]);
		//printf("Temp:%.1f\r\n\r\n", Temp);
	}
}
static void DelayMs(uint16_t ms)
{
	Sleep(ms);
}

static void SensorUartSend(uint8_t *p_data, uint32_t uiSize)
{
	SendUARTMessageLength((const char*)p_data, uiSize);
}
static void CopeSensorData(uint32_t uiReg, uint32_t uiRegNum)
{
	s_cDataUpdate = 1;
}

static void AutoScanSensor(void)
{
	const uint32_t c_uiBaud[7] = {2000000, 9600, 19200, 38400, 57600, 115200, 230400};
	int i, iRetry;
	
	for(i = 0; i < 7; i++)
	{
		SetBaundrate(c_uiBaud[i]);
		iRetry = 2;
		do
		{
			s_cDataUpdate = 0;
			//WitReadReg(AX, 3);
			Sleep(100);
			if(s_cDataUpdate != 0)
			{
				printf("%d baud find sensor\r\n\r\n", c_uiBaud[i]);
				return ;
			}
			iRetry--;
		}while(iRetry);		
	}
	printf("can not find sensor\r\n");
	printf("please check your connection\r\n");
}

