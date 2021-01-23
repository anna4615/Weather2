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
                sensor = context.Sensors.Find(id);
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

        public static List<Record> GetRecordsForSensor(int id)
        {
            List<Record> records;

            using (Weather2Context context = new Weather2Context())
            {
                records = context.Records.
                    Where(r => r.SensorId == id)
                    .ToList();
            }

            return records;
        }

        public static DailyData GetDataForSensorByDay(DateTime date, int sensorId)
        {
           DailyData dailyData = GetDailyDataListForSensor(sensorId)
                .FirstOrDefault(d => d.Day.Date == date.Date);

            return dailyData;
        }

        public enum Sortingselection
        {
            Date = 1,
            Temperature,
            Humidity,
            FungusRisk
        }

        public static List<DailyData> SortDailyRecordsForSensor(int sensorId, Sortingselection sortOn)
        {
            List<DailyData> sortedDailyRecords = new List<DailyData>();

            switch (sortOn)
            {
                case Sortingselection.Date:
                    sortedDailyRecords = GetDailyDataListForSensor(sensorId)
                        .OrderBy(d => d.Day)
                        .ToList();
                    break;
                case Sortingselection.Temperature:
                    sortedDailyRecords = GetDailyDataListForSensor(sensorId)
                        .OrderByDescending(d => d.AverageTemperature)
                        .ToList();
                    break;
                case Sortingselection.Humidity:
                    sortedDailyRecords = GetDailyDataListForSensor(sensorId)
                        .OrderBy(d => d.AverageHumidity)
                        .ToList();
                    break;
                case Sortingselection.FungusRisk:
                    sortedDailyRecords = GetDailyDataListForSensor(sensorId)
                        .OrderBy(d => d.FungusRisk)
                        .ToList();
                    break;
                default:
                    break;
            }

            return sortedDailyRecords;
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

        public static DateTime GetFirstDayOfSeason(int sensorId, string season)
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

            List<DailyData> selectedDailyData = GetDailyDataListForSensor(sensorId)
                .Where(d => d.Day >= earliestStart && d.AverageTemperature != null)
                .OrderBy(d => d.Day)
                .ToList();

            DailyData[] dailyDataArray = new DailyData[selectedDailyData.Count()];
            int index = 0;

            foreach (DailyData day in selectedDailyData)
            {
                dailyDataArray[index] = day;
                index++;
            }

            for (int i = 4; i < dailyDataArray.Length; i++)
            {
                if (dailyDataArray[i].AverageTemperature < startTemp &&   // Det finns inte data för alla dagar, räknar 5 dagar tillbaka av de som har data
                    dailyDataArray[i - 1].AverageTemperature < startTemp &&
                    dailyDataArray[i - 2].AverageTemperature < startTemp &&
                    dailyDataArray[i - 3].AverageTemperature < startTemp &&
                    dailyDataArray[i - 4].AverageTemperature < startTemp)
                {
                    seasonStart = dailyDataArray[i - 4].Day;
                    break;
                }
            }

            return seasonStart;
        }

        public static List<DailyData> GetDailyDataListForSensor(int sensorId)
        {
            List<DailyData> dailyDataList = new List<DailyData>();

            using (Weather2Context context = new Weather2Context())
            {
                dailyDataList = context.Records
                    .Where(r => r.SensorId == sensorId)
                    .GroupBy(r => r.Time.Date)
                    .Select(g => new DailyData()
                    {
                        Day = g.Key,
                        AverageTemperature = g.Average(r => r.Temperature),
                        AverageHumidity = g.Average(r => r.Humidity),
                        FungusRisk = GetFungusRisk(g.Average(r => r.Temperature), g.Average(r => r.Humidity)),
                        NumberOfTemperatureRecords = g.Select(r => r.Temperature).Count(),
                        NumberOfHumidityRecords = g.Select(r => r.Humidity).Count()
                    })
                    .ToList();
            }

            return dailyDataList;
        }
    }
}
