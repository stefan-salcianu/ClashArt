using ClashArt.Models;

namespace ClashArt.Models
{
    public class HomeViewModel
    {
        // Pentru Feed (jos)
        public IEnumerable<Post> Posts { get; set; }

        // Pentru Hero Section (sus)
        public CompetitionTheme ActiveTheme { get; set; }
    }
}