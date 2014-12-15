using System;
using System.Collections.Generic;
using System.Text;

namespace JdSoft.Apple.Apns.Notifications
{
	public class NotificationDeliveryError
	{
		public NotificationDeliveryError(DeliveryErrorType type, Notification notification)
		{
			this.ErrorType = type;
			this.Notification = notification;
		}

		public NotificationDeliveryError(Exception exception, Notification notification)
		{
			this.ErrorType = DeliveryErrorType.Unknown;
			this.Exception = exception;
			this.Notification = notification;
		}

		public DeliveryErrorType ErrorType { get; private set; }
		public Notification Notification { get; private set; }
		public Exception Exception { get; private set; }

		public bool IsException
		{
			get { return Exception != null; }
		}
	}

	public enum DeliveryErrorType : byte
	{
		NoErrors = 0,
		ProcessingError = 1,
		MissingDeviceToken = 2,
		MissingTopic = 3,
		MissingPayload = 4,
		InvalidTokenSize = 5,
		InvalidTopicSize = 6,
		InvalidPayloadSize = 7,
		InvalidToken = 8,
		Unknown = 255,
	}
}
