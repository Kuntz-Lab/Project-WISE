using System;
using System.IO;
using System.Linq;

using Tizen.System;

using TizenSensor.lib;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TizenSensor
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();

			Sensor.Create(HandleSensorCreated, HandleSensorUpdated);
			Recorder.Create(HandleRecorderCreated);

			startButton.Clicked += HandleStartButtonClicked;
		}

		protected Sensor sensor;

		protected Recorder recorder;

		protected string recordDirectoryPath;

		protected void HandleSensorCreated(Sensor sensor)
		{
			if (sensor is null)
			{
				messageLabel.Text = "Permission failed!";
				return;
			}
			this.sensor = sensor;
			if (!(recorder is null)) startButton.IsEnabled = true;
		}

		protected void HandleRecorderCreated(Recorder recorder)
		{
			if (recorder is null)
			{
				messageLabel.Text = "Permission failed!";
				return;
			}
			this.recorder = recorder;
			string documentsPath = StorageManager.Storages
				.Where(x => x.StorageType == StorageArea.Internal)
				.First()
				.GetAbsolutePath(DirectoryType.Documents);
			recordDirectoryPath = Path.Combine(documentsPath, "Wearable-ML-Records");
			Directory.CreateDirectory(recordDirectoryPath);
			if (!(sensor is null)) startButton.IsEnabled = true;
		}

		protected void HandleStartButtonClicked(object sender, EventArgs e)
		{
			if (sensor.IsRunning)
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					startButton.Text = "    Start    ";
					messageLabel.Text = "Record saved";
				});
				recorder.Stop();
				sensor.Stop();
			}
			else
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					startButton.Text = "    Stop    ";
					messageLabel.Text = "Recording... 0:00";
				});
				string dateTime = Util.GetFormattedDateTime();
				recorder.Start(Path.Combine(recordDirectoryPath, $"{dateTime}-Audio.wav"));
				sensor.Start(Path.Combine(recordDirectoryPath, $"{dateTime}-Sensor.csv"), 50);
			}
		}

		protected void HandleSensorUpdated(Sensor sensor, SensorData data)
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				if (sensor.IsRunning)
				{
					messageLabel.Text = $"Recording... {Util.FormatTime((int)data.Seconds)}\n";
				}
				else
				{
					startButton.Text = "    Start    ";
					messageLabel.Text = "Record saved";
				}
			});
		}
	}
}
