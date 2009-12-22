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
			: base(string.Format("Notification Payload Length ({0}) Exceeds the maximum length of {1} characters", notification.Payload.ToJson().Length, Notification.MAX_PAYLOAD_SIZE))
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
