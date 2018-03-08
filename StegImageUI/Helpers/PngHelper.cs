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
using log4net;
using StegImageUI.Extensions;

namespace StegImageUI.Helpers
{
    public class PngHelper
    {
        protected static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Private methods

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

        private static BitArray readAllBitArray(byte[] bytes, int startIterIncluded, int bits2Read, int secLevel = 1, bool oneBitReading = false)
        {
            var bTemp = new bool[bits2Read];
            int iter = 0;
            bool canIterate = true;
            // Read bits in position 0 then position 1
            while (canIterate)
            {
                if ((0 == startIterIncluded % secLevel) || oneBitReading)
                {
                    var currentVal = bytes[startIterIncluded];
                    // Bit to check at position 0 => value for logic AND is 1
                    var bit2Check = 1;
                    bTemp[iter] = (currentVal & bit2Check) == bit2Check;
                    iter++;
                    // check if still iterating
                    canIterate = iter < bits2Read;
                    // Read  the 2nd LSB
                    if (canIterate && !oneBitReading)
                    {
                        // Bit to check at position 1 => value for logic AND is 2
                        bit2Check = 2;
                        bTemp[iter] = (currentVal & bit2Check) == bit2Check;
                        iter++;
                        // check if still iterating
                        canIterate = iter < bits2Read;
                    }
                }
                startIterIncluded++;
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

        #endregion


        #region Public methods

        /// <summary>
        /// Return the available slots for embedding.
        /// </summary>
        /// <param name="PngFilePath"></param>
        /// <returns></returns>
        public static int GetAvailableSlots(string PngFilePath)
        {
            var bitmap = new Bitmap(PngFilePath);
            var imgHelper = new ImageHelper(bitmap);
            int byteCount = 0;
            imgHelper.LockBits();
            byteCount = imgHelper.Pixels.Length;
            imgHelper.UnlockBits();
            // Check slots availability
            // Count all bytes useful for embedding (2 bits per slot = byte)
            var slots = 2 * byteCount;
            return slots;
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


        public static void StegBinary(string PngFilePath, BitArray Bits, int SecurityLevel)
        {
            var bitmap = new Bitmap(PngFilePath);
            var imgHelper = new ImageHelper(bitmap);
            imgHelper.LockBits();
            var bytes = imgHelper.Pixels;
            var bitsLen = Bits.Length;
            // Count all bytes useful for embedding (2 bits per slot = byte)
            var slots = 2 * imgHelper.Pixels.Length;
            if (slots < bitsLen)
                throw new Exception("Content too large for the selected image.");
            // If ok continue
            int currentBit = 0;
            int byteCount = 0;
            bool canIterate = true;
            while (canIterate)
            {
                // Define the step of bits to write according to security level
                if ((0 == byteCount % SecurityLevel) || (currentBit < 64))
                {
                    // Get the current color component value
                    byte currentVal = bytes[byteCount];
                    // Get the current bit
                    bool bit = Bits[currentBit];
                    // Write the new value in position 0
                    byte newVal = getNewValForComponent(currentVal, bit, 0);
                    currentBit++;
                    canIterate = currentBit < bitsLen;
                    // If still bits write on the 1st position after the first 64 bits
                    if (canIterate && currentBit > 64)
                    {
                        // Get the current bit
                        bit = Bits[currentBit];
                        // Write the new value in position 1
                        newVal = getNewValForComponent(newVal, bit, 1);
                        currentBit++;
                        canIterate = currentBit < bitsLen;
                    }
                    // Set the new value for the color component
                    bytes[byteCount] = newVal;
                }
                byteCount++;
            }
            imgHelper.UnlockBits();
            var flPath = System.IO.Path.GetDirectoryName(PngFilePath);
            var filename = PngFilePath.Remove(0, string.Concat(flPath, "\\").Length);
            var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var newFileNameAndPath = $@"{docPath}\StegImageUI\steg_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{filename}";
            bitmap.Save(newFileNameAndPath, ImageFormat.Png);
        }

        public static StegStruct UnstegBinary(string PngFilePath)
        {
            var ssRet = new StegStruct();
            int iter = 0;
            var bitmap = new Bitmap(PngFilePath);
            var imgHelper = new ImageHelper(bitmap);
            imgHelper.LockBits();
            var bytes = imgHelper.Pixels;
            imgHelper.UnlockBits();
            // Read the content length
            BitArray baTotLength;
            try
            {
                baTotLength = readAllBitArray(bytes, iter, 32, oneBitReading: true);
            }
            catch (Exception ex)
            {
                _log.Debug(ex.Message);
                throw new Exception("No data embedded in this image or the image is corrupted.");
            }
            // If data is correct, continue
            int totalBits = baTotLength.ToInteger();
            ssRet.TotalSize = totalBits;
            // Read the security level: int => 32 bits (2 bits per component)
            iter += 32;
            BitArray baSecLev;
            try
            {
                baSecLev = readAllBitArray(bytes, iter, 32, oneBitReading: true);
            }
            catch (Exception ex)
            {
                _log.Debug(ex.Message);
                throw new Exception("No data embedded in this image or the image is corrupted.");
            }
            // If data is correct, continue
            int secLev = baSecLev.ToInteger();
            ssRet.SecurityLevel = secLev;
            // Read all steg bits step by security level
            // Start from index 16 for every DCT coeff 2 bits to take => end DCT index is 64
            iter += 32;
            // The security level bitrun has to excluded
            var bits2Read = totalBits - 32;
            // Get the byte array with the required data
            BitArray stegBits;
            try
            {
                stegBits = readAllBitArray(bytes, iter, bits2Read, secLev);
            }
            catch (Exception ex)
            {
                _log.Debug(ex.Message);
                throw new Exception("No data embedded in this image or the image is corrupted.");
            }
            // If data is correct, continue
            ssRet.ContentFull = baTotLength.Append(baSecLev).Append(stegBits);
            // Start reading the bit array struct --------------------------------------------------
            // Read if it is a string or file
            ssRet.IsFile = stegBits[0];
            // Read all the content
            int idx = 1;
            int lng = stegBits.Length - idx;
            var baTmp = readBitArrayData(stegBits, idx, lng);
            ssRet.Content = baTmp;
            ssRet.ContentSize = ssRet.Content.Length;

            return ssRet;
        }

        #endregion

    }
}
