using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wimm.Machines.Component;
using Wimm.Machines.Impl.Caucasus.Can;
using Wimm.Common;

namespace Wimm.Machines.Impl.Caucasus.Component
{
    // Hiwonder LD-220MG
    // PWM周波数は50Hz(周期20ms),パルス幅0.5ms~2.5ms
    internal class CaucasusServo : ServoMotor, IObserver<ServoResetInfo>
    {
        Func<double> SpeedModifierProvider { get; }
        const double MaxSpeed = 15;
        CanCommunicationUnit CanFrame { get; }
        int Index { get; }
        public CaucasusServo(string name, string description, double minAngle, double maxAngle, CanCommunicationUnit canFrame, int index, Func<double> speedModifierProvider) : base(name, description, Math.Max(0, minAngle), Math.Min(180, maxAngle))
        {
            SpeedModifierProvider = speedModifierProvider;
            CanFrame = canFrame;
            Index = index;
        }

        public override Feature<Action<double, double>> SetAngleFeature => new(
            ServoMotor.SetAngleFeatureName, "サーボの角度指定をします。", SetAngleImpl
        );
        public void SetAngleImpl(double angle, double speed)
        {
            Angle = angle;
            ApplyAngle();
        }

        public override Feature<Action<double>> RotationFeature => new(
            ServoMotor.RotationFeatureName, "サーボを回転させます。", RotateImpl
        );
        public void RotateImpl(double speed)
        {
            speed = Math.Clamp(speed, -1, 1);
            Angle += speed * SpeedModifierProvider() * MaxSpeed;
            ApplyAngle();
        }
        private void ApplyAngle()
        {
            CanFrame.Data[Index] = (byte)Angle;
        }

        public void OnCompleted(){ }

        public void OnError(Exception error){ }

        public void OnNext(ServoResetInfo value)
        {
            SetAngleImpl(90,1);
        }

        public override string ModuleName => "コーカサス サーボモーター";

        public override string ModuleDescription => "コーカサス搭載のサーボモーターデフォルト実装 for Hiwonder LD-220MG";
    }
    internal struct ServoResetInfo { }
}