# Guardian Health synthetic development demonstration

This dataset is **SYNTHETIC DEVELOPMENT DATA**. Names, clinical text, medication, results and payments are training examples, not real patients, clinical guidance, hospital claims or actual financial transactions. Existing development accounts and records are not repurposed.

## Current installed state — 2026-10-16

**Already populated; do not populate again.** The existing `guardian-synthetic-v1` run is `Ready`, anchored to 2026-10-15. Its initial manifest contains 271 records, including 11 new synthetic accounts, three doctors and four patients. The seven original development accounts were not replaced. The manifest describes the initial setup, not a live count of everything subsequently created through the application.

Intentional synthetic browser activity is retained for manual review: a patient-created appointment and its consultation/lab/dispensing journey; a separate emergency admission, nursing assessment, transfer and discharge; and a nursing assessment on the retained current inpatient. These actions have ordinary workflow history and notifications. An additional appointment already present before the browser QA was preserved. None of these records should be deleted as accidental regression-test residue.

### People and availability

| Synthetic clinician | Specialty | Department | Published future sessions at setup |
| --- | --- | --- | --- |
| Synthetic Dr Maya Reed | General Medicine | Synthetic General Medicine | 2026-10-16 through 2026-10-22, 09:00–12:00 UTC, 20-minute slots |
| Synthetic Dr Omar Lane | Cardiology | Synthetic Cardiology | Same dates and slot duration |
| Synthetic Dr Lina Park | Pediatrics | Synthetic Pediatrics | Same dates and slot duration |

There are 21 daily sessions plus one valid same-day session created on October 15: **22 synthetic schedule records**. Availability is time-dependent: past times and occupied slots are not offered. These dates are not automatically refreshed. After October 22, an Admin must explicitly publish further sessions through Hospital operations; do not rerun setup or backdate attendance.

| Patient | Portal identifier | Representative record to inspect |
| --- | --- | --- |
| Synthetic Alex Brook | `synthetic.patient1@guardianhealth.test` | Completed outpatient browser journey; signed consultation, final lab result, dispensed medicine, paid training invoice and upcoming appointment |
| Synthetic Sam River | `synthetic.patient2@guardianhealth.test` | Open consultation, pending lab/prescription and claim under review |
| Synthetic Jamie Vale | `synthetic.patient3@guardianhealth.test` | Resolved emergency episodes, transferred/discharged admissions, nursing notes and submitted claim |
| Synthetic Robin Lake | `synthetic.patient4@guardianhealth.test` | Active admission in Synthetic Observation Ward / SYN-101 / SYN-C; nursing handoff and assessment |

## Explicit setup

The instructions below describe the existing tool for a reviewed, unpopulated development target. They are **not** an instruction to recreate this already installed dataset.

1. Build Debug, run migrations through `021_Synthetic_Demo_Provenance.sql`, and stop interactive database use while setup/tests run. Do not run setup alongside regression fixtures.
2. In a local PowerShell session, supply the existing development database via `HOSPITALMS_CONNECTIONSTRING` using your private configuration. No connection value belongs in source control.
3. Explicitly set `HOSPITALMS_ENVIRONMENT` to `Development` and `HOSPITALMS_DEMO_OPT_IN` to `SYNTHETIC_DEVELOPMENT_ONLY`. `Web.config` must also identify Development when using the script.
4. Run `./Tools/Setup-SyntheticDemo.ps1 -Action preview`. This reports intent without creating data.
5. Supply a strong password privately through `HOSPITALMS_DEMO_PASSWORD`. It applies only to newly created synthetic accounts. Do not put it in this document, command transcripts you intend to publish, or source files.
6. Run `./Tools/Setup-SyntheticDemo.ps1 -Action seed`. Remove the password environment variable when finished. Redirect diagnostics only outside the web application's directory.

The console tool is not part of application startup or the website assembly. It requires both explicit flags and restricts the target to the current local SQL Express development database with integrated authentication. Production/missing flags fail closed. The script additionally checks the web application's Development setting. These controls are safeguards, not a substitute for excluding development tools/data from production.

## Accounts and scenarios

Use the password supplied to setup; this document intentionally contains none.

| Account identifier | Demonstration role |
| --- | --- |
| `synthetic.patient1@guardianhealth.test` | Completed walk-in clinical/lab/pharmacy journey, settled invoice, future appointment; doctor discovery and new booking |
| `synthetic.patient2@guardianhealth.test` | Open clinical note, pending laboratory/prescription, claim under review |
| `synthetic.patient3@guardianhealth.test` | Emergency-to-inpatient transfer/discharge history and submitted claim |
| `synthetic.patient4@guardianhealth.test` | Current inpatient nursing handoff; same-day checked-in appointment when time permits |
| `synthetic.doctor1@guardianhealth.test` | General Medicine, completed care and current inpatient |
| `synthetic.doctor2@guardianhealth.test` | Cardiology, pending laboratory and prescription |
| `synthetic.doctor3@guardianhealth.test` | Pediatrics directory/schedule example; no invented professional achievements |
| `synthetic.reception@guardianhealth.test` | Registration, appointment lifecycle and finance |
| `synthetic.lab@guardianhealth.test` | Pending sample/result workflow |
| `synthetic.nurse@guardianhealth.test` | Current inpatient assessment and emergency handoff |
| `synthetic.pharmacist@guardianhealth.test` | Pending dispensing, supplier receipts, low/near-expiry training stock |

Use the preserved existing Admin account for management. No duplicate Admin or replacement development credentials are created.

The dataset creates three explicitly synthetic departments, one ward/room with three beds, seven future days of 20-minute published sessions per doctor, four patients with service-generated unique MRNs, catalog items, supplier receipts, consultations, laboratory results, prescriptions, nursing assessments, admission/transfer/discharge histories, invoices/items, recorded payments, insurance policies/claims and normal workflow notifications/history. A valid same-day session is added only if it fits before midnight UTC; otherwise attended/checked-in appointments are omitted rather than backdated. Walk-in clinical history remains available.

## Medical team and booking

1. Sign in as a synthetic Patient and select **Find a doctor**.
2. Search by name, select a specialty/department, optionally select an availability date.
3. Open a doctor profile. The directory displays stored professional fields only, with availability derived from schedules and occupied slots.
4. Choose a date, expand a real available time, optionally enter a reason, and select **Confirm appointment**.
5. Read the confirmation/reference, then open **My appointments**. Existing cancellation/rescheduling rules still apply. All displayed clinical times are UTC.
6. Reception can find the same persisted appointment. The doctor's schedule shows it according to the existing date/status filters; consultation still requires reception check-in on the appointment day.

Admin manages name/specialty/status in **Doctor directory**, department assignment in **Clinical operations → Medical team departments**, and date/time/slot length in **Publish clinician availability**. These operations do not silently replace passwords or account identities.

## Idempotence, preservation and cleanup

- `gh.SyntheticDemoRuns` stores a fixed dataset version and date anchor; `gh.SyntheticDemoRecords` stores exact table/key provenance and fingerprints. Setup is transactional and verifies that every pre-existing captured record is unchanged before committing.
- Re-running `seed` is a no-op even if an account was renamed, locked or disabled. It never refreshes dates or overwrites credentials. Publish new sessions through Admin when the original seven-day window expires.
- `cleanup-preview` checks exact manifest ownership and fingerprints without deleting rows. `cleanup` removes only the original, unchanged manifest-owned records in foreign-key order, transactionally. The retained tombstone prevents accidental resurrection.
- **Cleanup intentionally refuses after records are edited or other database activity occurs**, including sign-in telemetry. There is no force option. This conservative baseline cleanup is suitable for an unused dataset; a used demonstration requires a separately reviewed dependency-aware cleanup. Do not erase the manifest or issue broad SQL deletes to bypass this safety check.
- Existing records, migrations and development credentials are never reset. A fresh database backup/restore into a separate development database is a release/deployment task, not performed by this tool.

## Verification

`Tests/Verify-Guardian.ps1 -CheckMigrationIdempotence` includes the existing regression suites plus `MedicalTeam` and `SyntheticData`. Full setup/cleanup safety is exercised in rolled-back fixtures before populating a persistent dataset. Once a manifest exists, the synthetic suite checks its no-op preservation behavior instead of deleting or recreating the live demonstration. Results and the distinction between browser versus service testing are recorded in `PROJECT_STATUS.md`.

**Latest result: 2026-10-16 — PASS.** Full Debug Rebuild, all 16 executable suites (745 assertions), DatabaseIntegrity, migration replay 001–021 and Verify-Guardian's account/business-data preservation comparison succeeded. The populated SyntheticData branch passed ten checks, including exact credential/security-state preservation and rolled-back rename/disable/lockout scenarios. No QA account remained. The run remains Ready with its original 271 provenance rows, 11 synthetic accounts and 22 schedule records. The initial pre-population full setup/cleanup test is separate evidence, not a deletion/recreation of the now-used demo.

### Actual browser verification, October 16

- Patient: specialty/department/name/date filtering, doctor profile, SQL-backed slot selection, confirmation and My appointments. The booked slot disappeared; the appointment was visible to reception and the assigned doctor.
- Reception: confirmed and checked in that appointment through actual form submissions.
- Doctor: recorded the linked consultation with vitals, diagnosis, prescription and lab order; later reviewed the final result and signed the consultation.
- Laboratory: collected the sample, moved it into processing and finalized the training result.
- Pharmacy: dispensed the one prescribed unit; the queue cleared that item and stock history showed the debit. Another synthetic prescription remains pending for review.
- Nurse/Admin: recorded triage, progressed to UnderCare, linked admission to an available synthetic bed, recorded nursing care, resolved the emergency episode, transferred SYN-A to SYN-B and discharged. Both vacated beds became Cleaning, then an authorized Admin marked each ready (stored status `Available`). SYN-C remains occupied.
- The discharge browser response timed out, but SQL and the restored session showed it had committed exactly once. It was **not resubmitted**. Patient 360 shows the nursing and discharge records, the inpatient outstanding task is removed, and transactional audit contains admission/transfer/discharge/readiness events.
- Patient: verified own final results, dispensed prescriptions, appointment history, invoice/insurance and cash payment records, laboratory/pharmacy/admission/discharge inbox messages and scoped document empty state.
- Admin: verified Command Center counts/worklists, date-filtered reports, admission audit filters and a successful admissions CSV download.

Finance/claim **writes**, laboratory amendments, stock receiving/reconciliation and populated document upload/download were not newly submitted in the browser during this demo pass. Their automated coverage and remaining acceptance work are explicitly listed in `PROJECT_STATUS.md`. No external payments, insurer decisions or real clinical treatment occurred.

The settled training invoice already existed before browser QA; the new browser consultation was not automatically billed. Current stored invoice totals are 375 charged, 125 paid and 250 outstanding across three synthetic invoices. Training stock reconciles to its ledger (47 units in SYN-BATCH-01, three in SYN-NEAR-EXPIRY). These are demonstration record values, never public hospital statistics.

The populated demo is ready for manual review and the separately approved final design phase. This is not a production-readiness declaration; deployment/privacy/clinical acceptance requirements remain in `PROJECT_STATUS.md`.

### Cleanup after use

This live demo has now been used. Its original fingerprints no longer describe the current rows, and additional related browser records are outside the initial manifest. Consequently **do not run destructive cleanup on it**. The safe cleanup-preview refuses changed records. Preserve the run/manifest and use a separately reviewed dependency-aware cleanup if removal is later requested; the current tool deliberately has no force-clean option. Ordinary regression fixtures still roll back or perform narrowly guarded cleanup independently of the retained demo.
