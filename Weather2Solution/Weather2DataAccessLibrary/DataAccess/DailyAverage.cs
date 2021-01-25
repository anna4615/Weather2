using System;
using System.Collections.Generic;
using System.Text;

namespace Weather2DataAccessLibrary.DataAccess
{
    public class DailyAverage
    {
        public DateTime Day { get; set; }
        public double? AverageTemperature { get; set; }
        public double? AverageHumidity { get; set; }
        public double? FungusRisk { get; set; }
        //public int NumberOfTemperatureRecords { get; set; }
        //public int NumberOfHumidityRecords { get; set; }


        public override string ToString()
        {
            string printString = $"{Day.ToShortDateString()}\t";

            printString += AverageTemperature != null ?
                $"{Math.Round((double)AverageTemperature, 1)}\t\t" :
                $"*\t\t";

            //printString += NumberOfTemperatureRecords + "\t\t\t";

            printString += AverageHumidity != null ?
                $"{Math.Round((double)AverageHumidity)}\t\t" :
                $"*\t\t";

            if (FungusRisk < 1)
                printString += "Obetydlig risk\t\t";
            else if (FungusRisk == null)
                printString += $"*";
            else
                printString += $"{Math.Round((double)FungusRisk)}\t\t\t";

            //printString += NumberOfHumidityRecords;

            return printString;
        }
    }
}
