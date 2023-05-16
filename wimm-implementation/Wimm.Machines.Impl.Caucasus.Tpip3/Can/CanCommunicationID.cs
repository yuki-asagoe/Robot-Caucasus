using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wimm.Machines.Impl.Caucasus.Tpip3.Can
{
    /// <summary>
    /// 六甲おろしモータードライバ用簡易ライブラリであるCanCommunication向けの定義です。以下を参照してください。
    /// <br/>
    /// <see href="https://github.com/RokkoOroshi/CanMotorBoard2016/tree/master/firmware/RokkoOroshiCanMotorBoard/avr/libraries/CanCommunication"/>
    /// </summary>
    internal struct CanID
    {
        public CanDataType MessageType { get; set; }
        public CanDestinationAddress SourceAddress { get; set; }
        public CanDestinationAddress DestinationAddress { get; set; }
        /// <summary>
        /// CAN通信メッセージフレームのIDとして利用できる値を生成します。
        /// </summary>
        /// <returns>
        /// 生成した値。CAN通信規格に基づき11bitで表現されます
        /// *****|***|****|****
        /// 順に 空白|メッセージタイプ|転送元ID|転送先アドレス|
        /// </returns>
        public ushort Construct()
        {
            return (ushort)((byte)MessageType << 8 | (byte)SourceAddress << 4 | (byte)DestinationAddress);
        }
    }
    internal enum CanDataType : byte
    {
        /// <summary>
        /// 非常時用
        /// </summary>
        Emergency = 0,
        /// <summary>
        /// 制御信号
        /// </summary>
        Command,
        /// <summary>
        /// 返答
        /// </summary>
        Response,
        /// <summary>
        /// 最大で 256*7-1 バイト、らしい
        /// </summary>
        Data,
        /// <summary>
        /// 心音(全ての接続されたボードは毎秒このメッセージを送信します。)
        /// とされていますがコーカサス用のファームウェアに実装予定はないです。
        /// </summary>
        HeartBeat = 7
    }
    public enum CanDestinationAddress : byte
    {
        BroadCast = 0
    }
}
