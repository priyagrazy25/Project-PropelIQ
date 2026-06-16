"""
scispaCy NER microservice for biomedical Named Entity Recognition (AIR-001).
Uses en_ner_bc5cdr_md model for medication, disease, and chemical entity extraction.
Implements entity mapping, confidence scoring, and schema validation (AIR-Q03, AIR-Q04, DR-010).
"""

from contextlib import asynccontextmanager
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
import spacy
import logging

from entity_mapper import entity_mapper, MappedEntity
from confidence_scorer import confidence_scorer, LOW_CONFIDENCE_THRESHOLD
from schema_validator import schema_validator

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

nlp = None


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Application lifespan for model loading."""
    global nlp
    # Try loading the biomedical model, fallback to general model
    models_to_try = ["en_ner_bc5cdr_md", "en_core_web_sm"]
    for model_name in models_to_try:
        try:
            logger.info(f"Attempting to load spaCy model {model_name}...")
            nlp = spacy.load(model_name)
            logger.info(f"spaCy model {model_name} loaded successfully.")
            break
        except OSError:
            logger.warning(f"Model {model_name} not found, trying next...")
    if nlp is None:
        raise RuntimeError("No spaCy model could be loaded. Install en_core_web_sm: py -m spacy download en_core_web_sm")
    yield
    logger.info("Shutting down NER service.")


app = FastAPI(
    title="scispaCy NER Service",
    version="2.0.0",
    description="Biomedical NER extraction with confidence scoring and schema validation",
    lifespan=lifespan,
)


class ExtractRequest(BaseModel):
    """Request model for NER extraction."""
    text: str = Field(..., description="Text to extract entities from")
    context: str = Field(default="", description="Optional surrounding context for better scoring")
    source_page: int | None = Field(default=None, description="Source page number if from document")


class Entity(BaseModel):
    """Basic entity model (legacy compatibility)."""
    text: str
    label: str
    start: int
    end: int
    confidence: float


class MappedEntityResponse(BaseModel):
    """Full mapped entity with category/key/value."""
    text: str
    label: str
    start: int
    end: int
    confidence: float
    category: str
    key: str
    value: str
    unit: str | None
    is_low_confidence: bool
    source_page: int | None = None


class ExtractResponse(BaseModel):
    """Response model for basic extraction (legacy compatibility)."""
    entities: list[Entity]


class ClinicalExtractResponse(BaseModel):
    """Response model for clinical extraction with full mapping."""
    entities: list[MappedEntityResponse]
    total_count: int
    low_confidence_count: int
    schema_validity_percent: float
    validation_errors: list[str] = []
    validation_warnings: list[str] = []


@app.get("/health")
def health():
    """Health check endpoint."""
    if nlp is None:
        raise HTTPException(status_code=503, detail="Model not loaded")
    return {"status": "healthy", "model": "en_ner_bc5cdr_md", "version": "2.0.0"}


@app.post("/extract", response_model=ExtractResponse)
def extract_entities(request: ExtractRequest):
    """
    Basic NER extraction (legacy endpoint).
    Returns raw scispaCy entities without clinical mapping.
    """
    if nlp is None:
        raise HTTPException(status_code=503, detail="Model not loaded")

    if not request.text or not request.text.strip():
        return ExtractResponse(entities=[])

    doc = nlp(request.text)
    entities = [
        Entity(
            text=ent.text,
            label=ent.label_,
            start=ent.start_char,
            end=ent.end_char,
            confidence=round(float(ent.kb_id_) if ent.kb_id_ else 0.85, 4),
        )
        for ent in doc.ents
    ]

    logger.info("Extracted %d entities from %d chars.", len(entities), len(request.text))
    return ExtractResponse(entities=entities)


@app.post("/extract/clinical", response_model=ClinicalExtractResponse)
def extract_clinical_entities(request: ExtractRequest):
    """
    Clinical NER extraction with entity mapping, confidence scoring, and schema validation.
    
    - Maps scispaCy labels to clinical DataCategory (AIR-001)
    - Computes per-entity confidence scores (AIR-Q04)
    - Flags entities below 0.7 confidence (DR-010)
    - Validates output schema (99% validity target per AIR-Q03)
    """
    if nlp is None:
        raise HTTPException(status_code=503, detail="Model not loaded")

    if not request.text or not request.text.strip():
        return ClinicalExtractResponse(
            entities=[],
            total_count=0,
            low_confidence_count=0,
            schema_validity_percent=100.0,
        )

    doc = nlp(request.text)
    context = request.context or request.text
    
    mapped_entities: list[MappedEntityResponse] = []
    low_confidence_count = 0

    for ent in doc.ents:
        # Get model confidence (default to 0.85 if not available)
        model_conf = float(ent.kb_id_) if ent.kb_id_ else 0.85

        # Score entity with context-aware confidence
        scored = confidence_scorer.score_entity(
            text=ent.text,
            label=ent.label_,
            start=ent.start_char,
            end=ent.end_char,
            model_confidence=model_conf,
            context=context,
        )

        # Map to clinical schema
        mapped = entity_mapper.map_entity(
            text=scored.text,
            label=scored.label,
            start=scored.start,
            end=scored.end,
            confidence=scored.confidence,
            context=context,
        )

        is_low_conf = scored.confidence < LOW_CONFIDENCE_THRESHOLD
        if is_low_conf:
            low_confidence_count += 1

        mapped_entities.append(MappedEntityResponse(
            text=mapped.text,
            label=mapped.label,
            start=mapped.start,
            end=mapped.end,
            confidence=scored.confidence,
            category=mapped.category,
            key=mapped.key,
            value=mapped.value,
            unit=mapped.unit,
            is_low_confidence=is_low_conf,
            source_page=request.source_page,
        ))

    # Validate output schema
    entities_dicts = [e.model_dump() for e in mapped_entities]
    validation_result = schema_validator.validate_entities(entities_dicts)

    logger.info(
        "Clinical extraction: %d entities, %d low-confidence, %.2f%% schema validity",
        len(mapped_entities),
        low_confidence_count,
        validation_result.validity_percentage,
    )

    return ClinicalExtractResponse(
        entities=mapped_entities,
        total_count=len(mapped_entities),
        low_confidence_count=low_confidence_count,
        schema_validity_percent=validation_result.validity_percentage,
        validation_errors=validation_result.errors,
        validation_warnings=validation_result.warnings,
    )


@app.post("/extract/batch", response_model=list[ClinicalExtractResponse])
def extract_batch(requests: list[ExtractRequest]):
    """
    Batch clinical extraction for multiple text chunks.
    Useful for processing all chunks from a document.
    """
    if nlp is None:
        raise HTTPException(status_code=503, detail="Model not loaded")

    results = []
    for req in requests:
        result = extract_clinical_entities(req)
        results.append(result)

    logger.info("Batch extraction: %d chunks processed.", len(results))
    return results

