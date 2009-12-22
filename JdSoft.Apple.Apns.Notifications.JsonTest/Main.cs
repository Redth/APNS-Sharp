using System;
using JdSoft.Apple.Apns.Notifications;

namespace JdSoft.Apple.Test
{
	class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			//Empty aps, just custom
			Notification notification = new Notification();
			notification.Payload.AddCustom("bar", 42);

			Console.WriteLine(notification.ToString());
			Console.WriteLine();
			System.Windows.Forms.Clipboard.SetText(notification.ToString());
			Console.ReadLine();


						
			//More complex notification
			Notification notification1 = new Notification();

			notification1.Payload.Alert.LocalizedKey = "GAME_PLAY_REQUEST_FORMAT";
			notification1.Payload.Alert.AddLocalizedArgs("Jenna", "Frank");

			notification1.Payload.Badge = 5;

			notification1.Payload.Sound = "chime";

			notification1.Payload.AddCustom("acme1", "bar");
			notification1.Payload.AddCustom("acme2", "bang", "whiz");


			Console.WriteLine(notification1.ToString());
			Console.WriteLine();
			System.Windows.Forms.Clipboard.SetText(notification1.ToString());
			Console.ReadLine();			

			
			
			////Simpler notification
			Notification notification2 = new Notification();

			notification2.Payload.Alert.Body = "Bob wants to play poker";
			notification2.Payload.Alert.ActionLocalizedKey = "PLAY";

			notification2.Payload.Badge = 5;

			notification2.Payload.Sound = "chime";

			notification2.Payload.AddCustom("acme1", "bar");
			notification2.Payload.AddCustom("acme2", "bang", "whiz");


			Console.WriteLine(notification2.ToString());
			Console.WriteLine();
			System.Windows.Forms.Clipboard.SetText(notification2.ToString());
			Console.ReadLine();
			
			
			////Very simple notification
			Notification notification3 = new Notification();

			notification3.Payload.Alert.Body = "Bob wants to play poker";

			notification3.Payload.Badge = 5;

			notification3.Payload.Sound = "chime";

			Console.WriteLine(notification3.ToString());
			Console.WriteLine();
			System.Windows.Forms.Clipboard.SetText(notification3.ToString());
			Console.ReadLine();
			
			
			////Badge update and sound only
			Notification notification4 = new Notification();

			notification4.Payload.Badge = 5;

			notification4.Payload.Sound = "chime";
			notification4.Payload.AddCustom("test", 4, 2, 12);

			Console.WriteLine(notification4.ToString());
			System.Windows.Forms.Clipboard.SetText(notification4.ToString());
			
			
			Console.WriteLine("Press enter to exit...");
			Console.ReadLine();
		}
	}
}