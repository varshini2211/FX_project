﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DolphinFx.Models;
using OfficeOpenXml;
using X.PagedList;

namespace DolphinFx.Controllers
{
    public class ClientsController : Controller
    {
        private readonly AppDbContext _context;

        public ClientsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Clients
        public IActionResult Index(string searchTerm, int? page)
        {
            var clients = _context.Clients.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                clients = clients.Where(c => c.ClientName.ToLower().Contains(searchTerm) ||
                                              c.PrimaryEmailID.ToLower().Contains(searchTerm) ||
                                              c.SecondaryEmailID.ToLower().Contains(searchTerm));
            }

            int pageSize = 5;
            int pageNumber = page ?? 1;
            var pagedClients = clients.ToPagedList(pageNumber, pageSize); // Use ToPagedList here

            ViewBag.SearchTerm = searchTerm; // Pass the search term to the view

            if (IsAjaxRequest(Request)) // Check if the request is an AJAX request
            {
                return PartialView("_ClientTablePartial", pagedClients); // Return a partial view
            }

            return View(pagedClients); // Return the full view for regular requests
        }

        // GET: Clients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .FirstOrDefaultAsync(m => m.ClientID == id);
            if (client == null)
            {
                return NotFound();
            }
            return PartialView("Details", client);
        }

        // GET: Clients/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClientID,ClientName,PrimaryContact,PrimaryEmailID,SecondaryContact,SecondaryEmailID")] Client client)
        {
            if (ModelState.IsValid)
            {
                _context.Add(client);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Client added.";
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // GET: Clients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            return View(client);
        }

        // POST: Clients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClientID,ClientName,PrimaryContact,PrimaryEmailID,SecondaryContact,SecondaryEmailID")] Client client)
        {
            if (id != client.ClientID)
            {
                return NotFound();
            }
            var existingClient = await _context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.ClientID == id);
            if (existingClient == null)
            {
                return NotFound();
            }
            if (existingClient.ClientName == client.ClientName && existingClient.PrimaryContact == client.PrimaryContact && existingClient.PrimaryEmailID == client.PrimaryEmailID && existingClient.SecondaryContact == client.SecondaryContact && existingClient.SecondaryEmailID == client.SecondaryEmailID)
            {
                TempData["InfoMessage"] = "No changes were made.";
                return RedirectToAction(nameof(Index));
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(client);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Client updated.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(client.ClientID))
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
            return View(client);
        }

        // GET: Clients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = await _context.Clients
                .FirstOrDefaultAsync(m => m.ClientID == id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // POST: Clients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                _context.Clients.Remove(client);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Client deleted.";
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> ExportToExcel()
        {
            var clients = await _context.Clients.ToListAsync();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Clients");

                // Add headers
                worksheet.Cells[1, 1].Value = "Client Name";
                worksheet.Cells[1, 2].Value = "Primary Contact";
                worksheet.Cells[1, 3].Value = "Primary Email";
                worksheet.Cells[1, 4].Value = "Secondary Contact";
                worksheet.Cells[1, 5].Value = "Secondary Email";

                // Add data
                for (int i = 0; i < clients.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = clients[i].ClientName;
                    worksheet.Cells[i + 2, 2].Value = clients[i].PrimaryContact;
                    worksheet.Cells[i + 2, 3].Value = clients[i].PrimaryEmailID;
                    worksheet.Cells[i + 2, 4].Value = clients[i].SecondaryContact;
                    worksheet.Cells[i + 2, 5].Value = clients[i].SecondaryEmailID;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Set content type and file name
                var stream = new MemoryStream();
                package.SaveAs(stream);
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = "Clients.xlsx";

                return File(stream.ToArray(), contentType, fileName);
            }
        }

        private bool ClientExists(int id)
        {
            return _context.Clients.Any(e => e.ClientID == id);
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
