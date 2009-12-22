using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JdSoft.Apple.Apns.Notifications
{
	public class BadDeviceTokenException : Exception
	{
		public BadDeviceTokenException(string deviceToken)
			: base(string.Format("Device Token Length ({0}) Is not the required length of {1} characters!", deviceToken.Length, Notification.DEVICE_TOKEN_STRING_SIZE))
		{
			this.DeviceToken = deviceToken;
		}

		public string DeviceToken
		{
			get;
			private set;
		}
	}
}
