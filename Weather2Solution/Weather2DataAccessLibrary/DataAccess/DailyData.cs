using System;
using System.Collections.Generic;
using System.Text;

namespace Weather2DataAccessLibrary.DataAccess
{
    public class DailyData
    {
        public DateTime Day { get; set; }
        public double? AverageTemperature { get; set; }
        public double? AverageHumidity { get; set; }
        public double? FungusRisk { get; set; }
        public int NumberOfRecords { get; set; }


        public override string ToString()
        {
            string printString = $"{Day.ToShortDateString()}\t";

            printString += AverageTemperature != null ?
                $"{Math.Round((double)AverageTemperature, 1)}\t\t" :
                $"data saknas\t";

            printString += AverageHumidity != null ?
                $"{Math.Round((double)AverageHumidity)}\t\t" :
                $"data saknas\t";

            if (FungusRisk < 0)
                printString += "Obefintlig risk\t\t";
            else if (FungusRisk != null)
                printString += $"{Math.Round((double)FungusRisk)}\t\t\t";
            else if (FungusRisk == null)
                printString += $"Kan inte beräknas\t";

            printString += NumberOfRecords;

            return printString;
        }

    }
}
