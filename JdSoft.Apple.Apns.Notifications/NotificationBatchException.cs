using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JdSoft.Apple.Apns.Notifications
{
	public class NotificationBatchException : Exception
	{
		internal NotificationBatchException(List<NotificationDeliveryError> errors)
			: base(String.Format("There were delivery problems with {0} notifications in batch.  See the DeliveryErrors property for details.", errors.Count))
		{
			DeliveryErrors = errors.AsReadOnly();
		}

		public IList<NotificationDeliveryError> DeliveryErrors { get; private set; }
	}
}
