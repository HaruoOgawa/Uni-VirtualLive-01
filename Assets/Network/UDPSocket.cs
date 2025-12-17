using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using binary;

public class UDPSocket : MonoBehaviour
{
    [SerializeField] int Port;
    [SerializeField] string IP;

    // https://learn.microsoft.com/ja-jp/dotnet/api/system.net.sockets.udpclient?view=net-8.0
    // https://learn.microsoft.com/ja-jp/dotnet/api/system.net.ipaddress.parse?view=net-9.0
    UdpClient m_UdpClient = null;

    Task m_RecieveTask = null;
    CancellationTokenSource m_CancellationTokenSource = new CancellationTokenSource();

    public UDPSocket()
    {
    }

    void OnDestroy()
    {
        m_CancellationTokenSource.Cancel();
    }

    void Start()
    {
        try
        {
            IPEndPoint IPEnd = new IPEndPoint(IPAddress.Any, Port);

            m_UdpClient = new UdpClient(IPEnd);

            m_RecieveTask = Task.Run(Receive, m_CancellationTokenSource.Token);
        }
        catch(Exception e)
        {
            Debug.LogException(e);
        }
    }

    async Task Receive()
    {
        CancellationToken ct = m_CancellationTokenSource.Token;

        // タスク開始前にキャンセルが飛んできてないかチェック
        ct.ThrowIfCancellationRequested();

        int WaitMS = (int)(1000.0f * 1.0f / 30.0f);

        for (;;)
        {
            var result = await m_UdpClient.ReceiveAsync();
            
            Debug.LogFormat("result.Buffer.Length: {0}", result.Buffer.Length);
            AnalyseData(result.Buffer);

            await Task.Delay(WaitMS);

            // キャンセルがリクエストされたら全てクリーンして終了
            if(ct.IsCancellationRequested)
            {
                ct.ThrowIfCancellationRequested();

                return;
            }
        }
    }

    bool AnalyseData(byte[] data)
    {
        CBinaryReader Analyser = new CBinaryReader();
        Analyser.Init(data);

        //string NetworkProtocol;

        return true;
    }
}
