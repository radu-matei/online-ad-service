using System.IO;
using Microsoft.Azure.WebJobs;
using OnlineAdService.Common;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace OnlineAdService.WebJob
{
    public class Functions
    {
        public static void GenerateThumbnail(
            [QueueTrigger("thumbnailrequest")] BlobInformation blobInfo,
            [Blob("images/{BlobName}", FileAccess.Read)] Stream input,
            [Blob("images/{BlobNameWithoutExtension}_thumbnail.jpg")]CloudBlockBlob outputBlob)
        {
            using(Stream output = outputBlob.OpenWrite())
            {
                ConvertImageToThumbnailJpg(input, output);
                outputBlob.Properties.ContentType = "image/jpeg"; 
            }

            using(AdContext adContext = new AdContext())
            {
                var ad = adContext.Ads.Find(blobInfo.AdId);

                if (ad == null)
                    throw new Exception(String.Format("Cannot create thumbnail for AdId {0}", blobInfo.AdId));

                ad.ThumbnailUrl = outputBlob.Uri.ToString();
                adContext.SaveChanges();
            }
        }

        public static void ConvertImageToThumbnailJpg(Stream input, Stream output)
        {
            int thumbnailsize = 80;
            int width;
            int height;
            var originalImage = new Bitmap(input);

            if (originalImage.Width > originalImage.Height)
            {
                width = thumbnailsize;
                height = thumbnailsize * originalImage.Height / originalImage.Width;
            }
            else
            {
                height = thumbnailsize;
                width = thumbnailsize * originalImage.Width / originalImage.Height;
            }

            Bitmap thumbnailImage = null;
            try
            {
                thumbnailImage = new Bitmap(width, height);

                using (Graphics graphics = Graphics.FromImage(thumbnailImage))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(originalImage, 0, 0, width, height);
                }

                thumbnailImage.Save(output, ImageFormat.Jpeg);
            }
            finally
            {
                if (thumbnailImage != null)
                {
                    thumbnailImage.Dispose();
                }
            }
        }
    }
}
