using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using log4net;
using StegImageUI.Extensions;

namespace StegImageUI.Helpers
{
    public class GifHelper
    {
        protected static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Private methods

        /// <summary>
        /// Convert an indexed image (Gif) to a non-indexed image.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private static Bitmap createNonIndexedImage(Image image)
        {
            var indexedBmp = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics gfx = Graphics.FromImage(indexedBmp))
                gfx.DrawImage(image, 0, 0);

            return indexedBmp;
        }

        /// <summary>
        /// Write bit on position: ((currentVal >> pos) & 1) == 1;
        /// </summary>
        /// <param name="currentVal"></param>
        /// <param name="bit"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private static byte getNewValForComponent(byte currentVal, bool bit, short position)
        {
            // Check the bit on position 1, true = 1, false = 0
            bool chkBit = ((currentVal >> position) & 1) == 1;
            // Write only if different
            if (chkBit != bit)
                currentVal ^= (byte)(1 << position);

            return currentVal;
        }

        /// <summary>
        /// It contains the steganographic data structure
        /// </summary>
        public struct StegStruct
        {
            public int SecurityLevel;
            public int TotalSize;
            public BitArray ContentFull;
            public bool IsFile;
            public int ContentSize;
            public BitArray Content;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Return the available slots for embedding.
        /// </summary>
        /// <param name="GifFilePath"></param>
        /// <returns></returns>
        public static int GetAvailableSlots(string GifFilePath)
        {
            Bitmap bitmap = new Bitmap(GifFilePath);
            var pal = bitmap.Palette;
            var palLen = pal.Entries.Length;
            // GIF: palette is 256 colors by 3 components by 2 bits
            return palLen * 3 * 2;
        }

        public static void StegBinary(string GifFilePath, BitArray Bits)
        {
            Bitmap bitmap = new Bitmap(GifFilePath);
            var pal = bitmap.Palette;
            int bitsLen = Bits.Length;
            int currentBit = 0;
            bool canIterate = true;
            int i = 0;
            while ((i < pal.Entries.Length) && canIterate)
            {
                // Red component
                byte compR = pal.Entries[i].R;
                bool bit = Bits[currentBit];
                // Write the new value in position 0
                byte newVal = getNewValForComponent(compR, bit, 0);

                currentBit++;
                canIterate = currentBit < bitsLen;
                // Insert bit in position 1 if any
                if (canIterate)
                {
                    // Get the current bit
                    bit = Bits[currentBit];
                    // Write the new value in position 1
                    newVal = getNewValForComponent(newVal, bit, 1);
                    currentBit++;
                    canIterate = currentBit < bitsLen;
                }
                // Set the new value for the red component
                compR = newVal;
                // Green component
                byte compG = 0;
                if (canIterate)
                {
                    compG = pal.Entries[i].G;
                    bit = Bits[currentBit];
                    // Write the new value in position 0
                    newVal = getNewValForComponent(compG, bit, 0);

                    currentBit++;
                    canIterate = currentBit < bitsLen;
                    // Insert bit in position 1 if any
                    if (canIterate)
                    {
                        // Get the current bit
                        bit = Bits[currentBit];
                        // Write the new value in position 1
                        newVal = getNewValForComponent(newVal, bit, 1);
                        currentBit++;
                        canIterate = currentBit < bitsLen;
                    }
                    // Set the new value for the red component
                    compG = newVal;
                }
                // Blue component
                byte compB = 0;
                if (canIterate)
                {
                    compB = pal.Entries[i].B;
                    bit = Bits[currentBit];
                    // Write the new value in position 0
                    newVal = getNewValForComponent(compB, bit, 0);

                    currentBit++;
                    canIterate = currentBit < bitsLen;
                    // Insert bit in position 1 if any
                    if (canIterate)
                    {
                        // Get the current bit
                        bit = Bits[currentBit];
                        // Write the new value in position 1
                        newVal = getNewValForComponent(newVal, bit, 1);
                        currentBit++;
                        canIterate = currentBit < bitsLen;
                    }
                    // Set the new value for the red component
                    compB = newVal;
                }
                pal.Entries[i] = Color.FromArgb(compR, compG, compB);
                i++;
            }

            bitmap.Palette = pal;

            var flPath = System.IO.Path.GetDirectoryName(GifFilePath);
            var filename = GifFilePath.Remove(0, string.Concat(flPath, "\\").Length);
            var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var newFileNameAndPath = $@"{docPath}\StegImageUI\steg_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{filename}";
            bitmap.Save(newFileNameAndPath, ImageFormat.Gif);
        }

        private static BitArray readAllBitArray(Bitmap bitmap, int bits2Read)
        {
            var bTemp = new bool[bits2Read];
            int iter = 0;
            bool canIterate = true;
            var pal = bitmap.Palette;
            int i = 0;
            while ((i < pal.Entries.Length) && canIterate)
            {
                // Red component
                byte currentVal = pal.Entries[i].R;
                var bit2Check = 1;
                bTemp[iter] = (currentVal & bit2Check) == bit2Check;
                iter++;
                // check if still iterating
                canIterate = iter < bits2Read;
                // Read  the 2nd LSB
                if (canIterate)
                {
                    // Bit to check at position 1 => value for logic AND is 2
                    bit2Check = 2;
                    bTemp[iter] = (currentVal & bit2Check) == bit2Check;
                    iter++;
                    canIterate = iter < bits2Read;
                }
                // Green component
                if (canIterate)
                {
                    currentVal = pal.Entries[i].G;
                    bit2Check = 1;
                    bTemp[iter] = (currentVal & bit2Check) == bit2Check;
                    iter++;
                    // check if still iterating
                    canIterate = iter < bits2Read;
                    // Read  the 2nd LSB
                    if (canIterate)
                    {
                        // Bit to check at position 1 => value for logic AND is 2
                        bit2Check = 2;
                        bTemp[iter] = (currentVal & bit2Check) == bit2Check;
                        iter++;
                        canIterate = iter < bits2Read;
                    }
                }
                // Blue component
                if (canIterate)
                {
                    currentVal = pal.Entries[i].B;
                    bit2Check = 1;
                    bTemp[iter] = (currentVal & bit2Check) == bit2Check;
                    iter++;
                    // check if still iterating
                    canIterate = iter < bits2Read;
                    // Read  the 2nd LSB
                    if (canIterate)
                    {
                        // Bit to check at position 1 => value for logic AND is 2
                        bit2Check = 2;
                        bTemp[iter] = (currentVal & bit2Check) == bit2Check;
                        iter++;
                        canIterate = iter < bits2Read;
                    }
                }
                i++;
            }
            return new BitArray(bTemp);
        }

        private static BitArray readBitArrayData(BitArray bits, int startIdx, int length)
        {
            var bTmp = new bool[length];
            for (int i = 0; i < length; i++)
                bTmp[i] = bits[startIdx + i];

            var baRet = new BitArray(bTmp);
            return baRet;
        }

        public static StegStruct UnstegBinary(string GifFilePath)
        {
            StegStruct ssRet = new StegStruct();
            Bitmap bitmap = new Bitmap(GifFilePath);
            // Content header length
            int headerLen = 32;
            // Get the heder to count the bits to read
            BitArray baTotLength;
            try
            {
                baTotLength = readAllBitArray(bitmap, headerLen);
            }
            catch (Exception ex)
            {
                _log.Debug(ex.Message);
                throw new Exception("No data embedded in this image or the image is corrupted.");
            }
            // If data is correct, continue
            int contentLen = baTotLength.ToInteger();
            // Total bits to read include the header
            int totalBits = headerLen + baTotLength.ToInteger();
            BitArray baFullContent;
            try
            {
                baFullContent = readAllBitArray(bitmap, totalBits);
            }
            catch (Exception ex)
            {
                _log.Debug(ex.Message);
                throw new Exception("No data embedded in this image or the image is corrupted.");
            }
            var contSize = readBitArrayData(baFullContent, 0, 32);
            var baContent = readBitArrayData(baFullContent, 32, contentLen);
            ssRet.ContentSize = contentLen;
            ssRet.Content = baContent;
            ssRet.TotalSize = totalBits;
            ssRet.ContentFull = baFullContent;

            return ssRet;
        } 

        #endregion

    }
}
