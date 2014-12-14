using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CodeLens.ConflictIndicator
{
    public static class Icons
    {
        private const string ResourceUriRoot = "pack://application:,,,/CodeLensConflictIndicator.Library;component/Resources/";

        private static ImageSource indicatorIcon;
        private static ImageSource getLatestIcon;
        private static ImageSource compareIcon;

        public static ImageSource IndicatorIcon
        {
            get { return indicatorIcon ?? (indicatorIcon = LoadImage(ResourceUriRoot + "IndicatorIcon2.png")); }
        }

        public static ImageSource GetLatestIcon
        {
            get { return getLatestIcon ?? (getLatestIcon = LoadImage(ResourceUriRoot + "GetLatestVersion.png")); }
        }

        public static ImageSource CompareIcon
        {
            get { return compareIcon ?? (compareIcon = LoadImage(ResourceUriRoot + "Compare.png")); }
        }

        private static ImageSource LoadImage(string uri)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(uri);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}
