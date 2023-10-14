using Wimm.Machines.Component;
using Wimm.Machines.Impl.Caucasus.Can;

using Wimm.Common;

namespace Wimm.Machines.Impl.Caucasus.Component
{
    internal class CaucasusMotor : Motor
    {
        internal enum DriverPort { M1=0,M2=1 }
        public CaucasusMotor(string name, string description, CanCommunicationUnit canMessage,DriverPort port,Func<double> speedModifierProvider) : base(name, description)
        {
            Port = port;
            if (canMessage.Data.Length != 4)
            {
                throw new ArgumentException($"与えられた<{nameof(CanCommunicationUnit)}>インスタンスのデータのサイズが4byteではありません。");
            }
            CanMessageFrame = canMessage;
            SpeedModifierProvider = speedModifierProvider;
        }
        private DriverPort Port { get; }
        private CanCommunicationUnit CanMessageFrame { get; }
        private Func<double> SpeedModifierProvider { get; }

        private void RotationImpl(double speed)
        {
            speed = Math.Clamp(speed, -1, 1)*SpeedModifierProvider();
            CanMessageFrame.Data[(int)Port * 2] = (byte)Math.Sign(speed); //回転方向決定
            CanMessageFrame.Data[(int)Port * 2 + 1] = (byte)(byte.MaxValue*Math.Abs(speed)); // 回転速度決定
        }

        public override Feature<Action<double>> RotationFeature => new Feature<Action<double>>(
            Motor.RotationFeatureName,
            "モーターを回転させます。\n\n[引数]\n- double speed - 範囲 -1 ~ 1 です。",
            RotationImpl
        );

        public override string ModuleName => "コーカサス モーター";

        public override string ModuleDescription => "コーカサス搭載のモータードライバデフォルト実装";
    }
}
