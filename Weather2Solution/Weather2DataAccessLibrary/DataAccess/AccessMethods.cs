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

                        if (double.TryParse(values[2], (NumberStyles)32, CultureInfo.InvariantCulture, out double temp))
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

        public static (DailyData, int) GetDataForSensorByDay(DateTime date, int id)
        {
            int numberOfRecords = 0;
            var dailyData = new DailyData();

            using (Weather2Context context = new Weather2Context())
            {
                var records = context.Records
                              .Where(r => r.SensorId == id && r.Time.Date == date.Date);

                numberOfRecords = records.Count();

                dailyData = records.GroupBy(r => r.Time.Date)
                        .Select(g => new DailyData
                        {
                            Day = g.Key,
                            AverageTemperature = g.Average(g => g.Temperature),
                            AverageHumidity = g.Average(g => g.Humidity),
                            FungusRisk = (g.Average(g => g.Humidity) - 78) * (g.Average(g => g.Temperature) / 15) / 0.22,
                            NumberOfRecords = numberOfRecords
                        })
                        .FirstOrDefault();
            }

            return (dailyData, numberOfRecords);
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
                            FungusRisk = GetFungusRisk(g.Average(g => g.Humidity), g.Average(g => g.Temperature)),
                            //FungusRisk = (g.Average(g => g.Humidity) - 78) * (g.Average(g => g.Temperature) / 15) / 0.22,
                            NumberOfRecords = g.Count()
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
                risk = risk > 0 ? risk : 0;
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
    }
}
