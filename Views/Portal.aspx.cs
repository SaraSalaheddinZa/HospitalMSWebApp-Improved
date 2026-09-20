using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using HospitalMSWebApp.Infrastructure;
using HospitalMSWebApp.Services;

namespace HospitalMSWebApp.Views
{
    public partial class Portal : AppPage
    {
        private readonly Database database = new Database();
        private readonly HospitalWorkflowService workflows = new HospitalWorkflowService();
        private string module;
        private string csrf;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!RequireRoles("Admin", "Doctor", "Receptionist", "Laboratorian", "Nurse", "Pharmacist", "Patient")) return;
            module = NormalizeModule(Request.QueryString["module"]);
            if (!CanOpen(module)) { Response.Redirect("Portal.aspx?module=dashboard", false); Context.ApplicationInstance.CompleteRequest(); return; }
            csrf = Convert.ToString(Session["PortalCsrf"]);
            if (string.IsNullOrEmpty(csrf)) { csrf = Guid.NewGuid().ToString("N"); Session["PortalCsrf"] = csrf; }
            if (Request.HttpMethod == "POST") HandlePost();
        }

        protected override void Render(HtmlTextWriter writer)
        {
            writer.Write(BuildDocument());
        }

        private void HandlePost()
        {
            if (!string.Equals(Request.Form["csrf"], csrf, StringComparison.Ordinal)) { Redirect("Security validation failed. Please retry.", true); return; }
            var action = Request.Form["action"];
            try
            {
                if (action == "savePatient" && Can("Admin", "Receptionist"))
                {
                    var gender = Required("gender");
                    var patientId = workflows.RegisterPatient(Required("name"), Required("phone"), gender, Date("birthDate"), Value("address"),
                        Value("allergies"), Value("email"), Value("emergencyName"), Value("emergencyPhone"), Value("bloodType"), Value("conditions"), CurrentUser.UserId);
                    AuditService.Write(CurrentUser, "PatientCreated", "Patient", patientId.ToString(), "New patient registered", Request);
                    Redirect("Patient registered with a Guardian Health MRN."); return;
                }
                if (action == "bookAppointment" && Can("Admin", "Receptionist", "Patient"))
                {
                    var patientId = CurrentUser.Role == "Patient" ? CurrentUser.PatientId.GetValueOrDefault() : Integer("patientId");
                    if (patientId < 1) throw new InvalidOperationException("Select a patient.");
                    var start = RequiredDateTime("start");
                    var end = RequiredDateTime("end");
                    var appointmentId = workflows.BookAppointment(patientId, Integer("doctorId"), start, end, Value("reason"), CurrentUser.UserId);
                    AuditService.Write(CurrentUser, "AppointmentBooked", "Appointment", appointmentId.ToString(), "Appointment booked", Request);
                    Redirect("Appointment booked successfully."); return;
                }
                if (action == "checkIn" && Can("Admin", "Receptionist", "Nurse"))
                {
                    var id = Long("appointmentId");
                    database.Execute("UPDATE gh.Appointments SET Status = 'CheckedIn', CheckInAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME() WHERE AppointmentId = @Id AND Status IN ('Booked','Confirmed')", Database.Parameter("@Id", id));
                    AuditService.Write(CurrentUser, "AppointmentCheckedIn", "Appointment", id.ToString(), "Patient checked in", Request);
                    Redirect("Patient checked in."); return;
                }
                if (action == "cancelAppointment" && Can("Admin", "Receptionist", "Patient"))
                {
                    var id = Long("appointmentId");
                    var sql = CurrentUser.Role == "Patient"
                        ? "UPDATE gh.Appointments SET Status = 'Cancelled', CancellationReason = @Reason, UpdatedAt = SYSUTCDATETIME() WHERE AppointmentId = @Id AND PatientId = @PatientId AND Status IN ('Requested','Booked','Confirmed','RescheduleRequested')"
                        : "UPDATE gh.Appointments SET Status = 'Cancelled', CancellationReason = @Reason, UpdatedAt = SYSUTCDATETIME() WHERE AppointmentId = @Id AND Status IN ('Requested','Booked','Confirmed','RescheduleRequested')";
                    if (CurrentUser.Role == "Patient") database.Execute(sql, Database.Parameter("@Id", id), Database.Parameter("@Reason", Value("reason")), Database.Parameter("@PatientId", CurrentUser.PatientId));
                    else database.Execute(sql, Database.Parameter("@Id", id), Database.Parameter("@Reason", Value("reason")));
                    AuditService.Write(CurrentUser, "AppointmentCancelled", "Appointment", id.ToString(), "Appointment cancelled", Request);
                    Redirect("Appointment cancellation recorded."); return;
                }
                if (action == "saveEncounter" && Can("Doctor"))
                {
                    var encounterId = workflows.RecordEncounter(Integer("patientId"), CurrentUser.UserId, NullableLong("appointmentId"), Value("complaint"), Value("assessment"),
                        Value("plan"), Date("followUp"), Decimal("temperature"), NullableInteger("pulse"), NullableInteger("systolic"), NullableInteger("diastolic"),
                        Value("diagnosis"), NullableInteger("medicationId"), Value("dose"), Value("frequency"), Decimal("quantity"), NullableInteger("labTestId"));
                    AuditService.Write(CurrentUser, "EncounterCreated", "Encounter", encounterId.ToString(), "Clinical note and orders recorded", Request);
                    Redirect("Consultation, clinical note, and requested orders were saved."); return;
                }
                if (action == "resultLab" && Can("Laboratorian"))
                {
                    var itemId = Long("labOrderItemId");
                    workflows.FinalizeLabResult(itemId, Required("resultValue"), Value("resultUnit"), Value("resultFlag"), CurrentUser.UserId);
                    AuditService.Write(CurrentUser, "LabResultFinalized", "LabOrderItem", itemId.ToString(), "Laboratory result finalized", Request);
                    Redirect("Laboratory result finalized."); return;
                }
                if (action == "addMedication" && Can("Admin", "Pharmacist"))
                {
                    database.Execute("INSERT INTO gh.MedicationCatalog(MedicationCode, MedicationName, Form, Strength, UnitPrice) VALUES(@Code, @Name, @Form, @Strength, @Price)",
                        Database.Parameter("@Code", Required("code")), Database.Parameter("@Name", Required("name")), Database.Parameter("@Form", Value("form")), Database.Parameter("@Strength", Value("strength")), Database.Parameter("@Price", Decimal("price").GetValueOrDefault()));
                    AuditService.Write(CurrentUser, "MedicationCreated", "Medication", null, "Medication catalog entry created", Request);
                    Redirect("Medication added to the catalog."); return;
                }
                if (action == "receiveStock" && Can("Admin", "Pharmacist"))
                {
                    workflows.ReceiveStock(Integer("medicationId"), Required("batch"), RequiredDate("expiration"), RequiredDecimal("quantity"), Decimal("minimum").GetValueOrDefault(), Decimal("unitCost").GetValueOrDefault(), NullableInteger("supplierId"));
                    AuditService.Write(CurrentUser, "StockReceived", "MedicationBatch", null, "Medication stock received", Request);
                    Redirect("Medication batch received into inventory."); return;
                }
                if (action == "createInvoice" && Can("Admin", "Receptionist"))
                {
                    var invoiceId = workflows.CreateInvoice(Integer("patientId"), Required("description"), RequiredDecimal("amount"), Date("dueDate"));
                    AuditService.Write(CurrentUser, "InvoiceCreated", "Invoice", invoiceId.ToString(), "Invoice created", Request);
                    Redirect("Invoice created."); return;
                }
                if (action == "takePayment" && Can("Admin", "Receptionist"))
                {
                    var invoiceId = Long("invoiceId");
                    workflows.TakePayment(invoiceId, RequiredDecimal("amount"), Required("method"), Value("reference"), CurrentUser.UserId);
                    AuditService.Write(CurrentUser, "PaymentReceived", "Invoice", invoiceId.ToString(), "Payment received", Request);
                    Redirect("Payment recorded."); return;
                }
                if (action == "addDepartment" && Can("Admin"))
                {
                    var id = workflows.AddDepartment(Required("code"), Required("name"), Value("location"), Value("phone"));
                    AuditService.Write(CurrentUser, "DepartmentCreated", "Department", id.ToString(), "Department created", Request);
                    Redirect("Department created."); return;
                }
                if (action == "createStaff" && Can("Admin"))
                {
                    var role = Required("role");
                    if (role != "Nurse" && role != "Pharmacist") throw new ArgumentException("Only nurse and pharmacist accounts are managed here.");
                    var password = Required("password"); string passwordError;
                    if (!PasswordSecurity.IsAcceptable(password, out passwordError)) throw new ArgumentException(passwordError);
                    var userId = workflows.CreateStaffAccount(Required("staffNumber"), role, Required("name"), Required("email"), Value("phone"), NullableInteger("departmentId"), password);
                    AuditService.Write(CurrentUser, "StaffCreated", "UserAccount", userId.ToString(), role + " account created", Request);
                    Redirect("Staff account created."); return;
                }
                if (action == "createPatientAccess" && Can("Admin", "Receptionist"))
                {
                    var password = Required("password"); string passwordError;
                    if (!PasswordSecurity.IsAcceptable(password, out passwordError)) throw new ArgumentException(passwordError);
                    var userId = workflows.CreatePatientAccount(Integer("patientId"), Required("email"), password);
                    AuditService.Write(CurrentUser, "PatientPortalAccessCreated", "UserAccount", userId.ToString(), "Patient portal account created", Request);
                    Redirect("Patient portal access created."); return;
                }
                if (action == "setSetting" && Can("Admin"))
                {
                    database.Execute(@"MERGE gh.SystemSettings AS t USING (SELECT @Key AS SettingKey) s ON t.SettingKey=s.SettingKey
                        WHEN MATCHED THEN UPDATE SET SettingValue=@Value, UpdatedAt=SYSUTCDATETIME(), UpdatedByUserId=@UserId
                        WHEN NOT MATCHED THEN INSERT(SettingKey,SettingValue,UpdatedByUserId) VALUES(@Key,@Value,@UserId);",
                        Database.Parameter("@Key", Required("key")), Database.Parameter("@Value", Value("value")), Database.Parameter("@UserId", CurrentUser.UserId));
                    AuditService.Write(CurrentUser, "SettingUpdated", "SystemSetting", Value("key"), "System setting updated", Request);
                    Redirect("System setting saved."); return;
                }
                Redirect("This action is not permitted.", true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Portal action failed: " + ex);
                Redirect(ex is ArgumentException || ex is InvalidOperationException ? ex.Message : "The request could not be completed. Please review the details and try again.", true);
            }
        }

        private string BuildDocument()
        {
            var page = new StringBuilder();
            page.Append("<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'>");
            page.Append("<title>Guardian Health | ").Append(H(ModuleTitle())).Append("</title><link rel='stylesheet' href='../Content/portal.css'></head><body>");
            page.Append("<aside class='sidebar'><a class='brand' href='Portal.aspx'><span class='brand-mark'>✚</span> Guardian <b>Health</b></a><p class='role'>").Append(H(CurrentUser.Role)).Append(" workspace</p><nav>");
            Nav(page, "dashboard", "Overview", "◫"); Nav(page, "patients", "Patients", "♙"); Nav(page, "appointments", "Appointments", "◷");
            if (Can("Doctor")) Nav(page, "clinical", "Clinical workspace", "⚕");
            if (Can("Laboratorian", "Doctor")) Nav(page, "laboratory", "Laboratory", "⌁");
            if (Can("Pharmacist", "Admin")) Nav(page, "pharmacy", "Pharmacy", "▣");
            if (Can("Receptionist", "Admin")) Nav(page, "billing", "Billing", "¤");
            if (Can("Admin", "Receptionist", "Nurse")) Nav(page, "operations", "Operations", "▤");
            if (Can("Admin")) { Nav(page, "admin", "Administration", "⚙"); Nav(page, "reports", "Reports", "▥"); }
            if (!Can("Patient")) Nav(page, "search", "Global search", "⌕");
            page.Append("</nav><a class='signout' href='Logout.aspx'>Sign out</a></aside><main class='workspace'><header class='topbar'><div><p class='eyebrow'>GUARDIAN HEALTH HOSPITAL</p><h1>").Append(H(ModuleTitle())).Append("</h1></div><div class='user-chip'><span>").Append(H(CurrentUser.Name)).Append("</span><small>").Append(H(CurrentUser.Email)).Append("</small></div></header>");
            var notice = Request.QueryString["notice"]; var error = Request.QueryString["error"];
            if (!string.IsNullOrEmpty(notice)) page.Append("<div class='notice success'>✓ ").Append(H(notice)).Append("</div>");
            if (!string.IsNullOrEmpty(error)) page.Append("<div class='notice error'>! ").Append(H(error)).Append("</div>");
            page.Append(RenderModule());
            page.Append("</main><script>document.querySelectorAll('[data-confirm]').forEach(function(x){x.addEventListener('submit',function(e){if(!confirm(x.dataset.confirm))e.preventDefault();});});</script></body></html>");
            return page.ToString();
        }

        private string RenderModule()
        {
            switch (module)
            {
                case "patients": return RenderPatients();
                case "appointments": return RenderAppointments();
                case "clinical": return RenderClinical();
                case "laboratory": return RenderLaboratory();
                case "pharmacy": return RenderPharmacy();
                case "billing": return RenderBilling();
                case "operations": return RenderOperations();
                case "admin": return RenderAdmin();
                case "reports": return RenderReports();
                case "search": return RenderSearch();
                default: return RenderDashboard();
            }
        }

        private string RenderDashboard()
        {
            if (Can("Patient")) return RenderPatientPortal();
            var totalPatients = Count("SELECT COUNT(*) FROM dbo.patientTbl WHERE IsActive=1");
            var todaysAppointments = Count("SELECT COUNT(*) FROM gh.Appointments WHERE CAST(ScheduledStart AS date)=CAST(SYSUTCDATETIME() AS date) AND Status NOT IN ('Cancelled','NoShow')");
            var waiting = Count("SELECT COUNT(*) FROM gh.Appointments WHERE CAST(ScheduledStart AS date)=CAST(SYSUTCDATETIME() AS date) AND Status IN ('CheckedIn','InProgress')");
            var pendingLabs = Count("SELECT COUNT(*) FROM gh.LabOrderItems WHERE ResultStatus <> 'Final'");
            var sb = new StringBuilder("<section class='metric-grid'>");
            Metric(sb, "Active patients", totalPatients, "Patient registry"); Metric(sb, "Today’s appointments", todaysAppointments, "Live schedule");
            Metric(sb, "Waiting patients", waiting, "Check-in queue"); Metric(sb, "Pending lab results", pendingLabs, "Laboratory worklist"); sb.Append("</section>");
            sb.Append("<section class='panel'><div class='panel-head'><div><h2>Today’s care activity</h2><p>Live information from the hospital database.</p></div><a class='button secondary' href='Portal.aspx?module=appointments'>Open schedule</a></div>");
            sb.Append(Table(database.Query(@"SELECT TOP (12) a.AppointmentId AS [ID], p.MedicalRecordNumber AS [MRN], p.patientName AS [Patient], d.doctorName AS [Clinician],
                CONVERT(varchar(16),a.ScheduledStart,120) AS [Start], a.Status FROM gh.Appointments a JOIN dbo.patientTbl p ON p.patientId=a.PatientId JOIN dbo.doctorTbl d ON d.doctorID=a.DoctorId
                WHERE CAST(a.ScheduledStart AS date)=CAST(SYSUTCDATETIME() AS date) ORDER BY a.ScheduledStart")));
            return sb.Append("</section>").ToString();
        }

        private string RenderPatients()
        {
            if (Can("Patient")) return RenderPatientPortal();
            var query = ValueQuery("q");
            var patients = database.Query(@"SELECT TOP (100) p.patientId AS [ID], p.MedicalRecordNumber AS [MRN], p.patientName AS [Patient], p.patientPhone AS [Phone],
                p.patientGender AS [Gender], CONVERT(varchar(10),p.patientDOB,23) AS [DOB], pp.BloodType AS [Blood type], pp.PatientStatus AS [Status]
                FROM dbo.patientTbl p LEFT JOIN gh.PatientProfiles pp ON pp.PatientId=p.patientId
                WHERE (@Query='' OR p.patientName LIKE '%' + @Query + '%' OR p.MedicalRecordNumber LIKE '%' + @Query + '%' OR p.patientPhone LIKE '%' + @Query + '%')
                ORDER BY p.patientName", Database.Parameter("@Query", query));
            var sb = new StringBuilder("<section class='panel'><div class='panel-head'><div><h2>Patient registry</h2><p>Search by name, MRN, or telephone.</p></div><form class='inline-search' method='get'><input type='hidden' name='module' value='patients'><input name='q' value='").Append(H(query)).Append("' placeholder='Search patients'><button>Search</button></form></div>");
            sb.Append(Table(patients)).Append("</section>");
            if (Can("Admin", "Receptionist"))
            {
                sb.Append("<section class='panel form-panel'><div class='panel-head'><div><h2>Register patient</h2><p>Creates an MRN and a complete profile.</p></div></div><form method='post' class='form-grid' data-confirm='Register this patient?'>")
                    .Append(Hidden("savePatient")).Append(Input("Full name", "name", true)).Append(Input("Telephone", "phone", true)).Append(Select("Gender", "gender", new[] { "Male", "Female", "Other" }, true))
                    .Append(Input("Date of birth", "birthDate", false, "date")).Append(Input("Email", "email", false, "email")).Append(Input("Address", "address", false)).Append(Input("Allergies", "allergies", false))
                    .Append(Input("Chronic conditions", "conditions", false)).Append(Select("Blood type", "bloodType", new[] { "", "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" }, false))
                    .Append(Input("Emergency contact", "emergencyName", false)).Append(Input("Emergency telephone", "emergencyPhone", false)).Append(Submit("Register patient")).Append("</form></section>");
            }
            return sb.ToString();
        }

        private string RenderAppointments()
        {
            var patientWhere = Can("Patient") ? " WHERE a.PatientId=@PatientId" : "";
            var appointments = database.Query(@"SELECT TOP (100) a.AppointmentId AS [ID], p.MedicalRecordNumber AS [MRN], p.patientName AS [Patient], d.doctorName AS [Clinician],
                CONVERT(varchar(16),a.ScheduledStart,120) AS [Start], CONVERT(varchar(5),a.ScheduledEnd,108) AS [End], a.Status, a.Reason
                FROM gh.Appointments a JOIN dbo.patientTbl p ON p.patientId=a.PatientId JOIN dbo.doctorTbl d ON d.doctorID=a.DoctorId" + patientWhere + " ORDER BY a.ScheduledStart DESC",
                Database.Parameter("@PatientId", CurrentUser.PatientId));
            var sb = new StringBuilder("<section class='panel'><div class='panel-head'><div><h2>Appointments</h2><p>Booking, confirmations, and current status.</p></div></div>").Append(Table(appointments)).Append("</section>");
            if (Can("Admin", "Receptionist", "Patient"))
            {
                sb.Append("<section class='panel form-panel'><div class='panel-head'><div><h2>Book appointment</h2><p>Conflicts are rejected at the database level.</p></div></div><form method='post' class='form-grid'>").Append(Hidden("bookAppointment"));
                if (!Can("Patient")) sb.Append(Options("Patient", "patientId", "SELECT patientId AS Id, MedicalRecordNumber + ' — ' + patientName AS Name FROM dbo.patientTbl WHERE IsActive=1 ORDER BY patientName", true));
                sb.Append(Options("Doctor", "doctorId", "SELECT doctorID AS Id, doctorName AS Name FROM dbo.doctorTbl WHERE doctorStatus='Active' ORDER BY doctorName", true))
                    .Append(Input("Start", "start", true, "datetime-local")).Append(Input("End", "end", true, "datetime-local")).Append(Input("Reason for visit", "reason", false)).Append(Submit("Book appointment")).Append("</form></section>");
            }
            if (Can("Admin", "Receptionist", "Nurse")) sb.Append("<section class='panel compact'><h2>Check in a booked patient</h2><form method='post' class='inline-form'>").Append(Hidden("checkIn")).Append(Input("Appointment ID", "appointmentId", true, "number")).Append(Submit("Check in")).Append("</form></section>");
            return sb.ToString();
        }

        private string RenderClinical()
        {
            var doctorId = CurrentUser.UserId;
            var encounters = database.Query(@"SELECT TOP (40) e.EncounterId AS [ID], p.MedicalRecordNumber AS [MRN], p.patientName AS [Patient], e.ChiefComplaint AS [Chief complaint],
                CONVERT(varchar(16),e.StartedAt,120) AS [Started], e.Status FROM gh.Encounters e JOIN dbo.patientTbl p ON p.patientId=e.PatientId WHERE e.DoctorId=@DoctorId ORDER BY e.StartedAt DESC", Database.Parameter("@DoctorId", doctorId));
            var sb = new StringBuilder("<section class='panel'><div class='panel-head'><div><h2>Recent consultations</h2><p>Only your encounters are shown.</p></div></div>").Append(Table(encounters)).Append("</section>");
            sb.Append("<section class='panel form-panel'><div class='panel-head'><div><h2>New consultation</h2><p>Clinical note, vital signs, diagnosis, prescription, and lab order are recorded together.</p></div></div><form method='post' class='form-grid three'>").Append(Hidden("saveEncounter"))
                .Append(Options("Patient", "patientId", "SELECT patientId AS Id, MedicalRecordNumber + ' — ' + patientName AS Name FROM dbo.patientTbl WHERE IsActive=1 ORDER BY patientName", true))
                .Append(Options("Appointment (optional)", "appointmentId", "SELECT a.AppointmentId AS Id, p.patientName + ' — ' + CONVERT(varchar(16),a.ScheduledStart,120) AS Name FROM gh.Appointments a JOIN dbo.patientTbl p ON p.patientId=a.PatientId WHERE a.DoctorId=" + doctorId + " AND a.Status IN ('Booked','CheckedIn','InProgress') ORDER BY a.ScheduledStart", false))
                .Append(Input("Chief complaint", "complaint", false)).Append(Textarea("Assessment", "assessment")).Append(Textarea("Treatment plan", "plan")).Append(Input("Follow-up date", "followUp", false, "date"))
                .Append(Input("Temperature °C", "temperature", false, "number", "step='0.1'")).Append(Input("Pulse", "pulse", false, "number")).Append(Input("Systolic BP", "systolic", false, "number")).Append(Input("Diastolic BP", "diastolic", false, "number"))
                .Append(Input("Primary diagnosis", "diagnosis", false)).Append(Options("Medication (optional)", "medicationId", "SELECT MedicationId AS Id, MedicationName AS Name FROM gh.MedicationCatalog WHERE IsActive=1 ORDER BY MedicationName", false))
                .Append(Input("Dose", "dose", false)).Append(Input("Frequency", "frequency", false)).Append(Input("Quantity", "quantity", false, "number", "step='0.01'"))
                .Append(Options("Laboratory test (optional)", "labTestId", "SELECT LabTestId AS Id, TestName AS Name FROM gh.LabCatalog WHERE IsActive=1 ORDER BY TestName", false)).Append(Submit("Save consultation")).Append("</form></section>");
            return sb.ToString();
        }

        private string RenderLaboratory()
        {
            var labItems = database.Query(@"SELECT TOP (100) i.LabOrderItemId AS [ID], p.MedicalRecordNumber AS [MRN], p.patientName AS [Patient], c.TestName AS [Test],
                i.ResultStatus AS [Status], i.ResultValue AS [Result], i.ResultFlag AS [Flag], CONVERT(varchar(16),o.OrderedAt,120) AS [Ordered]
                FROM gh.LabOrderItems i JOIN gh.LabOrders o ON o.LabOrderId=i.LabOrderId JOIN dbo.patientTbl p ON p.patientId=o.PatientId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId
                ORDER BY CASE WHEN i.ResultStatus='Pending' THEN 0 ELSE 1 END, o.OrderedAt DESC");
            var sb = new StringBuilder("<section class='panel'><div class='panel-head'><div><h2>Laboratory worklist</h2><p>Pending and completed orders from the clinical system.</p></div></div>").Append(Table(labItems)).Append("</section>");
            if (Can("Laboratorian")) sb.Append("<section class='panel compact'><h2>Finalize laboratory result</h2><form method='post' class='form-grid'>").Append(Hidden("resultLab"))
                .Append(Input("Lab order item ID", "labOrderItemId", true, "number")).Append(Input("Result", "resultValue", true)).Append(Input("Unit", "resultUnit", false))
                .Append(Select("Result flag", "resultFlag", new[] { "Normal", "Low", "High", "Critical", "Abnormal" }, false)).Append(Submit("Finalize result")).Append("</form></section>");
            return sb.ToString();
        }

        private string RenderPharmacy()
        {
            var stock = database.Query(@"SELECT TOP (100) m.MedicationName AS [Medication], b.BatchNumber AS [Batch], CONVERT(varchar(10),b.ExpirationDate,23) AS [Expires],
                b.QuantityOnHand AS [On hand], b.MinimumStock AS [Minimum], CASE WHEN b.QuantityOnHand <= b.MinimumStock THEN 'Low stock' WHEN b.ExpirationDate < DATEADD(day,30,CAST(SYSUTCDATETIME() AS date)) THEN 'Near expiry' ELSE 'In stock' END AS [Alert]
                FROM gh.MedicationBatches b JOIN gh.MedicationCatalog m ON m.MedicationId=b.MedicationId ORDER BY b.ExpirationDate");
            var sb = new StringBuilder("<section class='panel'><div class='panel-head'><div><h2>Pharmacy inventory</h2><p>Batch-level stock and expiration monitoring.</p></div></div>").Append(Table(stock)).Append("</section>");
            sb.Append("<section class='split'><div class='panel compact'><h2>Add medication</h2><form method='post' class='form-grid'>").Append(Hidden("addMedication"))
                .Append(Input("Code", "code", true)).Append(Input("Medication name", "name", true)).Append(Input("Form", "form", false)).Append(Input("Strength", "strength", false)).Append(Input("Unit price", "price", false, "number", "step='0.01'")).Append(Submit("Add medication")).Append("</form></div>")
                .Append("<div class='panel compact'><h2>Receive stock</h2><form method='post' class='form-grid'>").Append(Hidden("receiveStock")).Append(Options("Medication", "medicationId", "SELECT MedicationId AS Id, MedicationName AS Name FROM gh.MedicationCatalog WHERE IsActive=1 ORDER BY MedicationName", true))
                .Append(Input("Batch number", "batch", true)).Append(Input("Expiration", "expiration", true, "date")).Append(Input("Quantity", "quantity", true, "number", "step='0.01'")).Append(Input("Minimum stock", "minimum", false, "number", "step='0.01'")).Append(Input("Unit cost", "unitCost", false, "number", "step='0.01'")).Append(Submit("Receive stock")).Append("</form></div></section>");
            return sb.ToString();
        }

        private string RenderBilling()
        {
            var invoices = database.Query(@"SELECT TOP (100) i.InvoiceId AS [ID], i.InvoiceNumber AS [Invoice], p.MedicalRecordNumber AS [MRN], p.patientName AS [Patient],
                i.TotalAmount AS [Total], i.PaidAmount AS [Paid], i.TotalAmount-i.PaidAmount AS [Balance], i.Status FROM gh.Invoices i JOIN dbo.patientTbl p ON p.patientId=i.PatientId ORDER BY i.CreatedAt DESC");
            var sb = new StringBuilder("<section class='panel'><div class='panel-head'><div><h2>Invoices and balances</h2><p>Live billing records and payment status.</p></div></div>").Append(Table(invoices)).Append("</section>");
            sb.Append("<section class='split'><div class='panel compact'><h2>Create invoice</h2><form method='post' class='form-grid'>").Append(Hidden("createInvoice"))
                .Append(Options("Patient", "patientId", "SELECT patientId AS Id, MedicalRecordNumber + ' — ' + patientName AS Name FROM dbo.patientTbl WHERE IsActive=1 ORDER BY patientName", true)).Append(Input("Description", "description", true)).Append(Input("Amount", "amount", true, "number", "step='0.01'")).Append(Input("Due date", "dueDate", false, "date")).Append(Submit("Create invoice")).Append("</form></div>")
                .Append("<div class='panel compact'><h2>Record payment</h2><form method='post' class='form-grid'>").Append(Hidden("takePayment")).Append(Input("Invoice ID", "invoiceId", true, "number")).Append(Input("Amount", "amount", true, "number", "step='0.01'")).Append(Select("Method", "method", new[] { "Cash", "Card", "Transfer", "Insurance" }, true)).Append(Input("Reference", "reference", false)).Append(Submit("Record payment")).Append("</form></div></section>");
            return sb.ToString();
        }

        private string RenderOperations()
        {
            var admissions = database.Query(@"SELECT TOP (60) a.AdmissionId AS [ID], p.MedicalRecordNumber AS [MRN], p.patientName AS [Patient], w.WardName AS [Ward], r.RoomNumber AS [Room], b.BedNumber AS [Bed], a.Status, CONVERT(varchar(16),a.AdmittedAt,120) AS [Admitted]
                FROM gh.Admissions a JOIN dbo.patientTbl p ON p.patientId=a.PatientId LEFT JOIN gh.Beds b ON b.BedId=a.BedId LEFT JOIN gh.Rooms r ON r.RoomId=b.RoomId LEFT JOIN gh.Wards w ON w.WardId=r.WardId ORDER BY a.AdmittedAt DESC");
            var triage = database.Query(@"SELECT TOP (20) t.TriageAssessmentId AS [ID], p.MedicalRecordNumber AS [MRN], p.patientName AS [Patient], t.AcuityLevel AS [Acuity], t.ChiefComplaint AS [Complaint], CONVERT(varchar(16),t.AssessedAt,120) AS [Assessed]
                FROM gh.TriageAssessments t JOIN dbo.patientTbl p ON p.patientId=t.PatientId ORDER BY t.AssessedAt DESC");
            return "<section class='split'><div class='panel'><div class='panel-head'><div><h2>Admissions</h2><p>Current and recent inpatient activity.</p></div></div>" + Table(admissions) + "</div><div class='panel'><div class='panel-head'><div><h2>Emergency triage</h2><p>Latest acuity assessments.</p></div></div>" + Table(triage) + "</div></section>";
        }

        private string RenderAdmin()
        {
            var departments = database.Query("SELECT DepartmentCode AS [Code], DepartmentName AS [Department], Location, Phone, CASE WHEN IsActive=1 THEN 'Active' ELSE 'Inactive' END AS [Status] FROM gh.Departments ORDER BY DepartmentName");
            var audit = database.Query("SELECT TOP (30) ActionName AS [Action], EntityType AS [Entity], EntityId AS [ID], ActorRole AS [Role], CONVERT(varchar(16),OccurredAt,120) AS [When] FROM gh.AuditEvents ORDER BY OccurredAt DESC");
            var sb = new StringBuilder("<section class='split'><div class='panel'><div class='panel-head'><div><h2>Departments</h2><p>Hospital clinical and operational departments.</p></div></div>").Append(Table(departments)).Append("</div><div class='panel'><div class='panel-head'><div><h2>Audit trail</h2><p>Important security and record actions.</p></div></div>").Append(Table(audit)).Append("</div></section>");
            sb.Append("<section class='split'><div class='panel compact'><h2>Add department</h2><form method='post' class='form-grid'>").Append(Hidden("addDepartment")).Append(Input("Department code", "code", true)).Append(Input("Department name", "name", true)).Append(Input("Location", "location", false)).Append(Input("Telephone", "phone", false)).Append(Submit("Add department")).Append("</form></div>")
                .Append("<div class='panel compact'><h2>System setting</h2><form method='post' class='form-grid'>").Append(Hidden("setSetting")).Append(Input("Setting key", "key", true)).Append(Input("Value", "value", false)).Append(Submit("Save setting")).Append("</form></div></section>")
                .Append("<section class='split'><div class='panel compact'><h2>Create nurse or pharmacist</h2><form method='post' class='form-grid'>").Append(Hidden("createStaff")).Append(Input("Staff number", "staffNumber", true)).Append(Select("Role", "role", new[] { "Nurse", "Pharmacist" }, true)).Append(Input("Full name", "name", true)).Append(Input("Email", "email", true, "email")).Append(Input("Telephone", "phone", false)).Append(Options("Department", "departmentId", "SELECT DepartmentId AS Id, DepartmentName AS Name FROM gh.Departments WHERE IsActive=1 ORDER BY DepartmentName", false)).Append(Input("Temporary password", "password", true, "password")).Append(Submit("Create staff account")).Append("</form></div>")
                .Append("<div class='panel compact'><h2>Patient portal access</h2><form method='post' class='form-grid'>").Append(Hidden("createPatientAccess")).Append(Options("Patient", "patientId", "SELECT patientId AS Id, MedicalRecordNumber + ' — ' + patientName AS Name FROM dbo.patientTbl WHERE IsActive=1 ORDER BY patientName", true)).Append(Input("Email", "email", true, "email")).Append(Input("Temporary password", "password", true, "password")).Append(Submit("Create portal account")).Append("</form></div></section>");
            return sb.ToString();
        }

        private string RenderReports()
        {
            var activity = database.Query(@"SELECT d.DepartmentName AS [Department], COUNT(a.AppointmentId) AS [Appointments], SUM(CASE WHEN a.Status='Completed' THEN 1 ELSE 0 END) AS [Completed], SUM(CASE WHEN a.Status='NoShow' THEN 1 ELSE 0 END) AS [No shows]
                FROM gh.Departments d LEFT JOIN gh.Appointments a ON a.DepartmentId=d.DepartmentId AND a.ScheduledStart >= DATEADD(day,-30,SYSUTCDATETIME()) GROUP BY d.DepartmentName ORDER BY [Appointments] DESC");
            var lab = database.Query("SELECT Status, COUNT(*) AS [Orders] FROM gh.LabOrders GROUP BY Status ORDER BY [Orders] DESC");
            var finance = database.Query("SELECT Status, COUNT(*) AS [Invoices], SUM(TotalAmount) AS [Billed], SUM(PaidAmount) AS [Collected], SUM(TotalAmount-PaidAmount) AS [Outstanding] FROM gh.Invoices GROUP BY Status");
            return "<section class='panel'><div class='panel-head'><div><h2>30-day department activity</h2><p>Live appointment workload and no-show reporting.</p></div></div>" + Table(activity) + "</section><section class='split'><div class='panel'><h2>Laboratory workload</h2>" + Table(lab) + "</div><div class='panel'><h2>Billing summary</h2>" + Table(finance) + "</div></section>";
        }

        private string RenderSearch()
        {
            var term = ValueQuery("q");
            var table = string.IsNullOrWhiteSpace(term) ? new DataTable() : database.Query(@"SELECT TOP (50) p.patientId AS [ID], p.MedicalRecordNumber AS [MRN], p.patientName AS [Patient], p.patientPhone AS [Phone], pp.BloodType AS [Blood type], pp.PatientStatus AS [Status]
                FROM dbo.patientTbl p LEFT JOIN gh.PatientProfiles pp ON pp.PatientId=p.patientId WHERE p.patientName LIKE '%' + @Search + '%' OR p.MedicalRecordNumber LIKE '%' + @Search + '%' OR p.patientPhone LIKE '%' + @Search + '%' ORDER BY p.patientName", Database.Parameter("@Search", term));
            return "<section class='panel'><div class='panel-head'><div><h2>Authorized patient search</h2><p>Search returns only the minimum profile information needed for care.</p></div><form class='inline-search' method='get'><input type='hidden' name='module' value='search'><input name='q' value='" + H(term) + "' placeholder='Name, MRN, or telephone'><button>Search</button></form></div>" + (string.IsNullOrWhiteSpace(term) ? Empty("Enter a patient name, MRN, or telephone number to search.") : Table(table)) + "</section>";
        }

        private string RenderPatientPortal()
        {
            var patientId = CurrentUser.PatientId.GetValueOrDefault();
            if (patientId < 1) return "<section class='panel'>" + Empty("Your patient account is not linked to a patient record. Please contact reception.") + "</section>";
            var profile = database.Query(@"SELECT p.MedicalRecordNumber AS [MRN], p.patientName AS [Name], p.patientPhone AS [Telephone], pp.Email, pp.BloodType AS [Blood type], pp.PatientStatus AS [Status]
                FROM dbo.patientTbl p LEFT JOIN gh.PatientProfiles pp ON pp.PatientId=p.patientId WHERE p.patientId=@PatientId", Database.Parameter("@PatientId", patientId));
            var appointments = database.Query(@"SELECT TOP (10) a.AppointmentId AS [ID], d.doctorName AS [Clinician], CONVERT(varchar(16),a.ScheduledStart,120) AS [Start], a.Status, a.Reason
                FROM gh.Appointments a JOIN dbo.doctorTbl d ON d.doctorID=a.DoctorId WHERE a.PatientId=@PatientId ORDER BY a.ScheduledStart DESC", Database.Parameter("@PatientId", patientId));
            var results = database.Query(@"SELECT TOP (10) c.TestName AS [Test], i.ResultValue AS [Result], i.ResultUnit AS [Unit], i.ResultFlag AS [Flag], CONVERT(varchar(16),i.ResultedAt,120) AS [Resulted]
                FROM gh.LabOrderItems i JOIN gh.LabOrders o ON o.LabOrderId=i.LabOrderId JOIN gh.LabCatalog c ON c.LabTestId=i.LabTestId WHERE o.PatientId=@PatientId AND i.ResultStatus='Final' ORDER BY i.ResultedAt DESC", Database.Parameter("@PatientId", patientId));
            var invoices = database.Query("SELECT TOP (10) InvoiceNumber AS [Invoice], TotalAmount AS [Total], PaidAmount AS [Paid], TotalAmount-PaidAmount AS [Balance], Status FROM gh.Invoices WHERE PatientId=@PatientId ORDER BY CreatedAt DESC", Database.Parameter("@PatientId", patientId));
            return "<section class='panel'><div class='panel-head'><div><h2>My profile</h2><p>Contact reception to update personal or insurance details.</p></div></div>" + Table(profile) + "</section><section class='split'><div class='panel'><h2>My appointments</h2>" + Table(appointments) + "</div><div class='panel'><h2>My laboratory results</h2>" + Table(results) + "</div></section><section class='panel'><h2>My invoices</h2>" + Table(invoices) + "</section>";
        }

        private bool CanOpen(string candidate)
        {
            if (candidate == "dashboard" || candidate == "patients" || candidate == "appointments") return true;
            if (candidate == "clinical") return Can("Doctor");
            if (candidate == "laboratory") return Can("Laboratorian", "Doctor");
            if (candidate == "pharmacy") return Can("Admin", "Pharmacist");
            if (candidate == "billing") return Can("Admin", "Receptionist");
            if (candidate == "operations") return Can("Admin", "Receptionist", "Nurse");
            if (candidate == "admin" || candidate == "reports") return Can("Admin");
            if (candidate == "search") return !Can("Patient");
            return false;
        }
        private bool Can(params string[] roles) { foreach (var role in roles) if (string.Equals(CurrentUser.Role, role, StringComparison.OrdinalIgnoreCase)) return true; return false; }
        private static string NormalizeModule(string value) { var modules = new[] { "dashboard", "patients", "appointments", "clinical", "laboratory", "pharmacy", "billing", "operations", "admin", "reports", "search", "patient" }; foreach (var module in modules) if (module == value) return module; return "dashboard"; }
        private string ModuleTitle() { return module == "dashboard" ? (Can("Patient") ? "My Health" : "Hospital overview") : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(module.Replace("-", " ")); }
        private void Nav(StringBuilder sb, string target, string label, string icon) { sb.Append("<a class='").Append(module == target ? "active" : "").Append("' href='Portal.aspx?module=").Append(target).Append("'><i>").Append(icon).Append("</i>").Append(H(label)).Append("</a>"); }
        private static void Metric(StringBuilder sb, string label, int value, string caption) { sb.Append("<article class='metric'><span>").Append(H(label)).Append("</span><strong>").Append(value.ToString("N0")).Append("</strong><small>").Append(H(caption)).Append("</small></article>"); }
        private int Count(string sql) { return Convert.ToInt32(database.Scalar(sql)); }
        private string Hidden(string action) { return "<input type='hidden' name='csrf' value='" + H(csrf) + "'><input type='hidden' name='action' value='" + H(action) + "'>"; }
        private static string Input(string label, string name, bool required, string type = "text", string extra = "") { return "<label>" + H(label) + "<input type='" + H(type) + "' name='" + H(name) + "' " + (required ? "required " : "") + extra + "></label>"; }
        private static string Textarea(string label, string name) { return "<label class='wide'>" + H(label) + "<textarea name='" + H(name) + "' rows='3'></textarea></label>"; }
        private static string Select(string label, string name, IEnumerable<string> values, bool required) { var sb = new StringBuilder("<label>").Append(H(label)).Append("<select name='").Append(H(name)).Append("' ").Append(required ? "required" : "").Append("><option value=''>Select…</option>"); foreach (var value in values) sb.Append("<option value='").Append(H(value)).Append("'>").Append(H(value)).Append("</option>"); return sb.Append("</select></label>").ToString(); }
        private string Options(string label, string name, string sql, bool required) { var data = database.Query(sql); var sb = new StringBuilder("<label>").Append(H(label)).Append("<select name='").Append(H(name)).Append("' ").Append(required ? "required" : "").Append("><option value=''>Select…</option>"); foreach (DataRow row in data.Rows) sb.Append("<option value='").Append(H(Convert.ToString(row["Id"]))).Append("'>").Append(H(Convert.ToString(row["Name"]))).Append("</option>"); return sb.Append("</select></label>").ToString(); }
        private static string Submit(string text) { return "<div class='form-action'><button class='button' type='submit'>" + H(text) + "</button></div>"; }
        private static string Table(DataTable table) { if (table == null || table.Rows.Count == 0) return Empty("No records found."); var sb = new StringBuilder("<div class='table-scroll'><table><thead><tr>"); foreach (DataColumn column in table.Columns) sb.Append("<th>").Append(H(column.ColumnName)).Append("</th>"); sb.Append("</tr></thead><tbody>"); foreach (DataRow row in table.Rows) { sb.Append("<tr>"); foreach (DataColumn column in table.Columns) sb.Append("<td>").Append(H(row[column] == DBNull.Value ? "—" : Convert.ToString(row[column]))).Append("</td>"); sb.Append("</tr>"); } return sb.Append("</tbody></table></div>").ToString(); }
        private static string Empty(string text) { return "<div class='empty'>" + H(text) + "</div>"; }
        private static string H(string text) { return HttpUtility.HtmlEncode(text ?? string.Empty); }
        private string Value(string key) { return (Request.Form[key] ?? string.Empty).Trim(); }
        private string ValueQuery(string key) { return (Request.QueryString[key] ?? string.Empty).Trim(); }
        private string Required(string key) { var value = Value(key); if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Complete the required " + key + " field."); return value; }
        private int Integer(string key) { int value; if (!int.TryParse(Value(key), out value) || value < 1) throw new ArgumentException("Select a valid " + key + "."); return value; }
        private int? NullableInteger(string key) { int value; return int.TryParse(Value(key), out value) && value > 0 ? (int?)value : null; }
        private long Long(string key) { long value; if (!long.TryParse(Value(key), out value) || value < 1) throw new ArgumentException("Enter a valid " + key + "."); return value; }
        private long? NullableLong(string key) { long value; return long.TryParse(Value(key), out value) && value > 0 ? (long?)value : null; }
        private DateTime? Date(string key) { DateTime value; return DateTime.TryParse(Value(key), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out value) ? (DateTime?)value : null; }
        private DateTime RequiredDate(string key) { var value = Date(key); if (!value.HasValue) throw new ArgumentException("Enter a valid " + key + "."); return value.Value; }
        private DateTime RequiredDateTime(string key) { DateTime value; if (!DateTime.TryParse(Value(key), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out value)) throw new ArgumentException("Enter a valid " + key + "."); return value; }
        private decimal? Decimal(string key) { decimal value; return decimal.TryParse(Value(key), NumberStyles.Number, CultureInfo.InvariantCulture, out value) ? (decimal?)value : null; }
        private decimal RequiredDecimal(string key) { var value = Decimal(key); if (!value.HasValue || value.Value <= 0) throw new ArgumentException("Enter a valid " + key + "."); return value.Value; }
        private void Redirect(string message, bool isError = false) { Response.Redirect("Portal.aspx?module=" + HttpUtility.UrlEncode(module) + "&" + (isError ? "error" : "notice") + "=" + HttpUtility.UrlEncode(message), false); Context.ApplicationInstance.CompleteRequest(); }
    }
}
