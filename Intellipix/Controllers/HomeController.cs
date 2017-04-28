using ImageResizer;
using SmartGallery.Web.Models;
using Microsoft.Azure;
using Microsoft.ProjectOxford.Vision;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.ProjectOxford.Vision.Contract;

namespace SmartGallery.Web.Controllers
{
    public class HomeController : Controller
    {
        public HomeController(CloudStorageAccount account)
        {
            _account = account;
        }

        private readonly CloudStorageAccount _account;

        public ActionResult Index(string id)
        {
            // Pass a list of blob URIs and captions in ViewBag
            string photoContainer = CloudConfigurationManager.GetSetting("storage:photocontainer");
            string thumbContainer = CloudConfigurationManager.GetSetting("storage:thumbnailcontainer");
            CloudBlobClient client = _account.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(photoContainer);
            List<BlobInfo> blobs = new List<BlobInfo>();

            foreach (IListBlobItem item in container.ListBlobs())
            {
                var blob = item as CloudBlockBlob;

                if (blob != null)
                {
                    blob.FetchAttributes(); // Get blob metadata

                    if (String.IsNullOrEmpty(id) || HasMatchingMetadata(blob, id))
                    {
                        var caption = blob.Metadata.ContainsKey("Caption") ? blob.Metadata["Caption"] : blob.Name;

                        blobs.Add(new BlobInfo()
                        {
                            ImageUri = blob.Uri.ToString(),
                            ThumbnailUri = blob.Uri.ToString().Replace($"/{photoContainer}/", $"/{thumbContainer}/"),
                            Caption = caption,
                            FileName = blob.Name
                        });
                    }
                }
            }

            ViewBag.Blobs = blobs.ToArray();
            ViewBag.Search = id;
            return View();
        }

        [HttpPost]
        public ActionResult Search(string term)
        {
            return RedirectToAction("Index", new { id = term });
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        private bool HasMatchingMetadata(CloudBlockBlob blob, string term)
        {
            foreach (var item in blob.Metadata)
            {
                if (item.Key.StartsWith("Tag") && item.Value.Equals(term, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}