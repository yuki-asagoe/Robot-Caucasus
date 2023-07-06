using Wimm.Machines.TpipForRasberryPi;
using Wimm.Machines.Video;
using System.Collections.Immutable;
using Wimm.Machines.Impl.Caucasus.Can;
using Wimm.Machines.Impl.Caucasus.Component;
using Wimm.Machines.Impl.Caucasus.PCA9685;

namespace Wimm.Machines.Impl.Caucasus
{
    [LoadTarget]
    public class Caucasus : TpipForRasberryPiMachine
    {
        public override string Name => "コーカサス";

        public override Camera Camera { get; } = new Tpip4Camera(
            "カメラ1"
        );
        IEnumerable<(Action? Resetter, CanCommunicationUnit messageFrame)> CanMessageFrames { get; }
        public Caucasus(MachineConstructorArgs args) :base(args)
        {
            if (Camera is Tpip4Camera camera){ Hwnd?.AddHook(camera.WndProc); }
            (CanMessageFrames,StructuredModules)  = CreateStructuredModule(()=>SpeedModifier);
        }
        public Caucasus() : base()
        {
            (CanMessageFrames,StructuredModules) = CreateStructuredModule(()=>SpeedModifier);
        }
        private static (IEnumerable<(Action? Resetter, CanCommunicationUnit messageFrame)>,ModuleGroup) CreateStructuredModule(Func<double> speedModifierProvider)
        {
            CanCommunicationUnit CrawlersCanFrame = new(
                new()
                {
                    DestinationAddress = (CanDestinationAddress)1,
                    SourceAddress = CanDestinationAddress.BroadCast,
                    MessageType = CanDataType.Command
                },
                4
            );
            CanCommunicationUnit CrawlersUpDownCanFrame = new(
                new()
                {
                    DestinationAddress = (CanDestinationAddress)2,
                    SourceAddress = CanDestinationAddress.BroadCast,
                    MessageType = CanDataType.Command
                },
                4
            );
            CanCommunicationUnit ArmServoCanFrame = new(
                new()
                {
                    DestinationAddress = (CanDestinationAddress)3,
                    SourceAddress = CanDestinationAddress.BroadCast,
                    MessageType = CanDataType.Command
                },
                4
            );
            var canFrames = new (Action? Resetter, CanCommunicationUnit messageFrame)[]
            {
                (null,CrawlersCanFrame),
                (null,CrawlersUpDownCanFrame),
                (() => {
                    if(ArmServoCanFrame.Data.All(it => it == 255))
                    {
                        Array.Fill<byte>(ArmServoCanFrame.Data,0);
                    }
                }
                ,ArmServoCanFrame)
            };
            
            var structuredModules= new ModuleGroup("modules",
                ImmutableArray.Create(
                    new ModuleGroup("crawlers",
                        ImmutableArray.Create<ModuleGroup>(),
                        ImmutableArray.Create<Module>(
                            new CaucasusMotor(
                                "right","機動用右クローラー",
                                CrawlersCanFrame,CaucasusMotor.DriverPort.M1,
                                speedModifierProvider
                            ),
                            new CaucasusMotor(
                                "left", "機動用左クローラー",
                                CrawlersCanFrame, CaucasusMotor.DriverPort.M2,
                                speedModifierProvider
                            ),
                            new CaucasusMotor(
                                "updown", "クローラー上下用モーター",
                                CrawlersUpDownCanFrame, CaucasusMotor.DriverPort.M1,
                                speedModifierProvider
                            )
                        )
                    ),
                    new ModuleGroup("arm",
                        ImmutableArray.Create<ModuleGroup>(),
                        ImmutableArray.Create<Module>(
                            new CaucasusMotor(
                                "root","アーム根本モーター",
                                CrawlersUpDownCanFrame, CaucasusMotor.DriverPort.M2,
                                speedModifierProvider
                            ),
                            new CaucasusServo(
                                "grip","アーム掴みサーボ",
                                0, 180, ArmServoCanFrame, 0, speedModifierProvider
                            ),
                            new CaucasusServo(
                                "yaw", "アーム左右サーボ",
                                0, 180, ArmServoCanFrame, 1, speedModifierProvider
                            ),
                            new CaucasusServo(
                                "pitch", "アーム上下サーボ",
                                0, 180, ArmServoCanFrame, 2, speedModifierProvider
                            ),
                            new CaucasusServo(
                                "roll", "アームひねりサーボ",
                                0, 180, ArmServoCanFrame, 3, speedModifierProvider
                            )
                        )
                    )
                ),
                ImmutableArray.Create<Module>()
            );
            return (canFrames, structuredModules);
        }
        protected override ControlProcess StartControlProcess()
        {
            return new CaucasusControlProcess(this);
        }
        class CaucasusControlProcess : ControlProcess
        {
            Caucasus Caucasus { get; }
            public CaucasusControlProcess(Caucasus caucasus)
            {
                Caucasus = caucasus;
                foreach (var (resetter, message) in Caucasus.CanMessageFrames)
                {
                    resetter?.Invoke();
                }
            }
            public override void Dispose()
            {
                foreach(var (_, message) in Caucasus.CanMessageFrames)
                {
                    message.Send();
                }
                base.Dispose();
            }
        }
    }
}