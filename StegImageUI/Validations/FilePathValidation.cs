using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using ZetaLongPaths;

namespace StegImageUI.Validations
{
    public class FilePathValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            Regex re = new Regex(@"^(?:[\w]\:|\\)(\\[a-z_\-\s0-9\.]+)*\\?$", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            string strVal = (value ?? "").ToString();
            var valid = re.IsMatch(strVal) ? new ValidationResult(true, null) : new ValidationResult(false, Properties.Resources.WrongPathFormat);
            // IF VALID PATH CHECK IF EXISTS
            if (valid.IsValid)
            {
                // CHECK IF THE PATH IS FOR A FILE OR A DIRECTORY
                var fileInfo = new ZlpFileInfo(strVal);
                var attrs = fileInfo.Attributes;
                if ((attrs & ZetaLongPaths.Native.FileAttributes.Directory) == ZetaLongPaths.Native.FileAttributes.Directory)
                {
                    var dirInfo = new ZlpDirectoryInfo(strVal);
                    if (!dirInfo.Exists)
                        valid = new ValidationResult(false, Properties.Resources.PathNotFound);
                }
                else
                {
                    if (!fileInfo.Exists)
                        valid = new ValidationResult(false, Properties.Resources.PathNotFound);
                }
            }
            return valid;
        }
    }
}
