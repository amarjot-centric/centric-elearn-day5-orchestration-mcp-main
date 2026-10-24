using System.Collections;
using System.Data.Common;
using LearnHub.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnHub.Api.Reports;

public class PayoutRow
{
    public int InstructorId;
    public string Instructor = "";
    public int Lines;
    public decimal Gross;
    public decimal Share;
}

public class PayoutReport
{
    public string Month = "";
    public List<PayoutRow> Rows = new();
    public decimal TotalGross;
    public decimal TotalShare;
}

public class PayoutReportBuilder
{
    private readonly LearnHubDbContext db;

    public PayoutReportBuilder(LearnHubDbContext db)
    {
        this.db = db;
    }

    public PayoutReport Build(int year, int month)
    {
        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1);

        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != System.Data.ConnectionState.Open;
        if (wasClosed) conn.Open();

        var sql = "SELECT i.id, i.full_name, o.id, ol.final_price, o.status " +
                  "FROM order_lines ol " +
                  "JOIN orders o ON o.id = ol.order_id " +
                  "JOIN courses c ON c.id = ol.course_id " +
                  "JOIN instructors i ON i.id = c.instructor_id " +
                  "WHERE o.created_at >= @from AND o.created_at < @to " +
                  "ORDER BY i.full_name, o.id";

        var ids = new ArrayList();
        var names = new Hashtable();
        var lineCount = new Hashtable();
        var gross = new Hashtable();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = sql;
            AddParam(cmd, "@from", from);
            AddParam(cmd, "@to", to);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var iid = Convert.ToInt32(rd.GetValue(0));
                var amount = Convert.ToDecimal(rd.GetValue(3));
                if (!names.ContainsKey(iid))
                {
                    names[iid] = rd.GetString(1);
                    lineCount[iid] = 0;
                    gross[iid] = 0m;
                    ids.Add(iid);
                }
                lineCount[iid] = (int)lineCount[iid]! + 1;
                gross[iid] = (decimal)gross[iid]! + amount;
            }
        }
        if (wasClosed) conn.Close();

        var report = new PayoutReport { Month = year.ToString("D4") + "-" + month.ToString("D2") };
        foreach (int iid in ids)
        {
            var row = new PayoutRow
            {
                InstructorId = iid,
                Instructor = (string)names[iid]!,
                Lines = (int)lineCount[iid]!,
                Gross = (decimal)gross[iid]!
            };
            row.Share = Math.Round(row.Gross * 0.7m, 2, MidpointRounding.AwayFromZero);
            report.Rows.Add(row);
            report.TotalGross += row.Gross;
            report.TotalShare += row.Share;
        }
        return report;
    }

    private static void AddParam(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
