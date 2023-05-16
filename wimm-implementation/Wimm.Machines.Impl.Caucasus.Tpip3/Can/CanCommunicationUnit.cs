using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wimm.Machines.Tpip3.Import;

namespace Wimm.Machines.Impl.Caucasus.Tpip3.Can
{
    internal class CanCommunicationUnit
    {
        public CanCommunicationUnit(CanID id,byte dataLength)
        {
            ID = id;
            if (dataLength > 8)
            {
                throw new ArgumentOutOfRangeException($"引数<{nameof(dataLength)}>の値が8よりも大きいです。");
            }
            Data = new byte[dataLength];
        }
        public CanID ID { get; }
        public byte[] Data { get; }
        public bool Send(int targetBoardNumber=0)
        {
            TPJT3.CanMessage message = new();
            message.flg = 0;//send
            message.RTR = 0;//?
            message.sz = (byte)Data.Length;//size
            message.stat = 0;//?
            message.STD_ID = ID.Construct();
            var copiedData = new byte[8];
            Data.CopyTo(copiedData, 0);
            message.data = copiedData;
            var error = TPJT3.NativeMethods.Send_CANdata(targetBoardNumber, ref message, Data.Length);
            return error != 0;
        }
    }
}
