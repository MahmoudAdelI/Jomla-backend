using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Jomla.Infrastructure.Persistance.Seeders.SeedData;

namespace Jomla.Infrastructure.Persistance.Seeders
{
    internal static class SeedDataLoader
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        public static readonly Dictionary<string, List<CatalogItem>> Products =
            Load<Dictionary<string, List<CatalogItem>>>("product-catalog.json");

        public static readonly Dictionary<string, List<CatalogItem>> GroupRequestTitles =
            Load<Dictionary<string, List<CatalogItem>>>("group-request-titles.json");

        public static readonly Dictionary<string, Dictionary<string, List<string>>> VariantTemplates =
            Load<Dictionary<string, Dictionary<string, List<string>>>>("variant-templates.json");

        private static T Load<T>(string fileName)
        {
            var assembly = typeof(SeedDataLoader).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .Single(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            return JsonSerializer.Deserialize<T>(stream, JsonOptions)
                   ?? throw new InvalidOperationException($"Failed to load seed data: {fileName}");
        }
    }
}
