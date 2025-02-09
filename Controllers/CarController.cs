using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using CarInfoManagementSystem.Data;
using CarInfoManagementSystem.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;
using System.Text;
using System.Reflection.Metadata;

namespace CarInfoManagementSystem.Controllers
{
    [Authorize] // Requires authentication for all actions
    public class CarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "image";

        public CarController(ApplicationDbContext context, BlobServiceClient blobServiceClient)
        {
            _context = context;
            _blobServiceClient = blobServiceClient;
        }

        // GET: Car
        public async Task<IActionResult> Index()
        {
            var cars = await _context.Cars
                .Include(c => c.Manufacturer)
                .Include(c => c.CarType)
                .Include(c => c.TransmissionType)
                .ToListAsync();

            // Set cache control headers for better image performance
            Response.Headers.CacheControl = "public,max-age=300";

            // Ensure all photos have a valid URL
            foreach (var car in cars)
            {
                EnsureValidPhotoUrl(car);
            }

            PopulateDropDowns();
            return View(cars);
        }

        // GET: Car/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars
                .Include(c => c.Manufacturer)
                .Include(c => c.CarType)
                .Include(c => c.TransmissionType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (car == null)
            {
                return NotFound();
            }

            EnsureValidPhotoUrl(car);
            return View(car);
        }

        // GET: Car/Create
        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            PopulateDropDowns();
            return View();
        }

        // POST: Car/Create
        // POST: Car/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(Car car)
        {
            if (ModelState.IsValid)
            {
                if (car.PhotoFile != null && car.PhotoFile.Length > 0)
                {
                    var extension = Path.GetExtension(car.PhotoFile.FileName).ToLower();
                    var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    if (!Array.Exists(validExtensions, ext => ext == extension))
                    {
                        ModelState.AddModelError("", "Invalid file type. Only image files are allowed.");
                        PopulateDropDowns();
                        return View(car);  // Ensure we return the view with errors
                    }

                    // Set up the file path for saving the file locally
                    string fn = car.PhotoFile.FileName; // Use the original file name
                    string folder = "cars\\"; // Folder name in wwwroot
                    string filename = DateTime.Now.ToString("ddMMyyyyhhmmss") + fn;
                    folder += filename;

                    // Local path to save the file in wwwroot
                    string serverFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "cars", filename);

                    // Save the file locally
                    using (var fileStream = new FileStream(serverFolder, FileMode.Create))
                    {
                        await car.PhotoFile.CopyToAsync(fileStream);
                    }

                    // After saving the file, upload it to Azure Blob Storage
                    BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                    await containerClient.CreateIfNotExistsAsync();

                    // Create a BlobClient for the file
                    BlobClient blobClient = containerClient.GetBlobClient(filename);

                    // Upload the file to Azure Blob Storage
                    using (var stream = new FileStream(serverFolder, FileMode.Open))
                    {
                        await blobClient.UploadAsync(stream, overwrite: true);
                    }

                    // Optionally, delete the local file after uploading
                    System.IO.File.Delete(serverFolder);
                    var id = Guid.NewGuid().ToString();

                    string bloburi = blobClient.Uri.ToString();

                    var authJSON = new
                    {
                        //CarType = car.CarType?.Type,
                        Engine = car.Engine,
                        BHP = car.BHP,
                        Mileage = car.Mileage,
                        Seat = car.Seat,
                        //TransmissionType = car.TransmissionType?.Name,
                        BootSpace = car.BootSpace,
                        Price = car.Price
                    };

                    // Convert to JSON string
                    string json = JsonSerializer.Serialize(authJSON, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(json);

                    // Send to external service
                    string url = "https://prod-31.uaenorth.logic.azure.com:443/workflows/43897f618acd4373aceb809fd17de6f6/triggers/When_a_HTTP_request_is_received/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2FWhen_a_HTTP_request_is_received%2Frun&sv=1.0&sig=_OUgg0zNMDY4zRrtnPTE-pymnDNXc3bYERwafJbnuoM";
                    using HttpClient client = new HttpClient();

                    HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    try
                    {
                        // Send the POST request
                        HttpResponseMessage response = await client.PostAsync(url, content);

                        // Check if the request was successful
                        if (response.IsSuccessStatusCode)
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            Console.WriteLine("Response: " + responseBody);
                        }
                        else
                        {
                            Console.WriteLine($"Error: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception occurred: " + ex.Message);
                    }

                    car.PhotoUrl = bloburi;
                    _context.Add(car);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Car created successfully!";
                    return RedirectToAction(nameof(Index));  // Ensure we return to Index after success
                }

                PopulateDropDowns();
                return View(car);  // Return the view if photo file is not valid
            }

            PopulateDropDowns();
            return View(car);  // Return the view if model state is invalid
        }


        // GET: Car/Edit/5
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars.FindAsync(id);
            if (car == null)
            {
                return NotFound();
            }
            PopulateDropDowns();
            return View(car);
        }

        // POST: Car/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int id, Car car)
        {
            if (id != car.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (car.PhotoFile != null)
                    {
                        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                        await containerClient.CreateIfNotExistsAsync();

                        // Delete old photo if exists
                        if (!string.IsNullOrEmpty(car.PhotoUrl))
                        {
                            var oldUri = new Uri(car.PhotoUrl);
                            var oldBlobName = Path.GetFileName(oldUri.LocalPath);
                            var oldBlobClient = containerClient.GetBlobClient(oldBlobName);
                            await oldBlobClient.DeleteIfExistsAsync();
                        }

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(car.PhotoFile.FileName);
                        var blobClient = containerClient.GetBlobClient(fileName);

                        var blobHttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = car.PhotoFile.ContentType,
                            CacheControl = "public, max-age=31536000"
                        };

                        using (var stream = car.PhotoFile.OpenReadStream())
                        {
                            await blobClient.UploadAsync(stream, new BlobUploadOptions
                            {
                                HttpHeaders = blobHttpHeaders
                            });
                        }

                        car.PhotoUrl = blobClient.Uri.ToString();
                    }

                    _context.Update(car);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Car updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarExists(car.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            PopulateDropDowns();
            return View(car);
        }

        // GET: Car/Delete/5
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars
                .Include(c => c.Manufacturer)
                .Include(c => c.CarType)
                .Include(c => c.TransmissionType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        // POST: Car/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car != null)
            {
                _context.Cars.Remove(car);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Car/Search
        public async Task<IActionResult> Search(string searchName)
        {
            var cars = _context.Cars
                .Include(c => c.Manufacturer)
                .Include(c => c.CarType)
                .Include(c => c.TransmissionType)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchName))
            {
                cars = cars.Where(c => c.Model.Contains(searchName) || 
                    (c.Manufacturer != null && c.Manufacturer.Name.Contains(searchName)) || 
                    (c.CarType != null && c.CarType.Type.Contains(searchName)));
            }

            var result = await cars.ToListAsync();
            PopulateDropDowns();
            return View("Index", result);
        }

        // GET: Car/FilterByManufacturerAndType
        public async Task<IActionResult> FilterByManufacturerAndType(int? manufacturerId, int? typeId)
        {
            var cars = _context.Cars
                .Include(c => c.Manufacturer)
                .Include(c => c.CarType)
                .Include(c => c.TransmissionType)
                .AsQueryable();

            if (manufacturerId.HasValue && manufacturerId > 0)
            {
                cars = cars.Where(c => c.ManufacturerId == manufacturerId);
            }

            if (typeId.HasValue && typeId > 0)
            {
                cars = cars.Where(c => c.TypeId == typeId);
            }

            var result = await cars.ToListAsync();
            PopulateDropDowns();
            return View("Index", result);
        }

        private bool CarExists(int id)
        {
            return _context.Cars.Any(e => e.Id == id);
        }

        private void PopulateDropDowns()
        {
            ViewBag.Manufacturers = new SelectList(_context.Manufacturers.OrderBy(m => m.Name), "Id", "Name");
            ViewBag.CarTypes = new SelectList(_context.CarTypes.OrderBy(t => t.Type), "Id", "Type");
            ViewBag.TransmissionTypes = new SelectList(_context.CarTransmissionTypes.OrderBy(t => t.Name), "Id", "Name");
        }

        // Add this helper method
        private void EnsureValidPhotoUrl(Car car)
        {
            if (string.IsNullOrEmpty(car.PhotoUrl))
            {
                car.PhotoUrl = "/images/no-image.png";
                return;
            }

            if (!Uri.IsWellFormedUriString(car.PhotoUrl, UriKind.Absolute))
            {
                // If it's a relative URL, make it absolute
                car.PhotoUrl = Url.Content(car.PhotoUrl);
            }
            else
            {
                // Ensure the URL uses HTTPS
                var uri = new Uri(car.PhotoUrl);
                if (uri.Scheme == "http")
                {
                    car.PhotoUrl = "https" + car.PhotoUrl.Substring(4);
                }
            }
        }
    }
}