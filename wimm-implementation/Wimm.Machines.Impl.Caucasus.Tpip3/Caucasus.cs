using Wimm.Machines.Audio;
using Wimm.Machines.Tpip3;
using Wimm.Machines.Video;
using Wimm.Machines;
using Wimm.Machines.Component;
using System.Windows.Interop;
using System.Collections.Immutable;
using Wimm.Machines.Impl.Caucasus.Tpip3.Audio;
using Wimm.Machines.Impl.Caucasus.Tpip3.Can;
using Wimm.Machines.Impl.Caucasus.Tpip3.Component;

namespace Wimm.Machines.Impl.Caucasus.Tpip3
{
    public class Caucasus : Tpip3Machine
    {
        public override string Name => "コーカサス";

        public override Speaker Speaker { get; } = new CaucasusSpeaker();

        public override Microphone Microphone => new CaucasusMicrophone();

        public override Camera Camera { get; } = new Tpip3Camera(
            "カメラ1"
        );
        IEnumerable<CanCommunicationUnit> CanMessageFrames { get; }
        public Caucasus(string tpipIpAddress, HwndSource hwnd) : base(tpipIpAddress, hwnd)
        {
            if (Camera is Tpip3Camera camera) { hwnd.AddHook(camera.WndProc); }
            (CanMessageFrames, StructuredModules) = CreateStructuredModule(() => SpeedModifier);
        }
        public Caucasus() : base()
        {
            (CanMessageFrames, StructuredModules) = CreateStructuredModule(() => SpeedModifier);
        }
        private static (IEnumerable<CanCommunicationUnit>, ModuleGroup) CreateStructuredModule(Func<double> speedModifierProvider)
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
            var canFrames = new CanCommunicationUnit[]
            {
                CrawlersCanFrame,
                CrawlersUpDownCanFrame
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
                    )
                ),
                ImmutableArray.Create<Module>()
            );
            return (canFrames, structuredModules);
        }
        public override ControlProcess StartControlProcess()
        {
            return new CaucasusControlProcess(this);
        }
        class CaucasusControlProcess : ControlProcess
        {
            Caucasus Caucasus { get; }
            public CaucasusControlProcess(Caucasus caucasus)
            {
                Caucasus = caucasus;
            }
            public override void Dispose()
            {
                foreach (var message in Caucasus.CanMessageFrames)
                {
                    message.Send();
                }
                base.Dispose();
            }
        }
    }
}