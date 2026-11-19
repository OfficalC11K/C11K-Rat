// Dosya: Models/ClientSession.cs
using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;

namespace App.Service
{
    public class ClientSession
    {
        public string Sid, Ip, Tag, UserPc, Version, Os, Av, Ram, Cam;
        public int Monitors = 1;
        public bool IsAdmin;
        public bool Encrypted;
        public bool Online = true;
        public int UptimeSec;
        public string Key = "";
        public int Rtt = -1;
        public long EchoSent;
        public Image Thumb;
        public DateTime LastSeen = DateTime.UtcNow;
        public DateTime Connected = DateTime.UtcNow;
        public TcpClient Tcp;
        public Stream Stream;
        public bool Ws;
        public readonly object WriteLock = new object();
        public volatile bool Alive = true;
    }
}