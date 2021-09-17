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
			isRecorderOnButton.Clicked += HandleIsRecorderOnButtonClicked;
			samplingRateButton.Clicked += HandleSamplingRateButtonClicked;

			if (Application.Current.Properties.TryGetValue("isRecorderOnText", out object isRecorderOnText))
				isRecorderOnButton.Text = isRecorderOnText as string;
			if (Application.Current.Properties.TryGetValue("samplingRateText", out object samplingRateText))
				samplingRateButton.Text = samplingRateText as string;

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
			Server.Sensor = sensor;
			Server.Start(HandleServerStarted);
			Device.BeginInvokeOnMainThread(() =>
			{
				startButton.IsEnabled = isRecorderOnButton.IsEnabled = samplingRateButton.IsEnabled = true;
			});
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
					isRecorderOnButton.IsEnabled = samplingRateButton.IsEnabled = true;
				});
				if (isRecorderOnButton.Text == "Mic On") recorder.Stop();
				sensor.Stop();
			}
			else
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					startButton.Text = "    Stop    ";
					messageLabel.Text = "0:00";
					isRecorderOnButton.IsEnabled = samplingRateButton.IsEnabled = false;
				});
				string dateTime = Util.GetFormattedDateTime();
				if (isRecorderOnButton.Text == "Mic On") recorder.Start(Path.Combine(recordDirectoryPath, $"{dateTime}-Audio.wav"));
				uint updateInterval = 1000 / uint.Parse(samplingRateButton.Text.Substring(0, 2));
				sensor.Start(Path.Combine(recordDirectoryPath, $"{dateTime}-Sensor.csv"), updateInterval);
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
					messageLabel.Text = Util.FormatTime((int)data.Seconds);
				}
				else
				{
					startButton.Text = "    Start    ";
					messageLabel.Text = "Record saved";
				}
			});
		}

		protected void HandleIsRecorderOnButtonClicked(object sender, EventArgs e)
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				if (isRecorderOnButton.Text == "Mic On") isRecorderOnButton.Text = "Mic Off";
				else isRecorderOnButton.Text = "Mic On";
				Application.Current.Properties["isRecorderOnText"] = isRecorderOnButton.Text;
			});
		}

		protected void HandleSamplingRateButtonClicked(object sender, EventArgs e)
		{
			Device.BeginInvokeOnMainThread(() =>
			{
				if (samplingRateButton.Text == "20 FPS") samplingRateButton.Text = "40 FPS";
				else samplingRateButton.Text = "20 FPS";
				Application.Current.Properties["samplingRateText"] = samplingRateButton.Text;
			});
		}
	}
}
