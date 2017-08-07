using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAPS2.Lang.Resources;
using NAPS2.Operation;
using NAPS2.Scan.Images;
using NAPS2.Util;
using ZXing;
using NAPS2.Config;
using NAPS2.Scan;
using System.Text.RegularExpressions;

namespace NAPS2.ImportExport
{
    public class ImportOperation : OperationBase
    {
        private readonly IScannedImageImporter scannedImageImporter;
        private readonly ThreadFactory threadFactory;
        private readonly ScannedImageRenderer scannedImageRenderer;
        private readonly IProfileManager profileManager;

        private bool cancel;
        private Thread thread;

        public ImportOperation(IScannedImageImporter scannedImageImporter, IProfileManager profileManager, ScannedImageRenderer scannedImageRenderer, ThreadFactory threadFactory)
        {
            this.scannedImageImporter = scannedImageImporter;
            this.profileManager = profileManager;
            this.scannedImageRenderer = scannedImageRenderer;
            this.threadFactory = threadFactory;

            ProgressTitle = MiscResources.ImportProgress;
            AllowCancel = true;
        }

        public bool Start(List<string> filesToImport, Action<ScannedImage> imageCallback)
        {
            bool oneFile = filesToImport.Count == 1;
            Status = new OperationStatus
            {
                MaxProgress = oneFile ? 0 : filesToImport.Count
            };
            cancel = false;

            thread = threadFactory.StartThread(() =>
            {
                Run(filesToImport, imageCallback, oneFile);
                GC.Collect();
                InvokeFinished();
            });
            return true;
        }

        private void Run(IEnumerable<string> filesToImport, Action<ScannedImage> imageCallback, bool oneFile)
        {
            
            foreach (var fileName in filesToImport)
            {
                try
                {
                    Status.StatusText = string.Format(MiscResources.ImportingFormat, Path.GetFileName(fileName));
                    InvokeStatusChanged();
                    var images = scannedImageImporter.Import(fileName, (i, j) =>
                    {
                        if (oneFile)
                        {
                            Status.CurrentProgress = i;
                            Status.MaxProgress = j;
                            InvokeStatusChanged();
                        }
                        return !cancel;
                    });

                    ScanProfile profile = profileManager.DefaultProfile;
                    
                    foreach (var img in images)
                    {
                        //Squeeze Barcode Separation
                        if (profile.AutoSaveSettings.Separator == SaveSeparator.Barcode)
                        {
                            IMultipleBarcodeReader multiReader = new BarcodeReader();
                            multiReader.Options.TryHarder = true;
                            if (profile.AutoSaveSettings.BarcodeType != null && profile.AutoSaveSettings.BarcodeType != "")
                            {
                                switch (profile.AutoSaveSettings.BarcodeType)
                                {
                                    case "2of5 interleaved":
                                        multiReader.Options.PossibleFormats = new List<BarcodeFormat>();
                                        multiReader.Options.PossibleFormats.Add(BarcodeFormat.ITF);
                                        break;
                                    case "Code 39":
                                        multiReader.Options.PossibleFormats = new List<BarcodeFormat>();
                                        multiReader.Options.PossibleFormats.Add(BarcodeFormat.CODE_39);
                                        break;
                                    case "Code 93":
                                        multiReader.Options.PossibleFormats = new List<BarcodeFormat>();
                                        multiReader.Options.PossibleFormats.Add(BarcodeFormat.CODE_93);
                                        break;
                                    case "Code 128":
                                        multiReader.Options.PossibleFormats = new List<BarcodeFormat>();
                                        multiReader.Options.PossibleFormats.Add(BarcodeFormat.CODE_128);
                                        break;
                                    case "EAN 8":
                                        multiReader.Options.PossibleFormats = new List<BarcodeFormat>();
                                        multiReader.Options.PossibleFormats.Add(BarcodeFormat.EAN_8);
                                        break;
                                    case "EAN13":
                                        multiReader.Options.PossibleFormats = new List<BarcodeFormat>();
                                        multiReader.Options.PossibleFormats.Add(BarcodeFormat.EAN_13);
                                        break;
                                }

                            }
                            var test = scannedImageRenderer.Render(img);
                            var barcodeResult = multiReader.DecodeMultiple(test);
                            if (barcodeResult != null)
                            {
                                foreach (var barcode in barcodeResult)
                                {
                                    if (profile.AutoSaveSettings.BarcodeRegEx != "")
                                    {
                                        Regex regex = new Regex(@"^" + profile.AutoSaveSettings.BarcodeRegEx + "$");
                                        Match match = regex.Match(barcode.Text);
                                        if (match.Success)
                                        {
                                            if (barcode.Text != profile.AutoSaveSettings.BarcodeIgnore)
                                            { 
                                                img.Barcode = barcode.Text;
                                                System.Diagnostics.Debug.WriteLine(barcode.BarcodeFormat + " = " + barcode.Text);
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        img.Barcode = barcode.Text;
                                        System.Diagnostics.Debug.WriteLine(barcode.BarcodeFormat + " = " + barcode.Text);
                                        break;
                                    }
                                }
                            }
                        }
                        imageCallback(img);
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorException(string.Format(MiscResources.ImportErrorCouldNot, Path.GetFileName(fileName)), ex);
                    InvokeError(string.Format(MiscResources.ImportErrorCouldNot, Path.GetFileName(fileName)), ex);
                }
                if (!oneFile)
                {
                    Status.CurrentProgress++;
                    InvokeStatusChanged();
                }
            }
            Status.Success = true;
        }

        public override void WaitUntilFinished()
        {
            thread.Join();
        }

        public override void Cancel()
        {
            cancel = true;
        }
    }
}
