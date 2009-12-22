using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JdSoft.Apple.Apns.Notifications
{
	public class NotificationException : Exception
	{
		public NotificationException()
			: base()
		{
		}

		public NotificationException(int code, string message)
			: base(message)
		{
			this.Code = code;
		}

		public int Code
		{
			get;
			set;
		}
	}
}
