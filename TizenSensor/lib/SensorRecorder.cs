using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tizen.Location;
using Tizen.Sensor;

using Xamarin.Forms;

namespace TizenSensor.lib
{
	/// <summary>
	/// Captures and records readings from the watch's sensors such as the heart rate sensor and accelerometer.
	/// </summary>
	public class SensorRecorder
	{
		protected enum Stat
		{
			HeartRate,
			AccelerationX,
			AccelerationY,
			AccelerationZ,
			AngularVelocityX,
			AngularVelocityY,
			AngularVelocityZ,
			Longitude,
			Latitude,
		}

		protected static List<Stat> Stats => Enum.GetValues(typeof(Stat)).Cast<Stat>().ToList();

		protected const double SensorUpdateInterval = .05;

		protected static readonly string DataHeader = "seconds," + string.Join(',', Enum.GetNames(typeof(Stat))
			.Select(name => char.ToLower(name[0]) + name.Substring(1)));

		public static void Create(Action<SensorRecorder> onCreated)
		{
			PermissionManager.GetPermissions(
				isAllowed => onCreated(isAllowed ? new SensorRecorder() : null),
				"http://tizen.org/privilege/healthinfo",
				"http://tizen.org/privilege/location"
			);
		}

		protected SensorRecorder()
		{
			heartRateMonitor.DataUpdated += HandleHeartRateMonitorDataUpdated;
			accelerometer.DataUpdated += HandleAccelerometerDataUpdated;
			gyroscope.DataUpdated += HandleGyroscopeDataUpdated;
			locator.DistanceBasedLocationChanged += HandleLocatorDistanceBasedLocationChanged;
		}

		public bool IsRunning { get; protected set; } = false;

		public double RunningTime => (DateTime.Now - recordStartTime).TotalSeconds;

		protected HeartRateMonitor heartRateMonitor = new HeartRateMonitor
		{
			Interval = (uint)(SensorUpdateInterval * 1000 / 4),
			PausePolicy = SensorPausePolicy.None
		};

		protected Accelerometer accelerometer = new Accelerometer
		{
			Interval = (uint)(SensorUpdateInterval * 1000 / 4),
			PausePolicy = SensorPausePolicy.None
		};

		protected Gyroscope gyroscope = new Gyroscope
		{
			Interval = (uint)(SensorUpdateInterval * 1000 / 4),
			PausePolicy = SensorPausePolicy.None
		};

		protected Locator locator = new Locator(LocationType.Hybrid)
		{
			Distance = 1,
			StayInterval = 1
		};

		protected DateTime recordStartTime;

		protected StreamWriter dataWriter;

		protected SensorBuffer buffer = new SensorBuffer();

		public void Start(string recordFilePath)
		{
			if (IsRunning) return;

			try
			{
				heartRateMonitor.Start();
				accelerometer.Start();
				gyroscope.Start();
				locator.Start();
			}
			catch
			{
				Stop();
				return;
			}

			recordStartTime = DateTime.Now;
			dataWriter = new StreamWriter(recordFilePath);
			dataWriter.WriteLine(DataHeader);

			IsRunning = true;

			Device.StartTimer(TimeSpan.FromSeconds(60), () =>
			{
				lock (buffer) buffer.WriteCsv(dataWriter, RunningTime - 30);
				return IsRunning;
			});
		}

		public void Stop()
		{
			IsRunning = false;

			heartRateMonitor.Stop();
			accelerometer.Stop();
			gyroscope.Stop();
			locator.Stop();
			lock (buffer) buffer.WriteCsv(dataWriter, RunningTime);
			buffer.Clear();
			dataWriter?.Dispose();
			dataWriter = null;
		}

		protected void HandleHeartRateMonitorDataUpdated(object sender, HeartRateMonitorDataUpdatedEventArgs e)
		{
			buffer.Add(Stat.HeartRate, RunningTime, e.HeartRate);
		}

		protected void HandleAccelerometerDataUpdated(object sender, AccelerometerDataUpdatedEventArgs e)
		{
			buffer.Add(Stat.AccelerationX, RunningTime, e.X);
			buffer.Add(Stat.AccelerationY, RunningTime, e.Y);
			buffer.Add(Stat.AccelerationZ, RunningTime, e.Z);
		}

		protected void HandleGyroscopeDataUpdated(object sender, GyroscopeDataUpdatedEventArgs e)
		{
			buffer.Add(Stat.AngularVelocityX, RunningTime, e.X);
			buffer.Add(Stat.AngularVelocityY, RunningTime, e.Y);
			buffer.Add(Stat.AngularVelocityZ, RunningTime, e.Z);
		}

		protected void HandleLocatorDistanceBasedLocationChanged(object sender, LocationChangedEventArgs e)
		{
			double measureTime = (e.Location.Timestamp - recordStartTime).TotalSeconds;
			buffer.Add(Stat.Longitude, measureTime, e.Location.Longitude);
			buffer.Add(Stat.Latitude, measureTime, e.Location.Latitude);
		}

		protected class SensorBuffer
		{
			public SensorBuffer()
			{
				foreach (var stat in Stats)
				{
					history[stat] = 0;
					buffer[stat] = new SortedDictionary<double, double>();
				}
			}

			protected Dictionary<Stat, double> history = new Dictionary<Stat, double>(Stats.Count);

			protected Dictionary<Stat, SortedDictionary<double, double>> buffer =
				new Dictionary<Stat, SortedDictionary<double, double>>(Stats.Count);

			protected double lastWriteTime = -SensorUpdateInterval;

			public void Add(Stat stat, double measureTime, double value)
			{
				buffer[stat][measureTime] = value;
			}

			public void Clear()
			{
				foreach (var stat in Stats)
				{
					history[stat] = 0;
					buffer[stat].Clear();
				}
				lastWriteTime = -SensorUpdateInterval;
			}

			/// <summary>Writes buffered data formatted as CSV up to <c>timeLimit</c> and clears.</summary>
			public void WriteCsv(StreamWriter writer, double timeLimit)
			{
				while (lastWriteTime < timeLimit)
				{
					lastWriteTime += SensorUpdateInterval;
					foreach (var (stat, queue) in buffer)
					{
						var (measureTime, value) = queue.FirstOrDefault();
						while (queue.Count > 0 && measureTime <= lastWriteTime)
						{
							history[stat] = value;
							queue.Remove(measureTime);
							(measureTime, value) = queue.FirstOrDefault();
						}
					}

					string csv = $"{lastWriteTime},{string.Join(',', Stats.Select(stat => history[stat]))}";
					writer.WriteLine(csv);
				}
			}
		}
	}
}
