using FootballManagerSimulator.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManagerSimulator.Interfaces;

public interface IMatchSimulator
{
    void SimulateFirstHalf(Fixture fixture);

    void SimulateSecondHalf(Fixture fixture);
}
