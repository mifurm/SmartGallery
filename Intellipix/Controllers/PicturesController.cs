using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ImageResizer;
using SmartGallery.Web.Models;
using SmartGallery.Web.ViewModels;
using Microsoft.Azure;
using Microsoft.Azure.Documents.Client;
using Microsoft.ProjectOxford.Vision;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using SmartGallery.Data.Entities;
using SmartGallery.Data.Repositories;

namespace SmartGallery.Web.Controllers
{
    [RoutePrefix("Pictures")]
    public class PicturesController : Controller
    {
        public PicturesController(CloudStorageAccount account, CommentRepository context)
        {
            _account = account;
            _context = context;
        }

        private readonly CommentRepository _context;
        private readonly CloudStorageAccount _account;

        // GET: Images/imageName.jpg
        [HttpGet, Route("{name}")]
        public async Task<ActionResult> Details(string name)
        {
            string photoContainer = CloudConfigurationManager.GetSetting("storage:photocontainer");
            CloudBlobClient client = _account.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(photoContainer);
            var blob = container.GetBlockBlobReference(name);
            try
            {
                await blob.FetchAttributesAsync();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int) HttpStatusCode.NotFound)
                {
                    return HttpNotFound("Image does not exist");
                }
                throw;
            }
            
            var comments = await _context.GetAllAsync(x => x.PictureId == name);

            var vm = new PhotoVM()
            {
                PictureUrl = blob.Uri.ToString(),
                PictureName = blob.Name,
                Caption = blob.Metadata.ContainsKey("Caption") ? blob.Metadata["Caption"] : blob.Name,
                Tags = blob.Metadata.Where(x => x.Key.StartsWith("Tag")).Select(x => x.Value).ToList(),
                Comments = comments?.OrderByDescending(entity => entity.Created).Select(c => new CommentVM
                {
                    Created = c.Created,
                    Message = c.Message,
                    ProfileUrl = c.ProfileUrl,
                    UserId = c.UserId
                }).ToList()
            };
            return View(vm);
        }

        [Authorize]
        [HttpPost, Route("{name}/comment")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Comment(string name, [Bind(Include = "Message")] Comment comment)
        {
            if (!ModelState.IsValid)
            {
                return HttpNotFound("Invalid message");
            }

            string photoContainer = CloudConfigurationManager.GetSetting("storage:photocontainer");
            CloudBlobClient client = _account.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(photoContainer);
            var blob = container.GetBlockBlobReference(name);
            try
            {
                await blob.FetchAttributesAsync();
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    return HttpNotFound("Image does not exist");
                }
                throw;
            }

            var entity = new CommentEntity
            {
                Created = DateTime.Now,
                Message = comment.Message,
                PictureId = name,
                UserId = User.Identity.Name
            };
            await _context.AddAsync(entity);

            return RedirectToAction("Details", new { name = name });
        }

        [Authorize]
        [HttpPost, Route("Upload")]
        public async Task<ActionResult> Upload(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                // Make sure the user selected an image file
                if (!file.ContentType.StartsWith("image"))
                {
                    TempData["Message"] = "Only image files may be uploaded";
                }
                else
                {
                    // Save the original image in the "photos" container
                    string photoContainer = CloudConfigurationManager.GetSetting("storage:photocontainer");
                    string thumbContainer = CloudConfigurationManager.GetSetting("storage:thumbnailcontainer");
                    CloudBlobClient client = _account.CreateCloudBlobClient();
                    CloudBlobContainer container = client.GetContainerReference(photoContainer);
                    CloudBlockBlob photo = container.GetBlockBlobReference(Path.GetFileName(file.FileName));
                    photo.Properties.ContentType = file.ContentType;
                    await photo.UploadFromStreamAsync(file.InputStream);
                    await photo.SetPropertiesAsync();
                    file.InputStream.Seek(0L, SeekOrigin.Begin);

                    // Generate a thumbnail and save it in the "thumbnails" container
                    using (var outputStream = new MemoryStream())
                    {
                        var settings = new ResizeSettings { MaxWidth = 192, Format = "png" };
                        ImageBuilder.Current.Build(file.InputStream, outputStream, settings);
                        outputStream.Seek(0L, SeekOrigin.Begin);
                        container = client.GetContainerReference(thumbContainer);
                        CloudBlockBlob thumbnail = container.GetBlockBlobReference(Path.GetFileName(file.FileName));
                        await thumbnail.UploadFromStreamAsync(outputStream);
                    }

                    // Submit the image to Azure's Computer Vision API
                    VisionServiceClient vision = new VisionServiceClient(CloudConfigurationManager.GetSetting("vision:key"),
                        CloudConfigurationManager.GetSetting("vision:rootUrl"));
                    VisualFeature[] features = new VisualFeature[] { VisualFeature.Description };
                    var result = await vision.AnalyzeImageAsync(photo.Uri.ToString(), features);

                    // Record the image description and tags in blob metadata
                    photo.Metadata.Add("Caption", result.Description.Captions[0].Text);

                    for (int i = 0; i < result.Description.Tags.Length; i++)
                    {
                        string key = $"Tag{i}";
                        photo.Metadata.Add(key, result.Description.Tags[i]);
                    }

                    await photo.SetMetadataAsync();
                }
            }

            // redirect back to the index action to show the form once again
            return RedirectToAction("Details", "Pictures", new { name = file?.FileName });
        }
    }
}
