/** LearnHub MySQL MCP server: guarded read and write tools over stdio. */
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import mysql, { Connection, ResultSetHeader, RowDataPacket } from "mysql2/promise";
import { z } from "zod";

// Statement types read_query accepts (first keyword of the cleaned SQL).
const READ_STARTS = new Set(["SELECT", "WITH", "SHOW", "DESCRIBE", "DESC", "EXPLAIN"]);
// Statement types write_query accepts; DELETE and DDL are deliberately excluded.
const WRITE_STARTS = new Set(["INSERT", "UPDATE"]);
// Defense in depth: keywords rejected anywhere in a write, e.g. inside subqueries or file output.
const WRITE_FORBIDDEN =
  /\b(DROP|ALTER|DELETE|TRUNCATE|CREATE|GRANT|REVOKE|RENAME|OUTFILE|DUMPFILE|LOAD_FILE|LOAD\s+DATA)\b/i;

/** Open a fresh connection per tool call, configured via LEARNHUB_DB_* env vars (same names as db/reset.sh). */
function connect(): Promise<Connection> {
  return mysql.createConnection({
    host: process.env.LEARNHUB_DB_HOST ?? "127.0.0.1",
    port: Number(process.env.LEARNHUB_DB_PORT ?? "3306"),
    user: process.env.LEARNHUB_DB_USER ?? "root",
    password: process.env.LEARNHUB_DB_PASSWORD ?? "Root123$",
    database: process.env.LEARNHUB_DB_NAME ?? "learnhub",
    dateStrings: true, // return DATE/DATETIME as strings to avoid timezone shifts in JSON
  });
}

/** Strip comments and one trailing semicolon; reject chained statements. */
function clean(sql: string): string {
  let text = sql.replace(/\/\*[\s\S]*?\*\//g, " ");
  text = text.replace(/(--|#)[^\n]*/g, " ").trim();
  if (text.endsWith(";")) text = text.slice(0, -1).trim();
  if (text.includes(";")) throw new Error("Only a single statement is allowed.");
  if (!text) throw new Error("Empty statement.");
  return text;
}

/** First keyword of the statement, upper-cased, used to classify it as read or write. */
function firstWord(text: string): string {
  return text.split(/\s+/, 1)[0].toUpperCase();
}

/** JSON.stringify that tolerates BigInt values (e.g. COUNT or insert ids) by converting them to strings. */
function toJson(value: unknown): string {
  return JSON.stringify(value, (_k, v) => (typeof v === "bigint" ? v.toString() : v));
}

type ToolResult = { content: { type: "text"; text: string }[]; isError?: boolean };

/** Wrap a tool body: return its text on success, or the error message flagged as isError instead of throwing. */
async function run(fn: () => Promise<string>): Promise<ToolResult> {
  try {
    return { content: [{ type: "text", text: await fn() }] };
  } catch (e) {
    return { content: [{ type: "text", text: e instanceof Error ? e.message : String(e) }], isError: true };
  }
}

const server = new McpServer({ name: "learnhub-db", version: "1.0.0" });

server.tool(
  "read_query",
  "Run a read-only query (SELECT, WITH, SHOW, DESCRIBE, EXPLAIN) and return rows as JSON. " +
    "Runs in a read-only transaction; results are capped at `limit` rows.",
  { sql: z.string(), limit: z.number().int().default(200) },
  ({ sql, limit }) =>
    run(async () => {
      const text = clean(sql);
      if (!READ_STARTS.has(firstWord(text))) {
        throw new Error("read_query only allows SELECT, WITH, SHOW, DESCRIBE or EXPLAIN.");
      }
      if (/\b(OUTFILE|DUMPFILE|LOAD_FILE)\b/i.test(text)) throw new Error("File access is not allowed.");
      const conn = await connect();
      try {
        // The read-only transaction makes MySQL itself refuse writes, even if the keyword checks are bypassed.
        await conn.query("START TRANSACTION READ ONLY");
        // Rows are capped client-side after the query runs; add LIMIT to the SQL for very large tables.
        const [all] = await conn.query<RowDataPacket[]>(text);
        const max = Math.max(1, limit);
        const rows = all.slice(0, max);
        const truncated = all.length > max;
        await conn.query("ROLLBACK");
        return toJson({ rows, row_count: rows.length, truncated });
      } finally {
        await conn.end();
      }
    }),
);

server.tool(
  "write_query",
  "Run a single INSERT or UPDATE and commit. UPDATE must have a WHERE clause. " +
    "DELETE and DDL (DROP, ALTER, TRUNCATE, CREATE, ...) are rejected.",
  { sql: z.string() },
  ({ sql }) =>
    run(async () => {
      const text = clean(sql);
      const word = firstWord(text);
      if (!WRITE_STARTS.has(word)) throw new Error("write_query only allows INSERT or UPDATE.");
      if (WRITE_FORBIDDEN.test(text)) throw new Error("Statement contains a forbidden keyword.");
      if (word === "UPDATE" && !/\bWHERE\b/i.test(text)) throw new Error(`${word} requires a WHERE clause.`);
      const conn = await connect();
      try {
        // mysql2 autocommit is on, so the explicit commit is a safeguard; the catch rolls back on failure.
        const [res] = await conn.query<ResultSetHeader>(text);
        await conn.commit();
        return toJson({ affected_rows: res.affectedRows, last_insert_id: res.insertId });
      } catch (e) {
        await conn.rollback();
        throw e;
      } finally {
        await conn.end();
      }
    }),
);

server.tool("list_tables", "List every table in the learnhub database with its exact row count.", () =>
  run(async () => {
    const conn = await connect();
    try {
      // SHOW TABLES names its column "Tables_in_<db>", so read the first value of each row instead.
      const [tables] = await conn.query<RowDataPacket[]>("SHOW TABLES");
      const result: Record<string, number> = {};
      for (const t of tables) {
        const name = String(Object.values(t)[0]);
        // Table names can't be bound as parameters, so strip backticks and quote the identifier.
        const [rows] = await conn.query<RowDataPacket[]>(`SELECT COUNT(*) AS n FROM \`${name.replace(/`/g, "")}\``);
        result[name] = Number(rows[0].n);
      }
      return toJson(result);
    } finally {
      await conn.end();
    }
  }),
);

// Serve over stdio; Claude Code launches this via .mcp.json (node mcp/dist/server.js).
await server.connect(new StdioServerTransport());
