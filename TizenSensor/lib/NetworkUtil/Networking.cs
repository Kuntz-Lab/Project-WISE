/// General-Use Networking Library
/// 
/// Kevin Song (U1211977)
/// Qianlang Chen (U1172983)
/// 
/// v1.0 (H 11/07/19)

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkUtil
{
	public static class Networking
	{
		/////////////////////////////////////////////////////////////////////////////////////////
		// Server-Side Code
		/////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Starts a TcpListener on the specified port and starts an event-loop to accept new clients.
		/// The event-loop is started with BeginAcceptSocket and uses AcceptNewClient as the callback.
		/// AcceptNewClient will continue the event-loop.
		/// </summary>
		/// <param name="toCall">The method to call when a new connection is made</param>
		/// <param name="port">The port to listen on</param>
		public static TcpListener StartServer(Action<SocketState> toCall, int port)
		{
			var listener = new TcpListener(IPAddress.Any, port);

			try
			{
				listener.Start();
				listener.BeginAcceptSocket(AcceptNewClient, (listener, toCall));
			}
			catch (Exception ex)
			{
				HandleError(toCall, ex.Message);
				return null;
			}

			return listener;
		}

		/// <summary>
		/// To be used as the callback for accepting a new client that was initiated by StartServer, and 
		/// continues an event-loop to accept additional clients.
		///
		/// Uses EndAcceptSocket to finalize the connection and create a new SocketState. The SocketState's
		/// OnNetworkAction should be set to the delegate that was passed to StartServer.
		/// Then invokes the OnNetworkAction delegate with the new SocketState so the user can take action. 
		/// 
		/// If anything goes wrong during the connection process (such as the server being stopped externally), 
		/// the OnNetworkAction delegate should be invoked with a new SocketState with its ErrorOccured flag set to true 
		/// and an appropriate message placed in its ErrorMessage field. The event-loop should not continue if
		/// an error occurs.
		///
		/// If an error does not occur, after invoking OnNetworkAction with the new SocketState, an event-loop to accept 
		/// new clients should be continued by calling BeginAcceptSocket again with this method as the callback.
		/// </summary>
		/// <param name="ar">The object asynchronously passed via BeginAcceptSocket. It must contain a tuple with 
		/// 1) a delegate so the user can take action (a SocketState Action), and 2) the TcpListener</param>
		private static void AcceptNewClient(IAsyncResult ar)
		{
			var tuple = ((TcpListener, Action<SocketState>))ar.AsyncState;
			var listener = tuple.Item1;
			var toCall = tuple.Item2;

			try
			{
				var socketState = new SocketState(toCall, listener.EndAcceptSocket(ar));

				toCall(socketState);

				listener.BeginAcceptSocket(AcceptNewClient, (listener, toCall));
			}
			catch (Exception ex)
			{
				HandleError(toCall, ex.Message);
			}
		}

		/// <summary>
		/// Stops the given TcpListener.
		/// </summary>
		public static void StopServer(TcpListener listener)
		{
			listener.Stop();
		}

		/////////////////////////////////////////////////////////////////////////////////////////
		// Client-Side Code
		/////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Begins the asynchronous process of connecting to a server via BeginConnect, 
		/// and using ConnectedCallback as the method to finalize the connection once it's made.
		/// 
		/// If anything goes wrong during the connection process, toCall should be invoked 
		/// with a new SocketState with its ErrorOccured flag set to true and an appropriate message 
		/// placed in its ErrorMessage field. Between this method and ConnectedCallback, toCall should 
		/// only be invoked once on error.
		///
		/// This connection process should timeout and produce an error (as discussed above) 
		/// if a connection can't be established within 3 seconds of starting BeginConnect.
		/// 
		/// </summary>
		/// <param name="toCall">The action to take once the connection is open or an error occurs</param>
		/// <param name="hostName">The server to connect to</param>
		/// <param name="port">The port on which the server is listening</param>
		public static void ConnectToServer(Action<SocketState> toCall, string hostName, int port)
		{
			// Establish the remote endpoint for the socket.
			IPHostEntry ipHostInfo;
			var ipAddress = IPAddress.None;

			// Determine if the server address is a URL or an IP
			try
			{
				ipHostInfo = Dns.GetHostEntry(hostName);
				bool foundIPV4 = false;
				foreach (var addr in ipHostInfo.AddressList)
					if (addr.AddressFamily != AddressFamily.InterNetworkV6)
					{
						foundIPV4 = true;
						ipAddress = addr;
						break;
					}

				// Didn't find any IPV4 addresses
				if (!foundIPV4)
				{
					HandleError(toCall, "Didn't find any IPV4 addresses");
					return;
				}
			}
			catch (Exception)
			{
				// see if host name is a valid IP address
				try
				{
					ipAddress = IPAddress.Parse(hostName);
				}
				catch (Exception)
				{
					HandleError(toCall, "A host name is a invalid IP address");
					return;
				}
			}

			// Create a TCP/IP socket.
			var socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			// This disables Nagle's algorithm (Google if curious!)
			// Nagle's algorithm can cause problems for a latency-sensitive 
			// game like ours will be 
			socket.NoDelay = true;
			var socketState = new SocketState(toCall, socket);

			// Connect
			IAsyncResult ar;
			try
			{
				ar = socketState.Socket.BeginConnect(ipAddress, port, ConnectedCallback, socketState);
			}
			catch (Exception ex)
			{
				HandleError(socketState, ex.Message);
				return;
			}

			bool success = ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));
			if (!success)
			{
				socketState.ErrorMessage = "timeout";
				socket.Close(); // this closing action will trigger the error handling process in the callback.
			}
		}

		/// <summary>
		/// To be used as the callback for finalizing a connection process that was initiated by ConnectToServer.
		///
		/// Uses EndConnect to finalize the connection.
		/// 
		/// As stated in the ConnectToServer documentation, if an error occurs during the connection process,
		/// either this method or ConnectToServer (not both) should indicate the error appropriately.
		/// 
		/// If a connection is successfully established, invokes the toCall Action that was provided to ConnectToServer (above)
		/// with a new SocketState representing the new connection.
		/// 
		/// </summary>
		/// <param name="ar">The object asynchronously passed via BeginConnect</param>
		private static void ConnectedCallback(IAsyncResult ar)
		{
			var socketState = (SocketState)ar.AsyncState;

			try
			{
				socketState.Socket.EndConnect(ar);
			}
			catch (Exception ex)
			{
				string errorMessage = ex.Message;
				if (socketState.ErrorMessage == "timeout") // the socket is closed due to timeout
				{
					errorMessage = "A connection can't be established within 3 seconds of starting the network connection.";
					socketState.ErrorMessage = "";
				}

				HandleError(socketState, errorMessage);

				return;
			}

			socketState.OnNetworkAction(socketState);
		}

		/////////////////////////////////////////////////////////////////////////////////////////
		// Server and Client Common Code
		/////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Begins the asynchronous process of receiving data via BeginReceive, using ReceiveCallback 
		/// as the callback to finalize the receive and store data once it has arrived.
		/// The object passed to ReceiveCallback via the AsyncResult should be the SocketState.
		/// 
		/// If anything goes wrong during the receive process, the SocketState's ErrorOccured flag should 
		/// be set to true, and an appropriate message placed in ErrorMessage, then the SocketState's
		/// OnNetworkAction should be invoked. Between this method and ReceiveCallback, OnNetworkAction should only be 
		/// invoked once on error.
		/// 
		/// </summary>
		/// <param name="state">The SocketState to begin receiving</param>
		public static void GetData(SocketState state)
		{
			try
			{
				state.Socket.BeginReceive(state.buffer, 0, state.buffer.Length, SocketFlags.None, ReceiveCallback, state);
			}
			catch (Exception ex)
			{
				HandleError(state, ex.Message);
			}
		}

		/// <summary>
		/// To be used as the callback for finalizing a receive operation that was initiated by GetData.
		/// 
		/// Uses EndReceive to finalize the receive.
		///
		/// As stated in the GetData documentation, if an error occurs during the receive process,
		/// either this method or GetData (not both) should indicate the error appropriately.
		/// 
		/// If data is successfully received:
		///  (1) Read the characters as UTF8 and put them in the SocketState's unprocessed data buffer (its string builder).
		///      This must be done in a thread-safe manner with respect to the SocketState methods that access or modify its 
		///      string builder.
		///  (2) Call the saved delegate (OnNetworkAction) allowing the user to deal with this data.
		/// </summary>
		/// <param name="ar"> 
		/// This contains the SocketState that is stored with the callback when the initial BeginReceive is called.
		/// </param>
		private static void ReceiveCallback(IAsyncResult ar)
		{
			var socketState = (SocketState)ar.AsyncState;
			int numBytes;

			try
			{
				numBytes = socketState.Socket.EndReceive(ar);
			}
			catch (Exception ex)
			{
				HandleError(socketState, ex.Message);
				return;
			}

			if (numBytes == 0) // the remote socket is closed gracefully
			{
				HandleError(socketState, "The remote socket is closed.");
				return;
			}

			string message = Encoding.UTF8.GetString(socketState.buffer, 0, numBytes);

			socketState.data.Append(message);
			socketState.OnNetworkAction(socketState);
		}

		/// <summary>
		/// Begin the asynchronous process of sending data via BeginSend, using SendCallback to finalize the send process.
		/// 
		/// If the socket is closed, does not attempt to send.
		/// 
		/// If a send fails for any reason, this method ensures that the Socket is closed before returning.
		/// </summary>
		/// <param name="socket">The socket on which to send the data</param>
		/// <param name="data">The string to send</param>
		/// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
		public static bool Send(Socket socket, string data)
		{
			return SendInternal(socket, data, SendCallback);
		}

		/// <summary>
		/// To be used as the callback for finalizing a send operation that was initiated by Send.
		///
		/// Uses EndSend to finalize the send.
		/// 
		/// This method must not throw, even if an error occurred during the Send operation.
		/// </summary>
		/// <param name="ar">
		/// This is the Socket (not SocketState) that is stored with the callback when
		/// the initial BeginSend is called.
		/// </param>
		private static void SendCallback(IAsyncResult ar)
		{
			var socket = (Socket)ar.AsyncState;

			try
			{
				socket.EndSend(ar);
			}
			catch (Exception)
			{
				socket.Close();
			}
		}

		/// <summary>
		/// Begin the asynchronous process of sending data via BeginSend, using SendAndCloseCallback to finalize the send process.
		/// This variant closes the socket in the callback once complete. This is useful for HTTP servers.
		/// 
		/// If the socket is closed, does not attempt to send.
		/// 
		/// If a send fails for any reason, this method ensures that the Socket is closed before returning.
		/// </summary>
		/// <param name="socket">The socket on which to send the data</param>
		/// <param name="data">The string to send</param>
		/// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
		public static bool SendAndClose(Socket socket, string data)
		{
			return SendInternal(socket, data, SendAndCloseCallback);
		}

		/// <summary>
		/// To be used as the callback for finalizing a send operation that was initiated by SendAndClose.
		///
		/// Uses EndSend to finalize the send, then closes the socket.
		/// 
		/// This method must not throw, even if an error occurred during the Send operation.
		/// 
		/// This method ensures that the socket is closed before returning.
		/// </summary>
		/// <param name="ar">
		/// This is the Socket (not SocketState) that is stored with the callback when
		/// the initial BeginSend is called.
		/// </param>
		private static void SendAndCloseCallback(IAsyncResult ar)
		{
			SendCallback(ar);

			((Socket)ar.AsyncState).Close();
		}

		#region HELPER METHODS

		/// <summary>
		/// Indicates errors during sending/receiving stages accordingly.
		/// </summary>
		/// <param name="toCall">The callback delegate from the user.</param>
		/// <param name="message">The error message to report.</param>
		private static void HandleError(Action<SocketState> toCall, string message)
		{
			HandleError(new SocketState(toCall, null), message);
		}

		/// <summary>
		/// Indicates errors during sending/receiving stages accordingly, using a provided socket state.
		/// </summary>
		/// <param name="errorState">The provided socket state.</param>
		/// <param name="message">The error message to report.</param>
		private static void HandleError(SocketState errorState, string message)
		{
			errorState.ErrorOccurred = true;
			errorState.ErrorMessage = message;

			errorState.OnNetworkAction(errorState);
		}

		/// <summary>
		/// Begins the asynchronous process of sending data via BeginSend, using a custom callback to finalize the send process.
		/// If an error occurs during the sending process, the socket is closed silently.
		/// </summary>
		/// <param name="socket">The target socket to send the data.</param>
		/// <param name="data">The data to send.</param>
		/// <param name="callback"></param>
		/// <returns>The custom callback to finalize the send process.</returns>
		private static bool SendInternal(Socket socket, string data, AsyncCallback callback)
		{
			byte[] messageBytes = Encoding.UTF8.GetBytes(data);

			try
			{
				socket.BeginSend(messageBytes, 0, messageBytes.Length, SocketFlags.None, callback, socket);
			}
			catch (Exception)
			{
				socket.Close();
				return false;
			}

			return true;
		}

		#endregion
	}
}
