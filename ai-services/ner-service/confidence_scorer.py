"""
Confidence scoring module for NER entities (AIR-Q04, DR-010).
Computes per-entity confidence using model probability + context heuristics.
Flags entities below 0.7 threshold as "Low Confidence".
"""

from dataclasses import dataclass
import re
from typing import NamedTuple


LOW_CONFIDENCE_THRESHOLD = 0.7


class ScoredEntity(NamedTuple):
    """Entity with computed confidence score and flags."""
    text: str
    label: str
    start: int
    end: int
    confidence: float
    is_low_confidence: bool
    confidence_factors: dict


@dataclass
class ConfidenceFactors:
    """Individual factors contributing to confidence score."""
    model_confidence: float
    length_factor: float
    context_factor: float
    case_factor: float
    pattern_factor: float

    def weighted_average(self) -> float:
        """Compute weighted average confidence."""
        weights = {
            "model": 0.50,      # Model's native confidence
            "length": 0.10,     # Entity length heuristic
            "context": 0.20,    # Contextual appropriateness
            "case": 0.10,       # Case consistency
            "pattern": 0.10,    # Pattern matching
        }
        score = (
            self.model_confidence * weights["model"] +
            self.length_factor * weights["length"] +
            self.context_factor * weights["context"] +
            self.case_factor * weights["case"] +
            self.pattern_factor * weights["pattern"]
        )
        return min(1.0, max(0.0, score))


# Known medication patterns (boost confidence)
MEDICATION_PATTERNS = [
    r"\b\d+\s*(mg|ml|mcg|g)\b",  # Dosage patterns
    r"\b(tablet|capsule|injection|solution)\b",
    r"\b(daily|twice|once|bid|tid|qid|prn)\b",
]

# Known disease patterns (boost confidence)
DISEASE_PATTERNS = [
    r"\b(syndrome|disease|disorder|infection|cancer|carcinoma)\b",
    r"\b(chronic|acute|severe|mild|moderate)\b",
    r"\b(type\s*[12]|stage\s*[1-4])\b",
]

# Lab/vital numeric patterns
LAB_VITAL_PATTERNS = [
    r"\b\d+\.?\d*\s*(mg/dL|mmol/L|g/dL|U/L|mEq/L|%|mmHg|bpm)\b",
]


class ConfidenceScorer:
    """Computes confidence scores for NER entities."""

    def __init__(self, threshold: float = LOW_CONFIDENCE_THRESHOLD):
        self.threshold = threshold

    def score_entity(
        self,
        text: str,
        label: str,
        start: int,
        end: int,
        model_confidence: float,
        context: str = ""
    ) -> ScoredEntity:
        """
        Compute confidence score for an entity.
        
        Args:
            text: Entity text
            label: NER label
            start: Start position
            end: End position
            model_confidence: Model's native confidence
            context: Surrounding text for contextual scoring
            
        Returns:
            ScoredEntity with final confidence and flags
        """
        factors = self._compute_factors(text, label, model_confidence, context)
        final_confidence = factors.weighted_average()
        is_low = final_confidence < self.threshold

        return ScoredEntity(
            text=text,
            label=label,
            start=start,
            end=end,
            confidence=round(final_confidence, 4),
            is_low_confidence=is_low,
            confidence_factors={
                "model": factors.model_confidence,
                "length": factors.length_factor,
                "context": factors.context_factor,
                "case": factors.case_factor,
                "pattern": factors.pattern_factor,
            }
        )

    def _compute_factors(
        self,
        text: str,
        label: str,
        model_confidence: float,
        context: str
    ) -> ConfidenceFactors:
        """Compute individual confidence factors."""
        return ConfidenceFactors(
            model_confidence=self._clamp(model_confidence),
            length_factor=self._compute_length_factor(text),
            context_factor=self._compute_context_factor(text, label, context),
            case_factor=self._compute_case_factor(text, label),
            pattern_factor=self._compute_pattern_factor(text, label, context),
        )

    def _clamp(self, value: float) -> float:
        """Clamp value to [0, 1] range."""
        return min(1.0, max(0.0, value))

    def _compute_length_factor(self, text: str) -> float:
        """
        Compute confidence factor based on entity length.
        Too short or too long entities are less reliable.
        """
        length = len(text.strip())
        
        if length < 2:
            return 0.3  # Single char entities are suspicious
        if length < 4:
            return 0.6  # Very short
        if length > 100:
            return 0.5  # Too long, probably extraction error
        if 4 <= length <= 50:
            return 1.0  # Optimal range
        return 0.8  # Moderately long

    def _compute_context_factor(self, text: str, label: str, context: str) -> float:
        """
        Compute confidence factor based on contextual appropriateness.
        """
        if not context:
            return 0.7  # No context available, neutral

        context_lower = context.lower()
        text_lower = text.lower()

        # Check if entity appears in natural context
        if text_lower in context_lower:
            # Appears naturally in context
            base_score = 0.8
        else:
            base_score = 0.6

        # Boost for medical context indicators
        medical_indicators = ["patient", "prescribed", "diagnosed", "mg", "dose", "treatment", "symptoms"]
        if any(ind in context_lower for ind in medical_indicators):
            base_score += 0.1

        return self._clamp(base_score)

    def _compute_case_factor(self, text: str, label: str) -> float:
        """
        Compute confidence factor based on case consistency.
        Medications are often title-case or all-caps.
        """
        if not text:
            return 0.5

        # All caps (abbreviations like "HIV", "BP")
        if text.isupper() and len(text) <= 5:
            return 1.0

        # Title case (proper nouns, drug names)
        if text[0].isupper():
            return 0.9

        # All lowercase (might be incomplete extraction)
        if text.islower():
            return 0.7

        # Mixed case
        return 0.8

    def _compute_pattern_factor(self, text: str, label: str, context: str) -> float:
        """
        Compute confidence factor based on pattern matching.
        Known patterns boost confidence.
        """
        combined = f"{text} {context}"
        
        patterns_to_check = []
        if label == "CHEMICAL":
            patterns_to_check = MEDICATION_PATTERNS
        elif label == "DISEASE":
            patterns_to_check = DISEASE_PATTERNS
        else:
            patterns_to_check = LAB_VITAL_PATTERNS

        for pattern in patterns_to_check:
            if re.search(pattern, combined, re.IGNORECASE):
                return 1.0

        return 0.7  # No pattern match, neutral

    def flag_low_confidence(self, confidence: float) -> str | None:
        """Return flag string if below threshold."""
        if confidence < self.threshold:
            return "Low Confidence"
        return None


# Singleton instance
confidence_scorer = ConfidenceScorer()
