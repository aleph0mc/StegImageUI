using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using ZetaLongPaths;
namespace StegImageUI.Validations
{
    public class ImageFilePathValidation : ValidationRule
    {
        private byte[] fileToByteArray(string fileName, int numBytes)
        {
            byte[] buff;
            BinaryReader br = null;
            try
            {
                br = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read));
                br.BaseStream.Seek(0, SeekOrigin.Begin);
                buff = br.ReadBytes(numBytes);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                br?.BaseStream.Close();
                br?.Close();
                br?.Dispose();
            }

            return buff;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            Regex re = new Regex(@"^(?:[\w]\:|\\)(\\[a-z_\-\s0-9\.]+)*\\?$", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            string strVal = (value ?? "").ToString().Trim();
            var valid = (string.IsNullOrEmpty(strVal) || re.IsMatch(strVal)) ? new ValidationResult(true, null) : new ValidationResult(false, Properties.Resources.WrongPathFormat);
            // IF VALID PATH CHECK IF EXISTS
            if (valid.IsValid && !string.IsNullOrEmpty(strVal))
            {
                // CHECK IF THE FILE IS IN THE CORRECT FORMAT
                var fileInfo = new ZlpFileInfo(strVal);

                if (!fileInfo.Exists)
                    valid = new ValidationResult(false, Properties.Resources.PathNotFound);
                else // PARSE THE FILE TO CONFIRM THE EXTENSION
                {
                    string ext = fileInfo.Extension.ToLower();

                    var bytes = fileToByteArray(strVal, 16);

                    bool isCorrect = false;
                    // PARSE THE BINARY TO VERIFY EXTENSION
                    switch (ext)
                    {
                        case ".jpg":
                        case ".jpeg":
                            // JFIF or Exif
                            isCorrect = (null == bytes) || ((74 == bytes[6]) && (70 == bytes[7]) && (73 == bytes[8]) && (70 == bytes[9]))
                                                          || ((69 == bytes[6]) && (120 == bytes[7]) && (105 == bytes[8]) && (102 == bytes[9]));
                            break;
                        case ".bmp":
                            // BM
                            isCorrect = (null == bytes) || ((66 == bytes[0]) && (77 == bytes[1]));
                            break;
                        case ".png":
                            // ‰PNG
                            isCorrect = (null == bytes) || ((137 == bytes[0]) && (80 == bytes[1]) && (78 == bytes[2]) && (71 == bytes[3]));
                            break;
                        case ".gif":
                            // GIF
                            isCorrect = (null == bytes) || ((71 == bytes[0]) && (73 == bytes[1]) && (70 == bytes[2]) && (56 == bytes[3]));
                            break;
                        case ".tif":
                        case ".tiff":
                            // TIFF
                            isCorrect = (null == bytes) || ((73 == bytes[0]) && (73 == bytes[1]) && (42 == bytes[2]) && (0 == bytes[3]))
                                || ((77 == bytes[0]) && (77 == bytes[1]) && (0 == bytes[2]) && (42 == bytes[3]));
                            break;
                        default:
                            break;
                    }

                    valid = isCorrect ? new ValidationResult(true, null) : new ValidationResult(false, Properties.Resources.WrongFileType);
                }
            }
            return valid;
        }
    }
}
