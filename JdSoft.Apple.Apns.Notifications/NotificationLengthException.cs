using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JdSoft.Apple.Apns.Notifications
{
	public class NotificationLengthException : Exception
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="notification">Notification that caused the Exception</param>
		public NotificationLengthException(Notification notification)
			: base("Notification Payload Exceeds 256 bytes")
		{
			this.Notification = notification;
		}

		/// <summary>
		/// Notification that caused the Exception
		/// </summary>
		public Notification Notification
		{
			get;
			private set;
		}
		
	}
}
