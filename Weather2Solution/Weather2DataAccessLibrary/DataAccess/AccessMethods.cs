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
                // Tillfällig lista för att för vardera rad kolla om sensor finns, antar att det inte går att kolla mot context.Sensors innan SaveChanges körts.
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
                //return sensors.Count;  // ta bort
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

        public static (int, double?) GetAverageTempForDayAndSensor(DateTime date, int sensorId)
        {
            int numberOfRecords = 0;
            double? averageTemp = null;

            using (Weather2Context context = new Weather2Context())
            {
                var selectedRecords = context.Records
                .Where(r => r.SensorId == sensorId &&
                            r.Time.Date == date.Date)
                .Select(r => r.Temperature);

                numberOfRecords = selectedRecords.Count();
                averageTemp = selectedRecords.Average();
            }

            return (numberOfRecords, averageTemp);
        }

        public static (DailyData, int, int) GetDataForSensorByDay(DateTime date, int id)
        {

            int numberOfTemperatureRecords = 0;
            int numberOfHumidityRecords = 0;
            var dailyData = new DailyData();

            using (Weather2Context context = new Weather2Context())
            {
                var records = context.Records
                              .Where(r => r.SensorId == id && r.Time.Date == date.Date);

                numberOfTemperatureRecords = records.Select(r => r.Temperature).Count();
                numberOfHumidityRecords = records.Select(r => r.Humidity).Count();

                dailyData = records.GroupBy(r => r.Time.Date)
                        .Select(g => new DailyData
                        {
                            Day = g.Key,
                            AverageTemperature = g.Average(g => g.Temperature),
                            AverageHumidity = g.Average(g => g.Humidity),
                            FungusRisk = GetFungusRisk(g.Average(r => r.Temperature), g.Average(r => r.Humidity)),
                            NumberOfTemperatureRecords = numberOfTemperatureRecords,
                            NumberOfHumidityRecords = numberOfHumidityRecords
                        })
                        .FirstOrDefault();
            }

            return (dailyData, numberOfTemperatureRecords, numberOfHumidityRecords);
        }

        public enum Sortingselection
        {
            Date = 1,
            Temperature,
            Humidity,
            FungusRisk
        }

        public static List<DailyData> SortDailyRecords(int id, Sortingselection sortOn)
        {
            List<DailyData> sortedDailyRecords = new List<DailyData>();

            using (Weather2Context context = new Weather2Context())
            {
                var dailyRecords = context.Records
                        .Where(r => r.SensorId == id)
                        .GroupBy(r => r.Time.Date)
                        .Select(g => new DailyData
                        {
                            Day = g.Key,
                            AverageTemperature = g.Average(r => r.Temperature),
                            AverageHumidity = g.Average(r => r.Humidity),
                            FungusRisk = GetFungusRisk(g.Average(r => r.Temperature), g.Average(r => r.Humidity)),
                            NumberOfTemperatureRecords = g.Select(r => r.Temperature).Count(),
                            NumberOfHumidityRecords = g.Select(r => r.Humidity).Count()
                        })
                        .ToList();

                switch (sortOn)
                {
                    case Sortingselection.Date:
                        sortedDailyRecords = dailyRecords.OrderBy(d => d.Day).ToList();
                        break;
                    case Sortingselection.Temperature:
                        sortedDailyRecords = dailyRecords.OrderByDescending(d => d.AverageTemperature).ToList(); ;
                        break;
                    case Sortingselection.Humidity:
                        sortedDailyRecords = dailyRecords.OrderBy(d => d.AverageHumidity).ToList();
                        break;
                    case Sortingselection.FungusRisk:
                        sortedDailyRecords = dailyRecords.OrderBy(d => d.FungusRisk).ToList();
                        break;
                    default:
                        break;
                }
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

        public static List<Record> SortOnTemperatureForSensor(int id)
        {
            List<Record> sortedOnDate = new List<Record>();

            using (Weather2Context context = new Weather2Context())
            {
                var sortedByDate = context.Records
                        .Where(r => r.SensorId == id)
                        .GroupBy(r => r.Time.Date)
                        .Select(g => new DailyData
                        {
                            Day = g.Key,
                            AverageTemperature = g.Average(g => g.Temperature),
                            AverageHumidity = g.Average(g => g.Humidity)
                        })
                        .OrderBy(a => a.AverageTemperature);
            }

            return sortedOnDate;
        }

        public static DateTime GetFirstAutumnDay(int id)
        {
            // Villkor för höst:
            // - Första dygnet av 5 dygn i rad med medeltemperatur under 10,0C
            // - Kan starta tidigast 1:a augusti

            int earliestStart = new DateTime(2016, 8, 1).DayOfYear;
            DateTime autumnStart = new DateTime();

            using (Weather2Context context = new Weather2Context())
            {
                var dailyData = context.Records
                        .Where(r => r.SensorId == id)
                        .GroupBy(r => r.Time.Date)
                        .Where(g => g.Key.DayOfYear >= earliestStart && g.Average(r => r.Temperature) != null)
                        .Select(g => new DailyData
                        {
                            Day = g.Key,
                            AverageTemperature = g.Average(r => r.Temperature)
                        })
                        .OrderBy(d => d.Day);

                DailyData[] dailyDataArray = new DailyData[dailyData.Count()];
                int index = 0;

                foreach (DailyData day in dailyData)
                {
                    dailyDataArray[index] = day;
                    index++;
                }

                for (int i = 4; i < dailyDataArray.Length; i++)
                {
                    if (dailyDataArray[i].AverageTemperature < 10.0 &&   // Det finns inte data för alla dagar men räknar 5 dagar tillbaka av de som har data
                        dailyDataArray[i-1].AverageTemperature < 10.0 &&
                        dailyDataArray[i-2].AverageTemperature < 10.0 &&
                        dailyDataArray[i-3].AverageTemperature < 10.0 &&
                        dailyDataArray[i-4].AverageTemperature < 10.0)
                    {
                        autumnStart = dailyDataArray[i - 4].Day;
                        break;
                    }
                }
            }

            return autumnStart;
        }
    }
}
