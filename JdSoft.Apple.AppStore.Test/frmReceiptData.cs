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
				ReceiptVerification verifier = new ReceiptVerification(this.checkSandbox.Text, receiptData);

				//Do the actual verification
				// this makes the https post to itunes verification servers
				if (verifier.Verify())
					AppendText("iTunes Receipt Verification: OK!");
				else
					AppendText("iTunes Receipt Verification: Failed!");

				AppendText(string.Empty);
				AppendText(string.Format("iTunes Receipt Verification Raw Response: {0}{1}", Environment.NewLine, verifier.Response));
				AppendText(string.Empty);
	
				//Spit out the info
				if (verifier.Receipt != null)
				{
					AppendText("RECEIPT DATA:");
					AppendText(string.Format("  AppItemId: {0}", verifier.Receipt.AppItemId));
					AppendText(string.Format("  Bid: {0}", verifier.Receipt.Bid));
					AppendText(string.Format("  Bvrs: {0}", verifier.Receipt.Bvrs));
					AppendText(string.Format("  OriginalPurchaseDate: {0}", verifier.Receipt.OriginalPurchaseDate));
					AppendText(string.Format("  OriginalTransactionId: {0}", verifier.Receipt.OriginalTransactionId));
					AppendText(string.Format("  ProductId: {0}", verifier.Receipt.ProductId));
					AppendText(string.Format("  PurchaseDate: {0}", verifier.Receipt.PurchaseDate));
					AppendText(string.Format("  Quantity: {0}", verifier.Receipt.Quantity));
					AppendText(string.Format("  Timestamp: {0}", verifier.Receipt.Timestamp));
					AppendText(string.Format("  TransactionId: {0}", verifier.Receipt.TransactionId));
					AppendText(string.Format("  VerifyStatus: {0}", verifier.Receipt.VerifyStatus));
					AppendText(string.Format("  VersionExternalIdentifier: {0}", verifier.Receipt.VersionExternalIdentifier));
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
