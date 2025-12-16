using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Linq;
using System.Text;

public class UDPSocket : MonoBehaviour
{
    [SerializeField] int Port;
    [SerializeField] string IP;

    // https://learn.microsoft.com/ja-jp/dotnet/api/system.net.sockets.udpclient?view=net-8.0
    // https://learn.microsoft.com/ja-jp/dotnet/api/system.net.ipaddress.parse?view=net-9.0
    UdpClient udpClient = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        udpClient = new UdpClient(Port);

        try
        {
            IPAddress address = IPAddress.Parse(IP);

            // ê⁄ë±
            udpClient.Connect(address, Port);

            IPEndPoint iPEnd = new IPEndPoint(address, Port);
            Byte[] recieveBytes = udpClient.Receive(ref iPEnd);

            string returnData = Encoding.ASCII.GetString(recieveBytes);

            Debug.LogFormat("returnData: {0}", returnData);

        }
        catch(Exception e)
        {

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
