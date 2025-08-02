"""Main FastAPI application for the Python backend.

This module now also contains a small notification system which can either
produce messages to Kafka or deliver them to a webhook.  The notifier is very
light‑weight and is primarily intended to support the demo subscription feature
implemented in the .NET API and the front‑end.

The behaviour is controlled through the following environment variables:

* ``KAFKA_BROKER`` – address of a Kafka broker.  When provided a producer will
  be created and each IOC will be written to the topic specified by
  ``KAFKA_TOPIC`` (default: ``"sigma.alerts"``).
* ``WEBHOOK_URL`` – if set each IOC will also be sent via HTTP POST to the
  given URL.  This acts as a simple webhook mechanism for downstream services
  that do not consume Kafka.

Both mechanisms can be used together.  Failures are logged but do not interrupt
normal processing of IOC ingestion.
"""

from typing import List
import json
import logging
import os

import requests
from fastapi import Depends, FastAPI
from sqlmodel import SQLModel, Session, create_engine, select

try:  # Kafka is optional; fall back gracefully if the package is missing
    from kafka import KafkaProducer  # type: ignore
except Exception:  # pragma: no cover - best effort import
    KafkaProducer = None

from .ingest import fetch_censys, fetch_otx, fetch_shodan, fetch_virustotal
from .llm import summarize_iocs
from .models import Indicator


DATABASE_URL = "sqlite:///./ioc.db"
engine = create_engine(DATABASE_URL, echo=False)

app = FastAPI(title="Sigma Threat Intel API")


# ---------------------------------------------------------------------------
# Notification utilities
# ---------------------------------------------------------------------------
KAFKA_BROKER = os.getenv("KAFKA_BROKER")
KAFKA_TOPIC = os.getenv("KAFKA_TOPIC", "sigma.alerts")
WEBHOOK_URL = os.getenv("WEBHOOK_URL")

_producer = None
if KAFKA_BROKER and KafkaProducer is not None:
    try:
        _producer = KafkaProducer(
            bootstrap_servers=KAFKA_BROKER,
            value_serializer=lambda v: json.dumps(v).encode("utf-8"),
        )
    except Exception as exc:  # pragma: no cover - logging is best effort
        logging.error("Failed to create Kafka producer: %s", exc)


def _notify(ioc: Indicator) -> None:
    """Send IOC information to Kafka and/or webhook subscribers."""

    payload = ioc.model_dump()

    if _producer is not None:
        try:
            _producer.send(KAFKA_TOPIC, payload)
        except Exception as exc:  # pragma: no cover - logging is best effort
            logging.error("Kafka send failed: %s", exc)

    if WEBHOOK_URL:
        try:
            requests.post(WEBHOOK_URL, json=payload, timeout=5)
        except Exception as exc:  # pragma: no cover - logging is best effort
            logging.error("Webhook post failed: %s", exc)


@app.on_event("startup")
def on_startup():
    SQLModel.metadata.create_all(engine)


def get_session():
    with Session(engine) as session:
        yield session


@app.post("/ingest", response_model=List[Indicator])
def ingest_iocs(session: Session = Depends(get_session)):
    iocs = []
    iocs.extend(fetch_otx())
    iocs.extend(fetch_shodan())
    iocs.extend(fetch_censys())
    for ip in {ioc.ioc for ioc in iocs if ioc.source != "VirusTotal"}:
        iocs.extend(fetch_virustotal(ip))
    for ioc in iocs:
        session.add(ioc)
        _notify(ioc)
    session.commit()
    return iocs


@app.get("/iocs", response_model=List[Indicator])
def list_iocs(session: Session = Depends(get_session)):
    statement = select(Indicator).order_by(Indicator.first_seen.desc()).limit(100)
    results = session.exec(statement).all()
    return results


@app.get("/summary")
async def summary(session: Session = Depends(get_session)):
    statement = select(Indicator.ioc).order_by(Indicator.first_seen.desc()).limit(50)
    iocs = session.exec(statement).all()
    text = await summarize_iocs([ioc for ioc in iocs])
    return {"summary": text}
