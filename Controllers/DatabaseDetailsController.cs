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
    public class DatabaseDetailsController : Controller
    {
        private readonly AppDbContext _context;

        public DatabaseDetailsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: DatabaseDetails
        // public async Task<IActionResult> Index(int? page)
        // {
        //     int pageSize = 5; // Number of records per page
        //     int pageNumber = page ?? 1; // Default to page 1 if no page is specified
        //     var appDbContext = _context.DatabaseDetails.Include(d => d.Application).Include(d => d.Client).Include(d => d.Environments);
        //     var result = await appDbContext.ToListAsync();
        //     return View(result.ToPagedList(pageNumber, pageSize));
        // }

        public async Task<IActionResult> Index(string searchTerm, int? page)
{
    // Start with a queryable collection of DatabaseDetails
    var databaseDetails = _context.DatabaseDetails
        .Include(d => d.Application)
        .Include(d => d.Client)
        .Include(d => d.Environments)
        .AsQueryable();

    // Apply search filter if a search term is provided
    if (!string.IsNullOrEmpty(searchTerm))
    {
        searchTerm = searchTerm.ToLower();
        databaseDetails = databaseDetails.Where(d => d.Datasource.ToLower().Contains(searchTerm) || // Replace SomeProperty with actual properties
                                                     d.Username.ToLower().Contains(searchTerm) ||
                                                     d.Password.ToLower().Contains(searchTerm) ||
                                                     d.Client.ClientName.ToLower().Contains(searchTerm) ||
                                                     d.Application.ApplicationName.ToLower().Contains(searchTerm) ||
                                                     d.Environments.EnvironmentName.ToLower().Contains(searchTerm)
                        
                                                    

                                                     ); // Add more properties as needed
    }

    // Pagination setup
    int pageSize = 5; // Number of records per page
    int pageNumber = page ?? 1; // Default to page 1 if no page is specified

    // Execute the query and get paginated results
    var result = await databaseDetails.ToListAsync();
    var pagedDatabaseDetails = result.ToPagedList(pageNumber, pageSize);

    ViewBag.SearchTerm = searchTerm; // Pass the search term to the view

    // Check if the request is an AJAX request
    if (IsAjaxRequest(Request)) // Ensure you have IsAjaxRequest method defined
    {
        return PartialView("_DatabaseDetailsTablePartial", pagedDatabaseDetails); // Return a partial view for AJAX requests
    }

    return View(pagedDatabaseDetails); // Return the full view for regular requests
}


        // GET: DatabaseDetails/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var databaseDetail = await _context.DatabaseDetails
                .Include(d => d.Application)
                .Include(d => d.Client)
                .Include(d => d.Environments)
                .FirstOrDefaultAsync(m => m.DbId == id);
            if (databaseDetail == null)
            {
                return NotFound();
            }
            if (IsAjaxRequest(Request))
            {
                return PartialView("Details", databaseDetail);
            }

            return PartialView(databaseDetail);
        }

        // GET: DatabaseDetails/Create
        public IActionResult Create()
        {
            ViewData["ApplicationID"] = new SelectList(_context.Applications, "ApplicationID", "ApplicationName");
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientID", "ClientName");
            ViewData["EnvironmentID"] = new SelectList(_context.Environments, "EnvironmentID", "EnvironmentName");
            return View();
        }

        // POST: DatabaseDetails/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DbId,Datasource,Username,Password,ClientId,EnvironmentID,ApplicationID")] DatabaseDetail databaseDetail)
        {
            if (ModelState.IsValid)
            {
                _context.Add(databaseDetail);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Database Details created.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["ApplicationID"] = new SelectList(_context.Applications, "ApplicationID", "ApplicationName", databaseDetail.ApplicationID);
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientID", "ClientName", databaseDetail.ClientId);
            ViewData["EnvironmentID"] = new SelectList(_context.Environments, "EnvironmentID", "EnvironmentName", databaseDetail.EnvironmentID);
            return View(databaseDetail);
        }

        // GET: DatabaseDetails/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var databaseDetail = await _context.DatabaseDetails.FindAsync(id);
            if (databaseDetail == null)
            {
                return NotFound();
            }
            ViewData["ApplicationID"] = new SelectList(_context.Applications, "ApplicationID", "ApplicationName", databaseDetail.ApplicationID);
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientID", "ClientName", databaseDetail.ClientId);
            ViewData["EnvironmentID"] = new SelectList(_context.Environments, "EnvironmentID", "EnvironmentName", databaseDetail.EnvironmentID);
            return View(databaseDetail);
        }

        // POST: DatabaseDetails/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DbId,Datasource,Username,Password,ClientId,EnvironmentID,ApplicationID")] DatabaseDetail databaseDetail)
        {
            if (id != databaseDetail.DbId)
            {
                return NotFound();
            }
            var existingDb = await _context.DatabaseDetails.AsNoTracking().FirstOrDefaultAsync(d => d.DbId == id);
            if (existingDb == null)
            {
                return NotFound();
            }
            if (existingDb.Datasource == databaseDetail.Datasource && existingDb.Username == databaseDetail.Username && existingDb.Password == databaseDetail.Password && existingDb.ClientId == databaseDetail.ClientId && existingDb.ApplicationID == databaseDetail.ApplicationID && existingDb.EnvironmentID == databaseDetail.EnvironmentID)
            {
                TempData["InfoMessage"] = "No changes were made.";
                return RedirectToAction(nameof(Index));
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(databaseDetail);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Database Details updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DatabaseDetailExists(databaseDetail.DbId))
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
            ViewData["ApplicationID"] = new SelectList(_context.Applications, "ApplicationID", "ApplicationName", databaseDetail.ApplicationID);
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientID", "ClientName", databaseDetail.ClientId);
            ViewData["EnvironmentID"] = new SelectList(_context.Environments, "EnvironmentID", "EnvironmentName", databaseDetail.EnvironmentID);
            return View(databaseDetail);
        }

        // GET: DatabaseDetails/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var databaseDetail = await _context.DatabaseDetails
                .Include(d => d.Application)
                .Include(d => d.Client)
                .Include(d => d.Environments)
                .FirstOrDefaultAsync(m => m.DbId == id);
            if (databaseDetail == null)
            {
                return NotFound();
            }

            return View(databaseDetail);
        }

        // POST: DatabaseDetails/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var databaseDetail = await _context.DatabaseDetails.FindAsync(id);
            if (databaseDetail != null)
            {
                _context.DatabaseDetails.Remove(databaseDetail);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Database Details deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> ExportToExcel()
        {
            var databaseDetails = await _context.DatabaseDetails
                .Include(d => d.Client)
                .Include(d => d.Environments)
                .Include(d => d.Application)
                .ToListAsync();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("DatabaseDetails");

                // Add headers
                worksheet.Cells[1, 1].Value = "Datasource";
                worksheet.Cells[1, 2].Value = "Username";
                worksheet.Cells[1, 3].Value = "Password";
                worksheet.Cells[1, 4].Value = "Client";
                worksheet.Cells[1, 5].Value = "Environment";
                worksheet.Cells[1, 6].Value = "Application";

                // Add data
                for (int i = 0; i < databaseDetails.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = databaseDetails[i].Datasource;
                    worksheet.Cells[i + 2, 2].Value = databaseDetails[i].Username;
                    worksheet.Cells[i + 2, 3].Value = databaseDetails[i].Password;
                    worksheet.Cells[i + 2, 4].Value = databaseDetails[i].Client?.ClientName;
                    worksheet.Cells[i + 2, 5].Value = databaseDetails[i].Environments?.EnvironmentName;
                    worksheet.Cells[i + 2, 6].Value = databaseDetails[i].Application?.ApplicationName;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Set content type and file name
                var stream = new MemoryStream();
                package.SaveAs(stream);
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = "DatabaseDetails.xlsx";

                return File(stream.ToArray(), contentType, fileName);
            }
        }

        private bool DatabaseDetailExists(int id)
        {
            return _context.DatabaseDetails.Any(e => e.DbId == id);
        }
        public static bool IsAjaxRequest(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}
