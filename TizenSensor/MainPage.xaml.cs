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

			startButton.Clicked += HandleStartButtonClicked;

			Sensor.Create(HandleSensorCreated, HandleSensorUpdated);
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
			Recorder.Create(HandleRecorderCreated);
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
			Server.RecordDirectoryPath = recordDirectoryPath;
			Server.Start(HandleServerStarted);
			Device.BeginInvokeOnMainThread(() => startButton.IsEnabled = true);
		}

		protected void HandleServerStarted(string ipAddress)
		{
			Device.BeginInvokeOnMainThread(() => titleLabel.Text += '\n' + ipAddress);
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

		protected override bool OnBackButtonPressed()
		{
			if (sensor.IsRunning)
			{
				HandleStartButtonClicked(null, null);
				return true;
			}

			return false;
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
