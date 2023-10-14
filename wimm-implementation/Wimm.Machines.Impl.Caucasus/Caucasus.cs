using Wimm.Machines.TpipForRasberryPi;
using Wimm.Machines.Video;
using System.Collections.Immutable;
using Wimm.Machines.Impl.Caucasus.Can;
using Wimm.Machines.Impl.Caucasus.Component;
using Wimm.Machines.Impl.Caucasus.PCA9685;
using Wimm.Machines.Extension;
using Wimm.Machines.TpipForRasberryPi.Import;
using System.Runtime.InteropServices;
using Wimm.Common;

namespace Wimm.Machines.Impl.Caucasus
{
    [LoadTarget]
    public class Caucasus : TpipForRasberryPiMachine, IPowerVoltageProvidable
    {
        public override string Name => "コーカサス";

        public override Camera Camera { get; } = new Tpip4Camera(
            "フロント","バック","アーム"
        );
        IEnumerable<(Action? Resetter, CanCommunicationUnit messageFrame)> CanMessageFrames { get; }

        public double MaxVoltage => 30;

        public double MinVoltage => 0;

        public double Voltage
        {
            get
            {
                var data = new TPJT4.INP_DT_STR[1];
                TPJT4.NativeMethods.get_sens(data, Marshal.SizeOf<TPJT4.INP_DT_STR>());
                return data[0].batt / 300.0;
            }
        }

        public Caucasus(MachineConstructorArgs args) :base(args)
        {
            if (args is not null && Camera is Tpip4Camera camera){ Hwnd?.AddHook(camera.WndProc); }
            (CanMessageFrames,StructuredModules)  = CreateStructuredModule(()=>SpeedModifier);
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
            CanCommunicationUnit MiscellaneousMotorCanFrame = new(
                new()
                {
                    DestinationAddress = (CanDestinationAddress)3,
                    SourceAddress = CanDestinationAddress.BroadCast,
                    MessageType = CanDataType.Command
                },
                4
            );
            CanCommunicationUnit ArmServoCanFrame = new(
                new()
                {
                    DestinationAddress = (CanDestinationAddress)6,
                    SourceAddress = CanDestinationAddress.BroadCast,
                    MessageType = CanDataType.Command
                },
                4
            );

            var servoResetNotificator = new ServoResetNotificator();
            var canFrames = new (Action? Resetter, CanCommunicationUnit messageFrame)[]
            {
                (()=>{ Array.Fill<byte>(CrawlersCanFrame.Data,0); },CrawlersCanFrame),
                (()=>{ Array.Fill<byte>(CrawlersUpDownCanFrame.Data,0); },CrawlersUpDownCanFrame),
                (()=>{ Array.Fill<byte>(MiscellaneousMotorCanFrame.Data,0); },MiscellaneousMotorCanFrame),
                (() => {if(servoResetNotificator.ResetNeeded)servoResetNotificator.Notify(); },ArmServoCanFrame)
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
                                MiscellaneousMotorCanFrame, CaucasusMotor.DriverPort.M2,
                                speedModifierProvider
                            ),
                            new CaucasusServo(
                                "grip","アーム掴みサーボ",
                                90, 180, ArmServoCanFrame, 0, speedModifierProvider
                            ).Apply(it =>
                            {
                                servoResetNotificator.Subscribe(it);
                            }),
                            new CaucasusServo(
                                "yaw", "アーム左右サーボ",
                                45, 135, ArmServoCanFrame, 1, speedModifierProvider
                            ).Apply(it =>
                            {
                                servoResetNotificator.Subscribe(it);
                            }),
                            new CaucasusServo(
                                "pitch", "アーム上下サーボ",
                                20, 90, ArmServoCanFrame, 2, speedModifierProvider
                            ).Apply(it =>
                            {
                                servoResetNotificator.Subscribe(it);
                            }),
                            new CaucasusServo(
                                "roll", "アームひねりサーボ",
                                0, 180, ArmServoCanFrame, 3, speedModifierProvider
                            ).Apply(it =>
                            {
                                servoResetNotificator.Subscribe(it);
                            })
                        )
                    )
                ),
                ImmutableArray.Create<Module>(
                    new CaucasusContainer(
                        "container","救助者格納用コンテナ",
                        new CaucasusMotor(
                                "belt_rotater", "ベルト回転モーター",
                                CrawlersUpDownCanFrame, CaucasusMotor.DriverPort.M2,
                                speedModifierProvider
                        ),
                        new CaucasusMotor(
                                "container_mover", "コンテナ出し入れモーター",
                                MiscellaneousMotorCanFrame, CaucasusMotor.DriverPort.M1,
                                speedModifierProvider
                        )
                    ),
                    new OtherFeatureProvider(
                        "other","その他機能提供モジュール",ArmServoCanFrame
                    ).Apply(it =>
                    {
                        it.OnAngleReset += servoResetNotificator.OnReset;
                    })
                )
            );
            return (canFrames, structuredModules);
        }
        class ServoResetNotificator : IObservable<ServoResetInfo>
        {
            LinkedList<IObserver<ServoResetInfo>> Observers { get; } = new LinkedList<IObserver<ServoResetInfo>>();
            public IDisposable Subscribe(IObserver<ServoResetInfo> observer)
            {
                Observers.AddLast(observer);
                return new Unsubscriber(observer, this);
            }
            public bool ResetNeeded { get; private set; } = false;
            public void OnReset()
            {
                ResetNeeded = true;
            }
            public void Notify()
            {
                if (ResetNeeded)
                {
                    var info = new ServoResetInfo();
                    foreach (var i in Observers)
                    {
                        i.OnNext(info);
                    }
                    ResetNeeded = false;
                }
            }
            record Unsubscriber(IObserver<ServoResetInfo> Observer, ServoResetNotificator Owner) : IDisposable
            {
                public void Dispose()
                {
                    Owner.Observers.Remove(Observer);
                }
            }
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
    static class ModuleExtension
    {
        public static T Apply<T>(this T module, Action<T> initializer) where T : Module
        {
            initializer(module);
            return module;
        }
    }
}