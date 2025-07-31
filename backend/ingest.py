from typing import List
from datetime import datetime
import os
import requests
from .models import Indicator

OTX_URL = "https://otx.alienvault.com/api/v1/indicators/export"
SHODAN_URL = "https://api.shodan.io/shodan/host/search"
CENSYS_URL = "https://search.censys.io/api/v2/hosts/search"
VT_URL = "https://www.virustotal.com/api/v3/ip_addresses/{ip}"


def fetch_otx(limit: int = 10) -> List[Indicator]:
    api_key = os.getenv("OTX_API_KEY", "")
    params = {"type": "IPv4", "limit": str(limit)}
    headers = {"X-OTX-API-KEY": api_key} if api_key else {}
    resp = requests.get(OTX_URL, params=params, headers=headers, timeout=10)
    resp.raise_for_status()
    lines = [l.strip() for l in resp.text.splitlines() if l.strip()]
    return [Indicator(source="OTX", ioc=l, type="ip", first_seen=datetime.utcnow()) for l in lines]


def fetch_shodan(query: str = "malware") -> List[Indicator]:
    api_key = os.getenv("SHODAN_API_KEY", "")
    params = {"key": api_key, "query": query}
    resp = requests.get(SHODAN_URL, params=params, timeout=10)
    resp.raise_for_status()
    data = resp.json()
    indicators = []
    for match in data.get("matches", []):
        ip = match.get("ip_str")
        if ip:
            indicators.append(Indicator(source="Shodan", ioc=ip, type="ip"))
    return indicators


def fetch_censys(query: str = "services.service_name: HTTP") -> List[Indicator]:
    uid = os.getenv("CENSYS_UID", "")
    secret = os.getenv("CENSYS_SECRET", "")
    headers = {"Accept": "application/json"}
    resp = requests.post(CENSYS_URL, auth=(uid, secret), json={"q": query, "per_page": 10}, headers=headers, timeout=10)
    resp.raise_for_status()
    data = resp.json()
    results = data.get("result", {}).get("hits", [])
    return [Indicator(source="Censys", ioc=r.get("ip"), type="ip") for r in results if r.get("ip")]


def fetch_virustotal(ip: str) -> List[Indicator]:
    api_key = os.getenv("VT_API_KEY", "")
    headers = {"x-apikey": api_key}
    resp = requests.get(VT_URL.format(ip=ip), headers=headers, timeout=10)
    if resp.status_code != 200:
        return []
    data = resp.json()
    malicious = data.get("data", {}).get("attributes", {}).get("last_analysis_stats", {}).get("malicious", 0)
    if malicious:
        return [Indicator(source="VirusTotal", ioc=ip, type="ip")]
    return []
