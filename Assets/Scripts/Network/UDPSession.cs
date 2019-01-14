using Network.Core;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Network.UDP
{
    public abstract class UDPSession : NetworkSession
    {
        protected UdpClient m_Socket;
        protected override void OnClose()
        {
            base.OnClose();
            if (m_Socket != null)
            {
                m_Socket.Close();
                m_Socket = null;
            }
        }
    }
    public class UDPListener : UDPSession
    {
        private Thread m_AcceptThread;
        private Action<byte[], IPEndPoint> m_DataHandler;
        public UDPListener()
        {
            m_SessionType = ESessionType.Server;
        }
        public void SetDataHandler(Action<byte[], IPEndPoint> action)
        {
            m_DataHandler = action;
        }
        protected override bool OnInit(string addr, int port)
        {
            try
            {
                m_Socket = new UdpClient(port);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("AsServer error: " + e.ToString());
            }
            return false;
        }
        protected override void OnStart()
        {
            base.OnStart();
            m_AcceptThread = CreateThread(AcceptThreadFunc);
        }
        protected override void OnClose()
        {
            if (m_AcceptThread != null)
            {
                m_AcceptThread.Join(2000);
                m_AcceptThread.Abort();
                m_AcceptThread = null;
            }
            base.OnClose();
        }
        private void AcceptThreadFunc()
        {
            IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                if (IsClosed())
                {
                    return;
                }
                try
                {
                    if (m_Socket != null && m_Socket.Available > 0)
                    {
                        byte[] data = m_Socket.Receive(ref remoteIPEndPoint);
                        if (data != null && data.Length > 0)
                        {
                            if(m_DataHandler != null)
                            {
                                m_DataHandler.Invoke(data, remoteIPEndPoint);
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("error in accept thread:" + e.ToString());
                    return;
                }
            }
        }
    }
    public class UDPUser : UDPSession
    {
        public delegate byte[] EchoHandler();

        private Thread m_SendThread;
        private EchoHandler m_EchoHandler;
        public UDPUser()
        {
            m_SessionType = ESessionType.User;
        }
        public void SetEchoHandler(EchoHandler action)
        {
            m_EchoHandler = action;
        }
        protected override bool OnInit(string addr, int port)
        {
            try
            {
                m_Socket = new UdpClient();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("AsClient error: " + e.ToString());
            }
            return false;
        }
        protected override void OnStart()
        {
            base.OnStart();
            m_SendThread = CreateThread(SendThreadFunc);
        }
        protected override void OnClose()
        {
            if (m_SendThread != null)
            {
                m_SendThread.Join(2000);
                m_SendThread.Abort();
                m_SendThread = null;
            }
            base.OnClose();
        }
        private void SendThreadFunc()
        {
            while (true)
            {
                if (IsClosed())
                {
                    return;
                }
                try
                {
                    byte[] data = null;
                    if(m_EchoHandler != null)
                    {
                        data = m_EchoHandler.Invoke();
                    }
                    if(data != null && data.Length > 0)
                    {
                        m_Socket.Send(data, data.Length, m_Addr);
                    }
                    Thread.Sleep(1000);
                }
                catch (Exception e)
                {
                    Debug.LogError("error in send thread:" + e.ToString());
                    return;
                }
            }
        }
    }
}