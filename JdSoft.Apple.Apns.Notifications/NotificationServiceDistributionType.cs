using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JdSoft.Apple.Apns.Notifications
{
	/// <summary>
	/// Method used to load balance Notification Sending over the Apns Connections
	/// </summary>
	public enum NotificationServiceDistributionType
	{
		/// <summary>
		/// Loops through all connections in sequential order to ensure completely even distribution
		/// </summary>
		Sequential,
		/// <summary>
		/// Randomly chooses a connection to use
		/// </summary>
		Random
	}
}
