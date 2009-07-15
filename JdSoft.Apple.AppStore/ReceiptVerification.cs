using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JdSoft.Apple.AppStore
{
	public class ReceiptVerification
	{
		#region Constants
		private Regex rxResponse = new Regex("\\\"(signing-status|status)\\\"\\s{0,}(:|=)\\s{0,}\\\"{0,1}([0]|-42351){1}\\\"{0,1}", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private Regex rxPairs = new Regex("\\\"(?<jsonKey>[^\\\"]+)\\\"[ ]{0,}(:|=)[ ]{0,}\\\"(?<jsonVal>[^\\\"]+)\\\"", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
		private Regex rxPurchaseInfo = new Regex("\\\"purchase-info\\\"\\s{0,}(:|=)\\s{0,}\\\"(?<pinfo>[^\\\"]+)\\\"", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private const string urlSandbox = "https://sandbox.itunes.apple.com/verifyReceipt";
		private const string urlProduction = "https://buy.itunes.apple.com/verifyReceipt";
		#endregion

		#region Constructors
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sandbox">If true, Default Receipt Verification Sandbox Url will be used, otherwise Production Url will be used</param>
		/// <param name="receiptData">Raw string data of the Receipt that the iPhone returned</param>
		public ReceiptVerification(bool sandbox, string receiptData)
		{
			TransactionIsValid = false;
			Receipt = null;

			Url = sandbox ? urlSandbox : urlProduction;
			ReceiptData = receiptData;
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="url">Receipt Verification Url to Use</param>
		/// <param name="receiptData">Raw string data of the Receipt that the iPhone returned</param>
		public ReceiptVerification(string url, string receiptData)
		{
			TransactionIsValid = false;
			Receipt = null;

			Url = url;
			ReceiptData = receiptData;
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets the Receipt Verification Url being Used
		/// </summary>
		public string Url
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the Response after the Verify method is called
		/// </summary>
		public string Response
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the Raw iPhone Receipt data string being used
		/// </summary>
		public string ReceiptData
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the Validity of the Transaction.  This property will always be false before Verify is called.
		/// </summary>
		public bool TransactionIsValid
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the resulting Receipt object after the Verify method is called.  The receipt is just an object representation of the Receipt Data passed in.
		/// </summary>
		public Receipt Receipt
		{
			get;
			private set;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Sends the ReceiptData to the Verification Url to be verified.
		/// </summary>
		/// <returns>If true, the Receipt Verification Server indicates a valid transaction response</returns>
		public bool Verify()
		{
			//Receipt is for before it's verified
			Receipt = parseReceipt(ReceiptData);

			try
			{
				//Receipt needs to be base64 encoded			
				string receipt64 = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(ReceiptData));

				//Need to encase the received receipt into a JSON container that the verifyReceipt Url likes
				string json = string.Format("{{\"receipt-data\":\"{0}\"}}", receipt64);

				System.Net.WebRequest wr = System.Net.WebRequest.Create(Url);
				wr.ContentType = "text/plain";
				wr.Method = "POST";
				//wr.Headers.Add("content-encoding", "ASCII");

				System.IO.StreamWriter sw = new System.IO.StreamWriter(wr.GetRequestStream());
				sw.Write(json);
				sw.Flush();
				sw.Close();

				System.Net.WebResponse wresp = wr.GetResponse();
				System.IO.StreamReader sr = new System.IO.StreamReader(wresp.GetResponseStream());
				Response = sr.ReadToEnd();
				sr.Close();

				//Parse out the transaction from the response
				TransactionIsValid = rxResponse.IsMatch(Response);
			}
			catch { TransactionIsValid = false; }

			//Return if it's a 'valid' response
			return TransactionIsValid;
		}
		#endregion

		#region Private Methods
		private Receipt parseReceipt(string receiptData)
		{
			Receipt rcpt = null;

			Match match = rxPurchaseInfo.Match(receiptData);

			if (match != null && match.Success && match.Groups["pinfo"] != null)
			{
				string purchaseInfo = string.Empty;

				try { purchaseInfo = System.Text.Encoding.ASCII.GetString(Convert.FromBase64String(match.Groups["pinfo"].Value)); }
				catch { }

				if (!string.IsNullOrEmpty(purchaseInfo))
				{
					rcpt = new Receipt();
					rcpt.Quantity = 1;
					rcpt.Timestamp = DateTime.Now;
					rcpt.VersionExternalIdentifier = string.Empty;
					rcpt.Bvrs = string.Empty;

					MatchCollection matches = rxPairs.Matches(purchaseInfo);

					foreach (Match pair in matches)
					{
						if (pair.Groups["jsonKey"] != null && pair.Groups["jsonVal"] != null)
						{
							string key = pair.Groups["jsonKey"].Value.ToLower().Trim();
							string val = pair.Groups["jsonVal"].Value.Trim();

							switch (key)
							{
								case "transaction_id":
								case "transaction-id":
									rcpt.TransactionId = val;
									break;
								case "item_id":
								case "app_item_id":
								case "item-id":
								case "app-item-id":
									rcpt.AppItemId = val;
									break;
								case "product_id":
								case "product-id":
									rcpt.ProductId = val;
									break;
								case "bid":
									rcpt.Bid = val;
									break;
								case "bvrs":
									rcpt.Bvrs = val;
									break;
								case "original_transaction_id":
								case "original-transaction-id":
									rcpt.OriginalTransactionId = val;
									break;
								case "version_external_identifier":
								case "version-external-identifier":
									rcpt.VersionExternalIdentifier = val;
									break;
								case "purchase_date":
								case "purchase-date":
									if (val.Length > 20)
									{
										DateTime pDate = DateTime.Now;
										DateTime.TryParse(val.Substring(0, 19), out pDate);
										rcpt.PurchaseDate = pDate;
									}
									break;
								case "original_purchase_date":
								case "original-purchase-date":
									if (val.Length > 20)
									{
										DateTime opDate = DateTime.Now.AddYears(-11);
										DateTime.TryParse(val.Substring(0, 19), out opDate);
										rcpt.OriginalPurchaseDate = opDate;
									}
									break;
							}

						}
					}
				}
			}

			return rcpt;
		}
		#endregion
	}
}
