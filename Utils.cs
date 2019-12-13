using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DermalogMultiScannerDemo
{
    public static class Utils
    {
        public static readonly Brush COLOR_DERMALOG_GREEN = new SolidColorBrush(HexToMediaColor(0x11aa11));
        public static readonly Brush COLOR_DERMALOG_RED = new SolidColorBrush(HexToMediaColor(0xff0511));
        public static readonly Brush COLOR_DERMALOG_BLUE = new SolidColorBrush(HexToMediaColor(0x004289));



        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);     

        public static BitmapSource BitmapToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSource img = null;
            try
            {
                img = Imaging.CreateBitmapSourceFromHBitmap(
                             hBitmap,
                             IntPtr.Zero,
                             System.Windows.Int32Rect.Empty,
                             BitmapSizeOptions.FromEmptyOptions());
                img.Freeze();
            }
            finally
            {
                DeleteObject(hBitmap);
            }
            return img;
        }  

        public static Brush GetBrushFromNFIQ2(int nfiq2)
        {            
            if (nfiq2 < 5)
                return COLOR_DERMALOG_RED;
            else if (nfiq2 > 15)
                return COLOR_DERMALOG_GREEN;
            else
                return Brushes.DarkOrange;       
        }

        public static Color ToMediaColor(System.Drawing.Color color)
        {
            return Color.FromArgb(0xFF, color.R, color.G, color.B);
        }

        public static System.Drawing.Color HexToColor(int rgb)
        {
            return System.Drawing.Color.FromArgb(rgb);
        }

        public static Color HexToMediaColor(int rgb)
        {
            return ToMediaColor(HexToColor(rgb));
        }
    }
}
