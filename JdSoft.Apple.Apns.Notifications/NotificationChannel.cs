using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.IO;

namespace JdSoft.Apple.Apns.Notifications
{
    /// <summary>
    /// Apns Notification Channel - NOT THREAD SAFE - Only to be used for specialized scenarios.
    /// Use the NotificationService or NotificationConnection instead.
    /// </summary>
    /// <remarks>
    /// The purpose of the NotificationChannel is to provide direct access to the Apns stream
    /// with no thread safety and no queueing notifications in memory.
    /// 
    /// If you use this class directly, the onus is on you to protect access to the class with
    /// synchronization locks or another scheme to ensure that two threads cannot call the Send
    /// method simultaneously.
    /// 
    /// One use for this class would be a messaging/service-oriented scenario where notifications
    /// are sent to a messaging endpoint to be sent.  
    /// 
    /// If using the NotificationService or NotificationConnection classes, if the application 
    /// failed between when the notification was enqueued and when the notification was sent, 
    /// that notification would be lost.
    /// 
    /// In this messaging scenario, these notifications would instead be queued in MSMQ or something
    /// similar, and when taken off that queue, would need to be sent immediately, to ensure that
    /// no notifications are lost in an in-memory queue.
    /// </remarks>
	public class NotificationChannel : IDisposable
	{
		#region Constants
		private const string hostSandbox = "gateway.sandbox.push.apple.com";
		private const string hostProduction = "gateway.push.apple.com";
		#endregion

		#region Delegates and Events
		/// <summary>
		/// Handles General Exceptions
		/// </summary>
		/// <param name="sender">NotificationChannel Instance that generated the Exception</param>
		/// <param name="ex">Exception Instance</param>
		public delegate void OnError(object sender, Exception ex);
		/// <summary>
		/// Occurs when a General Error is thrown
		/// </summary>
		public event OnError Error;

		/// <summary>
		/// Handles Connecting Event
		/// </summary>
		/// <param name="sender">NotificationChannel Instance</param>
		public delegate void OnConnecting(object sender);
		/// <summary>
		/// Occurs when Connecting to Apple's servers
		/// </summary>
		public event OnConnecting Connecting;

		/// <summary>
		/// Handles Connected Event
		/// </summary>
		/// <param name="sender">NotificationChannel Instance</param>
		public delegate void OnConnected(object sender);
		/// <summary>
		/// Occurs when successfully connected and authenticated via SSL to Apple's Servers
		/// </summary>
		public event OnConnected Connected;

		/// <summary>
		/// Handles Disconnected Event
		/// </summary>
		/// <param name="sender">NotificationChannel Instance</param>
		public delegate void OnDisconnected(object sender);
		/// <summary>
		/// Occurs when the connection to Apple's Servers has been lost
		/// </summary>
		public event OnDisconnected Disconnected;
		#endregion

		#region Instance Variables
		private bool connected;
		private bool firstConnect;

		private X509Certificate certificate;
		private X509CertificateCollection certificates;
		private TcpClient apnsClient;
		private SslStream apnsStream;
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="host">Push Notification Gateway Host</param>
		/// <param name="port">Push Notification Gateway Port</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		public NotificationChannel(string host, int port, string p12File)
			: this(host, port, p12File, null)
		{
		}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host">Push Notification Gateway Host</param>
        /// <param name="port">Push Notification Gateway Port</param>
        /// <param name="p12FileBytes">Byte array representation of PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
        public NotificationChannel(string host, int port, byte[] p12FileBytes)
            : this(host, port, p12FileBytes, null)
        {
        }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		public NotificationChannel(bool sandbox, string p12File)
			: this(sandbox ? hostSandbox : hostProduction, 2195, p12File, null)
		{
		}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
        /// <param name="p12FileBytes">Byte array representation of PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
        public NotificationChannel(bool sandbox, byte[] p12FileBytes)
            : this(sandbox ? hostSandbox : hostProduction, 2195, p12FileBytes, null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
        /// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
        /// <param name="p12FilePassword">Password protecting the p12File</param>
        public NotificationChannel(bool sandbox, string p12File, string p12FilePassword)
            : this(sandbox ? hostSandbox : hostProduction, 2195, p12File, p12FilePassword)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
        /// <param name="p12FileBytes">Byte array representation of PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
        /// <param name="p12FilePassword">Password protecting the p12File</param>
        public NotificationChannel(bool sandbox, byte[] p12FileBytes, string p12FilePassword)
            : this(sandbox ? hostSandbox : hostProduction, 2195, p12FileBytes, p12FilePassword)
        {
        }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="host">Push Notification Gateway Host</param>
		/// <param name="port">Push Notification Gateway Port</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		/// <param name="p12FilePassword">Password protecting the p12File</param>
		public NotificationChannel(string host, int port, string p12File, string p12FilePassword)
            : this(host, port, System.IO.File.ReadAllBytes(p12File), p12FilePassword)
		{
		}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host">Push Notification Gateway Host</param>
        /// <param name="port">Push Notification Gateway Port</param>
        /// <param name="p12FileBytes">Byte array representation of PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
        /// <param name="p12FilePassword">Password protecting the p12File</param>
        public NotificationChannel(string host, int port, byte[] p12FileBytes, string p12FilePassword)
        {
            Host = host;
            Port = port;

            connected = false;
            firstConnect = true;
            ReconnectDelay = 3000;
            ConnectRetries = 6;

            //Need to load the private key seperately from apple
            // Fixed by danielgindi@gmail.com :
            //      The default is UserKeySet, which has caused internal encryption errors,
            //      Because of lack of permissions on most hosting services.
            //      So MachineKeySet should be used instead.
            certificate = new X509Certificate2(p12FileBytes, p12FilePassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            certificates = new X509CertificateCollection();
            certificates.Add(certificate);
        }

		#endregion

		#region Properties
		
		/// <summary>
		/// Gets or Sets the Number of Milliseconds to wait before Reconnecting to the Apns Host if the connection was lost or failed
		/// </summary>
		public int ReconnectDelay
		{
			get;
			set;
		}

        /// <summary>
        /// Gets or Sets the number of times to retry establishing a connection before giving up.
        /// </summary>
		public int ConnectRetries
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the Push Notification Gateway Host
		/// </summary>
		public string Host
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the Push Notification Gateway Port
		/// </summary>
		public int Port
		{
			get;
			private set;
		}

		#endregion

		#region Public Methods

        public void SendNotifications(Notification[] notifications)
        {
            NotificationBatch batch = new NotificationBatch(this, notifications);
            bool complete = false;
            while(!complete)
                complete = batch.SendMessages();
			batch.Complete();
        }

        class NotificationBatch
		{
			private NotificationChannel channel;
            private Notification[] notifications;
			private List<NotificationDeliveryError> errors;
			private ManualResetEvent mre = new ManualResetEvent(false);
			private byte[] readBuffer;
			private int current;
			private bool faulted;

            internal NotificationBatch(NotificationChannel channel, Notification[] notifications)
            {
                this.channel = channel;
				this.notifications = notifications;
				this.errors = new List<NotificationDeliveryError>();
                this.readBuffer = new byte[6];
                this.current = 0;
            }

			internal bool SendMessages()
			{
				faulted = false;
				channel.EnsureConnection();
				mre.Reset();
				IAsyncResult ar = this.channel.apnsStream.BeginRead(this.readBuffer, 0, 6, new AsyncCallback(OnAsyncRead), null);
				while (!faulted && current < notifications.Length)
				{
					Notification notification = notifications[current];
					byte[] notificationBytes = null;

					try
					{
						notificationBytes = notification.ToBytes();
					}
					catch (Exception x)
					{
						errors.Add(new NotificationDeliveryError(x, notification));
						current++;
						continue;
					}

					try
					{
						if (!faulted)
						{
							this.channel.apnsStream.Write(notificationBytes);
							Console.WriteLine("Sent {0}", notification.DeviceToken);
							current++;
						}
					}
					catch (IOException)
					{
					}
					catch (ObjectDisposedException)
					{
					}
				}
				if (!ar.IsCompleted)
				{
					// Give Apple a chance to let us know something went wrong
					ar.AsyncWaitHandle.WaitOne(500);
					if (!ar.IsCompleted)
					{
						// Dispose the channel, which will force the async callback,
						// resulting in an ObjectDisposedException on EndRead.
						channel.apnsStream.Dispose();
					}
				}
				mre.WaitOne();
				return !faulted;
			}

			private void OnAsyncRead(IAsyncResult ar)
			{
				try
				{
					if (channel.apnsStream.EndRead(ar) == 6 && readBuffer[0] == 8)
					{
						DeliveryErrorType error = (DeliveryErrorType)readBuffer[1];
						faulted = true;
						int index = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(readBuffer, 2));
						Notification faultedNotification = notifications[index];
						errors.Add(new NotificationDeliveryError(error, faultedNotification));
						current = index + 1;
						channel.ForceReconnect();
					}
				}
				catch (ObjectDisposedException)
				{
					// This is how we "cancel" the asynchronous read, so just make sure
					// the channel must reconnect to try again.
					channel.ForceReconnect();
				}
				catch (Exception x)
				{
					faulted = true;
					Console.WriteLine(x.Message);
				}
				mre.Set();
			}

			internal void Complete()
			{
				if (this.errors.Count > 0)
					throw new NotificationBatchException(this.errors);
			}
        }

        /// <summary>
        /// Send a notification to a connected channel immediately.  Must call EnsureConnection() before starting to send.
        /// </summary>
        /// <param name="notification">The Notification to send.</param>
		public void Send(Notification notification)
		{
            apnsStream.Write(notification.ToBytes());
		}

        /// <summary>
        /// Ensure that the connection is established before starting to send notifications.
        /// </summary>
		public void EnsureConnection()
		{
			while (!connected)
				Reconnect();
		}

        /// <summary>
        /// Force a reconnection, for example, if an exception is received, in which case Apple normally
        /// closes the existing channel.
        /// </summary>
		public void ForceReconnect()
		{
			this.connected = false;
		}

        /// <summary>
        /// Disposes of the NotificationChannel and all associated resources.
        /// </summary>
		public void Dispose()
		{
			try { apnsStream.Close(); }
			catch { }

			try { apnsStream.Dispose(); }
			catch { }

			try { apnsClient.Client.Shutdown(SocketShutdown.Both); }
			catch { }

			try { apnsClient.Client.Close(); }
			catch { }

			try { apnsClient.Close(); }
			catch { }
		}

		#endregion

		#region Private Methods

		private bool Reconnect()
		{
			if (!firstConnect)
			{
				for (int i = 0; i < this.ReconnectDelay; i += 100)
					System.Threading.Thread.Sleep(100);
			}
			else
			{
				firstConnect = false;
			}


			if (apnsStream != null && apnsStream.CanWrite)
			{
				try { Disconnect(); }
				catch { }
			}

			if (apnsClient != null && apnsClient.Connected)
			{
				try { CloseSslStream(); }
				catch { }
			}

			if (Connect())
			{
				this.connected = OpenSslStream();

				return this.connected;
			}

			this.connected = false;

			return this.connected;
		}

		private bool Connect()
		{
			int connectionAttempts = 0;
			while (connectionAttempts < this.ConnectRetries && (apnsClient == null || !apnsClient.Connected))
			{
				if (connectionAttempts > 0)
					Thread.Sleep(this.ReconnectDelay);

				connectionAttempts++;

				try
				{
					if (this.Connecting != null)
						this.Connecting(this);

					apnsClient = new TcpClient();
					apnsClient.Connect(this.Host, this.Port);

				}
				catch (SocketException ex)
				{
					if (this.Error != null)
						this.Error(this, ex);

					return false;
				}
			}
			if (connectionAttempts >= 3)
			{
				if (this.Error != null)
					this.Error(this, new NotificationException(3, "Too many connection attempts"));

				return false;
			}

			return true;
		}

		private bool OpenSslStream()
		{
			apnsStream = new SslStream(apnsClient.GetStream(), false, new RemoteCertificateValidationCallback(validateServerCertificate), new LocalCertificateSelectionCallback(selectLocalCertificate));

			try
			{
				apnsStream.AuthenticateAsClient(this.Host, this.certificates, System.Security.Authentication.SslProtocols.Ssl3, false);
			}
			catch (System.Security.Authentication.AuthenticationException ex)
			{
				if (this.Error != null)
					this.Error(this, ex);

				return false;
			}

			if (!apnsStream.IsMutuallyAuthenticated)
			{
				if (this.Error != null)
					this.Error(this, new NotificationException(4, "Ssl Stream Failed to Authenticate"));

				return false;
			}

			if (!apnsStream.CanWrite)
			{
				if (this.Error != null)
					this.Error(this, new NotificationException(5, "Ssl Stream is not Writable"));

				return false;
			}

			if (this.Connected != null)
				this.Connected(this);

			return true;
		}

		private void EnsureDisconnected()
		{
			if (apnsStream != null)
				CloseSslStream();
			if (apnsClient != null)
				Disconnect();
		}

		private void CloseSslStream()
		{
			try
			{
				apnsStream.Close();
				apnsStream.Dispose();
				apnsStream = null;
			}
			catch (Exception ex)
			{
				if (this.Error != null)
					this.Error(this, ex);
			}

			if (this.Disconnected != null)
				this.Disconnected(this);
		}

		private void Disconnect()
		{
			try
			{
				apnsClient.Close();
			}
			catch (Exception ex)
			{
				if (this.Error != null)
					this.Error(this, ex);
			}
		}

		private bool validateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true; // Dont care about server's cert
		}

		private X509Certificate selectLocalCertificate(object sender, string targetHost, X509CertificateCollection localCertificates,
			X509Certificate remoteCertificate, string[] acceptableIssuers)
		{
			return certificate;
		}

		#endregion
	}
}
