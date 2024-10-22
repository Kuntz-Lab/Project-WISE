﻿using NetworkUtil;

using System;
using System.Net.Sockets;

namespace TizenSensor.lib
{
	/// <summary>
	/// A CS 3500 TCP Client. Call <c>Client.Create()</c> instead of using the constructor to create a new Client
	/// instance.
	/// </summary>
	public class Client
	{
		public const int Port = 6912;

		public static void Connect(
			string serverAddr,
			Action<Client> onConnected,
			Action<Client, string> onReceived,
			Action<Client> onDisconnected
		)
		{
			Networking.ConnectToServer(
				state =>
				{
					if (state.ErrorOccurred) onConnected(null);
					else onConnected(new Client(state, onReceived, onDisconnected));
				},
				serverAddr,
				Port
			);
		}

		protected Client(SocketState state, Action<Client, string> onReceived, Action<Client> onDisconnected)
		{
			socket = state.Socket;
			OnReceived = onReceived;
			OnDisconnected = onDisconnected;

			state.OnNetworkAction = OnNetworkAction;
			Networking.GetData(state);
		}

		public Action<Client, string> OnReceived { get; set; }

		public Action<Client> OnDisconnected { get; set; }

		protected Socket socket;

		public bool Send(string data)
		{
			return Networking.Send(socket, data);
		}

		protected void OnNetworkAction(SocketState state)
		{
			if (state.ErrorOccurred)
			{
				OnDisconnected?.Invoke(this);
				return;
			}

			string data = state.GetData();
			OnReceived?.Invoke(this, data);
			state.RemoveData(0, data.Length);
			Networking.GetData(state);
		}
	}
}
