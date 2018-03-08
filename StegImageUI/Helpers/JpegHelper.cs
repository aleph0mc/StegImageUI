# define DEBUG
// 
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BitMiracle.LibJpeg.Classic;
using log4net;
using StegImageUI.Extensions;

namespace StegImageUI.Helpers
{
    public class JpegHelper
    {
        protected static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // STANDARD QUANTIZATION TABLE FOR LUMINANCE (Y)
        private static readonly int[] _qLuminance =
        {
            16,  11,  10,  16,  24,  40,  51,  61,
            12,  12,  14,  19,  26,  58,  60,  55,
            14,  13,  16,  24,  40,  57,  69,  56,
            14,  17,  22,  29,  51,  87,  80,  62,
            18,  22,  37,  56,  68, 109, 103,  77,
            24,  35,  55,  64,  81, 104, 113,  92,
            49,  64,  78,  87, 103, 121, 120, 101,
            72,  92,  95,  98, 112, 100, 103,  99
        };

        // STANDARD QUANTIZATION TABLE FOR CHROMINANCE (Cb/Cr)
        private static readonly int[] _qChromiance =
        {
            17,  18,  24,  47,  99,  99,  99,  99,
            18,  21,  26,  66,  99,  99,  99,  99,
            24,  26,  56,  99,  99,  99,  99,  99,
            47,  66,  99,  99,  99,  99,  99,  99,
            99,  99,  99,  99,  99,  99,  99,  99,
            99,  99,  99,  99,  99,  99,  99,  99,
            99,  99,  99,  99,  99,  99,  99,  99,
            99,  99,  99,  99,  99,  99,  99,  99
        };

        // USE FOR ZIGZAG SCAN
        private static readonly int[] _zzArray =
        {
            0, 1, 5, 6,14,15,27,28,
            2, 4, 7,13,16,26,29,42,
            3, 8,12,17,25,30,41,43,
            9,11,18,24,31,40,44,53,
            10,19,23,32,39,45,52,54,
            20,22,33,38,46,51,55,60,
            21,34,37,47,50,56,59,61,
            35,36,48,49,57,58,62,63
        };

        // SECURITY LEVELS
        private static readonly int[] _securityLevels = { 1, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

        #region PRIVATE METHODS

        private static int[] zigZag(int[] arr)
        {
            int[] zz = new int[arr.Length];
            for (int i = 0; i < zz.Length; i++)
            {
                zz[_zzArray[i]] = arr[i];
            }
            return zz;
        }

        private static int[] invZigZag(int[] arr)
        {
            int[] zz = new int[arr.Length];
            for (int i = 0; i < zz.Length; i++)
            {
                zz[i] = arr[_zzArray[i]];
            }
            return zz;
        }

        /// <summary>
        /// Write bit on position: ((currentVal >> pos) & 1) == 1;
        /// </summary>
        /// <param name="currentVal"></param>
        /// <param name="bit"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private static short getNewValForComponent(short currentVal, bool bit, short position)
        {
            // Powers of 2 for bit positions: 0, 1, 2, 3, 4, only the first 2 are used in this context
            short[] pow2Arr = { 1, 2 }; // {1, 2, 4, 8, 16, ... };

            // Check the bit on position 1, true = 1, false = 0
            bool chkBit = ((currentVal >> position) & 1) == 1;
            // Power of 2 to skip
            short pow2 = pow2Arr[position];
            // Write only if different
            if (chkBit != bit)
                currentVal = (short)(currentVal > 0 ? (currentVal + pow2) : (currentVal - pow2));

            return currentVal;
        }

        /// <summary>
        /// For bits 0-63 only the LSB of DCT coefficients is written (it takes the full length of the bits to read). From bit 64 every DCT coefficient is written with the last 2 LSBs.
        /// </summary>
        /// <param name="jpegFilePath"></param>
        /// <param name="bits"></param>
        /// <param name="securityLevel"></param>
        private static void changeDctCoefficients(string jpegFilePath, BitArray bits, int securityLevel)
        {
            var jpds = new jpeg_decompress_struct();
            FileStream fileStreamImg = new FileStream(jpegFilePath, FileMode.Open, FileAccess.Read);
            jpds.jpeg_stdio_src(fileStreamImg);
            jpds.jpeg_read_header(true);
            // DCT coefficients
            var jBlock = jpds.jpeg_read_coefficients();
            var bitsLen = bits.Length;
            int currentBit = 0;
            bool canIterate = true;
            int cntAcCoeffNotZero = 0;
            // Write on position 0 then 1
            // Start with Cr component
            int component = (int)EnumStegoChannel.Cr;
            while ((component >= 0) && canIterate)
            {
                int z = 0;
                while (canIterate)
                {
                    int countCrRows;
                    try
                    {
                        countCrRows = jBlock[component].Access(z, 1)[0].Length;
                    }
                    catch
                    {
                        break;
                    }
                    int i = 0;
                    while ((i < countCrRows) && canIterate)
                    {
                        // Skip DC coefficients j = 0
                        int j = 1;
                        while ((j < 64) && canIterate)
                        {
                            short currentVal = jBlock[component].Access(z, 1)[0][i][j];

                            // Skip bits 0 and dct according to the security level (steps)
                            if (0 != currentVal) // Set only 1 bit for the first 64 AC coefficients
                            {
                                // Define the step of bits to write after 64 bits
                                if ((0 == cntAcCoeffNotZero % securityLevel) || (currentBit < 64))
                                {
                                    // Get the current bit
                                    bool bit = bits[currentBit];
                                    // Write the new value in position 0
                                    short newVal = getNewValForComponent(currentVal, bit, 0);
                                    jBlock[component].Access(z, 1)[0][i][j] = newVal;

                                    currentBit++;
                                    canIterate = currentBit < bitsLen;
                                    // Start writing on bit 1 after 64 bits to encode the content length
                                    if (canIterate && currentBit > 64)
                                    {

                                        // Get the current bit
                                        bit = bits[currentBit];
                                        // Write the new value in position 1
                                        newVal = getNewValForComponent(newVal, bit, 1);
                                        jBlock[component].Access(z, 1)[0][i][j] = newVal;
                                        currentBit++;
                                        canIterate = currentBit < bitsLen;
                                    }
                                }
                                cntAcCoeffNotZero++;
                            }
                            j++;
                        }
                        i++;
                    }
                    z++;
                }
                component--;
            }
            jpds.jpeg_finish_decompress();
            fileStreamImg.Close();
            // Get file info
            FileInfo fInfo = new FileInfo(jpegFilePath);
            string dir = fInfo.DirectoryName;
            string fName = fInfo.Name;
            var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var stegJpegFilePath = $@"{docPath}\StegImageUI\steg_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{fName}";
            // Compress process
            FileStream fileStream = File.Create(stegJpegFilePath);
            jpeg_compress_struct jpcs = new jpeg_compress_struct();
            jpcs.jpeg_stdio_dest(fileStream);
            jpds.jpeg_copy_critical_parameters(jpcs);
            jpcs.jpeg_write_coefficients(jBlock);
            jpcs.jpeg_finish_compress();
            fileStream.Close();
            jpds.jpeg_abort_decompress();
        }

        private static HuffmanTable[] getHTables(JHUFF_TBL[] jpegHuffmanTables, HuffmanTable.EnumComponent comp)
        {
            int hCount = jpegHuffmanTables.Length;

            HuffmanTable[] ht = new HuffmanTable[hCount];
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            int idx = 0;
            while (null != jpegHuffmanTables[idx])
            {
                ht[idx].Component = comp;
                Type type = jpegHuffmanTables[idx].GetType();
                object instance = jpegHuffmanTables[idx];
                FieldInfo field = type.GetField("m_bits", bindFlags);
                var obj = field.GetValue(instance);
                ht[idx].Bits = field.GetValue(instance) as byte[];
                field = type.GetField("m_huffval", bindFlags);
                ht[idx].Huffval = field.GetValue(instance) as byte[];
                ++idx;
            }
            var hTables = ht.Where(tab => null != tab.Bits).ToArray();

            return hTables;
        }

        /// <summary>
        /// Read DCT per component and return an array list of short. It fills the flat list of short with all coefficients order by Cr, Cb, Y. The list prameters are by ref.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="jBlock"></param>
        /// <param name="dctFull"></param>
        /// <param name="dctFullDc"></param>
        /// <param name="dctFullAc"></param>
        /// <returns></returns>
        private static List<short[]> readDctCoeffs(EnumStegoChannel component, jvirt_array<JBLOCK>[] jBlock, List<short> dctFull, List<short> dctFullDc, List<short> dctFullAc)
        {
            var dct = new List<short[]>();
            var u = 0;
            while (true)
            {
                int countBlocks = 0;
                JBLOCK[][] jblk = null;
                try
                {
                    jblk = jBlock[(int)component].Access(u, 1); // accessing the block
                    countBlocks = jblk[0].Length;
                }
                catch { break; }

                for (int i = 0; i < countBlocks; i++)
                {
                    var arrTmp = new short[64];
                    for (int j = 0; j < 64; j++)
                    {
                        arrTmp[j] = jblk[0][i][j];
                        dctFull.Add(arrTmp[j]);
                        // Flat list for all DC coefficients
                        if (0 == j)
                            dctFullDc.Add(arrTmp[j]);
                        else // Flat list for all AC coefficients
                            dctFullAc.Add(arrTmp[j]);
                    }
                    // ADD ARRAY TO LIST
                    dct.Add(arrTmp);
                }

                ++u;
            }
            return dct;
        }

        private static BitArray readAllBitArray(IReadOnlyList<short> dctFullAc, int startIterIncluded, int bits2Read, int secLevel = 1)
        {
            var bTemp = new bool[bits2Read];
            int iter = 0;
            bool canIterate = true;
            // Read bits in position 0 then position 1
            while (canIterate)
            {
                if ((0 == startIterIncluded % secLevel) || (startIterIncluded < 64))
                {
                    short currentVal = dctFullAc[startIterIncluded];
                    // Bit to check at position 0 => value for logic AND is 1
                    var bit2Check = 1;
                    bTemp[iter] = (currentVal & bit2Check) == bit2Check;
                    iter++;
                    // check if still iterating
                    canIterate = iter < bits2Read;
                    // The 2 LSBs reading starts at DCT AC coefficient with index 64
                    if (canIterate && startIterIncluded > 63)
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

        #region PUBLIC METHODS

        public enum EnumStegoChannel
        {
            Y,
            Cb,
            Cr
        }

        /// <summary>
        /// The structure for the Huffman tables
        /// </summary>
        public struct HuffmanTable
        {
            public enum EnumComponent
            {
                AC,
                DC
            }

            public EnumComponent Component;
            /// <summary>
            /// Number of Symbols
            /// </summary>
            public byte[] Bits;
            /// <summary>
            /// Symbols given in increasing order
            /// </summary>
            public byte[] Huffval;
        }

        /// <summary>
        /// It Contains the DCT coefficients in blocks per compoent and the full array
        /// </summary>
        public struct DCTCoefficients
        {
            // 2D arrays: [Blocks][Coeffs] - size[Coeffs] = 64
            public short[][] DctY;
            public short[][] DctCb;
            public short[][] DctCr;
            // Array with all the coefficients order by Cr, Cb, Y
            public short[] DctFull;
            // Array with all the DC coefficients order by Cr, Cb, Y
            public short[] DctFullDc;
            // Array with all the AC coefficients order by Cr, Cb, Y
            public short[] DctFullAc;
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

        /// <summary>
        /// Get the Quantization tables using reflection per component
        /// </summary>
        /// <returns></returns>
        public static short[][] GetQuantizationTables(string JpegFilePath)
        {
            FileStream objFileStreamMegaMap = File.Create(JpegFilePath);
            jpeg_decompress_struct jpds = new jpeg_decompress_struct();
            jpeg_compress_struct jpcs = new jpeg_compress_struct();
            jpcs.jpeg_stdio_dest(objFileStreamMegaMap);
            jpds.jpeg_copy_critical_parameters(jpcs);
            jpds.jpeg_finish_decompress();
            objFileStreamMegaMap.Close();
            JQUANT_TBL[] jpeg_quant_tables = jpcs.Quant_tbl_ptrs;
            int qCount = jpeg_quant_tables.Length;

            short[][] qt = new short[qCount][];
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            int idx = 0;
            while (null != jpeg_quant_tables[idx])
            {
                Type type = jpeg_quant_tables[idx].GetType();
                object instance = jpeg_quant_tables[idx];
                FieldInfo field = type.GetField("quantval", bindFlags);
                qt[idx] = field.GetValue(instance) as short[];
                ++idx;
            }
            var qTables = qt.Where(tab => null != tab).ToArray();

            return qTables;
        }

        /// <summary>
        /// Get the Huffman tables using reflection for a specific component (AC, DC)
        /// </summary>
        /// <returns></returns>
        public static HuffmanTable[] GetHuffmanTables(string JpegFilePath)
        {
            FileStream objFileStreamMegaMap = File.Create(JpegFilePath);
            jpeg_decompress_struct jpds = new jpeg_decompress_struct();
            jpeg_compress_struct jpcs = new jpeg_compress_struct();
            jpcs.jpeg_stdio_dest(objFileStreamMegaMap);
            jpds.jpeg_copy_critical_parameters(jpcs);
            jpds.jpeg_finish_decompress();
            objFileStreamMegaMap.Close();

            // DC Huffman tables
            JHUFF_TBL[] jpeg_dc_huffman_tables = jpcs.Dc_huff_tbl_ptrs;
            var comp = HuffmanTable.EnumComponent.DC;
            var htdc = getHTables(jpeg_dc_huffman_tables, comp);

            // AC Huffman tables
            JHUFF_TBL[] jpeg_ac_huffman_tables = jpcs.Ac_huff_tbl_ptrs;
            comp = HuffmanTable.EnumComponent.AC;
            var htac = getHTables(jpeg_ac_huffman_tables, comp);

            HuffmanTable[] hts = new HuffmanTable[htdc.Length + htac.Length];
            Array.Copy(htdc, hts, htdc.Length);
            Array.Copy(htac, 0, hts, htdc.Length, htac.Length);

            return hts;
        }

        /// <summary>
        /// The DCT coefficients are returned in a structure where the different components are separated in 2D arrays od shorts[][].
        /// The first dimension of the array contains the Blocks and the second dimension the 64 values for that block.
        /// </summary>
        /// <returns></returns>
        public static DCTCoefficients GetDctCoefficients(string JpegFilePath)
        {
            jpeg_decompress_struct jpds = new jpeg_decompress_struct();
            FileStream fileStreamImg = new FileStream(JpegFilePath, FileMode.Open, FileAccess.Read);
            jpds.jpeg_stdio_src(fileStreamImg);
            jpds.jpeg_read_header(true);
            // DCT coefficients
            var jBlock = jpds.jpeg_read_coefficients();
            // Initialize the list for all the dct
            var dctFull = new List<short>();
            var dctFullDc = new List<short>();
            var dctFullAc = new List<short>();
            // Get coeffs and fill the list _dctFull
            List<short[]> dctCr = readDctCoeffs(EnumStegoChannel.Cr, jBlock, dctFull, dctFullDc, dctFullAc);
            List<short[]> dctCb = readDctCoeffs(EnumStegoChannel.Cb, jBlock, dctFull, dctFullDc, dctFullAc);
            List<short[]> dctY = readDctCoeffs(EnumStegoChannel.Y, jBlock, dctFull, dctFullDc, dctFullAc);
            // Close decompress and filestream
            jpds.jpeg_finish_decompress();
            fileStreamImg.Close();
            // Return DCT coefficients
            DCTCoefficients dctCoeffs = new DCTCoefficients
            {
                DctY = dctY.ToArray(),
                DctCb = dctCb.ToArray(),
                DctCr = dctCr.ToArray(),
                DctFull = dctFull.ToArray(),
                DctFullDc = dctFullDc.ToArray(),
                DctFullAc = dctFullAc.ToArray()
            };

            return dctCoeffs;
        }

        /// <summary>
        /// Encode a message inside the selected Jpeg with a security level of bit coding
        /// </summary>
        /// <param name="JpegFilePath"></param>
        /// <param name="Bits"></param>
        /// <param name="SecurityLevel"></param>
        public static void StegBinary(string JpegFilePath, BitArray Bits, int SecurityLevel)
        {
            // Check available slots
            var dctCoeffs = GetDctCoefficients(JpegFilePath);
            // Remove values 0
            var dctFullAcNotZero = dctCoeffs.DctFullAc.Where(s => 0 != s).ToArray();
            // 2 bits per coefficient
            var slots = dctFullAcNotZero.Count() * 2 / SecurityLevel;
            // Extra validtion check
            if (slots < Bits.Length)
                throw new Exception("Content too large for the selected image.");

            changeDctCoefficients(JpegFilePath, Bits, SecurityLevel);
        }

        /// <summary>
        /// Decode a message inside the selected Jpeg
        /// </summary>
        /// <returns></returns>
        public static StegStruct UnstegBinary(string JpegFilePath)
        {
            var dctCoeffs = GetDctCoefficients(JpegFilePath);
            // Get the AC coefficients and remove 0s
            var dctFullAcNotZero = dctCoeffs.DctFullAc.Where(s => 0 != s).ToArray();
            // Read data
            StegStruct ssRet;
            int startIter = 0;
            // Read length of steg data (bits) - integer => len = 32
            // => 16 dct coefficients: 2 bits/coeff
            BitArray baTotLength;
            try
            {
                baTotLength = readAllBitArray(dctFullAcNotZero, startIter, 32);
            }
            catch (Exception ex)
            {
                _log.Debug(ex.Message);
                throw new Exception("No data embedded in this image or the image is corrupted.");
            }
            // If data is correct, continue
            int totalBits = baTotLength.ToInteger();
            ssRet.TotalSize = totalBits;
            // Read the security level: int => 32 bits
            startIter = 32;
            BitArray baSecLev;
            try
            {
                baSecLev = readAllBitArray(dctFullAcNotZero, startIter, 32);
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
            // Start from index 16 for every DCT coeff 2 bits to take => end DCT index is 16
            startIter = 64;
            // The security level bitrun has to excluded
            var bits2Read = totalBits - 32;
            BitArray stegBits;
            try
            {
                stegBits = readAllBitArray(dctFullAcNotZero, startIter, bits2Read, secLev);
            }
            catch (Exception ex)
            {
                _log.Debug(ex.Message);
                throw new Exception("No data embedded in this image or the image is corrupted.");
            }
            // If data is correct, continue
            ssRet.ContentFull = stegBits;
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

        public static void ApplyQuantizationTable(string JpegFilePath, EnumStegoChannel Component = EnumStegoChannel.Y, int ScaleFactor = 50, int[] QuantizationTable = null)
        {
            var jpds = new jpeg_decompress_struct();
            FileStream fileStreamImg = new FileStream(JpegFilePath, FileMode.Open, FileAccess.Read);
            jpds.jpeg_stdio_src(fileStreamImg);
            jpds.jpeg_read_header(true);
            // DCT coefficients
            var jBlock = jpds.jpeg_read_coefficients();
            jpds.jpeg_finish_decompress();
            fileStreamImg.Close();

            if (null == QuantizationTable)
                switch (Component)
                {
                    case EnumStegoChannel.Y:
                        QuantizationTable = _qLuminance;
                        break;
                    case EnumStegoChannel.Cb:
                    case EnumStegoChannel.Cr:
                        QuantizationTable = _qChromiance;
                        break;
                }

            // Get fle info
            FileInfo fInfo = new FileInfo(JpegFilePath);
            string dir = fInfo.DirectoryName;
            string fNane = fInfo.Name;
            string stegFName = $"steg_{DateTime.Now.ToString("yyyyMMddHHmm")}_{fNane}";
            string stegJpegFilePath = Path.Combine(dir, stegFName);
            // Compress process
            FileStream fileStream = File.Create(stegJpegFilePath);
            jpeg_compress_struct jpcs = new jpeg_compress_struct();
            jpcs.jpeg_stdio_dest(fileStream);
            jpds.jpeg_copy_critical_parameters(jpcs);
            jpcs.jpeg_write_coefficients(jBlock);
            jpcs.jpeg_finish_compress();
            fileStream.Close();
            jpds.jpeg_abort_decompress();
        }

        #endregion
    }
}

