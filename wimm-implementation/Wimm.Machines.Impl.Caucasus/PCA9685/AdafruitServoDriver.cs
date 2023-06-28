using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wimm.Machines.TpipForRasberryPi.Import;

namespace Wimm.Machines.Impl.Caucasus.PCA9685
{
    internal abstract class AdafruitServoDriver
    {
        private bool Initialized { get; set; } = false;
        public float OscillatorFrequency { get; set; } = DefaultOscillatorFrequency;
        public byte SlaveID { get; }
        public int TpipBoardNumber { get; }
        public AdafruitServoDriver(byte id,int connectedTpipBoard)
        {
            SlaveID = id;
            TpipBoardNumber = connectedTpipBoard;
        }
        public void SetPWM(int pinNumber,short onTick,short offTick)
        {
            if (!CheckInitialized()) return;
            Write(new byte[]
            {
                (byte)((byte)(ModeRegister.LED0OnLow)+4*pinNumber),
                (byte)(onTick & 0xFF),//多分下位8bit
                (byte)(onTick >> 8),//上位8bit
                (byte)(offTick & 0xFF),
                (byte)(offTick >> 8)
            });
        }
        public bool SetPWMFrequency(float frequency)
        {
            var oldMode = Mode1Bits.Restart;

            frequency = Math.Clamp(frequency, 1, 3500);
            float prescaleval = ((OscillatorFrequency / (frequency * 4096.0f)) + 0.5f) - 1;
            byte prescale = (byte)Math.Clamp(prescaleval, 3, 255);

            Mode1Bits newMode = (oldMode & ~Mode1Bits.Restart) | Mode1Bits.Sleep;
            bool success = true;
            success&=WriteByte(ModeRegister.Mode1, newMode);
            success&=WriteByte(ModeRegister.Prescale, prescale);
            success &= WriteByte(ModeRegister.Mode1, oldMode);

            // delay(5);
            Thread.Sleep(5); // 代用。このメソッドの精度を考えると正確に5ms待機はできないだろうけど、無いよりは...

            success &= WriteByte(ModeRegister.Mode1, oldMode | Mode1Bits.Restart | Mode1Bits.AutoIncrement);
            return success;
        }
        private bool CheckInitialized() => Initialized || Initialize();
        private bool Initialize()
        {
            if (Initialized) return true;
            return Initialized = (Reset() && SetPWMFrequency(50));
        }
        private bool Reset() =>
            WriteByte(ModeRegister.Mode1, Mode1Bits.Restart);
        protected abstract bool Write(ModeRegister registerAddr,byte[] data);
        protected abstract bool WriteByte(ModeRegister registerAddr, byte data);
        protected abstract bool Write(byte[] data);
        protected bool WriteByte(ModeRegister registerAddr, Mode1Bits data) => WriteByte(registerAddr, (byte)data);
        protected abstract byte? ReadByte(ModeRegister registerAddr);
        public enum ModeRegister : byte
        {
            Mode1=0x00,
            Mode2=0x01,
            LED0OnLow=0x06,
            Prescale = 0xFE
        }
        [Flags]
        public enum Mode1Bits : byte
        {
            AllCall=0x01,
            Sub3=0x02,
            Sub2=0x04,
            Sub1=0x08,
            Sleep=0x10,
            AutoIncrement=0x20,
            ExtraClock=0x40,
            Restart=0x80
        }
        public static readonly byte DefaultSlaveID = 0x42;
        private static readonly float DefaultOscillatorFrequency = 25000000;
    }
    internal class AdafruitServoDriverByTpip : AdafruitServoDriver
    {
        public AdafruitServoDriverByTpip(byte id, int connectedTpipBoard) : base(id, connectedTpipBoard)
        {}

        protected override byte? ReadByte(ModeRegister registerAddr)
        {
            if (TPJT4.NativeMethods.Send_I2Cdata(TpipBoardNumber, new byte[] { (byte)registerAddr }, SlaveID, 1) is 0) return null;
            if (TPJT4.NativeMethods.Req_Recv_I2Cdata(TpipBoardNumber, SlaveID,1) is 0) return null;
            var buffer = new byte[32];
            int slaveID = 0, dataSize =0;
            if (TPJT4.NativeMethods.Recv_I2Cdata(TpipBoardNumber, buffer, ref slaveID, ref dataSize) is not 0 && dataSize > 0)
            {
                return buffer[0];
            }
            else return null;
        }

        protected override bool Write(ModeRegister registerAddr, byte[] data)
        {
            byte[] sentData = new byte[data.Length + 1];
            data.CopyTo((Span<byte>)sentData[1..]);
            data[0] = (byte)registerAddr;
            return TPJT4.NativeMethods.Send_I2Cdata(TpipBoardNumber, sentData, SlaveID, sentData.Length) is not 0;
        }

        protected override bool Write(byte[] data) =>
            TPJT4.NativeMethods.Send_I2Cdata(TpipBoardNumber, data, SlaveID, data.Length) is not 0;

        protected override bool WriteByte(ModeRegister registerAddr, byte data) =>
            TPJT4.NativeMethods.Send_I2Cdata(TpipBoardNumber, new byte[] { (byte)registerAddr ,data }, SlaveID, 1) is not 0;
    }
}
