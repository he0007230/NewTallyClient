using System.Collections;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Threading;

namespace SYNCC
{

    public class NetWorkScript
    {

        /// <summary>
        /// 全局唯一的连接对象实例
        /// </summary>
        private static NetWorkScript instance;

        private byte[] readBuff = new byte[1024];
        private bool isRead = false;
        public List<SocketModel> messageList = new List<SocketModel>();
        private Socket socket;
        private string serverIP;
        private int serverPort;
        private int timeoutMSec;
        private int recvTimeoutMSec;
        private static bool IsConnectionSuccessful = false;
        private static ManualResetEvent TimeoutObject = new ManualResetEvent(false);


        private List<byte> cache = new List<byte>();

        private void ReadConfig()
        {
            timeoutMSec = 8000;
            recvTimeoutMSec = 10000;
            XmlDocument xmlData = new XmlDocument();
            try
            {
                xmlData.Load("\\Program Files\\CONFIG.XML");
                serverIP = xmlData.SelectSingleNode("Root/System/server_ip").InnerText;
                serverPort = int.Parse(xmlData.SelectSingleNode("Root/System/server_port").InnerText);
            }
            catch
            {
            }
        }
        

        private NetWorkScript() 
        {
            ReadConfig();
        }

        public static NetWorkScript Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new NetWorkScript();
                }
                return instance;
            }
        }

        public void release()
        {
            if (socket != null)
            {
                socket.Close();
                socket = null;
            }
            cache.Clear();
            messageList.Clear();
        }

        public int init()
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ip = IPAddress.Parse(serverIP);
                IPEndPoint localEP = new IPEndPoint(ip, serverPort);
                //IPAddress ip2 = WifiCtrl.GetInstance().GetWifiStatus().CurrentIpAddress;
                //IPEndPoint localEP2 = new IPEndPoint(ip2, 49161);
                //socket.Bind(localEP2);
                
                //TimeoutObject.Set();
                TimeoutObject.Reset();
                IsConnectionSuccessful = false;

                socket.BeginConnect(localEP, new AsyncCallback(CallBackMethod), socket);

                if (TimeoutObject.WaitOne(timeoutMSec, false))
                {
                    if (!IsConnectionSuccessful)
                    {
                        release();
                        return -1;
                    }
                }
                else
                {
                    release();
                    return -1;
                }
                //socket.Connect(localEP);
                return 0;
            }
            catch(Exception e)
            {
                string str =e.Message.ToString();
                release();
                return -1;
            }
        }

        private static void CallBackMethod(IAsyncResult asyncresult)
        {
            try
            {
                IsConnectionSuccessful = false;
                Socket socket = asyncresult.AsyncState as Socket;

                if (socket != null)
                {
                    socket.EndConnect(asyncresult);
                    IsConnectionSuccessful = true;
                }
            }
            catch
            {
                IsConnectionSuccessful = false;
            }
            finally
            {
                TimeoutObject.Set();
            }
        }  

        public int write(int type, int area, int command, object message)
        {
            if (socket == null)
            {
                init();
            }
            ByteArray arr = new ByteArray();
            arr.write(type);
            arr.write(area);
            arr.write(command);
            if (message != null)
            {
                byte[] bs = SerializeUtil.encode(message);
                arr.write(bs);
            }
            ByteArray arr1 = new ByteArray();
            arr1.write(arr.Length);
            arr1.write(arr.getBuff());
            try
            {
                socket.Send(arr1.getBuff());
            }
            catch
            {
                //Console.WriteLine("网络错误，请重新登录" + e.Message);
                release();
                return -1;
            }
            return 0;
        }
        public void AsyncReceive()
        {
            if (socket == null)
            {
                return;
            }
            //TimeoutObject.Set();
            TimeoutObject.Reset();
            IsConnectionSuccessful = false;
            socket.BeginReceive(readBuff, 0, 1024, SocketFlags.None, ReceiveCallBack, readBuff);
            if (TimeoutObject.WaitOne(recvTimeoutMSec, false))
            {
                if (!IsConnectionSuccessful)
                {
                    release();
                }
            }
            else
            {
                release();
                TimeoutObject.Set();
            }
        }
        public void Receive()
        {
            if (socket == null)
            {
                return;
            }
            try
            {
                int readCount = socket.Receive(readBuff, 0, 1024, SocketFlags.None);
                //Console.WriteLine(readCount);
                byte[] bytes = new byte[readCount];
                //将接收缓冲池的内容复制到临时消息存储数组
                Buffer.BlockCopy(readBuff, 0, bytes, 0, readCount);
                cache.AddRange(bytes);
                if (!isRead)
                {
                    isRead = true;
                    onData();
                }
            }
            catch
            {
                socket.Close();
                //Console.WriteLine("Socket Close");
                return;
            }

        }

        private void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                //结束异步消息读取 并获取消息长度
                int readCount = socket.EndReceive(ar);
                //Console.WriteLine(readCount);
                byte[] bytes = new byte[readCount];
                //将接收缓冲池的内容复制到临时消息存储数组
                Buffer.BlockCopy(readBuff, 0, bytes, 0, readCount);
                cache.AddRange(bytes);
                if (!isRead)
                {
                    isRead = true;
                    onData();
                }
            }
            catch
            {
                //Console.WriteLine("远程服务器主动断开连接" + e.Message);
                IsConnectionSuccessful = false;
                TimeoutObject.Set();
                //socket.Close();
                return;
            }
            socket.BeginReceive(readBuff, 0, 1024, SocketFlags.None, ReceiveCallBack, readBuff);
        }

        private void onData()
        {
            //消息体长度为一个4字节数值 长度不足的时候 说明消息未接收完成 或者是废弃消息
            if (cache.Count < 4)
            {
                isRead = false;
                return;
            }

            byte[] result = ldecode(ref cache);

            if (result == null)
            {
                isRead = false;
                //Receive();
                return;
            }
            //转换为传输模型用于使用
            SocketModel model = mDecode(result);
            //将消息存储进消息列表 等待读取
            messageList.Add(model);
            IsConnectionSuccessful = true;
            TimeoutObject.Set();
            onData();
        }


        public static byte[] ldecode(ref List<byte> cache)
        {
            if (cache.Count < 4) return null;
            MemoryStream ms = new MemoryStream(cache.ToArray());
            BinaryReader br = new BinaryReader(ms);
            int length = br.ReadInt32();
            if (length > ms.Length - ms.Position)
            {
                return null;
            }

            byte[] result = br.ReadBytes(length);
            cache.Clear();
            cache.AddRange(br.ReadBytes((int)(ms.Length - ms.Position)));
            br.Close();
            ms.Close();
            return result;
        }

        public static SocketModel mDecode(byte[] value)
        {
            ByteArray ba = new ByteArray(value);
            SocketModel sm = new SocketModel();
            int type;
            int area;
            int command;
            ba.read(out type);
            ba.read(out area);
            ba.read(out command);

            sm.type = type;
            sm.area = area;
            sm.command = command;
            if (ba.Readnable)
            {
                byte[] message;
                ba.read(out message, ba.Length - ba.Position);
                sm.message = SerializeUtil.decoder(message);
            }
            ba.Close();
            return sm;
        }
    }
}