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
			notification.Custom.Add("bar", new object[]{42});
			Console.WriteLine(notification.ToString());
			Console.WriteLine();
			System.Windows.Forms.Clipboard.SetText(notification.ToString());
			Console.ReadLine();


						
			//More complex notification
			Notification notification1 = new Notification();
						
			notification1.Alert.LocalizedKey = "GAME_PLAY_REQUEST_FORMAT";
			notification1.Alert.LocalizedArgs = new object[] {"Jenna", "Frank"};
									
			notification1.Badge = 5;
			
			notification1.Sound = "chime";
			
			notification1.Custom.Add("acme1", new object[]{"bar"});
			notification1.Custom.Add("acme2", new object[]{"bang", "whiz"});
			
			
			Console.WriteLine(notification1.ToString());
			Console.WriteLine();
			System.Windows.Forms.Clipboard.SetText(notification1.ToString());
			Console.ReadLine();			

			
			
			//Simpler notification
			Notification notification2 = new Notification();
						
			notification2.Alert.Body = "Bob wants to play poker";
			notification2.Alert.ActionLocalizedKey = "PLAY";
											
			notification2.Badge = 5;
			
			notification2.Sound = "chime";
			
			notification2.Custom.Add("acme1", new object[]{"bar"});
			notification2.Custom.Add("acme2", new object[]{"bang", "whiz"});
			
			
			Console.WriteLine(notification2.ToString());
			Console.WriteLine();
			System.Windows.Forms.Clipboard.SetText(notification2.ToString());
			Console.ReadLine();
			
			
			//Very simple notification
			Notification notification3 = new Notification();
						
			notification3.Alert.Body = "Bob wants to play poker";
								
			notification3.Badge = 5;
			
			notification3.Sound = "chime";	
			
			Console.WriteLine(notification3.ToString());
			Console.WriteLine();
			System.Windows.Forms.Clipboard.SetText(notification3.ToString());
			Console.ReadLine();
			
			
			//Badge update and sound only
			Notification notification4 = new Notification();
														
			notification4.Badge = 5;
			
			notification4.Sound = "chime";	
			
			Console.WriteLine(notification4.ToString());
			System.Windows.Forms.Clipboard.SetText(notification4.ToString());
			
			
			Console.WriteLine("Press enter to exit...");
			Console.ReadLine();
		}
	}
}