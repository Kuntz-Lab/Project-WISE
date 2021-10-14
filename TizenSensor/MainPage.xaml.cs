using System;
using System.Diagnostics;
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

			AudioRecorder.Create(HandleAudioRecorderCreated);
		}

		protected AudioRecorder audioRecorder;

		protected SensorRecorder sensorRecorder;

		protected Stopwatch recordTimeStopwatch = new Stopwatch();

		protected string recordDirectoryPath;

		protected bool isRecording;

		protected void HandleAudioRecorderCreated(AudioRecorder audioRecorder)
		{
			this.audioRecorder = audioRecorder;

			SensorRecorder.Create(HandleSensorRecorderCreated);
		}

		protected void HandleSensorRecorderCreated(SensorRecorder sensorRecorder)
		{
			this.sensorRecorder = sensorRecorder;
			string documentsPath = StorageManager.Storages
				.Where(x => x.StorageType == StorageArea.Internal)
				.First()
				.GetAbsolutePath(DirectoryType.Documents);
			recordDirectoryPath = Path.Combine(documentsPath, "Wearable-ML-Records");
			Directory.CreateDirectory(recordDirectoryPath);

			Server.RecordDirectoryPath = recordDirectoryPath;
			Server.Sensor = sensorRecorder;
			Server.Start(HandleServerStarted);
		}

		protected void HandleServerStarted(string ipAddress)
		{
			Device.BeginInvokeOnMainThread(() => titleLabel.Text += '\n' + ipAddress);
		}

		protected void HandleStartButtonClicked(object sender, EventArgs e)
		{
			if (isRecording)
			{
				isRecording = false;
				Device.BeginInvokeOnMainThread(() =>
				{
					startButton.Text = "      Start      ";
					messageLabel.Text = "Record saved";
				});
				audioRecorder?.Stop();
				sensorRecorder?.Stop();
				recordTimeStopwatch.Reset();
			}
			else
			{
				string dateTime = Util.GetFormattedDateTime(DateTime.Now);
				if (!(sensorRecorder is null))
				{
					sensorRecorder.Start(Path.Combine(recordDirectoryPath, $"{dateTime}-Sensor.csv"));
					if (!sensorRecorder.IsRunning)
					{
						Device.BeginInvokeOnMainThread(() => messageLabel.Text = "Failed to start");
						return;
					}
				}
				audioRecorder?.Start(Path.Combine(recordDirectoryPath, $"{dateTime}-Audio.wav"));

				isRecording = true;
				recordTimeStopwatch.Start();
				Device.BeginInvokeOnMainThread(() => startButton.Text = "      Stop      ");
				Device.StartTimer(TimeSpan.FromSeconds(.25), () =>
				{
					if (!isRecording) return false;

					Device.BeginInvokeOnMainThread(() =>
					{
						messageLabel.Text = Util.FormatTime((int)recordTimeStopwatch.Elapsed.TotalSeconds);
					});
					return true;
				});
			}
		}

		protected override bool OnBackButtonPressed()
		{
			if (isRecording)
			{
				HandleStartButtonClicked(null, null);
				return true;
			}

			return false;
		}

	}
}
