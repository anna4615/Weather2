using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Weather2DataAccessLibrary.Models;

namespace Weather2DataAccessLibrary.DataAccess
{
    public static class NewMethods
    {
        public static Sensor GetSensor(int id)
        {
            Sensor sensor = new Sensor();

            using (Weather2Context context = new Weather2Context())
            {
                sensor = context.Sensors
                    .Where(s => s.Id == id)
                    .Include(s => s.Records)
                    .FirstOrDefault();
            }

            return sensor;
        }

        public static List<Record> GetRecordsForSensor(Sensor sensor)
        {
            var records = new List<Record>();

            using (Weather2Context context = new Weather2Context())
            {
                records = context.Records
                    .Where(r => r.SensorId == sensor.Id)
                    .ToList();
            }

            return records;
        }


        public static List<IGrouping<DateTime, Record>> GetLIstOfRecordsForSensorGroupedByDay(Sensor sensor)
        {
            var groupedRecords = new List<IGrouping<DateTime, Record>>(); ;

            using (Weather2Context context = new Weather2Context())
            {
                groupedRecords = context.Sensors
                    .Where(s => s.Id == sensor.Id)
                    .Include(s => s.Records)
                    .Select(s => s.Records)
                    .FirstOrDefault()
                    .GroupBy(r => r.Time.Date)
                    .ToList();
            }

            return groupedRecords;
        }

        public static bool DateHasRecords(DateTime date)
        {
            bool foundRecord = false;

            using (Weather2Context context = new Weather2Context())
            {

                var q = context.Records
                    .FirstOrDefault(r => r.Time.Date == date.Date);

                foundRecord = q == null ? false : true;
            }

            return foundRecord;
        }


        // ta bort?
        public static DailyAverage CreateDailyData(DateTime date, double? aveTemp, double? aveHum, double? aveFungus)
        {
            DailyAverage dailyAverage = new DailyAverage()
            {
                Day = date,
                AverageTemperature = aveTemp,
                AverageHumidity = aveHum,
                FungusRisk = aveFungus
            };

            return dailyAverage;
        }

        public static (double?, double?, double?) GetAveragesForDayAndSensor(IGrouping<DateTime, Record> records)
        {
            //Ekholm - Modéns formel:
            //Tm = (aT07 + bT13 + cT19 + dTx + eTn) / 100

            //Koefficienterna a-e är en funktioner av månad och longitud
            //Tm; är dygnets medeltemperatur.
            //T07; är temperaturen klockan 07 svensk normaltid(klockan 08 svensk sommartid).
            //T13; är temperaturen klockan 13 svensk normaltid(klockan 14 svensk sommartid).
            //T19; är temperaturen klockan 19 svensk normaltid(klockan 20 svensk sommartid).
            //Tx; är maximitemperaturen från klockan 19 föregående dygn till klockan 19 innevarande dygn(klockan 20 - 20 vid sommartid).
            //Tn; är minimitemperaturen från klockan 19 föregående dygn till klockan 19 innevarande dygn(klockan 20 - 20 vid sommartid).

            // Jag har förändrat några av kriterierna:
            // Väljer första avläsning från respektive timme.
            // Tx är maximitemperaturen för dygnet, dvs midnatt till midnatt.
            // Tn är minimitemperaturen för dygnet, dvs midnatt till midnatt.

            // Medelfuktighet och mögelrisk beräknas från samma records som används för medeltemperatur.

            int[] coeffForMonth = new int[5];

            for (int i = 0; i < 5; i++)
            {
                coeffForMonth[i] = EMCoefficients[records.Key.Month - 1, i];
            }

            (int time07, int time13, int time19) = GetTimesAdjustedForDayLightSaving(records.Key);

            double? averageTemp = null;
            double? averageHumidity = null;
            double? averageFungusRisk = null;

            //Loop som avbryter beräkningen om någon tidpunkt saknas, hoppades 
            //det skulle spara tid men det blev nog ingen större skillnad.
            while (true)
            {
                var record07 = records
                .FirstOrDefault(r => r.Time.Hour == time07);

                if (record07 == null)
                {
                    break;
                }

                var record13 = records
                    .FirstOrDefault(r => r.Time.Hour == time13);

                if (record13 == null)
                {
                    break;
                }

                var record19 = records
                    .FirstOrDefault(r => r.Time.Hour == time19);

                if (record19 == null)
                {
                    break;
                }

                double? temp07 = record07.Temperature;
                double? hum07 = record07.Humidity;
                double? fungus07 = GetFungusRisk(temp07, hum07);
                //fungus07 = fungus07 < 0 ? 0 : fungus07;  // En risk kan inte vara lägre än 0

                double? temp13 = record13 != null ? record13.Temperature : null;
                double? hum13 = record13 != null ? record13.Humidity : null;
                double? fungus13 = GetFungusRisk(temp13, hum13);
                //fungus13 = fungus13 < 0 ? 0 : fungus13;  // En risk kan inte vara lägre än 0

                double? temp19 = record19.Temperature;
                double? hum19 = record19.Humidity;
                double? fungus19 = GetFungusRisk(temp19, hum19);
                //fungus19 = fungus19 < 0 ? 0 : fungus19;  // En risk kan inte vara lägre än 0

                Record recordWithTempMax = records
                    .Where(r => r.Temperature == records.Max(r => r.Temperature))
                    .FirstOrDefault();

                double? tempMax = recordWithTempMax.Temperature;
                double? humAtTempMax = recordWithTempMax.Humidity;
                double? fungusAtTempMax = GetFungusRisk(tempMax, humAtTempMax);
                //fungusAtTempMax = fungusAtTempMax < 0 ? 0 : fungusAtTempMax;  // En risk kan inte vara lägre än 0

                Record recordWithTempMin = records
                    .Where(r => r.Temperature == records.Min(r => r.Temperature))
                    .FirstOrDefault();

                double? tempMin = recordWithTempMin.Temperature;
                double? humAtTempMin = recordWithTempMin.Humidity;
                double? fungusAtTempMin = GetFungusRisk(tempMin, humAtTempMin);
                fungusAtTempMin = fungusAtTempMin < 0 ? 0 : fungusAtTempMin;  // En risk kan inte vara lägre än 0


                averageTemp = ((coeffForMonth[0] * temp07) + (coeffForMonth[1] * temp13) +
                               (coeffForMonth[2] * temp19) + (coeffForMonth[3] * tempMax) +
                               (coeffForMonth[4] * tempMin)) / 100;

                averageHumidity = (hum07 + hum13 + hum19 + humAtTempMax + humAtTempMin) / 5;

                averageFungusRisk = (fungus07 + fungus13 + fungus19 + fungusAtTempMax + fungusAtTempMin) / 5;

                break;
            }

            return (averageTemp, averageHumidity, averageFungusRisk);
        }


        static int[,] EMCoefficients = new int[,]
        {
             // Koefficienter till Ekholm-Modéns formel för Stockholm, longitud 18
            // ref: https://www.smhi.se/kunskapsbanken/meteorologi/koefficienterna-i-ekholm-modens-formel-1.18371

           { 33, 15, 32, 10, 10 }, // januari
           { 31, 18, 31, 10, 10 }, // februari
           { 31, 21, 28, 10, 10 }, // mars
           { 23, 18, 30, 10, 19 }, // april
           { 22, 20, 23, 10, 25 }, // maj
           { 21, 19, 24, 10, 26 }, // juni
           { 19, 18, 26, 10, 27 }, // juli
           { 18, 22, 23, 10, 27 }, // augusti
           { 25, 23, 24, 10, 18 }, // september
           { 29, 19, 32, 10, 10 }, // oktober
           { 30, 16, 34, 10, 10 }, // november
           { 34, 15, 31, 10, 10 }  // december
        };


        private static (int time07, int time13, int time19) GetTimesAdjustedForDayLightSaving(DateTime date)
        {
            if (date.IsDaylightSavingTime())
            {
                return (8, 14, 20);
            }

            else
            {
                return (7, 13, 19);
            }
        }

        public static double? GetFungusRisk(double? temp, double? humidity)
        {
            double? risk = (humidity - 78) * (temp / 15) / 0.22;

            return risk;
        }

        public static DateTime GetFirstDayOfSeason(Sensor sensor, string season)
        {
            // Villkor för höst:
            // - Första dygnet av 5 dygn i rad med medeltemperatur under 10,0C
            // - Kan starta tidigast 1:a augusti

            // Villkor för vinter:
            // - Första dygnet av 5 dygn i rad med medeltemperatur under 0,0C
            // - Kan starta tidigast 1:a augusti

            // Det finns inte data för alla dagar, räknar 5 dagar tillbaka av de som har data

            DateTime earliestStart = new DateTime(2016, 8, 1);


            IEnumerable<IGrouping<DateTime, Record>> selectedRecords = GetLIstOfRecordsForSensorGroupedByDay(sensor)
                                                                       .Where(d => d.Key.Date >= earliestStart)
                                                                       .OrderBy(d => d.Key);

            List<DailyAverage> dailyAverageList = new List<DailyAverage>();

            foreach (var group in selectedRecords)
            {
                (double? aveTemp, double? aveHum, double? aveFungus) = NewMethods.GetAveragesForDayAndSensor(group);

                if (aveTemp != null)
                {
                    DailyAverage dailyAverage = new DailyAverage()
                    {
                        Day = group.Key,
                        AverageTemperature = aveTemp
                    };

                    dailyAverageList.Add(dailyAverage);
                }
            }


            double startTemp = 0.0;

            switch (season.ToLower())
            {
                case "höst":
                    startTemp = 10.0;
                    break;
                case "vinter":
                    startTemp = 0.0;
                    break;
                default:
                    break;
            }

            // Skapar array för att kunna jämföra värden på bestämda positioner
            DailyAverage[] dailyAverageArray = new DailyAverage[dailyAverageList.Count()];
            int index = 0;

            foreach (DailyAverage day in dailyAverageList)
            {
                dailyAverageArray[index] = day;
                index++;
            }

            DateTime seasonStart = new DateTime();

            for (int i = 4; i < dailyAverageArray.Length; i++)
            {
                if (dailyAverageArray[i].AverageTemperature < startTemp &&
                    dailyAverageArray[i - 1].AverageTemperature < startTemp &&
                    dailyAverageArray[i - 2].AverageTemperature < startTemp &&
                    dailyAverageArray[i - 3].AverageTemperature < startTemp &&
                    dailyAverageArray[i - 4].AverageTemperature < startTemp)
                {
                    seasonStart = dailyAverageArray[i - 4].Day;
                    break;
                }
            }

            return seasonStart;
        }


    }
}
