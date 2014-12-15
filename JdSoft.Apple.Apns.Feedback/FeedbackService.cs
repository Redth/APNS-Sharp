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

namespace JdSoft.Apple.Apns.Feedback
{
	/// <summary>
	/// Feedback Service Consumer
	/// </summary>
	public class FeedbackService : IDisposable
	{
		#region Delegates and Events
		/// <summary>
		/// Handles General Error Exceptions
		/// </summary>
		/// <param name="sender">FeedbackService Instance</param>
		/// <param name="ex">Exception Instance</param>
		public delegate void OnError(object sender, Exception ex);
		/// <summary>
		/// Occurs when a General Exception is thrown
		/// </summary>
		public event OnError Error;

		/// <summary>
		/// Handles Feedback Received Event
		/// </summary>
		/// <param name="sender">FeedbackService Instance</param>
		/// <param name="feedback">Feedback Instance</param>
		public delegate void OnFeedback(object sender, Feedback feedback);
		/// <summary>
		/// Occurs when Feedback Information is Received
		/// </summary>
		public event OnFeedback Feedback;
		#endregion

		#region Constants
		private const string hostSandbox = "feedback.sandbox.push.apple.com";
		private const string hostProduction = "feedback.push.apple.com";
		#endregion

		#region Instance Variables
		private bool disposing;
		private Encoding encoding;
		private X509Certificate certificate;
		private X509CertificateCollection certificates;
		private string P12FilePassword;
		private TcpClient apnsClient;
		private SslStream apnsStream;
		#endregion

		#region Constructors
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="host">Push Notification Feedback Host</param>
		/// <param name="port">Push Notification Feedback Port</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		public FeedbackService(string host, int port, string p12File)
		{
			Id = System.Guid.NewGuid().ToString("N");
			Host = host;
			Port = port;
			P12File = p12File;
			ConnectAttempts = 3;
			ReconnectDelay = 10000;
		}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host">Push Notification Feedback Host</param>
        /// <param name="port">Push Notification Feedback Port</param>
        /// <param name="p12FileBytes">Byte array representation of PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
        public FeedbackService(string host, int port, byte[] p12FileBytes)
        {
            Id = System.Guid.NewGuid().ToString("N");
            Host = host;
            Port = port;
            // Fixed by danielgindi@gmail.com :
 	    //      The default is UserKeySet, which has caused internal encryption errors,
 	    //      Because of lack of permissions on most hosting services.
 	    //      So MachineKeySet should be used instead.
            certificate = new X509Certificate2(p12FileBytes, (string)null, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            ConnectAttempts = 3;
            ReconnectDelay = 10000;
        }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="host">Push Notification Feedback Host</param>
		/// <param name="port">Push Notification Feedback Port</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		/// <param name="p12FilePassword">Password protecting the p12File</param>
		public FeedbackService(string host, int port, string p12File, string p12FilePassword)
		{
			Id = System.Guid.NewGuid().ToString("N");
			Host = host;
			Port = port;
			P12File = p12File;
			P12FilePassword = p12FilePassword;
			ConnectAttempts = 3;
			ReconnectDelay = 10000;
		}

        /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="host">Push Notification Feedback Host</param>
		/// <param name="port">Push Notification Feedback Port</param>
        /// <param name="p12FileBytes">Byte array representation of PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		/// <param name="p12FilePassword">Password protecting the p12File</param>
		public FeedbackService(string host, int port, byte[] p12FileBytes, string p12FilePassword)
        {
            Id = System.Guid.NewGuid().ToString("N");
            Host = host;
            Port = port;
            // Fixed by danielgindi@gmail.com :
 	    //      The default is UserKeySet, which has caused internal encryption errors,
 	    //      Because of lack of permissions on most hosting services.
 	    //      So MachineKeySet should be used instead.
            certificate = new X509Certificate2(p12FileBytes, p12FilePassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            P12FilePassword = p12FilePassword;
            ConnectAttempts = 3;
            ReconnectDelay = 10000;
        }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		public FeedbackService(bool sandbox, string p12File)
		{
			Id = System.Guid.NewGuid().ToString("N");
			Host = sandbox ? hostSandbox : hostProduction;
			Port = 2196;
			P12File = p12File;
			ConnectAttempts = 3;
		}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
        /// <param name="p12FileBytes">Byte array representation of PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
        public FeedbackService(bool sandbox, byte[] p12FileBytes)
        {
            Id = System.Guid.NewGuid().ToString("N");
            Host = sandbox ? hostSandbox : hostProduction;
            Port = 2196;
            // Fixed by danielgindi@gmail.com :
 	    //      The default is UserKeySet, which has caused internal encryption errors,
 	    //      Because of lack of permissions on most hosting services.
 	    //      So MachineKeySet should be used instead.
            certificate = new X509Certificate2(p12FileBytes, (string)null, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            ConnectAttempts = 3;
        }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
		/// <param name="p12File">PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		/// <param name="p12FilePassword">Password protecting the p12File</param>
		public FeedbackService(bool sandbox, string p12File, string p12FilePassword)
		{
			Id = System.Guid.NewGuid().ToString("N");
			Host = sandbox ? hostSandbox : hostProduction;
			Port = 2196;
			P12File = p12File;
			P12FilePassword = p12FilePassword;
			ConnectAttempts = 3;
			ReconnectDelay = 10000;
		}

        /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sandbox">Boolean flag indicating whether the default Sandbox or Production Host and Port should be used</param>
        /// <param name="p12FileBytes">Byte array representation of PKCS12 .p12 or .pfx File containing Public and Private Keys</param>
		/// <param name="p12FilePassword">Password protecting the p12File</param>
		public FeedbackService(bool sandbox, byte[] p12FileBytes, string p12FilePassword)
        {
            Id = System.Guid.NewGuid().ToString("N");
            Host = sandbox ? hostSandbox : hostProduction;
            Port = 2196;
            // Fixed by danielgindi@gmail.com :
 	    //      The default is UserKeySet, which has caused internal encryption errors,
 	    //      Because of lack of permissions on most hosting services.
 	    //      So MachineKeySet should be used instead.
            certificate = new X509Certificate2(p12FileBytes, p12FilePassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            P12FilePassword = p12FilePassword;
            ConnectAttempts = 3;
            ReconnectDelay = 10000;
        }

		#endregion

		#region Public Methods
		/// <summary>
		/// Ensures the Connection is closed and all resources are cleaned up
		/// </summary>
		public void Dispose()
		{
			disposing = true;

			ensureDisconnected();
		}

		/// <summary>
		/// Initiates the Connection to the Feedback Server and receives all Feedback data then closes the connection
		/// </summary>
		public void Run()
		{
			disposing = false;

			encoding = Encoding.ASCII;

            // certificate will already be set if one of the constructors that takes a byte array was used.
 	    if (certificate == null)
 	    {
                // Fixed by danielgindi@gmail.com :
 	        //      The default is UserKeySet, which has caused internal encryption errors,
 	        //      Because of lack of permissions on most hosting services.
 	        //      So MachineKeySet should be used instead.
                certificate = new X509Certificate2(System.IO.File.ReadAllBytes(P12File), P12FilePassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
 	    }

			certificates = new X509CertificateCollection();
			certificates.Add(certificate);

			if (ensureConnected() && !disposing)
			{
				//Set up
				byte[] buffer = new byte[38];
				int recd = 0;
				DateTime minTimestamp = DateTime.Now.AddYears(-1);

				//Get the first feedback
				recd = apnsStream.Read(buffer, 0, buffer.Length);

				//Continue while we have results and are not disposing
				while (recd > 0 && !disposing)
				{
					try
					{
						Feedback fb = new Feedback();

						//Get our seconds since 1970 ?
						byte[] bSeconds = new byte[4];
						byte[] bDeviceToken = new byte[32];

						Array.Copy(buffer, 0, bSeconds, 0, 4);

						//Check endianness
						if (BitConverter.IsLittleEndian)
							Array.Reverse(bSeconds);

						int tSeconds = BitConverter.ToInt32(bSeconds, 0);

						//Add seconds since 1970 to that date, in UTC and then get it locally
						fb.Timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(tSeconds).ToLocalTime();


						//Now copy out the device token
						Array.Copy(buffer, 6, bDeviceToken, 0, 32);

						fb.DeviceToken = BitConverter.ToString(bDeviceToken).Replace("-", "").ToLower().Trim();
												
						//Make sure we have a good feedback tuple
						if (fb.DeviceToken.Length == 64
							&& fb.Timestamp > minTimestamp
							&& this.Feedback != null)
						{
							//Raise event
							this.Feedback(this, fb);
						}			
						
					}
					catch (Exception ex)
					{
						if (this.Error != null)
							this.Error(this, ex);
					}

					//Clear our array to reuse it
					Array.Clear(buffer, 0, buffer.Length);

					//Read the next feedback
					recd = apnsStream.Read(buffer, 0, buffer.Length);
				}
			}

			ensureDisconnected();
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets the Unique Id for this Instance
		/// </summary>
		public string Id
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the Push Notification Feedback Host
		/// </summary>
		public string Host
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the Push Notification Feedback Port
		/// </summary>
		public int Port
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the PKCS12 .p12 or .pfx File Used
		/// </summary>
		public string P12File
		{
			get;
			private set;
		}

		/// <summary>
		/// How many times to try connecting before giving up
		/// </summary>
		public int ConnectAttempts
		{
			get;
			set;
		}

		/// <summary>
		/// Number of milliseconds to wait between connection attempts
		/// </summary>
		public int ReconnectDelay
		{
			get;
			set;
		}

		/// <summary>
		/// For whatever use you please :)
		/// </summary>
		public object Tag
		{
			get;
			set;
		}
		#endregion

		#region Private Methods
		private bool ensureConnected()
		{
			bool connected = false;

			if (apnsStream == null || !apnsStream.CanWrite)
				connected = false;

			if (apnsClient == null || !apnsClient.Connected)
				connected = false;

			int tries = 0;
			
			while (!connected && !disposing && tries < ConnectAttempts)
			{
				tries++;

				try
				{
					apnsClient = new TcpClient(Host, Port);

					apnsStream = new SslStream(apnsClient.GetStream(), true,
						new RemoteCertificateValidationCallback(validateServerCertificate),
						new LocalCertificateSelectionCallback(selectLocalCertificate));

					apnsStream.AuthenticateAsClient(Host,
						certificates,
						System.Security.Authentication.SslProtocols.Tls,
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

		private void ensureDisconnected()
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
