using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using OnlineAdService.Common;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace OnlineAdService.Web.Controllers
{
    public class AdController : Controller
    {
        private AdContext _adContext { get; set; }
        private CloudQueue _requestQueue { get; set; }
        private CloudBlobContainer _imagesBlobContainer { get; set; }

        public AdController()
        {
            _adContext = new AdContext();
            InitializeStorage();
        }

        private void InitializeStorage()
        {
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            var blobClient = storageAccount.CreateCloudBlobClient();
            _imagesBlobContainer = blobClient.GetContainerReference("images");

            var queueClient = storageAccount.CreateCloudQueueClient();
            _requestQueue = queueClient.GetQueueReference("thumbnailrequest");
        }

        public async Task<ActionResult> Index(int? category)
        {
            var adList = _adContext.Ads.AsQueryable();

            if(category != null)
            {
                adList = adList.Where(a => a.Category == (Category)category);
            }

            return View(await adList.ToListAsync());
        }

        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var ad = await _adContext.Ads.FindAsync(id);

            if (ad == null)
            {
                return HttpNotFound();
            }

            return View(ad);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(
             [Bind(Include = "Title,Price,Description,Category,Phone")] Ad ad,
             HttpPostedFileBase imageFile)
        {
            CloudBlockBlob imageBlob = null;

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.ContentLength != 0) 
                {
                    imageBlob = await UploadBlobAsync(imageFile);
                    ad.ImageUrl = imageBlob.Uri.ToString();
                }

                ad.PostedDate = DateTime.Now;

                _adContext.Ads.Add(ad);
                await _adContext.SaveChangesAsync();

                Trace.TraceInformation("Created AdId {0} in database", ad.AdId);

                if (imageBlob != null)
                {
                    var blobInfo = new BlobInformation()
                    {
                        AdId = ad.AdId,
                        BlobUri = new Uri(ad.ImageUrl)
                    };

                    var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(blobInfo));
                    await _requestQueue.AddMessageAsync(queueMessage);

                    Trace.TraceInformation("Created queue message for AdId {0}", ad.AdId);
                }
                return RedirectToAction("Index");
            }
            return View(ad);
        }

        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var ad = await _adContext.Ads.FindAsync(id);

            if (ad == null)
            {
                return HttpNotFound();
            }
            return View(ad);
        }

        public async Task<ActionResult> Edit(
             [Bind(Include = "AdId,Title,Price,Description,ImageURL,ThumbnailURL,PostedDate,Category,Phone")] Ad ad,
             HttpPostedFileBase imageFile)
        {
            CloudBlockBlob imageBlob = null;
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.ContentLength != 0)
                {
                    await DeleteAdBlobsAsync(ad);

                    imageBlob = await UploadBlobAsync(imageFile);
                    ad.ImageUrl = imageBlob.Uri.ToString();
                }
                _adContext.Entry(ad).State = EntityState.Modified;
                await _adContext.SaveChangesAsync();

                Trace.TraceInformation("Updated AdId {0} in database", ad.AdId);

                if (imageBlob != null)
                {
                    var blobInfo = new BlobInformation()
                    {
                        AdId = ad.AdId,
                        BlobUri = new Uri(ad.ImageUrl)
                    };

                    var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(blobInfo));
                    await _requestQueue.AddMessageAsync(queueMessage);

                    Trace.TraceInformation("Created queue message for AdId {0}", ad.AdId);
                }
                return RedirectToAction("Index");
            }
            return View(ad);
        }

        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ad ad = await _adContext.Ads.FindAsync(id);
            if (ad == null)
            {
                return HttpNotFound();
            }
            return View(ad);
        }

        // POST: Ad/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Ad ad = await _adContext.Ads.FindAsync(id);

            await DeleteAdBlobsAsync(ad);

            _adContext.Ads.Remove(ad);
            await _adContext.SaveChangesAsync();

            Trace.TraceInformation("Deleted ad {0}", ad.AdId);

            return RedirectToAction("Index");
        }

        private async Task<CloudBlockBlob> UploadBlobAsync(HttpPostedFileBase imageFile)
        {
            Trace.TraceInformation("Uploading image file {0}", imageFile.FileName);

            var blobName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var imageBlob = _imagesBlobContainer.GetBlockBlobReference(blobName);

            using(var fileStream = imageFile.InputStream)
            {
                await imageBlob.UploadFromStreamAsync(fileStream);
            }

            Trace.TraceInformation("Uploaded image file to {0}", imageBlob.Uri.ToString());

            return imageBlob;
        }

        private async Task DeleteAdBlobsAsync(Ad ad)
        {
            if (!string.IsNullOrWhiteSpace(ad.ImageUrl))
            {
                var blobUri = new Uri(ad.ImageUrl);
                await DeleteBlobAsync(blobUri);
            }

            if (!string.IsNullOrWhiteSpace(ad.ThumbnailUrl))
            {
                var blobUri = new Uri(ad.ThumbnailUrl);
                await DeleteBlobAsync(blobUri);
            }
        }

        private async Task DeleteBlobAsync(Uri blobUri)
        {
            var blobName = blobUri.Segments[blobUri.Segments.Length - 1];
            Trace.TraceInformation("Deleting image blob {0}", blobName);

            var blobToDelete = _imagesBlobContainer.GetBlockBlobReference(blobName);
            await blobToDelete.DeleteAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _adContext.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}