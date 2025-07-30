from fastapi import FastAPI, Depends
from sqlmodel import SQLModel, Session, create_engine, select
from typing import List

from .models import Indicator
from .ingest import fetch_otx, fetch_shodan, fetch_censys, fetch_virustotal
from .llm import summarize_iocs

DATABASE_URL = "sqlite:///./ioc.db"
engine = create_engine(DATABASE_URL, echo=False)

app = FastAPI(title="Sigma Threat Intel API")


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
