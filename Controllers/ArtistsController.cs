using ClashArt.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClashArt.Controllers
{
    public class ArtistsController : Controller
    {
        // Și în constructor
        private readonly ApplicationDbContext _context;
        public ArtistsController(ApplicationDbContext context)
        {
            _context = context;
        }
    }
}
