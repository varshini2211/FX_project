﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DolphinFx.Models;
using OfficeOpenXml;
using X.PagedList;

namespace DolphinFx.Controllers
{
    public class EnvironmentsController : Controller
    {
        private readonly AppDbContext _context;

        public EnvironmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Environments
        // public async Task<IActionResult> Index(int? page)
        // {
        //     int pageSize = 5; // Number of records per page
        //     int pageNumber = page ?? 1; // Default to page 1 if no page is specified
        //     var result = await _context.Environments.ToListAsync();
        //     return View(result.ToPagedList(pageNumber, pageSize));
        // }
private bool IsAjaxRequest(HttpRequest request)
{
    return request.Headers["X-Requested-With"] == "XMLHttpRequest";
}

        public async Task<IActionResult> Index(string searchTerm, int? page)
{
    // Create a queryable collection of environments
    var environments = _context.Environments.AsQueryable();

    // Check if the search term is provided
    if (!string.IsNullOrEmpty(searchTerm))
    {
        searchTerm = searchTerm.ToLower();
        environments = environments.Where(e => e.EnvironmentName.ToLower().Contains(searchTerm) || // Adjust based on your model properties
                                                e.EnvironmentDescription.ToLower().Contains(searchTerm)); // Add other fields as necessary
    }

    // Define pagination
    int pageSize = 5; // Number of records per page
    int pageNumber = page ?? 1; // Default to page 1 if no page is specified

    // Get all environments as a list (not recommended for large datasets, consider using pagination here too)
    var result = await environments.ToListAsync(); // Fetch all records matching the search criteria
    var pagedEnvironments = result.ToPagedList(pageNumber, pageSize); // Use ToPagedList for pagination

    ViewBag.SearchTerm = searchTerm; // Pass the search term to the view

    if (IsAjaxRequest(Request)) // Check if the request is an AJAX request
    {
        return PartialView("_EnvironmentTablePartial", pagedEnvironments); // Return a partial view for AJAX requests
    }

    return View(pagedEnvironments); // Return the full view for regular requests
}


        // GET: Environments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var environment = await _context.Environments
                .FirstOrDefaultAsync(m => m.EnvironmentID == id);
            if (environment == null)
            {
                return NotFound();
            }

            return PartialView(environment);
        }

        // GET: Environments/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Environments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EnvironmentID,EnvironmentName,EnvironmentDescription")] Models.Environment environment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(environment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Environment added.";
                return RedirectToAction(nameof(Index));
            }
            return View(environment);
        }

        // GET: Environments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var environment = await _context.Environments.FindAsync(id);
            if (environment == null)
            {
                return NotFound();
            }
            return View(environment);
        }

        // POST: Environments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EnvironmentID,EnvironmentName,EnvironmentDescription")] Models.Environment environment)
        {
            if (id != environment.EnvironmentID)
            {
                return NotFound();
            }
            var existingEnvironment = await _context.Environments.AsNoTracking().FirstOrDefaultAsync(e => e.EnvironmentID == id);
            if (existingEnvironment == null)
            {
                return NotFound();
            }
            if (existingEnvironment.EnvironmentName == environment.EnvironmentName && existingEnvironment.EnvironmentDescription == environment.EnvironmentDescription)
            {
                TempData["InfoMessage"] = "No changes were made.";
                return RedirectToAction(nameof(Index));
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(environment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Environment updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EnvironmentExists(environment.EnvironmentID))
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
            return View(environment);
        }

        // GET: Environments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var environment = await _context.Environments
                .FirstOrDefaultAsync(m => m.EnvironmentID == id);
            if (environment == null)
            {
                return NotFound();
            }

            return View(environment);
        }

        // POST: Environments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var environment = await _context.Environments.FindAsync(id);
            if (environment != null)
            {
                _context.Environments.Remove(environment);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Environment deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> ExportToExcel()
        {
            var environments = await _context.Environments.ToListAsync();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Environments");

                // Add headers
                worksheet.Cells[1, 1].Value = "Environment Name";
                worksheet.Cells[1, 2].Value = "Environment Description";

                // Add data
                for (int i = 0; i < environments.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = environments[i].EnvironmentName;
                    worksheet.Cells[i + 2, 2].Value = environments[i].EnvironmentDescription;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Set content type and file name
                var stream = new MemoryStream();
                package.SaveAs(stream);
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = "Environments.xlsx";

                return File(stream.ToArray(), contentType, fileName);
            }
        }

        private bool EnvironmentExists(int id)
        {
            return _context.Environments.Any(e => e.EnvironmentID == id);
        }
    }
}
