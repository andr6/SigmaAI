import sys
from pathlib import Path
sys.path.append(str(Path(__file__).resolve().parents[2]))
import types
import pytest
from backend import ingest
from backend.models import Indicator

class MockResponse:
    def __init__(self, *, status_code=200, text='', json_data=None):
        self.status_code = status_code
        self.text = text
        self._json = json_data or {}
    def raise_for_status(self):
        if 400 <= self.status_code < 600:
            raise Exception('HTTP error')
    def json(self):
        return self._json


def test_fetch_otx(monkeypatch):
    def mock_get(url, params=None, headers=None, timeout=10):
        return MockResponse(text='1.1.1.1\n2.2.2.2')
    monkeypatch.setattr(ingest.requests, 'get', mock_get)
    results = ingest.fetch_otx(limit=2)
    assert [i.ioc for i in results] == ['1.1.1.1', '2.2.2.2']
    assert all(i.source == 'OTX' for i in results)


def test_fetch_shodan(monkeypatch):
    def mock_get(url, params=None, timeout=10):
        return MockResponse(json_data={'matches': [{'ip_str': '3.3.3.3'}, {'ip_str': '4.4.4.4'}]})
    monkeypatch.setattr(ingest.requests, 'get', mock_get)
    results = ingest.fetch_shodan(query='malware')
    assert [i.ioc for i in results] == ['3.3.3.3', '4.4.4.4']
    assert all(i.source == 'Shodan' for i in results)


def test_fetch_censys(monkeypatch):
    def mock_post(url, auth=None, json=None, headers=None, timeout=10):
        return MockResponse(json_data={'result': {'hits': [{'ip': '5.5.5.5'}, {'ip': '6.6.6.6'}]}})
    monkeypatch.setattr(ingest.requests, 'post', mock_post)
    results = ingest.fetch_censys(query='query')
    assert [i.ioc for i in results] == ['5.5.5.5', '6.6.6.6']
    assert all(i.source == 'Censys' for i in results)


def test_fetch_virustotal(monkeypatch):
    def mock_get(url, headers=None, timeout=10):
        return MockResponse(json_data={'data': {'attributes': {'last_analysis_stats': {'malicious': 1}}}})
    monkeypatch.setattr(ingest.requests, 'get', mock_get)
    results = ingest.fetch_virustotal('7.7.7.7')
    assert [i.ioc for i in results] == ['7.7.7.7']
    assert results[0].source == 'VirusTotal'


def test_fetch_virustotal_no_malicious(monkeypatch):
    def mock_get(url, headers=None, timeout=10):
        return MockResponse(json_data={'data': {'attributes': {'last_analysis_stats': {'malicious': 0}}}})
    monkeypatch.setattr(ingest.requests, 'get', mock_get)
    results = ingest.fetch_virustotal('8.8.8.8')
    assert results == []
