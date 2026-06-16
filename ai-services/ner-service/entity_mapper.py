"""
Entity mapping module for scispaCy NER extraction (AIR-001).
Maps scispaCy labels (CHEMICAL, DISEASE) to clinical DataCategory values.
"""

from enum import Enum
from typing import NamedTuple


class DataCategory(str, Enum):
    """Clinical data categories matching .NET DataCategory enum."""
    DIAGNOSIS = "Diagnosis"
    MEDICATION = "Medication"
    ALLERGY = "Allergy"
    PROCEDURE = "Procedure"
    LAB_RESULT = "LabResult"
    VITAL_SIGN = "VitalSign"
    SYMPTOM = "Symptom"
    FAMILY_HISTORY = "FamilyHistory"


class MappedEntity(NamedTuple):
    """Mapped clinical entity with normalized category/key/value."""
    text: str
    label: str
    start: int
    end: int
    confidence: float
    category: str
    key: str
    value: str
    unit: str | None


# scispaCy label to DataCategory mapping
LABEL_CATEGORY_MAP: dict[str, DataCategory] = {
    # en_ner_bc5cdr_md labels
    "CHEMICAL": DataCategory.MEDICATION,
    "DISEASE": DataCategory.DIAGNOSIS,
    # Additional labels from en_ner_bionlp13cg_md if loaded
    "CANCER": DataCategory.DIAGNOSIS,
    "ORGAN": DataCategory.DIAGNOSIS,
    "CELL": DataCategory.LAB_RESULT,
    "SIMPLE_CHEMICAL": DataCategory.MEDICATION,
    "AMINO_ACID": DataCategory.LAB_RESULT,
    "GENE_OR_GENE_PRODUCT": DataCategory.LAB_RESULT,
    # Custom labels for vitals/labs (post-processed)
    "VITAL": DataCategory.VITAL_SIGN,
    "LAB": DataCategory.LAB_RESULT,
}

# Keywords indicating vital signs (contextual post-processing)
VITAL_KEYWORDS = {
    "bp", "blood pressure", "systolic", "diastolic",
    "pulse", "heart rate", "hr", "bpm",
    "temperature", "temp", "fever",
    "respiratory rate", "rr", "breaths",
    "oxygen saturation", "spo2", "o2 sat",
    "weight", "height", "bmi",
}

# Keywords indicating lab results
LAB_KEYWORDS = {
    "glucose", "hba1c", "a1c", "hemoglobin",
    "cholesterol", "ldl", "hdl", "triglycerides",
    "creatinine", "bun", "gfr", "egfr",
    "sodium", "potassium", "chloride", "calcium",
    "wbc", "rbc", "platelet", "hematocrit",
    "alt", "ast", "bilirubin", "albumin",
    "tsh", "t3", "t4", "cortisol",
    "psa", "inr", "pt", "ptt",
}

# Unit extraction patterns
UNIT_PATTERNS: dict[str, list[str]] = {
    DataCategory.VITAL_SIGN.value: ["mmHg", "bpm", "°F", "°C", "breaths/min", "%", "kg", "lb", "cm", "in"],
    DataCategory.LAB_RESULT.value: ["mg/dL", "mmol/L", "g/dL", "U/L", "mEq/L", "ng/mL", "pg/mL", "IU/L", "%"],
    DataCategory.MEDICATION.value: ["mg", "ml", "mcg", "g", "tablet", "capsule", "drops"],
}


class EntityMapper:
    """Maps scispaCy entities to clinical ExtractedData schema."""

    def map_entity(self, text: str, label: str, start: int, end: int, confidence: float, context: str = "") -> MappedEntity:
        """
        Map a scispaCy entity to clinical data category/key/value format.
        
        Args:
            text: Entity text
            label: scispaCy NER label
            start: Start character position
            end: End character position
            confidence: Entity confidence score
            context: Surrounding text for contextual classification
            
        Returns:
            MappedEntity with category, key, value, and unit
        """
        # Determine category from label
        category = self._determine_category(text, label, context)
        
        # Extract key (normalized entity type)
        key = self._extract_key(text, label, category)
        
        # Extract value (the entity text, normalized)
        value = self._normalize_value(text)
        
        # Extract unit if present
        unit = self._extract_unit(text, context, category)

        return MappedEntity(
            text=text,
            label=label,
            start=start,
            end=end,
            confidence=confidence,
            category=category.value if isinstance(category, DataCategory) else category,
            key=key,
            value=value,
            unit=unit,
        )

    def _determine_category(self, text: str, label: str, context: str) -> DataCategory:
        """Determine clinical category from label and context."""
        text_lower = text.lower()
        context_lower = context.lower()
        combined = f"{text_lower} {context_lower}"

        # Check for vital sign indicators
        if any(kw in combined for kw in VITAL_KEYWORDS):
            return DataCategory.VITAL_SIGN

        # Check for lab result indicators
        if any(kw in combined for kw in LAB_KEYWORDS):
            return DataCategory.LAB_RESULT

        # Use label mapping
        if label in LABEL_CATEGORY_MAP:
            return LABEL_CATEGORY_MAP[label]

        # Default to diagnosis for DISEASE-like labels
        if "disease" in label.lower() or "disorder" in label.lower():
            return DataCategory.DIAGNOSIS

        # Default to medication for CHEMICAL-like labels
        if "chem" in label.lower() or "drug" in label.lower():
            return DataCategory.MEDICATION

        return DataCategory.DIAGNOSIS  # Safe default

    def _extract_key(self, text: str, label: str, category: DataCategory) -> str:
        """Extract a normalized key for the entity."""
        # For medications, the key is typically the drug name
        if category == DataCategory.MEDICATION:
            return "medication_name"

        # For diagnoses, the key indicates the condition type
        if category == DataCategory.DIAGNOSIS:
            return "condition"

        # For vitals, try to identify the specific measurement
        if category == DataCategory.VITAL_SIGN:
            text_lower = text.lower()
            if "pressure" in text_lower or "bp" in text_lower:
                return "blood_pressure"
            if "pulse" in text_lower or "heart rate" in text_lower:
                return "heart_rate"
            if "temp" in text_lower:
                return "temperature"
            if "oxygen" in text_lower or "spo2" in text_lower:
                return "oxygen_saturation"
            return "vital_measurement"

        # For labs, try to identify the test type
        if category == DataCategory.LAB_RESULT:
            text_lower = text.lower()
            if "glucose" in text_lower:
                return "blood_glucose"
            if "cholesterol" in text_lower:
                return "cholesterol"
            if "creatinine" in text_lower:
                return "creatinine"
            return "lab_value"

        return label.lower()

    def _normalize_value(self, text: str) -> str:
        """Normalize the entity value."""
        # Strip whitespace and normalize case for consistency
        return text.strip()

    def _extract_unit(self, text: str, context: str, category: DataCategory) -> str | None:
        """Extract measurement unit if present."""
        combined = f"{text} {context}"
        units = UNIT_PATTERNS.get(category.value, [])

        for unit in units:
            if unit.lower() in combined.lower():
                return unit

        return None


# Singleton instance
entity_mapper = EntityMapper()
