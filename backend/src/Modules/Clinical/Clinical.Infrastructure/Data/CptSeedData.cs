using Clinical.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clinical.Infrastructure.Data;

/// <summary>
/// Seed data for CPT lookup table (AIR-005).
/// Contains common procedure codes used in primary care settings.
/// Source: AMA CPT Code Guidelines.
/// </summary>
public static class CptSeedData
{
    /// <summary>
    /// Seeds the CPT lookup table with common codes.
    /// </summary>
    public static async Task SeedAsync(ClinicalDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.CptCodes.AnyAsync(cancellationToken))
        {
            return; // Already seeded
        }

        var codes = GetCommonCptCodes();
        await context.CptCodes.AddRangeAsync(codes, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<CptCode> GetCommonCptCodes()
    {
        return new[]
        {
            // Evaluation and Management (E/M) Codes (99201-99499)
            new CptCode
            {
                Code = "99213",
                Description = "Office or other outpatient visit for the evaluation and management of an established patient, low complexity",
                ShortDescription = "Office visit, established patient, low",
                Category = "E/M",
                Keywords = "office visit|outpatient|established patient|evaluation|follow-up|routine|checkup",
                RVU = 1.3m,
                IsActive = true
            },
            new CptCode
            {
                Code = "99214",
                Description = "Office or other outpatient visit for the evaluation and management of an established patient, moderate complexity",
                ShortDescription = "Office visit, established patient, moderate",
                Category = "E/M",
                Keywords = "office visit|outpatient|established patient|evaluation|moderate|follow-up",
                RVU = 1.92m,
                IsActive = true
            },
            new CptCode
            {
                Code = "99215",
                Description = "Office or other outpatient visit for the evaluation and management of an established patient, high complexity",
                ShortDescription = "Office visit, established patient, high",
                Category = "E/M",
                Keywords = "office visit|outpatient|established patient|evaluation|complex|comprehensive",
                RVU = 2.8m,
                IsActive = true
            },
            new CptCode
            {
                Code = "99203",
                Description = "Office or other outpatient visit for the evaluation and management of a new patient, low complexity",
                ShortDescription = "Office visit, new patient, low",
                Category = "E/M",
                Keywords = "office visit|outpatient|new patient|evaluation|initial|first visit",
                RVU = 1.6m,
                IsActive = true
            },
            new CptCode
            {
                Code = "99204",
                Description = "Office or other outpatient visit for the evaluation and management of a new patient, moderate complexity",
                ShortDescription = "Office visit, new patient, moderate",
                Category = "E/M",
                Keywords = "office visit|outpatient|new patient|evaluation|moderate|initial",
                RVU = 2.6m,
                IsActive = true
            },
            new CptCode
            {
                Code = "99205",
                Description = "Office or other outpatient visit for the evaluation and management of a new patient, high complexity",
                ShortDescription = "Office visit, new patient, high",
                Category = "E/M",
                Keywords = "office visit|outpatient|new patient|evaluation|complex|comprehensive|initial",
                RVU = 3.5m,
                IsActive = true
            },

            // Preventive Medicine Services
            new CptCode
            {
                Code = "99385",
                Description = "Initial comprehensive preventive medicine evaluation and management, age 18-39",
                ShortDescription = "Preventive visit, new patient, 18-39",
                Category = "Preventive",
                Keywords = "wellness|preventive|annual|physical|checkup|new patient|adult",
                RVU = 2.8m,
                IsActive = true
            },
            new CptCode
            {
                Code = "99386",
                Description = "Initial comprehensive preventive medicine evaluation and management, age 40-64",
                ShortDescription = "Preventive visit, new patient, 40-64",
                Category = "Preventive",
                Keywords = "wellness|preventive|annual|physical|checkup|new patient|adult|middle age",
                RVU = 3.0m,
                IsActive = true
            },
            new CptCode
            {
                Code = "99395",
                Description = "Periodic comprehensive preventive medicine evaluation and management, age 18-39",
                ShortDescription = "Preventive visit, established, 18-39",
                Category = "Preventive",
                Keywords = "wellness|preventive|annual|physical|checkup|established|adult",
                RVU = 2.3m,
                IsActive = true
            },
            new CptCode
            {
                Code = "99396",
                Description = "Periodic comprehensive preventive medicine evaluation and management, age 40-64",
                ShortDescription = "Preventive visit, established, 40-64",
                Category = "Preventive",
                Keywords = "wellness|preventive|annual|physical|checkup|established|adult|middle age",
                RVU = 2.5m,
                IsActive = true
            },

            // Laboratory Procedures
            new CptCode
            {
                Code = "36415",
                Description = "Collection of venous blood by venipuncture",
                ShortDescription = "Venipuncture",
                Category = "Laboratory",
                Keywords = "blood draw|venipuncture|blood collection|phlebotomy|lab draw",
                RVU = 0.17m,
                IsActive = true
            },
            new CptCode
            {
                Code = "81003",
                Description = "Urinalysis, by dip stick or tablet reagent, automated, without microscopy",
                ShortDescription = "Urinalysis, automated",
                Category = "Laboratory",
                Keywords = "urinalysis|urine test|dipstick|UA|urine analysis",
                RVU = 0.04m,
                IsActive = true
            },
            new CptCode
            {
                Code = "85025",
                Description = "Complete blood count (CBC) with differential, automated",
                ShortDescription = "CBC with differential",
                Category = "Laboratory",
                Keywords = "cbc|complete blood count|blood test|differential|hemogram",
                RVU = 0.19m,
                IsActive = true
            },
            new CptCode
            {
                Code = "80053",
                Description = "Comprehensive metabolic panel",
                ShortDescription = "Comprehensive metabolic panel",
                Category = "Laboratory",
                Keywords = "cmp|metabolic panel|chemistry|electrolytes|kidney|liver|glucose",
                RVU = 0.27m,
                IsActive = true
            },
            new CptCode
            {
                Code = "80061",
                Description = "Lipid panel",
                ShortDescription = "Lipid panel",
                Category = "Laboratory",
                Keywords = "lipid|cholesterol|triglycerides|hdl|ldl|lipid profile",
                RVU = 0.21m,
                IsActive = true
            },
            new CptCode
            {
                Code = "83036",
                Description = "Hemoglobin A1c",
                ShortDescription = "Hemoglobin A1c",
                Category = "Laboratory",
                Keywords = "hba1c|a1c|hemoglobin a1c|glycated hemoglobin|diabetes",
                RVU = 0.15m,
                IsActive = true
            },

            // Immunizations
            new CptCode
            {
                Code = "90471",
                Description = "Immunization administration (includes percutaneous, intradermal, subcutaneous, or intramuscular), 1 vaccine",
                ShortDescription = "Vaccine administration, single",
                Category = "Immunization",
                Keywords = "vaccine|immunization|injection|shot|administration",
                RVU = 0.37m,
                IsActive = true
            },
            new CptCode
            {
                Code = "90472",
                Description = "Immunization administration, each additional vaccine",
                ShortDescription = "Vaccine administration, additional",
                Category = "Immunization",
                Keywords = "vaccine|immunization|injection|additional|shot",
                RVU = 0.19m,
                IsActive = true
            },
            new CptCode
            {
                Code = "90715",
                Description = "Tetanus, diphtheria toxoids, and acellular pertussis vaccine (Tdap), for use in individuals 7 years or older",
                ShortDescription = "Tdap vaccine",
                Category = "Immunization",
                Keywords = "tdap|tetanus|diphtheria|pertussis|whooping cough|vaccine",
                RVU = 0.01m,
                IsActive = true
            },
            new CptCode
            {
                Code = "90658",
                Description = "Influenza virus vaccine, trivalent (IIV3), split virus, 0.5 mL dosage",
                ShortDescription = "Flu vaccine",
                Category = "Immunization",
                Keywords = "flu|influenza|flu shot|seasonal flu|vaccine",
                RVU = 0.01m,
                IsActive = true
            },

            // Minor Procedures
            new CptCode
            {
                Code = "12001",
                Description = "Simple repair of superficial wounds, 2.5 cm or less",
                ShortDescription = "Simple wound repair, small",
                Category = "Surgery",
                Keywords = "wound|laceration|repair|suture|stitches|cut|simple repair",
                RVU = 1.14m,
                IsActive = true
            },
            new CptCode
            {
                Code = "12002",
                Description = "Simple repair of superficial wounds, 2.6 cm to 7.5 cm",
                ShortDescription = "Simple wound repair, medium",
                Category = "Surgery",
                Keywords = "wound|laceration|repair|suture|stitches|cut|simple repair",
                RVU = 1.44m,
                IsActive = true
            },
            new CptCode
            {
                Code = "11102",
                Description = "Tangential biopsy of skin",
                ShortDescription = "Skin biopsy, tangential",
                Category = "Surgery",
                Keywords = "biopsy|skin|shave biopsy|lesion|tangential",
                RVU = 0.83m,
                IsActive = true
            },
            new CptCode
            {
                Code = "11104",
                Description = "Punch biopsy of skin",
                ShortDescription = "Skin punch biopsy",
                Category = "Surgery",
                Keywords = "biopsy|skin|punch biopsy|lesion|dermatology",
                RVU = 0.96m,
                IsActive = true
            },

            // Diagnostic Procedures
            new CptCode
            {
                Code = "93000",
                Description = "Electrocardiogram, routine ECG with at least 12 leads; with interpretation and report",
                ShortDescription = "ECG with interpretation",
                Category = "Diagnostic",
                Keywords = "ecg|ekg|electrocardiogram|heart rhythm|cardiac|12 lead",
                RVU = 0.57m,
                IsActive = true
            },
            new CptCode
            {
                Code = "94010",
                Description = "Spirometry, including graphic record, total and timed vital capacity, expiratory flow rate measurement(s)",
                ShortDescription = "Spirometry",
                Category = "Diagnostic",
                Keywords = "spirometry|pulmonary function|breathing test|lung function|pft",
                RVU = 0.41m,
                IsActive = true
            },

            // Injection Procedures
            new CptCode
            {
                Code = "96372",
                Description = "Therapeutic, prophylactic, or diagnostic injection; subcutaneous or intramuscular",
                ShortDescription = "IM/SubQ injection",
                Category = "Injection",
                Keywords = "injection|intramuscular|subcutaneous|therapeutic|medication injection",
                RVU = 0.27m,
                IsActive = true
            },
            new CptCode
            {
                Code = "20610",
                Description = "Arthrocentesis, aspiration and/or injection, major joint or bursa",
                ShortDescription = "Joint injection, major",
                Category = "Injection",
                Keywords = "joint injection|arthrocentesis|aspiration|knee|shoulder|hip|steroid",
                RVU = 1.24m,
                IsActive = true
            },
            new CptCode
            {
                Code = "20605",
                Description = "Arthrocentesis, aspiration and/or injection, intermediate joint or bursa",
                ShortDescription = "Joint injection, intermediate",
                Category = "Injection",
                Keywords = "joint injection|arthrocentesis|wrist|elbow|ankle|steroid",
                RVU = 0.86m,
                IsActive = true
            }
        };
    }
}
