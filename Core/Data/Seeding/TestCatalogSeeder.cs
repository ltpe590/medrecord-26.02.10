using Core.Data.Context;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Core.Data.Seeding
{
    /// <summary>
    /// Seeds the TestCatalogs table.
    /// • SeedTestCatalogs(ModelBuilder) — called from OnModelCreating for migration-based seeding.
    /// • SeedAsync(ApplicationDbContext) — called from Program.cs for runtime seeding of existing DBs.
    /// </summary>
    public static class TestCatalogSeeder
    {
        // ── Master list ───────────────────────────────────────────────────────
        private static readonly TestsCatalog[] _tests = new[]
        {
            // Haematology
            new TestsCatalog { TestId = 1,  TestName = "Haemoglobin (Hb)",          TestUnit = "g/dL",       NormalRange = "12–17",        UnitImperial = "g/dL",    NormalRangeImperial = "12–17" },
            new TestsCatalog { TestId = 2,  TestName = "White Blood Cells (WBC)",    TestUnit = "×10³/µL",    NormalRange = "4.5–11",       UnitImperial = "×10³/µL", NormalRangeImperial = "4.5–11" },
            new TestsCatalog { TestId = 3,  TestName = "Platelets (PLT)",            TestUnit = "×10³/µL",    NormalRange = "150–400",      UnitImperial = "×10³/µL", NormalRangeImperial = "150–400" },
            new TestsCatalog { TestId = 4,  TestName = "Haematocrit (HCT)",          TestUnit = "%",          NormalRange = "38–52",        UnitImperial = "%",       NormalRangeImperial = "38–52" },
            new TestsCatalog { TestId = 5,  TestName = "MCV",                        TestUnit = "fL",         NormalRange = "80–100",       UnitImperial = "fL",      NormalRangeImperial = "80–100" },
            new TestsCatalog { TestId = 6,  TestName = "MCH",                        TestUnit = "pg",         NormalRange = "27–33",        UnitImperial = "pg",      NormalRangeImperial = "27–33" },
            new TestsCatalog { TestId = 7,  TestName = "MCHC",                       TestUnit = "g/dL",       NormalRange = "32–36",        UnitImperial = "g/dL",    NormalRangeImperial = "32–36" },
            new TestsCatalog { TestId = 8,  TestName = "ESR",                        TestUnit = "mm/hr",      NormalRange = "0–20 (M) / 0–30 (F)", UnitImperial = "mm/hr", NormalRangeImperial = "0–20 (M) / 0–30 (F)" },
            new TestsCatalog { TestId = 9,  TestName = "Reticulocytes",              TestUnit = "%",          NormalRange = "0.5–1.5",      UnitImperial = "%",       NormalRangeImperial = "0.5–1.5" },
            new TestsCatalog { TestId = 10, TestName = "Neutrophils",                TestUnit = "%",          NormalRange = "50–70",        UnitImperial = "%",       NormalRangeImperial = "50–70" },
            new TestsCatalog { TestId = 11, TestName = "Lymphocytes",                TestUnit = "%",          NormalRange = "20–40",        UnitImperial = "%",       NormalRangeImperial = "20–40" },
            new TestsCatalog { TestId = 12, TestName = "Monocytes",                  TestUnit = "%",          NormalRange = "2–8",          UnitImperial = "%",       NormalRangeImperial = "2–8" },
            new TestsCatalog { TestId = 13, TestName = "Eosinophils",                TestUnit = "%",          NormalRange = "1–4",          UnitImperial = "%",       NormalRangeImperial = "1–4" },
            new TestsCatalog { TestId = 14, TestName = "Basophils",                  TestUnit = "%",          NormalRange = "0–1",          UnitImperial = "%",       NormalRangeImperial = "0–1" },

            // Coagulation
            new TestsCatalog { TestId = 15, TestName = "PT (Prothrombin Time)",      TestUnit = "sec",        NormalRange = "11–15",        UnitImperial = "sec",     NormalRangeImperial = "11–15" },
            new TestsCatalog { TestId = 16, TestName = "INR",                        TestUnit = "",           NormalRange = "0.8–1.2",      UnitImperial = "",        NormalRangeImperial = "0.8–1.2" },
            new TestsCatalog { TestId = 17, TestName = "aPTT",                       TestUnit = "sec",        NormalRange = "25–35",        UnitImperial = "sec",     NormalRangeImperial = "25–35" },
            new TestsCatalog { TestId = 18, TestName = "D-Dimer",                    TestUnit = "µg/mL FEU",  NormalRange = "< 0.5",        UnitImperial = "µg/mL",   NormalRangeImperial = "< 0.5" },
            new TestsCatalog { TestId = 19, TestName = "Fibrinogen",                 TestUnit = "mg/dL",      NormalRange = "200–400",      UnitImperial = "mg/dL",   NormalRangeImperial = "200–400" },

            // Biochemistry — renal
            new TestsCatalog { TestId = 20, TestName = "Blood Urea Nitrogen (BUN)",  TestUnit = "mg/dL",      NormalRange = "7–20",         UnitImperial = "mg/dL",   NormalRangeImperial = "7–20" },
            new TestsCatalog { TestId = 21, TestName = "Serum Creatinine",           TestUnit = "mg/dL",      NormalRange = "0.6–1.2",      UnitImperial = "mg/dL",   NormalRangeImperial = "0.6–1.2" },
            new TestsCatalog { TestId = 22, TestName = "eGFR",                       TestUnit = "mL/min/1.73m²", NormalRange = "> 90",     UnitImperial = "mL/min/1.73m²", NormalRangeImperial = "> 90" },
            new TestsCatalog { TestId = 23, TestName = "Uric Acid",                  TestUnit = "mg/dL",      NormalRange = "3.5–7.2",      UnitImperial = "mg/dL",   NormalRangeImperial = "3.5–7.2" },

            // Biochemistry — electrolytes
            new TestsCatalog { TestId = 24, TestName = "Sodium (Na⁺)",              TestUnit = "mEq/L",      NormalRange = "135–145",      UnitImperial = "mEq/L",   NormalRangeImperial = "135–145" },
            new TestsCatalog { TestId = 25, TestName = "Potassium (K⁺)",            TestUnit = "mEq/L",      NormalRange = "3.5–5.0",      UnitImperial = "mEq/L",   NormalRangeImperial = "3.5–5.0" },
            new TestsCatalog { TestId = 26, TestName = "Chloride (Cl⁻)",            TestUnit = "mEq/L",      NormalRange = "98–106",       UnitImperial = "mEq/L",   NormalRangeImperial = "98–106" },
            new TestsCatalog { TestId = 27, TestName = "Bicarbonate (HCO₃⁻)",      TestUnit = "mEq/L",      NormalRange = "22–29",        UnitImperial = "mEq/L",   NormalRangeImperial = "22–29" },
            new TestsCatalog { TestId = 28, TestName = "Calcium (Ca²⁺)",            TestUnit = "mg/dL",      NormalRange = "8.5–10.5",     UnitImperial = "mg/dL",   NormalRangeImperial = "8.5–10.5" },
            new TestsCatalog { TestId = 29, TestName = "Magnesium (Mg²⁺)",          TestUnit = "mg/dL",      NormalRange = "1.7–2.2",      UnitImperial = "mg/dL",   NormalRangeImperial = "1.7–2.2" },
            new TestsCatalog { TestId = 30, TestName = "Phosphate (PO₄³⁻)",        TestUnit = "mg/dL",      NormalRange = "2.5–4.5",      UnitImperial = "mg/dL",   NormalRangeImperial = "2.5–4.5" },

            // Biochemistry — liver
            new TestsCatalog { TestId = 31, TestName = "ALT (SGPT)",                TestUnit = "U/L",        NormalRange = "7–56",         UnitImperial = "U/L",     NormalRangeImperial = "7–56" },
            new TestsCatalog { TestId = 32, TestName = "AST (SGOT)",                TestUnit = "U/L",        NormalRange = "10–40",        UnitImperial = "U/L",     NormalRangeImperial = "10–40" },
            new TestsCatalog { TestId = 33, TestName = "ALP",                       TestUnit = "U/L",        NormalRange = "44–147",       UnitImperial = "U/L",     NormalRangeImperial = "44–147" },
            new TestsCatalog { TestId = 34, TestName = "GGT",                       TestUnit = "U/L",        NormalRange = "8–61",         UnitImperial = "U/L",     NormalRangeImperial = "8–61" },
            new TestsCatalog { TestId = 35, TestName = "Total Bilirubin",           TestUnit = "mg/dL",      NormalRange = "0.2–1.2",      UnitImperial = "mg/dL",   NormalRangeImperial = "0.2–1.2" },
            new TestsCatalog { TestId = 36, TestName = "Direct Bilirubin",          TestUnit = "mg/dL",      NormalRange = "0–0.3",        UnitImperial = "mg/dL",   NormalRangeImperial = "0–0.3" },
            new TestsCatalog { TestId = 37, TestName = "Total Protein",             TestUnit = "g/dL",       NormalRange = "6.0–8.3",      UnitImperial = "g/dL",    NormalRangeImperial = "6.0–8.3" },
            new TestsCatalog { TestId = 38, TestName = "Albumin",                   TestUnit = "g/dL",       NormalRange = "3.4–5.4",      UnitImperial = "g/dL",    NormalRangeImperial = "3.4–5.4" },

            // Biochemistry — lipids
            new TestsCatalog { TestId = 39, TestName = "Total Cholesterol",         TestUnit = "mg/dL",      NormalRange = "< 200",        UnitImperial = "mg/dL",   NormalRangeImperial = "< 200" },
            new TestsCatalog { TestId = 40, TestName = "LDL Cholesterol",           TestUnit = "mg/dL",      NormalRange = "< 100",        UnitImperial = "mg/dL",   NormalRangeImperial = "< 100" },
            new TestsCatalog { TestId = 41, TestName = "HDL Cholesterol",           TestUnit = "mg/dL",      NormalRange = "> 40 (M) / > 50 (F)", UnitImperial = "mg/dL", NormalRangeImperial = "> 40 (M) / > 50 (F)" },
            new TestsCatalog { TestId = 42, TestName = "Triglycerides",             TestUnit = "mg/dL",      NormalRange = "< 150",        UnitImperial = "mg/dL",   NormalRangeImperial = "< 150" },
            new TestsCatalog { TestId = 43, TestName = "VLDL Cholesterol",          TestUnit = "mg/dL",      NormalRange = "5–40",         UnitImperial = "mg/dL",   NormalRangeImperial = "5–40" },

            // Glucose / diabetes
            new TestsCatalog { TestId = 44, TestName = "Fasting Blood Glucose",     TestUnit = "mg/dL",      NormalRange = "70–100",       UnitImperial = "mg/dL",   NormalRangeImperial = "70–100" },
            new TestsCatalog { TestId = 45, TestName = "Random Blood Glucose",      TestUnit = "mg/dL",      NormalRange = "< 140",        UnitImperial = "mg/dL",   NormalRangeImperial = "< 140" },
            new TestsCatalog { TestId = 46, TestName = "HbA1c",                     TestUnit = "%",          NormalRange = "< 5.7",        UnitImperial = "%",       NormalRangeImperial = "< 5.7" },
            new TestsCatalog { TestId = 47, TestName = "Insulin (Fasting)",         TestUnit = "µIU/mL",     NormalRange = "2–25",         UnitImperial = "µIU/mL",  NormalRangeImperial = "2–25" },
            new TestsCatalog { TestId = 48, TestName = "C-Peptide",                 TestUnit = "ng/mL",      NormalRange = "0.8–3.1",      UnitImperial = "ng/mL",   NormalRangeImperial = "0.8–3.1" },

            // Thyroid
            new TestsCatalog { TestId = 49, TestName = "TSH",                       TestUnit = "mIU/L",      NormalRange = "0.4–4.0",      UnitImperial = "mIU/L",   NormalRangeImperial = "0.4–4.0" },
            new TestsCatalog { TestId = 50, TestName = "Free T3 (fT3)",             TestUnit = "pg/mL",      NormalRange = "2.0–4.4",      UnitImperial = "pg/mL",   NormalRangeImperial = "2.0–4.4" },
            new TestsCatalog { TestId = 51, TestName = "Free T4 (fT4)",             TestUnit = "ng/dL",      NormalRange = "0.8–1.8",      UnitImperial = "ng/dL",   NormalRangeImperial = "0.8–1.8" },
            new TestsCatalog { TestId = 52, TestName = "Anti-TPO Antibodies",       TestUnit = "IU/mL",      NormalRange = "< 35",         UnitImperial = "IU/mL",   NormalRangeImperial = "< 35" },
            new TestsCatalog { TestId = 53, TestName = "Anti-TG Antibodies",        TestUnit = "IU/mL",      NormalRange = "< 115",        UnitImperial = "IU/mL",   NormalRangeImperial = "< 115" },

            // Cardiac markers
            new TestsCatalog { TestId = 54, TestName = "Troponin I (hsTnI)",        TestUnit = "ng/L",       NormalRange = "< 53",         UnitImperial = "ng/L",    NormalRangeImperial = "< 53" },
            new TestsCatalog { TestId = 55, TestName = "CK-MB",                     TestUnit = "ng/mL",      NormalRange = "0–5",          UnitImperial = "ng/mL",   NormalRangeImperial = "0–5" },
            new TestsCatalog { TestId = 56, TestName = "BNP",                       TestUnit = "pg/mL",      NormalRange = "< 100",        UnitImperial = "pg/mL",   NormalRangeImperial = "< 100" },
            new TestsCatalog { TestId = 57, TestName = "NT-proBNP",                 TestUnit = "pg/mL",      NormalRange = "< 125",        UnitImperial = "pg/mL",   NormalRangeImperial = "< 125" },
            new TestsCatalog { TestId = 58, TestName = "CRP (High-Sensitivity)",    TestUnit = "mg/L",       NormalRange = "< 1.0 (low CV risk)", UnitImperial = "mg/L", NormalRangeImperial = "< 1.0 (low CV risk)" },

            // Inflammatory / infection
            new TestsCatalog { TestId = 59, TestName = "CRP",                       TestUnit = "mg/L",       NormalRange = "< 10",         UnitImperial = "mg/L",    NormalRangeImperial = "< 10" },
            new TestsCatalog { TestId = 60, TestName = "Procalcitonin (PCT)",       TestUnit = "ng/mL",      NormalRange = "< 0.1",        UnitImperial = "ng/mL",   NormalRangeImperial = "< 0.1" },
            new TestsCatalog { TestId = 61, TestName = "Ferritin",                  TestUnit = "ng/mL",      NormalRange = "12–300 (M) / 12–150 (F)", UnitImperial = "ng/mL", NormalRangeImperial = "12–300 (M) / 12–150 (F)" },

            // Hormones — reproductive
            new TestsCatalog { TestId = 62, TestName = "β-hCG (Quantitative)",     TestUnit = "mIU/mL",     NormalRange = "< 5 (non-preg)", UnitImperial = "mIU/mL", NormalRangeImperial = "< 5 (non-preg)" },
            new TestsCatalog { TestId = 63, TestName = "FSH",                       TestUnit = "mIU/mL",     NormalRange = "1.4–18.1",     UnitImperial = "mIU/mL",  NormalRangeImperial = "1.4–18.1" },
            new TestsCatalog { TestId = 64, TestName = "LH",                        TestUnit = "mIU/mL",     NormalRange = "1.9–12.5",     UnitImperial = "mIU/mL",  NormalRangeImperial = "1.9–12.5" },
            new TestsCatalog { TestId = 65, TestName = "Estradiol (E2)",            TestUnit = "pg/mL",      NormalRange = "20–350 (follicular)", UnitImperial = "pg/mL", NormalRangeImperial = "20–350 (follicular)" },
            new TestsCatalog { TestId = 66, TestName = "Progesterone",              TestUnit = "ng/mL",      NormalRange = "0.2–1.5 (follicular)", UnitImperial = "ng/mL", NormalRangeImperial = "0.2–1.5 (follicular)" },
            new TestsCatalog { TestId = 67, TestName = "Prolactin",                 TestUnit = "ng/mL",      NormalRange = "2–18 (F) / 2–18 (M)", UnitImperial = "ng/mL", NormalRangeImperial = "2–18 (F) / 2–18 (M)" },
            new TestsCatalog { TestId = 68, TestName = "Testosterone (Total)",      TestUnit = "ng/dL",      NormalRange = "270–1070 (M)",  UnitImperial = "ng/dL",  NormalRangeImperial = "270–1070 (M)" },
            new TestsCatalog { TestId = 69, TestName = "DHEA-S",                    TestUnit = "µg/dL",      NormalRange = "80–560 (M)",   UnitImperial = "µg/dL",   NormalRangeImperial = "80–560 (M)" },
            new TestsCatalog { TestId = 70, TestName = "Anti-Müllerian Hormone (AMH)", TestUnit = "ng/mL",  NormalRange = "1.0–3.5",      UnitImperial = "ng/mL",   NormalRangeImperial = "1.0–3.5" },

            // Vitamins / minerals
            new TestsCatalog { TestId = 71, TestName = "Vitamin D (25-OH)",         TestUnit = "ng/mL",      NormalRange = "30–100",       UnitImperial = "ng/mL",   NormalRangeImperial = "30–100" },
            new TestsCatalog { TestId = 72, TestName = "Vitamin B12",               TestUnit = "pg/mL",      NormalRange = "200–900",      UnitImperial = "pg/mL",   NormalRangeImperial = "200–900" },
            new TestsCatalog { TestId = 73, TestName = "Folic Acid",                TestUnit = "ng/mL",      NormalRange = "2.7–17",       UnitImperial = "ng/mL",   NormalRangeImperial = "2.7–17" },
            new TestsCatalog { TestId = 74, TestName = "Iron (Serum)",              TestUnit = "µg/dL",      NormalRange = "60–170",       UnitImperial = "µg/dL",   NormalRangeImperial = "60–170" },
            new TestsCatalog { TestId = 75, TestName = "TIBC",                      TestUnit = "µg/dL",      NormalRange = "250–370",      UnitImperial = "µg/dL",   NormalRangeImperial = "250–370" },
            new TestsCatalog { TestId = 76, TestName = "Transferrin Saturation",    TestUnit = "%",          NormalRange = "20–50",        UnitImperial = "%",       NormalRangeImperial = "20–50" },

            // Urinalysis
            new TestsCatalog { TestId = 77, TestName = "Urine Protein",             TestUnit = "mg/dL",      NormalRange = "Negative",     UnitImperial = "mg/dL",   NormalRangeImperial = "Negative" },
            new TestsCatalog { TestId = 78, TestName = "Urine Glucose",             TestUnit = "",           NormalRange = "Negative",     UnitImperial = "",        NormalRangeImperial = "Negative" },
            new TestsCatalog { TestId = 79, TestName = "Urine Ketones",             TestUnit = "",           NormalRange = "Negative",     UnitImperial = "",        NormalRangeImperial = "Negative" },
            new TestsCatalog { TestId = 80, TestName = "Urine RBCs",               TestUnit = "/HPF",       NormalRange = "0–2",          UnitImperial = "/HPF",    NormalRangeImperial = "0–2" },
            new TestsCatalog { TestId = 81, TestName = "Urine WBCs",               TestUnit = "/HPF",       NormalRange = "0–5",          UnitImperial = "/HPF",    NormalRangeImperial = "0–5" },
            new TestsCatalog { TestId = 82, TestName = "Urine pH",                  TestUnit = "",           NormalRange = "4.5–8.0",      UnitImperial = "",        NormalRangeImperial = "4.5–8.0" },
            new TestsCatalog { TestId = 83, TestName = "24h Urine Protein",         TestUnit = "mg/24h",     NormalRange = "< 150",        UnitImperial = "mg/24h",  NormalRangeImperial = "< 150" },
            new TestsCatalog { TestId = 84, TestName = "Microalbumin (Urine)",      TestUnit = "mg/g creat", NormalRange = "< 30",         UnitImperial = "mg/g",    NormalRangeImperial = "< 30" },

            // Microbiology / serology
            new TestsCatalog { TestId = 85, TestName = "HBsAg",                     TestUnit = "",           NormalRange = "Negative",     UnitImperial = "",        NormalRangeImperial = "Negative" },
            new TestsCatalog { TestId = 86, TestName = "Anti-HCV",                  TestUnit = "",           NormalRange = "Negative",     UnitImperial = "",        NormalRangeImperial = "Negative" },
            new TestsCatalog { TestId = 87, TestName = "Anti-HIV 1/2",              TestUnit = "",           NormalRange = "Non-reactive", UnitImperial = "",        NormalRangeImperial = "Non-reactive" },
            new TestsCatalog { TestId = 88, TestName = "Widal Test",                TestUnit = "Titre",      NormalRange = "< 1:80",       UnitImperial = "Titre",   NormalRangeImperial = "< 1:80" },
            new TestsCatalog { TestId = 89, TestName = "Malaria Antigen (RDT)",     TestUnit = "",           NormalRange = "Negative",     UnitImperial = "",        NormalRangeImperial = "Negative" },
            new TestsCatalog { TestId = 90, TestName = "Blood Culture & Sensitivity", TestUnit = "",         NormalRange = "No growth",    UnitImperial = "",        NormalRangeImperial = "No growth" },
            new TestsCatalog { TestId = 91, TestName = "Urine Culture & Sensitivity", TestUnit = "",         NormalRange = "No growth",    UnitImperial = "",        NormalRangeImperial = "No growth" },

            // Tumour markers
            new TestsCatalog { TestId = 92, TestName = "PSA (Total)",               TestUnit = "ng/mL",      NormalRange = "< 4.0",        UnitImperial = "ng/mL",   NormalRangeImperial = "< 4.0" },
            new TestsCatalog { TestId = 93, TestName = "CA-125",                    TestUnit = "U/mL",       NormalRange = "< 35",         UnitImperial = "U/mL",    NormalRangeImperial = "< 35" },
            new TestsCatalog { TestId = 94, TestName = "CEA",                       TestUnit = "ng/mL",      NormalRange = "< 2.5 (non-smoker)", UnitImperial = "ng/mL", NormalRangeImperial = "< 2.5 (non-smoker)" },
            new TestsCatalog { TestId = 95, TestName = "AFP (Alpha-Fetoprotein)",   TestUnit = "ng/mL",      NormalRange = "< 8.1",        UnitImperial = "ng/mL",   NormalRangeImperial = "< 8.1" },
            new TestsCatalog { TestId = 96, TestName = "CA 19-9",                   TestUnit = "U/mL",       NormalRange = "< 37",         UnitImperial = "U/mL",    NormalRangeImperial = "< 37" },
        };

        // ── Migration-based seeding (HasData) ─────────────────────────────────
        /// <summary>Called from ApplicationDbContext.OnModelCreating.</summary>
        public static void SeedTestCatalogs(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestsCatalog>().HasData(_tests);
        }

        // ── Runtime seeding (for existing databases) ──────────────────────────
        /// <summary>Called from Program.cs. Skips seeding if any rows already exist.</summary>
        public static async Task SeedAsync(ApplicationDbContext db)
        {
            if (await db.TestCatalogs.AnyAsync()) return;

            db.TestCatalogs.AddRange(_tests);
            await db.SaveChangesAsync();
            Console.WriteLine($"[TestCatalogSeeder] Seeded {_tests.Length} lab tests.");
        }
    }
}
