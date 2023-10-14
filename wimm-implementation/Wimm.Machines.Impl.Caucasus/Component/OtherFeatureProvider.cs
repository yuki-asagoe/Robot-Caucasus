using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wimm.Machines.Impl.Caucasus.Can;
using Wimm.Common;

namespace Wimm.Machines.Impl.Caucasus.Component
{
    internal class OtherFeatureProvider : Module
    {
        CanCommunicationUnit ArmServoCanFrame { get; }
        public OtherFeatureProvider(string name, string description, CanCommunicationUnit armServoCanFrame) : base(name, description)
        {
            ArmServoCanFrame = armServoCanFrame;
            Features = ImmutableArray.Create<Feature<Delegate>>(
                new Feature<Delegate>("reboot_arm_servo","Action:サーボを再起動して初期角度に設定します。これの呼び出し後にサーボに角度指定を行った場合、再起動はキャンセルされます。",ResetArmServo)
            );
        }
        public event Action? OnAngleReset;

        public override string ModuleName => "コーカサス その他機能提供用モジュール";

        public override string ModuleDescription => "コーカサスが提供する雑多な機能を公開するモジュールです";

        void ResetArmServo()
        {
            Array.Fill<byte>(ArmServoCanFrame.Data, 255);
            OnAngleReset?.Invoke();
        }
    }
}
