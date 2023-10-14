using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wimm.Machines.Component;
using Wimm.Machines.Impl.Caucasus.PCA9685;
using Wimm.Common;

namespace Wimm.Machines.Impl.Caucasus.Component
{
    // Hiwonder LD-220MG
    // PWM周波数は50Hz(周期20ms),パルス幅0.5ms~2.5ms
    internal class CaucasusPCA9685Servo : ServoMotor
    {
        Func<double> SpeedModifierProvider { get; }
        const double MaxSpeed = 15;
        AdafruitServoDriver ServoDriver { get; }
        int PinNumber { get; }
        public CaucasusPCA9685Servo(string name, string description, double minAngle, double maxAngle, AdafruitServoDriver servoDriver, int pinNumber, Func<double> speedModifierProvider) : base(name, description, Math.Max(0, minAngle), Math.Min(180, maxAngle))
        {
            ServoDriver = servoDriver;
            SpeedModifierProvider = speedModifierProvider;
            PinNumber = pinNumber;
        }

        public override Feature<Action<double, double>> SetAngleFeature => new(
            ServoMotor.SetAngleFeatureName, "サーボの角度指定をします。", SetAngleImpl
        );
        public void SetAngleImpl(double angle, double speed)
        {
            Angle += angle;
            ApplyAngle(Angle);
        }

        public override Feature<Action<double>> RotationFeature => new(
            ServoMotor.RotationFeatureName, "サーボを回転させます。", RotateImpl
        );
        public void RotateImpl(double speed)
        {
            speed = Math.Clamp(speed, -1, 1);
            Angle += speed * SpeedModifierProvider() * MaxSpeed;
            ApplyAngle(Angle);
        }
        private void ApplyAngle(double angle)
        {
            // 0~180の角度を0.5~2.5msに線形に対応させ、20msの一周期を4096段階に区切ったうえでの段階を計算。
            ServoDriver.SetPWM(PinNumber, 0, (short)(4096 * (0.5 + 2 * (angle / 180)) / 20));
        }

        public override string ModuleName => "コーカサス サーボモーター";

        public override string ModuleDescription => "コーカサス搭載のサーボモーターデフォルト実装 for Hiwonder LD-220MG";
    }
}
