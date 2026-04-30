import json
import re
import sys

from presidio_analyzer import AnalyzerEngine
from presidio_analyzer.nlp_engine import NlpEngineProvider
from presidio_anonymizer import AnonymizerEngine
from presidio_anonymizer.entities import OperatorConfig


def build_analyzer(language: str, spacy_model: str) -> AnalyzerEngine:
    configuration = {
        "nlp_engine_name": "spacy",
        "models": [{"lang_code": language, "model_name": spacy_model}],
    }
    provider = NlpEngineProvider(nlp_configuration=configuration)
    return AnalyzerEngine(
        nlp_engine=provider.create_engine(),
        supported_languages=[language],
    )


def replacement_for(entity_type: str) -> str:
    replacements = {
        "PERSON": "Client",
        "PHONE_NUMBER": "PhoneNumber",
        "EMAIL_ADDRESS": "EmailAddress",
        "US_SSN": "Identifier",
        "US_DRIVER_LICENSE": "Identifier",
        "US_PASSPORT": "Identifier",
        "CREDIT_CARD": "Identifier",
        "DATE_TIME": "Date",
        "LOCATION": "Location",
        "IP_ADDRESS": "Identifier",
        "URL": "Url",
    }
    return replacements.get(entity_type, entity_type.title().replace("_", ""))


def redact_labeled_names(text: str) -> str:
    # Presidio's NLP model can miss uncommon names. Field labels are deterministic and safe to redact locally.
    labeled_name_pattern = re.compile(
        r"\b((?:Patient|Client|Member)?\s*Name\s*[:\-]\s*)"
        r"([A-Z][A-Za-z'\-]+(?:\s+[A-Z][A-Za-z'\-]+){0,4})"
        r"(?=\s*[\.;,\r\n]|$)",
        re.IGNORECASE,
    )
    return labeled_name_pattern.sub(r"\1Client", text)


def redact(payload: dict) -> dict:
    text = payload.get("text") or ""
    language = payload.get("language") or "en"
    spacy_model = payload.get("spacy_model") or "en_core_web_sm"
    entities = payload.get("entities")

    analyzer = build_analyzer(language, spacy_model)
    analyzer_results = analyzer.analyze(
        text=text,
        language=language,
        entities=entities,
    )

    operators = {
        result.entity_type: OperatorConfig(
            "replace",
            {"new_value": replacement_for(result.entity_type)},
        )
        for result in analyzer_results
    }
    operators["DEFAULT"] = OperatorConfig("replace", {"new_value": "Redacted"})

    anonymized = AnonymizerEngine().anonymize(
        text=text,
        analyzer_results=analyzer_results,
        operators=operators,
    )
    redacted_text = redact_labeled_names(anonymized.text)

    return {
        "redacted_text": redacted_text,
        "entities": sorted({result.entity_type for result in analyzer_results}),
        "items": len(analyzer_results),
    }


def main() -> int:
    try:
        payload = json.loads(sys.stdin.buffer.read().decode("utf-8-sig"))
        print(json.dumps(redact(payload)))
        return 0
    except Exception as exc:
        print(json.dumps({"error": str(exc)}))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
