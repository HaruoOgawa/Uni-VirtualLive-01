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

namespace network
{
    public class UDPSocket : MonoBehaviour
    {
        [SerializeField] int Port;
        [SerializeField] string IP;

        // https://learn.microsoft.com/ja-jp/dotnet/api/system.net.sockets.udpclient?view=net-8.0
        // https://learn.microsoft.com/ja-jp/dotnet/api/system.net.ipaddress.parse?view=net-9.0
        UdpClient m_UdpClient = null;

        Task m_RecieveTask = null;
        CancellationTokenSource m_CancellationTokenSource = new CancellationTokenSource();

        // ネットワークイベント
        // event デリゲート 変数名で構築
        // Actionは.NETが標準で定義しているテンプレートを取れる便利なデリゲート
        public static event Action<ushort, ushort, ushort, byte[]> OnReceivedDMX = delegate { };

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

                // コンストラクタでIPEndPointを指定していればConnect関数の呼び出しは不要。IPEndPointは特定のアドレスとやり取りしたいときのみ使用する
                m_UdpClient = new UdpClient(IPEnd);

                m_RecieveTask = Task.Run(Receive, m_CancellationTokenSource.Token);
            }
            catch (Exception e)
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

                AnalyseData(result.Buffer);

                await Task.Delay(WaitMS);

                // キャンセルがリクエストされたら全てクリーンして終了
                if (ct.IsCancellationRequested)
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

            string NetworkProtocol = string.Empty;
            if (!Analyser.GetStringToZeroByte(ref NetworkProtocol)) return false;

            if (NetworkProtocol == "Art-Net")
            {
                if (!AnalyseArtNet(ref Analyser)) return false;
            }

            return true;
        }

        bool AnalyseArtNet(ref CBinaryReader Analyser)
        {
            // 伝送プロトコルは何か(Art-Netの中に何のデータが入っているか)
            ushort OpCode = 0;
            if (!Analyser.GetUShort(ref OpCode)) return false;

            // プロトコルバージョン
            ushort ProtocolVersion = 0;
            if (!Analyser.GetUShort(ref ProtocolVersion)) return false;

            // パケット順序制御
            byte Sequence = 0;
            if (!Analyser.GetByte(ref Sequence)) return false;

            //物理ポート番号
            byte Physical = 0;
            if (!Analyser.GetByte(ref Physical)) return false;

            // 出力先ユニバース番号(AbsoluteUniverse: ユニバース番号の絶対値)
            // ユニバースは簡単にいうとこの信号をどの機材に渡すかどうかを判別するためのラベルのようなもの
            // Art-Netのバイナリで届くときは絶対値表記されていて0 から 32767 の間の数値が入っているが
            // Art-Netの仕様としては、「Net: 0 ～ 127」「SubNet: 0 ～ 15」「Universe: 0 ～ 15」で3つの大きな単位に分かれている
            // Netの中にSubNetがあり、SubNetの中にUniverseがある
            // これらの数値を計算すると、 128 x 16 x 16 で 32768 となる → この3つを計算したのがAbsoluteUniverse
            // https://qiita.com/LUDO/items/eec489555ecf3a872197#%E3%83%A6%E3%83%8B%E3%83%90%E3%83%BC%E3%82%B9%E3%81%AB%E3%81%A4%E3%81%84%E3%81%A6
            ushort AbsoluteUniverse = 0;
            if (!Analyser.GetUShort(ref AbsoluteUniverse)) return false;

            // AbsoluteUniverseをNet・SubNet・Universeに分解する
            ushort Net = 0;
            ushort SubNet = 0;
            ushort Universe = 0;

            if (!DecomposeAbsoluteUniverse(AbsoluteUniverse, ref Net, ref SubNet, ref Universe)) return false;

            // DMXは513バイトのバイナリ
            // 1バイト目はデータサイズで512が入っている
            // 残りの512バイトがDMX本体
            // DMXは1チャンネル1バイトで0～255の間の整数をとる
            // つまりTDから浮動小数点を渡そうとすると0に丸め込まれるので必ず0から255までの整数を指定することに注意
            // ちなみにもしかすると4バイト分使ってその整数をいい感じに使えば、小数も表現できるかもしれない
            // (このテクニックはカメラ制御に使えるかも。まぁ現場では本当はそんな使い方しないんだろうけど、勉強がてらね)

            // データ長
            ushort DataLength = 0;
            if (!Analyser.GetUShortReverse(ref DataLength)) return false;

            // データ本体
            byte[] DataBuffer = new byte[DataLength];
            if (!Analyser.GetBinary(0, ref DataBuffer, DataLength)) return false;

            if (OpCode == 0x5000)
            {
                // 受信したDMXデータをアプリケーションに通知する
                OnReceivedDMX(Net, SubNet, Universe, DataBuffer);
            }

            return true;
        }

        bool DecomposeAbsoluteUniverse(ushort AbsoluteUniverse, ref ushort Net, ref ushort SubNet, ref ushort Universe)
        {
            {
                var val = (AbsoluteUniverse >> 8) & 0x7f;
                Net = BitConverter.ToUInt16(BitConverter.GetBytes(val), 0);
            }

            {
                var val = (AbsoluteUniverse >> 4) & 0x0f;
                SubNet = BitConverter.ToUInt16(BitConverter.GetBytes(val), 0);
            }

            {
                var val = (AbsoluteUniverse) & 0x0f;
                Universe = BitConverter.ToUInt16(BitConverter.GetBytes(val), 0);
            }

            return true;
        }
    }

}