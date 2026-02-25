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
                    TestId             = 1,
                    TestName           = "Complete Blood Count",
                    TestUnit           = "×10⁹/L",
                    NormalRange        = "4.5–11.0",
                    UnitImperial       = "cells/µL",
                    NormalRangeImperial= "4500–11000"
                },
                new TestsCatalog
                {
                    TestId             = 2,
                    TestName           = "Glucose Fasting",
                    TestUnit           = "mmol/L",
                    NormalRange        = "3.9–6.1",
                    UnitImperial       = "mg/dL",
                    NormalRangeImperial= "70–110"
                },
                new TestsCatalog
                {
                    TestId             = 3,
                    TestName           = "Cholesterol Total",
                    TestUnit           = "mmol/L",
                    NormalRange        = "<5.2",
                    UnitImperial       = "mg/dL",
                    NormalRangeImperial= "<200"
                }
            );
        }
    }
}
