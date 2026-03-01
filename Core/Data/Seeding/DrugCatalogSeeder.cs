using Core.Data.Context;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Core.Data.Seeding
{
    /// <summary>
    /// Seeds the DrugCatalogs table on first startup.
    /// Called from Program.cs after migrations; skipped if any rows already exist.
    /// </summary>
    public static class DrugCatalogSeeder
    {
        private static readonly DrugCatalog[] _drugs = new[]
        {
            // ── Analgesics / Antipyretics ──────────────────────────────────────
            new DrugCatalog { BrandName = "Panadol",         Composition = "Paracetamol",                          Form = "Tablet",    DosageStrength = "500 mg",   Frequency = "Every 4–6 h PRN",    Route = "Oral",       Instructions = "Max 4 g/day; avoid in hepatic failure" },
            new DrugCatalog { BrandName = "Brufen",          Composition = "Ibuprofen",                            Form = "Tablet",    DosageStrength = "400 mg",   Frequency = "Every 8 h with food", Route = "Oral",      Instructions = "Take with meals; avoid in renal impairment" },
            new DrugCatalog { BrandName = "Voltaren",        Composition = "Diclofenac sodium",                   Form = "Tablet",    DosageStrength = "50 mg",    Frequency = "TID",                Route = "Oral",       Instructions = "EC tablet; take with food" },
            new DrugCatalog { BrandName = "Tramadol",        Composition = "Tramadol HCl",                         Form = "Capsule",   DosageStrength = "50 mg",    Frequency = "Every 4–6 h PRN",    Route = "Oral",       Instructions = "Risk of dependency; avoid driving" },
            new DrugCatalog { BrandName = "Cataflam",        Composition = "Diclofenac potassium",                Form = "Tablet",    DosageStrength = "50 mg",    Frequency = "TID",                Route = "Oral",       Instructions = "Faster onset than sodium salt" },
            new DrugCatalog { BrandName = "Mobic",           Composition = "Meloxicam",                            Form = "Tablet",    DosageStrength = "15 mg",    Frequency = "OD",                 Route = "Oral",       Instructions = "COX-2 selective; take with food" },
            new DrugCatalog { BrandName = "Celebrex",        Composition = "Celecoxib",                            Form = "Capsule",   DosageStrength = "200 mg",   Frequency = "OD–BD",              Route = "Oral",       Instructions = "COX-2 inhibitor; avoid sulfonamide allergy" },

            // ── Antibiotics ────────────────────────────────────────────────────
            new DrugCatalog { BrandName = "Amoxil",          Composition = "Amoxicillin",                          Form = "Capsule",   DosageStrength = "500 mg",   Frequency = "TID",                Route = "Oral",       Instructions = "Complete full course" },
            new DrugCatalog { BrandName = "Augmentin",       Composition = "Amoxicillin / Clavulanate",            Form = "Tablet",    DosageStrength = "625 mg",   Frequency = "TID",                Route = "Oral",       Instructions = "Take with meals to reduce GI upset" },
            new DrugCatalog { BrandName = "Ciprofloxacin",   Composition = "Ciprofloxacin HCl",                   Form = "Tablet",    DosageStrength = "500 mg",   Frequency = "BD",                 Route = "Oral",       Instructions = "Avoid antacids; avoid in tendon disorders" },
            new DrugCatalog { BrandName = "Zinnat",          Composition = "Cefuroxime axetil",                   Form = "Tablet",    DosageStrength = "500 mg",   Frequency = "BD",                 Route = "Oral",       Instructions = "Take after food for better absorption" },
            new DrugCatalog { BrandName = "Rocephin",        Composition = "Ceftriaxone sodium",                  Form = "Injection", DosageStrength = "1 g",      Frequency = "OD",                 Route = "IV/IM",      Instructions = "Reconstitute with lidocaine for IM use" },
            new DrugCatalog { BrandName = "Zithromax",       Composition = "Azithromycin",                         Form = "Tablet",    DosageStrength = "500 mg",   Frequency = "OD for 3 days",      Route = "Oral",       Instructions = "Take 1 h before or 2 h after meals" },
            new DrugCatalog { BrandName = "Klacid",          Composition = "Clarithromycin",                       Form = "Tablet",    DosageStrength = "500 mg",   Frequency = "BD",                 Route = "Oral",       Instructions = "Can be taken with or without food" },
            new DrugCatalog { BrandName = "Doxycycline",     Composition = "Doxycycline hyclate",                 Form = "Capsule",   DosageStrength = "100 mg",   Frequency = "BD",                 Route = "Oral",       Instructions = "Take with plenty of water; avoid lying down" },
            new DrugCatalog { BrandName = "Flagyl",          Composition = "Metronidazole",                        Form = "Tablet",    DosageStrength = "500 mg",   Frequency = "TID",                Route = "Oral",       Instructions = "Avoid alcohol; take with food" },
            new DrugCatalog { BrandName = "Trimethoprim-SMX",Composition = "Trimethoprim / Sulfamethoxazole",     Form = "Tablet",    DosageStrength = "80/400 mg",Frequency = "BD",                 Route = "Oral",       Instructions = "Ensure adequate hydration" },
            new DrugCatalog { BrandName = "Vancomycin",      Composition = "Vancomycin HCl",                       Form = "Injection", DosageStrength = "500 mg",   Frequency = "Every 6 h",          Route = "IV",         Instructions = "Infuse over ≥ 60 min; monitor levels" },
            new DrugCatalog { BrandName = "Meropenem",       Composition = "Meropenem trihydrate",                Form = "Injection", DosageStrength = "1 g",      Frequency = "Every 8 h",          Route = "IV",         Instructions = "Reserve for MDR organisms" },

            // ── Antihypertensives ──────────────────────────────────────────────
            new DrugCatalog { BrandName = "Norvasc",         Composition = "Amlodipine besylate",                 Form = "Tablet",    DosageStrength = "5 mg",     Frequency = "OD",                 Route = "Oral",       Instructions = "Once daily; ankle oedema common" },
            new DrugCatalog { BrandName = "Concor",          Composition = "Bisoprolol fumarate",                 Form = "Tablet",    DosageStrength = "5 mg",     Frequency = "OD",                 Route = "Oral",       Instructions = "Do not stop abruptly" },
            new DrugCatalog { BrandName = "Coversyl",        Composition = "Perindopril arginine",                Form = "Tablet",    DosageStrength = "5 mg",     Frequency = "OD",                 Route = "Oral",       Instructions = "Monitor K⁺ and creatinine" },
            new DrugCatalog { BrandName = "Cozaar",          Composition = "Losartan potassium",                  Form = "Tablet",    DosageStrength = "50 mg",    Frequency = "OD",                 Route = "Oral",       Instructions = "ARB; avoid in pregnancy" },
            new DrugCatalog { BrandName = "Micardis",        Composition = "Telmisartan",                          Form = "Tablet",    DosageStrength = "40 mg",    Frequency = "OD",                 Route = "Oral",       Instructions = "Take at same time each day" },
            new DrugCatalog { BrandName = "Lasix",           Composition = "Furosemide",                           Form = "Tablet",    DosageStrength = "40 mg",    Frequency = "OD morning",         Route = "Oral",       Instructions = "Monitor K⁺; avoid hypotension" },
            new DrugCatalog { BrandName = "Aldactone",       Composition = "Spironolactone",                       Form = "Tablet",    DosageStrength = "25 mg",    Frequency = "OD–BD",              Route = "Oral",       Instructions = "K⁺-sparing; monitor K⁺" },
            new DrugCatalog { BrandName = "Cardura",         Composition = "Doxazosin mesylate",                  Form = "Tablet",    DosageStrength = "4 mg",     Frequency = "OD",                 Route = "Oral",       Instructions = "Start low; risk of first-dose hypotension" },

            // ── Cardiovascular ────────────────────────────────────────────────
            new DrugCatalog { BrandName = "Aspirin Low-dose",Composition = "Acetylsalicylic acid",                Form = "Tablet",    DosageStrength = "100 mg",   Frequency = "OD",                 Route = "Oral",       Instructions = "Enteric-coated; antiplatelet use" },
            new DrugCatalog { BrandName = "Plavix",          Composition = "Clopidogrel bisulfate",               Form = "Tablet",    DosageStrength = "75 mg",    Frequency = "OD",                 Route = "Oral",       Instructions = "Take with or without food" },
            new DrugCatalog { BrandName = "Warfarin",        Composition = "Warfarin sodium",                     Form = "Tablet",    DosageStrength = "5 mg",     Frequency = "OD (dose adjusted)", Route = "Oral",       Instructions = "Monitor INR; many drug/food interactions" },
            new DrugCatalog { BrandName = "Eliquis",         Composition = "Apixaban",                             Form = "Tablet",    DosageStrength = "5 mg",     Frequency = "BD",                 Route = "Oral",       Instructions = "No routine monitoring required" },
            new DrugCatalog { BrandName = "Digoxin",         Composition = "Digoxin",                              Form = "Tablet",    DosageStrength = "0.25 mg",  Frequency = "OD",                 Route = "Oral",       Instructions = "Narrow TI; monitor levels and K⁺" },
            new DrugCatalog { BrandName = "Cordarone",       Composition = "Amiodarone HCl",                      Form = "Tablet",    DosageStrength = "200 mg",   Frequency = "OD (maintenance)",   Route = "Oral",       Instructions = "Monitor TFTs and LFTs; photosensitivity" },
            new DrugCatalog { BrandName = "Crestor",         Composition = "Rosuvastatin calcium",                Form = "Tablet",    DosageStrength = "10 mg",    Frequency = "OD",                 Route = "Oral",       Instructions = "Take at same time daily; monitor LFTs" },
            new DrugCatalog { BrandName = "Lipitor",         Composition = "Atorvastatin calcium",                Form = "Tablet",    DosageStrength = "20 mg",    Frequency = "OD (evening)",       Route = "Oral",       Instructions = "Report unexplained muscle pain" },
            new DrugCatalog { BrandName = "Nitroquick",      Composition = "Nitroglycerin",                        Form = "Tablet",    DosageStrength = "0.4 mg",   Frequency = "PRN sublingual",     Route = "Sublingual", Instructions = "Max 3 tabs/15 min; call EMS if no relief" },

            // ── Antidiabetics ─────────────────────────────────────────────────
            new DrugCatalog { BrandName = "Glucophage",      Composition = "Metformin HCl",                       Form = "Tablet",    DosageStrength = "500 mg",   Frequency = "BD–TID with meals",  Route = "Oral",       Instructions = "Hold 48 h before contrast; GI side effects" },
            new DrugCatalog { BrandName = "Januvia",         Composition = "Sitagliptin phosphate",               Form = "Tablet",    DosageStrength = "100 mg",   Frequency = "OD",                 Route = "Oral",       Instructions = "DPP-4 inhibitor; adjust in renal failure" },
            new DrugCatalog { BrandName = "Jardiance",       Composition = "Empagliflozin",                        Form = "Tablet",    DosageStrength = "10 mg",    Frequency = "OD morning",         Route = "Oral",       Instructions = "SGLT-2 inhibitor; genital hygiene advice" },
            new DrugCatalog { BrandName = "Victoza",         Composition = "Liraglutide",                          Form = "Injection", DosageStrength = "1.2 mg",   Frequency = "OD subcut",          Route = "SC",         Instructions = "GLP-1 RA; rotate injection sites" },
            new DrugCatalog { BrandName = "Lantus",          Composition = "Insulin glargine (U-100)",            Form = "Injection", DosageStrength = "100 U/mL", Frequency = "OD bedtime",         Route = "SC",         Instructions = "Basal insulin; do not mix in same syringe" },
            new DrugCatalog { BrandName = "NovoRapid",       Composition = "Insulin aspart",                       Form = "Injection", DosageStrength = "100 U/mL", Frequency = "TID before meals",   Route = "SC",         Instructions = "Inject immediately before meal" },
            new DrugCatalog { BrandName = "Amaryl",          Composition = "Glimepiride",                          Form = "Tablet",    DosageStrength = "2 mg",     Frequency = "OD before breakfast",Route = "Oral",       Instructions = "Sulphonylurea; monitor for hypoglycaemia" },

            // ── Respiratory ───────────────────────────────────────────────────
            new DrugCatalog { BrandName = "Ventolin",        Composition = "Salbutamol sulfate",                  Form = "Inhaler",   DosageStrength = "100 µg/puff", Frequency = "PRN (1–2 puffs)", Route = "Inhaled",    Instructions = "Shake; 1 min between puffs; rinse mouth" },
            new DrugCatalog { BrandName = "Seretide",        Composition = "Fluticasone / Salmeterol",            Form = "Inhaler",   DosageStrength = "250/25 µg",Frequency = "BD",                 Route = "Inhaled",    Instructions = "Rinse mouth after use; not for acute relief" },
            new DrugCatalog { BrandName = "Spiriva",         Composition = "Tiotropium bromide",                  Form = "Inhaler",   DosageStrength = "18 µg",    Frequency = "OD",                 Route = "Inhaled",    Instructions = "LAMA; do not exceed 1 capsule/day" },
            new DrugCatalog { BrandName = "Fluimucil",       Composition = "Acetylcysteine",                       Form = "Tablet",    DosageStrength = "600 mg",   Frequency = "OD",                 Route = "Oral",       Instructions = "Effervescent tablet in water" },
            new DrugCatalog { BrandName = "Montelukast",     Composition = "Montelukast sodium",                  Form = "Tablet",    DosageStrength = "10 mg",    Frequency = "OD evening",         Route = "Oral",       Instructions = "Chew tablet for children" },
            new DrugCatalog { BrandName = "Prednisolone",    Composition = "Prednisolone",                         Form = "Tablet",    DosageStrength = "5 mg",     Frequency = "OD morning",         Route = "Oral",       Instructions = "Take with food; taper slowly" },

            // ── GI ────────────────────────────────────────────────────────────
            new DrugCatalog { BrandName = "Nexium",          Composition = "Esomeprazole magnesium",              Form = "Capsule",   DosageStrength = "40 mg",    Frequency = "OD before breakfast",Route = "Oral",       Instructions = "Swallow whole; 30 min before food" },
            new DrugCatalog { BrandName = "Losec",           Composition = "Omeprazole",                           Form = "Capsule",   DosageStrength = "20 mg",    Frequency = "OD",                 Route = "Oral",       Instructions = "Take 30–60 min before eating" },
            new DrugCatalog { BrandName = "Pantoprazole",    Composition = "Pantoprazole sodium",                 Form = "Tablet",    DosageStrength = "40 mg",    Frequency = "OD",                 Route = "Oral",       Instructions = "Swallow whole; before meals" },
            new DrugCatalog { BrandName = "Motilium",        Composition = "Domperidone",                          Form = "Tablet",    DosageStrength = "10 mg",    Frequency = "TID before meals",   Route = "Oral",       Instructions = "Short-term use only; cardiac caution" },
            new DrugCatalog { BrandName = "Zofran",          Composition = "Ondansetron HCl",                     Form = "Tablet",    DosageStrength = "8 mg",     Frequency = "BD–TID",             Route = "Oral",       Instructions = "ODT dissolves on tongue without water" },
            new DrugCatalog { BrandName = "Loperamide",      Composition = "Loperamide HCl",                      Form = "Capsule",   DosageStrength = "2 mg",     Frequency = "After each loose stool", Route = "Oral",   Instructions = "Max 16 mg/day; not for bloody diarrhea" },
            new DrugCatalog { BrandName = "Duphalac",        Composition = "Lactulose",                            Form = "Syrup",     DosageStrength = "10 g/15 mL", Frequency = "BD–TID",           Route = "Oral",       Instructions = "Dose titrated to produce 2–3 soft stools/day" },
            new DrugCatalog { BrandName = "Colofac",         Composition = "Mebeverine HCl",                      Form = "Tablet",    DosageStrength = "135 mg",   Frequency = "TID before meals",   Route = "Oral",       Instructions = "20 min before meals; for IBS" },

            // ── Endocrine ─────────────────────────────────────────────────────
            new DrugCatalog { BrandName = "Euthyrox",        Composition = "Levothyroxine sodium",                Form = "Tablet",    DosageStrength = "50 µg",    Frequency = "OD fasting",         Route = "Oral",       Instructions = "Take 30–60 min before breakfast; monitor TSH" },
            new DrugCatalog { BrandName = "Neomercazole",    Composition = "Carbimazole",                          Form = "Tablet",    DosageStrength = "5 mg",     Frequency = "TID",                Route = "Oral",       Instructions = "Monitor CBC; report sore throat urgently" },
            new DrugCatalog { BrandName = "Calcium-D3",      Composition = "Calcium carbonate / Vitamin D3",      Form = "Tablet",    DosageStrength = "500 mg/400 IU", Frequency = "BD",              Route = "Oral",       Instructions = "Take with meals; chew or swallow" },

            // ── Neurology / Psychiatry ────────────────────────────────────────
            new DrugCatalog { BrandName = "Sertraline",      Composition = "Sertraline HCl",                      Form = "Tablet",    DosageStrength = "50 mg",    Frequency = "OD morning",         Route = "Oral",       Instructions = "SSRI; 4–6 weeks for full effect" },
            new DrugCatalog { BrandName = "Escitalopram",    Composition = "Escitalopram oxalate",                Form = "Tablet",    DosageStrength = "10 mg",    Frequency = "OD",                 Route = "Oral",       Instructions = "SSRI; take with or without food" },
            new DrugCatalog { BrandName = "Xanax",           Composition = "Alprazolam",                           Form = "Tablet",    DosageStrength = "0.25 mg",  Frequency = "TID PRN",            Route = "Oral",       Instructions = "Benzodiazepine; risk of dependence" },
            new DrugCatalog { BrandName = "Risperdal",       Composition = "Risperidone",                          Form = "Tablet",    DosageStrength = "1 mg",     Frequency = "BD",                 Route = "Oral",       Instructions = "Atypical antipsychotic; monitor metabolic profile" },
            new DrugCatalog { BrandName = "Tegretol",        Composition = "Carbamazepine",                        Form = "Tablet",    DosageStrength = "200 mg",   Frequency = "BD–TID",             Route = "Oral",       Instructions = "Monitor CBC and liver function; many interactions" },
            new DrugCatalog { BrandName = "Epilim",          Composition = "Sodium valproate",                     Form = "Tablet",    DosageStrength = "200 mg",   Frequency = "BD",                 Route = "Oral",       Instructions = "Teratogenic; monitor valproate levels and LFTs" },
            new DrugCatalog { BrandName = "Lyrica",          Composition = "Pregabalin",                           Form = "Capsule",   DosageStrength = "75 mg",    Frequency = "BD",                 Route = "Oral",       Instructions = "Neuropathic pain; dizziness, weight gain common" },
            new DrugCatalog { BrandName = "Imigran",         Composition = "Sumatriptan succinate",               Form = "Tablet",    DosageStrength = "50 mg",    Frequency = "PRN (max 2/24 h)",   Route = "Oral",       Instructions = "For migraine acute; not prophylaxis" },

            // ── Allergology / Dermatology ─────────────────────────────────────
            new DrugCatalog { BrandName = "Clarityne",       Composition = "Loratadine",                           Form = "Tablet",    DosageStrength = "10 mg",    Frequency = "OD",                 Route = "Oral",       Instructions = "Non-sedating antihistamine" },
            new DrugCatalog { BrandName = "Zyrtec",          Composition = "Cetirizine HCl",                      Form = "Tablet",    DosageStrength = "10 mg",    Frequency = "OD evening",         Route = "Oral",       Instructions = "Mild sedation; avoid driving" },
            new DrugCatalog { BrandName = "Phenergan",       Composition = "Promethazine HCl",                    Form = "Tablet",    DosageStrength = "25 mg",    Frequency = "OD at bedtime",      Route = "Oral",       Instructions = "Sedating; avoid in children < 2 yrs" },
            new DrugCatalog { BrandName = "Hydrocortisone Cream", Composition = "Hydrocortisone",                 Form = "Cream",     DosageStrength = "1%",       Frequency = "BD–TID",             Route = "Topical",    Instructions = "Thin layer; avoid face, skin folds" },
            new DrugCatalog { BrandName = "Betamethasone",   Composition = "Betamethasone valerate",              Form = "Cream",     DosageStrength = "0.1%",     Frequency = "BD",                 Route = "Topical",    Instructions = "Potent steroid; avoid prolonged use" },

            // ── Gynaecology / Obstetrics ──────────────────────────────────────
            new DrugCatalog { BrandName = "Progesterone",    Composition = "Micronised progesterone",             Form = "Capsule",   DosageStrength = "200 mg",   Frequency = "OD vaginally at bedtime", Route = "Vaginal", Instructions = "Luteal support; lie down after insertion" },
            new DrugCatalog { BrandName = "Folic Acid",      Composition = "Folic acid",                           Form = "Tablet",    DosageStrength = "5 mg",     Frequency = "OD",                 Route = "Oral",       Instructions = "Pre-conception and first trimester" },
            new DrugCatalog { BrandName = "Ferrous Sulfate", Composition = "Ferrous sulfate",                      Form = "Tablet",    DosageStrength = "200 mg",   Frequency = "BD",                 Route = "Oral",       Instructions = "Take on empty stomach; may cause GI upset" },
            new DrugCatalog { BrandName = "Clomid",          Composition = "Clomifene citrate",                   Form = "Tablet",    DosageStrength = "50 mg",    Frequency = "OD Days 2–6",        Route = "Oral",       Instructions = "Ovulation induction; monitor ovaries" },
            new DrugCatalog { BrandName = "Duphaston",       Composition = "Dydrogesterone",                       Form = "Tablet",    DosageStrength = "10 mg",    Frequency = "BD",                 Route = "Oral",       Instructions = "Progestogen; supports corpus luteum" },
            new DrugCatalog { BrandName = "Provera",         Composition = "Medroxyprogesterone acetate",         Form = "Tablet",    DosageStrength = "10 mg",    Frequency = "OD",                 Route = "Oral",       Instructions = "For cycle regulation; taper as directed" },

            // ── Urology ───────────────────────────────────────────────────────
            new DrugCatalog { BrandName = "Flomax",          Composition = "Tamsulosin HCl",                      Form = "Capsule",   DosageStrength = "0.4 mg",   Frequency = "OD after meal",      Route = "Oral",       Instructions = "Alpha-blocker; orthostatic hypotension risk" },
            new DrugCatalog { BrandName = "Proscar",         Composition = "Finasteride",                          Form = "Tablet",    DosageStrength = "5 mg",     Frequency = "OD",                 Route = "Oral",       Instructions = "5α-RI; monitor PSA; teratogenic (women)" },
            new DrugCatalog { BrandName = "Detrusitol",      Composition = "Tolterodine tartrate",                Form = "Tablet",    DosageStrength = "2 mg",     Frequency = "BD",                 Route = "Oral",       Instructions = "Anticholinergic; avoid in urinary retention" },

            // ── Ophthalmology ─────────────────────────────────────────────────
            new DrugCatalog { BrandName = "Xalatan",         Composition = "Latanoprost",                          Form = "Drops",     DosageStrength = "0.005%",   Frequency = "OD evening",         Route = "Ophthalmic", Instructions = "One drop each eye; refrigerate; may darken iris" },
            new DrugCatalog { BrandName = "Tobradex",        Composition = "Tobramycin / Dexamethasone",           Form = "Drops",     DosageStrength = "0.3%/0.1%",Frequency = "QID",               Route = "Ophthalmic", Instructions = "Shake well; monitor IOP if used > 2 weeks" },
            new DrugCatalog { BrandName = "Systane",         Composition = "Polyethylene glycol / PG",             Form = "Drops",     DosageStrength = "0.4%/0.3%",Frequency = "PRN",               Route = "Ophthalmic", Instructions = "Artificial tears; preservative-free vials" },

            // ── Supplements / IV fluids ───────────────────────────────────────
            new DrugCatalog { BrandName = "Normal Saline",   Composition = "Sodium chloride 0.9%",                Form = "Injection", DosageStrength = "0.9%",     Frequency = "As prescribed",      Route = "IV",         Instructions = "Isotonic; 500 mL/1000 mL bags" },
            new DrugCatalog { BrandName = "Ringer's Lactate",Composition = "Na⁺/K⁺/Ca²⁺/Lactate",               Form = "Injection", DosageStrength = "—",        Frequency = "As prescribed",      Route = "IV",         Instructions = "Balanced crystalloid; preferred for resuscitation" },
            new DrugCatalog { BrandName = "Dextrose 5%",     Composition = "Glucose 5%",                           Form = "Injection", DosageStrength = "5%",       Frequency = "As prescribed",      Route = "IV",         Instructions = "Hypotonic after distribution; no K⁺" },
            new DrugCatalog { BrandName = "Potassium Chloride", Composition = "Potassium chloride",                Form = "Injection", DosageStrength = "15 mEq/10 mL", Frequency = "Diluted in IV", Route = "IV",         Instructions = "NEVER give as undiluted bolus; max 10 mEq/h peripheral" },
            new DrugCatalog { BrandName = "Magnesium Sulfate", Composition = "Magnesium sulfate",                 Form = "Injection", DosageStrength = "50% = 2 g/4 mL", Frequency = "Per protocol", Route = "IV/IM",      Instructions = "Eclampsia: 4 g loading over 20 min; monitor RR, UO, reflexes" },
        };

        public static async Task SeedAsync(ApplicationDbContext db)
        {
            if (await db.DrugCatalogs.AnyAsync()) return;

            db.DrugCatalogs.AddRange(_drugs);
            await db.SaveChangesAsync();
            Console.WriteLine($"[DrugCatalogSeeder] Seeded {_drugs.Length} drugs.");
        }
    }
}
