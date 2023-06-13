using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wimm.Machines.Component;

namespace Wimm.Machines.Impl.Caucasus.Component
{
    // Hiwonder LD-220MG
    // PWM周波数は50Hz(周期20ms),パルス幅0.5ms~2.5ms
    // Tpip4はFull PWMモードじゃないと多分このパルス幅に対応できない
    // てかなんだよRC PWMモードのパルス幅0.8ms~2.2msって
    internal class CaucasusServo : ServoMotor
    {
        Func<double> SpeedModifierProvider { get; }
        const double MaxSpeed = 15;
        short[] PwmOutput { get; }
        int Index { get; }
        public CaucasusServo(string name, string description, double minAngle, double maxAngle, short[] tpipPWMOutputArray,int index,Func<double> speedModifierProvider) : base(name, description, Math.Max(0,minAngle), Math.Min(180,maxAngle))
        {
            SpeedModifierProvider = speedModifierProvider;
            PwmOutput = tpipPWMOutputArray;
            Index = index;
        }

        public override Feature<Action<double, double>> SetAngleFeature => new (
            ServoMotor.SetAngleFeatureName, "サーボの角度指定をします。", SetAngleImpl
        );
        public void SetAngleImpl(double angle,double speed)
        {
            Angle += angle;
            ApplyAngle();
        }

        public override Feature<Action<double>> RotationFeature => new (
            ServoMotor.RotationFeatureName,"サーボを回転させます。",RotateImpl
        );
        public void RotateImpl(double speed)
        {
            speed = Math.Clamp(speed, 0, 1);
            Angle += speed * SpeedModifierProvider() * MaxSpeed;
            ApplyAngle();
        }
        private void ApplyAngle()
        {
            PwmOutput[Index] = (short)(25 + 100 * (Angle / 180)); // 0~180 map to 25~125 (パルス幅0.5ms~2.5ms)
        }
        public override string ModuleName => "コーカサス サーボモーター";

        public override string ModuleDescription => "コーカサス搭載のサーボモーターデフォルト実装 for Hiwonder LD-220MG";
    }
}
