using Microsoft.AspNetCore.Mvc;
using Lumera.Data;
using Lumera.Models;
using Microsoft.EntityFrameworkCore;

namespace Lumera.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get featured organizers
            var featuredOrganizers = await _context.Organizers
                .Include(o => o.User)
                .Where(o => o.IsActive && o.AverageRating >= 4.5m)
                .OrderByDescending(o => o.AverageRating)
                .Take(6)
                .ToListAsync();

            ViewBag.FeaturedOrganizers = featuredOrganizers;

            // Get featured suppliers
            var featuredSuppliers = await _context.Suppliers
                .Include(s => s.User)
                .Where(s => s.IsActive && s.AverageRating >= 4.5m)
                .OrderByDescending(s => s.AverageRating)
                .Take(6)
                .ToListAsync();

            ViewBag.FeaturedSuppliers = featuredSuppliers;

            // Get service categories with counts
            var serviceCategories = await _context.Services
                .Where(s => s.IsActive && s.IsApproved)
                .GroupBy(s => s.Category)
                .Select(g => new ServiceCategoryViewModel
                {
                    Category = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(c => c.Count)
                .ToListAsync();

            ViewBag.ServiceCategories = serviceCategories;

            return View();
        }

        public IActionResult AIPlanner()
        {
            return View();
        }

        public IActionResult Index2()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

    }
}