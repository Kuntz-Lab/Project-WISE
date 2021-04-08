using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

			addrEntry.Completed += OnAddrEntryCompleted;
			if (Application.Current.Properties.TryGetValue("lastAddr", out object lastAddr))
				addrEntry.Text = lastAddr as string;
			gestureRecognizer.Tapped += OnGestureRecognizerTapped;
			mainLayout.GestureRecognizers.Add(gestureRecognizer);

			Sensor.Create(OnSensorCreated, OnSensorUpdated);
		}

		protected TapGestureRecognizer gestureRecognizer = new TapGestureRecognizer();

		protected Sensor sensor;

		protected Client client;

		protected bool isShowingDetails = false;

		protected void OnSensorCreated(Sensor sensor)
		{
			if (sensor is null)
			{
				messageLabel.Text = "Permission failed!";
				return;
			}

			this.sensor = sensor;
			addrEntry.IsEnabled = true;
		}

		protected void OnAddrEntryCompleted(object sender, EventArgs e)
		{
			addrEntry.IsEnabled = false;
			messageLabel.Text = "Connecting...";
			Client.Connect(addrEntry.Text.Trim(), OnClientConnected, OnClientReceived, OnClientDisconnected);
		}

		protected void OnClientConnected(Client client)
		{
			if (client is null)
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					messageLabel.Text = "Connection failed!";
					addrEntry.IsEnabled = true;
				});
				return;
			}

			this.client = client;
			Device.BeginInvokeOnMainThread(() =>
			{
				titleLabel.IsVisible = false;
				addrEntry.IsVisible = false;
				messageLabel.IsVisible = false;
				instructionLabel.IsVisible = true;
			});
			// remember entered host address
			Application.Current.Properties["lastAddr"] = addrEntry.Text.Trim();
			Application.Current.SavePropertiesAsync();
		}

		protected void OnClientReceived(Client client, string data)
		{
			string[] command = data.Trim().Split();
			switch (command[0])
			{
				case "start": // start <update_interval>
					sensor.Start(uint.Parse(command[1]));
					Device.BeginInvokeOnMainThread(() =>
					{
						instructionLabel.IsVisible = false;
						dataLabel.IsVisible = true;
						dataLabel.Text = "";
					});
					break;
				case "stop":
					sensor.Stop();
					Device.BeginInvokeOnMainThread(() =>
					{
						instructionLabel.IsVisible = true;
						dataLabel.IsVisible = false;
					});
					break;
				default:
					Console.WriteLine("Sensor: unknown server command: " + data);
					break;
			}
		}

		protected void OnClientDisconnected(Client client)
		{
			this.client = null;
			sensor.Stop();
			Device.BeginInvokeOnMainThread(() =>
			{
				titleLabel.IsVisible = true;
				addrEntry.IsVisible = true;
				addrEntry.IsEnabled = true;
				messageLabel.IsVisible = true;
				messageLabel.Text = "Disconnected!";
				instructionLabel.IsVisible = false;
				dataLabel.IsVisible = false;
			});
		}

		protected void OnSensorUpdated(Sensor sensor, SensorData data)
		{
			client.Send(data.ToJson() + '\n');
			Device.BeginInvokeOnMainThread(() =>
			{
				if (isShowingDetails)
				{
					dataLabel.Text = $"Time elapsed:  {(int)data.Seconds / 60:0}:{(int)data.Seconds % 60:00}\n"
					   + $"Heart rate:  {data.HeartRate} bpm\n"
					   + $"X acceleration:  {data.AccelerationX:0.00} m/s²\n"
					   + $"Y acceleration:  {data.AccelerationY:0.00} m/s²\n"
					   + $"Z acceleration:  {data.AccelerationZ:0.00} m/s²\n"
					   + $"X angular velocity:  {data.AngularVelocityX:0} °/s\n"
					   + $"Y angular velocity:  {data.AngularVelocityY:0} °/s\n"
					   + $"Z angular velocity:  {data.AngularVelocityZ:0} °/s";
				}
				else
				{
					dataLabel.Text = "Sensor is running...\n\nTap to show/hide details";
				}
			});
		}

		protected void OnGestureRecognizerTapped(object sender, EventArgs e)
		{
			if (!dataLabel.IsVisible) return;

			isShowingDetails = !isShowingDetails;
		}
	}
}
