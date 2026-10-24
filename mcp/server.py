"""LearnHub MySQL MCP server: guarded read and write tools over stdio."""
import json
import os
import re

import pymysql
from pymysql.cursors import DictCursor
from mcp.server.fastmcp import FastMCP

mcp = FastMCP("learnhub-db")

READ_STARTS = {"SELECT", "WITH", "SHOW", "DESCRIBE", "DESC", "EXPLAIN"}
WRITE_STARTS = {"INSERT", "UPDATE"}
WRITE_FORBIDDEN = re.compile(
    r"\b(DROP|ALTER|DELETE|TRUNCATE|CREATE|GRANT|REVOKE|RENAME|OUTFILE|DUMPFILE|LOAD_FILE|LOAD\s+DATA)\b", re.I)


def connect():
    return pymysql.connect(
        host=os.environ.get("LEARNHUB_DB_HOST", "127.0.0.1"),
        port=int(os.environ.get("LEARNHUB_DB_PORT", "3306")),
        user=os.environ.get("LEARNHUB_DB_USER", "root"),
        password=os.environ.get("LEARNHUB_DB_PASSWORD", "Root123$"),
        database=os.environ.get("LEARNHUB_DB_NAME", "learnhub"),
        cursorclass=DictCursor,
        autocommit=False,
    )


def clean(sql: str) -> str:
    """Strip comments and one trailing semicolon; reject chained statements."""
    text = re.sub(r"/\*.*?\*/", " ", sql, flags=re.S)
    text = re.sub(r"(--|#)[^\n]*", " ", text).strip()
    if text.endswith(";"):
        text = text[:-1].strip()
    if ";" in text:
        raise ValueError("Only a single statement is allowed.")
    if not text:
        raise ValueError("Empty statement.")
    return text


def first_word(text: str) -> str:
    return text.split(None, 1)[0].upper()


@mcp.tool()
def read_query(sql: str, limit: int = 200) -> str:
    """Run a read-only query (SELECT, WITH, SHOW, DESCRIBE, EXPLAIN) and return rows as JSON.
    Runs in a read-only transaction; results are capped at `limit` rows."""
    text = clean(sql)
    if first_word(text) not in READ_STARTS:
        raise ValueError("read_query only allows SELECT, WITH, SHOW, DESCRIBE or EXPLAIN.")
    if re.search(r"\b(OUTFILE|DUMPFILE|LOAD_FILE)\b", text, re.I):
        raise ValueError("File access is not allowed.")
    conn = connect()
    try:
        with conn.cursor() as cur:
            cur.execute("START TRANSACTION READ ONLY")
            cur.execute(text)
            rows = cur.fetchmany(max(1, limit))
            truncated = cur.fetchone() is not None
            cur.execute("ROLLBACK")
        return json.dumps({"rows": rows, "row_count": len(rows), "truncated": truncated}, default=str)
    finally:
        conn.close()


@mcp.tool()
def write_query(sql: str) -> str:
    """Run a single INSERT, UPDATE or DELETE and commit. UPDATE and DELETE must have a WHERE clause.
    DDL (DROP, ALTER, TRUNCATE, CREATE, ...) is rejected."""
    text = clean(sql)
    word = first_word(text)
    if word not in WRITE_STARTS:
        raise ValueError("write_query only allows INSERT, UPDATE or DELETE.")
    if WRITE_FORBIDDEN.search(text):
        raise ValueError("Statement contains a forbidden keyword.")
    if word in {"UPDATE", "DELETE"} and not re.search(r"\bWHERE\b", text, re.I):
        raise ValueError(f"{word} requires a WHERE clause.")
    conn = connect()
    try:
        with conn.cursor() as cur:
            affected = cur.execute(text)
            last_id = cur.lastrowid
        conn.commit()
        return json.dumps({"affected_rows": affected, "last_insert_id": last_id})
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()


@mcp.tool()
def list_tables() -> str:
    """List every table in the learnhub database with its exact row count."""
    conn = connect()
    try:
        with conn.cursor() as cur:
            cur.execute("SHOW TABLES")
            names = [next(iter(r.values())) for r in cur.fetchall()]
            result = {}
            for name in names:
                cur.execute(f"SELECT COUNT(*) AS n FROM `{name.replace('`', '')}`")
                result[name] = cur.fetchone()["n"]
        return json.dumps(result)
    finally:
        conn.close()


if __name__ == "__main__":
    mcp.run()
