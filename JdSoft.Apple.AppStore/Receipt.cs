using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JdSoft.Apple.AppStore
{
	public class Receipt
	{
		public Receipt()
		{
			TransactionId = string.Empty;
			Timestamp = DateTime.Now;
			AppItemId = string.Empty;
			PurchaseDate = DateTime.MinValue;
			ProductId = string.Empty;
			Quantity = 0;
			OriginalPurchaseDate = DateTime.MinValue;
			OriginalTransactionId = string.Empty;
			Bid = string.Empty;
			Bvrs = string.Empty;
			VersionExternalIdentifier = string.Empty;
			VerifyStatus = string.Empty;
		}

		public string TransactionId
		{
			get;
			set;
		}

		public DateTime Timestamp
		{
			get;
			set;
		}

		public string AppItemId
		{
			get;
			set;
		}

		public DateTime PurchaseDate
		{
			get;
			set;
		}

		public string ProductId
		{
			get;
			set;
		}

		public int Quantity
		{
			get;
			set;
		}

		public DateTime OriginalPurchaseDate
		{
			get;
			set;
		}

		public string OriginalTransactionId
		{
			get;
			set;
		}

		public string Bid
		{
			get;
			set;
		}

		public string Bvrs
		{
			get;
			set;
		}

		public string VersionExternalIdentifier
		{
			get;
			set;
		}

		public string VerifyStatus
		{
			get;
			set;
		}
	}
}
