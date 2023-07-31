using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManagerSimulator.Structures;

public class PlayerDetailModel
{
    public int Row { get; set; }
    public Player Player { get; set; } = new Player();

}
