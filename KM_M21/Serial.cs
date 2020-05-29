using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace KM_M21
{
    #region------------enumerators----------------------------

    public enum HardwareEnum
    {
        // 硬件
        Win32_Processor, // CPU 处理器
        Win32_PhysicalMemory, // 物理内存条
        Win32_Keyboard, // 键盘
        Win32_PointingDevice, // 点输入设备，包括鼠标。
        Win32_FloppyDrive, // 软盘驱动器
        Win32_DiskDrive, // 硬盘驱动器
        Win32_CDROMDrive, // 光盘驱动器
        Win32_BaseBoard, // 主板
        Win32_BIOS, // BIOS 芯片
        Win32_ParallelPort, // 并口
        Win32_SerialPort, // 串口
        Win32_SerialPortConfiguration, // 串口配置
        Win32_SoundDevice, // 多媒体设置，一般指声卡。
        Win32_SystemSlot, // 主板插槽 (ISA & PCI & AGP)
        Win32_USBController, // USB 控制器
        Win32_NetworkAdapter, // 网络适配器
        Win32_NetworkAdapterConfiguration, // 网络适配器设置
        Win32_Printer, // 打印机
        Win32_PrinterConfiguration, // 打印机设置
        Win32_PrintJob, // 打印机任务
        Win32_TCPIPPrinterPort, // 打印机端口
        Win32_POTSModem, // MODEM
        Win32_POTSModemToSerialPort, // MODEM 端口
        Win32_DesktopMonitor, // 显示器
        Win32_DisplayConfiguration, // 显卡
        Win32_DisplayControllerConfiguration, // 显卡设置
        Win32_VideoController, // 显卡细节。
        Win32_VideoSettings, // 显卡支持的显示模式。

        // 操作系统
        Win32_TimeZone, // 时区
        Win32_SystemDriver, // 驱动程序
        Win32_DiskPartition, // 磁盘分区
        Win32_LogicalDisk, // 逻辑磁盘
        Win32_LogicalDiskToPartition, // 逻辑磁盘所在分区及始末位置。
        Win32_LogicalMemoryConfiguration, // 逻辑内存配置
        Win32_PageFile, // 系统页文件信息
        Win32_PageFileSetting, // 页文件设置
        Win32_BootConfiguration, // 系统启动配置
        Win32_ComputerSystem, // 计算机信息简要
        Win32_OperatingSystem, // 操作系统信息
        Win32_StartupCommand, // 系统自动启动程序
        Win32_Service, // 系统安装的服务
        Win32_Group, // 系统管理组
        Win32_GroupUser, // 系统组帐号
        Win32_UserAccount, // 用户帐号
        Win32_Process, // 系统进程
        Win32_Thread, // 系统线程
        Win32_Share, // 共享
        Win32_NetworkClient, // 已安装的网络客户端
        Win32_NetworkProtocol, // 已安装的网络协议
        Win32_PnPEntity,//all device
    }

    public enum ResultType : ushort
    {
        //ushort 0 到 65,535 
        //int表示范围:-2147483648 到2147483648
        //uint表示范围是0到4294967295(2^32-1)
        RET_DO_SUCCESS = 0x00e0,
        RET_ERR_CMD_FAIL = 0x00e1,
        RET_ERR_SEND_FAIL = 0x00e2,
        RET_ERR_TIME_OUT,
        RET_ERR_NOT_OPEN,
        RET_ERR_INVALID_PARAMETER,
        RET_ERR_BUZY_MSR,
        RET_ERR_BUZY_PINPAD,
        RET_ERR_PROTOCOL_FAIL,
        RET_ERR_OTHER = 0x00e9,
    }

    public enum COMM_BAUD : int
    {
        COMM_RS_9600 = 9600,
        COMM_RS_19200 = 19200,
        COMM_RS_38400 = 38400,
        COMM_RS_57600 = 57600,
        COMM_RS_115200 = 115200,
        COMM_RS_230400 = 230400,
        COMM_RS_460800 = 460800,
        COMM_RS_921600 = 921600,
    }

    #endregion
    class Serial
    {
        #region------------Constants----------------------------
        private const int BUFFERSIZE = 2560;
        private const int WaitTimeInMs = /*600*/100;
        #endregion------------Constants----------------------------

        #region------------Variables----------------------------

        private class SERIAL_INFO_st
        {
            public int iComPortNum;
            public int iBaudRate;
            public int iCmdSleepinMs;
            public int iCmdTimeoutDurationinMs;
        }

        private static int s_spbuffersize = 512;
        private static string s_strTMP = null, s_str2TMP = null, s_strNum = null;
        private static byte[] s_byTMP = new byte[1];
        private static byte[] s_byData = new byte[1];
        private static byte[] s_byReadFile = new byte[s_spbuffersize];
        private static byte[] s_byReceived = new byte[BUFFERSIZE];
        private static byte[] s_cmd = new byte[1];
        private static byte[] s_dataIO = new byte[1];

        private static bool s_1CmdIdle = true;
        private static bool s_statusPort = false;
        private static bool s_getCharLength1 = false;
        private static bool s_getCharLength2 = false;
        private static bool s_getCharLength3 = false;
        private static bool s_getCharLength4 = false;

        private DateTime dt;
        private static SERIAL_INFO_st st_Serial = new SERIAL_INFO_st();
        private static ResultType res = ResultType.RET_ERR_CMD_FAIL;
        private static AutoResetEvent s_Event = new AutoResetEvent(false);
        private SerialPort sp = new SerialPort();

        #endregion------------Variables----------------------------

        #region------------SerialWriteRead----------------------------

        private bool IsOpened()
        {
            return s_statusPort;
        }

        private bool OpenSerialPort(int _ComPortNum, int _baudrate)
        {
            //String portName, Int32 baudRate, Parity parity, Int32 dataBits, StopBits stopBits, Int32 readTimeout, Int32 writeTimeout,
            //Handshake handshake, Boolean dtrEnable, Boolean rtsEnable, Boolean discardNull, Byte parityReplace
            sp.PortName = @"COM" + _ComPortNum.ToString();
            sp.BaudRate = _baudrate;
            /*MediaTek USB VCOM (Android) (COM13)
            if (_port != null)
            {
                int idx = _port.IndexOf("(COM");
                sp.PortName = _port.Substring(idx + 1, _port.Length - idx - 2);
            }
            */

            //---N,1,N,8---
            sp.Parity = Parity.None;
            sp.StopBits = StopBits.One;
            sp.Handshake = Handshake.None;
            sp.DataBits = 8;

            sp.ReadTimeout = WaitTimeInMs * 3;
            sp.WriteTimeout = WaitTimeInMs / 3;
            s_spbuffersize = ((_baudrate / 75 + 64) < 320) ? 320 : (_baudrate / 75 + 64);
            sp.ReadBufferSize = s_spbuffersize;
            sp.WriteBufferSize = s_spbuffersize;

            try { sp.Open(); }
            catch (Exception ex)
            {
                MessageBox.Show(" * * * [sp.Open] * * * " + ex.Message);
                goto _FAIL;
            }
            if (!sp.IsOpen) goto _FAIL;

            this.sp.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.Serial_ThreadResponse);
            //s_ijustWaitonce = WaitTimeInMs / 3;
            s_statusPort = true;
            return s_statusPort;

        _FAIL:
            s_statusPort = false;
            return s_statusPort;
        }

        private bool comOpen()
        {
            return OpenSerialPort(st_Serial.iComPortNum, st_Serial.iBaudRate);
        }

        private void comClose()
        {
            try
            {
                this.sp.DataReceived -= new System.IO.Ports.SerialDataReceivedEventHandler(this.Serial_ThreadResponse);
                if (sp.IsOpen) sp.Close();
            }
            catch (Exception ex) { MessageBox.Show(" * * * [comClose] * * * " + ex.Message); }
            finally { s_statusPort = false; GC.Collect(); }
        }

        private bool justOpendevice()
        {
            try
            {
                if (!comOpen()) { comClose(); return false; }
                return true;
            }
            catch (Exception ex) { MessageBox.Show(" * * * [justOpendevice] * * * " + ex.Message); return false; }
        }

        private void Serial_ThreadResponse(object sender, SerialDataReceivedEventArgs e)
        {
            int iRead, iReceived = 0;
            s_byReceived = new byte[BUFFERSIZE];

            if (!s_1CmdIdle)
            {
                //this should be to retrieve the cmd response
                dt = System.DateTime.Now;
                while (true)
                {
                    try
                    {
                        Thread.Sleep(WaitTimeInMs / 10);
                        if (!IsOpened()) break;
                        if (!sp.IsOpen) break;
                        if (sp.BytesToRead < 1)
                        {
                            if ((System.DateTime.Now - dt).TotalMilliseconds > st_Serial.iCmdSleepinMs) break;
                            else continue;
                        }
                        s_byReadFile = new byte[s_spbuffersize];
                        if ((iRead = sp.Read(s_byReadFile, 0, s_spbuffersize)) > 0)
                        {
                            Array.Copy(s_byReadFile, 0, s_byReceived, iReceived, iRead);
                            iReceived += iRead;
                        }
                    }
                    catch { break; }
                }
                if (iReceived > 0)
                {
                    s_dataIO = new byte[iReceived];
                    Array.Copy(s_byReceived, s_dataIO, iReceived);
                    s_byReceived = new byte[BUFFERSIZE];
                    s_Event.Set();
                }
                return;
            }

            //devices data out w/o cmd response
            while (true)
            {
                try
                {
                    Thread.Sleep(WaitTimeInMs / 10);
                    if (!IsOpened()) return;
                    if (!sp.IsOpen) return;
                    if (sp.BytesToRead < 1) return;

                    s_byReadFile = new byte[s_spbuffersize];
                    if ((iRead = sp.Read(s_byReadFile, 0, s_spbuffersize)) > 0)
                    {
                        Array.Copy(s_byReadFile, 0, s_byReceived, iReceived, iRead);
                        iReceived += iRead;
                    }
                }
                catch { return; }
            }
        }

        private void SerialWrite(byte[] aucmpData)
        {
            if (!IsOpened()) return;
            /*
            在控制（或采样）程序中，经常需要通过串口发送或者接收数据，而且一般都会设置采样周期，
            那么在一个周期中可以通过串口传送多少字节的数据呢？下面我们举例说明：
            串口参数：    波特率：9600bps， 8个数据位， 1个停止位， 无奇偶校验
            采样周期：    T=100ms
            则在每个控制内能传送的字节数为：N=Baudrate*T/(DataBit+StopBit) = 9600*0.1/(8+1)=106.7
            再考虑到程序本身数据处理及其它语句需要的时间，每个控制约可传输 90个字节。
            接收数据也可依此估计。注意：程序中有物理存盘（或读盘）操作需要较长时间。
            */
            int auiDataLen = aucmpData.Length, lujj = 0;
            int luiCommandCount = (auiDataLen / s_spbuffersize) + ((auiDataLen % s_spbuffersize != 0) ? 1 : 0);
            while (luiCommandCount > 0)
            {
                if (luiCommandCount > 1)
                {
                    auiDataLen -= s_spbuffersize;
                    sp.Write(aucmpData, lujj * s_spbuffersize, s_spbuffersize);
                }
                else sp.Write(aucmpData, lujj * s_spbuffersize, auiDataLen);
                luiCommandCount--;
                lujj++;
            }

            //---set the flag---
            s_1CmdIdle = false;
            return;
        }

        #endregion------------SerialWriteRead----------------------------

        #region------------ResultCodeScript----------------------------

        private string ResultCodeScript(ResultType nCode)
        {
            switch (nCode)
            {
                case ResultType.RET_DO_SUCCESS: return "RET_DO_SUCCESS";
                case ResultType.RET_ERR_CMD_FAIL: return "RET_ERR_CMD_FAIL";
                case ResultType.RET_ERR_SEND_FAIL: return "RET_ERR_SEND_FAIL";
                case ResultType.RET_ERR_TIME_OUT: return "RET_ERR_TIME_OUT";
                case ResultType.RET_ERR_NOT_OPEN: return "RET_ERR_NOT_OPEN";
                case ResultType.RET_ERR_INVALID_PARAMETER: return "RET_ERR_INVALID_PARAMETER";
                case ResultType.RET_ERR_BUZY_MSR: return "RET_ERR_BUZY_MSR";
                case ResultType.RET_ERR_BUZY_PINPAD: return "RET_ERR_BUZY_PINPAD";
                case ResultType.RET_ERR_PROTOCOL_FAIL: return "RET_ERR_PROTOCOL_FAIL";
                case ResultType.RET_ERR_OTHER: return "RET_ERR_OTHER";
                default: return comErrorCodeScript((ushort)nCode);
            }
        }

        private string comErrorCodeScript(ushort nCode)
        {
            switch (nCode)
            {
                //---MIR function Error Code---
                case 0x2900: return "Unknown ID warning";
                case 0x2A00: return "latch failed to close";    //Command received correctly, but could not be completed
                case 0x2A01: return "configuration update failed";
                case 0x2A02: return "configuration update failed";
                case 0x2A03: return "unable to write byte to EEPROM configuration";
                case 0x2A04: return "configuration update failed";
                case 0x2A05: return "configuration update failed";
                case 0x2A06: return "no ICC voltage not defined on connector";
                case 0x2A07: return "length != 1 or 2 on SAM options setting";
                case 0x2A08: return "latch failed to open";
                case 0x2A09: return "command aborted by the ICC";
                case 0x2A0A: return "sle4406 parameters error";
                case 0x2A0B: return "sle4406 parameters error";
                case 0x2B00: return "sle4404 invalid address must be even 16 bit boundary";
                case 0x2C02: return "no microprocessor ICC seated";
                case 0x2C03: return "no memory card seated";
                case 0x2C04: return "no T=1 raw card seated";
                case 0x2C06: return "no card seated to request ATR";
                case 0x2C07: return "no card seated to latch";
                case 0x2C08: return "T=1 card unseated";
                case 0x2C09: return "T=0 card unseated";
                case 0x2D00: return "memory card type not supported";
                case 0x2D01: return "Card Not Supported,";
                case 0x2D03: return "Card Not Supported, wants CRC";
                case 0x2F01: return "Fault Alarm, ICC powered off";
                case 0x6686: return "4404 no more counter to decrease";
                //case 0x6686: return "gpm271 no more counter to decrease";
                //case 0x6686: return "gpm276 no more counter to decrease";
                //case 0x6686: return "not enough tokens";
                case 0x6687: return "sle4428 report no retries remain";
                //case 0x6687: return "sle4442 report no retries remain";
                case 0x6688: return "4406 invalid secret key presented";
                //case 0x6688: return "4428 invalid PSC presented";
                //case 0x6688: return "sle4442 invalid PSC presented";
                case 0x6701: return "failed to properly reset configuration";
                case 0x6704: return "gpm271 read value different from expected";
                case 0x6705: return "gpm276 byte(s) not successfully erased";
                case 0x6706: return "4406 invalid length";
                case 0x6707: return "sle4404 value written doesn't match";
                case 0x6708: return "sle4404 value written to fuse doesn't match expected";
                case 0x6709: return "sle4406 value written doesn't match";
                //case 0x6900: return "latch function subtype must be 0 or 1";
                case 0x6901: return "reader is not configured with latch hardware";
                case 0x6902: return "reader is not configured with a latch";
                case 0x6903: return "reader configured without ICC support";
                case 0x6904: return "reader configured without ICC support";
                case 0x6905: return "reader not configured with SAM support or SAM conn > 5";
                case 0x6906: return "reader configured without ICC support";
                case 0x6907: return "reader configured without ICC support";
                case 0x6908: return "'R' command subtype invalid";
                case 0x6909: return "invalid baud rate value";
                case 0x690A: return "Set C4 command legal values only 0-1";
                case 0x690B: return "Set C8 command legal values only 0-1";
                case 0x690C: return "set command subtype invalid";
                case 0x690D: return "command not supported on reader without ICC support";
                case 0x690E: return "invalid command response";
                case 0x690F: return "invalid baud rate value";
                case 0x6910: return "invalid serial number length";
                case 0x6911: return "'Q' command length must be 1";
                case 0x6912: return "'P' command length must be 1";
                case 0x6913: return "2nd byte of LED command must be 30-37";
                case 0x6914: return "invalid value of the memory card type";
                case 0x6915: return "invalid erasing string";
                case 0x6916: return "'P' command must be 0x30 or 0x32";
                case 0x6918: return "gpm276 cannot erase 0 bytes";
                case 0x6919: return "gpm271 count of bytes to erase is zero";
                case 0x691B: return "sle4404 cannot erase 0 bytes or odd # of bytes";
                case 0x691C: return "sle4404 password must be even # of bytes > 0";
                case 0x691D: return "download code missing or corrupted";
                case 0x691E: return "MSR read method must be 1, 2, 3 or 4";
                case 0x691F: return "host LED control not enabled";
                case 0x6920: return "Reader not configured for buffered mode";
                case 0x6921: return "reader not configured for buffered mode";
                case 0x6922: return "Reader not configured for magstripe read";
                case 0x6923: return "reader not armed for buff mode";
                case 0x6925: return "illegal value for track selection";
                case 0x692B: return "already in OPOS/JPOS mode";
                case 0x692D: return "invalid session ID length";
                case 0x692E: return "invalid SFR value";
                case 0x692F: return "invalid SFR selection";
                case 0x6930: return "len must be 1 or securityLevel<3";
                case 0x6931: return "invalid DUKPT activation challenge";
                case 0x6932: return "authentication failure";
                case 0x6933: return "load device key failure";
                case 0x6934: return "invalid deactivation command";
                case 0x6935: return "deactivation authorization failed";
                case 0x6936: return "invalid challenge command";
                case 0x6937: return "challenge command failure";
                case 0x6938: return "inform of failure to execute cmd";
                case 0x6939: return "warn: bad command ignored";
                case 0x693A: return "invalid configure string";
                case 0x693B: return "authentication failure";
                case 0x693C: return "load device key failure";
                case 0x693D: return "deactivation cmd disallowed";
                case 0x693E: return "invalid deactivation cmd len";
                case 0x6941: return "reset command not supported";
                case 0x6D01: return "INS not supported on i2c cards";
                case 0x6D02: return "INS not supported on sle4428 cards";
                case 0x6D03: return "INS not supported on sle4442 cards";
                case 0x6D04: return "INS not supported on gpm271 cards";
                case 0x6D05: return "INS not supported on gpm276 cards";
                case 0x6D06: return "INS not supported on sle4404 cards";
                case 0x6D07: return "INS not supported on sle4406 cards";
                case 0x6E00: return "memory card class must be 'DA'";
                //case 0x8001: return "sle4428 count bytes to verify must be 2";
                //case 0x8002: return "i2c no Data or write count != byte received";
                case 0x8003: return "sle4428 count bytes to read required";
                case 0x8004: return "sle4428 no Data or write count != bytes received";
                case 0x8005: return "i2c count bytes to read required";
                case 0x8006: return "sle4442 count bytes to verify must be 3";
                case 0x8007: return "sle4428 no Data or write count != bytes received";
                case 0x8008: return "sle4442 count bytes to read required";
                case 0x8009: return "sle4442 count of protection bytes to read must be 4";
                case 0x800A: return "sle4442 no Data or write count != byte received";
                case 0x800B: return "sle4442 no Data or write count != byte received";
                case 0x800D: return "gpm271 count bytes to read required";
                case 0x800E: return "sle4428 count bytes to read required";
                case 0x800F: return "gpm276 count bytes to read required";
                case 0x8013: return "sle4406 count bytes to read required";
                case 0x8014: return "sle4404 count bytes to read required";
                case 0x8015: return "sle4404 no Data or write count != byte received";
                case 0x8016: return "gpm276 no Data or write count != byte received";
                case 0x8017: return "gpm271 no Data or write count != byte received";
                case 0x8018: return "sle4406 no Data or write count != byte received";
                case 0x8019: return "setting mag prefix D2 command invalid length";
                case 0x801A: return "setting mag postfix D3 command invalid length";
                case 0x801B: return "setting baud rate invalid length";
                case 0x801C: return "Case 2 incorrect Data length or should be write command";
                case 0x801D: return "Case 4 incorrect Data length";
                //case 0x8100: return "ICC err time out on power-up req. by Sp2 Driver";
                //case 0x8100: return "ICC error timeout on power-up";
                //case 0x8100: return "timeout receiving proc byte from T=0 card";
                case 0x8101: return "ICC timeout error";
                //case 0x8200: return "invalid TS character received";
                //case 0x8300: return "T=0 parity on header transmission";
                case 0x8301: return "T=0 parity on transmission";
                //case 0x8400: return "parity error receiving proc byte from T=0 card";
                case 0x8500: return "PPS confirmation error";
                case 0x8501: return "PPS confirmation error unexpected input";
                case 0x8600: return "Unsupported F, D, or combination of F and D";
                case 0x8700: return "protocol not supported EMV TD1 out of range";
                case 0x8800: return "power not at proper level";
                case 0x8801: return "unable to power memory card";
                case 0x8802: return "T=1 power not at proper level";
                case 0x8803: return "error on power up";
                //case 0x8803: return "power not at proper level";
                //case 0x8803: return "power not at proper level";
                case 0x8804: return "power not at proper level";
                //case 0x8804: return "power not at proper level";
                case 0x8805: return "power not at proper level";
                //case 0x8805: return "power not at proper level";
                case 0x8900: return "ATR length too long";
                case 0x8B01: return "EMV invalid TA1 byte value";
                case 0x8B02: return "if EMV TB1 required";
                case 0x8B03: return "EMV Unsupported TB1 only 00 allowed";
                case 0x8B04: return "EMV Card Error, invalid BWI or CWI";
                case 0x8B06: return "EMV TB2 not allowed in ATR";
                case 0x8B07: return "EMV TC2 out of range";
                case 0x8B08: return "EMV TC2 out of range";
                case 0x8B09: return "per EMV96 TA3 must be > 0xF";
                case 0x8B10: return "ICC error on power-up";
                case 0x8B11: return "if EMV96 T=1 then TB3 required";
                case 0x8B12: return "Card Error, invalid BWI or CWI";
                case 0x8B13: return "Card Error, invalid BWI or CWI";
                case 0x8B17: return "EMV TC1/TB3 conflict";
                case 0x8B20: return "EMV TD2 out of range must be T=1";
                case 0x8C00: return "TCK error";
                //case 0x9000: return "assume ISO status is OK";
                //case 0x9000: return "initialize response status";
                //case 0x9000: return "Success";
                case 0xA301: return "ICC error on power-up current overflow";
                case 0xA304: return "connector has no voltage setting";
                case 0xA305: return "ICC error on power-up invalid (sblk(IFSD) xchg";
                case 0xA306: return "ICC error on request ATR from CPU ICC";
                case 0xA307: return "ICC error on power-up at current limit";
                //case 0xB0xx: return "card status reporting";
                case 0xC000: return "no magstripe data available";
                //case 0xE300: return "i2c card ack failure";
                //case 0xE300: return "io line low--card error after session start";
                case 0xE301: return "ICC error after session start";
                case 0xE302: return "io line low--card error after session start";
                //case 0xE302: return "T=1 ICC error (data too long)";
                case 0xE303: return "i2c card ack failure";
                case 0xE304: return "T=1 too many retries to get response";
                case 0xE305: return "3 byte i2c command & MS address ack failure";
                case 0xE306: return "3 byte i2c LS address ack failure";
                case 0xE307: return "4 byte i2c command ack failure";
                case 0xE308: return "4 byte i2c MS address ack failure";
                case 0xE309: return "4 byte i2c LS address ack failure";
                case 0xE30A: return "i2c failure to reset card";
                case 0xE310: return "report timeout error";
                case 0xE311: return "ICC error unexpected input";
                case 0xE401: return "error sending command to T=0 card";
                case 0xE402: return "error receiving proc byte from T=0 card";
                case 0xE403: return "T=0 error getting 2nd status byte";
                case 0xE404: return "T=0 inconsistent proc byte received";
                case 0xE405: return "T=0 error processing block after proc byte";
                case 0xE406: return "problem receiving proper # bytes from ICC";
                case 0xFFFE: return "internal error code";
                case 0xFFFF: return "if we haven't had a bad status";

                case 0x8100: return "Timeout for Get Fun key|Get Encrypted PIN|Get Numeric";
                case 0x8200: return "Wrong operate step";
                //---MIR function Error Code---

                //---ICC EMV Level II Transaction Error Code---
                case 0x0000: return "APPROVED(offline)";
                case 0x0001: return "DECLINED(offline)";
                case 0x0002: return "APPROVED";
                case 0x0003: return "DECLINED";
                case 0x0004: return "GOONLINE";
                case 0x0005: return "CALLYOURBANK";
                case 0x0006: return "NOTACCEPTED";
                case 0x0007: return "USEMAGSTRIPE";
                case 0x0008: return "TIMEOUT";
                case 0x0009: return "ADVICE";
                case 0x0010: return "(starttransactionsuccess)";
                case 0x0203: return "REVERSAL";
                case 0x0501: return "Key is all zero";
                case 0x0502: return "TR-31 format error";
                case 0x0705: return "No Internal MSR PAN (or Internal MSR PAN is erased timeout)";
                case 0x0F00: return "Encryption Or Decryption Failed";

                case 0x1000: return "Battery Low Warning (It is High Priority Response while Battery is Low.)";
                case 0x1001: return "INVALIDARG";
                case 0x1002: return "FILE_OPEN_FAILED";
                case 0x1003: return "FILEOPERATION_FAILED";
                case 0x2001: return "MEMORY_NOT_ENOUGH";
                //case 0x2C02: return "No Microprocessor ICC seated";
                //case 0x2C06: return "No card seated to request ATR";
                case 0x3002: return "SMARTCARD_FAIL";
                case 0x3003: return "SMARTCARD_INIT_FAILED";
                case 0x3004: return "FALLBACK_SITUATION";
                case 0x3005: return "SMARTCARD_ABSENT";
                case 0x3006: return "SMARTCARD_TIMEOUT";
                case 0x3007: return "MSR_FAILED";
                case 0x3008: return "ICC_FAILED_MSR_SUCCESS";
                case 0x3009: return "ICC_FAILED_MSR_FAILED";

                case 0x5001: return "EMV_PARSING_TAGS_FAILED";
                case 0x5002: return "EMV_DUPLICATE_CARD_DATA_ELEMENT";
                case 0x5003: return "EMV_DATA_FORMAT_INCORRECT";
                case 0x5004: return "EMV_NO_TERM_APP";
                case 0x5005: return "EMV_NO_MATCHING_APP";
                case 0x5006: return "EMV_MISSING_MANDATORY_OBJECT";
                case 0x5007: return "EMV_APP_SELECTION_RETRY";
                case 0x5008: return "EMV_GET_AMOUNT_ERROR";
                case 0x5009: return "EMV_CARD_REJECTED";
                case 0x5010: return "EMV_AIP_NOT_RECEIVED";
                case 0x5011: return "EMV_AFL_NOT_RECEIVED";
                case 0x5012: return "EMV_AFL_LEN_OUT_OF_RANGE";
                case 0x5013: return "EMV_SFI_OUT_OF_RANGE";
                case 0x5014: return "EMV_AFL_INCORRECT";
                case 0x5015: return "EMV_EXP_DATE_INCORRECT";
                case 0x5016: return "EMV_EFF_DATE_INCORRECT";
                case 0x5017: return "EMV_ISS_COD_TBL_OUT_OF_RANGE";
                case 0x5018: return "EMV_CRYPTOGRAM_TYPE_INCORRECT";
                case 0x5019: return "EMV_PSE_NOT_SUPPORTED_BY_CARD";
                case 0x5020: return "EMV_USER_SELECTED_LANGUAGE";
                case 0x5021: return "EMV_SERVICE_NOT_ALLOWED";
                case 0x5022: return "EMV_NO_TAG_FOUND";
                case 0x5023: return "EMV_CARD_BLOCKED";
                case 0x5024: return "EMV_LEN_INCORRECT";
                case 0x5025: return "CARD_COM_ERROR";
                case 0x5026: return "EMV_TSC_NOT_INCREASED";
                case 0x5027: return "EMV_HASH_INCORRECT";
                case 0x5028: return "EMV_NO_ARC";
                case 0x5029: return "EMV_INVALID_ARC";

                case 0x5030: return "EMV_NO_ONLINE_COMM/Timeout in Work State1: Device quit Bootloader Status and run in Application Status";
                case 0x5031: return "TRAN_TYPE_INCORRECT/Timeout in Work State2: Device need receive data from Block 0 Data";
                case 0x5032: return "EMV_APP_NO_SUPPORT/Data Error";
                case 0x5033: return "EMV_APP_NOT_SELECT";
                case 0x5034: return "EMV_LANG_NOT_SELECT/Application Version Error";
                case 0x5035: return "EMV_NO_TERM_DATA/Erase flash or write flash Failed";
                case 0x5036: return "Firmware check value Error";
                case 0x5037: return "Device Name Error";
                case 0x5038: return "Encryption Mode Error";
                case 0x5039: return "Firmware Address Error";

                case 0x6001: return "CVM_TYPE_UNKNOWN";
                case 0x6002: return "CVM_AIP_NOT_SUPPORTED";
                case 0x6003: return "CVM_TAG_8E_MISSING";
                case 0x6004: return "CVM_TAG_8E_FORMAT_ERROR";
                case 0x6005: return "CVM_CODE_IS_NOT_SUPPORTED";
                case 0x6006: return "CVM_COND_CODE_IS_NOT_SUPPORTED";
                case 0x6007: return "NO_MORE_CVM";
                case 0x6008: return "PIN_BYPASSED_BEFORE";
                case 0x6A01: return "Unsupported Command – Protocol and task ID are right, but command is invalid – In this State";
                case 0x6C00: return "Unknown parameter in command – Protocol task ID and command are right, but length is out of the requirement";

                case 0x7001: return "PK_BUFFER_SIZE_TOO_BIG";
                case 0x7002: return "PK_FILE_WRITE_ERROR";
                case 0x7003: return "PK_HASH_ERROR";
                case 0x8001: return "NO_CARD_HOLDER_CONFIRMATION";
                case 0x8002: return "GET_ONLINE_PIN";
                //case 0x8200: return "Wrong operate step";
                case 0x8300: return "No Card Data";
                case 0x8400: return "TriMagII no Response";
                //case 0x8B10: return "ICC error on power-up";
                //---ICC EMV Level II Transaction Error Code---

                case 0x0079: return "Driver reported time out error";
                case 0x0080: return "Driver reported user supplied buffer too small";
                case 0x0100: return "Log (Removal / Fix) is full";
                case 0x0300: return "Key Type(TDES) of Session Key is not same as the related Master Key";
                case 0x0400: return "Related key was not loaded";
                case 0x0500: return "Key Same";
                case 0x0700: return "No BDK of Pairing Key";
                case 0x0701: return "There is BDK of Pairing Key, Not Pairing with MSR(No PAN Encryption Key)";
                case 0x0702: return "Plaintext PAN is Error or Pairing succeeds, but the Encrypted PAN is wrong";
                case 0x0703: return "Pairing Failed";
                case 0x0704: return "MSR Pairing Key Other Error";
                case 0x0D00: return "This Key had been loaded";
                case 0x0E00: return "Base Time was loaded";
                case 0x1800: return "Send \"Cancel Command\" after send \"Get Encrypted PIN\" & \"Get Numeric\" & \"Get Amount\"";
                case 0x1900: return "Press \"Cancel\" key after send \"Get Encrypted PIN\" & \"Get Numeric\" & \"Get Amount\"";
                case 0x2C00: return "Card not present";
                case 0x2F00: return "ICC pulled from user slot while powered";
                case 0x3000: return "Only Security Chip is deactivation for No Secure data. (Unit is In Removal Legally State)";
                case 0x3001: return "Only Security Chip is deactivation for ST Chip Firmware Check Error. (Unit is In Removal Legally State)";
                //case 0x3002: return "Only Security Chip is deactivation for Security Chip Firmware Check Error. (Unit is In Removal Legally State)";
                //case 0x3003: return "Only Security Chip is deactivation for Illegally Removal";
                case 0x3101: return "Security Chip is activation. (Unit is In Removal Legally State)";
                case 0x30FF: return "Security Chip is not connect";

                //---Remote Key Injection Error Code Definition---
                case 0x5500: return "No Admin DUKPT Key";
                case 0x5501: return "Admin DUKPT Key STOP";
                case 0x5502: return "Admin DUKPT Key KSN is Error";
                case 0x5503: return "Get Authentication Code1 Failed";
                case 0x5504: return "Validate Authentication Code Error";
                case 0x5505: return "Encrypt or Decrypt data failed";
                case 0x5506: return "Not Support the New Key Type";
                case 0x5507: return "New Key Index is Error";
                case 0x5508: return "Step Error";
                case 0x5509: return "Timed out";
                case 0x550A: return "MAC checking error";
                case 0x550B: return "Key Usage Error";
                case 0x550C: return "Mode of Use Error";
                case 0x550D: return "Algorithm Error";
                case 0x550F: return "Other Error(FM)";
                //---Remote Key Injection Error Code Definition---

                case 0x6000: return "Save or Config Failed / Or Read Config Error";
                case 0x6200: return "No Serial Number";
                case 0x6900: return "Invalid Command - Protocol is right, but task ID is invalid";
                case 0x6A00: return "Unsupported Command - Protocol and task ID are right, but command is invalid";
                case 0x6B00: return "Unknown parameter in command - Protocol task ID and command are right, but parameter is invalid";
                case 0x7200: return "Device is suspend (MKSK suspend or press password suspend)";
                case 0x7300: return "PIN DUKPT is STOP (21 bit 1)";
                case 0x7400: return "Device is Busy";
                //case 0x8100: return "Timeout for Get Fun key|Get Encrypted PIN|Get Numeric";

                /*
                '9000' indicates a successful execution of the command.
                SW1 SW2 = '6A82' (file not found).
                */
                case 0x6281: return "Part of returned data may be corrupted";
                case 0x6283: return "State of non-volatile memory unchanged; selected file invalidated";
                case 0x6300: return "State of non-volatile memory changed; authentication failed";
                case 0x63C0: return "State of non-volatile memory changed; counter provided by 'x'(from 0-15)";
                case 0x6700: return "Length field incorrect";
                case 0x6983: return "Command not allowed; authentication method blocked";
                case 0x6984: return "Command not allowed; referenced data invalidated";
                case 0x6985: return "Command not allowed; conditions of use not satisfied";
                case 0x6A81: return "Wrong parameter(s) P1 P2; function not supported";
                case 0x6A82: return "Wrong parameter(s) P1 P2; file not found";
                case 0x6A83: return "Wrong parameter(s) P1 P2; record not found";
                case 0x6A86: return "P1 or P2 should not be 0x00";
                case 0x6A88: return "Referenced data (data objects) not found";
                case 0x6F00: return "No precise diagnosis";
                //case 0x9000: return "Process completed (any other value for SW2 is RFU)";
                case 0x9000: return "Process completed";

                //General
                case 0x9031: return "Unknown command";
                case 0x9032: return "Wrong parameter(such as the length of the command is incorrect)";
                case 0x9038: return "Wait(the command couldn’t be finished in BWT)";
                case 0x9039: return "Busy(a previously command has not been finished)";
                case 0x903A: return "Number of retries over limit";

                //State error
                case 0x9040: return "Invalid Manufacturing system data";
                case 0x9041: return "Not authenticated";
                case 0x9042: return "Invalid Master DUKPT Key";
                case 0x9043: return "Invalid MAC Key";
                case 0x9044: return "Invalid CHIPCARD DUKPT Key";
                case 0x9045: return "Invalid PIN DUKPT Key";
                case 0x9046: return "Invalid MSR DUKPT Key";
                case 0x9047: return "Invalid PIN Pairing DUKPT Key";
                case 0x9048: return "Invalid MSR Pairing DUKPT Key";
                case 0x9049: return "No nonce generated";
                case 0x904A: return "Not ready";
                case 0x904B: return "Not MAC data";

                //Data error
                case 0x9050: return "Invalid Certificate";
                case 0x9051: return "Duplicate key detected";
                case 0x9052: return "AT checks failed";
                case 0x9053: return "TR34 checks failed";
                case 0x9054: return "TR31 checks failed";
                case 0x9055: return "MAC checks failed";
                case 0x9056: return "Firmware download failed";

                //Resource error
                case 0x9060: return "Log is full";
                case 0x9061: return "Removal sensor unengaged";
                case 0x9062: return "Any hardware problems";

                //Third party error
                case 0x9070: return "ICC communication timeout";
                case 0x9071: return "ICC data error(such check sum error)";
                case 0x9072: return "SmartCard not powered up";
                case 0x90E1: return "The operator ID is the same for command";

                //---SecuRED---
                case 0xE000: return "No Card Account number(Paring key part)";
                case 0xE100: return "Paring key don’t exist. Operate related command before loading Paring key";
                case 0xE200: return "Paring key has existed";
                case 0xE300: return "The parameter doesn’t match.  Parameter of the command doesn’t match requirement";
                case 0xE313: return "IO line low -- Card error after session start";
                case 0xE400: return "Fail to decrypt data";
                case 0xE500: return "SmartCard Error";
                case 0xE600: return "Get MSR Card data is error";
                case 0xE700: return "Command time out";
                /*
                0xE5 (ID code)	Command length is error. ID code is command ID.
                0xE6 (ID code)	Parameter is error. The parameter is out scope.
                0xE7 (ID code)	Command is error. The device don’t support the command.
                */
                case 0xE800: return "Command LRC is error";
                case 0xE900: return "Command time overflow";
                case 0xEA00: return "Operation is error. It is often occured by error operation order";
                case 0xEB00: return "Random data don`t match";
                case 0xEC00: return "MSR key has existed";
                case 0xED00: return "MSR key don`t exist";
                case 0xEE00: return "Secure level don`t match requirement";
                case 0xEF00: return "EEPROM write error";
                //---SecuRED---

                //ICC EMV Lv2 error
                case 0xF002: return "ICC communication timeout";
                case 0xF003: return "ICC communication Error";
                case 0xF00F: return "ICC Card Seated and Highest Priority, disable MSR work request";
                case 0xF200: return "No AID or No Application Data";
                case 0xF201: return "No Terminal Data";
                case 0xF202: return "Wrong TLV format";
                case 0xF203: return "AID list is full, maxim is 16";
                case 0xF204: return "No any CA Key";
                case 0xF205: return "No CA Key RID";
                case 0xF206: return "No CA Key Index";
                case 0xF207: return "CA Key list is full, maxim is 96";
                case 0xF208: return "Wrong CA Key hash";
                case 0xF209: return "Wrong Transaction Command Format";
                case 0xF20A: return "Unexpected Command";
                case 0xF20B: return "No CRL";
                case 0xF20C: return "CRL list is full, maxim is 30";
                case 0xF20D: return "No amount, other amount and transaction type in Transaction Command";
                case 0xF20E: return "Wrong CA Hash and Encryption algorithm";

                //常用APDU指令错误码 http://blog.csdn.net/lonet/article/details/7541265
                //case 0x9000: return "正常 成功执行";
                //case 0x6200: return "警告 信息未提供";
                //case 0x6281: return "警告 回送数据可能出错";
                case 0x6282: return "警告 文件长度小于Le";
                //case 0x6283: return "警告 选中的文件无效";
                case 0x6284: return "警告 FCI格式与P2指定的不符";
                //case 0x6300: return "警告 鉴别失败";
                //case 0x63Cx: return "警告 校验失败（x－允许重试次数）";
                case 0x6400: return "出错 状态标志位没有变";
                case 0x6581: return "出错 内存失败";
                //case 0x6700: return "出错 长度错误";
                case 0x6882: return "出错 不支持安全报文";
                case 0x6981: return "出错 命令与文件结构不相容，当前文件非所需文件";
                case 0x6982: return "出错 操作条件（AC）不满足，没有校验PIN";
                //case 0x6983: return "出错 认证方法锁定，PIN被锁定";
                //case 0x6984: return "出错 随机数无效，引用的数据无效";
                //case 0x6985: return "出错 使用条件不满足";
                case 0x6986: return "出错 不满足命令执行条件（不允许的命令，INS有错）";
                case 0x6987: return "出错 MAC丢失";
                case 0x6988: return "出错 MAC不正确";
                case 0x698D: return "保留";
                case 0x6A80: return "出错 数据域参数不正确";
                //case 0x6A81: return "出错 功能不支持；创建不允许；目录无效；应用锁定";
                //case 0x6A82: return "出错 该文件未找到";
                //case 0x6A83: return "出错 该记录未找到";
                case 0x6A84: return "出错 文件预留空间不足";
                //case 0x6A86: return "出错 P1或P2不正确";
                //case 0x6A88: return "出错 引用数据未找到";
                //case 0x6B00: return "出错 参数错误";
                //case 0x6Cxx: return "出错 Le长度错误，实际长度是xx";
                //case 0x6E00: return "出错 不支持的类：CLA有错";
                //case 0x6F00: return "出错 数据无效";
                case 0x6D00: return "出错 不支持的指令代码";
                case 0x9301: return "出错 资金不足";
                case 0x9302: return "出错 MAC无效";
                case 0x9303: return "出错 应用被永久锁定";
                case 0x9401: return "出错 交易金额不足";
                case 0x9402: return "出错 交易计数器达到最大值";
                case 0x9403: return "出错 密钥索引不支持";
                case 0x9406: return "出错 所需MAC不可用";
                //case 0x6900: return "出错 不能处理";
                //case 0x6901: return "出错 命令不接受（无效状态）";
                //case 0x61xx: return "正常 需发GET";
                case 0x6600: return "出错 接收通讯超时";
                case 0x6601: return "出错 接收字符奇偶错";
                case 0x6602: return "出错 校验和不对";
                case 0x6603: return "警告 当前DF文件无FCI";
                case 0x6604: return "警告 当前DF下无SF或KF";

                default: return "UNKNOWN CODE";
            }
        }

        #endregion------------ResultCodeScript----------------------------

        #region------------Device_SetCMD----------------------------

        private string GetMidValue(string begin, string end, string html)
        {
            //http://blog.csdn.net/xanxus46/article/details/8474081
            //http://www.cnblogs.com/JensonBin/archive/2011/09/13/2174611.html
            //Regex reg = new Regex("(02|60)[.\\s\\S]*(03)", RegexOptions.Multiline | RegexOptions.Singleline);
            Regex reg = new Regex("(?<=(" + begin + "))[.\\s\\S]*?(?=(" + end + "))", RegexOptions.Multiline | RegexOptions.Singleline);
            //return reg.Matches(html).ToString();
            return reg.Match(html).Value;
        }

        private string GetInputChar2bytes(string html)
        {
            //http://www.cnblogs.com/JensonBin/archive/2011/09/13/2174611.html
            //^字符串必须以指定的字符开始
            //$字符串必须以指定的字符结束
            s_byTMP = System.Text.Encoding.Default.GetBytes(GetMidValue("<", ">", html));
            int ilen = s_byTMP.Length;
            if (s_getCharLength1)
                return GetMidValue("^", "<", html) + ilen.ToString("X2")
                    + BitConverter.ToString(s_byTMP).Replace("-", null)
                    + GetMidValue(">", "$", html);
            else if (s_getCharLength2)
                return GetMidValue("^", "<", html) + (ilen + 1).ToString("X2") + ilen.ToString("X2")
                    + BitConverter.ToString(s_byTMP).Replace("-", null)
                    + GetMidValue(">", "$", html);
            else if (s_getCharLength3)
                return GetMidValue("^", "<", html) + (ilen % 256).ToString("X2") + (ilen / 256).ToString("X2")
                    + BitConverter.ToString(s_byTMP).Replace("-", null)
                    + GetMidValue(">", "$", html);
            else if (s_getCharLength4)
                return GetMidValue("^", "<", html) + (ilen / 256).ToString("X2") + (ilen % 256).ToString("X2")
                    + BitConverter.ToString(s_byTMP).Replace("-", null)
                    + GetMidValue(">", "$", html);
            else
                return GetMidValue("^", "<", html)
                    + BitConverter.ToString(s_byTMP).Replace("-", null)
                    + GetMidValue(">", "$", html);
        }

        private string Get2InputChar2bytes(string html)
        {
            int ilen;
            try { ilen = verifyHexString(GetMidValue("{}", "$", html)).Length / 2; }
            catch { ilen = 0; }
            return GetMidValue("^", "{}", html) + (ilen % 256).ToString("X2") + (ilen / 256).ToString("X2")
                + GetMidValue("{}", "$", html);
        }

        private string verifyHexString(string strinput)
        {
            if (strinput == null) return null;
            strinput = strinput.Replace("0X", null).Replace("0x", null);
            if (strinput == null) return null;
            s_str2TMP = null;
            for (int i = 0; i < strinput.Length; i++)
                if (('0' <= strinput[i] && '9' >= strinput[i]) || ('A' <= strinput[i] && 'F' >= strinput[i]) || ('a' <= strinput[i] && 'f' >= strinput[i]))
                    s_str2TMP += strinput[i];
            return s_str2TMP;
        }

        private string GetCmdParsed(string byteStrs)
        {
            if (byteStrs == null) return null;

            s_strTMP = null;
            while (Regex.IsMatch(byteStrs, @"<", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)
                && Regex.IsMatch(byteStrs, @">", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace))
            {
                s_strTMP = GetInputChar2bytes(byteStrs);
                if (s_strTMP != null) byteStrs = s_strTMP;
            }

            if (Regex.IsMatch(byteStrs, @"{}", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace))
            {
                s_strTMP = Get2InputChar2bytes(byteStrs);
                if (s_strTMP != null) byteStrs = s_strTMP;
            }

            return verifyHexString(byteStrs);
        }

        private int convertHexStringToBytes2(string str)
        {
            str = verifyHexString(str);
            if (str == null) return 0;
            int s_nDataLen = str.Length / 2;
            if (s_nDataLen < 1) return 0;

            s_byData = new byte[s_nDataLen];
            for (int i = 0; i < s_nDataLen; i++)
            {
                s_strNum = str.Substring(i * 2, 2);
                s_byData[i] = Convert.ToByte(s_strNum, 16);
            }
            return s_nDataLen;
        }

        private int CustomizedPackage(string strData, byte byLRC)
        {
            int s_nDataLen = convertHexStringToBytes2(strData);
            if (s_nDataLen < 1) return 0;

            s_cmd = new byte[s_nDataLen];
            Array.Copy(s_byData, s_cmd, s_nDataLen);

            s_byData = new byte[1];
            return s_nDataLen;
        }

        private int GetCommunicationProtocolsPackage(string strData)
        {
            return CustomizedPackage(strData, 0x00);
            /*
            case 0: return GetSMagPackage(strData, 0x00);
            case 1: return GetSMagPackage(strData, 0x00);
            case 2: return GetNGAPackage(strData, 0x00);
            case 3: return GetNGAPackage(strData, 0x00);
            case 4: return GetMIRPackage(strData, 0x00);
            case 5: return GetSPIPackage(strData, 0x00);
            default: return CustomizedPackage(strData, 0x00);
            */
        }

        private string GetResponseHexString(string strReplaced)
        {
            int ilen = s_dataIO.Length;
            while (--ilen > 0)
                if (s_dataIO[ilen] != 0x00)
                    break;

            if (s_dataIO[ilen] == 0x90) ++ilen;
            else if (s_dataIO[ilen] == 0x6D) ++ilen;
            ++ilen;

            s_byData = new byte[ilen];
            Array.Copy(s_dataIO, s_byData, ilen);
            s_dataIO = new byte[ilen];
            Array.Copy(s_byData, s_dataIO, ilen);
            return BitConverter.ToString(s_dataIO).Replace("-", strReplaced);
        }

        private string GetHexToASCIIString()
        {
            int ilen = s_dataIO.Length, ilen0 = ilen;
            while (--ilen > 0)
                if (s_dataIO[ilen] != 0x00)
                    break;

            if (s_dataIO[ilen] == 0x90) ++ilen;
            else if (s_dataIO[ilen] == 0x6D) ++ilen;
            ++ilen;

            ilen = (ilen > ilen0) ? ilen0 : ilen;

            s_byData = new byte[ilen];
            Array.Copy(s_dataIO, s_byData, ilen);
            s_dataIO = new byte[ilen];
            Array.Copy(s_byData, s_dataIO, ilen);

            int iLen = s_dataIO.Length;
            s_byTMP = new byte[iLen];
            Array.Copy(s_dataIO, s_byTMP, iLen);
            for (int i = 0; i < iLen; i++)
            {
                if (s_byTMP[i] == 0x0d) continue;
                if (s_byTMP[i] == 0x0a) continue;

                if ((s_byTMP[i] >= 0x7f) || (s_byTMP[i] <= 0x1f)) s_byTMP[i] = 0x2e;
            }
            //~........~~....%.....ERROR:Secure boot enabled..~~..........OK..~
            return System.Text.Encoding.Default.GetString(s_byTMP).Replace("\r\n\r\n", "\r\n");
        }

        /*
        private ResultType Device_SetCMD_Original(ref string byteStrs)
        {
            byteStrs = (GetCmdParsed(byteStrs));
            int respLen = GetCommunicationProtocolsPackage(byteStrs);
            if (respLen < 1) return ResultType.RET_ERR_PROTOCOL_FAIL;
            else byteStrs = BitConverter.ToString(s_cmd).Replace("-", " ");

            res = Serial_General_DoCommand();
            if (ResultType.RET_DO_SUCCESS != res) byteStrs += CR_T + ResultCodeScript(res);
            else byteStrs += CR_T + GetResponseHexString(" ");

            comClose();
            return res;
        }
        */

        private ResultType Serial_General_DoCommand()
        {
            /*
            if (!IsOpened()) return ResultType.RET_ERR_NOT_OPEN;

            try { SerialWrite(s_cmd); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "...port error, restart the computer and try again later...");
                return ResultType.RET_ERR_SEND_FAIL;
            }
            */
            do
            {
                if (!IsOpened()) return ResultType.RET_ERR_NOT_OPEN;
                try { SerialWrite(s_cmd); break; }
                catch { Thread.Sleep(WaitTimeInMs / 10); continue; }
            } while (true);

            s_1CmdIdle = s_Event.WaitOne(st_Serial.iCmdTimeoutDurationinMs, false);

            if (!s_1CmdIdle)
            {
                s_1CmdIdle = true;
                return ResultType.RET_ERR_TIME_OUT;
            }
            return ResultType.RET_DO_SUCCESS;
        }

        private ResultType Device_SetCMD_New(ref string byteStrs)
        {
            byteStrs = (GetCmdParsed(byteStrs));
            int respLen = GetCommunicationProtocolsPackage(byteStrs);
            if (respLen < 1)
            {
                res = ResultType.RET_ERR_PROTOCOL_FAIL;
                byteStrs = ResultCodeScript(res);
                comClose();
                return res;
            }

            res = Serial_General_DoCommand();

            if (ResultType.RET_DO_SUCCESS != res) byteStrs = ResultCodeScript(res);
            else byteStrs = /*GetResponseHexString(" ")*/GetHexToASCIIString();
            comClose();
            return res;
        }

        public void InitializeParameters(int _iComPortNum, COMM_BAUD _iBaudRate/*, int _iCmdSleeps, int _iCmdTimeoutDuration*/)
        {
            st_Serial.iComPortNum = _iComPortNum;
            st_Serial.iBaudRate = (int)_iBaudRate;
            /*
            st_Serial.iCmdSleeps = _iCmdSleeps;
            st_Serial.iCmdTimeoutDuration = _iCmdTimeoutDuration;
            */
        }

        public bool SerialCmdManualSendReceive(ref string _strSendReceive, int _iCmdSleeps, int _iCmdTimeoutDuration)
        {
            /*---绑定和解绑作为一对工具使用_界面上做明显的区分---Lead 10/27/2016 10:31:01 AM---
            波特率9600,8位数据位，1位停止位，无检验位。扫描完后发命令53 01 00 54到夹具
            压合夹具有两个要求：1，现有的工具作为绑定工具，不做变更；2.另外改一个解绑工具，在扫描完上盖，下盖之后，判断压合时间已到，就往串口发送这个指令。
            返回54 01 00 55 
            */
            /*
            this.m_editDirectIO.Update();
            string byteStrs = this.m_editDirectIO.Text;
            if (byteStrs == null) return;
            if (byteStrs.Length < 2) return;
            */
            if (!IsOpened())
                if (!justOpendevice())
                    return false;

            st_Serial.iCmdSleepinMs = _iCmdSleeps > 0 ? _iCmdSleeps : 1;
            st_Serial.iCmdTimeoutDurationinMs = _iCmdTimeoutDuration;
            /*
            21:40:23->53 01 00 54
                    ->53 5E 41 5E 40 54
            */
            return (ResultType.RET_DO_SUCCESS == Device_SetCMD_New(ref _strSendReceive));
        }

        public void RetrieveSerialDataInOut(ref byte[] _DataIn, ref byte[] _DataOut)
        {
            _DataIn = new byte[s_cmd.Length];
            Array.Copy(s_cmd, _DataIn, s_cmd.Length);

            _DataOut = new byte[s_dataIO.Length];
            Array.Copy(s_dataIO, _DataOut, s_dataIO.Length);
        }

        #endregion------------Device_SetCMD----------------------------
    }
}
