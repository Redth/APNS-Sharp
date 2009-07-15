using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JdSoft.Apple.Apns.Feedback
{
	/// <summary>
	/// Feedback object
	/// </summary>
	public class Feedback
	{

		/// <summary>
		/// Constructor
		/// </summary>
		public Feedback()
		{
			this.DeviceToken = string.Empty;
			this.Timestamp = DateTime.MinValue;
		}

		/// <summary>
		/// Device Token string in hex form without any spaces or dashes
		/// </summary>
		public string DeviceToken
		{
			get;
			set;
		}

		/// <summary>
		/// Timestamp of the Feedback for when Apple received the notice to stop sending notifications to the device
		/// </summary>
		public DateTime Timestamp
		{
			get;
			set;
		}
	}
}
