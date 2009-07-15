using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JdSoft.Apple.Apns.Notifications
{
	/// <summary>
	/// Notification object
	/// </summary>
	public class Notification
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public Notification()
		{
			DeviceToken = null;
			Badge = null;
			Sound = null;
			Alert = new NotificationAlert();
			Custom = new Dictionary<string, object[]>();
		}

		/// <summary>
		/// Copies the Device Token to a byte array buffer
		/// </summary>
		/// <param name="buffer">Array to Copy to</param>
		/// <param name="start">Starting Index to Copy to in the buffer</param>
		public void CopyDeviceTokenToBuffer(ref byte[] buffer, int start)
		{
			int i = 0;
			int b = 0;

			while (i < DeviceToken.Length)
			{
				buffer[start + b] = Convert.ToByte(DeviceToken.Substring(i, 2), 16);
				i += 2;
				b++;
			}
		}

		/// <summary>
		/// Device Token represented in hex format without any spaces or dashes.  This should be 64 characters long.
		/// </summary>
		public string DeviceToken
		{
			get;
			set;
		}

		/// <summary>
		/// Badge count to send to Device.  If null, no badge will be sent.
		/// </summary>
		public int? Badge
		{
			get;
			set;
		}

		/// <summary>
		/// Sound file to Send to Device.  If null or empty, no sound will be sent.
		/// </summary>
		public string Sound
		{
			get;
			set;
		}

		/// <summary>
		/// Alert to send to Device.  If no properties are set within the Alert object, it will not be sent.
		/// </summary>
		public NotificationAlert Alert
		{
			get;
			set;
		}

		/// <summary>
		/// Custom attributes to include in the payload.
		/// </summary>
		public Dictionary<string, object[]> Custom
		{
			get;
			set;
		}

		private bool IsNumber(object obj)
		{
			return (obj is short || obj is int || obj is long);
		}

		private bool BuildPayload(ref StringBuilder payload)
		{
			bool valid = false;

			payload.Append("{\"aps\":{");


			if (Badge.HasValue && Badge.Value >= 0)
			{
				payload.Append(string.Format("\"badge\":{0},", Badge));
				valid = true;
			}

			if (!string.IsNullOrEmpty(Sound))
			{
				payload.Append(string.Format("\"sound\":\"{0}\",", Sound));
				valid = true;
			}

			if (!Alert.IsEmpty)
			{
				//payload.Append(string.Format("\"alert\":\"{0}\",", Alert.ToString()));
				payload.Append("\"alert\":");

				if (string.IsNullOrEmpty(Alert.ActionLocalizedKey)
					&& string.IsNullOrEmpty(Alert.LocalizedKey)
					&& Alert.LocalizedArgs == null)
				{
					//Ok to send just body
					payload.Append(string.Format("\"{0}\"", Alert.Body));
				}
				else
				{
					//Send whole json
					payload.Append("{");

					if (!string.IsNullOrEmpty(Alert.LocalizedKey))
						payload.Append(string.Format("\"loc-key\":\"{0}\",", Alert.LocalizedKey));

					if (!string.IsNullOrEmpty(Alert.ActionLocalizedKey))
						payload.Append(string.Format("\"action-loc-key\":\"{0}\",", Alert.ActionLocalizedKey));

					if (Alert.LocalizedArgs != null && Alert.LocalizedArgs.Length > 0)
					{
						payload.Append("\"loc-args\":[");

						foreach (object locArg in Alert.LocalizedArgs)
							if (locArg is int || locArg is long || locArg is short)
								payload.Append(string.Format("{0},",locArg));
							else 
								payload.Append(string.Format("\"{0}\",", locArg));

						payload.Remove(payload.Length - 1, 1);
						payload.Append("],");
					}

					payload.Remove(payload.Length - 1, 1);
					payload.Append("}");
				}

				payload.Append(",");
				valid = true;
			}

			//Now generate json for the custom attributes
			foreach (string customKey in Custom.Keys)
			{
				if (Custom[customKey] != null)
				{
					payload.Append(string.Format("\"{0}\":", customKey));

					if (Custom[customKey].Length > 1)
					{
						payload.Append("[");

						foreach (object custListItem in Custom[customKey])
						{
							if (custListItem is int || custListItem is long || custListItem is short)
								payload.Append(string.Format("{0},", custListItem));
							else
								payload.Append(string.Format("\"{0}\",", custListItem));
						}
				
						//Get rid of the trailing comma
						payload.Remove(payload.Length - 1, 1);
						payload.Append("]");
					}
					else
					{
						if (Custom[customKey][0] is int || Custom[customKey][0] is long || Custom[customKey][0] is short)
							payload.Append(string.Format("{0},", Custom[customKey][0]));
						else
							payload.Append(string.Format("\"{0}\",", Custom[customKey][0]));
					}

					payload.Append(",");
				}
			}

			//remove the trailing comma
			if (payload.Length > 1)
				payload.Remove(payload.Length - 1, 1);

			payload.Append("}}");

			return valid;
		}

		/// <summary>
		/// Generates a Notification in the specified buffer ready to be sent to Apple
		/// </summary>
		/// <param name="buffer">Buffer to copy the notification to</param>
		/// <returns>If true, the Payload was generated successfully</returns>
		public bool Build(ref byte[] buffer)
		{
			if (buffer.Length != 293)
				return false;

			StringBuilder payload = new StringBuilder(256);

			if (BuildPayload(ref payload))
			{
				buffer[0] = 0; //Command byte
				buffer[1] = 0; //Device token len
				buffer[2] = 32; //Device token len

				//Copy device token in
				this.CopyDeviceTokenToBuffer(ref buffer, 3);
				//DeviceToken.CopyTo(buffer 3);

				//Get the raw bytes of the length	
				byte[] payloadLenArr = BitConverter.GetBytes(Convert.ToInt16(payload.Length));

				//Must be in big endian order
				if (BitConverter.IsLittleEndian)
					Array.Reverse(payloadLenArr);

				//Copy the payload len to the buffer
				payloadLenArr.CopyTo(buffer, 35);

				//Check for too long of a payload and catch it so we don't have to let
				// Apple deal with it instead
				if (payload.Length > 256)
					throw new NotificationLengthException(this);

				//Encode the payload into ascii, to the buffer
				Encoding.ASCII.GetBytes(payload.ToString(), 0, payload.Length, buffer, 37);

				return true;
			}
			
			return false;
		}

		/// <summary>
		/// Outputs the Payload's raw JSON generated by the Notification
		/// </summary>
		/// <returns>Raw JSON</returns>
		public override string ToString()
		{
			StringBuilder payload = new StringBuilder(256);

			BuildPayload(ref payload);

			return payload.ToString();	
		}
	}
}
