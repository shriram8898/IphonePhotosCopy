using MediaDevices;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUI3
{
    public class MediaFileItem
    {
        public string FileName => FileInfo.Name;
        public BitmapImage Thumbnail { get; set; }
        public string LocalPath { get; set; }
        public MediaFileInfo FileInfo { get; set; }
    }
}
