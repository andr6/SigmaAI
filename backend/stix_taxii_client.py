"""Simple TAXII 2.0/2.1 client helpers."""
from __future__ import annotations

import logging
from typing import List, Optional

from taxii2client.v20 import Collection, Server
from stix2 import Bundle

logger = logging.getLogger(__name__)


class StixTaxiiClient:
    """Helper for interacting with a TAXII server."""

    def __init__(self, server_url: str, username: Optional[str] = None, password: Optional[str] = None):
        self.server_url = server_url.rstrip("/")
        self.username = username
        self.password = password
        try:
            self.server = Server(self.server_url, user=username, password=password)
        except Exception as exc:
            logger.error("Failed to connect to TAXII server %s: %s", server_url, exc)
            self.server = None

    def list_collections(self) -> List[dict]:
        """Return available collections on the TAXII server."""
        if not self.server:
            return []
        cols: List[dict] = []
        try:
            for api_root in self.server.api_roots:
                for col in api_root.collections:
                    cols.append({"id": col.id, "title": col.title})
        except Exception as exc:
            logger.error("Failed to list collections: %s", exc)
        return cols

    def fetch_objects(self, collection_id: str, since: Optional[str] = None) -> List[dict]:
        """Fetch STIX objects from a collection."""
        if not self.server:
            return []
        try:
            api_root = self.server.api_roots[0]
            url = f"{api_root.url}collections/{collection_id}/"
            col = Collection(url, user=self.username, password=self.password)
            resp = col.get_objects(since=since)
            return resp.get("objects", [])
        except Exception as exc:
            logger.error("Failed to fetch objects for collection %s: %s", collection_id, exc)
            return []

    def push_bundle(self, collection_id: str, bundle: Bundle) -> bool:
        """Push a STIX bundle into the specified collection."""
        if not self.server:
            return False
        try:
            api_root = self.server.api_roots[0]
            url = f"{api_root.url}collections/{collection_id}/"
            col = Collection(url, user=self.username, password=self.password)
            col.add_objects(bundle.serialize())
            return True
        except Exception as exc:
            logger.error("Failed to push bundle to collection %s: %s", collection_id, exc)
            return False

