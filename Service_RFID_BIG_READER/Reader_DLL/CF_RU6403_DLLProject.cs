﻿using Newtonsoft.Json;
using Service_RFID_BIG_READER.Reader;
using Service_RFID_BIG_READER.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service_RFID_BIG_READER.Reader_DLL

{

    public class CF_RU6403_DLL_DEFINE
    {
        RFIDCallBack elegateRFIDCallBack;
        Int16 port = 0;
        private byte fComAdr = 0xff; //ComAdr hiện đang hoạt động
        private int fOpenComIndex; //mở số chỉ mục cổng nối tiếp
        private byte fBaud;
        private byte[] fPassWord = new byte[4];
        private string isFull = "1";
        private int fCmdRet = 30;// Giá trị trả về của tất cả các lệnh đã thực hiện
        private int fErrorCode;
        private byte maskadr;
        private byte maskLen;
        private byte maskFlag;
        private string fInventoryEPCList; //Lưu trữ danh sách truy vấn (nếu dữ liệu đọc không thay đổi thì sẽ không được làm mới)
        private int frmComportIndex;
        TagInfo tagInfo;

        private int totalTagNum = 0;
        private int cardNum = 0;
        public CF_RU6403_DLL_DEFINE()
        {
            tagInfo = new TagInfo();
            elegateRFIDCallBack = new RFIDCallBack(GetUid);
        }
        public bool Connect(string port_com)
        {
            try
            {
                //Lấy số port
                int FrmPortIndex = 0;
                string temp;
                temp = port_com;
                temp = temp.Trim();
                port = Convert.ToInt16(temp.Substring(3, temp.Length - 3));

                string strException = string.Empty;

                fBaud = Convert.ToByte(ConfigurationManager.AppSettings["baundRate"]);
                if (fBaud > 2)
                    fBaud = Convert.ToByte(fBaud + 2);

                CF_RU6403_DLLProject.CloseComPort();
                Thread.Sleep(50);
                fCmdRet = CF_RU6403_DLLProject.OpenComPort(port, ref fComAdr, fBaud, ref fOpenComIndex);
                if (fCmdRet != 0)
                    return false;

                fOpenComIndex = FrmPortIndex;
                if (FrmPortIndex > 0)
                    CF_RU6403_DLLProject.InitRFIDCallBack(elegateRFIDCallBack, true, FrmPortIndex);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        string epcAndTid = "";
        int lastNum = 0;
        public void GetUid(IntPtr p, Int32 nEvt)
        {

            RFIDTag ce = (RFIDTag)Marshal.PtrToStructure(p, typeof(RFIDTag));

            //mixsing.Checked)

            int gnum = ce.PacketParam;
            if (gnum < 0x80)//EPC
            {
                lastNum = gnum;
                epcAndTid = ce.UID;
            }
            else
            {
                if (((lastNum & 0x3F) == ((gnum & 0x3F) - 1)) || ((lastNum & 0x3F) == 127 && ((gnum & 0x3F) == 0)))//相邻的滚码
                {
                    epcAndTid = epcAndTid + "-" + ce.UID;
                    {
                        int Antnum = ce.ANT;
                        string str_ant = Convert.ToString(Antnum, 2).PadLeft(4, '0');
                        string para = str_ant + "," + epcAndTid + "," + ce.RSSI.ToString();

                    }
                }
                else
                {
                    epcAndTid = "";
                }
            }
            totalTagNum++;
            cardNum++;
        }

        public void MixTag(string param)
        {
            string s_tagInfo = param;
            string sEPC, sData;
            string str_ant = s_tagInfo.Substring(0, 4);
            s_tagInfo = s_tagInfo.Substring(5);

            int index = s_tagInfo.IndexOf(',');
            sEPC = s_tagInfo.Substring(0, index);
            int n = sEPC.IndexOf("-");
            sData = sEPC.Substring(n + 1);
            sEPC = sEPC.Substring(0, n);
            index++;
            string RSSI = s_tagInfo.Substring(index);

           // tagInfo.PublishMessage(TopicPub.APP1_MESSAGE, "30", sEPC, sData, "crc", RSSI, str_ant);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RFIDTag
    {
        public byte PacketParam;
        public byte LEN;
        public string UID;
        public byte RSSI;
        public byte ANT;
        public Int32 Handles;
    }

    public delegate void RFIDCallBack(IntPtr p, Int32 nEvt);

    public static class CF_RU6403_DLLProject

    {
        private const string DLLNAME = @"Reader_DLL\CF_RU6403_DLLProject_x86.dll";

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern void InitRFIDCallBack(RFIDCallBack t, bool uidBack, int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int OpenNetPort(int Port,
                                             string IPaddr,
                                             ref byte ComAddr,
                                             ref int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CloseNetPort(int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int OpenComPort(int Port,
                                                 ref byte ComAddr,
                                                 byte Baud,
                                                 ref int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CloseComPort();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int AutoOpenComPort(ref int Port,
                                                 ref byte ComAddr,
                                                 byte Baud,
                                                 ref int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CloseSpecComPort(int Port);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int OpenUSBPort(ref byte ComAddr,
                                                 ref int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CloseUSBPort(int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetReaderInformation(ref byte ComAdr,              //读写器地址		
                                                      byte[] VersionInfo,           //软件版本
                                                      ref byte ReaderType,              //读写器型号
                                                      ref byte TrType,      //支持的协议
                                                      ref byte dmaxfre,           //当前读写器使用的最高频率
                                                      ref byte dminfre,           //当前读写器使用的最低频率
                                                      ref byte powerdBm,             //读写器的输出功率
                                                      ref byte ScanTime,
                                                      ref byte Ant,
                                                      ref byte BeepEn,
                                                      ref byte OutputRep,
                                                      ref byte CheckAnt,
                                                      int FrmHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetRegion(ref byte ComAdr,
                                           byte dmaxfre,
                                           byte dminfre,
                                           int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetAddress(ref byte ComAdr,
                                             byte ComAdrData,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetInventoryScanTime(ref byte ComAdr,
                                               byte ScanTime,
                                               int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetBaudRate(ref byte ComAdr,
                                           byte baud,
                                           int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetRfPower(ref byte ComAdr,
                                             byte powerDbm,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int BuzzerAndLEDControl(ref byte ComAdr,
                                                     byte AvtiveTime,
                                                     byte SilentTime,
                                                     byte Times,
                                                     int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWorkMode(ref byte ComAdr,
                                             byte Read_mode,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetAntennaMultiplexing(ref byte ComAdr,
                                            byte Ant,
                                            int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetBeepNotification(ref byte ComAdr,
                                         byte BeepEn,
                                         int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetReal_timeClock(ref byte ComAdr,
                                          byte[] paramer,
                                          int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetTime(ref byte ComAdr,
                                          byte[] paramer,
                                          int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetRelay(ref byte ComAdr,
                                          byte RelayTime,
                                          int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetGPIO(ref byte ComAdr,
                                         byte OutputPin,
                                         int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetGPIOStatus(ref byte ComAdr,
                                         ref byte OutputPin,
                                         int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetNotificationPulseOutput(ref byte ComAdr,
                                              byte OutputRep,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetSystemParameter(ref byte ComAdr,
                                                      ref byte Read_mode,
                                                      ref byte Accuracy,
                                                      ref byte RepCondition,
                                                      ref byte RepPauseTime,
                                                      ref byte ReadPauseTim,
                                                      ref byte TagProtocol,
                                                      ref byte MaskMem,
                                                      byte[] MaskAdr,
                                                      ref byte MaskLen,
                                                      byte[] MaskData,
                                                      ref byte TriggerTime,
                                                      ref byte AdrTID,
                                                      ref byte LenTID,
                                                      int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetEASSensitivity(ref byte ComAdr,
                                             byte Accuracy,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetTriggerTime(ref byte ComAdr,
                                             byte TriggerTime,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetTIDParameter(ref byte ComAdr,
                                             byte AdrTID,
                                             byte LenTID,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetMask(ref byte ComAdr,
                                         byte MaskMem,
                                         byte[] MaskAdr,
                                         byte MaskLen,
                                         byte[] MaskData,
                                         int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetResponsePamametersofAuto_runningMode(ref byte ComAdr,
                                                 byte RepCondition,
                                                 byte RepPauseTime,
                                                 int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetInventoryInterval(ref byte ComAdr,
                                                  byte ReadPauseTim,
                                                  int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SelectTagType(ref byte ComAdr,
                                                byte Protocol,
                                                int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetCommType(ref byte ComAdr,
                                                byte CommType,
                                                int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetTagBufferInfo(ref byte ComAdr,
                                                   byte[] Data,
                                                   ref int dataLength,
                                                   int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ClearTagBuffer(ref byte ComAdr,
                                             int frmComPortindex);




        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadActiveModeData(byte[] ScanModeData,
                                                    ref int ValidDatalength,
                                                    int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Inventory_G2(ref byte ComAdr,
                                              byte QValue,
                                              byte Session,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              byte MaskFlag,
                                              byte AdrTID,
                                              byte LenTID,
                                              byte TIDFlag,
                                              byte Target,
                                              byte InAnt,
                                              byte Scantime,
                                              byte FastFlag,
                                              byte[] pEPCList,
                                              ref byte Ant,
                                              ref int Totallen,
                                              ref int cardNum,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int InventoryMix_G2(ref byte ComAdr,
                                              byte QValue,
                                              byte Session,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              byte MaskFlag,
                                              byte ReadMem,
                                              byte[] ReadAdr,
                                              byte ReadLen,
                                              byte[] Psd,
                                              byte Target,
                                              byte InAnt,
                                              byte Scantime,
                                              byte FastFlag,
                                              byte[] pEPCList,
                                              ref byte Ant,
                                              ref int Totallen,
                                              ref int cardNum,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadData_G2(ref byte ComAdr,
                                             byte[] EPC,
                                             byte ENum,
                                             byte Mem,
                                             byte WordPtr,
                                             byte Num,
                                             byte[] Password,
                                             byte MaskMem,
                                             byte[] MaskAdr,
                                             byte MaskLen,
                                             byte[] MaskData,
                                             byte[] Data,
                                             ref int errorcode,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteData_G2(ref byte ComAdr,
                                              byte[] EPC,
                                              byte WNum,
                                              byte ENum,
                                              byte Mem,
                                              byte WordPtr,
                                              byte[] Wdt,
                                              byte[] Password,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              ref int errorcode,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteEPC_G2(ref byte ComAdr,
                                             byte[] Password,
                                             byte[] WriteEPC,
                                             byte ENum,
                                             ref int errorcode,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int KillTag_G2(ref byte ComAdr,
                                                byte[] EPC,
                                                byte ENum,
                                                byte[] Password,
                                                byte MaskMem,
                                                byte[] MaskAdr,
                                                byte MaskLen,
                                                byte[] MaskData,
                                                ref int errorcode,
                                                int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Lock_G2(ref byte ComAdr,
                                                   byte[] EPC,
                                                   byte ENum,
                                                   byte select,
                                                   byte setprotect,
                                                   byte[] Password,
                                                   byte MaskMem,
                                                   byte[] MaskAdr,
                                                   byte MaskLen,
                                                   byte[] MaskData,
                                                   ref int errorcode,
                                                   int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int BlockErase_G2(ref byte ComAdr,
                                              byte[] EPC,
                                              byte ENum,
                                              byte Mem,
                                              byte WordPtr,
                                              byte Num,
                                              byte[] Password,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              ref int errorcode,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetPrivacyWithoutEPC_G2(ref byte ComAdr,
                                                          byte[] Password,
                                                          ref int errorcode,
                                                          int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetPrivacyByEPC_G2(ref byte ComAdr,
                                                  byte[] EPC,
                                                  byte ENum,
                                                  byte[] Password,
                                                  byte MaskMem,
                                                  byte[] MaskAdr,
                                                  byte MaskLen,
                                                  byte[] MaskData,
                                                  ref int errorcode,
                                                  int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ResetPrivacy_G2(ref byte ComAdr,
                                                      byte[] Password,
                                                      ref int errorcode,
                                                      int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CheckPrivacy_G2(ref byte ComAdr,
                                                      ref byte readpro,
                                                      ref int errorcode,
                                                      int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int EASConfigure_G2(ref byte ComAdr,
                                                  byte[] EPC,
                                                  byte ENum,
                                                  byte[] Password,
                                                  byte EAS,
                                                  byte MaskMem,
                                                  byte[] MaskAdr,
                                                  byte MaskLen,
                                                  byte[] MaskData,
                                                  ref int errorcode,
                                                  int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int EASAlarm_G2(ref byte ComAdr,
                                                  ref int errorcode,
                                                  int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int BlockLock_G2(ref byte ComAdr,
                                                  byte[] EPC,
                                                  byte ENum,
                                                  byte[] Password,
                                                  byte WrdPointer,
                                                  byte MaskMem,
                                                  byte[] MaskAdr,
                                                  byte MaskLen,
                                                  byte[] MaskData,
                                                  ref int errorcode,
                                                  int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int BlockWrite_G2(ref byte ComAdr,
                                              byte[] EPC,
                                              byte WNum,
                                              byte ENum,
                                              byte Mem,
                                              byte WordPtr,
                                              byte[] Wdt,
                                              byte[] Password,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              ref int errorcode,
                                              int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ChangeATMode(ref byte ConAddr,
                                               byte ATMode,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int TransparentCMD(ref byte ConAddr,
                                               byte timeout,
                                               byte cmdlen,
                                               byte[] cmddata,
                                               ref byte recvLen,
                                               byte[] recvdata,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetSeriaNo(ref byte ConAddr,
                                               byte[] SeriaNo,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetCheckAnt(ref byte ComAdr,
                                             byte CheckAnt,
                                             int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int InventorySingle_6B(ref byte ConAddr,
                                                  ref byte ant,
                                                  byte[] ID_6B,
                                                  int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int InventoryMultiple_6B(ref byte ConAddr,
                                               byte Condition,
                                               byte StartAddress,
                                               byte mask,
                                               byte[] ConditionContent,
                                               ref byte ant,
                                               byte[] ID_6B,
                                               ref int cardNum,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadData_6B(ref byte ConAddr,
                                               byte[] ID_6B,
                                               byte StartAddress,
                                               byte Num,
                                               byte[] Data,
                                               ref int errorcode,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteData_6B(ref byte ConAddr,
                                               byte[] ID_6B,
                                               byte StartAddress,
                                               byte[] Writedata,
                                               byte Writedatalen,
                                               ref int writtenbyte,
                                               ref int errorcode,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Lock_6B(ref byte ConAddr,
                                               byte[] ID_6B,
                                               byte Address,
                                               ref int errorcode,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CheckLock_6B(ref byte ConAddr,
                                               byte[] ID_6B,
                                               byte Address,
                                               ref byte ReLockState,
                                               ref int errorcode,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetQS(ref byte ConAddr,
                                               byte Qvalue,
                                               byte Session,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetQS(ref byte ConAddr,
                                       ref byte Qvalue,
                                       ref byte Session,
                                       int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetFlashRom(ref byte ConAddr,
                                       int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetModuleVersion(ref byte ConAddr,
                                               byte[] Version,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ExtReadData_G2(ref byte ComAdr,
                                             byte[] EPC,
                                             byte ENum,
                                             byte Mem,
                                             byte[] WordPtr,
                                             byte Num,
                                             byte[] Password,
                                             byte MaskMem,
                                             byte[] MaskAdr,
                                             byte MaskLen,
                                             byte[] MaskData,
                                             byte[] Data,
                                             ref int errorcode,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ExtWriteData_G2(ref byte ComAdr,
                                              byte[] EPC,
                                              byte WNum,
                                              byte ENum,
                                              byte Mem,
                                              byte[] WordPtr,
                                              byte[] Wdt,
                                              byte[] Password,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              ref int errorcode,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int InventoryBuffer_G2(ref byte ComAdr,
                                              byte QValue,
                                              byte Session,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              byte MaskFlag,
                                              byte AdrTID,
                                              byte LenTID,
                                              byte TIDFlag,
                                              byte Target,
                                              byte InAnt,
                                              byte Scantime,
                                              byte FastFlag,
                                              ref int BufferCount,
                                              ref int TagNum,
                                              int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetSaveLen(ref byte ComAdr,
                                              byte SaveLen,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetSaveLen(ref byte ComAdr,
                                            ref byte SaveLen,
                                            int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadBuffer_G2(ref byte ComAdr,
                                              ref int Totallen,
                                              ref int cardNum,
                                              byte[] pEPCList,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ClearBuffer_G2(ref byte ComAdr,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetBufferCnt_G2(ref byte ComAdr,
                                               ref int Count,
                                              int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetReadMode(ref byte ComAdr,
                                             byte ReadMode,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetReadParameter(ref byte ComAdr,
                                              byte[] Parameter,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              byte MaskFlag,
                                              byte AdrTID,
                                              byte LenTID,
                                              byte TIDFlag,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetReadParameter(ref byte ComAdr,
                                             byte[] Parameter,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteRfPower(ref byte ComAdr,
                                             byte powerDbm,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadRfPower(ref byte ComAdr,
                                             ref byte powerDbm,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int RetryTimes(ref byte ComAdr,
                                             ref byte Times,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetDRM(ref byte ComAdr,
                                             byte DRM,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetDRM(ref byte ComAdr,
                                             ref byte DRM,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetReaderTemperature(ref byte ComAdr,
                                             ref byte PlusMinus,
                                             ref byte Temperature,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MeasureReturnLoss(ref byte ComAdr,
                                             byte[] TestFreq,
                                             byte Ant,
                                             ref byte ReturnLoss,
                                             int frmComPortindex);

    }
}
