using System;
namespace HospitalMSWebApp.Infrastructure
{
    public static class ApplicationErrors
    {
        // Callers supply a fixed user-facing message, never exception text or submitted input.
        public static string Report(Exception error,string message)
        {
            System.Diagnostics.Trace.TraceError(error.ToString());
            return message;
        }
    }
}
