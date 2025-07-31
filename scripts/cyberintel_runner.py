import sys
import json
import os
import re
from PyPDF2 import PdfReader


def extract_text(pdf_path):
    reader = PdfReader(pdf_path)
    text = ""
    for page in reader.pages:
        if page.extract_text():
            text += page.extract_text() + "\n"
    return text


def extract_iocs(text):
    ips = re.findall(r"\b(?:\d{1,3}\.){3}\d{1,3}\b", text)
    domains = re.findall(r"\b(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}\b", text)
    urls = re.findall(r"https?://[^\s]+", text)
    hashes = re.findall(r"\b[A-Fa-f0-9]{32,64}\b", text)
    return {
        "ips": sorted(set(ips)),
        "domains": sorted(set(domains)),
        "urls": sorted(set(urls)),
        "hashes": sorted(set(hashes)),
    }


def main():
    if len(sys.argv) < 2:
        print("Usage: python cyberintel_runner.py <pdf_path>")
        sys.exit(1)
    pdf_path = sys.argv[1]
    text = extract_text(pdf_path)
    iocs = extract_iocs(text)
    os.makedirs('output', exist_ok=True)
    with open('output/iocs_output.json', 'w') as f:
        json.dump(iocs, f, indent=4)
    print(json.dumps(iocs))


if __name__ == '__main__':
    main()

