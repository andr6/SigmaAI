import os
from typing import Any, Optional
from neo4j import GraphDatabase, Driver
from .models import Indicator

NEO4J_URL = os.getenv("NEO4J_URL", "bolt://localhost:7687")
NEO4J_USER = os.getenv("NEO4J_USER", "neo4j")
NEO4J_PASSWORD = os.getenv("NEO4J_PASSWORD", "test")

_driver: Optional[Driver] = None


def get_driver() -> Driver:
    global _driver
    if _driver is None:
        _driver = GraphDatabase.driver(NEO4J_URL, auth=(NEO4J_USER, NEO4J_PASSWORD))
    return _driver


def close_driver() -> None:
    if _driver is not None:
        _driver.close()


def _create_indicator(tx: Any, indicator: Indicator) -> None:
    tx.run(
        "MERGE (s:Source {name: $source})"
        "MERGE (i:Indicator {ioc: $ioc, type: $type})"
        "MERGE (s)-[:HAS_INDICATOR]->(i)",
        source=indicator.source,
        ioc=indicator.ioc,
        type=indicator.type,
    )


def write_indicator(indicator: Indicator) -> None:
    """Persist an indicator relationship in Neo4j."""
    driver = get_driver()
    with driver.session() as session:
        session.execute_write(_create_indicator, indicator)
