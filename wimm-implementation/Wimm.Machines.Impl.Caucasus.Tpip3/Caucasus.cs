using Wimm.Machines.Tpip3;
using Wimm.Machines.Video;
using Wimm.Machines;
using Wimm.Machines.Component;
using System.Windows.Interop;
using System.Collections.Immutable;
using Wimm.Machines.Impl.Caucasus.Tpip3.Can;
using Wimm.Machines.Impl.Caucasus.Tpip3.Component;

namespace Wimm.Machines.Impl.Caucasus.Tpip3
{
    [LoadTarget]
    public class Caucasus : Tpip3Machine
    {
        public override string Name => "コーカサス";

        public override Camera Camera { get; } = new Tpip3Camera(
            "カメラ1"
        );
        IEnumerable<(bool needResetToZero,CanCommunicationUnit messageFrame)> CanMessageFrames { get; }
        public Caucasus(MachineConstructorArgs args) : base(args)
        {
            if (Camera is Tpip3Camera camera) { Hwnd?.AddHook(camera.WndProc); }
            (CanMessageFrames, StructuredModules) = CreateStructuredModule(() => SpeedModifier);
        }
        public Caucasus() : base()
        {
            (CanMessageFrames, StructuredModules) = CreateStructuredModule(() => SpeedModifier);
        }
        private static (IEnumerable<(bool,CanCommunicationUnit)>, ModuleGroup) CreateStructuredModule(Func<double> speedModifierProvider)
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
                    DestinationAddress=(CanDestinationAddress)2,
                    SourceAddress=CanDestinationAddress.BroadCast,
                    MessageType=CanDataType.Command
                },
                4
            );
            var canFrames = new (bool needReset,CanCommunicationUnit messageFrame)[]
            {
                (true,CrawlersCanFrame),
                (true,CrawlersUpDownCanFrame)
            };

            var structuredModules = new ModuleGroup("modules",
                ImmutableArray.Create(
                    new ModuleGroup("crawlers",
                        ImmutableArray.Create<ModuleGroup>(),
                        ImmutableArray.Create<Module>(
                            new CaucasusMotor(
                                "right", "機動用右クローラー",
                                CrawlersCanFrame, CaucasusMotor.DriverPort.M1,
                                speedModifierProvider
                            ),
                            new CaucasusMotor(
                                "left", "機動用左クローラー",
                                CrawlersCanFrame, CaucasusMotor.DriverPort.M2,
                                speedModifierProvider
                            ),
                            new CaucasusMotor(
                                "updown","クローラー上下用モーター",
                                CrawlersUpDownCanFrame,CaucasusMotor.DriverPort.M1,
                                speedModifierProvider
                            )
                        )
                    ),
                    new ModuleGroup("arm",
                        ImmutableArray.Create<ModuleGroup>(),
                        ImmutableArray.Create<Module>(
                            new CaucasusMotor(
                                "root", "アーム根本モーター",
                                CrawlersUpDownCanFrame, CaucasusMotor.DriverPort.M2,
                                speedModifierProvider
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
                foreach(var (needReset,message) in Caucasus.CanMessageFrames)
                {
                    if (needReset)
                    {
                        for(int i = 0; i < message.Data.Length; i++)
                        {
                            message.Data[i] = 0;
                        }
                    }
                }
            }
            public override void Dispose()
            {
                foreach (var (_,message) in Caucasus.CanMessageFrames)
                {
                    message.Send();
                }
                base.Dispose();
            }
        }
    }
}