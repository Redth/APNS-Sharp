using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JdSoft.Apple.Apns.Notifications;

namespace Tests.Notifications
{
	[TestClass]
	public class NotificationCreationTest
	{
		[TestMethod]
		public void NewNotificationTest()
		{
			var n = new Notification("", new NotificationPayload("This is a test", 9, null));

			Assert.IsNotNull(n);
		}
	}
}
