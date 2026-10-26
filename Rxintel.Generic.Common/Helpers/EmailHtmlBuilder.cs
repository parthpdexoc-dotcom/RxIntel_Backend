using System.Net;
using System.Text;

namespace Rxintel.Generic.Common.Helpers;

public static class EmailHtmlBuilder
{
    public const string TableStyle =
        "width:100%;max-width:640px;border-collapse:collapse;border:1px solid #e5e7eb;";

    public static StringBuilder StartDocument()
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\" /></head>");
        sb.Append("<body style=\"font-family:Arial,Helvetica,sans-serif;color:#1f2937;line-height:1.5;\">");
        return sb;
    }

    public static void AppendGreeting(StringBuilder sb, string name) =>
        sb.Append($"<p>Hello {WebUtility.HtmlEncode(name)},</p>");

    public static void AppendParagraph(StringBuilder sb, string text) =>
        sb.Append($"<p>{WebUtility.HtmlEncode(text)}</p>");

    public static void AppendSection(StringBuilder sb, string title, Action<StringBuilder> appendRows, string? tableStyle = null)
    {
        sb.Append($"<h3 style=\"color:#047857;margin-bottom:8px;\">{WebUtility.HtmlEncode(title)}</h3>");
        sb.Append($"<table style=\"{tableStyle ?? TableStyle}\">");
        appendRows(sb);
        sb.Append("</table>");
    }

    public static void AppendRow(StringBuilder sb, string label, string value)
    {
        sb.Append("<tr>");
        sb.Append($"<td style=\"padding:10px 12px;border-bottom:1px solid #e5e7eb;background:#f9fafb;width:35%;font-weight:600;\">{WebUtility.HtmlEncode(label)}</td>");
        sb.Append($"<td style=\"padding:10px 12px;border-bottom:1px solid #e5e7eb;\">{WebUtility.HtmlEncode(value)}</td>");
        sb.Append("</tr>");
    }

    public static void AppendFooter(StringBuilder sb)
    {
        sb.Append("<p style=\"margin-top:24px;color:#6b7280;font-size:13px;\">");
        sb.Append("© RxIntel. All rights reserved.</p>");
        sb.Append("</body></html>");
    }

    public static string Finish(StringBuilder sb)
    {
        AppendFooter(sb);
        return sb.ToString();
    }
}
