using System;
using System.Data;
using System.Globalization;
using System.Text;
namespace HospitalMSWebApp.Infrastructure
{
    public static class CsvExport
    {
        public static string Encode(DataTable table)
        {
            var output=new StringBuilder();
            for(int i=0;i<table.Columns.Count;i++){if(i>0)output.Append(',');output.Append(Cell(table.Columns[i].ColumnName));}output.Append("\r\n");
            foreach(DataRow row in table.Rows){
                for(int i=0;i<table.Columns.Count;i++){if(i>0)output.Append(',');output.Append(Cell(row[i]));}output.Append("\r\n");
            }
            return output.ToString();
        }
        private static string Cell(object value)
        {
            string text=value==DBNull.Value?"":value is DateTime?((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture):Convert.ToString(value,CultureInfo.InvariantCulture);
            // Quoting alone does not stop spreadsheet formula execution.
            if(value is string){var start=text.TrimStart();if((start.Length>0&&"=+-@".IndexOf(start[0])>=0)||(text.Length>0&&(text[0]=='\t'||text[0]=='\r'||text[0]=='\n')))text="'"+text;}
            return "\""+text.Replace("\"","\"\"")+"\"";
        }
    }
}
