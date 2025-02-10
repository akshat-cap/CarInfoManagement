using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using CarInfoManagementSystem.Data;
using CarInfoManagementSystem.Models;

namespace CarInfoManagementSystem.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class ManufacturerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManufacturerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Manufacturer/Create
        public IActionResult Create(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Manufacturer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Manufacturer manufacturer, string? returnUrl)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(manufacturer);
                    await _context.SaveChangesAsync();
                    
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Create", "Car");
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException?.Message.Contains("IX_Manufacturers_Name") == true)
                    {
                        ModelState.AddModelError("Name", "This manufacturer name already exists.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. Please try again.");
                    }
                }
            }
            ViewBag.ReturnUrl = returnUrl;
            return View(manufacturer);
        }

        // POST: Manufacturer/QuickAdd
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAdd([FromBody] QuickAddManufacturerModel model)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return Json(new { success = false, message = "Name is required" });
            }

            try
            {
                var manufacturer = new Manufacturer { 
                    Name = model.Name
                };

                _context.Add(manufacturer);
                await _context.SaveChangesAsync();

                return Json(new { success = true, id = manufacturer.Id, name = manufacturer.Name });
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException?.Message.Contains("IX_Manufacturers_Name") == true)
                {
                    return Json(new { success = false, message = "This manufacturer name already exists." });
                }
                return Json(new { success = false, message = "An error occurred while adding the manufacturer." });
            }
        }
    }

    public class QuickAddManufacturerModel
    {
        public required string Name { get; set; }
    }
}