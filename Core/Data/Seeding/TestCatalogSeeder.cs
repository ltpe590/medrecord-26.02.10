using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Core.Data.Seeding
{
    public static class TestCatalogSeeder
    {
        public static void SeedTestCatalogs(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestsCatalog>().HasData(
                new TestsCatalog
                {
                    TestId = 1,
                    TestName = "Complete Blood Count",
                    TestUnit = "cells/µL",
                    NormalRange = "4.5-11.0"
                },
                new TestsCatalog
                {
                    TestId = 2,
                    TestName = "Glucose Fasting",
                    TestUnit = "mg/dL",
                    NormalRange = "70-100"
                },
                new TestsCatalog
                {
                    TestId = 3,
                    TestName = "Cholesterol Total",
                    TestUnit = "mg/dL",
                    NormalRange = "<200"
                }
            );
        }
    }
}
