using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.ServiceModel.Web;

namespace JdSoft.Apple.Apns.Notifications
{
	public class Notification
	{
		public string DeviceToken { get; set; }
		public NotificationPayload Payload { get; set; }
         /// <summary>
        /// The expiration date after which Apple will no longer store and forward this push notification.
        /// If no value is provided, an assumed value of one year from now is used.  If you do not wish
        /// for Apple to store and forward, set this value to Notification.DoNotStore.
        /// </summary>
        public DateTime? Expiration { get; set; }
		public const int DEVICE_TOKEN_BINARY_SIZE = 32;
		public const int DEVICE_TOKEN_STRING_SIZE = 64;
		public const int MAX_PAYLOAD_SIZE = 256;
        public static readonly DateTime DoNotStore = DateTime.MinValue;
        private static readonly DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public Notification()
		{
		    DeviceToken = string.Empty;
		    Payload = new NotificationPayload();
		}

		public Notification(string deviceToken)
		{
			if (!string.IsNullOrEmpty(deviceToken) && deviceToken.Length != DEVICE_TOKEN_STRING_SIZE)
				throw new BadDeviceTokenException(deviceToken);

			DeviceToken = deviceToken;
			Payload = new NotificationPayload();
		}

		public Notification(string deviceToken, NotificationPayload payload)
		{
			if (!string.IsNullOrEmpty(deviceToken) && deviceToken.Length != DEVICE_TOKEN_STRING_SIZE)
				throw new BadDeviceTokenException(deviceToken);

			DeviceToken = deviceToken;
			Payload = payload;
		}

		/// <summary>
		/// Object for storing state.  This does not affect the actual notification!
		/// </summary>
		public object Tag
		{
			get;
			set;
		}

		public override string ToString()
		{
			return Payload.ToJson();
		}

        public static String HexEncode(byte[] data)
        {
            int len = data.Length;

            if (len == 0)
            {
                throw new BadDeviceTokenException(@"");
            }

            StringBuilder hexString = new StringBuilder(len);

            foreach (byte tokenByte in data)
            {
                hexString.Append(tokenByte.ToString(@"x2"));
            }

            return hexString.ToString();
        }

		public byte[] ToBytes()
		{
            // Without reading the response which would make any identifier useful, it seems silly to
	            // expose the value in the object model, although that would be easy enough to do. For
	            // now we'll just use zero.
	            int identifier = 0;
                byte[] identifierBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(identifier));
	
	            // APNS will not store-and-forward a notification with no expiry, so set it one year in the future
	            // if the client does not provide it.
	            int expiryTimeStamp = -1;
	            if (Expiration != DoNotStore)
	            {
	                DateTime concreteExpireDateUtc = (Expiration ?? DateTime.UtcNow.AddMonths(1)).ToUniversalTime();
	                TimeSpan epochTimeSpan = concreteExpireDateUtc - UNIX_EPOCH;
	                expiryTimeStamp = (int)epochTimeSpan.TotalSeconds;
	            }

	            byte[] expiry = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(expiryTimeStamp));


			byte[] deviceToken = new byte[DeviceToken.Length / 2];
			for (int i = 0; i < deviceToken.Length; i++)
				deviceToken[i] = byte.Parse(DeviceToken.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);

			if (deviceToken.Length != DEVICE_TOKEN_BINARY_SIZE)
				throw new BadDeviceTokenException(DeviceToken);
			

			byte[] deviceTokenSize = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Convert.ToInt16(deviceToken.Length)));

			byte[] payload = Encoding.UTF8.GetBytes(Payload.ToJson());
			if (payload.Length > MAX_PAYLOAD_SIZE)
			{
				int newSize = Payload.Alert.Body.Length - (payload.Length - MAX_PAYLOAD_SIZE);
				if (newSize > 0)
				{
					Payload.Alert.Body = Payload.Alert.Body.Substring(0, newSize);
					payload = Encoding.UTF8.GetBytes(Payload.ToString());
				}
				else
				{
					do
					{
						Payload.Alert.Body = Payload.Alert.Body.Remove(Payload.Alert.Body.Length - 1);
						payload = Encoding.UTF8.GetBytes(Payload.ToString());
					}
					while (payload.Length > MAX_PAYLOAD_SIZE && !string.IsNullOrEmpty(Payload.Alert.Body));
				}

				if (payload.Length > MAX_PAYLOAD_SIZE)
					throw new NotificationLengthException(this);
			}
			byte[] payloadSize = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Convert.ToInt16(payload.Length)));

			int bufferSize = sizeof(Byte) + deviceTokenSize.Length + deviceToken.Length + payloadSize.Length + payload.Length;
			byte[] buffer = new byte[bufferSize];

            List<byte[]> notificationParts = new List<byte[]>();

            notificationParts.Add(new byte[] { 0x01 }); // Enhanced notification format command
            notificationParts.Add(identifierBytes);
            notificationParts.Add(expiry);
            notificationParts.Add(deviceTokenSize);
            notificationParts.Add(deviceToken);
            notificationParts.Add(payloadSize);
            notificationParts.Add(payload);

            return BuildBufferFrom(notificationParts);
		}

        private byte[] BuildBufferFrom(IList<byte[]> bufferParts)
        {
            int bufferSize = 0;
            for (int i = 0; i < bufferParts.Count; i++)
                bufferSize += bufferParts[i].Length;

            byte[] buffer = new byte[bufferSize];
            int position = 0;
            for (int i = 0; i < bufferParts.Count; i++)
            {
                byte[] part = bufferParts[i];
                Buffer.BlockCopy(bufferParts[i], 0, buffer, position, part.Length);
                position += part.Length;
            }
            return buffer;
        }
	}
}
