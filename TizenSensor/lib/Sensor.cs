using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tizen.Location;
using Tizen.Sensor;

using Xamarin.Forms;

namespace TizenSensor.lib
{
	/// <summary>
	/// Captures and records readings from the watch's heart rate sensor, accelerometer, and gyroscope.
	/// </summary>
	public class Sensor
	{
		public static void Create(Action<Sensor> onCreated, Action<Sensor, SensorData> onUpdated)
		{
			Permission.Check(
				isAllowed => onCreated(isAllowed ? new Sensor(onUpdated) : null),
				"http://tizen.org/privilege/healthinfo",
				"http://tizen.org/privilege/location"
			);
		}

		protected Sensor(Action<Sensor, SensorData> onUpdated)
		{
			OnUpdated = onUpdated;
		}

		public Action<Sensor, SensorData> OnUpdated { get; set; }

		public bool IsRunning { get; protected set; } = false;

		protected HeartRateMonitor heartRateMonitor = new HeartRateMonitor
		{
			PausePolicy = SensorPausePolicy.None
		};

		protected Accelerometer accelerometer = new Accelerometer
		{
			PausePolicy = SensorPausePolicy.None
		};

		protected Gyroscope gyroscope = new Gyroscope
		{
			PausePolicy = SensorPausePolicy.None
		};

		protected Locator locator;

		protected StreamWriter recordWriter;

		protected Stopwatch stopwatch = new Stopwatch();

		protected List<string> recordData = new List<string>();

		public void Start(string recordFilePath, uint updateInterval, LocationType locationType)
		{
			if (IsRunning) return;

			recordWriter = new StreamWriter(recordFilePath);
			recordWriter.WriteLine(SensorData.CsvHeader);
			heartRateMonitor.Interval = updateInterval;
			accelerometer.Interval = updateInterval;
			gyroscope.Interval = updateInterval;

			try
			{
				heartRateMonitor.Start();
				accelerometer.Start();
				gyroscope.Start();

				locator = new Locator(locationType);
				locator.Start();
			}
			catch (Exception ex)
			{
				OnUpdated.Invoke(this, new SensorData(ex.ToString()));
				Stop();
				return;
			}

			IsRunning = true;
			long lastReportTime = -updateInterval; // force immediate report
			stopwatch.Reset();
			stopwatch.Start();
			Device.StartTimer(TimeSpan.FromMilliseconds(12.5), () =>
			{
				if (stopwatch.ElapsedMilliseconds - lastReportTime < updateInterval) return IsRunning;

				lastReportTime = stopwatch.ElapsedMilliseconds;
				Update(lastReportTime);
				return IsRunning;
			});
		}

		public void Stop()
		{
			IsRunning = false;
			recordData.Clear();
			recordWriter?.Dispose();
			recordWriter = null;
			heartRateMonitor.Stop();
			accelerometer.Stop();
			gyroscope.Stop();
			locator?.Dispose();
			locator = null;
			stopwatch.Reset();
		}

		public List<string> GetData(int startIndex)
		{
			return recordData.GetRange(startIndex, recordData.Count - startIndex);
		}

		protected void Update(long elapsedMilliseconds)
		{
			if (!IsRunning) return;

			Location location = locator.GetLocation();

			var data = new SensorData
			{
				Seconds = elapsedMilliseconds * .001F,
				HeartRate = heartRateMonitor.HeartRate,
				AccelerationX = accelerometer.X,
				AccelerationY = accelerometer.Y,
				AccelerationZ = accelerometer.Z,
				AngularVelocityX = gyroscope.X,
				AngularVelocityY = gyroscope.Y,
				AngularVelocityZ = gyroscope.Z,
				Message = $"{location.Longitude:0.00000}\n{location.Latitude:0.00000}"
			};
			Console.WriteLine($"Locator.cs **sensor** location {location.Longitude:0.00000} {location.Latitude:0.00000}");

			string csv = data.ToCsvRow();
			recordData.Add(csv);
			recordWriter.WriteLine(csv);
			OnUpdated?.Invoke(this, data);
		}
	}
}
