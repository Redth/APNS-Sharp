using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace JdSoft.Apple.AppStore
{
	[Serializable()]
	public class Receipt
	{
		#region Constructor

		/// <summary>
		/// Creates the receipt from Apple's Response
		/// </summary>
		/// <param name="receipt"></param>
		public Receipt(string receipt)
		{
			JObject json = JObject.Parse(receipt);

			int status = -1;

			int.TryParse(json["status"].ToString(), out status);
			this.Status = status;

			// Receipt is actually a child
			json = (JObject)json["receipt"];

			
			this.OriginalTransactionId = json["original_transaction_id"].ToString();
			this.Bvrs = json["bvrs"].ToString();
			this.ProductId = json["product_id"].ToString();

			DateTime purchaseDate = DateTime.MinValue;
			if (DateTime.TryParseExact(json["purchase_date"].ToString().Replace(" Etc/GMT", string.Empty).Replace("\"", string.Empty).Trim(), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out purchaseDate))
				this.PurchaseDate = purchaseDate;

			DateTime originalPurchaseDate = DateTime.MinValue;
			if (DateTime.TryParseExact(json["original_purchase_date"].ToString().Replace(" Etc/GMT", string.Empty).Replace("\"", string.Empty).Trim(), "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out originalPurchaseDate))
				this.OriginalPurchaseDate = originalPurchaseDate;

			int quantity = 1;
			int.TryParse(json["quantity"].ToString(), out quantity);
			this.Quantity = quantity;

			this.BundleIdentifier = json["bid"].ToString();

			this.TransactionId = json["transaction_id"].ToString();
		} 

		#endregion Constructor

		#region Properties

		public string OriginalTransactionId
		{
			get;
			set;
		}

		public string Bvrs
		{
			get;
			set;
		}

		public string ProductId
		{
			get;
			set;
		}

		public DateTime? PurchaseDate
		{
			get;
			set;
		}

		public int Quantity
		{
			get;
			set;
		}

		public string BundleIdentifier
		{
			get;
			set;
		}

		public DateTime? OriginalPurchaseDate
		{
			get;
			set;
		}

		public string TransactionId
		{
			get;
			set;
		}

		public int Status
		{
			get;
			set;
		}

		#endregion Properties
	}
}
