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
using JdSoft.Apple.Apns.Notifications.Collections;

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
		#endregion

		#region Instance Variables
		private bool disposing;
		private bool closing;
		private bool accepting;

		private Encoding encoding;
		private LockFreeQueue<Notification> notifications;
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
			notifications = new LockFreeQueue<Notification>();
			Id = System.Guid.NewGuid().ToString("N");
			ReconnectDelay = 5000; //5 seconds
			SendRetries = 5;

			//Need to load the private key seperately from apple
			if (string.IsNullOrEmpty(p12FilePassword))
				certificate = new X509Certificate2(p12File);
			else
				certificate = new X509Certificate2(p12File, p12FilePassword);

			certificates = new X509CertificateCollection();
			certificates.Add(certificate);

			workerThread = new Thread(new ThreadStart(workerMethod));
			workerThread.Start();
		}

		private void workerMethod()
		{
			byte[] buffer = new byte[293];

			while (!this.disposing && !closing) //Keep going until disposing or closing
			{
				Notification notification = null;

				//This we do while not disposing, but continue even if closing
				// to flush out the buffer of messages to send
				while (this.notifications.Dequeue(out notification) && !disposing)
				{
					int tries = 0;
					bool sent = false;
					bool alertFailed = true;

					while (tries <= SendRetries && !sent)
					{
						if (ensureConnected())
						{
							//Generate the notification into our buffer
							try 
							{ 
								notification.Build(ref buffer); 
							}
							catch (NotificationLengthException nex)
							{
								tries = SendRetries + 1;
								sent = false;
								alertFailed = false; //Already putting an exception here

								if (this.NotificationTooLong != null)
									this.NotificationTooLong(this, nex);
							}

							try
							{
								//Send the notification
								apnsStream.Write(buffer);
								sent = true; //Can only assume it worked at this point
							}
							catch (Exception ex)
							{
								if (this.Error != null)
									this.Error(this, ex);
							}
						}

						tries++;
					}

					//Trigger events
					if (sent && this.NotificationSuccess != null)
						this.NotificationSuccess(this, notification);
					else if (!sent && this.NotificationFailed != null && alertFailed)
						this.NotificationFailed(this, notification);
				}

				System.Threading.Thread.Sleep(100);
			}
		}


		private bool ensureConnected()
		{
			bool connected = false;

			if (apnsStream == null || !apnsStream.CanWrite)
				connected = false;

			if (apnsClient == null || !apnsClient.Connected)
				connected = false;

			while (!connected && !disposing)
			{
				try
				{
					apnsClient = new TcpClient(Host, Port);

					apnsStream = new SslStream(apnsClient.GetStream(), true,
						new RemoteCertificateValidationCallback(validateServerCertificate),
						new LocalCertificateSelectionCallback(selectLocalCertificate));

					apnsStream.AuthenticateAsClient(Host,
						certificates,
						System.Security.Authentication.SslProtocols.Ssl3,
						false);

					connected = apnsStream.CanWrite;
				}
				catch (Exception ex)
				{
					if (this.Error != null)
						this.Error(this, ex);

					connected = false;
										
				}

				if (!connected)
				{
					int wait = ReconnectDelay;
					int waited = 0;

					while (waited < wait && !disposing)
					{
						System.Threading.Thread.Sleep(250);
						waited += 250;
					}
				}

			}

			return connected;
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
