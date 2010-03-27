using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JdSoft.Apple.Apns.Notifications;

namespace JdSoft.Apple.Apns.Test
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			//Variables you may need to edit:
			//---------------------------------

			//True if you are using sandbox certificate, or false if using production
			bool sandbox = true;

			//Put your device token in here
			string testDeviceToken = "fe58fc8f527c363d1b775dca133e04bff24dc5032d08836992395cc56bfa62ef";
			
			//Put your PKCS12 .p12 or .pfx filename here.
			// Assumes it is in the same directory as your app
			string p12File = "apn_developer_identity.p12";

			//This is the password that you protected your p12File 
			//  If you did not use a password, set it as null or an empty string
			string p12FilePassword = "yourpassword";

			//Number of notifications to send
			int count = 3;

			//Number of milliseconds to wait in between sending notifications in the loop
			// This is just to demonstrate that the APNS connection stays alive between messages
			int sleepBetweenNotifications = 15000;
			

			//Actual Code starts below:
			//--------------------------------

			string p12Filename = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p12File);
			
			NotificationService service = new NotificationService(sandbox, p12Filename, p12FilePassword, 1);

			service.SendRetries = 5; //5 retries before generating notificationfailed event
			service.ReconnectDelay = 5000; //5 seconds

			service.Error += new NotificationService.OnError(service_Error);
			service.NotificationTooLong += new NotificationService.OnNotificationTooLong(service_NotificationTooLong);

			service.BadDeviceToken += new NotificationService.OnBadDeviceToken(service_BadDeviceToken);
			service.NotificationFailed += new NotificationService.OnNotificationFailed(service_NotificationFailed);
			service.NotificationSuccess += new NotificationService.OnNotificationSuccess(service_NotificationSuccess);
			service.Connecting += new NotificationService.OnConnecting(service_Connecting);
			service.Connected += new NotificationService.OnConnected(service_Connected);
			service.Disconnected += new NotificationService.OnDisconnected(service_Disconnected);
			
			//The notifications will be sent like this:
			//		Testing: 1...
			//		Testing: 2...
			//		Testing: 3...
			// etc...
			for (int i = 1; i <= count; i++)
			{
				//Create a new notification to send
				Notification alertNotification = new Notification(testDeviceToken);
				
				alertNotification.Payload.Alert.Body = string.Format("Testing {0}...", i);
				alertNotification.Payload.Sound = "default";
				alertNotification.Payload.Badge = i;
								
				//Queue the notification to be sent
				if (service.QueueNotification(alertNotification))
					Console.WriteLine("Notification Queued!");
				else
					Console.WriteLine("Notification Failed to be Queued!");

				//Sleep in between each message
				if (i < count)
				{
					Console.WriteLine("Sleeping " + sleepBetweenNotifications + " milliseconds before next Notification...");
					System.Threading.Thread.Sleep(sleepBetweenNotifications);
				}
			}
			
			Console.WriteLine("Cleaning Up...");

			//First, close the service.  
			//This ensures any queued notifications get sent befor the connections are closed
			service.Close();

			//Clean up
			service.Dispose();

			Console.WriteLine("Done!");
			Console.WriteLine("Press enter to exit...");
			Console.ReadLine();
		}

		static void service_BadDeviceToken(object sender, BadDeviceTokenException ex)
		{
			Console.WriteLine("Bad Device Token: {0}", ex.Message);
		}

		static void service_Disconnected(object sender)
		{
			Console.WriteLine("Disconnected...");
		}

		static void service_Connected(object sender)
		{
			Console.WriteLine("Connected...");
		}

		static void service_Connecting(object sender)
		{
			Console.WriteLine("Connecting...");
		}

		static void service_NotificationTooLong(object sender, NotificationLengthException ex)
		{
			Console.WriteLine(string.Format("Notification Too Long: {0}", ex.Notification.ToString()));
		}

		static void service_NotificationSuccess(object sender, Notification notification)
		{
			Console.WriteLine(string.Format("Notification Success: {0}", notification.ToString()));
		}

		static void service_NotificationFailed(object sender, Notification notification)
		{
			Console.WriteLine(string.Format("Notification Failed: {0}", notification.ToString()));
		}

		static void service_Error(object sender, Exception ex)
		{
			Console.WriteLine(string.Format("Error: {0}", ex.Message));
		}
	}
}
