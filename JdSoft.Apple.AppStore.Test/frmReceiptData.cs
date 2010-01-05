using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace JdSoft.Apple.AppStore.Test
{
	public partial class frmReceiptData : Form
	{

		public frmReceiptData()
		{
			InitializeComponent();
		}

		private void frmReceiptData_Load(object sender, EventArgs e)
		{
		}

		private void buttonOk_Click(object sender, EventArgs e)
		{
			string receiptData = this.textReceiptData.Text;

			if (!string.IsNullOrEmpty(receiptData))
			{
				//New verification instance
				Receipt receipt = ReceiptVerification.GetReceipt(this.checkSandbox.Checked, receiptData);

				//Do the actual verification
				// this makes the https post to itunes verification servers
				if (ReceiptVerification.IsReceiptValid(receipt))
					AppendText("iTunes Receipt Verification: OK!");
				else
					AppendText("iTunes Receipt Verification: Failed!");

				AppendText(string.Empty);
	
				//Spit out the info
				if (receipt != null)
				{
					AppendText("RECEIPT DATA:");
					//AppendText(string.Format("  AppItemId: {0}", receipt.ProjectId));
					//AppendText(string.Format("  Bid: {0}", receipt.Bid));
					AppendText(string.Format("  Bvrs: {0}", receipt.Bvrs));
					AppendText(string.Format("  OriginalPurchaseDate: {0}", receipt.OriginalPurchaseDate));
					AppendText(string.Format("  OriginalTransactionId: {0}",receipt.OriginalTransactionId));
					AppendText(string.Format("  ProductId: {0}", receipt.ProductId));
					AppendText(string.Format("  PurchaseDate: {0}", receipt.PurchaseDate));
					AppendText(string.Format("  Quantity: {0}", receipt.Quantity));
					//AppendText(string.Format("  Timestamp: {0}", receipt.Timestamp));
					AppendText(string.Format("  TransactionId: {0}", receipt.TransactionId));
					//AppendText(string.Format("  VerifyStatus: {0}", receipt.VerifyStatus));
					//AppendText(string.Format("  VersionExternalIdentifier: {0}", receipt.VersionExternalIdentifier));
					AppendText(string.Empty);
				}
			}
			else
			{
				AppendText("No Receipt Data Entered");
			}


			AppendText(string.Empty);
		}

		private void buttonPaste_Click(object sender, EventArgs e)
		{
			this.textReceiptData.Text = Clipboard.GetText();
		}

		private void AppendText(string text)
		{
			this.textResults.AppendText(text + Environment.NewLine);
		}
	}
}
