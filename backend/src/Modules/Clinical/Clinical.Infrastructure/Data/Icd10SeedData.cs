using Clinical.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clinical.Infrastructure.Data;

/// <summary>
/// Seed data for ICD-10-CM lookup table (AIR-004).
/// Contains common diagnosis codes used in primary care settings.
/// Source: CMS ICD-10-CM Official Guidelines.
/// </summary>
public static class Icd10SeedData
{
    /// <summary>
    /// Seeds the ICD-10 lookup table with common codes.
    /// </summary>
    public static async Task SeedAsync(ClinicalDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Icd10Codes.AnyAsync(cancellationToken))
        {
            return; // Already seeded
        }

        var codes = GetCommonIcd10Codes();
        await context.Icd10Codes.AddRangeAsync(codes, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<Icd10Code> GetCommonIcd10Codes()
    {
        return new[]
        {
            // Endocrine, Nutritional, and Metabolic Diseases (E00-E89)
            new Icd10Code
            {
                Code = "E11.9",
                Description = "Type 2 diabetes mellitus without complications",
                ShortDescription = "Type 2 diabetes",
                Category = "Endocrine",
                Keywords = "diabetes|diabetic|type 2|dm2|t2dm|high blood sugar|hyperglycemia",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "E11.65",
                Description = "Type 2 diabetes mellitus with hyperglycemia",
                ShortDescription = "Type 2 diabetes w/ hyperglycemia",
                Category = "Endocrine",
                Keywords = "diabetes|diabetic|hyperglycemia|high blood sugar|elevated glucose",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "E11.8",
                Description = "Type 2 diabetes mellitus with unspecified complications",
                ShortDescription = "Type 2 diabetes w/ complications",
                Category = "Endocrine",
                Keywords = "diabetes|diabetic|complications",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "E10.9",
                Description = "Type 1 diabetes mellitus without complications",
                ShortDescription = "Type 1 diabetes",
                Category = "Endocrine",
                Keywords = "diabetes|diabetic|type 1|dm1|t1dm|juvenile|insulin dependent",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "E78.5",
                Description = "Hyperlipidemia, unspecified",
                ShortDescription = "High cholesterol",
                Category = "Endocrine",
                Keywords = "cholesterol|lipid|hyperlipidemia|dyslipidemia|high cholesterol",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "E78.0",
                Description = "Pure hypercholesterolemia, unspecified",
                ShortDescription = "Pure hypercholesterolemia",
                Category = "Endocrine",
                Keywords = "cholesterol|hypercholesterolemia|elevated cholesterol|ldl",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "E66.9",
                Description = "Obesity, unspecified",
                ShortDescription = "Obesity",
                Category = "Endocrine",
                Keywords = "obesity|obese|overweight|bmi|weight",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "E03.9",
                Description = "Hypothyroidism, unspecified",
                ShortDescription = "Hypothyroidism",
                Category = "Endocrine",
                Keywords = "hypothyroid|thyroid|low thyroid|tsh|underactive thyroid",
                IsBillable = true
            },

            // Circulatory System (I00-I99)
            new Icd10Code
            {
                Code = "I10",
                Description = "Essential (primary) hypertension",
                ShortDescription = "Hypertension",
                Category = "Circulatory",
                Keywords = "hypertension|high blood pressure|htn|bp|elevated blood pressure",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "I11.9",
                Description = "Hypertensive heart disease without heart failure",
                ShortDescription = "Hypertensive heart disease",
                Category = "Circulatory",
                Keywords = "hypertension|heart|cardiac|hypertensive heart",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "I25.10",
                Description = "Atherosclerotic heart disease of native coronary artery without angina pectoris",
                ShortDescription = "Coronary artery disease",
                Category = "Circulatory",
                Keywords = "cad|coronary|atherosclerosis|heart disease|cardiovascular",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "I48.91",
                Description = "Unspecified atrial fibrillation",
                ShortDescription = "Atrial fibrillation",
                Category = "Circulatory",
                Keywords = "afib|atrial fibrillation|irregular heartbeat|arrhythmia",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "I50.9",
                Description = "Heart failure, unspecified",
                ShortDescription = "Heart failure",
                Category = "Circulatory",
                Keywords = "heart failure|chf|congestive|cardiac failure",
                IsBillable = true
            },

            // Respiratory System (J00-J99)
            new Icd10Code
            {
                Code = "J45.20",
                Description = "Mild intermittent asthma, uncomplicated",
                ShortDescription = "Mild intermittent asthma",
                Category = "Respiratory",
                Keywords = "asthma|mild|intermittent|wheeze|breathing",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "J45.30",
                Description = "Mild persistent asthma, uncomplicated",
                ShortDescription = "Mild persistent asthma",
                Category = "Respiratory",
                Keywords = "asthma|mild|persistent|wheeze|breathing",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "J45.909",
                Description = "Unspecified asthma, uncomplicated",
                ShortDescription = "Asthma unspecified",
                Category = "Respiratory",
                Keywords = "asthma|wheeze|breathing|reactive airway",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "J44.9",
                Description = "Chronic obstructive pulmonary disease, unspecified",
                ShortDescription = "COPD",
                Category = "Respiratory",
                Keywords = "copd|chronic obstructive|emphysema|bronchitis|pulmonary",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "J06.9",
                Description = "Acute upper respiratory infection, unspecified",
                ShortDescription = "Upper respiratory infection",
                Category = "Respiratory",
                Keywords = "uri|cold|upper respiratory|infection|rhinitis",
                IsBillable = true
            },

            // Mental and Behavioral Disorders (F00-F99)
            new Icd10Code
            {
                Code = "F32.9",
                Description = "Major depressive disorder, single episode, unspecified",
                ShortDescription = "Depression",
                Category = "Mental",
                Keywords = "depression|depressive|mood|sad|mdd",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "F41.1",
                Description = "Generalized anxiety disorder",
                ShortDescription = "Anxiety disorder",
                Category = "Mental",
                Keywords = "anxiety|gad|anxious|worry|nervousness",
                IsBillable = true
            },

            // Musculoskeletal (M00-M99)
            new Icd10Code
            {
                Code = "M54.5",
                Description = "Low back pain",
                ShortDescription = "Low back pain",
                Category = "Musculoskeletal",
                Keywords = "back pain|low back|lumbar|lbp|spine",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "M17.9",
                Description = "Osteoarthritis of knee, unspecified",
                ShortDescription = "Knee osteoarthritis",
                Category = "Musculoskeletal",
                Keywords = "osteoarthritis|knee|arthritis|joint|oa",
                IsBillable = true
            },

            // Digestive System (K00-K95)
            new Icd10Code
            {
                Code = "K21.0",
                Description = "Gastro-esophageal reflux disease with esophagitis",
                ShortDescription = "GERD with esophagitis",
                Category = "Digestive",
                Keywords = "gerd|reflux|heartburn|acid|esophagitis",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "K21.9",
                Description = "Gastro-esophageal reflux disease without esophagitis",
                ShortDescription = "GERD",
                Category = "Digestive",
                Keywords = "gerd|reflux|heartburn|acid reflux",
                IsBillable = true
            },

            // Genitourinary (N00-N99)
            new Icd10Code
            {
                Code = "N39.0",
                Description = "Urinary tract infection, site not specified",
                ShortDescription = "UTI",
                Category = "Genitourinary",
                Keywords = "uti|urinary|infection|bladder|cystitis",
                IsBillable = true
            },

            // Skin (L00-L99)
            new Icd10Code
            {
                Code = "L30.9",
                Description = "Dermatitis, unspecified",
                ShortDescription = "Dermatitis",
                Category = "Skin",
                Keywords = "dermatitis|rash|skin|eczema|inflammation",
                IsBillable = true
            },

            // Symptoms and Signs (R00-R99)
            new Icd10Code
            {
                Code = "R05.9",
                Description = "Cough, unspecified",
                ShortDescription = "Cough",
                Category = "Symptoms",
                Keywords = "cough|coughing",
                IsBillable = true
            },
            new Icd10Code
            {
                Code = "R51.9",
                Description = "Headache, unspecified",
                ShortDescription = "Headache",
                Category = "Symptoms",
                Keywords = "headache|head pain|cephalgia|migraine",
                IsBillable = true
            },
        };
    }
}
