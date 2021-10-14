using NetworkUtil;

using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace TizenSensor.lib
{
	public static class Server
	{
		private const string HttpOkHeader = "HTTP/1.1 200 OK\r\nConnection: close\r\nContent-Type: text/html; charset=UTF-8\r\n\r\n";

		private const string HttpBadHeader = "HTTP/1.1 404 Not Found\r\nConnection: close\r\nContent-Type: text/html; charset=UTF-8\r\n\r\n";

		public static string RecordDirectoryPath { get; set; }

		public static SensorRecorder Sensor { get; set; }

		public static void Start(Action<string> onStarted)
		{
			PermissionManager.GetPermissions(
				allowed =>
				{
					if (!allowed) return;
					Networking.StartServer(HandleClientJoins, 3456);
					onStarted(Networking.GetIPAddress().ToString());
				},
				"http://tizen.org/privilege/internet",
				"http://tizen.org/privilege/network.get",
				"http://tizen.org/privilege/network.profile",
				"http://tizen.org/privilege/network.set"
			);
		}

		private static void HandleClientJoins(SocketState state)
		{
			state.OnNetworkAction = HandleClientRequests;
			Networking.GetData(state);
		}

		private static void HandleClientRequests(SocketState state)
		{
			if (state.ErrorOccurred) return;
			string[] request = state.GetData().Split(' ')[1].Substring(1).Split(':');
			string command = request[0];
			string[] args = request.Length == 2 ? request[1].Split(',') : new string[0];
			Execute(state.Socket, command, args);
		}

		private static void Execute(Socket target, string command, string[] args)
		{
			try
			{
				switch (command)
				{
					case "list":
						var files = Directory.GetFiles(RecordDirectoryPath).Select(x => Path.GetFileName(x));
						Networking.SendAndClose(target, HttpOkHeader + string.Join("\n", files));
						break;

					case "size":
						long size = new FileInfo(Path.Combine(RecordDirectoryPath, args[0])).Length;
						Networking.SendAndClose(target, HttpOkHeader + size);
						break;

					case "retrieve":
						byte[] content = File.ReadAllBytes(Path.Combine(RecordDirectoryPath, args[0]));
						Networking.SendAndClose(target, Encoding.UTF8.GetBytes(HttpOkHeader), content);
						break;

					case "delete":
						File.Delete(Path.Combine(RecordDirectoryPath, args[0]));
						Networking.SendAndClose(target, HttpOkHeader + '1');
						break;

					//case "stream":
					//	try
					//	{
					//		Networking.SendAndClose(target, HttpOkHeader + string.Join("\n", Sensor.GetData(int.Parse(args[0]))));
					//	}
					//	catch
					//	{
					//		Networking.SendAndClose(target, HttpOkHeader + "-1");
					//	}
					//	break;

					default:
						throw new ArgumentException("Unknown command: " + command);
				}

			}
			catch (Exception ex)
			{
				Networking.SendAndClose(target, HttpBadHeader + ex.Message);
			}
		}
	}
}
