using CorporateKnowledgeBase.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CorporateKnowledgeBase.Web.Controllers
{
    /// <summary>
    /// API controller for tag-related operations, used by the Tagify.js frontend component.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TagsController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Gets a list of tag names for autocomplete suggestions based on a search term.
        /// </summary>
        /// <param name="searchTerm">The term to search for.</param>
        /// <returns>A JSON array of matching tag names.</returns>
        [HttpGet]
        public async Task<IActionResult> GetTags([FromQuery] string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Ok(new List<string>());
            }

            var tagNames = await _context.Tags
                                         .Where(t => t.Name.StartsWith(searchTerm))
                                         .Select(t => t.Name)
                                         .Take(10)
                                         .ToListAsync();

            return Ok(tagNames);
        }
    }
}