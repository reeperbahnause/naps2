using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using NAPS2.ImportExport;
using NAPS2.Scan;

namespace NAPS2.WinForms
{
    public partial class FAutoSaveSettings : FormBase
    {
        private readonly DialogHelper dialogHelper;

        private bool result;

        public FAutoSaveSettings(DialogHelper dialogHelper)
        {
            this.dialogHelper = dialogHelper;
            InitializeComponent();
        }

        protected override void OnLoad(object sender, EventArgs e)
        {
            if (ScanProfile.AutoSaveSettings != null)
            {
                txtFilePath.Text = ScanProfile.AutoSaveSettings.FilePath;
                cbPromptForFilePath.Checked = ScanProfile.AutoSaveSettings.PromptForFilePath;
                cbClearAfterSave.Checked = ScanProfile.AutoSaveSettings.ClearImagesAfterSaving;
                if (ScanProfile.AutoSaveSettings.Separator == SaveSeparator.FilePerScan)
                {
                    rdFilePerScan.Checked = true;
                }
                else if (ScanProfile.AutoSaveSettings.Separator == SaveSeparator.PatchT)
                {
                    rdSeparateByPatchT.Checked = true;
                }
                else if (ScanProfile.AutoSaveSettings.Separator == SaveSeparator.Barcode)
                {
                    rdSeparateByBarcode.Checked = true;
                }
                else
                {
                    rdFilePerPage.Checked = true;
                }

                /*
                 *
                 * 2of5 interleaved
                 * Code 39
                 * Code 93
                 * Code 128
                 * EAN 8
                 * EAN13
                 *
                */
                if (ScanProfile.AutoSaveSettings.BarcodeType != null && ScanProfile.AutoSaveSettings.BarcodeType != "")
                {
                    for (var i = 0; i < clbBarcodes.Items.Count; i++)
                    {
                        if (clbBarcodes.Items[i].ToString() == ScanProfile.AutoSaveSettings.BarcodeType)
                        {
                            clbBarcodes.SetItemChecked(i, true);
                        }
                    }
                }

                if (ScanProfile.AutoSaveSettings.BarcodeRegEx != null && ScanProfile.AutoSaveSettings.BarcodeRegEx != "")
                {
                    tbBarcodeRegEx.Text = ScanProfile.AutoSaveSettings.BarcodeRegEx;
                }

                if (ScanProfile.AutoSaveSettings.BarcodeIgnore != null && ScanProfile.AutoSaveSettings.BarcodeIgnore != "")
                {
                    tbBarcodeIgnore.Text = ScanProfile.AutoSaveSettings.BarcodeIgnore;
                }

                if (ScanProfile.AutoSaveSettings.RestParameter.Count > 0)
                {
                    dgRestParameter.Rows.Clear();
                    foreach (string param in ScanProfile.AutoSaveSettings.RestParameter)
                    {
                        string[] values = param.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                        if (values.Length == 2)
                        {
                            DataGridViewRow row = (DataGridViewRow)dgRestParameter.Rows[0].Clone();
                            row.Cells[0].Value = values[0];
                            row.Cells[1].Value = values[1];
                            dgRestParameter.Rows.Add(row);
                        }
                    }
                }
            }

            
            this.MinimumSize = new System.Drawing.Size(420, 600);
            this.MaximumSize = new System.Drawing.Size(420, 600);

            linkPatchCodeInfo.Top = 152;
            linkPatchCodeInfo.Left = 162;

            rdSeparateByBarcode.Top = 173;
            rdSeparateByBarcode.Left = 12;

            clbBarcodes.Top = 196;
            clbBarcodes.Left = 26;
            clbBarcodes.SelectionMode = SelectionMode.One;

            lblBarcodeRegEx.Top = 196;
            lblBarcodeRegEx.Left = 171;

            tbBarcodeRegEx.Top = 212;
            tbBarcodeRegEx.Left = 174;

            lblBarcodeIgnore.Top = 235;
            lblBarcodeIgnore.Left = 171;

            tbBarcodeIgnore.Top = 251;
            tbBarcodeIgnore.Left = 174;

            dgRestParameter.Top = 310;
            dgRestParameter.Left = 26;

            cbClearAfterSave.Top = 456;
            cbClearAfterSave.Left = 11;

            btnOK.Top = 451;
            btnOK.Left = 209;

            btnCancel.Top = 451;
            btnCancel.Left = 298;
            


            /*
            new LayoutManager(this)
                .Bind(txtFilePath).WidthToForm()
                .Bind(btnChooseFolder, btnOK, btnCancel).BottomToForm().RightToForm()
                .Activate();
            */
        }

        public bool Result => result;

        public ScanProfile ScanProfile { get; set; }

        private void SaveSettings()
        {
            List<string> restParams = new List<string>();

            foreach(DataGridViewRow row in dgRestParameter.Rows)
            {
                string value = (string)row.Cells[0].Value + "=" + (string)row.Cells[1].Value;
                if (value != "=")
                {
                    restParams.Add(value);
                }
            }

            ScanProfile.AutoSaveSettings = new AutoSaveSettings
            {
                FilePath = txtFilePath.Text,
                PromptForFilePath = cbPromptForFilePath.Checked,
                ClearImagesAfterSaving = cbClearAfterSave.Checked,
                Separator = rdFilePerScan.Checked ? SaveSeparator.FilePerScan
                          : rdSeparateByPatchT.Checked ? SaveSeparator.PatchT
                          : rdSeparateByBarcode.Checked ? SaveSeparator.Barcode
                          : SaveSeparator.FilePerPage,
                BarcodeType = clbBarcodes.CheckedItems[0].ToString(),
                BarcodeRegEx = tbBarcodeRegEx.Text,
                BarcodeIgnore = tbBarcodeIgnore.Text,
                RestParameter = restParams,
            };
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFilePath.Text) && !cbPromptForFilePath.Checked)
            {
                txtFilePath.Focus();
                return;
            }
            result = true;
            SaveSettings();
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnChooseFolder_Click(object sender, EventArgs e)
        {
            string savePath;
            if (dialogHelper.PromptToSavePdfOrImage(null, out savePath))
            {
                txtFilePath.Text = savePath;
            }
        }

        private void linkPlaceholders_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var form = FormFactory.Create<FPlaceholders>();
            form.FileName = txtFilePath.Text;
            if (form.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = form.FileName;
            }
        }

        private void linkPatchCodeInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(FBatchScan.PATCH_CODE_INFO_URL);
        }

        private void clbBarcodes_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
                for (int ix = 0; ix < clbBarcodes.Items.Count; ++ix)
                    if (e.Index != ix) clbBarcodes.SetItemChecked(ix, false);
        }
    }
}
