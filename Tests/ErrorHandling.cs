using System;
using System.Diagnostics;
using System.IO;
using HospitalMSWebApp.Infrastructure;
class ErrorHandling
{
    static void Main()
    {
        foreach(var error in new Exception[]{new Exception("private server detail"),new InvalidOperationException("private connection detail"),new ArgumentException("private submitted value <script>")}){
            var log=new StringWriter();var listener=new TextWriterTraceListener(log);Trace.Listeners.Add(listener);
            try{
                string message=ApplicationErrors.Report(error,"The record could not be saved. Review the form and retry.");Trace.Flush();
                if(message.Contains("private")||message.Contains("<script>")||!message.Contains("Review the form"))throw new Exception("Internal error was exposed or useful feedback was lost.");
                if(!log.ToString().Contains(error.Message))throw new Exception("Server diagnostic detail was not retained.");
                Console.WriteLine("PASS: Legacy error feedback hides "+error.GetType().Name+" detail and retains server diagnostics");
            }finally{Trace.Listeners.Remove(listener);listener.Dispose();}
        }
    }
}
