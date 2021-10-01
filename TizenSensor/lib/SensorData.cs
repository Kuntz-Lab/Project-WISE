using Newtonsoft.Json;

namespace TizenSensor.lib
{
	/// <summary>
	/// Represents one frame/update of sensor readings.
	/// </summary>
	[JsonObject(MemberSerialization.OptIn)]
	public class SensorData
	{
		public const string CsvHeader = "seconds,heartRate,accelerationX,accelerationY,accelerationZ,angularVelocityX"
			+ ",angularVelocityY,angularVelocityZ";

		public static SensorData FromJson(string json)
		{
			return JsonConvert.DeserializeObject<SensorData>(json);
		}

		public SensorData(string message = "")
		{
			Message = message;
		}

		[JsonProperty(PropertyName = "seconds")]
		public float Seconds { get; set; }

		[JsonProperty(PropertyName = "heartRate")]
		public int HeartRate { get; set; }

		[JsonProperty(PropertyName = "accelerationX")]
		public float AccelerationX { get; set; }

		[JsonProperty(PropertyName = "accelerationY")]
		public float AccelerationY { get; set; }

		[JsonProperty(PropertyName = "accelerationZ")]
		public float AccelerationZ { get; set; }

		[JsonProperty(PropertyName = "angularVelocityX")]
		public float AngularVelocityX { get; set; }

		[JsonProperty(PropertyName = "angularVelocityY")]
		public float AngularVelocityY { get; set; }

		[JsonProperty(PropertyName = "angularVelocityZ")]
		public float AngularVelocityZ { get; set; }

		public string Message { get; set; }

		public string ToJson()
		{
			return JsonConvert.SerializeObject(this);
		}

		public string ToCsvRow()
		{
			return $"{Seconds},{HeartRate},{AccelerationX},{AccelerationY},{AccelerationZ},{AngularVelocityX}"
				+ $",{AngularVelocityY},{AngularVelocityZ}";
		}
	}
}
