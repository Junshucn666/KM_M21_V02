using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Management;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.IO.Ports;  
using System.Data.OleDb;
using Microsoft.Office.Interop.Excel;
using Excel = Microsoft.Office.Interop.Excel;
using System.Security.Cryptography;//引用Md5转换功能
using Common;

namespace KM_M21
{

    public partial class KM_M21 : Form
    {
        public struct TableCell
        {
            public int iX;//主表坐标
            public int iY;//主表坐标
            public int iNum;//表格编号
            public bool bG1;//Gateway1找到的
            public bool bG2;//Gateway2找到的
            public int iTimeMark;//找到的时间戳
            public string sText;//表格内容
            public string sTempter;//表格温度戳
            public string sMacaddr;//表格MAC值
            public bool bMyTableMatch;//A表配对情况
        };
        TableCell[] stuTableCell = new TableCell[200];//这是主表
        TableCell[] stuTACell = new TableCell[200];//这是表A

        public struct MacInfor
        {
            public string sMACaddr;
            public Int32 hashMacValue;
            public string sTempter;
            public int iTimeMark;
            public int iNum;
        }
        //Mac信息
        MacInfor stuMacInfor = new MacInfor();
        List<MacInfor> lsMacInforsG1 = new List<MacInfor>();
        List<MacInfor> lsMacInforsG2 = new List<MacInfor>();
        //不在表内Mac信息
        MacInfor stuMacInforNotInTable= new MacInfor();
        List<MacInfor> lsMacInforNotInTables = new List<MacInfor>();
        List<MacInfor> lsMacInforNotInTablesG2 = new List<MacInfor>();
       

        #region VARIABLES
        bool bCycleScanG1 = false;//循环开关
        bool bCycleScanG2 = false;//循环开关
        public Thread ScanStartG1ThreadProc;
        public Thread ScanStartG2ThreadProc;
        public Thread ScanENDG1ThreadProc;
        public Thread ScanENDG2ThreadProc;
        public Thread SaveDataToSTUG1TestThreadProc;
        public Thread SaveDataToSTUG2TestThreadProc;
        public Thread CompDataTestG1ThreadProc;
        public Thread CompDataTestG2ThreadProc;
        public Thread ScanMacThreadG1;
        public Thread ScanMacThreadG2;
        public Thread CheckTableThreadProc;

        
        public static int ScanNum = 200;
        int aa = -1;
        int bb = 0;
        int iuStatus = 0;
        Byte[] OutputBuf = new Byte[128];//串口设置
        #endregion

        //[StructLayout(LayoutKind.Sequential)]
        private int Data_Count = 0;
        private static byte[] _byTMP = new byte[0];
        private static byte[] _byRead = new byte[128];
        string[] global_data = new string[200];
        bool g_bSignalNoRunG1 = true;
        bool g_bSignalNoRunG2 = true;
        int g_iMacIndexG1=-1;
        int g_iMacIndexG2 = -1;
        int g_iFunctinIndexG1 = -1;//选择
        int g_iFunctinIndexG2 = -1;//选择
        int g_Testtime = 0;
        int g_TesttimeG1 = 0;
        int g_TesttimeG2 = 0;
        int g_NonKMcountG1 = 0;
        int g_NonKMcountG2 = 0;
        int g_iInterTime=0;
        int g_iInterTimeG1=0;
        int g_iInterTimeG2 = 0;
        Int32[] dgv_itemA_hash = new Int32[200];//哈希校验数据
        int[] IndexItemA = new int[200];
        Int32[] dgv_itemB_hash = new Int32[200];
        int[] IndexItemB = new int[200];
        string[][] xxx = new string[400][];
        public KM_View ScanMacForm = new KM_View();
        
        private const int ISLEEPS = 1300, CMDTIMEOUTDURATION = 4700, CMDERRNORESPONSES = 10000;

        public KM_M21()
        {
            InitializeComponent();
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            InitDgvtbc();
            InitDgvtbcA();
            InitDgvtbcB();
            listView1_caption();
            dgv_items.CurrentCell = dgv_items.Rows[0].Cells[1];//
            dgv_items.BeginEdit(false);//
            System.Windows.Forms.TextBox tb = (System.Windows.Forms.TextBox)dgv_items.EditingControl;//
            tb.SelectionStart = 0;//
        }

        private void KM_M21_Load(object sender, EventArgs e)
        {
            dgv_items.AllowUserToAddRows = false;
            dgvA_items.AllowUserToAddRows = false;
            dgvB_items.AllowUserToAddRows = false;
            //设置计时牌颜色
            this.label_TestTime_G1.BackColor = Color.Orange;
            this.label_TestTime_G2.BackColor = Color.Blue;
            //设置按钮
            textBox_G1.Enabled = false;
            START_G1.Enabled = true;
            END_G1.Enabled = false;
            textBox_G2.Enabled = false;
            START_G2.Enabled = true;
            END_G2.Enabled = false;
            checkBox1.Visible = false;
            //增加
            for (int i = 0; i < 100; i++)
            {
                comboBoxG1_Name.Items.Add("COM"+(i + 1));
            }
            for (int i = 0; i < 100; i++)
            {
                comboBoxG2_Name.Items.Add("COM" + (i + 1));
            }
            //设置参数
            string sInifileSrc = System.Windows.Forms.Application.StartupPath;
            sInifileSrc = sInifileSrc + "\\Config.ini";
            IniFileCls _inifile = new IniFileCls(sInifileSrc);
            string sPortNameG1 = _inifile.ReadFromFile("Gateway1", "Name", "0");
            string sBaudRateG1 = _inifile.ReadFromFile("Gateway1", "BaudRate", "0");
            string sFunctionG1 = _inifile.ReadFromFile("Gateway1", "Function", "0");
            string sPortNameG2 = _inifile.ReadFromFile("Gateway2", "Name", "0");
            string sBaudRateG2 = _inifile.ReadFromFile("Gateway2", "BaudRate", "0");
            string sFunctionG2 = _inifile.ReadFromFile("Gateway2", "Function", "0");
            string sInterTime = _inifile.ReadFromFile("IntervalTime", "Time", "180");
            this.comboBoxG1_Name.SelectedIndex = int.Parse(sPortNameG1) - 1;
            this.comboBoxG1_BaudRate.SelectedIndex = int.Parse(sBaudRateG1);
            this.comboBoxG1_Function.SelectedIndex = int.Parse(sFunctionG1);
            this.comboBoxG2_Name.SelectedIndex = int.Parse(sPortNameG2) - 1;
            this.comboBoxG2_BaudRate.SelectedIndex = int.Parse(sBaudRateG2);
            this.comboBoxG2_Function.SelectedIndex = int.Parse(sFunctionG2);
            label_Inter.Text = sInterTime;
            g_iInterTime = int.Parse(sInterTime); 
            g_iInterTimeG1 = int.Parse(sInterTime);
            g_iInterTimeG2 = int.Parse(sInterTime);
        }

        private void KM_M21_FormClosing(object sender, FormClosingEventArgs e)
        {
            bCycleScanG1 = false;//关闭循环
            bCycleScanG2 = false;//关闭循环
            Thread.Sleep(100);
            serialPort1.Dispose();
            serialPort1.Close();
            serialPort2.Dispose();
            serialPort2.Close();
            if (CheckTableThreadProc != null) CheckTableThreadProc.Abort();    
            if (ScanMacThreadG1 != null) ScanMacThreadG1.Abort();
            if (SaveDataToSTUG1TestThreadProc != null) SaveDataToSTUG1TestThreadProc.Abort();
            if (CompDataTestG1ThreadProc != null) CompDataTestG1ThreadProc.Abort();
            if (ScanMacThreadG2 != null) ScanMacThreadG2.Abort();
            if (SaveDataToSTUG2TestThreadProc != null) SaveDataToSTUG2TestThreadProc.Abort();
            if (CompDataTestG2ThreadProc != null) CompDataTestG2ThreadProc.Abort();
            System.Windows.Forms.Application.Exit();
        }

        private void InitDgvtbc()
        {
            dgv_items.AllowUserToAddRows = true;
            dgv_items.AllowUserToOrderColumns = false;
            dgv_items.AllowUserToResizeRows = true;
            //add Column
            for (int i = 0; i < 20; i++)
            {
                DataGridViewAddColumn("Row" + (i + 1));
            }
            dgv_items.Columns[0].ReadOnly = true;
            //string[] value = { "No1", "No2", "No3", "No4", "No5" };
            add_Row(20);
        }

        private void InitDgvtbcA()
        {
            dgvA_items.AllowUserToAddRows = true;
            dgvA_items.AllowUserToOrderColumns = false;
            dgvA_items.AllowUserToResizeRows = true;

            //add Column
            //  DataGridViewAAddColumn("No");
            //  DataGridViewAAddColumn("Mac");
            //  dgvA_items.Columns[0].ReadOnly = true;
            //  add_VectorA_Row(25);
        }

        private void InitDgvtbcB()
        {
            dgvB_items.AllowUserToAddRows = true;
            dgvB_items.AllowUserToOrderColumns = false;
            dgvB_items.AllowUserToResizeRows = true;

            //add Column
            //DataGridViewBAddColumn("No");
            //DataGridViewBAddColumn("Mac");
            //dgvB_items.Columns[0].ReadOnly = true;
            //add_VectorB_Row(25);
        }

        private void DataGridViewAddColumn(string name)
        {
            //实例化列
            DataGridViewTextBoxColumn dgvtbc = new DataGridViewTextBoxColumn();
            //列名称
            dgvtbc.Name = name;
            //列头名称
            dgvtbc.HeaderText = name;
            //将列增加在dataGridView控件中
            dgv_items.Columns.Add(dgvtbc);
            //修改该列的宽度
            //dgv_items.Columns[name].Width = (dgv_items.Width) / 20-1;  
            dgv_items.Columns[name].Width = 50;
        }

        private void DataGridViewAAddColumn(string name)
        {
            //实例化列
            DataGridViewTextBoxColumn dgvtbc = new DataGridViewTextBoxColumn();
            //列名称
            dgvtbc.Name = name;
            //列头名称
            dgvtbc.HeaderText = name;
            //将列增加在dataGridView控件中
            dgvA_items.Columns.Add(dgvtbc);
            //修改该列的宽度
            if (name == "Mac")
            {
                dgvA_items.Columns[name].Width = ((dgv_items.Width) / 5);
            }
            else
                dgvA_items.Columns[name].Width = (dgv_items.Width) / 10;

        }

        private void DataGridViewBAddColumn(string name)
        {
            //实例化列
            DataGridViewTextBoxColumn dgvtbc = new DataGridViewTextBoxColumn();
            //列名称
            dgvtbc.Name = name;
            //列头名称
            dgvtbc.HeaderText = name;
            //将列增加在dataGridView控件中
            dgvB_items.Columns.Add(dgvtbc);
            //修改该列的宽度
            if (name == "Mac")
            {
                dgvB_items.Columns[name].Width = ((dgv_items.Width) / 5);
            }
            else
                dgvB_items.Columns[name].Width = (dgv_items.Width) / 10;

        }

        private void add_Row(int index)
        {
            for (int i = 0; i < 10; i++)//i为行数
            {
                DataGridViewRow dr = new DataGridViewRow();
                dr.CreateCells(dgv_items);
                for (int j = 0; j < index; j++)//j为列数
                {
                    dr.Cells[j].Value = (++aa);
                }
                dgv_items.Rows.Add(dr);              
                //dgv_items.Rows[0].Height = 20;
                dgv_items.Rows[i].Height = 40;
                //dgv.Rows.Insert(0, dr);　　　　
            }
        }

        private void add_VectorA_Row(int index)
        {
            for (int i = 0; i < index; i++)
            {
                DataGridViewRow dt = new DataGridViewRow();
                dt.CreateCells(dgvA_items);
                dt.Cells[0].Value = (++bb);
                dgvA_items.Rows.Add(dt);                    //插入的数据作为最后一行显示
            }
        }

        private void add_VectorB_Row(int index)
        {
            for (int i = 0; i < index; i++)
            {
                DataGridViewRow dt = new DataGridViewRow();
                dt.CreateCells(dgvB_items);
                dt.Cells[0].Value = (++bb);
                dgvB_items.Rows.Add(dt);                    //插入的数据作为最后一行显示
            }
        }

        #region Threadpoint

        private void RunKM_M21_ScanStartG1()//
        {
            try
            {
                ScanStartG1ThreadProc = new Thread(new ParameterizedThreadStart(this.DoKM_M21ScanStartG1_Thread));
                ScanStartG1ThreadProc.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("thread run fail");
            }
        }

        private void RunKM_M21_ScanStartG2()//
        {
            try
            {
                ScanStartG2ThreadProc = new Thread(new ParameterizedThreadStart(this.DoKM_M21ScanStartG2_Thread));
                ScanStartG2ThreadProc.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("thread run fail");
            }
        }

        private void RunKM_M21_CompDataG1()//
        {
            try
            {
                CompDataTestG1ThreadProc = new Thread(new ParameterizedThreadStart(this.CompDataThreadG1));
                CompDataTestG1ThreadProc.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("thread run fail");
            }
        }

        private void RunKM_M21_CompDataG2()//
        {
            try
            {
                CompDataTestG2ThreadProc = new Thread(new ParameterizedThreadStart(this.CompDataThreadG2));
                CompDataTestG2ThreadProc.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("thread run fail");
            }
        }

        private void Run_CheckTable()//
        {
            try
            {
                CheckTableThreadProc = new Thread(new ParameterizedThreadStart(this.CheckTableThread));
                CheckTableThreadProc.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("thread run fail");
            }
        }

        private void RunKM_M21_SaveDataToSTUG1()//
        {
            try
            {
                SaveDataToSTUG1TestThreadProc = new Thread(new ParameterizedThreadStart(this.SaveDataToSTUG1_Thread));
                SaveDataToSTUG1TestThreadProc.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("thread run fail");
            }
        }

        private void RunKM_M21_SaveDataToSTUG2()//
        {
            try
            {
                SaveDataToSTUG2TestThreadProc = new Thread(new ParameterizedThreadStart(this.SaveDataToSTUG2_Thread));
                SaveDataToSTUG2TestThreadProc.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("thread run fail");
            }
        }

        private void RunKM_M21_StopScanG1()//
        {
            try
            {
                ScanENDG1ThreadProc = new Thread(new ParameterizedThreadStart(this.DoKM_M21StopScanG1_Thread));
                ScanENDG1ThreadProc.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("thread run fail");
            }
        }

        private void RunKM_M21_StopScanG2()//
        {
            try
            {
                ScanENDG1ThreadProc = new Thread(new ParameterizedThreadStart(this.DoKM_M21StopScanG2_Thread));
                ScanENDG1ThreadProc.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("thread run fail");
            }
        }

        private void ScanMacFuncG1()//
        {
            try
            {
                ScanMacThreadG1 = new Thread(new ParameterizedThreadStart(this.ScanMacTestG1_Thread));
                ScanMacThreadG1.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("thread run fail");
            }
        }

        private void ScanMacFuncG2()//
        {
            try
            {
                ScanMacThreadG2 = new Thread(new ParameterizedThreadStart(this.ScanMacTestG2_Thread));
                ScanMacThreadG2.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("thread run fail");
            }
        }

        #endregion

        #region Button

        private void timer_UpdateTestTime_G1_Tick(object sender, EventArgs e)
        {
            g_TesttimeG1 += 1;
            if (comboBoxG1_Function.SelectedIndex == 0)
            {
                this.label_TestTime_G1.Text = g_Testtime.ToString().Trim();
            }
            else
            {
                this.label_TestTime_G1.Text = g_TesttimeG1.ToString().Trim();                
            }         
            
            g_iInterTimeG1= g_iInterTimeG1-1;
            if (0 == g_iInterTimeG1)
            {
                g_iInterTimeG1 = g_iInterTime;
                Thread.Sleep(100);
                UARTON_G1.Enabled = false;
                string Senddata = "AT+BTTEST=0\r\n";
                serialPort1.Write(Senddata);
                Thread.Sleep(200);
                Senddata = "AT+BTTEST=1\r\n";
                serialPort1.Write(Senddata);
                UARTON_G1.Enabled = true;
            }
        }

        private void timer_UpdateTestTime_G2_Tick(object sender, EventArgs e)
        {
            g_TesttimeG2 += 1;
            if (comboBoxG2_Function.SelectedIndex == 0)
            {
                this.label_TestTime_G2.Text = g_Testtime.ToString().Trim();
            }
            else
            {
                this.label_TestTime_G2.Text = g_TesttimeG2.ToString().Trim();
            }  
            
            g_iInterTimeG2 = g_iInterTimeG2 - 1;
            if (0 == g_iInterTimeG2)
            {
                g_iInterTimeG2 = g_iInterTime;
                Thread.Sleep(100);
                UARTON_G2.Enabled = false;
                string Senddata = "AT+BTTEST=0\r\n";
                serialPort2.Write(Senddata);
                Thread.Sleep(200);
                Senddata = "AT+BTTEST=1\r\n";
                serialPort2.Write(Senddata);
                UARTON_G2.Enabled = true;
            }
        }

        private void UARTON_G1_Click(object sender, EventArgs e)//UART ON
        {
            if (UARTON_G1.Text == "UART_ON_G1")
            {
                serialPort1.PortName = comboBoxG1_Name.Text;
                serialPort1.BaudRate = int.Parse(comboBoxG1_BaudRate.Text);
                try
                {
                    serialPort1.Open();     //打开串口
                    UARTON_G1.Text = "UART_OFF_G1";
                    //UARTON_G1.BackColor = System.Drawing.SystemColors.ActiveCaption;
                    comboBoxG1_Name.Enabled = false;//关闭使能                  
                    comboBoxG1_BaudRate.Enabled = false;
                    comboBoxG1_Function.Enabled = false;
                    richTextBox1.Text = "";
                    g_iMacIndexG1 = -1;//编号清零
                    g_NonKMcountG1 = 0;
                    bCycleScanG1 = true;//循环打开,End的时候不要关线程
                    lsMacInforsG1.Clear();//
                    lsMacInforNotInTables.Clear();//
                    g_TesttimeG1 = 0;//计时清零
                    g_iInterTimeG1 = g_iInterTime;
                    this.label_TestTime_G1.Text = "000";//计时清零
                    if (CompDataTestG2ThreadProc == null)
                    {
                        int iNum = -1;//主列表置空
                        for (int i = 0; i < 10; i++)//i为行数
                        {
                            for (int j = 0; j < 20; j++)//j为列数
                            {
                                dgv_items[j, i].Value = (++iNum);
                                stuTableCell[iNum].iX = i;
                                stuTableCell[iNum].iY = j;
                                stuTableCell[iNum].iNum = iNum;
                                stuTableCell[iNum].bG1 = false;
                                stuTableCell[iNum].bG2 = false;
                                stuTableCell[iNum].iTimeMark = 0;
                                stuTableCell[iNum].sText = "";
                                stuTableCell[iNum].sTempter = "";
                            }
                        }
                        this.label_InTable.Text = "000";
                    }
                    if (comboBoxG1_Function.SelectedIndex == 1)
                    {
                        ScanMacFuncG1();//开启ScanMacTest_Thread进程
                    }
                    else
                    {
                        //if (CheckTableThreadProc == null) Run_CheckTable();
                        Run_CheckTable();
                        Thread.Sleep(200);
                        RunKM_M21_CompDataG1();
                        Thread.Sleep(200);
                        RunKM_M21_SaveDataToSTUG1();
                    }
                    //serialPort1.DataReceived += new SerialDataReceivedEventHandler(post_DataReceived);//串口接收处理函数
                }
                catch
                {
                    MessageBox.Show("Uart Cannot open!");
                }
            }
            else
            {
                try
                {  //关闭所有
                    bCycleScanG1 = false;//关闭循环
                    Thread.Sleep(200);
                    serialPort1.Dispose();//关闭串口,放后面会报错不知为啥
                    serialPort1.Close(); 
                    if (ScanMacThreadG1 != null) ScanMacThreadG1.Abort();
                    if (SaveDataToSTUG1TestThreadProc != null) SaveDataToSTUG1TestThreadProc.Abort();
                    if (CompDataTestG1ThreadProc != null) CompDataTestG1ThreadProc.Abort();
                    if (CheckTableThreadProc != null) CheckTableThreadProc.Abort();
                    UARTON_G1.Text = "UART_ON_G1";
                    //UARTON_G1.BackColor = System.Drawing.SystemColors.Control;
                    comboBoxG1_Name.Enabled = true;//打开使能
                    comboBoxG1_BaudRate.Enabled = true;
                    comboBoxG1_Function.Enabled = true;
                    serialPort1.Dispose();
                    serialPort1.Close(); //关闭串口 
                }
                catch
                {
                    MessageBox.Show("Uart Close Fail!");
                }
            }
        }

        private void UARTON_G2_Click(object sender, EventArgs e)
        {
            if (UARTON_G2.Text == "UART_ON_G2")
            {
                serialPort2.PortName = comboBoxG2_Name.Text;
                serialPort2.BaudRate = int.Parse(comboBoxG2_BaudRate.Text);
                try
                {
                    serialPort2.Open();     //打开串口
                    UARTON_G2.Text = "UART_OFF_G2";
                    //UARTON_G1.BackColor = System.Drawing.SystemColors.ActiveCaption;
                    comboBoxG2_Name.Enabled = false;//关闭使能                  
                    comboBoxG2_BaudRate.Enabled = false;
                    comboBoxG2_Function.Enabled = false;

                    ////////////////////////清零部分///////////////////
                    richTextBox2.Text = "";
                    g_iMacIndexG2 = -1;//编号清零
                    g_NonKMcountG2 = 0;
                    bCycleScanG2 = true;//循环打开,End的时候不要关线程
                    lsMacInforsG2.Clear();//
                    lsMacInforNotInTablesG2.Clear();//
                    g_TesttimeG2 = 0;//计时清零
                    g_iInterTimeG2 = g_iInterTime;
                    this.label_TestTime_G2.Text = "000";//计时清零
                    if (CompDataTestG1ThreadProc == null)
                    {
                        int iNum = -1;//主列表置空
                        for (int i = 0; i < 10; i++)//i为行数
                        {
                            for (int j = 0; j < 20; j++)//j为列数
                            {
                                dgv_items[j, i].Value = (++iNum);
                                dgv_items[j, i].Style.BackColor = Color.White;                                    
                                stuTableCell[iNum].iX = i;
                                stuTableCell[iNum].iY = j;
                                stuTableCell[iNum].iNum = iNum;
                                stuTableCell[iNum].bG1 = false;
                                stuTableCell[iNum].bG2 = false;
                                stuTableCell[iNum].iTimeMark = 0;
                                stuTableCell[iNum].sText = "";
                                stuTableCell[iNum].sTempter = "";
                            }
                        }
                        this.label_InTable.Text = "000";

                    }
                    

                    if (comboBoxG2_Function.SelectedIndex == 1)
                    {
                        ScanMacFuncG2();//开启ScanMacTest_Thread进程
                    }
                    else
                    {
                        //if (CheckTableThreadProc == null) Run_CheckTable();  
                        Run_CheckTable(); 
                        Thread.Sleep(200);
                        RunKM_M21_CompDataG2();
                        Thread.Sleep(200);
                        RunKM_M21_SaveDataToSTUG2();
                    }
                    //serialPort1.DataReceived += new SerialDataReceivedEventHandler(post_DataReceived);//串口接收处理函数
                }
                catch
                {
                    MessageBox.Show("Uart Cannot open!");
                }
            }
            else
            {
                try
                {  //关闭所有
                    bCycleScanG2 = false;//关闭循环
                    Thread.Sleep(200);
                    serialPort2.Dispose();//关闭串口,放后面会报错不知为啥
                    serialPort2.Close();
                    if (ScanMacThreadG2 != null) ScanMacThreadG2.Abort();
                    if (SaveDataToSTUG2TestThreadProc != null) SaveDataToSTUG2TestThreadProc.Abort();
                    if (CompDataTestG2ThreadProc != null) CompDataTestG2ThreadProc.Abort();
                    UARTON_G2.Text = "UART_ON_G2";
                    //UARTON_G2.BackColor = System.Drawing.SystemColors.Control;
                    comboBoxG2_Name.Enabled = true;//打开使能
                    comboBoxG2_BaudRate.Enabled = true;
                    comboBoxG2_Function.Enabled = true;
                    serialPort2.Dispose();
                    serialPort2.Close(); //关闭串口 
                }
                catch
                {
                    MessageBox.Show("Uart Close Fail!");
                }
            }
        }

        private void START_G1_Click(object sender, EventArgs e) //Start
        {
            if (!serialPort1.IsOpen)
            {
                MessageBox.Show("串口未打开");
                return;
            }
            if (comboBoxG1_Function.SelectedIndex == 0)//如果是对比功能要检查A，B表格
            {
                if (dgv_itemB_hash[0] == 0 && dgvA_items.RowCount == 0)//如果都是空的
                {
                    MessageBox.Show("A组B组均是空");
                    return;
                }
            }
            if (comboBoxG1_Function.SelectedIndex == 1)//如果是扫描功能
            {
                iuStatus = 0;
                listView1.Items.Clear();
            }
            RunKM_M21_ScanStartG1();            
            START_G1.Enabled = false;
            END_G1.Enabled = true;
            
            this.timer_UpdateTestTime_G1.Enabled = true;
            this.timer_UpdateTestTime_G1.Start();
            //g_Testtime = 0;  //Start不要清零，时间戳会乱，UartOn时间再清零
            //this.label_TestTime_G1.Text = "00";
            //label_InTable.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
        }

        private void START_G2_Click(object sender, EventArgs e)
        {
            if (!serialPort2.IsOpen)
            {
                MessageBox.Show("串口未打开");
                return;
            }
            if (comboBoxG2_Function.SelectedIndex == 0)//如果是对比功能要检查A，B表格
            {
                if (dgv_itemB_hash[0] == 0 && dgvA_items.RowCount == 0)//如果都是空的
                {
                    MessageBox.Show("A组B组均是空");
                    return;
                }
            }
            if (comboBoxG2_Function.SelectedIndex == 1)//如果是扫描功能
            {
                iuStatus = 0;
                listView1.Items.Clear();
            }
            RunKM_M21_ScanStartG2();
            START_G2.Enabled = false;
            END_G2.Enabled = true;

            this.timer_UpdateTestTime_G2.Enabled = true;
            this.timer_UpdateTestTime_G2.Start();
        }

        private void END_G1_Click(object sender, EventArgs e)//END
        {
            //iuStatus = false;
            iuStatus = 2;
            END_G1.Enabled = false;
            START_G1.Enabled = true;
            this.timer_UpdateTestTime_G1.Stop();
            this.timer_UpdateTestTime_G1.Enabled = false;
            //this.timer_UpdateTestTime.Dispose();
            if (serialPort1.IsOpen)
            {//如果串口开启   
                RunKM_M21_StopScanG1();
                //string Senddata = "AT+BTTest=0\r\n";
                //serialPort1.Write(Senddata);

                Data_Count = 0;
                ScanNum = 0;
                /*
                for (int i = 0; i < dgv_items.RowCount; i++)
                {
                    for (int j = 0; j < dgv_items.ColumnCount; j++)
                    {
                        dgv_items[j, i].Style.BackColor = Color.White;
                    }
                }
                 * */
                //richTextBox1.Text = "";

            }
            else
            {
                MessageBox.Show("Uart can not open");
            }
        }

        private void END_G2_Click(object sender, EventArgs e)
        {
            //iuStatus = false;
            iuStatus = 2;
            END_G2.Enabled = false;
            START_G2.Enabled = true;
            this.timer_UpdateTestTime_G2.Stop();
            this.timer_UpdateTestTime_G2.Enabled = false;
            //this.timer_UpdateTestTime.Dispose();
            if (serialPort2.IsOpen)
            {//如果串口开启   
                RunKM_M21_StopScanG2();
                //string Senddata = "AT+BTTest=0\r\n";
                //serialPort1.Write(Senddata);

                Data_Count = 0;
                ScanNum = 0;
                /*
                for (int i = 0; i < dgv_items.RowCount; i++)
                {
                    for (int j = 0; j < dgv_items.ColumnCount; j++)
                    {
                        dgv_items[j, i].Style.BackColor = Color.White;
                    }
                }
                 */

                //richTextBox1.Text = "";

            }
            else
            {
                MessageBox.Show("Uart can not open");
            }
        }

        private void button_OpenA_Click(object sender, EventArgs e)//Open A
        {
            char[] TrimChar = { ' ', '"', '\r', '\"', '\\', '\n', '\t' };
            System.Data.DataTable dt = new System.Data.DataTable();//dt数据阵列
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Excel Files|*.xls";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                String filename = ofd.FileName;
                Microsoft.Office.Interop.Excel.Application excel = new Microsoft.Office.Interop.Excel.Application();
                Microsoft.Office.Interop.Excel.Workbook workbook;//新建工作簿
                Microsoft.Office.Interop.Excel.Worksheet wsWorksheet;//新建工作表
                object oMissing = System.Reflection.Missing.Value;//表示这个参数可以传入缺省值
                workbook = excel.Workbooks.Open(filename, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing);
                wsWorksheet = (Worksheet)workbook.Worksheets[1];//获取第一个工作表
                int iRowCount = wsWorksheet.UsedRange.Rows.Count;//当前Excel工作表中已经被使用过的数据行数
                Console.WriteLine(iRowCount);
                int iColCount = wsWorksheet.UsedRange.Columns.Count;
                Microsoft.Office.Interop.Excel.Range range1;//一定单元格范围
                for (int i = 0; i < iColCount; i++)//遍历第一行的所有列
                {
                    range1 = wsWorksheet.Range[wsWorksheet.Cells[1, i + 1], wsWorksheet.Cells[1, i + 1]];
                    dt.Columns.Add(range1.Value2.ToString());//第一行为dt.Columns数据
                }
                for (int j = 1; j < iRowCount; j++)//从第二行开始遍历所有列
                {
                    DataRow dr = dt.NewRow();//dr为行数据
                    for (int i = 0; i < iColCount; i++)
                    {
                        range1 = wsWorksheet.Range[wsWorksheet.Cells[j + 1, i + 1], wsWorksheet.Cells[j + 1, i + 1]];
                        dr[i] = range1.Value2 == null ? null : range1.Value2;
                    }
                    dt.Rows.Add(dr);//第二行开始为dt.Rows数据
                }
                dgvA_items.DataSource = null;
                dgvA_items.DataSource = dt;
                //dataGridView1.Rows[0].Selected = false;
                dgvA_items.Columns[0].Width = 30;
                dgvA_items.Columns[1].Width = dgvA_items.Width - 30;
                dgvA_items.ClearSelection();
                excel.Quit();
            }
            //for (int i = 0; i < dgvA_items.RowCount;i++ )
            //{
            //    dgvA_items[1, i].Value.ToString() = dgvA_items[1, i].Value.ToString().Trim(TrimChar);
            //}
            for (int i = 0; i < dgvA_items.RowCount; i++)
            {
                stuTACell[i].iNum = int.Parse(dgvA_items[0, i].Value.ToString().Trim(TrimChar));
                stuTACell[i].sMacaddr = dgvA_items[1, i].Value.ToString().Trim(TrimChar);//第一列的第i个值
                stuTACell[i].bMyTableMatch = false;
                dgv_itemA_hash[i] = GetMD5WithString(stuTACell[i].sMacaddr).GetHashCode();
            }
        }

        private void button_OpenB_Click(object sender, EventArgs e) //Open B
        {
            char[] TrimChar = { ' ', '"', '\r', '\"', '\\', '\n', '\t' };
            string aaa = "";
            System.Data.DataTable dt = new System.Data.DataTable();
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Excel Files|*.xls";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                String filename = ofd.FileName;
                Microsoft.Office.Interop.Excel.Application excel = new Microsoft.Office.Interop.Excel.Application();
                Microsoft.Office.Interop.Excel.Workbook workbook;
                Microsoft.Office.Interop.Excel.Worksheet worksheet;
                object oMissing = System.Reflection.Missing.Value;//相当null
                workbook = excel.Workbooks.Open(filename, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing, oMissing);
                worksheet = (Worksheet)workbook.Worksheets[1];
                int rowCount = worksheet.UsedRange.Rows.Count;
                Console.WriteLine(rowCount);
                int colCount = worksheet.UsedRange.Columns.Count;
                Microsoft.Office.Interop.Excel.Range range1;
                for (int i = 0; i < colCount; i++)
                {
                    range1 = worksheet.Range[worksheet.Cells[1, i + 1], worksheet.Cells[1, i + 1]];
                    dt.Columns.Add(range1.Value2.ToString());
                }
                for (int j = 1; j < rowCount; j++)
                {
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i < colCount; i++)
                    {
                        range1 = worksheet.Range[worksheet.Cells[j + 1, i + 1], worksheet.Cells[j + 1, i + 1]];
                        dr[i] = range1.Value2 == null ? null : range1.Value2;
                    }
                    dt.Rows.Add(dr);
                }
                dgvB_items.DataSource = null;
                dgvB_items.DataSource = dt;
                dgvB_items.Columns[0].Width = 35;
                dgvB_items.Columns[1].Width = dgvB_items.Width - 35;
                dgvB_items.ClearSelection();
                excel.Quit();
            }
            for (int i = 0; i < dgvB_items.RowCount; i++)
            {
                aaa = dgvB_items[1, i].Value.ToString().Trim(TrimChar);
                //dgv_itemA_hash[i] = GetMD5WithString(dgvA_items[1, i].Value.ToString()).GetHashCode();
                dgv_itemB_hash[i] = GetMD5WithString(aaa).GetHashCode();
            }
        }

        private void button_TableClear_Click(object sender, EventArgs e)
        {
            int iNum = -1;//主列表置空
            for (int i = 0; i < 10; i++)//i为行数
            {
                for (int j = 0; j < 20; j++)//j为列数
                {
                    dgv_items[j, i].Value = (++iNum);
                    stuTableCell[iNum].iX = i;
                    stuTableCell[iNum].iY = j;
                    stuTableCell[iNum].iNum = iNum;
                    stuTableCell[iNum].bG1 = false;
                    stuTableCell[iNum].bG2 = false;
                    stuTableCell[iNum].iTimeMark = 0;
                    stuTableCell[iNum].sText = "";
                    stuTableCell[iNum].sTempter = "";
                }
            }
        }
        
        #endregion

        #region Other
        private string[] GetstrToMac(string str)
        {
            //global_data.
            string pattrn = "([A-Fa-f0-9]{2}:){5}[A-Fa-f0-9]{2}";
            MatchCollection mc = Regex.Matches(str, pattrn);
            //string[] data = new string[200];
            for (int i = 0; i < mc.Count; i++)
            {
                global_data[i] = mc[i].Value;
            }
            return global_data;
        }

        //找到表格里面的编号
        private bool ContrastExcelA(string[] str, ref string[] indexdata)
        {
            //MessageBox.Show("turn in ContrastExcelA");
            int k;
            for (k = 0; k < str.Length; k++)
            {
                if (str[k] == null)
                {
                    break;
                }
            }

            ///problem
            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    //dgv_items[]
                    if (dgvA_items[1, j].Value.ToString() == "")
                    {
                        break;
                    }
                    if (str[i] == dgvA_items[1, j].Value.ToString())
                    {
                        indexdata[i] = dgvA_items[0, j].Value.ToString();
                        break;
                    }
                    else if (str[i] == dgvB_items[1, j].Value.ToString())
                    {
                        indexdata[i] = dgvB_items[0, j].Value.ToString();
                        break;
                    }
                }
            }
            if (indexdata[0] != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string GetMD5WithString(String input)
        {
            MD5 md5Hash = MD5.Create();
            // 将输入字符串转换为字节数组并计算哈希数据  
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            // 创建一个 Stringbuilder 来收集字节并创建字符串  
            StringBuilder str = new StringBuilder();
            // 循环遍历哈希数据的每一个字节并格式化为十六进制字符串  
            for (int i = 0; i < data.Length; i++)
            {
                str.Append(data[i].ToString("x2"));//加密结果"x2"结果为32位,"x3"结果为48位,"x4"结果为64位
            }
            // 返回十六进制字符串  
            return str.ToString();
        }

        private void showKM_Scan_Num()
        {
            ScanNum = 0;
            if (this.ScanMacForm.Visible)
            {
                this.ScanMacForm.Close();
                this.ScanMacForm.Dispose();
                MessageBox.Show("Scan Fail");
                //return false;
            }
            if (this.ScanMacForm.IsDisposed)
            {
                this.ScanMacForm = new KM_View();
            }
            ScanMacForm.ShowDialog();

        }

        private void listView1_caption()
        {
            listView1.Columns.Add(" NO ", 40, HorizontalAlignment.Center);        //第1列标题添加 
            listView1.Columns.Add(" Mac ", 135, HorizontalAlignment.Center);            //第2列标题添加 
            ImageList imgList = new ImageList();
            imgList.ImageSize = new Size(1, 20);            // 设置行高 25 //分别是宽和高 
            listView1.SmallImageList = imgList;            //这里设置listView的SmallImageList ,用imgList将其撑大
        }

        private void button6_Click(object sender, EventArgs e) //Save
        {
            if (0 == listView1.Items.Count)
            {
                MessageBox.Show("数据为空！");
                return;
            }
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "xls";
            sfd.Filter = "Excel文件(*.xls)|*.xls";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                DoExport(this.listView1, sfd.FileName);
            }
        }

        /// <summary>
        /// 具体导出的方法
        /// </summary>
        /// <param name="listView">ListView</param>
        /// <param name="strFileName">导出到的文件名</param>
        /// 
        private void DoExport(ListView listView, string strFileName)
        {
            int rowNum = listView.Items.Count;
            int columnNum = listView.Items[0].SubItems.Count;
            int rowIndex = 1;//行号
            int columnIndex = 0;//列号
            if (rowNum == 0 || string.IsNullOrEmpty(strFileName))//列表为空或导出的文件名为空
            {
                return;
            }
            if (rowNum > 0)
            {
                //加载Excel
                Microsoft.Office.Interop.Excel.Application xlApp = new Microsoft.Office.Interop.Excel.Application();
                if (xlApp == null)//判断是否装了Excel
                {
                    MessageBox.Show("无法创建excel对象，可能您的系统没有安装excel");
                    return;
                }
                xlApp.DefaultFilePath = "";
                xlApp.DisplayAlerts = true;//是否需要显示提示
                xlApp.SheetsInNewWorkbook = 1;//返回或设置Microsoft Excel自动插入到新工作簿中的工作表数。
                Microsoft.Office.Interop.Excel.Workbook xlBook = xlApp.Workbooks.Add(true);//创建工作铺
                //将ListView的列名导入Excel表第一行
                foreach (ColumnHeader dc in listView.Columns)
                {
                    columnIndex++;//行号自增
                    xlApp.Cells[rowIndex, columnIndex] = dc.Text;
                }
                //将ListView中的数据导入Excel中
                for (int i = 0; i < rowNum; i++)
                {
                    rowIndex++;//列号自增
                    columnIndex = 0;
                    for (int j = 0; j < columnNum; j++)
                    {
                        columnIndex++;
                        //注意这个在导出的时候加了“\t” 的目的就是避免导出的数据显示为科学计数法。可以放在每行的首尾。
                        xlApp.Cells[rowIndex, columnIndex] = Convert.ToString(listView.Items[i].SubItems[j].Text) + "\t";
                    }
                }
                //例外需要说明的是用strFileName,Excel.XlFileFormat.xlExcel9795保存方式时 当你的Excel版本不是95、97 而是2003、2007 时导出的时候会报一个错误：异常来自 HRESULT:0x800A03EC。 解决办法就是换成strFileName, Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookNormal。
                //xlBook.SaveAs(strFileName, Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookNormal, Type.Missing, Type.Missing, false, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing, true);
                xlBook.SaveAs(strFileName, Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookNormal, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);

                //xlApp = null;
                //xlBook = null;
                xlBook.Close(Type.Missing, Type.Missing, Type.Missing);
                xlApp.Quit();
                MessageBox.Show("导出文件成功！");
                GC.Collect();
            }
        }

        /// <summary>
        /// 在已有路径文件追加保存
        /// </summary>
        private void Save(string pathFile)
        {
            int columnNum = listView1.Items[0].SubItems.Count;
            int rowIndex = 1;//行号
            int columnIndex = 0;//列号
            int rowNum = this.listView1.Items.Count;
            if (rowNum == 0)//列表为空
            {
                return;
            }
            else
            {
                //加载Excel
                Microsoft.Office.Interop.Excel.Application xlApp = new Microsoft.Office.Interop.Excel.Application();
                if (xlApp == null)//判断是否装了Excel
                {
                    MessageBox.Show("无法创建excel对象，可能您的系统没有安装excel");
                    return;
                }
                xlApp.DefaultFilePath = "";
                //Microsoft.Office.Interop.Excel.Workbook xlBook = xlApp.Workbooks.Add(pathFile);//已有模版创建工作铺                               
                //Microsoft.Office.Interop.Excel.Workbook xlBook = xlApp.Workbooks.Open(pathFile, Type.Missing, false, Type.Missing, Type.Missing, Type.Missing, false, Type.Missing, Type.Missing, false, true, Type.Missing, Type.Missing, true, Type.Missing);//创建工作铺
                //Microsoft.Office.Interop.Excel.Workbook xlBook = xlApp.Workbooks.Open(pathFile, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                Microsoft.Office.Interop.Excel.Workbook xlBook = xlApp.Workbooks.Open(pathFile, Type.Missing, false, Type.Missing, Type.Missing, Type.Missing, false, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, Type.Missing, false, true, Type.Missing, Type.Missing, true, Type.Missing);//创建工作铺
                //将ListView中的数据导入Excel中
                for (int i = 0; i < rowNum; i++)
                {
                    rowIndex = Convert.ToInt32(listView1.Items[i].Text) + 2;//行号由表格行号给出,可以结合自己表格修改
                    columnIndex = 0;//列号归零
                    for (int j = 0; j < columnNum; j++)
                    {
                        columnIndex++;
                        //注意这个在导出的时候加了“\t” 的目的就是避免导出的数据显示为科学计数法。可以放在每行的首尾。
                        xlApp.Cells[rowIndex, columnIndex] = Convert.ToString(listView1.Items[i].SubItems[j].Text) + "\t";
                    }
                }
                //例外需要说明的是用strFileName,Excel.XlFileFormat.xlExcel9795保存方式时 当你的Excel版本不是95、97 而是2003、2007 时导出的时候会报一个错误：异常来自 HRESULT:0x800A03EC。 解决办法就是换成strFileName, Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookNormal。
                xlBook.Save();
                xlBook.Close(Type.Missing, Type.Missing, Type.Missing);
                xlApp.Quit();
                MessageBox.Show("导出文件成功！");
                GC.Collect();
            }

        }

        private void comboBoxG1_Function_SelectedIndexChanged(object sender, EventArgs e)
        {
            g_iFunctinIndexG1 = comboBoxG1_Function.SelectedIndex;
            if (g_iFunctinIndexG1 == 2 || g_iFunctinIndexG1==4)
            {
                textBox_G1.Enabled = true;
            }
            else
            {
                textBox_G1.Enabled = false;
            }
        }

        private void comboBoxG2_Function_SelectedIndexChanged(object sender, EventArgs e)
        {
            g_iFunctinIndexG2 = comboBoxG2_Function.SelectedIndex;
            if (g_iFunctinIndexG2 == 2 || g_iFunctinIndexG2 == 4)
            {
                textBox_G2.Enabled = true;
            }
            else
            {
                textBox_G2.Enabled = false;
            }

        }


        private void label2_Click(object sender, EventArgs e)
        {

        }

        #endregion

        private void DoKM_M21ScanStartG1_Thread(object pa)
        {        
            if (!serialPort1.IsOpen) 
            {
                MessageBox.Show("串口没有打开！"); //如果串口开启 
                return;
            }
            if (comboBoxG1_Function.SelectedIndex == 0 || comboBoxG1_Function.SelectedIndex == 1)
            {
                Thread.Sleep(100);
                UARTON_G1.Enabled = false;
                string Senddata = "AT+BTTEST=1\r\n";
                serialPort1.Write(Senddata);
                Thread.Sleep(200);
                UARTON_G1.Enabled = true;
            }
            if (g_iFunctinIndexG1 == 2)
            {
                string sTime=textBox_G1.Text;
                if (sTime == "")
                {
                    MessageBox.Show("请输入间隔时间"); //如果串口开启 
                    return;
                }
                UARTON_G1.Enabled = false;
                string Senddata2 = "AT+GCRESOURCE\r\n";
                serialPort1.Write(Senddata2);
                //int iCount = 0;
                //while(true)
                //{
                //    string logdata = serialPort1.ReadLine(); //循环读取串口
                //    richTextBox1.Text += logdata; //放入编辑框中
                //    if ("OK\r" == logdata)
                //    {
                //        //Thread.Sleep(200);
                //        string Senddata = "AT+BTSCANINTERVAL=" + sTime + "\r\n";
                //        serialPort1.Write(Senddata);
                //        break;
                //    }
                //    Thread.Sleep(100);
                //    iCount++;
                //    if (iCount > 100)
                //    {
                //        MessageBox.Show("GCRESOURCE FAIL!");
                //        break;
                //    }
                //}                
                Thread.Sleep(10000);
                string Senddata = "AT+BTSCANINTERVAL=" + sTime + "\r\n";
                serialPort1.Write(Senddata);
                UARTON_G1.Enabled = true;
                START_G1.Enabled = true;

            }
            if (g_iFunctinIndexG1 == 3)
            {
                Thread.Sleep(100);
                UARTON_G1.Enabled = false;
                string Senddata = "AT+BTSCANINTERVAL?\r\n";
                serialPort1.Write(Senddata);
                Thread.Sleep(200);
                UARTON_G1.Enabled = true;
                START_G1.Enabled = true;
            }
            if (g_iFunctinIndexG1 == 4)
            {
                Thread.Sleep(100);                
                string sTime = textBox_G1.Text;
                if (sTime=="")
                {
                    MessageBox.Show("请输入间隔时间"); //如果串口开启 
                    return;                    
                }                      
                UARTON_G1.Enabled = false;
                string Senddata2 = "AT+GCRESOURCE\r\n";
                serialPort1.Write(Senddata2);
                //int iCount = 0;
                //while (true)
                //{
                //    string logdata = serialPort1.ReadLine(); //循环读取串口
                //    richTextBox1.Text += logdata; //放入编辑框中
                //    if ("OK\r" == logdata)
                //    {
                //        //Thread.Sleep(200);
                //        string Senddata = "AT+BTSCANWINDOW=" + sTime + "\r\n";
                //        serialPort1.Write(Senddata);
                //        break;
                //    }
                //    Thread.Sleep(100);
                //    iCount++;
                //    if (iCount > 100)
                //    {
                //        MessageBox.Show("GCRESOURCE FAIL!");
                //        break;
                //    }
                //}              
                Thread.Sleep(10000);
                string Senddata = "AT+BTSCANWINDOW=" + sTime + "\r\n";
                serialPort1.Write(Senddata);
                UARTON_G1.Enabled = true;
                START_G1.Enabled = true;
            }
            if (g_iFunctinIndexG1 == 5)
            {
                Thread.Sleep(100);
                UARTON_G1.Enabled = false;
                string Senddata = "AT+BTSCANWINDOW?\r\n";
                serialPort1.Write(Senddata);
                Thread.Sleep(200);
                UARTON_G1.Enabled = true;
                START_G1.Enabled = true;
            }
        }

        private void DoKM_M21ScanStartG2_Thread(object pa)
        {
            if (!serialPort2.IsOpen)
            {
                MessageBox.Show("串口没有打开！"); //如果串口开启 
                return;
            }
            if (comboBoxG2_Function.SelectedIndex == 0 || comboBoxG2_Function.SelectedIndex == 1)
            {
                Thread.Sleep(100);
                UARTON_G2.Enabled = false;
                string Senddata = "AT+BTTEST=1\r\n";
                serialPort2.Write(Senddata);
                Thread.Sleep(200);
                UARTON_G2.Enabled = true;
            }
            if (g_iFunctinIndexG2 == 2)
            {
                string sTime = textBox_G2.Text;
                if (sTime == "")
                {
                    MessageBox.Show("请输入间隔时间"); //如果串口开启 
                    return;
                }
                UARTON_G2.Enabled = false;
                string Senddata2 = "AT+GCRESOURCE\r\n";
                serialPort2.Write(Senddata2);
                //int iCount = 0;
                //while (true)
                //{
                //    string logdata = serialPort2.ReadLine(); //循环读取串口
                //    richTextBox2.Text += logdata; //放入编辑框中
                //    if ("OK\r" == logdata)
                //    {
                //        //Thread.Sleep(200);
                //        string Senddata = "AT+BTSCANINTERVAL=" + sTime + "\r\n";
                //        serialPort2.Write(Senddata);
                //        break;
                //    }
                //    Thread.Sleep(100);
                //    iCount++;
                //    if (iCount > 100)
                //    {
                //        MessageBox.Show("GCRESOURCE FAIL!");
                //        break;
                //    }
                //}                
                Thread.Sleep(10000);
                string Senddata = "AT+BTSCANINTERVAL=" + sTime + "\r\n";
                serialPort2.Write(Senddata);
                UARTON_G2.Enabled = true;
                START_G2.Enabled = true;
            }
            if (g_iFunctinIndexG2 == 3)
            {
                Thread.Sleep(100);
                UARTON_G2.Enabled = false;
                string Senddata = "AT+BTSCANINTERVAL?\r\n";
                serialPort2.Write(Senddata);
                Thread.Sleep(200);
                UARTON_G2.Enabled = true;
                START_G2.Enabled = true;
            }
            if (g_iFunctinIndexG2 == 4)
            {
                Thread.Sleep(100);
                string sTime = textBox_G2.Text;
                if (sTime == "")
                {
                    MessageBox.Show("请输入间隔时间"); //如果串口开启 
                    return;
                }
                UARTON_G2.Enabled = false;
                string Senddata2 = "AT+GCRESOURCE\r\n";
                serialPort2.Write(Senddata2);
                //int iCount = 0;
                //while (true)
                //{
                //    string logdata = serialPort2.ReadLine(); //循环读取串口
                //    richTextBox2.Text += logdata; //放入编辑框中
                //    if ("OK\r" == logdata)
                //    {
                //        //Thread.Sleep(200);
                //        string Senddata = "AT+BTSCANWINDOW=" + sTime + "\r\n";
                //        serialPort2.Write(Senddata);
                //        break;
                //    }
                //    Thread.Sleep(100);
                //    iCount++;
                //    if (iCount > 100)
                //    {
                //        MessageBox.Show("GCRESOURCE FAIL!");
                //        break;
                //    }
                //} 
                Thread.Sleep(10000);
                string Senddata = "AT+BTSCANWINDOW=" + sTime + "\r\n";
                serialPort2.Write(Senddata);
                UARTON_G2.Enabled = true;
                START_G2.Enabled = true;
            }
            if (g_iFunctinIndexG2 == 5)
            {
                Thread.Sleep(100);
                UARTON_G2.Enabled = false;
                string Senddata = "AT+BTSCANWINDOW?\r\n";
                serialPort2.Write(Senddata);
                Thread.Sleep(200);
                UARTON_G2.Enabled = true;
                START_G2.Enabled = true;
            }

        }

        private void DoKM_M21StopScanG1_Thread(object pa)
        {
            if (serialPort1.IsOpen)
            {//如果串口开启  
                for (int i = 0; i < 2; i++)
                {
                    UARTON_G1.Enabled = false;
                    Thread.Sleep(500);
                    string Senddata = "AT+BTTEST=0\r\n";
                    serialPort1.Write(Senddata);
                }
                UARTON_G1.Enabled = true;
            }
        }

        private void DoKM_M21StopScanG2_Thread(object pa)
        {
            if (serialPort2.IsOpen)
            {//如果串口开启  
                for (int i = 0; i < 2; i++)
                {
                    UARTON_G2.Enabled = false;
                    Thread.Sleep(500);
                    string Senddata = "AT+BTTEST=0\r\n";
                    serialPort2.Write(Senddata);
                }
                UARTON_G2.Enabled = true;
            }
        }
        //此线程实现扫描循环检测数据
        private void ScanMacTestG1_Thread(object pa)
        {
            do
            {
                try
                {
                    string logdata = "";
                    //if (iuStatus == 0)//判断是否读满mac数
                    //{
                    logdata = serialPort1.ReadLine();
                    richTextBox1.Text += logdata;
                    if ("KM Goldie power on\r" == logdata)
                    {
                        Thread.Sleep(100);
                        string Senddata = "AT+FACTTEST\r\n";
                        serialPort1.Write(Senddata);
                        START_G1.Enabled = true;
                        END_G1.Enabled = false;
                    }
                    if (logdata.IndexOf("NonKM") != -1)
                    {
                        string[] sArray = logdata.Split(new char[1] { ':' });
                        string sNonKMcount = sArray[1];
                        g_NonKMcountG1 = int.Parse(sNonKMcount.Trim());
                        label_OtherG1.Text = g_NonKMcountG1.ToString();
                        continue;
                    }
                    Regex rg_chk = new Regex("([A-Fa-f0-9]{2}:){5}[A-Fa-f0-9]{2}");
                    // 定义一个Regex对象实例
                    Match mt_chk = rg_chk.Match(logdata);
                    string macData = mt_chk.Value.Trim();
                    if (macData != "")
                    {
                        ////////////过滤相同的////////
                        stuMacInfor.sMACaddr = macData;//地址
                        var stuIsnewMacInfor = lsMacInforsG1.FirstOrDefault(MacInfor => MacInfor.sMACaddr == stuMacInfor.sMACaddr);
                        if (stuIsnewMacInfor.sMACaddr == null)
                        {//如果是新MAC增加
                            lsMacInforsG1.Add(stuMacInfor);
                        }
                        else continue;//如果是旧MAC不管
                        ///////////////////////     
                        ListViewItem item = new ListViewItem(xxx[Data_Count]);
                        item.Text = Data_Count.ToString();
                        // item.SubItems.Add(Data_Count.ToString());
                        item.SubItems.Add(macData);
                        listView1.Items.Add(item);
                        Data_Count++;

                        int iTemplabel = Data_Count;
                        this.label_InTable.Text = iTemplabel.ToString();

                        int iTemprich = Data_Count - 1;
                        string sTemp = iTemprich.ToString();
                        sTemp = "Num:" + sTemp + "\r";
                        richTextBox1.Text += sTemp; //放入编辑框中
                    }
                }
                catch (System.Exception ex)
                {
                    //System.Windows.Forms.Application.Exit();
                }
            } while (bCycleScanG1);
            //serialPort1.DiscardOutBuffer();//清空串口输出缓冲区
        }

        private void ScanMacTestG2_Thread(object pa)
        {
            do
            {
                try
                {
                    string logdata = "";
                    //if (iuStatus == 0)//判断是否读满mac数
                    //{
                    logdata = serialPort2.ReadLine();
                    richTextBox2.Text += logdata;
                    if ("KM Goldie power on\r" == logdata)
                    {
                        Thread.Sleep(100);
                        string Senddata = "AT+FACTTEST\r\n";
                        serialPort2.Write(Senddata);
                        START_G2.Enabled = true;
                        END_G2.Enabled = false;
                    }
                    if (logdata.IndexOf("NonKM") != -1)
                    {
                        string[] sArray = logdata.Split(new char[1] { ':' });
                        string sNonKMcount = sArray[1];
                        g_NonKMcountG2 = int.Parse(sNonKMcount.Trim());
                        label_OtherG2.Text = g_NonKMcountG2.ToString();
                        continue;
                    }
                    Regex rg_chk = new Regex("([A-Fa-f0-9]{2}:){5}[A-Fa-f0-9]{2}");
                    // 定义一个Regex对象实例
                    Match mt_chk = rg_chk.Match(logdata);
                    string macData = mt_chk.Value.Trim();
                    if (macData != "")
                    {
                        ////////////过滤相同的////////
                        stuMacInfor.sMACaddr = macData;//地址
                        var stuIsnewMacInfor = lsMacInforsG2.FirstOrDefault(MacInfor => MacInfor.sMACaddr == stuMacInfor.sMACaddr);
                        if (stuIsnewMacInfor.sMACaddr == null)
                        {//如果是新MAC增加
                            lsMacInforsG2.Add(stuMacInfor);
                        }
                        else continue;//如果是旧MAC不管
                        ///////////////////// 
                        ListViewItem item = new ListViewItem(xxx[Data_Count]);
                        item.Text = Data_Count.ToString();
                        // item.SubItems.Add(Data_Count.ToString());
                        item.SubItems.Add(macData);
                        listView1.Items.Add(item);
                        Data_Count++;

                        int iTemplabel = Data_Count;
                        this.label_InTable.Text = iTemplabel.ToString();

                        int iTemprich = Data_Count - 1;
                        string sTemp = iTemprich.ToString();
                        sTemp = "Num:" + sTemp + "\r";
                        richTextBox2.Text += sTemp; //放入编辑框中
                    }
                }
                catch (System.Exception ex)
                {
                    //System.Windows.Forms.Application.Exit();
                }
            } while (bCycleScanG2);
        }

        private void SaveDataToSTUG1_Thread(object pa)
        {
            do
            {
                try
                {
                    string logdata = serialPort1.ReadLine(); //循环读取串口
                    richTextBox1.Text += logdata; //放入编辑框中
                    if ("KM Goldie power on\r" == logdata)
                    {
                        Thread.Sleep(100);
                        string Senddata = "AT+FACTTEST\r\n";
                        serialPort1.Write(Senddata);
                        START_G1.Enabled = true;
                        END_G1.Enabled = false;
                    }
                    if (logdata.IndexOf("NonKM")!=-1)
                    {
                        string[] sArray = logdata.Split(new char[1] { ':' });
                        string sNonKMcount = sArray[1];
                        g_NonKMcountG1 = int.Parse(sNonKMcount.Trim());
                        label_OtherG1.Text = g_NonKMcountG1.ToString();
                        continue;
                    }
                    // 定义一个Regex对象实例，正则表达式符号模式
                    Regex rg_chk = new Regex("([A-Fa-f0-9]{2}:){5}[A-Fa-f0-9]{2}");
                    Match mt_chk = rg_chk.Match(logdata);//静态Match方法，可以得到源中第一个匹配模式的连续子串
                    string macData = mt_chk.Value;
                    if (macData!="")
                    {
                        string[] sArray = logdata.Split(new char[1] { ';' });
                        string sTemper = sArray[2];
                        float fTemper = float.Parse(sTemper);
                        fTemper = fTemper / 10;
                        sTemper = fTemper.ToString();//温度

                        g_bSignalNoRunG1 = true; //线程同步    
             
                        g_iMacIndexG1++; //读数记一   
                        stuMacInfor.sMACaddr = macData;//地址
                        stuMacInfor.sTempter = sTemper;//温度
                        stuMacInfor.iTimeMark = g_TesttimeG1;//时间戳
                                         
                        var stuIsnewMacInfor = lsMacInforsG1.FirstOrDefault(MacInfor => MacInfor.sMACaddr == stuMacInfor.sMACaddr);
                        if (stuIsnewMacInfor.sMACaddr==null)
                        {//如果是新MAC增加
                            lsMacInforsG1.Add(stuMacInfor);
                        }
                        else
                        {//如果是旧MAC,只改时间和温度
                            if (stuMacInfor.iTimeMark>stuIsnewMacInfor.iTimeMark)
                            {
                                for (int i = lsMacInforsG1.Count - 1; i >= 0; i--)//先删除在增加
                                {
                                    if (lsMacInforsG1[i].sMACaddr == stuIsnewMacInfor.sMACaddr)
                                        lsMacInforsG1.Remove(lsMacInforsG1[i]);
                                }
                                lsMacInforsG1.Add(stuMacInfor);
                            }                           
                        }
                        g_bSignalNoRunG1 = false; //线程同步
                        /////////////////Log框显示数目
                        string sTemp = g_iMacIndexG1.ToString();
                        sTemp = "Num:" + sTemp + "\r";
                        richTextBox1.Text += sTemp; //放入编辑框中
                        //////////////////////////////                          
                    }
                }
                catch (System.Exception ex)
                {
                    //System.Windows.Forms.Application.Exit();
                }
            } while (bCycleScanG1);
         }

        private void SaveDataToSTUG2_Thread(object pa)
        {
            do
            {
                try
                {
                    string logdata = serialPort2.ReadLine(); //循环读取串口
                    richTextBox2.Text += logdata; //放入编辑框中
                    if ("KM Goldie power on\r" == logdata)
                    {
                        Thread.Sleep(100);
                        string Senddata = "AT+FACTTEST\r\n";
                        serialPort2.Write(Senddata);
                        START_G2.Enabled = true;
                        END_G2.Enabled = false;
                    }
                    if (logdata.IndexOf("NonKM") != -1)
                    {
                        string[] sArray = logdata.Split(new char[1] { ':' });
                        string sNonKMcount = sArray[1];
                        g_NonKMcountG2 = int.Parse(sNonKMcount.Trim());
                        label_OtherG2.Text = g_NonKMcountG2.ToString();
                        continue;
                    }
                    // 定义一个Regex对象实例，正则表达式符号模式
                    Regex rg_chk = new Regex("([A-Fa-f0-9]{2}:){5}[A-Fa-f0-9]{2}");
                    Match mt_chk = rg_chk.Match(logdata);//静态Match方法，可以得到源中第一个匹配模式的连续子串
                    string macData = mt_chk.Value;
                    if (macData!="")
                    {
                        string[] sArray = logdata.Split(new char[1] { ';' });
                        string sTemper = sArray[2];
                        float fTemper = float.Parse(sTemper);
                        fTemper = fTemper / 10;
                        sTemper = fTemper.ToString();//温度

                        g_bSignalNoRunG2 = true; //线程同步  
                        g_iMacIndexG2++; //读数记一   
                        stuMacInfor.sMACaddr = macData;//地址
                        stuMacInfor.sTempter = sTemper;//温度
                        stuMacInfor.iTimeMark = g_TesttimeG2;//时间戳
                        var stuIsnewMacInfor = lsMacInforsG2.FirstOrDefault(MacInfor => MacInfor.sMACaddr == stuMacInfor.sMACaddr);
                        if (stuIsnewMacInfor.sMACaddr==null)
                        {//如果是新MAC增加
                            lsMacInforsG2.Add(stuMacInfor);
                        }
                        else
                        {//如果是旧MAC,只改时间和温度
                            if (stuMacInfor.iTimeMark>stuIsnewMacInfor.iTimeMark)
                            {
                                if (stuMacInfor.iTimeMark > stuIsnewMacInfor.iTimeMark)
                                {
                                    for (int i = lsMacInforsG2.Count - 1; i >= 0; i--)//先删除在增加
                                    {
                                        if (lsMacInforsG2[i].sMACaddr == stuIsnewMacInfor.sMACaddr)
                                            lsMacInforsG2.Remove(lsMacInforsG1[i]);
                                    }
                                    lsMacInforsG2.Add(stuMacInfor);
                                } 
                            }                           
                        }
                        g_bSignalNoRunG2 = false; //线程同步

                        /////////////////Log框显示数目
                        string sTemp = g_iMacIndexG2.ToString();
                        sTemp = "Num:" + sTemp + "\r";
                        richTextBox2.Text += sTemp; //放入编辑框中
                        //////////////////////////////                          
                    }
                }
                catch (System.Exception ex)
                {
                    //System.Windows.Forms.Application.Exit();
                }
            } while (bCycleScanG2);
         }

        private void CompDataThreadG1(object pa)
        {
            do
            {
            try
            {
                if (g_bSignalNoRunG1 == true) continue;//线程同步             
                if (g_iMacIndexG1 == -1) continue;
                if (lsMacInforsG1[0].sMACaddr == null) continue; //没有值的时候 
                Thread.Sleep(500); //等待其他进程
                if (dgv_itemA_hash[0] != 0)//A组不为空
                {
                    for (int iTemp = 0; iTemp <lsMacInforsG1.Count; iTemp++)
                    {
                        string sA_Index = "";                        
                        for (int i = 0; i < dgvA_items.RowCount; i++)
                        {
                            if (lsMacInforsG1[iTemp].sMACaddr == stuTACell[i].sMacaddr)
                            {
                                int iA_Index = stuTACell[i].iNum;
                                sA_Index = dgvA_items[0, i].Value.ToString();
                                stuTACell[i].bMyTableMatch = true;
                                break;
                            }
                        }
                        if (sA_Index != "")//找到了匹配，A组编号sA_Index不为空   
                        {  
                            int iA_Index = int.Parse(sA_Index);
                            stuTableCell[iA_Index].sMacaddr = lsMacInforsG1[iTemp].sMACaddr;
                            if (iA_Index > 200)//当前主表格中没有A组的这个编号
                            {
                                MessageBox.Show("Your Table Index Is Out Of Range!");
                            }
                            else
                            {
                                //只要时间大，不管什么颜色，都要改时间，温度
                                if (stuTableCell[iA_Index].iTimeMark <= lsMacInforsG1[iTemp].iTimeMark)//新G2时间大于主表时间
                                {
                                    stuTableCell[iA_Index].iTimeMark = lsMacInforsG1[iTemp].iTimeMark;//变主表的时间戳
                                    stuTableCell[iA_Index].sTempter = lsMacInforsG1[iTemp].sTempter;//改温度戳
                                    dgv_items[stuTableCell[iA_Index].iY, stuTableCell[iA_Index].iX].Value = sA_Index + "#" + lsMacInforsG1[iTemp].sTempter;
                                }
                                if (stuTableCell[iA_Index].bG1 == true)
                                { //颜色检测
                                    if (stuTableCell[iA_Index].bG2 == true)//之前是绿色，不变色
                                    {
                                        continue;
                                    }
                                    else continue;                             
                                }
                                else
                                {  //颜色检测
                                    stuTableCell[iA_Index].bG1 = true;//先改变主表属性
                                    if (stuTableCell[iA_Index].bG2 == true)
                                    {
                                        dgv_items[stuTableCell[iA_Index].iY, stuTableCell[iA_Index].iX].Style.BackColor = Color.Green;//改颜色
                                    }
                                    else
                                    {
                                        dgv_items[stuTableCell[iA_Index].iY, stuTableCell[iA_Index].iX].Style.BackColor = Color.Orange;
                                    }
                                    //计数牌显示数目
                                    int iInTabel = 0;
                                    for (int i = 0; i < dgvA_items.RowCount; i++)
                                    {
                                        if (stuTACell[i].bMyTableMatch == true)
                                        {
                                            iInTabel = iInTabel + 1;
                                        }
                                    }
                                    this.label_InTable.Text = iInTabel.ToString();
                                    if (iInTabel == dgvA_items.RowCount)
                                    {
                                        END_G1_Click(null, null);
                                        MessageBox.Show("All Device Find, Scan Stop!");
                                    }
                                }
                            }
                        }
                        else//A组编号sA_Index为空，当前值不在A表格内   
                        {
                            //var tempMacInfor = lsMacInforNotInTables.FirstOrDefault(stuMacInforNotInTable => stuMacInforNotInTable.sMACaddr == lsMacInforsG1[iTemp].sMACaddr);
                            var tempMacInfor = lsMacInforNotInTables.FirstOrDefault(MacInfor => MacInfor.sMACaddr == lsMacInforsG1[iTemp].sMACaddr);
                            if (tempMacInfor.sMACaddr == null)//当前lsMacInforsG1[iTemp].sMACaddr不在lsMacInforNotInTables中
                            {
                                stuMacInforNotInTable.sMACaddr = lsMacInforsG1[iTemp].sMACaddr;
                                lsMacInforNotInTables.Add(stuMacInforNotInTable);
                                label_NONKM.Text = lsMacInforNotInTables.Count.ToString();
                            }
                        }
                    }
                }

            }
            catch (System.Exception ex)
            {
                //System.Windows.Forms.Application.Exit();
            }
            } while (bCycleScanG1);
        }

        private void CompDataThreadG2(object pa)
        {
            do
            {
            try
            {
                if (g_bSignalNoRunG2 == true) continue;//线程同步             
                if (g_iMacIndexG2 == -1) continue;
                if (lsMacInforsG2[0].sMACaddr == null) continue; //没有值的时候 
                Thread.Sleep(500); //等待其他进程
                if (dgv_itemA_hash[0] != 0)//A组不为空
                {
                    for (int iTemp = 0; iTemp <lsMacInforsG2.Count; iTemp++)
                    { //主表:stuTableCell[iA_Index];A表stuTACell[i];MacList:lsMacInforsG2[iTemp];
                        string sA_Index = "";
                        for (int i = 0; i < dgvA_items.RowCount; i++)
                        {
                            if (lsMacInforsG2[iTemp].sMACaddr == stuTACell[i].sMacaddr)
                            {
                                int iA_Index = stuTACell[i].iNum;
                                sA_Index = dgvA_items[0, i].Value.ToString();
                                stuTACell[i].bMyTableMatch = true;
                                break;
                            }
                        }                       
                        if (sA_Index != "")//找到了匹配，A组编号sA_Index不为空   
                        {  
                            int iA_Index = int.Parse(sA_Index);
                            stuTableCell[iA_Index].sMacaddr = lsMacInforsG2[iTemp].sMACaddr;
                            if (iA_Index > 200)//当前主表格中没有A组的这个编号
                            {
                                MessageBox.Show("Your Table Index Is Out Of Range!");
                            }
                            else
                            {
                                if (stuTableCell[iA_Index].iTimeMark <= lsMacInforsG2[iTemp].iTimeMark)//新G2时间大于主表时间
                                {
                                    stuTableCell[iA_Index].iTimeMark = lsMacInforsG2[iTemp].iTimeMark;//变主表的时间戳
                                    stuTableCell[iA_Index].sTempter = lsMacInforsG2[iTemp].sTempter;//改温度戳
                                    dgv_items[stuTableCell[iA_Index].iY, stuTableCell[iA_Index].iX].Value = sA_Index + "#" + lsMacInforsG2[iTemp].sTempter;
                                }
                                if (stuTableCell[iA_Index].bG2 == true)//主表存过G2蓝色，之前是蓝色或绿色，
                                {   
                                    //颜色检测
                                    if (stuTableCell[iA_Index].bG1 == true)//之前是绿色，不变色
                                    {
                                        continue;
                                    }
                                    else continue;//之前是蓝色，不变色                                 
                                }
                                else//主表没存过G2,要么G1橘色，要么没色
                                {
                                    //颜色检测
                                    stuTableCell[iA_Index].bG2 = true;//先改变主表属性
                                    if (stuTableCell[iA_Index].bG1 == true)//之前是橘色，变绿色
                                    {
                                        dgv_items[stuTableCell[iA_Index].iY, stuTableCell[iA_Index].iX].Style.BackColor = Color.Green;//改颜色
                                    }
                                    else//之前是白色，变蓝色
                                    {
                                        dgv_items[stuTableCell[iA_Index].iY, stuTableCell[iA_Index].iX].Style.BackColor = Color.Blue;
                                    }
                                    //计数牌显示数目
                                    int iInTabel = 0;
                                    for (int i = 0; i < dgvA_items.RowCount; i++)
                                    {
                                        if (stuTACell[i].bMyTableMatch == true)
                                        {
                                            iInTabel = iInTabel + 1;
                                        }
                                    }
                                    this.label_InTable.Text = iInTabel.ToString();
                                    if (iInTabel == dgvA_items.RowCount)
                                    {
                                        END_G2_Click(null, null);
                                        MessageBox.Show("All Device Find, Scan Stop!");
                                    }
                                }
                            }
                        }
                        else//A组编号sA_Index为空，当前值不在A表格内   
                        {
                            var tempMacInfor = lsMacInforNotInTables.FirstOrDefault(MacInfor => MacInfor.sMACaddr == lsMacInforsG2[iTemp].sMACaddr);
                            if (tempMacInfor.sMACaddr == null)//当前lsMacInforsG1[iTemp].sMACaddr不在lsMacInforNotInTables中
                            {
                                stuMacInforNotInTable.sMACaddr = lsMacInforsG2[iTemp].sMACaddr;
                                lsMacInforNotInTables.Add(stuMacInforNotInTable);
                                label_NONKM.Text = lsMacInforNotInTables.Count.ToString();
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                //System.Windows.Forms.Application.Exit();
            }
            } while (bCycleScanG2);
        }
            
        private void CheckTableThread(object pa)
        {
            int iInterTime = int.Parse(label_Inter.Text.ToString());
            do
            {
                Thread.Sleep(500);
                if (g_TesttimeG1 >= g_TesttimeG2) g_Testtime = g_TesttimeG1;
                else g_Testtime = g_TesttimeG2;
                for (int iTemp = 0; iTemp < 200; iTemp++)
                {
                    if (stuTableCell[iTemp].bG1 == false && stuTableCell[iTemp].bG2 == false) continue;
                    if (iInterTime < (g_Testtime - stuTableCell[iTemp].iTimeMark))
                    {
                        if (stuTableCell[iTemp].bG1==true)
                        {
                             for (int i = lsMacInforsG1.Count-1; i>=0; i--)
                            {
                                if (lsMacInforsG1[i].sMACaddr == stuTableCell[iTemp].sMacaddr)
                                    lsMacInforsG1.Remove(lsMacInforsG1[i]);
                            }
                        }
                        if (stuTableCell[iTemp].bG2 == true)
                        {
                            for (int i = lsMacInforsG2.Count - 1; i >= 0; i--)
                            {
                                if (lsMacInforsG2[i].sMACaddr == stuTableCell[iTemp].sMacaddr)
                                    lsMacInforsG2.Remove(lsMacInforsG2[i]);
                            }
                        } //先删除list的数据然后在变表格，不然没用
                        dgv_items[stuTableCell[iTemp].iY, stuTableCell[iTemp].iX].Value = stuTableCell[iTemp].iNum.ToString();
                        dgv_items[stuTableCell[iTemp].iY, stuTableCell[iTemp].iX].Style.BackColor = Color.White;//改颜色
                        stuTableCell[iTemp].bG1 = false;
                        stuTableCell[iTemp].bG2 = false;
                        for (int i = 0; i < dgvA_items.RowCount; i++)
                        {
                            if (stuTableCell[iTemp].sMacaddr == stuTACell[i].sMacaddr)
                            {
                                stuTACell[i].bMyTableMatch = false;//   释放A表匹配
                            }
                        }
                        //计数牌显示数目
                        int iInTabel = 0;
                        for (int i = 0; i < dgvA_items.RowCount; i++)
                        {
                            if (stuTACell[i].bMyTableMatch == true)
                            {
                                iInTabel = iInTabel + 1;
                            }
                        }
                        this.label_InTable.Text = iInTabel.ToString();
                    }
                }
            } while (bCycleScanG2||bCycleScanG1);

        }

    }
}
