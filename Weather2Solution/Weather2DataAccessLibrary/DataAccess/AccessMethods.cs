using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Weather2DataAccessLibrary.Models;

namespace Weather2DataAccessLibrary.DataAccess
{
    public static class AccessMethods
    {
        public static int CreateSensors(string[] fileContent)
        {
            int numberOfNewSensors = 0;

            using (Weather2Context context = new Weather2Context())
            {
                // Skapar lista för att för vardera avläsning kolla om sensor finns, antar
                //att det inte går att kolla mot context.Sensors innan SaveChanges körts.
                List<Sensor> sensors = context.Sensors.ToList();

                foreach (string line in fileContent)
                {
                    string[] values = line.Split(',');
                    // [0]:Time [1]:Inne/Ute = SensorName [2]:Temp [3]:Humidity

                    if (sensors.Count() == 0 ||
                        sensors.Where(s => s.SensorName == values[1]).Count() == 0)
                    {
                        Sensor newSensor = new Sensor();
                        newSensor.SensorName = values[1];

                        sensors.Add(newSensor);
                        context.Sensors.Add(newSensor);
                    }
                }

                numberOfNewSensors = context.SaveChanges();
            }

            return numberOfNewSensors;
        }

        public static List<Sensor> GetSensorList()
        {
            List<Sensor> sensors = new List<Sensor>();

            using (Weather2Context context = new Weather2Context())
            {
                sensors = context.Sensors.ToList();
            }

            return sensors;
        }

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

        public static int LoadData(string[] fileContent)
        {
            int records = 0;

            using (Weather2Context context = new Weather2Context())
            {
                if (context.Records.Count() == 0)
                {
                    // Skapar lista med alla sensorer för att inte behöva hämta SensorId från context för alla avläsningar
                    List<Sensor> sensors = context.Sensors.ToList();

                    foreach (string line in fileContent)
                    {
                        string[] values = line.Split(',');
                        // [0]:Time [1]:Inne/Ute = SensorName [2]:Temp [3]:Humidity

                        Record record = new Record();

                        record.SensorId = sensors.FirstOrDefault(s => s.SensorName == values[1]).Id; // Kollade först mot context för varja avläsning men det tog väldigt lång tid

                        if (DateTime.TryParse(values[0], out DateTime time))
                            record.Time = time;

                        if (double.TryParse(values[2], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double temp))
                            record.Temperature = temp;

                        if (int.TryParse(values[3], out int humidity))
                            record.Humidity = humidity;


                        if (record.Time != null || record.SensorId != 0)
                        {
                            context.Records.Add(record);
                        }
                    }
                }

                records = context.SaveChanges();
            }

            return records;
        }

        public static int GetNumberOfRecordsForSensor(Sensor sensor)
        {
            int numberOfRecordsecords = 0;

            using (Weather2Context context = new Weather2Context())
            {
                numberOfRecordsecords = context.Records
                    .Where(r => r.SensorId == sensor.Id)
                    .Count();
            }

            return numberOfRecordsecords;
        }

        public static List<Record> GetRecordsForSensor(int sensorId)
        {
            List<Record> records;

            using (Weather2Context context = new Weather2Context())
            {
                //records = context.Sensors
                //    .Where(s => s.Id == sensorId)
                //    .Include(s => s.Records)
                //    .Select(s => s.Records)
                //    .FirstOrDefault()
                //    .ToList();

                records = context.Records.
                    Where(r => r.SensorId == sensorId)
                    .ToList();
            }

            return records;
        }

        public static DailyAverage GetDailyAverageForSensor(DateTime date, Sensor sensor)
        {
            DailyAverage dailyAverage = GetDailyAverageListForSensor(sensor)
                 .FirstOrDefault(d => d.Day.Date == date.Date);

            return dailyAverage;
        }

        public enum Sortingselection
        {
            Date = 1,
            Temperature,
            Humidity,
            FungusRisk
        }

        public static List<DailyAverage> SortByDailyAverageForSensor(Sensor sensor, Sortingselection sortOn)
        {
            List<DailyAverage> sortedDailyAverages = new List<DailyAverage>();

            switch (sortOn)
            {
                case Sortingselection.Date:
                    sortedDailyAverages = GetDailyAverageListForSensor(sensor)
                        .OrderBy(d => d.Day)
                        .ToList();
                    break;
                case Sortingselection.Temperature:
                    sortedDailyAverages = GetDailyAverageListForSensor(sensor)
                        .OrderByDescending(d => d.AverageTemperature)
                        .ToList();
                    break;
                case Sortingselection.Humidity:
                    sortedDailyAverages = GetDailyAverageListForSensor(sensor)
                        .OrderBy(d => d.AverageHumidity)
                        .ToList();
                    break;
                case Sortingselection.FungusRisk:
                    sortedDailyAverages = GetDailyAverageListForSensor(sensor)
                        .OrderBy(d => d.FungusRisk)
                        .ToList();
                    break;
                default:
                    break;
            }

            return sortedDailyAverages;
        }

        public static double? GetFungusRisk(double? temp, double? humidity)
        {
            double? risk = null;

            if (temp != null && humidity != null)
            {
                risk = Math.Round((double)((humidity - 78) * (temp / 15) / 0.22), 1);
            }

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

            DateTime earliestStart = new DateTime(2016, 8, 1);
            DateTime seasonStart = new DateTime();
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

            List<DailyAverage> selectedDailyAverages = GetDailyAverageListForSensor(sensor)
                .Where(d => d.Day >= earliestStart && d.AverageTemperature != null)
                .OrderBy(d => d.Day)
                .ToList();

            DailyAverage[] dailyAverageArray = new DailyAverage[selectedDailyAverages.Count()];
            int index = 0;

            foreach (DailyAverage day in selectedDailyAverages)
            {
                dailyAverageArray[index] = day;
                index++;
            }

            for (int i = 4; i < dailyAverageArray.Length; i++)
            {
                if (dailyAverageArray[i].AverageTemperature < startTemp &&   // Det finns inte data för alla dagar, räknar 5 dagar tillbaka av de som har data
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

        public static List<DailyAverage> GetDailyAverageListForSensor(Sensor sensor)
        {
            List<DailyAverage> dailyAverages = new List<DailyAverage>();

            using (Weather2Context context = new Weather2Context())
            {
                var groupedByDay = context.Records
                   .Where(r => r.SensorId == sensor.Id)
                   .AsEnumerable()
                   .GroupBy(r => r.Time.Date);

                foreach (var group in groupedByDay)
                {
                    (double? temp, double? hum, double? fungus) = GetAveragesForDayAndSensor(sensor, group.Key);

                    DailyAverage dailyAverage = new DailyAverage()
                    {
                        Day = group.Key,
                        AverageTemperature = temp,
                        AverageHumidity = hum,
                        FungusRisk = fungus,
                        //NumberOfTemperatureRecords = group.Select(r => r.Temperature).Count(),
                        //NumberOfHumidityRecords = group.Select(r => r.Humidity).Count()
                    };

                    dailyAverages.Add(dailyAverage);
                }

                    //.Select(g => new DailyAverage()
                    //{
                    //    Day = g.Key,
                    //    AverageTemperature = GetAverageTemperatureForDayAndSensor(sensor, g.Key),
                    //    // AverageTemperature = g.Average(r => r.Temperature),
                    //    AverageHumidity = g.Average(r => r.Humidity),
                    //    FungusRisk = GetFungusRisk(g.Average(r => r.Temperature), g.Average(r => r.Humidity)),
                    //    NumberOfTemperatureRecords = g.Select(r => r.Temperature).Count(),
                    //    NumberOfHumidityRecords = g.Select(r => r.Humidity).Count()
                    //})
                    //.ToList();
            }

            return dailyAverages;
        }

        public static (double?, double?, double?) GetAveragesForDayAndSensor(Sensor sensor, DateTime date)
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

            // Väljer första avläsning från respektive timme.
            // Har inte justerat för sommartid.
            // Använder dygnets värden för att hitta max och min, dvs midnatt till midnatt.

            // Medefuktighet och mögelrisk beräknas från mätvärdena från klockan 7, 13 och 19.

            int[] coeffForMonth = new int[5];

            for (int i = 0; i < 5; i++)
            {
                coeffForMonth[i] = EMCoefficients[date.Month - 1, i];
            }

            var sensorRecords = sensor.Records
                .Where(r => r.Time.Date == date.Date);


            var record07 = sensorRecords
            .FirstOrDefault(r => r.Time.Hour == 7);

            double? temp07 = record07 != null ? record07.Temperature : null;
            double? hum07 = record07 != null ? record07.Humidity : null;
            double? fungus07 = GetFungusRisk(temp07, hum07);


            var record13 = sensorRecords
                .FirstOrDefault(r => r.Time.Hour == 13);

            double? temp13 = record13 != null ? record13.Temperature : null;
            double? hum13 = record13 != null ? record13.Humidity : null;
            double? fungus13 = GetFungusRisk(temp13, hum13);


            var record19 = sensorRecords
                .FirstOrDefault(r => r.Time.Hour == 19);

            double? temp19 = record19 != null ? record19.Temperature : null;
            double? hum19 = record19 != null ? record19.Humidity : null;
            double? fungus19 = GetFungusRisk(temp19, hum19);


            double? tempMax = sensorRecords
                .Max(r => r.Temperature);

            double? tempMin = sensorRecords
                .Min(r => r.Temperature);


            double? averageTemp = ((coeffForMonth[0] * temp07) + (coeffForMonth[1] * temp13) +
                                   (coeffForMonth[2] * temp19) + (coeffForMonth[3] * tempMax) + 
                                   (coeffForMonth[4] * tempMin)) / 100;

            double? averageHumidity = (hum07 + hum13 + hum19) / 3;

            double? averageFungusRisk = (fungus07 + fungus13 + fungus19) / 3;


            return (averageTemp, averageHumidity, averageFungusRisk);
        }


        static int[,] EMCoefficients = new int[,]
        {
             // Koefficienter till Ekholm-Modéns formel för Stockholm, longitud 18
            // källa: https://www.smhi.se/kunskapsbanken/meteorologi/koefficienterna-i-ekholm-modens-formel-1.18371

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

        public static int[] GetCoefficientsForMonth(int month)
        {
            int[] coefficientsForMonth = new int[5];

            for (int i = 0; i < 5; i++)
            {
                coefficientsForMonth[i] = EMCoefficients[month - 1, i];
            }

            return coefficientsForMonth;
        }
    }
}


