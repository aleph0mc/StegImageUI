using StegImageUI.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using StegImageUI.Extensions;
using log4net;
using StegImageUI.ViewModels;
using System.Windows.Threading;
using System.IO.Compression;
using System.Windows.Controls.Primitives;
using StegImageUI.Controls;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace StegImageUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 32 bits for integer and 1 for bool
        private const int HEADER_BITS_LEN = 65;
        // For indexed images GIF or TIFF
        private const int HEADER_BITS_IDX_LEN = 32;

        private readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int _slots;
        private FilePathNotifier _fpn;

        #region Private methods

        private void setDataContext()
        {
            _fpn = new FilePathNotifier { FilePath = string.Empty, FilePathEmbed = String.Empty };

            grid.DataContext = _fpn;
        }

        private void getSlotsByImageType(string imagePath)
        {
            var trimPath = imagePath.ToLower().Trim();
            // Check image type
            if (trimPath.EndsWith(".jpg") || trimPath.EndsWith(".jpeg")) // JPEG
            {
                // Available slots not 0 - DCT values 0 won't be overwritten
                var dctCoeffs = JpegHelper.GetDctCoefficients(imagePath);
                // 2 bits for each dct coefficient (0 values are skpped)
                _slots = 2 * dctCoeffs.DctFullAc.Where(s => 0 != s).Count() - HEADER_BITS_LEN;
            }
            else if (trimPath.EndsWith(".bmp")) // BITMAP
            {
                _slots = BmpHelper.GetAvailableSlots(imagePath) - HEADER_BITS_LEN;
            }
            else if (trimPath.EndsWith(".png")) // PNG
            {
                _slots = PngHelper.GetAvailableSlots(imagePath) - HEADER_BITS_LEN;
            }
            else if (trimPath.EndsWith(".gif")) // GIF
            {
                sldSecLevel.Value = 1;
                _slots = GifHelper.GetAvailableSlots(imagePath) - HEADER_BITS_IDX_LEN;
            }
            else if (trimPath.EndsWith(".tif") || trimPath.EndsWith(".tiff")) // TIFF
            {
                sldSecLevel.Value = 1;
                _slots = TifHelper.GetAvailableSlots(imagePath) - HEADER_BITS_IDX_LEN;
            }
            if (_slots < 10)
            {
                tbxSlots.Text = tbxSlotsUsed.Text = tbxSlotsLeft.Text = "0";
                MessageBox.Show("Image not suitable for embedding content.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }

        private static void stegImageByImageType(string path, BitArray baBits, int securityLevel)
        {
            var trimPath = path.ToLower().Trim();
            // Check image type
            if (trimPath.EndsWith(".jpg") || trimPath.EndsWith(".jpeg")) // JPEG
                JpegHelper.StegBinary(path, baBits, securityLevel);
            else if (trimPath.EndsWith(".bmp")) // BITMAP
                BmpHelper.StegBinary(path, baBits, securityLevel);
            else if (trimPath.EndsWith(".png")) // PNG
                PngHelper.StegBinary(path, baBits, securityLevel);
            else if (trimPath.EndsWith(".gif")) // GIF
                GifHelper.StegBinary(path, baBits);
            else if (trimPath.EndsWith(".tif") || trimPath.EndsWith(".tiff")) // TIFF
                TifHelper.StegBinary(path, baBits, securityLevel);
            else if (!string.IsNullOrEmpty(trimPath))
            {
                MessageBox.Show("Wrong extension.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private static StegModel unstegImageByImageType(string path)
        {
            var sm = new StegModel();
            var trimPath = path.ToLower().Trim();
            // Check image type
            if (trimPath.EndsWith(".jpg") || trimPath.EndsWith(".jpeg")) // JPEG
            {
                var stegStruct = JpegHelper.UnstegBinary(path);
                sm.SecurityLevel = stegStruct.SecurityLevel;
                sm.TotalSize = stegStruct.TotalSize;
                sm.ContentFull = stegStruct.ContentFull;
                sm.IsFile = stegStruct.IsFile;
                sm.ContentSize = stegStruct.ContentSize;
                sm.Content = stegStruct.Content;
            }
            else if (trimPath.EndsWith(".bmp")) // BITMAP
            {
                var stegStruct = BmpHelper.UnstegBinary(path);
                sm.SecurityLevel = stegStruct.SecurityLevel;
                sm.TotalSize = stegStruct.TotalSize;
                sm.ContentFull = stegStruct.ContentFull;
                sm.IsFile = stegStruct.IsFile;
                sm.ContentSize = stegStruct.ContentSize;
                sm.Content = stegStruct.Content;
            }
            else if (trimPath.EndsWith(".png")) // PNG
            {
                var stegStruct = PngHelper.UnstegBinary(path);
                sm.SecurityLevel = stegStruct.SecurityLevel;
                sm.TotalSize = stegStruct.TotalSize;
                sm.ContentFull = stegStruct.ContentFull;
                sm.IsFile = stegStruct.IsFile;
                sm.ContentSize = stegStruct.ContentSize;
                sm.Content = stegStruct.Content;
            }
            else if (trimPath.EndsWith(".gif")) // GIF
            {
                var stegStruct = GifHelper.UnstegBinary(path);
                sm.SecurityLevel = stegStruct.SecurityLevel;
                sm.TotalSize = stegStruct.TotalSize;
                sm.ContentFull = stegStruct.ContentFull;
                sm.IsFile = stegStruct.IsFile;
                sm.ContentSize = stegStruct.ContentSize;
                sm.Content = stegStruct.Content;
            }
            else if (trimPath.EndsWith(".tif") || trimPath.EndsWith(".tiff")) // TIFF
            {
                var stegStruct = TifHelper.UnstegBinary(path);
                sm.SecurityLevel = stegStruct.SecurityLevel;
                sm.TotalSize = stegStruct.TotalSize;
                sm.ContentFull = stegStruct.ContentFull;
                sm.IsFile = stegStruct.IsFile;
                sm.ContentSize = stegStruct.ContentSize;
                sm.Content = stegStruct.Content;
            }

            else if (!string.IsNullOrEmpty(trimPath))
            {
                MessageBox.Show("Wrong extension.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return sm;
        }

        private bool checkCurrentSliderValue()
        {
            int secVal = (int)sldSecLevel.Value;
            var newVal = _slots / secVal;
            tbxSlots.Text = newVal.ToString("##,###0", CultureInfo.InvariantCulture).Replace(",", " ");
            var slotsUsed = int.Parse(tbxSlotsUsed.Text.Replace(" ", ""));
            var slotsLeft = newVal - slotsUsed;
            bool noSlots = newVal < slotsUsed;
            tbxSlotsLeft.Text = slotsLeft.ToString("##,###0", CultureInfo.InvariantCulture).Replace(",", " ");
            return noSlots;
        }

        private void computeSlots()
        {
            if ((_slots > 0) && checkCurrentSliderValue())
                MessageBox.Show("Message too long for this image.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private BitArray dataEncoding(BitArray baContent, bool isFile = false)
        {
            BitArray baToSteg;
            // Get the file extension
            bool isGif = txtStegFilePath.Text.ToLower().EndsWith(".gif");
            if (isGif)
            {
                BitArray baContlen = baContent.Length.ToBinary();
                baToSteg = baContlen.Append(baContent);
            }
            else
            {
                // The content type to encode string or file
                int secLevel = (int)sldSecLevel.Value;
                BitArray baSecLev = secLevel.ToBinary();
                BitArray baIsFile = new BitArray(new bool[] { isFile });
                BitArray baData = baSecLev.Append(baIsFile).Append(baContent);
                BitArray baDataLen = baData.Length.ToBinary();
                baToSteg = baDataLen.Append(baData);

            }
            // Current slots available 
            tbxSlotsUsed.Text = baToSteg.Length.ToString("##,###0", CultureInfo.InvariantCulture).Replace(",", " ");
            if (checkCurrentSliderValue())
            {
                MessageBox.Show("Message too long for this image.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }
            return baToSteg;
        }

        private string dataDecoding(StegModel stegModel)
        {
            // Log unsteg data
            _log.Debug("---------------------------------------------------------------------------------");
            _log.Debug("Unstegged data:");
            _log.Debug($"Content Size: {stegModel.ContentSize}");
            _log.Debug($"Steg Content: {stegModel.Content.ToDigitString()}");
            _log.Debug($"Full Size: {stegModel.TotalSize}");
            _log.Debug($"Full Content: {stegModel.ContentFull.ToDigitString()}");
            _log.Debug("---------------------------------------------------------------------------------");

            string msg;
            if (stegModel.IsFile)
            {
                var filename = string.IsNullOrEmpty(txtDecFileName.Text.Trim()) ? "file" : txtDecFileName.Text;
                var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var fileNameAndPath = $@"{docPath}\StegImageUI\Docs\steg_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{filename}";
                try
                {
                    msg = CompressionHelper.Unzip(stegModel.Content, true, fileNameAndPath);

                }
                catch (Exception ex)
                {
                    _log.Debug(ex.Message);
                    MessageBox.Show(ex.Message, "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Error);
                    msg = null;
                }
            }
            else
                try
                {
                    msg = CompressionHelper.Unzip(stegModel.Content);

                }
                catch (Exception ex)
                {
                    _log.Debug(ex.Message);
                    MessageBox.Show(ex.Message, "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Error);
                    msg = null;
                }

            return msg;
        }

        private void slotCounter()
        {
            // Check if image is GIF
            bool isGif = txtStegFilePath.Text.ToLower().Trim().EndsWith(".gif");

            BitArray baContent;
            if ((bool)chkStegFile.IsChecked)
                baContent = CompressionHelper.Zip(txtFile2Encode.Text, true);
            else
                baContent = CompressionHelper.Zip(txtMessage.Text);

            var chkEncLen = dataEncoding(baContent);
            if (null == chkEncLen) // Message too long
                return;

            var newVal = _slots / sldSecLevel.Value;
            var msgLen = isGif ? chkEncLen.Length - HEADER_BITS_IDX_LEN : chkEncLen.Length - HEADER_BITS_LEN;
            var left = newVal - msgLen;
            tbxSlotsUsed.Text = msgLen.ToString("##,###0", CultureInfo.InvariantCulture).Replace(",", " ");
            tbxSlotsLeft.Text = left.ToString("##,###0", CultureInfo.InvariantCulture).Replace(",", " ");
        }

        /// <summary>
        /// Can be a text or a file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isFile"></param>
        private void performSteg(string path, bool isFile = false)
        {
            if (Validation.GetHasError(txtStegFilePath) || Validation.GetHasError(txtFile2Encode))
            {
                MessageBox.Show("The form has errors. Please fix them before continuing.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            BitArray baBits;
            // Content to be encoded
            if (isFile)
            {
                var baFile = CompressionHelper.Zip(txtFile2Encode.Text, true);
                baBits = dataEncoding(baFile, true);
            }
            else
            {
                var baMsg = CompressionHelper.Zip(txtMessage.Text);
                baBits = dataEncoding(baMsg);
            }
            // Start process to embed image
            if (null != baBits)
            {
                startOrStopAnimation(aGifControlExe, isStart: true);

                // Log steg data
                _log.Debug("---------------------------------------------------------------------------------");
                _log.Debug("Data to steg:");
                _log.Debug($"Full Size: {baBits.Count}");
                _log.Debug($"Full Content: {baBits.ToDigitString()}");
                _log.Debug("---------------------------------------------------------------------------------");

                try
                {
                    stegImageByImageType(path, baBits, (int)sldSecLevel.Value);
                    startOrStopAnimation(aGifControlExe, isStart: false);
                    MessageBox.Show("Content successfully embedded.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Information);
                    string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string folder = $@"{docPath}\StegImageUI\";
                    Process.Start(folder);
                }
                catch (Exception ex)
                {
                    _log.Debug(ex.Message);
                    startOrStopAnimation(aGifControlExe, isStart: false);
                    System.Windows.MessageBox.Show("Content successfully embedded.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        /// <summary>
        /// If it is a file, it will be saved in the host image directory
        /// </summary>
        /// <param name="path"></param>
        private void performUnsteg(string path)
        {
            if (Validation.GetHasError(txtStegFilePath) || Validation.GetHasError(txtFile2Encode))
            {
                MessageBox.Show("The form has errors. Please fix them before continuing.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            startOrStopAnimation(aGifControlExe, isStart: true);
            txtMessage.Dispatcher.Invoke(() => txtMessage.Text = string.Empty, DispatcherPriority.Background);
            txtMessage.Dispatcher.Invoke(() => txtMessage.IsEnabled = false, DispatcherPriority.Background);
            StegModel sm;
            try
            {
                sm = unstegImageByImageType(path);
            }
            catch (Exception ex)
            {
                _log.Debug(ex.Message);
                startOrStopAnimation(aGifControlExe, isStart: false);
                MessageBox.Show(ex.Message, "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // If operation performed correctly, continue
            var msg = dataDecoding(sm);
            // If msg is null => nothing decoded
            if (null == msg)
            {
                startOrStopAnimation(aGifControlExe, isStart: false);
                return;
            }
            // if it is content is file
            if (sm.IsFile)
            {
                startOrStopAnimation(aGifControlExe, isStart: false);
                MessageBox.Show($"File {txtDecFileName.Text} successfully retrieved.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Information);
                string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string folder = $@"{docPath}\StegImageUI\Docs\";
                Process.Start(folder);
            }
            else
            {
                // Show the message in textbox and stop animation
                txtMessage.Dispatcher.Invoke(() => txtMessage.Text = msg, DispatcherPriority.Background);
                txtMessage.Dispatcher.Invoke(() => txtMessage.IsEnabled = true, DispatcherPriority.Background);
                sldSecLevel.Dispatcher.Invoke(() => sldSecLevel.Value = sm.SecurityLevel, DispatcherPriority.Background);
                checkCurrentSliderValue();
                startOrStopAnimation(aGifControlExe, isStart: false);
                MessageBox.Show("Text successfully retrieved.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void startOrStopAnimation(AnimatedGIFControl aGifCtrl, bool isStart)
        {
            // Start animation
            aGifCtrl.Dispatcher.Invoke(() =>
            {
                if (isStart)
                    aGifCtrl.StartAnimate();
                else
                    aGifCtrl.StopAnimate();
                aGifCtrl.Visibility = isStart ? Visibility.Visible : Visibility.Hidden;
            }, DispatcherPriority.Background);
        }

        private void setFieldVisibility(bool visible, bool isGifOrTiff)
        {
            if (visible && !isGifOrTiff)
            {
                // set visibility of sec level
                lblSecLevel.Visibility = lblSecLevHi.Visibility = lblSecLevLow.Visibility = sldSecLevel.Visibility = (bool)rdEncode.IsChecked ? Visibility.Visible : Visibility.Hidden;

                computeSlots();
                txtMessage.IsEnabled = (bool)rdEncode.IsChecked && !(bool)chkStegFile.IsChecked;
            }
            else if (visible && isGifOrTiff)
            {
                // set visibility of sec level
                lblSecLevel.Visibility = lblSecLevHi.Visibility = lblSecLevLow.Visibility = sldSecLevel.Visibility = Visibility.Hidden;

                computeSlots();
                txtMessage.IsEnabled = (bool)rdEncode.IsChecked && !(bool)chkStegFile.IsChecked;
            }
            else
            {
                // set visibility of sec level
                lblSecLevel.Visibility = lblSecLevHi.Visibility = lblSecLevLow.Visibility = sldSecLevel.Visibility = Visibility.Hidden;
                txtMessage.IsEnabled = false;
                tbxSlots.Text = tbxSlotsLeft.Text = tbxSlotsUsed.Text = "0";
            }
            chkStegFile.IsEnabled = !isGifOrTiff;
        }

        private void slotControlVisibility(bool visibility)
        {
            lblSlots.Visibility = lblSlotsUsed.Visibility = lblSlotsLeft.Visibility =
                tbxSlots.Visibility = lblSecLevel.Visibility = lblSecLevLow.Visibility = lblSecLevHi.Visibility = sldSecLevel.Visibility =
                    tbxSlotsUsed.Visibility = tbxSlotsLeft.Visibility = visibility ? Visibility.Visible : Visibility.Hidden;
        }

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            string path = txtStegFilePath.Text;
            if (string.IsNullOrEmpty(path))
            {
                System.Windows.MessageBox.Show("Please, select an image.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool opSelEncode = grid.Children.OfType<System.Windows.Controls.RadioButton>().FirstOrDefault(r => (bool)r.IsChecked).Name.Contains("Encode");

            if (opSelEncode && (bool)chkStegFile.IsChecked && string.IsNullOrEmpty(txtFile2Encode.Text))
            {
                MessageBox.Show("Please, select a file to be embedded.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (opSelEncode && !(bool)chkStegFile.IsChecked && string.IsNullOrEmpty(txtMessage.Text))
            {
                MessageBox.Show("Please, insert a text.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!opSelEncode && (bool)chkStegFile.IsChecked && string.IsNullOrEmpty(txtDecFileName.Text))
            {
                MessageBox.Show("Please, insert the file name to be saved.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // DISASBLE BUTTON TO AVOID MULTIPLE CLICKS
            txtMessage.IsEnabled = btnExecute.IsEnabled = false;

            if (opSelEncode)
            {
                performSteg(path, (bool)chkStegFile.IsChecked);
                txtMessage.IsEnabled = (bool)chkStegFile.IsChecked;
            }
            else
                performUnsteg(path);

            // REENABLE BUTTON
            btnExecute.IsEnabled = true;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "BMP (*.bmp)|*.bmp|GIF (*.gif)|*.gif|JPEG (*.jpeg, *.jpg)|*.jpeg;*.jpg|PNG (*.png)|*.png|TIFF (*.tif; *.tiff)|*.tif;*.tiff";
            dialog.FilterIndex = 3;
            dialog.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            dialog.Title = "Please select an image to encrypt.";
            var result = dialog.ShowDialog();

            startOrStopAnimation(aGifControlBrowse, isStart: true);

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string path = dialog.FileName;
                txtMessage.Dispatcher.Invoke(() => txtStegFilePath.Text = path, DispatcherPriority.Background);
                // DOUBLE CHECK FOR VALIDATION
                txtStegFilePath.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty).UpdateSource();
                // If error, return
                if (Validation.GetHasError(txtStegFilePath))
                {
                    startOrStopAnimation(aGifControlBrowse, isStart: false);
                    return;
                }
                // get the slots available for the selected image
                getSlotsByImageType(path);

                setFieldVisibility((bool)rdEncode.IsChecked, path.ToLower().EndsWith(".gif"));
            }

            startOrStopAnimation(aGifControlBrowse, isStart: false);
        }

        private void rdEncode_Click(object sender, RoutedEventArgs e)
        {
            if (Validation.GetHasError(txtStegFilePath) || Validation.GetHasError(txtFile2Encode))
            {
                MessageBox.Show("The form has errors. Please fix them before continuing.", "StegImageUI", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            txtFile2Encode.Text = txtDecFileName.Text = txtMessage.Text = string.Empty;
            // Enabled after selecting the host image
            txtMessage.IsEnabled = !string.IsNullOrEmpty(txtStegFilePath.Text);
            chkStegFile.IsChecked = false;
            wpFile.Visibility = Visibility.Hidden;
            lblDecFileName.Visibility = txtDecFileName.Visibility = Visibility.Hidden;
            slotControlVisibility(true);
        }

        private void rdDecode_Click(object sender, RoutedEventArgs e)
        {
            chkStegFile.IsChecked = false;
            wpFile.Visibility = Visibility.Hidden;
            lblDecFileName.Visibility = txtDecFileName.Visibility = Visibility.Hidden;
            slotControlVisibility(false);
            txtFile2Encode.Text = txtDecFileName.Text = txtMessage.Text = string.Empty;
            txtMessage.IsEnabled = false;
        }

        private void txtMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtMessage.IsEnabled)
                slotCounter();
        }

        private void chkStegFile_OnClick(object sender, RoutedEventArgs e)
        {
            lblDecFileName.Visibility = txtDecFileName.Visibility = !(bool)chkStegFile.IsChecked ? Visibility.Hidden : ((bool)chkStegFile.IsChecked && (bool)rdDecode.IsChecked) ? Visibility.Visible : Visibility.Hidden;
            wpFile.Visibility = !(bool)chkStegFile.IsChecked ? Visibility.Hidden : ((bool)chkStegFile.IsChecked && (bool)rdEncode.IsChecked) ? Visibility.Visible : Visibility.Hidden;
            txtMessage.IsEnabled = !string.IsNullOrEmpty(txtStegFilePath.Text) && !Validation.GetHasError(txtStegFilePath) && !(bool)chkStegFile.IsChecked;
        }

        private void btnChooseFile_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Files |*.*";
            dialog.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            dialog.Title = "Please select a file to encrypt.";
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                txtFile2Encode.Dispatcher.Invoke(() => txtFile2Encode.Text = dialog.FileName, DispatcherPriority.Background);
                slotCounter();
            }
        }

        private void sldSecLevel_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (0 == _slots)
                return;

            computeSlots();
        }

        private void txtStegFilePath_LostFocus(object sender, RoutedEventArgs e)
        {
            txtStegFilePath.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            string path = txtStegFilePath.Text.Trim();
            if (!string.IsNullOrEmpty(path) && !Validation.GetHasError(txtStegFilePath))
            {
                getSlotsByImageType(path);
                setFieldVisibility(true, path.ToLower().EndsWith(".gif"));
                slotControlVisibility(true);
            }
            else
            {
                setFieldVisibility(false, path.ToLower().EndsWith(".gif"));
                chkStegFile.IsEnabled = false;
                slotControlVisibility(false);

            }
        }

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            // Prepare layout, hide some controls and set the data context
            lblDecFileName.Visibility = txtDecFileName.Visibility = aGifControlBrowse.Visibility = aGifControlExe.Visibility = wpFile.Visibility = Visibility.Hidden;
            lblSecLevel.Visibility = lblSecLevHi.Visibility = lblSecLevLow.Visibility = sldSecLevel.Visibility = Visibility.Hidden;
            _slots = 0;
            setDataContext();
        }
    }
}