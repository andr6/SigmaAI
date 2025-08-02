"""Helpers to export IOC data to a STIX bundle."""
import json
import sys
from pathlib import Path

from stix2 import Bundle, DomainName, File, IPv4Address, URL


def bundle_from_iocs(iocs: dict) -> Bundle:
    objects = []
    for ip in iocs.get("ips", []):
        objects.append(IPv4Address(value=ip))
    for domain in iocs.get("domains", []):
        objects.append(DomainName(value=domain))
    for url in iocs.get("urls", []):
        objects.append(URL(value=url))
    for h in iocs.get("hashes", []):
        objects.append(File(hashes={"SHA-256": h}))
    return Bundle(objects)


def main():
    if len(sys.argv) < 3:
        print("Usage: python stix_exporter.py <iocs.json> <output_bundle.json>")
        sys.exit(1)
    in_path = Path(sys.argv[1])
    out_path = Path(sys.argv[2])
    with in_path.open() as f:
        iocs = json.load(f)
    bundle = bundle_from_iocs(iocs)
    out_path.write_text(bundle.serialize(indent=2))
    print(f"Wrote STIX bundle with {len(bundle.objects)} objects to {out_path}")


if __name__ == "__main__":
    main()

