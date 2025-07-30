from sqlmodel import SQLModel, Field
from typing import Optional
from datetime import datetime

class Indicator(SQLModel, table=True):
    id: Optional[int] = Field(default=None, primary_key=True)
    source: str
    ioc: str
    type: str
    first_seen: datetime = Field(default_factory=datetime.utcnow)
