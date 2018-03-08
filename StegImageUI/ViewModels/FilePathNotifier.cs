using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StegImageUI.ViewModels
{
    public class FilePathNotifier : INotifyPropertyChanged
    {
        private string _filePath;
        private string _filePathEmbed;

        private void OnPropertyChanged(string propertyName)
        {
            // Use the Null Propagation Operator (c# 6.0)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                _filePath = value;
                OnPropertyChanged("FilePath");
            }
        }

        public string FilePathEmbed
        {
            get
            {
                return _filePathEmbed;
            }
            set
            {
                _filePathEmbed = value;
                OnPropertyChanged("FilePathEmbed");
            }
        }

    }
}
