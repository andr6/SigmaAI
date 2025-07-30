import os
import re
import sys
import json
from io import BytesIO

import requests
from bs4 import BeautifulSoup
from PyPDF2 import PdfReader

try:
    from stix2 import (Bundle, Report, ObservedData, AttackPattern,
                       IntrusionSet, IPv4Address, DomainName, URL as StixURL,
                       File)
except ImportError:
    Bundle = Report = ObservedData = AttackPattern = IntrusionSet = IPv4Address = DomainName = StixURL = File = None


def _extract_text_from_pdf(data: bytes) -> str:
    reader = PdfReader(BytesIO(data))
    text = ""
    for page in reader.pages:
        if page.extract_text():
            text += page.extract_text() + "\n"
    return text


def fetch_text(source: str) -> str:
    """Fetch text from a local file path or URL."""
    if source.startswith("http://") or source.startswith("https://"):
        resp = requests.get(source, timeout=10)
        resp.raise_for_status()
        content_type = resp.headers.get("Content-Type", "").lower()
        if "pdf" in content_type or source.lower().endswith(".pdf"):
            return _extract_text_from_pdf(resp.content)
        soup = BeautifulSoup(resp.text, "html.parser")
        return soup.get_text(separator="\n")
    else:
        if source.lower().endswith(".pdf"):
            with open(source, "rb") as f:
                return _extract_text_from_pdf(f.read())
        with open(source, "r", errors="ignore") as f:
            return f.read()


def parse_report(text: str) -> dict:
    """Extract IOCs, MITRE techniques and actors from text."""
    ips = re.findall(r"\b(?:\d{1,3}\.){3}\d{1,3}\b", text)
    domains = re.findall(r"\b(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}\b", text)
    urls = re.findall(r"https?://[^\s'\"<>]+", text)
    hashes = re.findall(r"\b[A-Fa-f0-9]{32,64}\b", text)
    cves = re.findall(r"CVE-\d{4}-\d{4,7}", text)
    mitre = re.findall(r"T\d{4,5}", text)
    actors = re.findall(r"APT\d{1,3}|[A-Za-z]+(?:Team|Group|Gang|Cartel)", text)
    return {
        "ips": sorted(set(ips)),
        "domains": sorted(set(domains)),
        "urls": sorted(set(urls)),
        "hashes": sorted(set(hashes)),
        "cves": sorted(set(cves)),
        "mitre": sorted(set(mitre)),
        "actors": sorted(set(actors)),
    }


def summarize_text(text: str) -> str:
    api_key = os.getenv("OPENAI_API_KEY")
    if api_key:
        import openai
        openai.api_key = api_key
        prompt = "Summarize the following threat report focusing on attacker methods and key findings:\n" + text[:4000]
        resp = openai.ChatCompletion.create(
            model="gpt-3.5-turbo",
            messages=[{"role": "user", "content": prompt}],
        )
        return resp.choices[0].message["content"].strip()
    # fallback simple summary
    words = text.split()
    return " ".join(words[:100]) + ("..." if len(words) > 100 else "")


def to_stix(data: dict, summary: str) -> str:
    if Bundle is None:
        raise RuntimeError("stix2 library is not installed")
    objects = []
    for ip in data.get("ips", []):
        objects.append(IPv4Address(value=ip))
    for domain in data.get("domains", []):
        objects.append(DomainName(value=domain))
    for url in data.get("urls", []):
        objects.append(StixURL(value=url))
    for h in data.get("hashes", []):
        objects.append(File(hashes={"SHA-256": h}))
    for t in data.get("mitre", []):
        objects.append(AttackPattern(name=t, external_references=[{"source_name": "mitre-attack", "external_id": t}]))
    for a in data.get("actors", []):
        objects.append(IntrusionSet(name=a))
    observed = ObservedData(first_observed="1970-01-01T00:00:00Z",
                             last_observed="1970-01-01T00:00:00Z",
                             number_observed=1,
                             objects={i: obj for i, obj in enumerate(objects)})
    rep = Report(name="Threat Report", description=summary, object_refs=[observed.id])
    bundle = Bundle(objects + [observed, rep])
    return bundle.serialize()


def main():
    if len(sys.argv) < 2:
        print("Usage: python threat_report_parser.py <path_or_url> [--stix]", file=sys.stderr)
        sys.exit(1)
    source = sys.argv[1]
    text = fetch_text(source)
    data = parse_report(text)
    summary = summarize_text(text)
    if len(sys.argv) > 2 and sys.argv[2] == "--stix":
        print(to_stix(data, summary))
    else:
        data["summary"] = summary
        print(json.dumps(data, indent=2))


if __name__ == "__main__":
    main()
