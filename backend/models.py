from sqlmodel import SQLModel, Field
from typing import Optional
from datetime import datetime
import os
from sqlalchemy import Column
from sqlalchemy.types import String, TypeDecorator
from cryptography.fernet import Fernet


_key = os.environ.get("ENCRYPTION_KEY")
if _key is None:
    raise ValueError("ENCRYPTION_KEY environment variable not set")
fernet = Fernet(_key.encode())


class EncryptedString(TypeDecorator):
    impl = String
    cache_ok = True

    def process_bind_param(self, value, dialect):
        if value is None:
            return value
        return fernet.encrypt(value.encode()).decode()

    def process_result_value(self, value, dialect):
        if value is None:
            return value
        return fernet.decrypt(value.encode()).decode()


class Indicator(SQLModel, table=True):
    id: Optional[int] = Field(default=None, primary_key=True)
    source: str = Field(sa_column=Column(EncryptedString))
    ioc: str = Field(sa_column=Column(EncryptedString))
    type: str
    first_seen: datetime = Field(default_factory=datetime.utcnow)
