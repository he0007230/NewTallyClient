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
        /// Lazy单例模式
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

        #region 全局变量
        //private WirelessNetworkInterface m_wzc = null;//全局的WZC变量

        //static private string keyValue = null;    
        //private const int POWER_NAME = 0x00000001;//用于操作Wifi设备的电源命令
        #endregion

        #region 控制wifi代码中的DLL引用
        /// <summary>
        /// 获取设备电源状态
        /// </summary>
        /// <param name="pvDevice">设备在注册表中的键值</param>
        /// <param name="Flags">Power_Name</param>
        /// <param name="state">电源状态枚举，来自OpenNETCF.WindowsCE命名空间下的DevicePowerState</param>
        /// <returns></returns>
        //[DllImport("coredll.dll")]
        //protected static extern int GetDevicePower(string pvDevice, int Flags, ref DevicePowerState state);

        /// <summary>
        /// 设备电源管理，相信大家查阅MSDN后就不会陌生了
        /// </summary>
        /// <param name="pvDevice">设备在注册表中的键值</param>
        /// <param name="dwDeviceFlags">Power_Name</param>
        /// <param name="DeviceState">电源状态枚举，来自OpenNETCF.WindowsCE命名空间下的DevicePowerState</param>
        /// <returns></returns>
        //[DllImport("coredll.dll", SetLastError = true)]
        //private static extern int SetDevicePower(string pvDevice, int dwDeviceFlags, DevicePowerState DeviceState);
        /// <summary>
        /// 电源状态通知，用于更新顶部那个状态信息图标，参数同上
        /// </summary>
        /// <param name="device">设备在注册表中的键值</param>
        /// <param name="state">电源状态枚举，来自OpenNETCF.WindowsCE命名空间下的DevicePowerState</param>
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
        /// 测试网络连通性
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
        /// 读取wifi信息
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
        /// 重置Wifi模块
        /// </summary>
        public void CloseNI()
        {
            non_wzc = null;
        }



        /// <summary>
        /// 检查wifi状态
        /// </summary>
        /// <param name="msg">返回检查结果</param>
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
                    msg = "未开启Wifi";
                }
                else if (wni.CurrentIpAddress.ToString() != _IpAddress)
                {
                    msg = "正在获取IP地址 " + wni.CurrentIpAddress.ToString();
                }
                else if (wni.InterfaceOperationalStatus == InterfaceOperationalStatus.Operational)
                {

                    switch (wni.SignalStrength.Strength)
                    {
                        case StrengthType.NoSignal:
                            msg = "Wifi没信号";
                            result = false;
                            break;
                        case StrengthType.VeryLow:
                            msg = "Wifi信号极低(" + wni.SignalStrength.Decibels + ")";
                            result = false;
                            break;
                        case StrengthType.Low:
                            msg = "Wifi信号低(" + wni.SignalStrength.Decibels + ")";
                            result = true;
                            break;
                        case StrengthType.Good:
                            msg = "Wifi信号好(" + wni.SignalStrength.Decibels + ")";
                            result = true;
                            break;
                        case StrengthType.VeryGood:
                            msg = "Wifi信号很好(" + wni.SignalStrength.Decibels + ")";
                            result = true;
                            break;
                        case StrengthType.Excellent:
                            msg = "Wifi信号极好(" + wni.SignalStrength.Decibels + ")";
                            result = true;
                            break;
                    }
                    if (wifiMAC != wni.AssociatedAccessPointMAC.ToString())
                    {
                        wifiMAC = wni.AssociatedAccessPointMAC.ToString();
                        msg += " 切换WIFI";
                    }
                    msg += " MAC->" + wifiMAC;
                }
                else
                {
                    msg = "Wifi未开启";
                }
            }
            catch
            {
                CloseNI();
                //NetWorkScript.Instance.release();
                //MessageBox.Show("Wifi模块异常！");
                msg = "Wifi模块异常";
            }
            return result;

        }


        /* 控制wifi开关，起不到作用
         
         
        //获取网卡设备注册表键值，好像不对？
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

        //打开wifi
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

        //关闭wifi
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
