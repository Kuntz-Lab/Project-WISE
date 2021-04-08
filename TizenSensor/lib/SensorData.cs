using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TizenSensor.lib
{
	[JsonObject(MemberSerialization.OptIn)]
	public class SensorData
	{
		public static SensorData FromJson(string json)
		{
			return JsonConvert.DeserializeObject<SensorData>(json);
		}

		public SensorData()
		{
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

		public string ToJson()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}
