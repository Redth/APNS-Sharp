using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JdSoft.Apple.Apns.Feedback;

namespace JdSoft.Apple.Apns.Feedback.Test
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

			//Put your PKCS12 .p12 or .pfx filename here.
			// Assumes it is in the same directory as your app
			string p12File = "apn_developer_identity.p12";

			//This is the password that you protected your p12File 
			//  If you did not use a password, set it as null or an empty string
			string p12FilePassword = "password";


			//Actual Code starts below:
			//--------------------------------

			//Get the filename assuming the file is in the same directory as this app
			string p12Filename = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p12File);

			//Create the feedback service consumer
			FeedbackService service = new FeedbackService(sandbox, p12Filename, p12FilePassword);
			
			//Wireup the events
			service.Error += new FeedbackService.OnError(service_Error);
			service.Feedback += new FeedbackService.OnFeedback(service_Feedback);

			//Run it.  This actually connects and receives the feedback
			// the Feedback event will fire for each feedback object
			// received from the server
			service.Run();

			Console.WriteLine("Done.");
			Console.WriteLine("Cleaning up...");

			//Clean up
			service.Dispose();

			Console.WriteLine("Press enter to exit...");
			Console.ReadLine();
		}

		static void service_Feedback(object sender, Feedback feedback)
		{
			Console.WriteLine(string.Format("Feedback - Timestamp: {0} - DeviceId: {1}", feedback.Timestamp, feedback.DeviceToken));
		}

		static void service_Error(object sender, Exception ex)
		{
			Console.WriteLine(string.Format("Error: {0}", ex.Message));
		}
	}
}
