# scispaCy NER Service Setup

## Prerequisites
- Python 3.10+
- pip

## Installation

```bash
cd ai-services/ner-service
pip install -r requirements.txt
pip install https://s3-us-west-2.amazonaws.com/ai2-s2-scispacy/releases/v0.5.5/en_ner_bc5cdr_md-0.5.5.tar.gz
```

## Run

```bash
uvicorn app:app --host 0.0.0.0 --port 5100
```

## Verify

```bash
curl http://localhost:5100/health
curl -X POST http://localhost:5100/extract -H "Content-Type: application/json" -d '{"text": "Patient takes Metformin 500mg for Type 2 Diabetes"}'
```
