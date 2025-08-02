using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sigma.Core.Repositories.ThreatIntel;
using Sigma.Data;

namespace Sigma.Core.Domain.Service
{
    public class MitreMappingService
    {
        private static readonly Regex TechniqueRegex = new("T\\d{4}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly ApplicationDbContext _db;

        public MitreMappingService(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<string> MapAndStore(string text)
        {
            var ids = TechniqueRegex.Matches(text)
                .Select(m => m.Value.ToUpperInvariant())
                .Distinct()
                .ToList();
            if (ids.Count == 0)
            {
                return ids;
            }
            foreach (var id in ids)
            {
                _db.Set<MitreMapping>().Add(new MitreMapping { TechniqueId = id });
            }
            _db.SaveChanges();
            return ids;
        }
    }
}
