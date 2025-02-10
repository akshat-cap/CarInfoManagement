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
                await EnsureValidPhotoUrl(car);
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

            await EnsureValidPhotoUrl(car);
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(Car car)
        {
            if (ModelState.IsValid)
            {
                if (car.PhotoFile != null && car.PhotoFile.Length > 0)
                {
                    string fileName = DateTime.Now.ToString("ddMMyyyyhhmmss") + Path.GetFileName(car.PhotoFile.FileName);
                    string localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "cars", fileName);

                    try
                    {
                        // Save file locally first
                        using (var fileStream = new FileStream(localPath, FileMode.Create))
                        {
                            await car.PhotoFile.CopyToAsync(fileStream);
                        }

                        // Upload to Azure Blob Storage
                        BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                        await containerClient.CreateIfNotExistsAsync();
                        BlobClient blobClient = containerClient.GetBlobClient(fileName);

                        var blobHttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = car.PhotoFile.ContentType,
                            CacheControl = "public, max-age=31536000"
                        };

                        using (var stream = System.IO.File.OpenRead(localPath))
                        {
                            await blobClient.UploadAsync(stream, new BlobUploadOptions
                            {
                                HttpHeaders = blobHttpHeaders
                            });
                        }

                        car.PhotoUrl = blobClient.Uri.ToString();

                        // Prepare validation data
                        var authJSON = new
                        {
                            Engine = car.Engine,
                            BHP = car.BHP,
                            Mileage = car.Mileage,
                            Seat = car.Seat,
                            BootSpace = car.BootSpace,
                            Price = car.Price
                        };

                        string json = JsonSerializer.Serialize(authJSON);

                        try
                        {
                            using (var client = new HttpClient())
                            {
                                client.Timeout = TimeSpan.FromSeconds(30);
                                var validateUrl = "https://prod-31.uaenorth.logic.azure.com:443/workflows/43897f618acd4373aceb809fd17de6f6/triggers/When_a_HTTP_request_is_received/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2FWhen_a_HTTP_request_is_received%2Frun&sv=1.0&sig=_OUgg0zNMDY4zRrtnPTE-pymnDNXc3bYERwafJbnuoM";
                                
                                var content = new StringContent(json, Encoding.UTF8, "application/json");
                                var response = await client.PostAsync(validateUrl, content);
                                
                                if (response.IsSuccessStatusCode)
                                {
                                    _context.Add(car);
                                    await _context.SaveChangesAsync();
                                    TempData["Success"] = "Car created successfully!";
                                    return RedirectToAction(nameof(Index));
                                }
                                else
                                {
                                    var errorContent = await response.Content.ReadAsStringAsync();
                                    ModelState.AddModelError("", $"Validation failed: {errorContent}");
                                    TempData["ErrorMessage"] = $"Failed to validate the car details. Status: {response.StatusCode}. Please try again.";
                                    // Clean up the uploaded files if validation fails
                                    if (System.IO.File.Exists(localPath))
                                    {
                                        System.IO.File.Delete(localPath);
                                    }
                                    await blobClient.DeleteIfExistsAsync();
                                }
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            ModelState.AddModelError("", $"Network error during validation: {ex.Message}");
                            TempData["ErrorMessage"] = "Failed to connect to validation service. Please try again later.";
                            // Clean up the uploaded files if validation fails
                            if (System.IO.File.Exists(localPath))
                            {
                                System.IO.File.Delete(localPath);
                            }
                            await blobClient.DeleteIfExistsAsync();
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("", $"Validation error: {ex.Message}");
                            TempData["ErrorMessage"] = "An unexpected error occurred during validation. Please try again.";
                            // Clean up the uploaded files if validation fails
                            if (System.IO.File.Exists(localPath))
                            {
                                System.IO.File.Delete(localPath);
                            }
                            await blobClient.DeleteIfExistsAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"File upload error: {ex.Message}");
                        TempData["ErrorMessage"] = "Failed to upload image. Please try again.";
                        // Clean up any uploaded files
                        if (System.IO.File.Exists(localPath))
                        {
                            System.IO.File.Delete(localPath);
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("PhotoFile", "Please select a photo file.");
                }
            }

            PopulateDropDowns();
            return View(car);
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
                    var existingCar = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
                    if (existingCar == null)
                    {
                        return NotFound();
                    }

                    if (car.PhotoFile == null)
                    {
                        car.PhotoUrl = existingCar.PhotoUrl;
                    }
                    else
                    {
                        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                        await containerClient.CreateIfNotExistsAsync();

                        // Delete old photo from blob if exists
                        if (!string.IsNullOrEmpty(existingCar.PhotoUrl))
                        {
                            try
                            {
                                var oldUri = new Uri(existingCar.PhotoUrl);
                                var oldBlobName = Path.GetFileName(oldUri.LocalPath);
                                var oldBlobClient = containerClient.GetBlobClient(oldBlobName);
                                await oldBlobClient.DeleteIfExistsAsync();

                                // Delete old local file if it exists
                                string oldLocalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "cars", oldBlobName);
                                if (System.IO.File.Exists(oldLocalPath))
                                {
                                    System.IO.File.Delete(oldLocalPath);
                                }
                            }
                            catch
                            {
                                // If there's any error deleting the old photo, just continue
                            }
                        }

                        string fileName = DateTime.Now.ToString("ddMMyyyyhhmmss") + Path.GetFileName(car.PhotoFile.FileName);
                        string localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "cars", fileName);
                        
                        // Save locally first
                        using (var fileStream = new FileStream(localPath, FileMode.Create))
                        {
                            await car.PhotoFile.CopyToAsync(fileStream);
                        }

                        // Upload to blob storage
                        var blobClient = containerClient.GetBlobClient(fileName);
                        var blobHttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = car.PhotoFile.ContentType,
                            CacheControl = "public, max-age=31536000"
                        };

                        using (var stream = System.IO.File.OpenRead(localPath))
                        {
                            await blobClient.UploadAsync(stream, new BlobUploadOptions
                            {
                                HttpHeaders = blobHttpHeaders
                            });
                        }

                        // Store the full blob URL in the database
                        car.PhotoUrl = blobClient.Uri.ToString();
                    }

                    _context.Update(car);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Car updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarExists(car.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
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

        private async Task EnsureValidPhotoUrl(Car car)
        {
            try
            {
                if (string.IsNullOrEmpty(car.PhotoUrl))
                {
                    car.PhotoUrl = "no-image.png";
                    return;
                }

                var isUrl = Uri.TryCreate(car.PhotoUrl, UriKind.Absolute, out var uri);
                if (!isUrl || uri == null)
                {
                    // Already a relative path, just ensure it's only the filename
                    car.PhotoUrl = Path.GetFileName(car.PhotoUrl);
                    return;
                }

                // Handle Azure Blob Storage URLs
                if (uri.Host.Contains("blob.core.windows.net"))
                {
                    string fileName = Path.GetFileName(uri.LocalPath);
                    string localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "cars", fileName);
                    string? directoryPath = Path.GetDirectoryName(localPath);

                    // Only download if file doesn't exist locally
                    if (!System.IO.File.Exists(localPath))
                    {
                        using var semaphore = new SemaphoreSlim(1, 1);
                        await semaphore.WaitAsync();
                        
                        try
                        {
                            // Double-check after acquiring semaphore
                            if (!System.IO.File.Exists(localPath) && directoryPath != null)
                            {
                                Directory.CreateDirectory(directoryPath);
                                using var client = new HttpClient();
                                var response = await client.GetAsync(car.PhotoUrl);
                                if (response.IsSuccessStatusCode)
                                {
                                    using var fs = new FileStream(localPath, FileMode.Create);
                                    await response.Content.CopyToAsync(fs);
                                }
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }
                    
                    // Use just the filename as URL
                    car.PhotoUrl = fileName;
                }
                else if (uri.Scheme == "http")
                {
                    car.PhotoUrl = "no-image.png";
                }
            }
            catch (Exception)
            {
                // Fallback to no-image if anything goes wrong
                car.PhotoUrl = "no-image.png";
            }
        }
    }
}