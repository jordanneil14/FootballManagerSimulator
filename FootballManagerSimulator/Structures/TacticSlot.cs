using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManagerSimulator.Structures;

public class TacticSlot : ITacticSlot
{
    public TacticSlotType TacticSlotType { get; set; }
    public Player? Player { get; set; }

    public SerialisableTacticSlotModel SerialisableTacticSlot()
    {
        return new SerialisableTacticSlotModel
        {
            PlayerID = Player?.ID,
            TacticSlotType = TacticSlotType
        };
    }

    public class SerialisableTacticSlotModel
    {
        public TacticSlotType TacticSlotType { get; set; }
        public int? PlayerID { get; set; }
    }
}
