using Wimm.Machines.TpipForRasberryPi;
using Wimm.Machines.Video;
using Wimm.Machines;
using Wimm.Machines.Component;
using System.Windows.Interop;
using System.Collections.Immutable;
using Wimm.Machines.Impl.Caucasus.Can;
using Wimm.Machines.Impl.Caucasus.Component;
using Wimm.Machines.TpipForRasberryPi.Import;
using System.Runtime.InteropServices;

namespace Wimm.Machines.Impl.Caucasus
{
    [LoadTarget]
    public class Caucasus : TpipForRasberryPiMachine
    {
        public override string Name => "コーカサス";

        public override Camera Camera { get; } = new Tpip4Camera(
            "カメラ1"
        );
        IEnumerable<(bool needResetToZero, CanCommunicationUnit messageFrame)> CanMessageFrames { get; }
        TPJT4.OUT_DT_STR PwmAndDigitalOutputData { get; set; }
        public Caucasus(MachineConstructorArgs args) :base(args)
        {
            if (Camera is Tpip4Camera camera){ Hwnd?.AddHook(camera.WndProc); }
            (CanMessageFrames,StructuredModules)  = CreateStructuredModule(this,()=>SpeedModifier);
            PwmAndDigitalOutputData = new TPJT4.OUT_DT_STR()
            {
                PWM = new short[4],
                PWM2 = new short[16]
            };
        }
        public Caucasus() : base()
        {
            (CanMessageFrames,StructuredModules) = CreateStructuredModule(this,()=>SpeedModifier);
        }
        private static (IEnumerable<(bool needResetToZero, CanCommunicationUnit messageFrame)>,ModuleGroup) CreateStructuredModule(Caucasus parent,Func<double> speedModifierProvider)
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
            var canFrames = new (bool needReset, CanCommunicationUnit messageFrame)[]
            {
                (true,CrawlersCanFrame),
                (true,CrawlersUpDownCanFrame)
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
                                0,180,parent.PwmAndDigitalOutputData.PWM,0,speedModifierProvider
                            ),
                            new CaucasusServo(
                                "yaw", "アーム左右サーボ",
                                0, 180, parent.PwmAndDigitalOutputData.PWM, 1, speedModifierProvider
                            ),
                            new CaucasusServo(
                                "pitch", "アーム上下サーボ",
                                0, 180, parent.PwmAndDigitalOutputData.PWM, 2, speedModifierProvider
                            ),
                            new CaucasusServo(
                                "roll", "アームひねりサーボ",
                                0, 180, parent.PwmAndDigitalOutputData.PWM, 3, speedModifierProvider
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
                foreach (var (needReset, message) in Caucasus.CanMessageFrames)
                {
                    if (needReset)
                    {
                        for (int i = 0; i < message.Data.Length; i++)
                        {
                            message.Data[i] = 0;
                        }
                    }
                }
                var ctrlData = caucasus.PwmAndDigitalOutputData;
                var _=TPJT4.NativeMethods.set_ctrl(ref ctrlData, Marshal.SizeOf<TPJT4.OUT_DT_STR>());
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