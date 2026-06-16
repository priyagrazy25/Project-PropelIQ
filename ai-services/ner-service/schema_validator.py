"""
Schema validation module for NER output (AIR-Q03).
Enforces 99% output schema validity for extracted entities.
"""

from dataclasses import dataclass
from typing import Any
import logging

logger = logging.getLogger(__name__)


@dataclass
class ValidationResult:
    """Result of schema validation."""
    is_valid: bool
    errors: list[str]
    warnings: list[str]
    valid_count: int
    total_count: int

    @property
    def validity_percentage(self) -> float:
        """Calculate validity percentage."""
        if self.total_count == 0:
            return 100.0
        return (self.valid_count / self.total_count) * 100


# Required fields for ExtractedData schema
REQUIRED_FIELDS = {"text", "label", "start", "end", "confidence", "category", "key", "value"}

# Optional fields
OPTIONAL_FIELDS = {"unit", "is_low_confidence", "confidence_factors", "source_page"}

# Valid categories matching .NET DataCategory enum
VALID_CATEGORIES = {
    "Diagnosis", "Medication", "Allergy", "Procedure",
    "LabResult", "VitalSign", "Symptom", "FamilyHistory"
}

# Confidence score range
MIN_CONFIDENCE = 0.0
MAX_CONFIDENCE = 1.0


class SchemaValidator:
    """Validates NER extraction output against schema requirements."""

    def __init__(self, target_validity: float = 99.0):
        """
        Initialize validator.
        
        Args:
            target_validity: Target validity percentage (default 99% per AIR-Q03)
        """
        self.target_validity = target_validity

    def validate_entities(self, entities: list[dict[str, Any]]) -> ValidationResult:
        """
        Validate a list of extracted entities against schema.
        
        Args:
            entities: List of entity dictionaries
            
        Returns:
            ValidationResult with validity stats
        """
        errors: list[str] = []
        warnings: list[str] = []
        valid_count = 0
        total_count = len(entities)

        for i, entity in enumerate(entities):
            entity_errors = self._validate_entity(entity, i)
            entity_warnings = self._check_entity_warnings(entity, i)
            
            if not entity_errors:
                valid_count += 1
            else:
                errors.extend(entity_errors)
            
            warnings.extend(entity_warnings)

        result = ValidationResult(
            is_valid=len(errors) == 0,
            errors=errors,
            warnings=warnings,
            valid_count=valid_count,
            total_count=total_count,
        )

        if result.validity_percentage < self.target_validity:
            logger.warning(
                "Schema validity %.2f%% below target %.2f%% (%d/%d valid)",
                result.validity_percentage,
                self.target_validity,
                valid_count,
                total_count
            )

        return result

    def validate_single(self, entity: dict[str, Any]) -> tuple[bool, list[str]]:
        """
        Validate a single entity.
        
        Args:
            entity: Entity dictionary
            
        Returns:
            Tuple of (is_valid, errors)
        """
        errors = self._validate_entity(entity, 0)
        return len(errors) == 0, errors

    def _validate_entity(self, entity: dict[str, Any], index: int) -> list[str]:
        """Validate a single entity and return errors."""
        errors: list[str] = []
        prefix = f"Entity[{index}]"

        # Check required fields
        for field in REQUIRED_FIELDS:
            if field not in entity:
                errors.append(f"{prefix}: Missing required field '{field}'")
            elif entity[field] is None:
                errors.append(f"{prefix}: Field '{field}' is null")

        # Validate field types and values
        if "text" in entity:
            if not isinstance(entity["text"], str):
                errors.append(f"{prefix}: 'text' must be a string")
            elif not entity["text"].strip():
                errors.append(f"{prefix}: 'text' cannot be empty")

        if "label" in entity:
            if not isinstance(entity["label"], str):
                errors.append(f"{prefix}: 'label' must be a string")

        if "start" in entity:
            if not isinstance(entity["start"], int):
                errors.append(f"{prefix}: 'start' must be an integer")
            elif entity["start"] < 0:
                errors.append(f"{prefix}: 'start' cannot be negative")

        if "end" in entity:
            if not isinstance(entity["end"], int):
                errors.append(f"{prefix}: 'end' must be an integer")
            elif "start" in entity and entity["end"] < entity["start"]:
                errors.append(f"{prefix}: 'end' must be >= 'start'")

        if "confidence" in entity:
            confidence = entity["confidence"]
            if not isinstance(confidence, (int, float)):
                errors.append(f"{prefix}: 'confidence' must be a number")
            elif confidence < MIN_CONFIDENCE or confidence > MAX_CONFIDENCE:
                errors.append(f"{prefix}: 'confidence' must be between {MIN_CONFIDENCE} and {MAX_CONFIDENCE}")

        if "category" in entity:
            category = entity["category"]
            if not isinstance(category, str):
                errors.append(f"{prefix}: 'category' must be a string")
            elif category not in VALID_CATEGORIES:
                errors.append(f"{prefix}: 'category' must be one of {VALID_CATEGORIES}")

        if "key" in entity:
            if not isinstance(entity["key"], str):
                errors.append(f"{prefix}: 'key' must be a string")
            elif not entity["key"].strip():
                errors.append(f"{prefix}: 'key' cannot be empty")

        if "value" in entity:
            if not isinstance(entity["value"], str):
                errors.append(f"{prefix}: 'value' must be a string")

        return errors

    def _check_entity_warnings(self, entity: dict[str, Any], index: int) -> list[str]:
        """Check for non-critical issues that produce warnings."""
        warnings: list[str] = []
        prefix = f"Entity[{index}]"

        # Warn about low confidence
        if "confidence" in entity and entity.get("confidence", 1.0) < 0.7:
            warnings.append(f"{prefix}: Low confidence score ({entity['confidence']:.4f})")

        # Warn about very short text
        if "text" in entity and len(entity.get("text", "")) < 3:
            warnings.append(f"{prefix}: Very short entity text")

        # Warn about missing unit for vitals/labs
        category = entity.get("category", "")
        if category in ("VitalSign", "LabResult") and not entity.get("unit"):
            warnings.append(f"{prefix}: {category} entity missing unit")

        return warnings

    def meets_target(self, result: ValidationResult) -> bool:
        """Check if validation result meets target validity."""
        return result.validity_percentage >= self.target_validity


# Singleton instance
schema_validator = SchemaValidator()
