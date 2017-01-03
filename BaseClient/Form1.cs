using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using ArmAssistBll;
using SYNCC;
using OpenNETCF.Net.NetworkInformation;
using System.Threading;
//using System.Reflection;

namespace BaseClient
{
    enum RunningState
    {
        Login = 99,
        Main = 0,
        Func1 = 1,
        Func2 = 2,
        Func3 = 3,
        Restock = 11,
        GoodsCheck = 13
    }
    public partial class Form1 : Form
    {
        private string _stockNo;
        private string _workerNo;
        private TextBox _nowControlModule;    //��ǰ��ȡ����Ŀؼ�
        private TextBox _nextControlModule;   //��һ������ؼ�
        private RunningState _nowRunningState;        //��ǰ����״̬
        private string _codeStr;
        private int _pFlag;
        private string _outStr;
        //private NLSScanner scanCode = new NLSScanner();
        TcpClient m_socketClient;
        private int _ConnectTimeOut;
        private string _stockName;
        private string _workerName;
        private string _applicationName;
        private string _serverIP;
        private int _serverPort;
        private int _oldTime;
        private string _IpAddress;
        // 2M �Ľ��ջ�������Ŀ����һ�ν�������������ص���Ϣ
        byte[] m_receiveBuffer = new byte[2048 * 1024];

        private bool _cFlag;
        private SYSTEM_POWER_STATUS_EX status;

        private string _goodsNo;
        private string _goodsName;
        private string _goodsAmount;
        private string _goodsType;
        private string _goodsClss;
        private string _goodsSpec;
        private string _goodsTypeNo;
        private string _goodsTypeName;
        private string _inAmount;
        private string _inAmountP;
        private bool _connFlag;

        [DllImport("coredll.Dll")]
        public static extern int SetWindowPos(IntPtr hwnd, int hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);
        [DllImport("coredll.Dll")]
        public static extern void SetForegroundWindow(IntPtr hwnd);

        [DllImport("coredll.dll", EntryPoint = "ShowWindow")]
        public static extern int ShowWindow(IntPtr hWnd, Int32 nCmdShow);
        public const int SW_SHOW = 5; public const int SW_HIDE = 0;
        [DllImport("Coredll.dll", EntryPoint = "GetTickCount")]
        private static extern int GetTickCount();

        private class SYSTEM_POWER_STATUS_EX
        {
            public byte ACLineStatus = 0;
            public byte BatteryFlag = 0;
            public byte BatteryLifePercent = 0;
            public byte Reserved1 = 0;
            public uint BatteryLifeTime = 0;
            public uint BatteryFullLifeTime = 0;
            public byte Reserved2 = 0;
            public byte BackupBatteryFlag = 0;
            public byte BackupBatteryLifePercent = 0;
            public byte Reserved3 = 0;
            public uint BackupBatteryLifeTime = 0;
            public uint BackupBatteryFullLifeTime = 0;
        }
        [DllImport("coredll")]
        private static extern int GetSystemPowerStatusEx(SYSTEM_POWER_STATUS_EX lpSystemPowerStatus, bool fUpdate);

        public Form1()
        {
            InitializeComponent();
            Init();
        }
        /// <summary>
        /// ��ȡ���еĺ�����
        /// </summary>
        /// <returns></returns>
        private int GetTick()
        {
            return GetTickCount();
        }

        /// <summary>
        /// ��ȡ����
        /// </summary>
        /// <returns></returns>
        private int GetPower()
        {
            if (GetSystemPowerStatusEx(status, false) == 1)
            {
                if (status.BatteryLifePercent > 100)
                    status.BatteryLifePercent = 100;
                return status.BatteryLifePercent;
            }
            else
            {
                return -1;
            }
        }


        /// <summary>
        /// ��ʾ����
        /// </summary>
        /// <param name="msg"></param>
        private void ShowMessage(string msg, string title)
        {
            /*
            try
            {
                //_nextControlModule = _nowControlModule;
                // _nowControlModule = null;
                //MessageBox.Show("test","error");
                MessageBox.Show(msg, title);
                //_nowControlModule = _nextControlModule;
            }
            catch (Exception e)
            {
                MessageBox.Show("�������:" + e.Message, "����");
            }
             * */
        }

        /// <summary>
        /// У������
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private bool CheckBarCode(string code)
        {
            int mOdd = 0;
            int mEven = 0;
            int mNumber = 0;
            for (int i = 1; i < code.Length; i++)
            {
                mNumber = int.Parse(code[i - 1].ToString());
                if (i % 2 == 0)
                {
                    mEven += mNumber;
                }
                else
                {
                    mOdd += mNumber;
                }
            }
            mEven *= 3;
            mNumber = mOdd + mEven;
            mNumber = (10 - (mNumber % 10)) % 10;
            if (mNumber.ToString() == code[code.Length - 1].ToString())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// ˢ�µ���
        /// </summary>
        private void ShowPower()
        {
            // statusBar1.Text = "����:" + NLSSysInfo.GetPowerPercent().ToString() + "%";
            //statusBar1.Text = GetTick().ToString();
            statusBar1.Text = "�û�:" + _workerName + "    |����:" + GetPower() + "%";
        }

        /// <summary>
        /// �����ʼ��
        /// </summary>
        private void Init()
        {
            //SetWindowPos(this.Handle, -1, 0, 0, 0, 0, 1 | 2);
            //ShowWindow(this.Handle,SW_SHOW);
            // SetForegroundWindow(this.Handle);
            status = new SYSTEM_POWER_STATUS_EX();
            _oldTime = 0;
            _workerNo = "";
            _stockNo = "";
            _outStr = "";
            _codeStr = "";
            _stockName = "";
            _workerName = "";
            _goodsNo = "";
            _goodsAmount = "";
            _goodsName = "";
            _goodsType = "";
            _goodsClss = "";
            _inAmount = "";
            _inAmountP = "";
            _connFlag = true;
            //NetWorkScript.Instance.init();

            //todo
            _applicationName = "TallyClient";
            _cFlag = true;
            _pFlag = 1;
            _nowRunningState = RunningState.Login;
            _nowControlModule = tb_worker_no;
            tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(login);
            tb_worker_no.Focus();
            //tabControl1.Focus();
            XmlDocument xml = new XmlDocument();
            xml.Load("\\Program Files\\CONFIG.XML");
            _ConnectTimeOut = int.Parse(xml.SelectSingleNode("/Root/System/maxSessionTimeout").InnerText) * 1000;
            try
            {
                ProcessInfo[] list = ProcessCE.GetProcesses();
                foreach (ProcessInfo item in list)
                {
                    if (item.FullPath.IndexOf("AutoUpdate") > 0)
                    {
                        item.Kill();
                    }
                }
                _serverIP = xml.SelectSingleNode("/Root/System/server_ip").InnerText;
                _serverPort = int.Parse(xml.SelectSingleNode("/Root/System/server_port").InnerText);
                _stockName = xml.SelectSingleNode("/Root/System/stock_name").InnerText;
                _stockNo = xml.SelectSingleNode("/Root/System/stock_no").InnerText;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "����");
            }
            ShowPower();
            try
            {
                _IpAddress = WifiCtrl.GetInstance().GetWifiStatus().CurrentIpAddress.ToString();
                if (_IpAddress == "0.0.0.0")
                {
                    _IpAddress = IPHelper.GetIpAddress();
                }
            }
            catch
            {
                _IpAddress = IPHelper.GetIpAddress();
            }

        }

        /// <summary>
        /// ���ӷ�����
        /// </summary>
        private void Connect()
        {
            /*
            lock (this)
            {
                try
                {
                    m_socketClient = new TcpClient(_serverIP, _serverPort);
                    m_socketClient.ReceiveTimeout = 10 * 1000;

                    if (m_socketClient.Client.Connected)
                    {
                        //this.AddInfo("���ӳɹ�.");
                    }
                    else
                    {
                        //this.AddInfo("����ʧ��.");
                        ShowMessage("����ʧ��!", "����");
                    }

                }
                catch { }
            }
            //_oldTime = GetTick();
             * */
        }

        /// <summary>
        /// ��������Ͽ�����
        /// </summary>
        private void Disconnect()
        {
            /*
            lock (this)
            {
                if (m_socketClient == null)
                {
                    return;
                }

                try
                {
                    m_socketClient.Close();
                    //this.AddInfo("�Ͽ����ӳɹ���");
                }
                catch
                {
                    //this.AddInfo("�Ͽ�����ʱ����: " + err.Message);
                    // ShowMessage("�Ͽ�����ʱ����: " + err.Message,"����");
                }
                finally
                {
                    m_socketClient = null;
                    _oldTime = 0;
                }
            }
             * */
        }

        /// <summary>
        /// ��ʾ��Ϣ
        /// </summary>
        /// <param name="message"></param>
        private void AddInfo(string message,RunningState state,Color backColor)
        {
            ShowPower();
            //ShowMessage(msg, "��ʾ", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            tb_ResultShow.Text = "";
            if (message.Length == 0)
            {
                //ShowMessage("�޷�����Ϣ��","����");
                //tb_ResultShow.Text = "�޷�����Ϣ��";
                return;
            }
            else
            {
                string[] msg = message.Split('^');
                foreach (string str in msg)
                {
                    tb_ResultShow.Text = tb_ResultShow.Text + str + "\r\n";
                }
            }
            if (backColor == Color.Red)
            {
                tb_ResultShow.Text += "\r\n����1�� ȷ�Ͻ��";
            }
            //if (state == RunningState.Restock)  //todo ����״̬�ı䰴��������ʾ
            //{
                //tb_ResultShow.Text += "\r\n����1�� ȷ�ϲ���";
           // }
            //else
           // {
                //tb_ResultShow.Text += "\r\n��1ȷ�ϴ�����";
            //}
            tb_ResultShow.BackColor = backColor;
            //tb_ResultShow.Text += Test();
            p_msg.Visible = true;
            _nextControlModule = _nowControlModule;
            _nowControlModule = tb_Confirm;
            tb_Confirm.Focus();
            buz_on();
            /*
            if (message.IndexOf("�ɹ�") == -1)
            {
                tb_ResultShow.BackColor = Color.Red;
                //ShowMessage(textBox8.BackColor.ToString(),"color");
                buz_on();
            }
            else
            {
                tb_ResultShow.BackColor = Color.Green;
            }
            //_outStr="";
             * */

        }

        /// <summary>
        /// ������Ϣ
        /// </summary>
        private void SendOneDatagram()
        {
            if (GetTick() > (_oldTime + _ConnectTimeOut))
            {
                if (m_socketClient != null)
                {
                    this.Disconnect();
                }
                this.Connect();
            }

            string datagramText2 = "1#" + _pFlag + "#" + _codeStr + "#" + _applicationName + "#" + _stockNo;

            byte[] b = Encoding.UTF8.GetBytes(datagramText2);//����ָ�����뽫string����ֽ�����
            string datagramText = string.Empty;
            for (int i = 0; i < b.Length; i++)//���ֽڱ�Ϊ16�����ַ�����%����
            {
                datagramText += "%" + Convert.ToString(b[i], 16);
            }

            //byte[] encbuff = System.Text.Encoding.UTF8.GetBytes(datagramText);
            //datagramText = Convert.ToBase64String(encbuff);
            //if (ShowMessage(datagramText, "��ʾ", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.OK)
            //{
            //Application.Exit();
            //}
            //datagramText = textBox1.Text + "#" + textBox2.Text + "#" + textBox3.Text + "|" + textBox4.Text + "|" + textBox5.Text + "|";
            //datagramText += textBox6.Text + "|" + textBox8.Text + "|" + textBox7.Text + "|#";

            byte[] Cmd = Encoding.ASCII.GetBytes(datagramText);
            byte check = (byte)(Cmd[0] ^ Cmd[1]);
            for (int i = 2; i < Cmd.Length; i++)
            {
                check = (byte)(check ^ Cmd[i]);
            }
            datagramText = "<" + datagramText + (char)check + ">";
            byte[] datagram = Encoding.ASCII.GetBytes(datagramText);

            try
            {
                m_socketClient.Client.Send(datagram);
                //this.AddInfo("send text = " + datagramText);

                //if (ck_AsyncReceive.Checked)  // �첽���ջش�
                // {
                //m_socketClient.Client.BeginReceive(m_receiveBuffer, 0, m_receiveBuffer.Length, SocketFlags.None, this.EndReceiveDatagram, this);
                //}
                // else
                // {
                this.Receive();
                //}
            }
            catch (Exception err)
            {
                if (_cFlag)
                {
                    _cFlag = false;
                    if (m_socketClient != null)
                    {
                        this.Disconnect();
                    }
                    this.Connect();
                    try
                    {
                        m_socketClient.Client.Send(datagram);
                        this.Receive();
                    }
                    catch { }

                }
                else
                {
                    //this.AddInfo("���ʹ���: " + err.Message);
                    ShowMessage("���ӷ�����ʧ��: " + err.Message, "����");
                    //this.AddInfo("���ӷ�����ʧ��:!\r\n" + err.Message);
                    _outStr = "";
                    this.CloseClientSocket();
                    _oldTime = 0;
                }

            }
        }

        private void Receive()
        {
            try
            {
                int len = m_socketClient.Client.Receive(m_receiveBuffer, 0, m_receiveBuffer.Length, SocketFlags.None);
                if (len > 0)
                {
                    CheckReplyDatagram(len);
                }
                _oldTime = GetTick();
            }
            catch (Exception err)
            {
                //this.AddInfo("���մ���: " + err.Message);
                ShowMessage("���մ���: " + err.Message, "����");
                this.CloseClientSocket();
                _oldTime = 0;
            }
        }

        private void CheckReplyDatagram(int len)
        {
            string datagramText = Encoding.ASCII.GetString(m_receiveBuffer, 0, len);
            //byte[] decbuff = Convert.FromBase64String(replyMesage);
            if (datagramText[0] != '%')
            {
                _outStr = "���ص���Ϣ����";
                return;
            }
            string[] chars = datagramText.Substring(1, datagramText.Length - 1).Split('%');
            byte[] b = new byte[chars.Length];
            //����ַ���Ϊ16�����ֽ�����
            for (int i = 0; i < chars.Length; i++)
            {
                b[i] = Convert.ToByte(chars[i], 16);
            }
            //����ָ�����뽫�ֽ������Ϊ�ַ���
            //string content = Encoding.UTF8.GetString(b);
            _outStr = Encoding.UTF8.GetString(b, 0, b.Length);
            //this.AddInfo(replyMesage);
        }

        /// <summary>
        /// �رտͻ�������
        /// </summary>
        private void CloseClientSocket()
        {
            try
            {
                m_socketClient.Client.Shutdown(SocketShutdown.Both);
                m_socketClient.Client.Close();
            }
            catch
            {
            }
        }

        /// <summary>
        /// ����������
        /// </summary>
        private void buz_on()
        {
            /*
            int m_iFreq = 2730;
            int m_iVolume = 60;
            int m_iMdelay = 300;
            int m_iBuzCtrlRe = -1;
            m_iBuzCtrlRe = NLSSysCtrl.buz_ctrl(m_iFreq, m_iVolume, m_iMdelay);
            NLSSysCtrl.vibrator_ctrl(m_iMdelay);
             * */
            //Sound sound = new Sound("Program Files//GoodsHandle//buz.wav");
            //sound.Play();

        }

        /// <summary>
        /// �˳�����
        /// </summary>
        public void Quit()
        {
            if (MessageBox.Show("�Ƿ��˳�?", "��ʾ", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.OK)
            {
                _nowControlModule = null;
                _nextControlModule = null;
                //this.Disconnect();
                ProcessContext pi = new ProcessContext();
                ProcessCE.CreateProcess("\\Program Files\\AutoUpdate\\AutoUpdate.exe",
                                  "", IntPtr.Zero,
                                  IntPtr.Zero, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, pi);
                Thread.Sleep(2500);
                Application.Exit();
            }
        }

        /// <summary>
        /// �������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IsLostFocus(object sender, EventArgs e)
        {
            if (_nowControlModule != null)
            {
                _nowControlModule.Focus();
            }
        }

        /// <summary>
        /// �������ȷ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tb_Confirm_KeyUp(object sender, KeyEventArgs e)
        {
            /*
            if (e.KeyCode == Keys.D1)
            {
                
                try
                {
                    _pFlag = 4;
                    string codestr = _codeStr.Replace("|", "_");
                    _codeStr = _serialNo + "|" + _applicationName + "|" + _stockNo + "|" + codestr + "|" + textBox8.Text + "|" + _workerNo + "|";
                    SendOneDatagram();
                }
                catch
                {
                    ShowMessage(_outStr, "����");
                }
                finally
                {
                    tb_Confirm.Text = "";
                    tb_ResultShow.Text = "";
                    _nowControlModule = _nextControlModule;
                    p_msg.Visible = false;
                    _nowControlModule.Text = "";
                    _nowControlModule.Focus();
                    _codeStr = "";
                    _outStr = "";
                    tb_ResultShow.BackColor = Color.Yellow;
                }
            }
             * */
            switch (_nowRunningState)
            {
                case RunningState.Restock:
                    if (e.KeyCode==Keys.Enter)
                    {
                        ConfirmFinished();
                        _nowRunningState = RunningState.Func2;
                        _nowControlModule = tb_restock_num;
                        tb_restock_num.Text = "";
                        //label_GoodsNo.Text = _goodsNo;
                        p_restock_num.Visible = true;
                        _nowControlModule.Focus();
                    }
                        /*
                    else if (e.KeyCode == Keys.D2)
                    {
                        ConfirmFinished();
                        _nowRunningState = RunningState.Func2;
                        _codeStr = _goodsNo + "|" + _goodsAmount + "||D2|" + _stockNo + "|" + _workerNo;
                        _codeStr += "|0|" + _goodsClss + "|";
                        _pFlag = 23;
                        SendOneDatagram();
                        Color bc = Color.Silver;
                        if (_outStr.IndexOf("�ɹ�") >= 0)
                        {
                            bc = Color.Green;
                        }
                        else
                        {
                            bc = Color.Red;
                        }
                        AddInfo(_outStr, _nowRunningState, bc);
                    }
                         * */
                    break;
                case RunningState.GoodsCheck:
                    if (e.KeyCode == Keys.Enter)
                    {
                        ConfirmFinished();
                        _nowRunningState = RunningState.Func3;
                        _codeStr = _goodsNo + "|" + _stockNo + "|" + _inAmountP + "|" + _inAmount + "|" + _workerNo + "|";
                        _pFlag = 27;
                        //SendOneDatagram();
                        NewTransmit();
                        Color bc = Color.Silver;
                        if (_outStr != "0")
                        {
                            bc = Color.Red;
                            AddInfo(_outStr, _nowRunningState, bc);
                        }

                    }
                    break;
                default:
                    if (e.KeyCode == Keys.Enter)
                    {
                        if (tb_ResultShow.BackColor != Color.Red)
                        {
                            ConfirmFinished();
                        }
                    }
                    else if (e.KeyCode == Keys.D1)
                    {
                        if (tb_ResultShow.BackColor == Color.Red)
                        {
                            ConfirmFinished();
                        }
                    }
                    break;
            }
        }
        /// <summary>
        /// ȷ�ϲ�������󴥷�
        /// </summary>
        void ConfirmFinished()
        {
            tb_Confirm.Text = "";
            tb_ResultShow.Text = "";
            _nowControlModule = _nextControlModule;
            p_msg.Visible = false;
            _nowControlModule.Text = "";
            _nowControlModule.Focus();
            _codeStr = "";
            _outStr = "";
        }
        /// <summary>
        /// ��������ת
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tb_ui_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.D1:
                    _nowRunningState = RunningState.Func1;
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(Func1);
                    //todo
                    label7.Text = "����ѯ����";
                    label7.BackColor = Color.Silver;
                    _nowControlModule = tb_func1_focus;
                    tb_func1_focus.Focus();
                    break;
                case Keys.D2:
                    _nowRunningState = RunningState.Func2;
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(Func1);
                    //todo
                    label7.Text = "�����������";
                    label7.BackColor = Color.Green;
                    _nowControlModule = tb_func1_focus;
                    tb_func1_focus.Focus();
                    break;
                case Keys.D3:
                    _nowRunningState = RunningState.Func3;
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(Func2);
                    //todo
                    _nowControlModule = tb_func2_focus;
                    tb_func2_focus.Focus();
                    break;
                case Keys.D4:
                    _nowRunningState = RunningState.Login;
                    tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(login);
                    _nowControlModule = tb_worker_no;
                    tb_worker_no.Focus();
                    break;
            }
        }

        private void tb_worker_no_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (tb_worker_no.Text.Length > 0)
                    {
                        _nowControlModule = tb_password;
                        tb_password.Focus();
                    }
                    break;
                case Keys.Escape:
                    if (tb_worker_no.Text == "")
                    {
                        Quit();
                    }
                    else
                    {
                        tb_worker_no.Text = "";
                    }
                    break;
            }
        }

        /// <summary>
        /// �û���¼
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tb_password_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (tb_password.Text.Length > 0)
                    {
                        //todo
                        _pFlag = 25;
                        _codeStr = tb_worker_no.Text + "|" + tb_password.Text + "|" + _stockNo + "|";
                        //SendOneDatagram();
                        NewTransmit();
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        
                        string[] data = _outStr.Split('#');
                        if (data[0] == "SUCCESS")
                        {
                            _workerNo = tb_worker_no.Text;
                            _workerName = data[1];
                            _nowControlModule = tb_ui;
                            //todo
                            ShowPower();
                            //statusBar2.Text = "�û�:" + _workerName + "    | ����:" + GetPower();
                            //buz_on();
                            tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(main);
                            _nowRunningState = RunningState.Main;
                            tb_ui.Focus();
                            //this.AddInfo("��¼�ɹ�");
                            //textBox9.Focus();
                        }
                        else
                        {
                            _nowControlModule = tb_worker_no;
                            this.AddInfo(_outStr,_nowRunningState,Color.Red);
                        }
                        tb_worker_no.Text = "";
                        tb_password.Text = "";
                    }
                    break;
                case Keys.Escape:
                    if (tb_password.Text == "")
                    {
                        _nowControlModule = tb_worker_no;
                        tb_worker_no.Focus();
                    }
                    else
                    {
                        tb_password.Text = "";
                    }
                    break;
            }
        }

        private void tb_func1_focus_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    _pFlag = 22;
                    _codeStr = tb_func1_focus.Text + "|" + _workerNo + "|" + _stockNo + "|";
                    //SendOneDatagram();
                    NewTransmit();
                    string[] data = _outStr.Split('#');
                    string msg = "";
                    Color bc = Color.Silver;
                    if (data.Length > 8)
                    {
                        _goodsNo = data[0];
                        _goodsName = data[1];
                        _goodsAmount = data[2];
                        _goodsType = data[3];
                        _goodsClss = data[4];
                        _goodsSpec = data[5];
                        _goodsTypeNo = data[6];
                        _goodsTypeName = data[7];
                        _inAmount = data[9];
                        _inAmountP = data[10];
                        //if (_nowRunningState == RunningState.Func2)
                        //{
                            //_nowRunningState = RunningState.Restock;
                        //}
                        msg = "��Ʒ��ţ� "+data[0]+"^��Ʒ���ƣ�"+data[1]+"^��������� ";
                        if (data[2] == "")
                        {
                            msg += "�޷���ȡ";
                        }
                        else
                        {
                            msg += data[2];
                        }
                        msg += "^ǰ����������" + data[10];
                        msg += "^������������" + data[9];
                        msg += "^" + data[3];
                        msg += "^" + data[11];
                        if (_nowRunningState == RunningState.Func1)
                        {
                            AddInfo(msg, _nowRunningState, bc);
                        }
                        else
                        {
                            tb_func1_focus.Text = "";
                            tb_func2_goodsMsg.Text = "";
                            if (msg.Length == 0)
                            {
                                //ShowMessage("�޷�����Ϣ��","����");
                                //tb_ResultShow.Text = "�޷�����Ϣ��";
                                AddInfo("�޷�����Ϣ��", _nowRunningState, Color.Red);
                                return;
                            }
                            else
                            {
                                string[] msgs = msg.Split('^');
                                foreach (string str in msgs)
                                {
                                    tb_func2_goodsMsg.Text = tb_func2_goodsMsg.Text + str + "\r\n";
                                }
                                _nowRunningState = RunningState.Func2;
                                _nowControlModule = tb_restock_num;
                                tb_restock_num.Text = "";
                                p_restock_num.Visible = true;
                                _nowControlModule.Focus();
                            }

                        }
                    }
                    else
                    {
                        msg = "���ص���������!^" + _outStr;
                        bc = Color.Red;
                        AddInfo(msg, _nowRunningState, bc);
                    }
                    
                    break;
                case Keys.Escape:
                    if (tb_func1_focus.Text.Length > 0)
                    {
                        tb_func1_focus.Text = "";
                    }
                    else
                    {
                        _nowControlModule = tb_ui;
                        ShowPower();
                        tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(main);
                        _nowRunningState = RunningState.Main;
                        tb_ui.Focus();
                    }
                    break;
            }
        }

        /// <summary>
        /// ���벹��������ȷ�ϲ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tb_restock_num_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                /*
                try
                {
                    //int i = int.Parse(tb_restock_num.Text);
                    float f = float.Parse(tb_restock_num.Text);
                    //if (i > int.Parse(_goodsAmount))
                    //{
                        //AddInfo("������������ڿ����!", _nowRunningState, Color.Red);
                        //tb_restock_num.Text = "";
                        //return;
                   // }
                }
                catch
                {
                    tb_restock_num.Text = "";
                    return;
                }
                 * */
                string restock_sts;
                if (tb_restock_num.Text == "0")
                {
                    restock_sts = "D2";
                }
                else
                {
                    restock_sts = "D1";
                }
                _codeStr = _goodsNo + "|" + _goodsAmount + "||" + restock_sts + "|" + _stockNo + "|" + _workerNo + "|";
                _codeStr += tb_restock_num.Text + "|" + _goodsClss + "|" + _inAmount + "|" + _inAmountP + "|";
                _pFlag = 23;
                //SendOneDatagram();
                NewTransmit();
                Color bc = Color.Silver;
                _nowControlModule = tb_func1_focus;
                p_restock_num.Visible = false;
                tb_restock_num.Text = "";
                if (_outStr.IndexOf("�ɹ�") < 0)
                {
                    bc = Color.Red;
                    AddInfo(_outStr, _nowRunningState, bc);
                }
                _nowControlModule.Focus();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                tb_restock_num.Text = "";
                p_restock_num.Visible = false;
                _nowControlModule = tb_func1_focus;
                tb_func1_focus.Focus();
            }
            else if (e.KeyCode == Keys.D0)
            {
                string restock_sts;
                if (tb_restock_num.Text == "0")
                {
                    restock_sts = "D2";
                    _codeStr = _goodsNo + "|" + _goodsAmount + "||" + restock_sts + "|" + _stockNo + "|" + _workerNo + "|";
                    _codeStr += tb_restock_num.Text + "|" + _goodsClss + "|" + _inAmount + "|" + _inAmountP + "|";
                    _pFlag = 23;
                    //SendOneDatagram();
                    NewTransmit();
                    Color bc = Color.Silver;
                    _nowControlModule = tb_func1_focus;
                    p_restock_num.Visible = false;
                    tb_restock_num.Text = "";
                    if (_outStr.IndexOf("�ɹ�") < 0)
                    {
                        bc = Color.Red;
                        AddInfo(_outStr, _nowRunningState, bc);
                    }
                    _nowControlModule.Focus();
                }
            }
        }

        /// <summary>
        /// ��ѯ������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tb_func2_focus_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (tb_func2_focus.Text != "")
                    {
                        _pFlag = 26;
                        _codeStr = tb_func2_focus.Text + "|" + _workerNo + "|" + _stockNo + "|";
                        //SendOneDatagram();
                        NewTransmit();
                        string[] data = _outStr.Split('#');
                        string msg = "";
                        Color bc = Color.Silver;
                        if (data.Length > 8)
                        {
                            _goodsNo = data[0];
                            _goodsName = data[1];
                            _goodsAmount = data[2];
                            _goodsType = data[3];
                            _goodsClss = data[4];
                            _goodsSpec = data[5];
                            _goodsTypeNo = data[6];
                            _goodsTypeName = data[7];
                            _inAmount = data[9];
                            _inAmountP = data[10];
                            //if (_nowRunningState == RunningState.Func2)
                            //{
                            //_nowRunningState = RunningState.Restock;
                            //}
                            msg = "��Ʒ��ţ� " + _goodsNo + "^��Ʒ���ƣ�" + _goodsName + "^";
                            msg += "^" + data[11];
                            msg += "^ǰ����������" + _inAmountP;
                            msg += "^������������" + _inAmount;
                            msg += "^        �ϼƣ�"+(float.Parse(_inAmount)+float.Parse(_inAmountP));
                            _nowRunningState = RunningState.GoodsCheck;
                            AddInfo(msg, _nowRunningState, bc);
                        }
                        else
                        {
                            msg = "���ص���������!^" + _outStr;
                            bc = Color.Red;
                            AddInfo(msg, _nowRunningState, bc);
                        }
                           
                    }
                    break;
                case Keys.Escape:
                    if (tb_func2_focus.Text.Length > 0)
                    {
                        tb_func2_focus.Text = "";
                    }
                    else
                    {
                        _nowControlModule = tb_ui;
                        ShowPower();
                        tabControl1.SelectedIndex = tabControl1.TabPages.IndexOf(main);
                        _nowRunningState = RunningState.Main;
                        tb_ui.Focus();
                    }
                    break;
            }
        }
        /// <summary>
        /// �µ�ͨ�ŷ�ʽ
        /// </summary>
        private void NewTransmit()
        {
            string msg;
            if (!WifiCtrl.GetInstance().isConnectWifi(_IpAddress,out msg))
            {
                //MessageBox.Show(msg+",�뻻���ط����¿���!");
                NetWorkScript.Instance.release();
                _outStr = msg;
                return;
            }
            CompactFormatter.TransDTO transDTO = new CompactFormatter.TransDTO();
            transDTO.AppName = _applicationName;
            transDTO.CodeStr = _codeStr;
            transDTO.IP = _IpAddress;
            transDTO.pFlag = _pFlag;
            transDTO.StockNo = _stockNo;
            transDTO.Remark = msg;
            NetWorkScript.Instance.write(1, 1, 1, transDTO);
            NetWorkScript.Instance.AsyncReceive();
            if (NetWorkScript.Instance.messageList.Count > 0)
            {
                SocketModel socketModel = NetWorkScript.Instance.messageList[0];
                NetWorkScript.Instance.messageList.RemoveAt(0);
                _outStr = socketModel.message.ToString();
                _connFlag = true;
            }
            else
            {
                NetWorkScript.Instance.release();
                if (_connFlag)
                {
                    _connFlag = false;
                    Thread.Sleep(2000);
                    NewTransmit();
                }
                else
                {
                    _outStr = "û�з�����Ϣ!";
                }
            }
        }

    }
}