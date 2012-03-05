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

		private ThreadSafeQueue<Notification> notifications;
		private Thread workerThread;
		private NotificationChannel apnsChannel;
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
			apnsChannel = new NotificationChannel(host, port, p12File);
			start();
		}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host">Push Notification Gateway Host</param>
        /// <param name="port">Push Notification Gateway Port</param>
        /// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys, as a byte array</param>
        /// <param name="p12FilePassword">Password protecting the p12File</param>
        public NotificationConnection( string host, int port, byte[] p12FileBytes, string p12FilePassword )
        {
            apnsChannel = new NotificationChannel( host, port, p12FileBytes, p12FilePassword );
            start();
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
			apnsChannel = new NotificationChannel(host, port, p12File, p12FilePassword);
			start();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		public NotificationConnection(bool sandbox, string p12File)
		{
			apnsChannel = new NotificationChannel(sandbox, p12File);
			start();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		/// <param name="p12FilePassword">Password protecting the p12File</param>
		public NotificationConnection(bool sandbox, string p12File, string p12FilePassword)
		{
			apnsChannel = new NotificationChannel(sandbox, p12File, p12FilePassword);
			start();
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
			get { return apnsChannel.Host; }
		}

		/// <summary>
		/// Gets the Push Notification Gateway Port
		/// </summary>
		public int Port
		{
			get { return apnsChannel.Port; }
		}

		/// <summary>
		/// For whatever use you please :)
		/// </summary>
		public object Tag
		{
			get;
			set;
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
			// Changed to keep looping until the notifications count is 0, meaning all have been dequeued,
			// or a timeout of 10 seconds has occurred. 
			int slept = 0; 
			int maxSleep = 10000; //10 seconds

			while (notifications.Count > 0 && slept <= maxSleep)
			{
				Thread.Sleep(100);
				slept += 100;
			}

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

			try { apnsChannel.Dispose(); }
			catch { }
		}
		#endregion

		#region Private Methods
		private void start()
		{
			accepting = true;
			disposing = false;
			closing = false;

			notifications = new ThreadSafeQueue<Notification>();
			Id = System.Guid.NewGuid().ToString("N");
			ReconnectDelay = 3000; //3 seconds
			SendRetries = 3;

			apnsChannel.ReconnectDelay = this.ReconnectDelay;
			apnsChannel.ConnectRetries = SendRetries * 2;
			apnsChannel.Error += new NotificationChannel.OnError(OnChannelError);
			apnsChannel.Connected += new NotificationChannel.OnConnected(OnChannelConnected);
			apnsChannel.Connecting += new NotificationChannel.OnConnecting(OnChannelConnecting);
			apnsChannel.Disconnected += new NotificationChannel.OnDisconnected(OnChannelDisconnected);

			workerThread = new Thread(new ThreadStart(workerMethod));
			workerThread.Start();
		}

		void OnChannelDisconnected(object sender)
		{
		    var onDisconnected = Disconnected;
		    if (onDisconnected != null)
				onDisconnected(this);
		}

	    void OnChannelConnecting(object sender)
		{
		    var onConnecting = Connecting;
		    if (onConnecting != null)
				onConnecting(this);
		}

	    void OnChannelConnected(object sender)
	    {
	        var onConnected = Connected;
	        if (onConnected != null)
				onConnected(this);
	    }

	    void OnChannelError(object sender, Exception ex)
		{
		    var onError = Error;
		    if (onError != null)
				onError(this, ex);
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
									apnsChannel.EnsureConnection();

									try
									{
										apnsChannel.Send(notification);
									}
									catch (BadDeviceTokenException btex)
									{
									    var onBadDeviceToken = BadDeviceToken;
									    if (onBadDeviceToken != null)
                                            onBadDeviceToken(this, btex);
									}
									catch (NotificationLengthException nlex)
									{
									    var onNotificationTooLong = NotificationTooLong;
									    if (onNotificationTooLong != null)
											onNotificationTooLong(this, nlex);
									}

								    string txtAlert = string.Empty;

								    var onNotificationSuccess = NotificationSuccess;
								    if (onNotificationSuccess != null)
										onNotificationSuccess(this, notification);

									sent = true;
								}
								else
								{
									apnsChannel.ForceReconnect();
								}
							}
							catch (Exception ex)
							{
							    var onError = Error;
							    if (onError != null)
                                    onError(this, ex);

								apnsChannel.ForceReconnect();
							}

							tries++;
						}

						//Didn't send in 3 tries
					    var onNotificationFailed = NotificationFailed;
					    if (!sent && onNotificationFailed != null)
							onNotificationFailed(this, notification);
					}
				}
				catch (Exception ex)
				{
				    var onError = Error;
				    if (onError != null)
						onError(this, ex);

					apnsChannel.ForceReconnect();
				}

				if (!disposing)
					Thread.Sleep(500);
			}
		}





		#endregion
	}
}