using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FreddiChatClient
{

    /// <summary>
    /// GreyscaleOnDisabledImage is an <see cref="Image"/> used by <see cref="Button"/>s <see cref="MenuItem"/>s to 
    /// automatically switch to a greyscale version of the source image upon being disabled.
    /// </summary>
    public class GreyscaleOnDisabledImage : Image
    {

        /// <summary>
        /// Constructor. Creates a new <see cref="GreyscaleOnDisabledImage"/>. 
        /// Made static to only override the IsEnabled property metadata once.
        /// </summary>
        static GreyscaleOnDisabledImage()
        {
            // Override the metadata of the IsEnabled property to call OnAutoGreyScaleImageIsEnabledPropertyChanged
            IsEnabledProperty.OverrideMetadata(typeof(GreyscaleOnDisabledImage),
                new FrameworkPropertyMetadata(true, OnGreyscaleOnDisabledImageIsEnabledPropertyChanged));
        }

        /// <summary>
        /// Called when IsEnabled property is changed for all GreyscalOnDisabledImage instances.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private static void OnGreyscaleOnDisabledImageIsEnabledPropertyChanged(DependencyObject source,
            DependencyPropertyChangedEventArgs args)
        {
            // Get the image object.
            var img = source as GreyscaleOnDisabledImage;

            // Ensure we have an instance of GreyscaleOnDisabledImage.
            if (img == null) return;

            // Check the new value of the IsEnabled property
            if (Convert.ToBoolean(args.NewValue)) // It is enabled (true)?
            {
                // Set the Source property to the original value.
                img.Source = ((FormatConvertedBitmap)img.Source).Source;
                // Reset the Opcity Mask
                img.OpacityMask = null;
            }
            else // It isn't enabled (false), use greyscale!
            {
                // Get the image as a bitmap
                var bitmapImage = new BitmapImage(new Uri(img.Source.ToString()));
                // Convert the image to greyscale using the bitmap
                img.Source = new FormatConvertedBitmap(bitmapImage, PixelFormats.Gray32Float, null, 0);
                // Create an Opacity Mask to use for the greyscale image
                img.OpacityMask = new ImageBrush(bitmapImage);
            }
        }

    }

}
