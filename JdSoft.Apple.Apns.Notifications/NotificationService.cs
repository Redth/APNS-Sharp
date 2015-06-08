using System;
using System.Collections.Generic;
using System.Text;

namespace JdSoft.Apple.Apns.Notifications
{
    using System.IO;

    public class NotificationService : IDisposable
	{
		#region Constants
		private const string hostSandbox = "gateway.sandbox.push.apple.com";
		private const string hostProduction = "gateway.push.apple.com";
        private const int apnsPort = 2195;
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
		private List<NotificationConnection> notificationConnections = new List<NotificationConnection>();
		private Random rand = new Random((int)DateTime.Now.Ticks);
		private int sequential = 0;
		private int reconnectDelay = 5000;
		private int sendRetries = 1;

		private bool closing = false;
		private bool disposing = false;
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="host">Push Notification Gateway Host</param>
		/// <param name="port">Push Notification Gateway Port</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		/// <param name="connections">Number of Apns Connections to start with</param>
		public NotificationService(string host, int port, string p12File, int connections)
            : this(host, port, p12File, null, connections)
		{
		}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host">Push Notification Gateway Host</param>
        /// <param name="port">Push Notification Gateway Port</param>
        /// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
        /// <param name="p12FilePassword">Password protecting the p12File</param>
        /// <param name="connections">Number of Apns Connections to start with</param>
        public NotificationService( string host, int port, string p12File, string p12FilePassword, int connections )
            : this( host, port, System.IO.File.ReadAllBytes( p12File ), p12FilePassword, connections )
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host">Push Notification Gateway Host</param>
        /// <param name="port">Push Notification Gateway Port</param>
        /// <param name="p12FileBytes">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
        /// <param name="p12FilePassword">Password protecting the p12File</param>
        /// <param name="connections">Number of Apns Connections to start with</param>
        public NotificationService( string host, int port, byte[] p12FileBytes, string p12FilePassword, int connections )
        {
            this.SendRetries = 1;
            closing = false;
            disposing = false;
            Host = host;
            Port = port;
            P12FileBytes = p12FileBytes;
            P12FilePassword = p12FilePassword;
            DistributionType = NotificationServiceDistributionType.Sequential;
            Connections = connections;
        }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		/// <param name="connections">Number of Apns Connections to start with</param>
		public NotificationService(bool sandbox, string p12File, int connections)
            : this(sandbox ? hostSandbox : hostProduction, apnsPort, p12File, null, connections)
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		/// <param name="p12FilePassword">Password protecting the p12File</param>
		/// <param name="connections">Number of Apns Connections to start with</param>
        public NotificationService(bool sandbox, string p12File, string p12FilePassword, int connections) :
            this(sandbox ? hostSandbox : hostProduction, apnsPort, p12File, p12FilePassword, connections)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
        /// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
        /// <param name="p12FilePassword">Password protecting the p12File</param>
        /// <param name="connections">Number of Apns Connections to start with</param>
        public NotificationService( bool sandbox, byte[] p12FileBytes, string p12FilePassword, int connections ) :
            this( sandbox ? hostSandbox : hostProduction, apnsPort, p12FileBytes, p12FilePassword, connections )
        {
        }

		/// <summary>
		/// Constructor	
		/// </summary>
		/// <param name="host">Push Notification Gateway Host</param>
		/// <param name="port">Push Notification Gateway Port</param>
		/// <param name="p12FileStream">Stream to PKCS12 .p12 or .pfx file containing Public and Private Keys</param>
		/// <param name="p12FilePassword">Password protecting the p12File</param>
		/// <param name="connections">Number of Apns Connections to start with</param>
		public NotificationService(bool sandbox, Stream p12FileStream, string p12FilePassword, int connections) :
			this(sandbox ? hostSandbox : hostProduction, apnsPort, getAllBytesFromStream(p12FileStream), p12FilePassword, connections)
		{
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
			get { return reconnectDelay; }
			set
			{
				reconnectDelay = value;

				foreach (NotificationConnection con in notificationConnections)
					con.ReconnectDelay = reconnectDelay;
			}
		}

		/// <summary>
		/// Gets or Sets the Number of times to try resending a Notification before the NotificationFailed event is raised
		/// </summary>
		public int SendRetries
		{
			get { return sendRetries; }
			set
			{
				sendRetries = value;

				foreach (NotificationConnection con in notificationConnections)
					con.SendRetries = sendRetries;
			}
		}

		/// <summary>
		/// Gets or Sets the method used to distribute Queued Notifications over all open Apns Connections
		/// </summary>
		public NotificationServiceDistributionType DistributionType
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the PKCS12 .p12 or .pfx File being used
		/// </summary>
		public byte[] P12FileBytes
		{
			get;
			private set;
		}

		private string P12FilePassword;

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
		/// For whatever use you please :)
		/// </summary>
		public object Tag
		{
			get;
			set;
		}
				
		/// <summary>
		/// Gets or Sets the number of Apns Connections in use.  Changing this property will dynamically change the number of connections in use.  If it is decreased, connections will be Closed (waiting for the Queue to be Emptied first), or if raised, new connections will be added.
		/// </summary>
		public int Connections
		{
			get
			{
				return notificationConnections.Count;
			}
			set
			{
				//Don't want 0 connections or less
				if (value <= 0)
					return;

				//Get the delta
				int difference = value - notificationConnections.Count;

				if (difference > 0)
				{
					//Need to add connections
					for (int i = 0; i < difference; i++)
					{
						NotificationConnection newCon = new NotificationConnection(Host, Port, P12FileBytes, P12FilePassword);
						newCon.SendRetries = SendRetries;
						newCon.ReconnectDelay = ReconnectDelay;

						newCon.Error += new NotificationConnection.OnError(newCon_Error);
						newCon.NotificationFailed += new NotificationConnection.OnNotificationFailed(newCon_NotificationFailed);
						newCon.NotificationTooLong += new NotificationConnection.OnNotificationTooLong(newCon_NotificationTooLong);
						newCon.NotificationSuccess += new NotificationConnection.OnNotificationSuccess(newCon_NotificationSuccess);
						newCon.Connecting += new NotificationConnection.OnConnecting(newCon_Connecting);
						newCon.Connected += new NotificationConnection.OnConnected(newCon_Connected);
						newCon.Disconnected += new NotificationConnection.OnDisconnected(newCon_Disconnected);
						newCon.BadDeviceToken += new NotificationConnection.OnBadDeviceToken(newCon_BadDeviceToken);
						notificationConnections.Add(newCon);
					}

				}
				else if (difference < 0)
				{
					//Need to remove connections
					for (int i = 0; i < difference * -1; i++)
					{
						if (notificationConnections.Count > 0)
						{
							NotificationConnection toClose = notificationConnections[0];
							notificationConnections.RemoveAt(0);

							toClose.Close();
							toClose.Dispose();
							toClose = null;
						}
					}
				}
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Queues a Notification to one of the Apns Connections using the DistributionType specified by the property.
		/// </summary>
		/// <param name="notification">Notification object to send</param>
		/// <returns>If true, the Notification has been successfully queued</returns>
		public bool QueueNotification(Notification notification)
		{
			bool queued = false;

			if (!disposing && !closing)
			{
				int tries = 0;
				
				while (tries < SendRetries && !queued)
				{
					if (DistributionType == NotificationServiceDistributionType.Sequential)
						queued = queueSequential(notification);
					else if (DistributionType == NotificationServiceDistributionType.Random)
						queued = queueRandom(notification);

					tries++;
				}
			}

			return queued;
		}

		/// <summary>
		/// Closes all of the Apns Connections but first waits for all Queued Notifications on each Apns Connection to be sent.  This will cause QueueNotification to always return false after this method is called.
		/// </summary>
		public void Close()
		{
			closing = true;

			foreach (NotificationConnection con in notificationConnections)
				con.Close();
		}

		/// <summary>
		/// Closes all of the Apns Connections without waiting for Queued Notifications on each Apns Connection to be sent.  This will cause QueueNotification to always return false after this method is called.
		/// </summary>
		public void Dispose()
		{
			disposing = true;

			foreach (NotificationConnection con in notificationConnections)
				con.Dispose();
		}
		#endregion

		#region Private Methods
		void newCon_NotificationSuccess(object sender, Notification notification)
		{
		    var onNotificationSuccess = NotificationSuccess;
		    if (onNotificationSuccess != null)
				NotificationSuccess(sender, notification);
		}

        void newCon_NotificationTooLong(object sender, NotificationLengthException ex)
		{
		    var onNotificationTooLong = NotificationTooLong;
		    if (onNotificationTooLong != null)
				NotificationTooLong(sender, ex);
		}

        void newCon_BadDeviceToken(object sender, BadDeviceTokenException ex)
		{
		    var onBadDeviceToken = BadDeviceToken;
		    if (onBadDeviceToken != null)
				BadDeviceToken(this, ex);
		}

        void newCon_NotificationFailed(object sender, Notification failed)
		{
		    var onNotificationFailed = NotificationFailed;
		    if (onNotificationFailed != null)
				NotificationFailed(sender, failed);
		}

        void newCon_Error(object sender, Exception ex)
		{
		    var onError = Error;
		    if (onError != null)
				Error(sender, ex);
		}

        void newCon_Disconnected(object sender)
		{
		    var onDisconnected = Disconnected;
		    if (onDisconnected != null)
				Disconnected(sender);
		}

        void newCon_Connected(object sender)
		{
		    var onConnected = Connected;
		    if (onConnected != null)
				Connected(sender);
		}

        void newCon_Connecting(object sender)
		{
		    var onConnecting = Connecting;
		    if (onConnecting != null)
				onConnecting(sender);
		}

        private bool queueSequential(Notification notification)
		{
            var index = sequential;
            if (index > notificationConnections.Count - 1)
                sequential = index = 0;
            else
                sequential++;

            if (notificationConnections[index] != null)
                return notificationConnections[index].QueueNotification(notification);
			
			return false;
		}

		private bool queueRandom(Notification notification)
		{
			int index = rand.Next(0, notificationConnections.Count - 1);

			if (notificationConnections[index] != null)
				return notificationConnections[index].QueueNotification(notification);

			return false;
		}

		private static byte[] getAllBytesFromStream(Stream s)
		{
			byte[] buffer = new byte[16 * 1024];
			
			using (MemoryStream ms = new MemoryStream())
			{
				int read;	
				
				while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
					ms.Write(buffer, 0, read);
			
				return ms.ToArray();
			}
		}
		#endregion
	}
}
