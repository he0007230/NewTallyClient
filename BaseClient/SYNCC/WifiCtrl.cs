using System;
using System.Collections.Generic;
using System.Text;
//using OpenNETCF.WindowsCE;
using System.Runtime.InteropServices;
using OpenNETCF.Net.NetworkInformation;
using System.Threading;
using Microsoft.Win32;
using System.Net;

namespace SYNCC
{
    public class WifiCtrl
    {

        private static WifiCtrl _instance = null;
        private static object _lock = new object();
        private WirelessNetworkInterface non_wzc = null;
        private string wifiMAC = "";

        private WifiCtrl() { }

        /// <summary>
        /// Lazy����ģʽ
        /// </summary>
        /// <returns></returns>
        public static WifiCtrl GetInstance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new WifiCtrl();
                    }
                }
            }
            return _instance;
        }

        #region ȫ�ֱ���
        //private WirelessNetworkInterface m_wzc = null;//ȫ�ֵ�WZC����

        //static private string keyValue = null;    
        //private const int POWER_NAME = 0x00000001;//���ڲ���Wifi�豸�ĵ�Դ����
        #endregion

        #region ����wifi�����е�DLL����
        /// <summary>
        /// ��ȡ�豸��Դ״̬
        /// </summary>
        /// <param name="pvDevice">�豸��ע����еļ�ֵ</param>
        /// <param name="Flags">Power_Name</param>
        /// <param name="state">��Դ״̬ö�٣�����OpenNETCF.WindowsCE�����ռ��µ�DevicePowerState</param>
        /// <returns></returns>
        //[DllImport("coredll.dll")]
        //protected static extern int GetDevicePower(string pvDevice, int Flags, ref DevicePowerState state);

        /// <summary>
        /// �豸��Դ�������Ŵ�Ҳ���MSDN��Ͳ���İ����
        /// </summary>
        /// <param name="pvDevice">�豸��ע����еļ�ֵ</param>
        /// <param name="dwDeviceFlags">Power_Name</param>
        /// <param name="DeviceState">��Դ״̬ö�٣�����OpenNETCF.WindowsCE�����ռ��µ�DevicePowerState</param>
        /// <returns></returns>
        //[DllImport("coredll.dll", SetLastError = true)]
        //private static extern int SetDevicePower(string pvDevice, int dwDeviceFlags, DevicePowerState DeviceState);
        /// <summary>
        /// ��Դ״̬֪ͨ�����ڸ��¶����Ǹ�״̬��Ϣͼ�꣬����ͬ��
        /// </summary>
        /// <param name="device">�豸��ע����еļ�ֵ</param>
        /// <param name="state">��Դ״̬ö�٣�����OpenNETCF.WindowsCE�����ռ��µ�DevicePowerState</param>
        /// <param name="flags">Power_Name</param>
        /// <returns></returns>
        //[DllImport("coredll.dll")]
        //public static extern int DevicePowerNotify(string device, DevicePowerState state, int flags);
        #endregion



        #region Memory Management
        [DllImport("coredll")]
        extern public static IntPtr LocalAlloc(int flags, int size);
        [DllImport("coredll")]
        extern public static IntPtr LocalFree(IntPtr pMem);

        const int LMEM_ZEROINIT = 0x40;

        #endregion


        #region IPHLPAPI P/Invokes
        [DllImport("iphlpapi")]
        extern public static IntPtr IcmpCreateFile();

        [DllImport("iphlpapi")]
        extern public static bool IcmpCloseHandle(IntPtr h);

        [DllImport("iphlpapi")]
        extern public static uint IcmpSendEcho(
                         IntPtr IcmpHandle,
                         uint DestinationAddress,
                         byte[] RequestData,
                         short RequestSize,
                         IntPtr /*IP_OPTION_INFORMATION*/ RequestOptions,
                         byte[] ReplyBuffer,
                         int ReplySize,
                         int Timeout);

        #endregion

        [DllImport("coredll")]
        extern static int GetLastError();


        
        /// <summary>
        /// ����������ͨ��
        /// </summary>
        /// <param name="address"></param>
        /// <param name="rtt"></param>
        /// <returns></returns>
        public bool Ping(string address, out int reMsg)
        {
            bool isSuccess = false;
            byte[] RequestData = Encoding.ASCII.GetBytes(new string('\0', 64));
            ICMP_ECHO_REPLY reply = new ICMP_ECHO_REPLY(255);
            reply.DataSize = 255;
            IntPtr pData = LocalAlloc(LMEM_ZEROINIT, reply.DataSize);
            reply.Data = pData;
            IntPtr h = IcmpCreateFile();
            IPAddress ip = IPAddress.Parse(address);
            uint ipaddr = (uint)ip.Address;
            uint ret = IcmpSendEcho(h, ipaddr, RequestData, (short)RequestData.Length, IntPtr.Zero, reply._Data, reply._Data.Length, 500);
            int dwErr = 0;
            if (ret == 0)
            {
                dwErr = GetLastError();
                if (dwErr != 11010) // If error is other than timeout - display a message
                {
                    reMsg = 11010;
                    isSuccess = false;
                }
            }
            if (dwErr != 11010)
            {
                reMsg = reply.RoundTripTime;
                isSuccess = true;
            }
            else
            {
                reMsg = 500;
                isSuccess = false;
            }
            System.Threading.Thread.Sleep(500);
            IcmpCloseHandle(h);
            LocalFree(reply.Data);
            return isSuccess;
        }



        /// <summary>
        /// ��ȡwifi��Ϣ
        /// </summary>
        /// <returns></returns>
        public WirelessNetworkInterface GetWifiStatus()
        {
            try
            {
                non_wzc = null;
                foreach (INetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni is WirelessNetworkInterface)
                    {
                        non_wzc = ni as WirelessNetworkInterface;
                    }
                }
            }
            catch
            {
            }
            return non_wzc;

        }
        /// <summary>
        /// ����Wifiģ��
        /// </summary>
        public void CloseNI()
        {
            non_wzc = null;
        }



        /// <summary>
        /// ���wifi״̬
        /// </summary>
        /// <param name="msg">���ؼ����</param>
        /// <returns></returns>
        public bool isConnectWifi(string _IpAddress,out string msg)
        {
            bool result = false;
            msg = "";
            WirelessNetworkInterface wni = GetWifiStatus();
            try
            { 
                if (wni == null)
                {
                    msg = "δ����Wifi";
                }
                else if (wni.CurrentIpAddress.ToString() != _IpAddress)
                {
                    msg = "���ڻ�ȡIP��ַ " + wni.CurrentIpAddress.ToString();
                }
                else if (wni.InterfaceOperationalStatus == InterfaceOperationalStatus.Operational)
                {

                    switch (wni.SignalStrength.Strength)
                    {
                        case StrengthType.NoSignal:
                            msg = "Wifiû�ź�";
                            result = false;
                            break;
                        case StrengthType.VeryLow:
                            msg = "Wifi�źż���(" + wni.SignalStrength.Decibels + ")";
                            result = false;
                            break;
                        case StrengthType.Low:
                            msg = "Wifi�źŵ�(" + wni.SignalStrength.Decibels + ")";
                            result = true;
                            break;
                        case StrengthType.Good:
                            msg = "Wifi�źź�(" + wni.SignalStrength.Decibels + ")";
                            result = true;
                            break;
                        case StrengthType.VeryGood:
                            msg = "Wifi�źźܺ�(" + wni.SignalStrength.Decibels + ")";
                            result = true;
                            break;
                        case StrengthType.Excellent:
                            msg = "Wifi�źż���(" + wni.SignalStrength.Decibels + ")";
                            result = true;
                            break;
                    }
                    if (wifiMAC != wni.AssociatedAccessPointMAC.ToString())
                    {
                        wifiMAC = wni.AssociatedAccessPointMAC.ToString();
                        msg += " �л�WIFI";
                    }
                    msg += " MAC->" + wifiMAC;
                }
                else
                {
                    msg = "Wifiδ����";
                }
            }
            catch
            {
                CloseNI();
                //NetWorkScript.Instance.release();
                //MessageBox.Show("Wifiģ���쳣��");
                msg = "Wifiģ���쳣";
            }
            return result;

        }


        /* ����wifi���أ��𲻵�����
         
         
        //��ȡ�����豸ע����ֵ�����񲻶ԣ�
        static private void setKeyValue()
        {

            string wifiGUID = "{98C5250D-C29A-4985-AE5F-AFE5367E5006}";

            string keyName = "System\\CurrentControlSet\\Control\\POWER\\State";

            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(keyName);

            foreach (string val in registryKey.GetValueNames())
            {

                if (val.IndexOf(wifiGUID) != -1)
                {

                    keyValue = val;

                    break;

                }

            }

            registryKey.Close();

        }

        //��wifi
        static public void wifi_power_on()
        {

            try
            {

                if (keyValue != null)
                {

                    DevicePowerNotify("{98C5250D-C29A-4985-AE5F-AFE5367E5006}//" + "SDCSD30AG1", DevicePowerState.FullOn, POWER_NAME);

                    SetDevicePower("{98C5250D-C29A-4985-AE5F-AFE5367E5006}//" + "SDCSD30AG1", POWER_NAME, DevicePowerState.FullOn);

                }

            }

            catch (Exception ex)
            {

            }

        }

        //�ر�wifi
        static public void wifi_power_off()
        {

            try
            {

                if (keyValue != null)
                {

                    DevicePowerNotify("{98C5250D-C29A-4985-AE5F-AFE5367E5006}//" + "SDCSD30AG1", DevicePowerState.Off, 1);

                    SetDevicePower("{98C5250D-C29A-4985-AE5F-AFE5367E5006}//" + "SDCSD30AG1", 1, DevicePowerState.Off);

                }

            }

            catch (Exception ex)
            {

            }

        }

        */
    }
    public class ICMP_ECHO_REPLY
    {
        public ICMP_ECHO_REPLY(int size) { data = new byte[size]; }
        byte[] data;
        public byte[] _Data { get { return data; } }
        public int Address { get { return BitConverter.ToInt32(data, 0); } }
        public int Status { get { return BitConverter.ToInt32(data, 4); } }
        public int RoundTripTime { get { return BitConverter.ToInt32(data, 8); } }
        public short DataSize { get { return BitConverter.ToInt16(data, 0xc); } set { BitConverter.GetBytes(value).CopyTo(data, 0xc); } }
        public IntPtr Data { get { return new IntPtr(BitConverter.ToInt32(data, 0x10)); } set { BitConverter.GetBytes(value.ToInt32()).CopyTo(data, 0x10); } }
        public byte Ttl { get { return data[0x14]; } }
        public byte Tos { get { return data[0x15]; } }
        public byte Flags { get { return data[0x16]; } }
        public byte OptionsSize { get { return data[0x17]; } }
        public IntPtr OptionsData { get { return new IntPtr(BitConverter.ToInt32(data, 0x18)); } set { BitConverter.GetBytes(value.ToInt32()).CopyTo(data, 0x18); } }
    }
}
