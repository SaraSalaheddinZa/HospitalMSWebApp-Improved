using System;
using System.Data;
using System.Data.SqlClient;
using HospitalMSWebApp.Infrastructure;

namespace HospitalMSWebApp.Services
{
    /// <summary>Transactional workflows used by the Guardian Health workspace.</summary>
    public sealed partial class HospitalWorkflowService
    {
        private readonly Database database = new Database();

        public int RegisterPatient(string name, string phone, string gender, DateTime? birthDate, string address,
            string allergies, string email, string emergencyName, string emergencyPhone, string bloodType,
            string chronicConditions, int? registeredBy, int actor = 0)
        {
            if(string.IsNullOrWhiteSpace(name)||name.Length>100||!birthDate.HasValue||birthDate>DateTime.UtcNow.Date||birthDate.Value.Year<1753)throw new ArgumentException("Enter a patient name and a birth date that is not in the future.");
            bloodType=string.IsNullOrWhiteSpace(bloodType)||bloodType=="Unknown"?null:bloodType.Trim();
            if(bloodType!=null&&Array.IndexOf(new[]{"A+","A-","B+","B-","AB+","AB-","O+","O-"},bloodType)<0)throw new ArgumentException("Select a valid blood group, or leave it unknown.");
            return database.InTransaction((connection, transaction) =>
            {
                actor=RequireActor(connection,transaction,actor,"Receptionist");
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
                History(connection,transaction,"Patient",patientId,actor,"Registered",mrn);
                return patientId;
            });
        }

        public void UpdatePatientBasic(int patientId, string name, string phone, string gender, DateTime? birthDate,
            string address, string allergies, int actor = 0)
        {
            if(string.IsNullOrWhiteSpace(name)||name.Length>100||birthDate>DateTime.UtcNow.Date)throw new ArgumentException("Enter a patient name and a valid birth date.");
            database.InTransaction((c,t)=>{
            actor=RequireActor(c,t,actor,"Receptionist");
            var affected = Execute(c,t,@"UPDATE dbo.patientTbl SET patientName=@Name, patientPhone=@Phone,
                patientGender=@Gender, patientDOB=@BirthDate, patientAdd=@Address, patientAllergies=@Allergies
                WHERE patientId=@PatientId AND IsActive=1", P("@Name", name), P("@Phone", phone), P("@Gender", gender),
                P("@BirthDate", birthDate), P("@Address", address), P("@Allergies", allergies), P("@PatientId", patientId));
            if (affected != 1) throw new InvalidOperationException("The active patient record was not found.");
            History(c,t,"Patient",patientId,actor,"Updated","Patient registration details updated");return 0;
            });
        }

        public void DeactivatePatient(int patientId, int actor = 0)
        {
            database.InTransaction((connection, transaction) =>
            {
                actor=RequireActor(connection,transaction,actor,"Receptionist");
                if(Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM gh.Admissions WITH(UPDLOCK,HOLDLOCK) WHERE PatientId=@P AND DischargedAt IS NULL",P("@P",patientId)))>0)throw new InvalidOperationException("Discharge the active admission before deactivating this patient.");
                var affected = Execute(connection, transaction, "UPDATE dbo.patientTbl SET IsActive=0 WHERE patientId=@PatientId AND IsActive=1", P("@PatientId", patientId));
                if (affected != 1) throw new InvalidOperationException("The active patient record was not found.");
                Execute(connection, transaction, "UPDATE gh.PatientProfiles SET PatientStatus='Inactive', UpdatedAt=SYSUTCDATETIME() WHERE PatientId=@PatientId", P("@PatientId", patientId));
                Execute(connection, transaction, "UPDATE gh.UserAccounts SET IsActive=0, AuthVersion=AuthVersion+1 WHERE PatientId=@PatientId", P("@PatientId", patientId));
                History(connection,transaction,"Patient",patientId,actor,"Inactive","Patient access deactivated; historical records retained");
                return 0;
            });
        }

        public long BookAppointment(int patientId, int doctorId, DateTime start, DateTime end, string reason, int bookedBy)
        {
            return database.InTransaction((c,t)=>BookAppointmentCore(c,t,patientId,doctorId,start,end,reason,bookedBy));
        }
        private long BookAppointmentCore(SqlConnection connection,SqlTransaction transaction,int patientId,int doctorId,DateTime start,DateTime end,string reason,int bookedBy)
        {
            if(start<=DateTime.UtcNow || end<=start || start.Date!=end.Date || start.Ticks%TimeSpan.TicksPerMinute!=0 || end.Ticks%TimeSpan.TicksPerMinute!=0) throw new ArgumentException("Choose a future appointment within one day, using whole minutes.");
            if((reason??"").Length>500)throw new ArgumentException("Keep the appointment reason within 500 characters.");
                RequireActor(connection,transaction,bookedBy,"Receptionist","Patient");
                var caller=QueryOne(connection,transaction,"SELECT RoleName,PatientId FROM gh.UserAccounts WHERE UserId=@U",P("@U",bookedBy));
                if(Convert.ToString(caller["RoleName"])=="Patient"&&Convert.ToInt32(caller["PatientId"])!=patientId)throw new InvalidOperationException("You can only book appointments for your own patient record.");
                if(Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM dbo.patientTbl WHERE patientId=@P AND IsActive=1",P("@P",patientId)))!=1)throw new InvalidOperationException("Select an active patient.");
                if(Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM dbo.doctorTbl WHERE doctorID=@D AND doctorStatus='Active'",P("@D",doctorId)))!=1)throw new InvalidOperationException("Select an active clinician.");
                if(Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM gh.Appointments WITH(UPDLOCK,HOLDLOCK) WHERE PatientId=@P AND Status NOT IN('Cancelled','NoShow') AND @S<ScheduledEnd AND @E>ScheduledStart",P("@P",patientId),P("@S",start),P("@E",end)))>0)throw new InvalidOperationException("This patient already has an overlapping appointment.");
                if(Convert.ToInt32(Scalar(connection,transaction,@"SELECT COUNT(*) FROM gh.DoctorSchedules WITH(UPDLOCK,HOLDLOCK) WHERE DoctorId=@D AND ScheduleDate=CAST(@Start AS date) AND IsAvailable=1 AND CAST(@Start AS time)>=StartTime AND CAST(@End AS time)<=EndTime AND DATEDIFF(minute,StartTime,CAST(@Start AS time))%SlotMinutes=0 AND DATEDIFF(minute,@Start,@End)%SlotMinutes=0",P("@D",doctorId),P("@Start",start),P("@End",end)))==0)throw new InvalidOperationException("Choose a time within published clinician availability, aligned to its slot length.");
                var appointmentId = Convert.ToInt64(Scalar(connection, transaction, @"EXEC gh.BookAppointment @PatientId, @DoctorId, NULL, @Start, @End, @Reason, @BookedBy",
                    P("@PatientId", patientId), P("@DoctorId", doctorId), P("@Start", start), P("@End", end),
                    P("@Reason", reason), P("@BookedBy", bookedBy)));
                Execute(connection, transaction, @"INSERT INTO gh.Notifications(UserId,PatientId,NotificationType,Subject,Message,ScheduledFor,SentAt)
                    SELECT ua.UserId,@PatientId,'Appointment','Appointment booked','Your appointment has been booked.',SYSUTCDATETIME(),SYSUTCDATETIME()
                    FROM gh.UserAccounts ua WHERE ua.PatientId=@PatientId AND ua.RoleName='Patient' AND ua.IsActive=1",
                    P("@PatientId", patientId));
                History(connection,transaction,"Appointment",appointmentId,bookedBy,"Booked",reason);
                return appointmentId;
        }

        public int AddDoctorSchedule(int doctorId, int? departmentId, DateTime scheduleDate, TimeSpan startTime, TimeSpan endTime, int slotMinutes, int actor = 0)
        {
            if (scheduleDate.Date < DateTime.UtcNow.Date) throw new InvalidOperationException("Schedules cannot be published in the past.");
            if (startTime<TimeSpan.Zero||endTime>=TimeSpan.FromDays(1)||endTime <= startTime || startTime.Ticks%TimeSpan.TicksPerMinute!=0||endTime.Ticks%TimeSpan.TicksPerMinute!=0||slotMinutes < 5 || slotMinutes > 120) throw new InvalidOperationException("Enter a valid same-day time window using whole minutes and a slot length of 5–120 minutes.");
            return database.InTransaction((connection, transaction) =>
            {
                actor=RequireActor(connection,transaction,actor,"Admin");
                if(Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM dbo.doctorTbl WHERE doctorID=@D AND doctorStatus='Active'",P("@D",doctorId)))!=1)throw new InvalidOperationException("Publish availability for an active clinician.");
                if(departmentId.HasValue&&Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM gh.Departments WHERE DepartmentId=@D AND IsActive=1",P("@D",departmentId)))!=1)throw new InvalidOperationException("Select an active department.");
                var conflict = Scalar(connection, transaction, @"SELECT COUNT(*) FROM gh.DoctorSchedules WITH (UPDLOCK,HOLDLOCK)
                    WHERE DoctorId=@DoctorId AND ScheduleDate=@ScheduleDate AND @StartTime < EndTime AND @EndTime > StartTime",
                    P("@DoctorId", doctorId), P("@ScheduleDate", scheduleDate.Date), P("@StartTime", startTime), P("@EndTime", endTime));
                if (Convert.ToInt32(conflict) > 0) throw new InvalidOperationException("This schedule overlaps an existing availability window.");
                var id=Convert.ToInt32(Scalar(connection, transaction, @"INSERT INTO gh.DoctorSchedules
                    (DoctorId,DepartmentId,ScheduleDate,StartTime,EndTime,SlotMinutes,IsAvailable)
                    VALUES(@DoctorId,@DepartmentId,@ScheduleDate,@StartTime,@EndTime,@SlotMinutes,1); SELECT CAST(SCOPE_IDENTITY() AS int);",
                    P("@DoctorId", doctorId), P("@DepartmentId", departmentId), P("@ScheduleDate", scheduleDate.Date),
                    P("@StartTime", startTime), P("@EndTime", endTime), P("@SlotMinutes", slotMinutes)));
                History(connection,transaction,"DoctorSchedule",id,actor,"Published",scheduleDate.ToString("yyyy-MM-dd"));return id;
            });
        }

        public long RecordEncounter(int patientId, int doctorId, long? appointmentId, string complaint, string assessment,
            string plan, DateTime? followUp, decimal? temperature, int? pulse, int? systolic, int? diastolic,
            string diagnosis, int? medicationId, string dose, string frequency, decimal? quantity, int? labTestId, int actor = 0)
        {
            if(string.IsNullOrWhiteSpace(complaint)||complaint.Length>1000)throw new ArgumentException("Enter a chief complaint of up to 1,000 characters.");
            if(medicationId.HasValue && (quantity.GetValueOrDefault()<=0 || string.IsNullOrWhiteSpace(dose) || string.IsNullOrWhiteSpace(frequency))) throw new ArgumentException("A prescription requires dose, frequency and positive quantity.");
            if(temperature<25||temperature>45||pulse<1||pulse>300||systolic<1||systolic>350||diastolic<1||diastolic>250)throw new ArgumentException("Check the entered vital signs.");
            if(followUp.HasValue && followUp.Value.Date<DateTime.UtcNow.Date)throw new ArgumentException("Follow-up cannot be in the past.");
            return database.InTransaction((connection, transaction) =>
            {
                actor=RequireDoctor(connection,transaction,actor,doctorId);
                if(Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM dbo.patientTbl WHERE patientId=@P AND IsActive=1",P("@P",patientId)))!=1)throw new InvalidOperationException("Select an active patient.");
                if(Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM dbo.doctorTbl WHERE doctorID=@D AND doctorStatus='Active'",P("@D",doctorId)))!=1)throw new InvalidOperationException("Select an active clinician.");
                if(medicationId.HasValue && Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM gh.MedicationCatalog WHERE MedicationId=@M AND IsActive=1",P("@M",medicationId)))!=1)throw new InvalidOperationException("Select an active medication.");
                if(labTestId.HasValue && Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM gh.LabCatalog WHERE LabTestId=@L AND IsActive=1",P("@L",labTestId)))!=1)throw new InvalidOperationException("Select an active laboratory test.");
                string appointmentStatus=null;
                if(appointmentId.HasValue){
                    var appointment=QueryOne(connection,transaction,"SELECT Status,CheckInAt FROM gh.Appointments WITH(UPDLOCK,HOLDLOCK) WHERE AppointmentId=@A AND PatientId=@P AND DoctorId=@D",P("@A",appointmentId),P("@P",patientId),P("@D",doctorId));
                    if(appointment==null||appointment["CheckInAt"]==DBNull.Value||Array.IndexOf(new[]{"CheckedIn","InProgress"},Convert.ToString(appointment["Status"]))<0)throw new InvalidOperationException("Select this clinician's checked-in appointment for this patient. Reception must check the patient in before a linked consultation.");
                    if(Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM gh.Encounters WHERE AppointmentId=@A",P("@A",appointmentId)))>0)throw new InvalidOperationException("This appointment already has a consultation. Open its existing note.");
                    appointmentStatus=Convert.ToString(appointment["Status"]);
                }
                var encounterId = Convert.ToInt64(Scalar(connection, transaction, @"INSERT INTO gh.Encounters
                    (AppointmentId, PatientId, DoctorId, ChiefComplaint, Assessment, TreatmentPlan, FollowUpDate)
                    VALUES (@AppointmentId, @PatientId, @DoctorId, @Complaint, @Assessment, @Plan, @FollowUp);
                    SELECT CAST(SCOPE_IDENTITY() AS bigint);",
                    P("@AppointmentId", appointmentId), P("@PatientId", patientId), P("@DoctorId", doctorId),
                    P("@Complaint", complaint), P("@Assessment", assessment), P("@Plan", plan), P("@FollowUp", followUp)));

                if (temperature.HasValue || pulse.HasValue || systolic.HasValue || diastolic.HasValue)
                    Execute(connection, transaction, @"INSERT INTO gh.VitalSigns(EncounterId, PatientId, RecordedByUserId, TemperatureC, Pulse, SystolicBp, DiastolicBp)
                        VALUES(@EncounterId, @PatientId, @Actor, @Temperature, @Pulse, @Systolic, @Diastolic)", P("@EncounterId", encounterId),P("@PatientId",patientId),P("@Actor",actor),
                        P("@Temperature", temperature), P("@Pulse", pulse), P("@Systolic", systolic), P("@Diastolic", diastolic));
                if (!string.IsNullOrWhiteSpace(diagnosis))
                    Execute(connection, transaction, "INSERT INTO gh.Diagnoses(EncounterId, DiagnosisName, IsPrimary) VALUES(@EncounterId, @Diagnosis, 1)",
                        P("@EncounterId", encounterId), P("@Diagnosis", diagnosis));
                if (medicationId.HasValue)CreateClinicalOrder(connection,transaction,encounterId,doctorId,actor,"Prescription",medicationId.Value,dose,frequency,quantity.GetValueOrDefault());
                if (labTestId.HasValue)
                    CreateClinicalOrder(connection,transaction,encounterId,doctorId,actor,"Laboratory",labTestId.Value,null,null,0);
                if (appointmentId.HasValue)
                {
                    Execute(connection, transaction, "UPDATE gh.Appointments SET Status = 'Completed', UpdatedAt = SYSUTCDATETIME() WHERE AppointmentId = @AppointmentId", P("@AppointmentId", appointmentId));
                    Execute(connection,transaction,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,PreviousStatus,NewStatus,Detail) VALUES('Appointment',@A,@U,@Old,'Completed','Consultation recorded')",P("@A",appointmentId.Value),P("@U",actor),P("@Old",appointmentStatus));
                }
                History(connection,transaction,"Encounter",encounterId,actor,"Open","Consultation recorded");
                QueuePatientNotification(connection,transaction,patientId,"Consultation","Consultation recorded","Your clinician has recorded your consultation. You can view it in your medical record.");
                return encounterId;
            });
        }

        public void FinalizeLabResult(long itemId, string value, string unit, string flag, int userId)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 250 || (unit ?? "").Length > 40 || Array.IndexOf(new[]{"Normal","Low","High","Critical","Abnormal"},flag)<0)
                throw new ArgumentException("Enter a result, valid unit, and result flag.");
            database.InTransaction((connection, transaction) =>
            {
                RequireActor(connection,transaction,userId,"Laboratorian");
                var order = QueryOne(connection, transaction, @"SELECT o.LabOrderId,o.Status,o.OrderedByDoctorId,o.PatientId FROM gh.LabOrders o WITH(UPDLOCK,HOLDLOCK) JOIN gh.LabOrderItems i ON i.LabOrderId=o.LabOrderId WHERE i.LabOrderItemId=@Id",P("@Id",itemId));
                if(order==null || Convert.ToString(order["Status"])!="Processing") throw new InvalidOperationException("Collect and process the sample before finalizing its result.");
                var result = QueryOne(connection, transaction, @"UPDATE gh.LabOrderItems SET ResultValue = @Value, ResultUnit = @Unit,
                    ResultFlag = @Flag, ResultStatus = 'Final', ResultedAt = SYSUTCDATETIME(), ResultedByUserId = @UserId
                    OUTPUT INSERTED.LabOrderId
                    WHERE LabOrderItemId = @ItemId AND ResultStatus IN ('Pending','Processing')", P("@Value", value), P("@Unit", unit),
                    P("@Flag", flag), P("@UserId", userId), P("@ItemId", itemId));
                if (result == null) throw new InvalidOperationException("The laboratory order item was not found.");
                var orderId = Convert.ToInt64(result["LabOrderId"]);
                Execute(connection, transaction, @"UPDATE gh.LabOrders SET Status = CASE WHEN NOT EXISTS
                    (SELECT 1 FROM gh.LabOrderItems WHERE LabOrderId = @OrderId AND ResultStatus <> 'Final') THEN 'Completed' ELSE 'Processing' END, ResultVersion=ResultVersion+1
                    WHERE LabOrderId = @OrderId", P("@OrderId", orderId));
                var patientId = Convert.ToInt32(Scalar(connection, transaction, "SELECT PatientId FROM gh.LabOrders WHERE LabOrderId=@Id", P("@Id", orderId)));
                QueuePatientNotification(connection, transaction, patientId, "Laboratory", "Laboratory result available", "A laboratory result has been finalized and is now available in your portal.");
                Execute(connection,transaction,"INSERT INTO gh.Notifications(UserId,PatientId,NotificationType,Subject,Message,SentAt) SELECT UserId,@PatientId,'Laboratory',@Subject,@Message,SYSUTCDATETIME() FROM gh.UserAccounts WHERE RoleName='Doctor' AND LegacyUserId=@Doctor AND IsActive=1",P("@PatientId",patientId),P("@Doctor",order["OrderedByDoctorId"]),P("@Subject",flag=="Critical"?"Critical laboratory result — review required":"Laboratory result — "+flag),P("@Message","A finalized "+flag.ToLowerInvariant()+" result awaits your clinical review."));
                Execute(connection,transaction,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,NewStatus,Detail) VALUES('LabResult',@Id,@User,'Final',@Value)",P("@Id",itemId),P("@User",userId),P("@Value",value));
                return 0;
            });
        }

        public void ReceiveStock(int medicationId, string batchNumber, DateTime expires, decimal quantity, decimal minimum, decimal unitCost, int? supplierId, int actor = 0, string receiptReference = null)
        {
            Money(quantity);Money(minimum);Money(unitCost);
            if (quantity <= 0 || minimum < 0 || unitCost < 0 || expires.Date < DateTime.UtcNow.Date || string.IsNullOrWhiteSpace(batchNumber)||batchNumber.Length>80)
                throw new ArgumentException("Enter a valid batch, expiration date, positive quantity, and nonnegative cost and minimum.");
            batchNumber=batchNumber.Trim();
            if(receiptReference!=null){
                receiptReference=receiptReference.Trim();
                if(!supplierId.HasValue||receiptReference.Length<1||receiptReference.Length>80)throw new ArgumentException("Select a supplier and enter a unique delivery-line reference of up to 80 characters.");
            }
            database.InTransaction((connection, transaction) => {
            actor=RequireActor(connection,transaction,actor,"Pharmacist");
            if(Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM gh.MedicationCatalog WHERE MedicationId=@M AND IsActive=1",P("@M",medicationId)))!=1)throw new InvalidOperationException("Select an active medication.");
            if(supplierId.HasValue&&Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM gh.Suppliers WHERE SupplierId=@S AND IsActive=1",P("@S",supplierId)))!=1)throw new InvalidOperationException("Select an active supplier.");
            // Lock the unique supplier reference before the batch; a replay never adds stock.
            if(receiptReference!=null&&Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM gh.StockReceipts WITH(UPDLOCK,HOLDLOCK) WHERE SupplierId=@S AND ReceiptReference=@R",P("@S",supplierId),P("@R",receiptReference)))>0)throw new InvalidOperationException("This supplier delivery-line reference has already been received. Stock was not changed.");
            var existing=QueryOne(connection,transaction,"SELECT MedicationBatchId,SupplierId,ExpirationDate,QuantityReceived,QuantityOnHand FROM gh.MedicationBatches WITH(UPDLOCK,HOLDLOCK) WHERE MedicationId=@M AND BatchNumber=@B",P("@M",medicationId),P("@B",batchNumber));
            object batchId;
            if(existing!=null){
                if(receiptReference==null)throw new InvalidOperationException("This medication batch is already recorded. Use a supplier delivery-line reference to receive another delivery.");
                if(existing["SupplierId"]==DBNull.Value||Convert.ToInt32(existing["SupplierId"])!=supplierId.Value||Convert.ToDateTime(existing["ExpirationDate"]).Date!=expires.Date)throw new InvalidOperationException("The supplier and expiry must match the existing batch. Review the delivery before receiving it.");
                Money(Convert.ToDecimal(existing["QuantityReceived"])+quantity);Money(Convert.ToDecimal(existing["QuantityOnHand"])+quantity);
                batchId=existing["MedicationBatchId"];
                Execute(connection,transaction,"UPDATE gh.MedicationBatches SET QuantityReceived=QuantityReceived+@Q,QuantityOnHand=QuantityOnHand+@Q WHERE MedicationBatchId=@B",P("@Q",quantity),P("@B",batchId));
            }else{
            batchId = Scalar(connection, transaction, @"INSERT INTO gh.MedicationBatches(MedicationId, SupplierId, BatchNumber, ExpirationDate, QuantityReceived, QuantityOnHand, MinimumStock, UnitCost)
                VALUES(@MedicationId, @SupplierId, @BatchNumber, @Expires, @Quantity, @Quantity, @Minimum, @UnitCost); SELECT CAST(SCOPE_IDENTITY() AS bigint);",
                P("@MedicationId", medicationId), P("@SupplierId", supplierId), P("@BatchNumber", batchNumber), P("@Expires", expires),
                P("@Quantity", quantity), P("@Minimum", minimum), P("@UnitCost", unitCost));
            }
            var movement=Scalar(connection, transaction, "INSERT INTO gh.StockMovements(MedicationBatchId,MovementType,Quantity,RecordedByUserId,ReferenceType,ReferenceId) VALUES(@Id,'Received',@Qty,@U,@Type,@Reference);SELECT CAST(SCOPE_IDENTITY() AS bigint)", P("@Id", batchId), P("@Qty", quantity),P("@U",actor),P("@Type",receiptReference==null?null:"SupplierDelivery"),P("@Reference",receiptReference));
            if(receiptReference!=null){
                var receipt=Scalar(connection,transaction,"INSERT INTO gh.StockReceipts(SupplierId,ReceiptReference,MedicationBatchId,Quantity,UnitCost,StockMovementId,ReceivedByUserId) VALUES(@S,@R,@B,@Q,@Cost,@M,@U);SELECT CAST(SCOPE_IDENTITY() AS bigint)",P("@S",supplierId),P("@R",receiptReference),P("@B",batchId),P("@Q",quantity),P("@Cost",unitCost),P("@M",movement),P("@U",actor));
                History(connection,transaction,"SupplierDelivery",Convert.ToInt64(receipt),actor,"Received",receiptReference+"; batch "+batchNumber);
            }else History(connection,transaction,"StockReceipt",Convert.ToInt64(batchId),actor,"Received","Batch "+batchNumber);
            return 0;
            });
        }

        public void DispensePrescriptionItem(long prescriptionItemId, long medicationBatchId, decimal quantity, int dispensedByUserId)
        {
            Money(quantity);
            if (quantity <= 0) throw new InvalidOperationException("Dispense quantity must be greater than zero.");
            database.InTransaction((connection, transaction) =>
            {
                RequireActor(connection,transaction,dispensedByUserId,"Pharmacist");
                var item = QueryOne(connection, transaction, @"SELECT pi.PrescriptionOrderId,pi.MedicationId,pi.Quantity AS PrescribedQuantity,
                    po.PatientId,po.Status FROM gh.PrescriptionItems pi WITH(UPDLOCK,HOLDLOCK) JOIN gh.PrescriptionOrders po WITH(UPDLOCK,HOLDLOCK) ON po.PrescriptionOrderId=pi.PrescriptionOrderId
                    WHERE pi.PrescriptionItemId=@ItemId", P("@ItemId", prescriptionItemId));
                if (item == null || Convert.ToString(item["Status"]) == "Cancelled") throw new InvalidOperationException("The prescription item is not available for dispensing.");
                var batch = QueryOne(connection, transaction, @"SELECT MedicationId,QuantityOnHand,ExpirationDate FROM gh.MedicationBatches WITH (UPDLOCK,HOLDLOCK)
                    WHERE MedicationBatchId=@BatchId", P("@BatchId", medicationBatchId));
                if (batch == null || Convert.ToInt32(batch["MedicationId"]) != Convert.ToInt32(item["MedicationId"])) throw new InvalidOperationException("Select a stock batch for the prescribed medication.");
                if (Convert.ToDateTime(batch["ExpirationDate"]).Date < DateTime.UtcNow.Date) throw new InvalidOperationException("Expired medication cannot be dispensed.");
                if (Convert.ToDecimal(batch["QuantityOnHand"]) < quantity) throw new InvalidOperationException("The selected batch does not have enough stock.");
                var alreadyDispensed = Convert.ToDecimal(Scalar(connection, transaction, "SELECT ISNULL(SUM(Quantity),0) FROM gh.Dispensations WITH (UPDLOCK,HOLDLOCK) WHERE PrescriptionItemId=@ItemId", P("@ItemId", prescriptionItemId)));
                if (alreadyDispensed + quantity > Convert.ToDecimal(item["PrescribedQuantity"])) throw new InvalidOperationException("Dispense quantity exceeds the remaining prescribed quantity.");

                Execute(connection, transaction, "UPDATE gh.MedicationBatches SET QuantityOnHand=QuantityOnHand-@Quantity WHERE MedicationBatchId=@BatchId", P("@Quantity", quantity), P("@BatchId", medicationBatchId));
                Execute(connection, transaction, "INSERT INTO gh.Dispensations(PrescriptionItemId,MedicationBatchId,Quantity,DispensedByUserId) VALUES(@ItemId,@BatchId,@Quantity,@UserId)",
                    P("@ItemId", prescriptionItemId), P("@BatchId", medicationBatchId), P("@Quantity", quantity), P("@UserId", dispensedByUserId));
                Execute(connection, transaction, "INSERT INTO gh.StockMovements(MedicationBatchId,MovementType,Quantity,ReferenceType,ReferenceId,RecordedByUserId) VALUES(@BatchId,'Dispensed',-@Quantity,'PrescriptionItem',@ItemId,@UserId)",
                    P("@BatchId", medicationBatchId), P("@Quantity", quantity), P("@ItemId", prescriptionItemId), P("@UserId", dispensedByUserId));
                var orderId = Convert.ToInt64(item["PrescriptionOrderId"]);
                var complete = Convert.ToInt32(Scalar(connection, transaction, @"SELECT COUNT(*) FROM gh.PrescriptionItems pi WHERE pi.PrescriptionOrderId=@OrderId
                    AND pi.Quantity > ISNULL((SELECT SUM(d.Quantity) FROM gh.Dispensations d WHERE d.PrescriptionItemId=pi.PrescriptionItemId),0)", P("@OrderId", orderId))) == 0;
                Execute(connection, transaction, "UPDATE gh.PrescriptionOrders SET Status=@Status, DispensedAt=CASE WHEN @Complete=1 THEN SYSUTCDATETIME() ELSE DispensedAt END WHERE PrescriptionOrderId=@OrderId",
                    P("@Status", complete ? "Dispensed" : "PartiallyDispensed"), P("@Complete", complete), P("@OrderId", orderId));
                Execute(connection,transaction,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,PreviousStatus,NewStatus,Detail) VALUES('PrescriptionOrder',@O,@U,@Old,@New,@D)",P("@O",orderId),P("@U",dispensedByUserId),P("@Old",item["Status"]),P("@New",complete?"Dispensed":"PartiallyDispensed"),P("@D","Dispensed "+quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)+" from batch "+medicationBatchId));
                QueuePatientNotification(connection, transaction, Convert.ToInt32(item["PatientId"]), "Prescription", "Prescription updated", "A prescribed medication has been dispensed.");
                return 0;
            });
        }

        public long CreateInvoice(int patientId, string description, decimal amount, DateTime? dueDate, int actor = 0)
        {
            if(amount<=0 || string.IsNullOrWhiteSpace(description))throw new ArgumentException("Enter a description and positive invoice amount.");
            return database.InTransaction((connection, transaction) =>
            {
                actor=RequireActor(connection,transaction,actor,"Receptionist","Admin");
                Money(amount);
                var number = "INV-" + Guid.NewGuid().ToString("N").Substring(0,26);
                var invoiceId = Convert.ToInt64(Scalar(connection, transaction, @"INSERT INTO gh.Invoices(InvoiceNumber, PatientId, Status, TotalAmount, DueDate)
                    VALUES(@Number, @PatientId, 'Open', @Amount, @DueDate); SELECT CAST(SCOPE_IDENTITY() AS bigint);",
                    P("@Number", number), P("@PatientId", patientId), P("@Amount", amount), P("@DueDate", dueDate)));
                Execute(connection, transaction, "INSERT INTO gh.InvoiceItems(InvoiceId, ItemType, Description, Quantity, UnitPrice) VALUES(@InvoiceId, 'Service', @Description, 1, @Amount)",
                    P("@InvoiceId", invoiceId), P("@Description", description), P("@Amount", amount));
                QueuePatientNotification(connection, transaction, patientId, "Billing", "New invoice", "A new invoice has been added to your account.");
                History(connection,transaction,"Invoice",invoiceId,actor,"Open","Invoice created");
                return invoiceId;
            });
        }

        public void TakePayment(long invoiceId, decimal amount, string method, string reference, int userId)
        {
            Money(amount);
            if(!new[]{"Cash","Card","Transfer"}.ContainsValue(method))throw new ArgumentException("Select cash, card or transfer. Insurance payments must settle an approved claim.");
            if((reference??"").Length>80)throw new ArgumentException("Keep the payment reference within 80 characters.");
            reference=(reference??"").Trim();
            if(method!="Cash"&&reference.Length==0)throw new ArgumentException("Card and transfer payments require the received transaction reference.");
            database.InTransaction((connection, transaction) =>
            {
                RequireActor(connection,transaction,userId,"Receptionist","Admin");
                var totals = QueryOne(connection, transaction, "SELECT PatientId, TotalAmount, PaidAmount FROM gh.Invoices WITH (UPDLOCK, HOLDLOCK) WHERE InvoiceId = @InvoiceId AND Status IN ('Open','PartiallyPaid')", P("@InvoiceId", invoiceId));
                if (totals == null) throw new InvalidOperationException("Invoice was not found.");
                if(reference.Length>0&&Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM gh.Payments WHERE InvoiceId=@I AND Method=@M AND ReferenceNumber=@R",P("@I",invoiceId),P("@M",method),P("@R",reference)))>0)throw new InvalidOperationException("This payment reference is already recorded on the invoice. Review payment history before recording another receipt.");
                var remaining = Convert.ToDecimal(totals["TotalAmount"]) - Convert.ToDecimal(totals["PaidAmount"]);
                var reserved=Convert.ToDecimal(Scalar(connection,transaction,"SELECT ISNULL(SUM(CASE WHEN Status='Approved' THEN ApprovedAmount ELSE ClaimedAmount END),0) FROM gh.InsuranceClaims WITH(UPDLOCK,HOLDLOCK) WHERE InvoiceId=@I AND Status IN('Submitted','UnderReview','Approved')",P("@I",invoiceId)));
                if(amount>remaining-reserved)throw new InvalidOperationException("Payment exceeds the patient portion. An unsettled insurance claim reserves part of this balance.");
                if (amount <= 0 || amount > remaining) throw new InvalidOperationException("Payment must be greater than zero and no more than the outstanding balance.");
                Execute(connection, transaction, "INSERT INTO gh.Payments(InvoiceId, Amount, Method, ReferenceNumber, ReceivedByUserId) VALUES(@InvoiceId, @Amount, @Method, @Reference, @UserId)",
                    P("@InvoiceId", invoiceId), P("@Amount", amount), P("@Method", method), P("@Reference", reference), P("@UserId", userId));
                Execute(connection, transaction, @"UPDATE gh.Invoices SET PaidAmount = PaidAmount + @Amount, Status = CASE WHEN PaidAmount + @Amount >= TotalAmount THEN 'Paid' ELSE 'PartiallyPaid' END
                    WHERE InvoiceId = @InvoiceId", P("@Amount", amount), P("@InvoiceId", invoiceId));
                QueuePatientNotification(connection, transaction, Convert.ToInt32(totals["PatientId"]), "Billing", "Payment received", "Your payment has been recorded against an invoice.");
                History(connection,transaction,"Invoice",invoiceId,userId,"PaymentReceived",method+" payment: "+amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return 0;
            });
        }

        public long RecordTriage(int patientId, int acuityLevel, string complaint, int userId)
        {
            if (acuityLevel < 1 || acuityLevel > 5) throw new ArgumentException("Triage acuity must be between 1 and 5.");
            if (string.IsNullOrWhiteSpace(complaint) || complaint.Length > 1000) throw new ArgumentException("Enter a chief complaint of up to 1,000 characters.");
            return database.InTransaction((connection,transaction)=>{
            RequireActor(connection,transaction,userId,"Nurse","Doctor");
            var id = Scalar(connection,transaction,@"INSERT INTO gh.TriageAssessments(PatientId,AssessedByUserId,AcuityLevel,ChiefComplaint)
                SELECT @PatientId,@UserId,@Acuity,@Complaint WHERE EXISTS(SELECT 1 FROM dbo.patientTbl WHERE patientId=@PatientId AND IsActive=1);
                SELECT CAST(SCOPE_IDENTITY() AS bigint);", P("@PatientId", patientId), P("@UserId", userId), P("@Acuity", acuityLevel), P("@Complaint", complaint));
            if (id == null || id == DBNull.Value) throw new InvalidOperationException("The active patient record was not found.");
            History(connection,transaction,"Triage",Convert.ToInt64(id),userId,"Waiting","Acuity "+acuityLevel);
            return Convert.ToInt64(id);
            });
        }

        public int AddWard(string name, string type, int? departmentId, int actor = 0)
        {
            return AdminInsert("Ward",actor,"INSERT INTO gh.Wards(DepartmentId,WardName,WardType) VALUES(@DepartmentId,@Name,@Type); SELECT CAST(SCOPE_IDENTITY() AS int);", P("@DepartmentId", departmentId), P("@Name", name), P("@Type", type));
        }

        public int AddRoom(int wardId, string number, string type, int actor = 0)
        {
            return AdminInsert("Room",actor,"INSERT INTO gh.Rooms(WardId,RoomNumber,RoomType) VALUES(@WardId,@Number,@Type); SELECT CAST(SCOPE_IDENTITY() AS int);", P("@WardId", wardId), P("@Number", number), P("@Type", type));
        }

        public int AddBed(int roomId, string number, int actor = 0)
        {
            return AdminInsert("Bed",actor,"INSERT INTO gh.Beds(RoomId,BedNumber) VALUES(@RoomId,@Number); SELECT CAST(SCOPE_IDENTITY() AS int);", P("@RoomId", roomId), P("@Number", number));
        }

        public long RecordNursingNote(int patientId, long? admissionId, int userId, string careStatus, string note,
            decimal? temperature, int? pulse, int? systolic, int? diastolic)
        {
            if (string.IsNullOrWhiteSpace(note)) throw new ArgumentException("Enter a nursing note.");
            if(note.Length>2000) throw new ArgumentException("Keep the note within 2,000 characters.");
            if(temperature<25 || temperature>45 || pulse<1 || pulse>300 || systolic<1 || systolic>350 || diastolic<1 || diastolic>250) throw new ArgumentException("Check the entered vital signs.");
            if (careStatus != "Stable" && careStatus != "NeedsReview" && careStatus != "Escalated" && careStatus != "DischargeReady") throw new ArgumentException("Select a valid care status.");
            return database.InTransaction((connection, transaction) =>
            {
                RequireActor(connection,transaction,userId,"Nurse");
                var active = Convert.ToInt32(Scalar(connection, transaction, "SELECT COUNT(*) FROM dbo.patientTbl WHERE patientId=@PatientId AND IsActive=1", P("@PatientId", patientId)));
                if (active != 1) throw new InvalidOperationException("The active patient record was not found.");
                if(Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM gh.Admissions WHERE PatientId=@P AND DischargedAt IS NULL AND Status IN('Admitted','Transferred')",P("@P",patientId)))==0) throw new InvalidOperationException("Select a currently admitted patient.");
                if (admissionId.HasValue && Convert.ToInt32(Scalar(connection, transaction, "SELECT COUNT(*) FROM gh.Admissions WHERE AdmissionId=@AdmissionId AND PatientId=@PatientId AND DischargedAt IS NULL", P("@AdmissionId", admissionId), P("@PatientId", patientId))) != 1) throw new InvalidOperationException("The active admission was not found for this patient.");
                var noteId = Convert.ToInt64(Scalar(connection, transaction, @"INSERT INTO gh.NursingNotes(PatientId,AdmissionId,RecordedByUserId,CareStatus,Note)
                    VALUES(@PatientId,@AdmissionId,@UserId,@CareStatus,@Note); SELECT CAST(SCOPE_IDENTITY() AS bigint);", P("@PatientId", patientId), P("@AdmissionId", admissionId), P("@UserId", userId), P("@CareStatus", careStatus), P("@Note", note)));
                if (temperature.HasValue || pulse.HasValue || systolic.HasValue || diastolic.HasValue)
                    Execute(connection, transaction, @"INSERT INTO gh.VitalSigns(PatientId,RecordedByUserId,TemperatureC,Pulse,SystolicBp,DiastolicBp)
                        VALUES(@PatientId,@UserId,@Temperature,@Pulse,@Systolic,@Diastolic)",
                        P("@UserId", userId), P("@Temperature", temperature), P("@Pulse", pulse), P("@Systolic", systolic), P("@Diastolic", diastolic), P("@PatientId", patientId));
                if (careStatus == "Escalated") QueuePatientNotification(connection, transaction, patientId, "Care", "Care team update", "Your care team has recorded an update to your current care status.");
                if(careStatus=="Escalated"||careStatus=="NeedsReview")Execute(connection,transaction,@"INSERT INTO gh.Notifications(UserId,PatientId,NotificationType,Subject,Message,SentAt) SELECT u.UserId,@P,'Care',@S,'A nursing assessment requires clinical review.',SYSUTCDATETIME() FROM gh.UserAccounts u WHERE u.RoleName='Doctor' AND u.IsActive=1 AND EXISTS(SELECT 1 FROM gh.Admissions a WHERE a.PatientId=@P AND a.DischargedAt IS NULL AND a.AttendingDoctorId=u.LegacyUserId)",P("@P",patientId),P("@S","Nursing assessment: "+careStatus));
                History(connection,transaction,"NursingNote",noteId,userId,careStatus,"Assessment recorded");
                return noteId;
            });
        }

        public long AdmitPatient(int patientId, int bedId, int? attendingDoctorId, string reason, int actor = 0,long? triageAssessmentId=null)
        {
            if(string.IsNullOrWhiteSpace(reason))throw new ArgumentException("Enter an admission reason.");
            return database.InTransaction((connection, transaction) =>
            {
                actor=RequireActor(connection,transaction,actor,"Admin");
                if(Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM dbo.patientTbl WHERE patientId=@P AND IsActive=1",P("@P",patientId)))!=1)throw new InvalidOperationException("Select an active patient for admission.");
                if(triageAssessmentId.HasValue){
                    var assessment=QueryOne(connection,transaction,"SELECT Status FROM gh.TriageAssessments WITH(UPDLOCK,HOLDLOCK) WHERE TriageAssessmentId=@T AND PatientId=@P",P("@T",triageAssessmentId),P("@P",patientId));
                    if(assessment==null||Convert.ToString(assessment["Status"])!="UnderCare")throw new InvalidOperationException("Link an emergency assessment for this patient that is currently under care.");
                    if(Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM gh.Admissions WHERE TriageAssessmentId=@T",P("@T",triageAssessmentId)))>0)throw new InvalidOperationException("This emergency assessment already has an admission.");
                }
                var bed = QueryOne(connection, transaction, "SELECT b.BedId,b.Status FROM gh.Beds b WITH (UPDLOCK,HOLDLOCK) JOIN gh.Rooms r ON r.RoomId=b.RoomId JOIN gh.Wards w ON w.WardId=r.WardId WHERE b.BedId=@BedId AND r.IsActive=1 AND w.IsActive=1", P("@BedId", bedId));
                if(attendingDoctorId.HasValue&&Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM dbo.doctorTbl WHERE doctorID=@D AND doctorStatus='Active'",P("@D",attendingDoctorId)))!=1)throw new InvalidOperationException("Select an active attending clinician.");
                if (bed == null || !string.Equals(Convert.ToString(bed["Status"]), "Available", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The selected bed is not available.");
                var existing = Convert.ToInt32(Scalar(connection, transaction, "SELECT COUNT(*) FROM gh.Admissions WITH (UPDLOCK,HOLDLOCK) WHERE PatientId=@PatientId AND DischargedAt IS NULL AND Status IN ('Admitted','Transferred')", P("@PatientId", patientId)));
                if (existing > 0) throw new InvalidOperationException("This patient already has an active admission.");
                var admissionId = Convert.ToInt64(Scalar(connection, transaction, @"INSERT INTO gh.Admissions(PatientId,BedId,AttendingDoctorId,AdmissionReason,TriageAssessmentId)
                    VALUES(@PatientId,@BedId,@DoctorId,@Reason,@Triage);
                    SELECT CAST(SCOPE_IDENTITY() AS bigint);", P("@PatientId", patientId), P("@BedId", bedId), P("@DoctorId", attendingDoctorId), P("@Reason", reason),P("@Triage",triageAssessmentId)));
                if (admissionId < 1) throw new InvalidOperationException("The active patient record was not found.");
                Execute(connection, transaction, "UPDATE gh.Beds SET Status='Occupied' WHERE BedId=@BedId", P("@BedId", bedId));
                QueuePatientNotification(connection, transaction, patientId, "Admission", "Hospital admission", "You have been admitted to Guardian Health.");
                History(connection,transaction,"Admission",admissionId,actor,"Admitted",triageAssessmentId.HasValue?"Linked emergency assessment "+triageAssessmentId.Value+"; "+reason:reason);
                Execute(connection,transaction,"INSERT INTO gh.Notifications(UserId,PatientId,NotificationType,Subject,Message,SentAt) SELECT UserId,@P,'Admission','New inpatient admission','A patient has been assigned to a bed. Review the admitted-patient worklist for care and handoff.',SYSUTCDATETIME() FROM gh.UserAccounts WHERE IsActive=1 AND (RoleName='Nurse' OR (RoleName='Doctor' AND LegacyUserId=@D))",P("@P",patientId),P("@D",attendingDoctorId));
                return admissionId;
            });
        }

        public void DischargePatient(long admissionId, string summary, int actor = 0)
        {
            if(string.IsNullOrWhiteSpace(summary))throw new ArgumentException("Enter a discharge summary.");
            database.InTransaction((connection, transaction) =>
            {
                actor=RequireActor(connection,transaction,actor,"Admin");
                var admission = QueryOne(connection, transaction, "SELECT PatientId,BedId FROM gh.Admissions WITH (UPDLOCK,HOLDLOCK) WHERE AdmissionId=@AdmissionId AND DischargedAt IS NULL AND Status IN ('Admitted','Transferred')", P("@AdmissionId", admissionId));
                if (admission == null) throw new InvalidOperationException("An active admission was not found.");
                Execute(connection, transaction, "UPDATE gh.Admissions SET Status='Discharged',DischargedAt=SYSUTCDATETIME(),DischargeSummary=@Summary WHERE AdmissionId=@AdmissionId", P("@Summary", summary), P("@AdmissionId", admissionId));
                if (admission["BedId"] != DBNull.Value) Execute(connection, transaction, "UPDATE gh.Beds SET Status='Cleaning' WHERE BedId=@BedId", P("@BedId", admission["BedId"]));
                QueuePatientNotification(connection, transaction, Convert.ToInt32(admission["PatientId"]), "Admission", "Discharge recorded", "Your discharge has been recorded. Please follow your care team's instructions.");
                History(connection,transaction,"Admission",admissionId,actor,"Discharged","Discharge summary recorded; bed awaits cleaning");
                return 0;
            });
        }

        public int AddDepartment(string code, string name, string location, string phone, int actor = 0)
        {
            return AdminInsert("Department",actor,@"INSERT INTO gh.Departments(DepartmentCode, DepartmentName, Location, Phone)
                VALUES(@Code, @Name, @Location, @Phone); SELECT CAST(SCOPE_IDENTITY() AS int);", P("@Code", code), P("@Name", name), P("@Location", location), P("@Phone", phone));
        }

        public int CreateStaffAccount(string staffNumber, string role, string name, string email, string phone, int? departmentId, string password, int actor = 0)
        {
            staffNumber=(staffNumber??"").Trim();name=(name??"").Trim();email=(email??"").Trim().ToLowerInvariant();phone=(phone??"").Trim();
            ValidateStaffContact(name,email,phone);
            if(staffNumber.Length<1||staffNumber.Length>30)throw new ArgumentException("Enter a staff number of up to 30 characters.");
            string error;if(!PasswordSecurity.IsAcceptable(password,out error))throw new ArgumentException(error);
            if(!new[]{"Nurse","Pharmacist"}.ContainsValue(role))throw new ArgumentException("Use the dedicated directory for this staff role.");
            return database.InTransaction((connection, transaction) =>
            {
                actor=RequireActor(connection,transaction,actor,"Admin");
                RequireStaffDepartment(connection,transaction,departmentId);
                RequireAvailableStaffEmail(connection,transaction,email);
                var staffId = Convert.ToInt32(Scalar(connection, transaction, @"INSERT INTO gh.StaffMembers
                    (StaffNumber, DepartmentId, RoleName, FullName, Email, Phone) VALUES(@Number, @DepartmentId, @Role, @Name, @Email, @Phone);
                    SELECT CAST(SCOPE_IDENTITY() AS int);", P("@Number", staffNumber), P("@DepartmentId", departmentId), P("@Role", role),
                    P("@Name", name), P("@Email", email), P("@Phone", phone)));
                var userId = Convert.ToInt32(Scalar(connection, transaction, @"INSERT INTO gh.UserAccounts
                    (StaffMemberId, RoleName, DisplayName, Email, NormalizedEmail, PasswordHash) VALUES(@StaffId, @Role, @Name, @Email, @NormalizedEmail, @PasswordHash);
                    SELECT CAST(SCOPE_IDENTITY() AS int);", P("@StaffId", staffId), P("@Role", role), P("@Name", name), P("@Email", email),
                    P("@NormalizedEmail", email.ToUpperInvariant()), P("@PasswordHash", PasswordSecurity.Hash(password))));
                History(connection,transaction,"UserAccount",userId,actor,"Created","Staff account created");
                return userId;
            });
        }

        public int CreatePatientAccount(int patientId, string email, string password, int actor = 0)
        {
            string passwordError;
            if (!PasswordSecurity.IsAcceptable(password,out passwordError)) throw new ArgumentException(passwordError);
            email=(email??"").Trim();
            try { if(new System.Net.Mail.MailAddress(email).Address!=email || email.Length>256) throw new FormatException(); }
            catch(FormatException) { throw new ArgumentException("Enter a valid email address."); }
            return database.InTransaction((connection, transaction) =>
            {
                actor=RequireActor(connection,transaction,actor,"Receptionist","Admin");
                var name = Convert.ToString(Scalar(connection, transaction, "SELECT patientName FROM dbo.patientTbl WHERE patientId=@PatientId AND IsActive=1", P("@PatientId", patientId)));
                if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("The patient record is not active.");
                if(Convert.ToInt32(Scalar(connection,transaction,"SELECT COUNT(*) FROM gh.UserAccounts WITH(UPDLOCK,HOLDLOCK) WHERE RoleName='Patient' AND (PatientId=@P OR NormalizedEmail=@E)",P("@P",patientId),P("@E",email.ToUpperInvariant())))>0)
                    throw new InvalidOperationException("A patient portal account already exists for this patient or email. Use password recovery or account administration.");
                var userId = Convert.ToInt32(Scalar(connection, transaction, @"INSERT INTO gh.UserAccounts
                    (PatientId, RoleName, DisplayName, Email, NormalizedEmail, PasswordHash) VALUES(@PatientId, 'Patient', @Name, @Email, @NormalizedEmail, @PasswordHash);
                    SELECT CAST(SCOPE_IDENTITY() AS int);", P("@PatientId", patientId), P("@Name", name), P("@Email", email),
                    P("@NormalizedEmail", email.ToUpperInvariant()), P("@PasswordHash", PasswordSecurity.Hash(password))));
                Execute(connection, transaction, "UPDATE gh.PatientProfiles SET Email=@Email, UpdatedAt=SYSUTCDATETIME() WHERE PatientId=@PatientId; IF @@ROWCOUNT=0 INSERT INTO gh.PatientProfiles(PatientId,Email) VALUES(@PatientId,@Email);", P("@Email", email), P("@PatientId", patientId));
                History(connection,transaction,"UserAccount",userId,actor,"Created","Patient portal access created");
                return userId;
            });
        }

        /// <summary>Creates or maintains the secure account paired with a legacy role-table record.</summary>
        // Compatibility entry point: never synchronize unverified directory details independently.
        public void SynchronizeLegacyAccount(string role,int legacyUserId,string name,string email,string password,bool isActive,int actor=0)
        {
            var map=DirectoryMap(role);
            database.InTransaction((c,t)=>{
                actor=RequireActor(c,t,actor,"Admin");
                var record=QueryOne(c,t,"SELECT "+map.Name+" AS Name,"+map.Email+" AS Email,"+map.Status+" AS Status,"+map.Password+" AS PasswordHash FROM "+map.Table+" WITH(UPDLOCK,HOLDLOCK) WHERE "+map.Id+"=@I",P("@I",legacyUserId));
                if(record==null||Convert.ToString(record["Name"])!=name||!string.Equals(Convert.ToString(record["Email"]),email,StringComparison.OrdinalIgnoreCase)||(Convert.ToString(record["Status"])=="Active")!=isActive)
                    throw new InvalidOperationException("Save directory changes and account access together through staff management.");
                var currentEmail=Scalar(c,t,"SELECT Email FROM gh.UserAccounts WITH(UPDLOCK,HOLDLOCK) WHERE RoleName=@R AND LegacyUserId=@I",P("@R",role),P("@I",legacyUserId));
                if(currentEmail!=null&&!string.Equals(Convert.ToString(currentEmail),email,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Confirm a sign-in email change through staff management; synchronization cannot rename an account silently.");
                string hash=null,error;
                if(!string.IsNullOrEmpty(password)){
                    if(!PasswordSecurity.IsAcceptable(password,out error))throw new ArgumentException(error);
                    hash=PasswordSecurity.Hash(password);
                    Execute(c,t,"UPDATE "+map.Table+" SET "+map.Password+"=@H WHERE "+map.Id+"=@I",P("@H",hash),P("@I",legacyUserId));
                }
                SynchronizeDirectoryAccount(c,t,role,legacyUserId,name,email,hash,isActive,Convert.ToString(record["PasswordHash"]));
                History(c,t,role+"Directory",legacyUserId,actor,"AccessSynchronized","Directory and sign-in identity reconciled");
                return 0;
            });
        }

        private static SqlParameter P(string name, object value) { return Database.Parameter(name, value); }
        private int AdminInsert(string entity,int actor,string sql,params SqlParameter[] parameters)
        {
            return database.InTransaction((c,t)=>{actor=RequireActor(c,t,actor,"Admin");var id=Convert.ToInt32(Scalar(c,t,sql,parameters));History(c,t,entity,id,actor,"Created",null);return id;});
        }
        // Web callers must use their authenticated identity; non-HTTP jobs must supply an explicit actor.
        private static int RequireActor(SqlConnection c,SqlTransaction t,int actor,params string[] roles)
        {
            var context=System.Web.HttpContext.Current;
            if(context!=null&&context.Session!=null){
                int signedIn;
                if(!int.TryParse(Convert.ToString(context.Session["accountId"]),out signedIn)||signedIn<1||(actor!=0&&actor!=signedIn))throw new InvalidOperationException("Your authenticated account cannot perform this action.");
                actor=signedIn;
            }
            var role=Convert.ToString(Scalar(c,t,"SELECT RoleName FROM gh.UserAccounts WHERE UserId=@U AND IsActive=1",P("@U",actor)));
            if(actor<1||Array.IndexOf(roles,role)<0)throw new InvalidOperationException("Your account is not authorized for this workflow.");
            return actor;
        }
        private static int RequireDoctor(SqlConnection c,SqlTransaction t,int actor,int doctor)
        {
            actor=RequireActor(c,t,actor,"Doctor");
            if(Convert.ToInt32(Scalar(c,t,"SELECT COUNT(*) FROM gh.UserAccounts WHERE UserId=@U AND LegacyUserId=@D",P("@U",actor),P("@D",doctor)))!=1)throw new InvalidOperationException("The clinician must match your authenticated account.");
            return actor;
        }
        private static void History(SqlConnection c,SqlTransaction t,string entity,long id,int actor,string status,string detail)
        {
            Execute(c,t,"INSERT INTO gh.WorkflowHistory(EntityType,EntityId,ActorUserId,NewStatus,Detail) VALUES(@E,@I,@U,@S,@D)",P("@E",entity),P("@I",id),P("@U",actor),P("@S",status),P("@D",detail));
        }
        private static void Money(decimal amount)
        {
            if(amount<0||amount>9999999999.99m||decimal.Round(amount,2)!=amount)throw new ArgumentException("Enter an amount with no more than two decimal places.");
        }
        private static void QueuePatientNotification(SqlConnection connection, SqlTransaction transaction, int patientId, string type, string subject, string message)
        {
            Execute(connection, transaction, @"INSERT INTO gh.Notifications(UserId,PatientId,NotificationType,Subject,Message,ScheduledFor,SentAt)
                SELECT ua.UserId,@PatientId,@Type,@Subject,@Message,SYSUTCDATETIME(),SYSUTCDATETIME()
                FROM gh.UserAccounts ua WHERE ua.PatientId=@PatientId AND ua.RoleName='Patient' AND ua.IsActive=1",
                P("@PatientId", patientId), P("@Type", type), P("@Subject", subject), P("@Message", message));
        }
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
