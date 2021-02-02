using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
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

                    if (sensors.Where(s => s.SensorName == values[1]).Count() == 0)
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


        public static int LoadData(string[] fileContent)
        {
            int numberOfRecords = 0;

            using (Weather2Context context = new Weather2Context())
            {
                if (context.Records.Count() == 0)
                {
                    // Skapar lista med alla sensorer för att inte behöva hämta SensorId från context för alla avläsningar
                    List<Sensor> sensors = context.Sensors.ToList();

                    foreach (string line in fileContent)
                    {
                        // [0]:Time [1]:Inne/Ute = SensorName [2]:Temp [3]:Humidity
                        string[] values = line.Split(',');

                        Record record = new Record();

                        record.SensorId = sensors.FirstOrDefault(s => s.SensorName == values[1]).Id; // Kollade först mot context för varja avläsning men det tog väldigt lång tid

                        if (DateTime.TryParse(values[0], out DateTime time))
                        {
                            record.Time = time;
                        }

                        // Hade först problem med minustecken och decimaltecken, det löstes av NumberStyles och CultureInfo
                        if (double.TryParse(values[2], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double temp))
                        {
                            record.Temperature = temp;
                        }

                        // Här skulle man kunna ha en int som håller reda på hur många värden som inte kommer med, dvs har fel format
                        //else
                        //{
                        //    numberOfFailedTemperature++;
                        //}


                        if (int.TryParse(values[3], out int humidity))
                        {
                            record.Humidity = humidity;
                        }

                        // Här skulle man kunna ha en int som håller reda på hur många värden som inte kommer med, dvs har fel format
                        //else
                        //{
                        //    numberOfFailedHumidity++;
                        //}


                        if (record.Time != null && record.SensorId != 0)  // Temperature och Humidity tillåter null men inte Time och SensorId
                        {
                            context.Records.Add(record);
                        }

                        // Här skulle man kunna ha en int som håller reda på hur många rader som inte kommer med, dvs saknar Time eller SensorId
                        //else
                        //{
                        //    numberOfFailedRecords++;
                        //}
                    }
                }

                numberOfRecords = context.SaveChanges();
            }

            return numberOfRecords;
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
    }
}
