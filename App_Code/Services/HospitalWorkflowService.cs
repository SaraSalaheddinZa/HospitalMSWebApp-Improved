using System;
using System.Data;
using System.Data.SqlClient;
using HospitalMSWebApp.Infrastructure;

namespace HospitalMSWebApp.Services
{
    /// <summary>Transactional workflows used by the Guardian Health workspace.</summary>
    public sealed class HospitalWorkflowService
    {
        private readonly Database database = new Database();

        public int RegisterPatient(string name, string phone, string gender, DateTime? birthDate, string address,
            string allergies, string email, string emergencyName, string emergencyPhone, string bloodType,
            string chronicConditions, int registeredBy)
        {
            return database.InTransaction((connection, transaction) =>
            {
                var patientId = Convert.ToInt32(Scalar(connection, transaction, @"INSERT INTO dbo.patientTbl
                    (patientName, patientPhone, patientGender, patientDOB, patientAdd, patientAllergies, addBy)
                    VALUES (@Name, @Phone, @Gender, @BirthDate, @Address, @Allergies, @RegisteredBy);
                    SELECT CAST(SCOPE_IDENTITY() AS int);",
                    P("@Name", name), P("@Phone", phone), P("@Gender", gender), P("@BirthDate", birthDate),
                    P("@Address", address), P("@Allergies", allergies), P("@RegisteredBy", registeredBy)));
                var mrn = "GH-" + patientId.ToString("D8");
                Execute(connection, transaction, "UPDATE dbo.patientTbl SET MedicalRecordNumber = @Mrn WHERE patientId = @PatientId",
                    P("@Mrn", mrn), P("@PatientId", patientId));
                Execute(connection, transaction, @"INSERT INTO gh.PatientProfiles
                    (PatientId, Email, EmergencyContactName, EmergencyContactPhone, BloodType, ChronicConditions)
                    VALUES (@PatientId, @Email, @EmergencyName, @EmergencyPhone, @BloodType, @ChronicConditions)",
                    P("@PatientId", patientId), P("@Email", email), P("@EmergencyName", emergencyName),
                    P("@EmergencyPhone", emergencyPhone), P("@BloodType", bloodType), P("@ChronicConditions", chronicConditions));
                return patientId;
            });
        }

        public long BookAppointment(int patientId, int doctorId, DateTime start, DateTime end, string reason, int bookedBy)
        {
            return Convert.ToInt64(database.Scalar(@"EXEC gh.BookAppointment @PatientId, @DoctorId, NULL, @Start, @End, @Reason, @BookedBy",
                P("@PatientId", patientId), P("@DoctorId", doctorId), P("@Start", start), P("@End", end),
                P("@Reason", reason), P("@BookedBy", bookedBy)));
        }

        public long RecordEncounter(int patientId, int doctorId, long? appointmentId, string complaint, string assessment,
            string plan, DateTime? followUp, decimal? temperature, int? pulse, int? systolic, int? diastolic,
            string diagnosis, int? medicationId, string dose, string frequency, decimal? quantity, int? labTestId)
        {
            return database.InTransaction((connection, transaction) =>
            {
                var encounterId = Convert.ToInt64(Scalar(connection, transaction, @"INSERT INTO gh.Encounters
                    (AppointmentId, PatientId, DoctorId, ChiefComplaint, Assessment, TreatmentPlan, FollowUpDate)
                    VALUES (@AppointmentId, @PatientId, @DoctorId, @Complaint, @Assessment, @Plan, @FollowUp);
                    SELECT CAST(SCOPE_IDENTITY() AS bigint);",
                    P("@AppointmentId", appointmentId), P("@PatientId", patientId), P("@DoctorId", doctorId),
                    P("@Complaint", complaint), P("@Assessment", assessment), P("@Plan", plan), P("@FollowUp", followUp)));

                if (temperature.HasValue || pulse.HasValue || systolic.HasValue || diastolic.HasValue)
                    Execute(connection, transaction, @"INSERT INTO gh.VitalSigns(EncounterId, TemperatureC, Pulse, SystolicBp, DiastolicBp)
                        VALUES(@EncounterId, @Temperature, @Pulse, @Systolic, @Diastolic)", P("@EncounterId", encounterId),
                        P("@Temperature", temperature), P("@Pulse", pulse), P("@Systolic", systolic), P("@Diastolic", diastolic));
                if (!string.IsNullOrWhiteSpace(diagnosis))
                    Execute(connection, transaction, "INSERT INTO gh.Diagnoses(EncounterId, DiagnosisName, IsPrimary) VALUES(@EncounterId, @Diagnosis, 1)",
                        P("@EncounterId", encounterId), P("@Diagnosis", diagnosis));
                if (medicationId.HasValue && quantity.GetValueOrDefault() > 0 && !string.IsNullOrWhiteSpace(dose) && !string.IsNullOrWhiteSpace(frequency))
                {
                    var prescriptionId = Convert.ToInt64(Scalar(connection, transaction, @"INSERT INTO gh.PrescriptionOrders
                        (EncounterId, PatientId, PrescribedByDoctorId) VALUES(@EncounterId, @PatientId, @DoctorId);
                        SELECT CAST(SCOPE_IDENTITY() AS bigint);", P("@EncounterId", encounterId), P("@PatientId", patientId), P("@DoctorId", doctorId)));
                    Execute(connection, transaction, @"INSERT INTO gh.PrescriptionItems(PrescriptionOrderId, MedicationId, Dose, Frequency, Quantity)
                        VALUES(@PrescriptionId, @MedicationId, @Dose, @Frequency, @Quantity)", P("@PrescriptionId", prescriptionId),
                        P("@MedicationId", medicationId), P("@Dose", dose), P("@Frequency", frequency), P("@Quantity", quantity));
                }
                if (labTestId.HasValue)
                {
                    var labOrderId = Convert.ToInt64(Scalar(connection, transaction, @"INSERT INTO gh.LabOrders
                        (EncounterId, PatientId, OrderedByDoctorId) VALUES(@EncounterId, @PatientId, @DoctorId);
                        SELECT CAST(SCOPE_IDENTITY() AS bigint);", P("@EncounterId", encounterId), P("@PatientId", patientId), P("@DoctorId", doctorId)));
                    Execute(connection, transaction, "INSERT INTO gh.LabOrderItems(LabOrderId, LabTestId, ReferenceRange, ResultStatus) SELECT @LabOrderId, LabTestId, ReferenceRange, 'Pending' FROM gh.LabCatalog WHERE LabTestId = @LabTestId",
                        P("@LabOrderId", labOrderId), P("@LabTestId", labTestId));
                }
                if (appointmentId.HasValue)
                    Execute(connection, transaction, "UPDATE gh.Appointments SET Status = 'Completed', UpdatedAt = SYSUTCDATETIME() WHERE AppointmentId = @AppointmentId", P("@AppointmentId", appointmentId));
                return encounterId;
            });
        }

        public void FinalizeLabResult(long itemId, string value, string unit, string flag, int userId)
        {
            database.InTransaction((connection, transaction) =>
            {
                var orderId = Convert.ToInt64(Scalar(connection, transaction, @"UPDATE gh.LabOrderItems SET ResultValue = @Value, ResultUnit = @Unit,
                    ResultFlag = @Flag, ResultStatus = 'Final', ResultedAt = SYSUTCDATETIME(), ResultedByUserId = @UserId
                    OUTPUT INSERTED.LabOrderId WHERE LabOrderItemId = @ItemId", P("@Value", value), P("@Unit", unit),
                    P("@Flag", flag), P("@UserId", userId), P("@ItemId", itemId)));
                Execute(connection, transaction, @"UPDATE gh.LabOrders SET Status = CASE WHEN NOT EXISTS
                    (SELECT 1 FROM gh.LabOrderItems WHERE LabOrderId = @OrderId AND ResultStatus <> 'Final') THEN 'Completed' ELSE 'Processing' END
                    WHERE LabOrderId = @OrderId", P("@OrderId", orderId));
                return 0;
            });
        }

        public void ReceiveStock(int medicationId, string batchNumber, DateTime expires, decimal quantity, decimal minimum, decimal unitCost, int? supplierId)
        {
            database.Execute(@"INSERT INTO gh.MedicationBatches(MedicationId, SupplierId, BatchNumber, ExpirationDate, QuantityReceived, QuantityOnHand, MinimumStock, UnitCost)
                VALUES(@MedicationId, @SupplierId, @BatchNumber, @Expires, @Quantity, @Quantity, @Minimum, @UnitCost)",
                P("@MedicationId", medicationId), P("@SupplierId", supplierId), P("@BatchNumber", batchNumber), P("@Expires", expires),
                P("@Quantity", quantity), P("@Minimum", minimum), P("@UnitCost", unitCost));
        }

        public long CreateInvoice(int patientId, string description, decimal amount, DateTime? dueDate)
        {
            return database.InTransaction((connection, transaction) =>
            {
                var number = "INV-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                var invoiceId = Convert.ToInt64(Scalar(connection, transaction, @"INSERT INTO gh.Invoices(InvoiceNumber, PatientId, Status, TotalAmount, DueDate)
                    VALUES(@Number, @PatientId, 'Open', @Amount, @DueDate); SELECT CAST(SCOPE_IDENTITY() AS bigint);",
                    P("@Number", number), P("@PatientId", patientId), P("@Amount", amount), P("@DueDate", dueDate)));
                Execute(connection, transaction, "INSERT INTO gh.InvoiceItems(InvoiceId, ItemType, Description, Quantity, UnitPrice) VALUES(@InvoiceId, 'Service', @Description, 1, @Amount)",
                    P("@InvoiceId", invoiceId), P("@Description", description), P("@Amount", amount));
                return invoiceId;
            });
        }

        public void TakePayment(long invoiceId, decimal amount, string method, string reference, int userId)
        {
            database.InTransaction((connection, transaction) =>
            {
                var totals = QueryOne(connection, transaction, "SELECT TotalAmount, PaidAmount FROM gh.Invoices WITH (UPDLOCK, HOLDLOCK) WHERE InvoiceId = @InvoiceId", P("@InvoiceId", invoiceId));
                if (totals == null) throw new InvalidOperationException("Invoice was not found.");
                var remaining = Convert.ToDecimal(totals["TotalAmount"]) - Convert.ToDecimal(totals["PaidAmount"]);
                if (amount <= 0 || amount > remaining) throw new InvalidOperationException("Payment must be greater than zero and no more than the outstanding balance.");
                Execute(connection, transaction, "INSERT INTO gh.Payments(InvoiceId, Amount, Method, ReferenceNumber, ReceivedByUserId) VALUES(@InvoiceId, @Amount, @Method, @Reference, @UserId)",
                    P("@InvoiceId", invoiceId), P("@Amount", amount), P("@Method", method), P("@Reference", reference), P("@UserId", userId));
                Execute(connection, transaction, @"UPDATE gh.Invoices SET PaidAmount = PaidAmount + @Amount, Status = CASE WHEN PaidAmount + @Amount >= TotalAmount THEN 'Paid' ELSE 'PartiallyPaid' END
                    WHERE InvoiceId = @InvoiceId", P("@Amount", amount), P("@InvoiceId", invoiceId));
                return 0;
            });
        }

        public int AddDepartment(string code, string name, string location, string phone)
        {
            return Convert.ToInt32(database.Scalar(@"INSERT INTO gh.Departments(DepartmentCode, DepartmentName, Location, Phone)
                VALUES(@Code, @Name, @Location, @Phone); SELECT CAST(SCOPE_IDENTITY() AS int);", P("@Code", code), P("@Name", name), P("@Location", location), P("@Phone", phone)));
        }

        public int CreateStaffAccount(string staffNumber, string role, string name, string email, string phone, int? departmentId, string password)
        {
            return database.InTransaction((connection, transaction) =>
            {
                var staffId = Convert.ToInt32(Scalar(connection, transaction, @"INSERT INTO gh.StaffMembers
                    (StaffNumber, DepartmentId, RoleName, FullName, Email, Phone) VALUES(@Number, @DepartmentId, @Role, @Name, @Email, @Phone);
                    SELECT CAST(SCOPE_IDENTITY() AS int);", P("@Number", staffNumber), P("@DepartmentId", departmentId), P("@Role", role),
                    P("@Name", name), P("@Email", email), P("@Phone", phone)));
                var userId = Convert.ToInt32(Scalar(connection, transaction, @"INSERT INTO gh.UserAccounts
                    (StaffMemberId, RoleName, DisplayName, Email, NormalizedEmail, PasswordHash) VALUES(@StaffId, @Role, @Name, @Email, @NormalizedEmail, @PasswordHash);
                    SELECT CAST(SCOPE_IDENTITY() AS int);", P("@StaffId", staffId), P("@Role", role), P("@Name", name), P("@Email", email),
                    P("@NormalizedEmail", email.ToUpperInvariant()), P("@PasswordHash", PasswordSecurity.Hash(password))));
                return userId;
            });
        }

        public int CreatePatientAccount(int patientId, string email, string password)
        {
            return database.InTransaction((connection, transaction) =>
            {
                var name = Convert.ToString(Scalar(connection, transaction, "SELECT patientName FROM dbo.patientTbl WHERE patientId=@PatientId AND IsActive=1", P("@PatientId", patientId)));
                if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("The patient record is not active.");
                var userId = Convert.ToInt32(Scalar(connection, transaction, @"INSERT INTO gh.UserAccounts
                    (PatientId, RoleName, DisplayName, Email, NormalizedEmail, PasswordHash) VALUES(@PatientId, 'Patient', @Name, @Email, @NormalizedEmail, @PasswordHash);
                    SELECT CAST(SCOPE_IDENTITY() AS int);", P("@PatientId", patientId), P("@Name", name), P("@Email", email),
                    P("@NormalizedEmail", email.ToUpperInvariant()), P("@PasswordHash", PasswordSecurity.Hash(password))));
                Execute(connection, transaction, "UPDATE gh.PatientProfiles SET Email=@Email, UpdatedAt=SYSUTCDATETIME() WHERE PatientId=@PatientId", P("@Email", email), P("@PatientId", patientId));
                return userId;
            });
        }

        private static SqlParameter P(string name, object value) { return Database.Parameter(name, value); }
        private static object Scalar(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = Command(connection, transaction, sql, parameters)) return command.ExecuteScalar();
        }
        private static int Execute(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = Command(connection, transaction, sql, parameters)) return command.ExecuteNonQuery();
        }
        private static DataRow QueryOne(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            var table = new DataTable();
            using (var command = Command(connection, transaction, sql, parameters))
            using (var adapter = new SqlDataAdapter(command)) adapter.Fill(table);
            return table.Rows.Count == 0 ? null : table.Rows[0];
        }
        private static SqlCommand Command(SqlConnection connection, SqlTransaction transaction, string sql, SqlParameter[] parameters)
        {
            var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddRange(parameters);
            return command;
        }
    }
}
