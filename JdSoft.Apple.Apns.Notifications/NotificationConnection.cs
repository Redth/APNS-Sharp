using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace JdSoft.Apple.Apns.Notifications
{
	/// <summary>
	/// Apns Notification Server Connection
	/// </summary>
	public class NotificationConnection : IDisposable
	{
		#region Constants
		private const string hostSandbox = "gateway.sandbox.push.apple.com";
		private const string hostProduction = "gateway.push.apple.com";
		#endregion

		#region Delegates and Events
		/// <summary>
		/// Handles General Exceptions
		/// </summary>
		/// <param name="sender">NotificationConnection Instance that generated the Exception</param>
		/// <param name="ex">Exception Instance</param>
		public delegate void OnError(object sender, Exception ex);
		/// <summary>
		/// Occurs when a General Error is thrown
		/// </summary>
		public event OnError Error;

		/// <summary>
		/// Handles Notification Too Long Exceptions when a Notification's payload that is being sent is too long
		/// </summary>
		/// <param name="sender">NotificationConnection Instance that generated the Exception</param>
		/// <param name="ex">NotificationTooLongException Instance</param>
		public delegate void OnNotificationTooLong(object sender, NotificationLengthException ex);
		/// <summary>
		/// Occurs when a Notification that is being sent has a payload longer than the allowable limit of 256 bytes as per Apple's specifications
		/// </summary>
		public event OnNotificationTooLong NotificationTooLong;

		/// <summary>
		/// Handles Bad Device Token Exceptions when the device token provided is not the right length
		/// </summary>
		/// <param name="sender">NotificatioConnection Instance that generated the Exception</param>
		/// <param name="ex">BadDeviceTokenException Instance</param>
		public delegate void OnBadDeviceToken(object sender, BadDeviceTokenException ex);
		/// <summary>
		/// Occurs when a Device Token that's specified is not the right length
		/// </summary>
		public event OnBadDeviceToken BadDeviceToken;

		/// <summary>
		/// Handles Successful Notification Send Events
		/// </summary>
		/// <param name="sender">NotificationConnection Instance</param>
		/// <param name="notification">Notification object that was Sent</param>
		public delegate void OnNotificationSuccess(object sender, Notification notification);
		/// <summary>
		/// Occurs when a Notification has been successfully sent to Apple's Servers
		/// </summary>
		public event OnNotificationSuccess NotificationSuccess;

		/// <summary>
		/// Handles Failed Notification Deliveries
		/// </summary>
		/// <param name="sender">NotificationConnection Instance</param>
		/// <param name="failed">Notification object that failed to send</param>
		public delegate void OnNotificationFailed(object sender, Notification failed);
		/// <summary>
		/// Occurs when a Notification has failed to send to Apple's Servers.  This is event is raised after the NotificationConnection has attempted to resend the notification the number of SendRetries specified.
		/// </summary>
		public event OnNotificationFailed NotificationFailed;

		/// <summary>
		/// Handles Connecting Event
		/// </summary>
		/// <param name="sender">NotificationConnection Instance</param>
		public delegate void OnConnecting(object sender);
		/// <summary>
		/// Occurs when Connecting to Apple's servers
		/// </summary>
		public event OnConnecting Connecting;

		/// <summary>
		/// Handles Connected Event
		/// </summary>
		/// <param name="sender">NotificationConnection Instance</param>
		public delegate void OnConnected(object sender);
		/// <summary>
		/// Occurs when successfully connected and authenticated via SSL to Apple's Servers
		/// </summary>
		public event OnConnected Connected;

		/// <summary>
		/// Handles Disconnected Event
		/// </summary>
		/// <param name="sender">NotificationConnection Instance</param>
		public delegate void OnDisconnected(object sender);
		/// <summary>
		/// Occurs when the connection to Apple's Servers has been lost
		/// </summary>
		public event OnDisconnected Disconnected;
		#endregion

		#region Instance Variables
		private bool disposing;
		private bool closing;
		private bool accepting;
		private bool connected;
		private bool firstConnect;

		private Encoding encoding;
		private ThreadSafeQueue<Notification> notifications;
		private Thread workerThread;
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
		public NotificationConnection(string host, int port, string p12File)
		{
			connected = false;
			firstConnect = true;

			Host = host;
			Port = port;

			start(p12File, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="host">Push Notification Gateway Host</param>
		/// <param name="port">Push Notification Gateway Port</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		/// <param name="p12FilePassword">Password protecting the p12File</param>
		public NotificationConnection(string host, int port, string p12File, string p12FilePassword)
		{
			connected = false;
			firstConnect = true;

			Host = host;
			Port = port;
			
			start(p12File, p12FilePassword);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		public NotificationConnection(bool sandbox, string p12File)
		{
			Host = sandbox ? hostSandbox : hostProduction;
			Port = 2195;

			start(p12File, null);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		/// <param name="p12FilePassword">Password protecting the p12File</param>
		public NotificationConnection(bool sandbox, string p12File, string p12FilePassword)
		{
			connected = false;
			firstConnect = true;

			Host = sandbox ? hostSandbox : hostProduction;
			Port = 2195;

			start(p12File, p12FilePassword);
		}
		#endregion

		#region Properties
		/// <summary>
		/// Unique Identifier for this Instance
		/// </summary>
		public string Id
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets or Sets the Number of Milliseconds to wait before Reconnecting to the Apns Host if the connection was lost or failed
		/// </summary>
		public int ReconnectDelay
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or Sets the Number of times to try resending a Notification before the NotificationFailed event is raised
		/// </summary>
		public int SendRetries
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

		/// <summary>
		/// Gets the number of notifications currently in the queue
		/// </summary>
		public int QueuedNotificationsCount
		{
			get { return notifications.Count; }
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Queue's a Notification to be Sent as soon as possible in a First in First out pattern
		/// </summary>
		/// <param name="notification">Notification object to send</param>
		/// <returns>If true, the notification was queued successfully, otherwise it was not and will not be sent</returns>
		public bool QueueNotification(Notification notification)
		{
			if (!disposing && !closing && accepting)
			{
				notifications.Enqueue(notification);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Closes the Apns Connection but first waits for all Queued Notifications to be sent.  This will cause QueueNotification to always return false after this method is called.
		/// </summary>
		public void Close()
		{
			accepting = false;
			
			//Sleep here to prevent a race condition
			// in which a notification could be queued while the worker thread
			// is sleeping after its loop, but if we set closing true within that 100 ms,
			// the queued notifications during that time would not get dequeued as the loop
			// would exit due to closing = true;
			// 250 ms should be ample time for the loop to dequeue any remaining notifications
			// after we stopped accepting above
			Thread.Sleep(250);
			
			closing = true;

			//Wait for buffer to be flushed out
			if (workerThread != null && workerThread.IsAlive)
				workerThread.Join();
		}

		/// <summary>
		/// Closes the Apns Connections without waiting for Queued Notifications to be sent.  This will cause QueueNotification to always return false after this method is called.
		/// </summary>
		public void Dispose()
		{
			
			accepting = false;
			
			//We don't really care about the race condition here
			// since disposing does NOT wait for all notifications to be sent
			
			disposing = true;

			//Wait for the worker to finish cleanly
			if (workerThread != null && workerThread.IsAlive)
				workerThread.Join();


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
		private void start(string p12File, string p12FilePassword)
		{
			accepting = true;
			disposing = false;
			closing = false;

			encoding = Encoding.ASCII;
			notifications = new ThreadSafeQueue<Notification>();
			Id = System.Guid.NewGuid().ToString("N");
			ReconnectDelay = 3000; //3 seconds
			SendRetries = 3;

			//Need to load the private key seperately from apple
			if (string.IsNullOrEmpty(p12FilePassword))
				certificate = new X509Certificate2(System.IO.File.ReadAllBytes(p12File));
			else
				certificate = new X509Certificate2(System.IO.File.ReadAllBytes(p12File), p12FilePassword);

			certificates = new X509CertificateCollection();
			certificates.Add(certificate);

			workerThread = new Thread(new ThreadStart(workerMethod));
			workerThread.Start();
		}

		private void workerMethod()
		{
			while (!disposing && !closing)
			{
				try
				{
					while (this.notifications.Count > 0 && !disposing)
					{
						Notification notification = this.notifications.Dequeue();
	
						int tries = 0;
						bool sent = false;

						while (!sent && tries < this.SendRetries)
						{
							try
							{
								if (!disposing)
								{
									while (!connected)
										Reconnect();

									try
									{
										apnsStream.Write(notification.ToBytes());
									}
									catch (BadDeviceTokenException btex)
									{
										if (this.BadDeviceToken != null)
											this.BadDeviceToken(this, btex);
									}
									catch (NotificationLengthException nlex)
									{
										if (this.NotificationTooLong != null)
											this.NotificationTooLong(this, nlex);
									}

									string txtAlert = string.Empty;
																		
									if (this.NotificationSuccess != null)
										this.NotificationSuccess(this, notification);

									sent = true;
								}
								else
								{
									this.connected = false;
								}
							}
							catch (Exception ex)
							{
								if (this.Error != null)
									this.Error(this, ex);

								this.connected = false;
							}

							tries++;
						}

						//Didn't send in 3 tries
						if (!sent && this.NotificationFailed != null)
							this.NotificationFailed(this, notification);
					}
				}
				catch (Exception ex)
				{
					if (this.Error != null)
						this.Error(this, ex);

					this.connected = false;
				}

				if (!disposing)
					Thread.Sleep(500);
			}
		}

		


		private bool Reconnect()
		{
			if (!firstConnect)
			{
				for (int i = 0; i < this.ReconnectDelay; i+=100)
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
			while (connectionAttempts < (this.SendRetries * 2) && (apnsClient == null || !apnsClient.Connected))
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
