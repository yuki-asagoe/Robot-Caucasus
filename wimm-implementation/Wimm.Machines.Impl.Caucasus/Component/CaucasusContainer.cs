using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wimm.Machines.Impl.Caucasus.Component
{
    internal class CaucasusContainer : Module
    {
        CaucasusMotor BeltRotateMotor { get; }
        CaucasusMotor MoveContainerMotor { get; }
        public CaucasusContainer(string name, string description,CaucasusMotor beltRotateMotor,CaucasusMotor moveContainerMotor) : base(name, description)
        {
            BeltRotateMotor = beltRotateMotor;
            MoveContainerMotor = moveContainerMotor;
            Features = ImmutableArray.Create(
                new Feature<Delegate>("rotate_belt","Action<speed:double> : ベルトを回転させます。",RotateBelt),
                new Feature<Delegate>("move_container","Action<speed:double> : コンテナを出し入れします。",MoveContainer)
            );
        }
        public void RotateBelt(double speed)
        {
            BeltRotateMotor.Rotate(speed);
        }
        public void MoveContainer(double speed)
        {
            MoveContainerMotor.Rotate(speed);
        }

        public override string ModuleName => "コーカサス 救助者格納用コンテナ";

        public override string ModuleDescription => "コーカサスの救助者格納用のコンテナです";
    }
}
