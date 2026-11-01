# Guardian Health — project status

Last updated: 2026-10-20 (Asia/Riyadh). Authorized public homepage/sign-in cinematic design and verification; authenticated workspaces remain unchanged.

## 1. Scope and evidence

**Current continuation:** the user authorized a narrowly scoped cinematic homepage/sign-in redesign after the completed functional/UX verification. Sections 18–20 retain the historical evidence. Only public homepage/login presentation and its isolated theme/motion infrastructure are in the current design phase; clinical, patient and staff workspace redesign is not authorized. See PUBLIC_DESIGN_QA.md for implementation, defects and browser evidence.

This is the existing HospitalMSWebApp, not a replacement application. The roadmap below follows the hospital modules requested for Guardian Health and the subsequently requested Patient 360, journey, operational and accountability features. Existing uncommitted implementation, historical records and account credentials have been preserved.

Status meanings:

- **VERIFIED COMPLETE**: the explicitly bounded workflow in that row has usable application actions, persistence, relevant guards/integration, and successful automated verification. This is not a certification of the entire surrounding specialty or production readiness.
- **IMPLEMENTED BUT NOT FULLY VERIFIED**: implementation exists, but a meaningful positive path or deployment dependency has not been adequately exercised.
- **PARTIALLY IMPLEMENTED**: useful functionality exists but a material part of the requirement remains absent or insufficiently integrated.
- **MISSING**: no working end-to-end capability was found.

Evidence is code inspection, actual SQL Server queries and migration replay, executable regression suites, and interactive browser observations. Browser navigation is **not** counted as successful clinical or financial transaction testing. Most positive transaction tests use real database connections inside rollback-only fixtures. No production readiness claim is made.

## 2. Architecture and technology

- C#, ASP.NET Web Forms, .NET Framework 4.7.2; Visual Studio/MSBuild solution `HospitalMSWebApp.sln`.
- SQL Server; existing legacy `dbo` tables coexist with additive `gh` clinical/operational tables. The development instance is SQL Server Express. This document intentionally contains no connection-string values.
- ADO.NET with parameterized commands: `Models/Functions.cs` provides connection configuration and legacy compatibility; `Infrastructure/Database.cs` provides query helpers and serializable transaction execution.
- `HospitalWorkflowService` is split across `HospitalWorkflowService.cs`, `WorkspaceWorkflows.cs`, `PatientJourney.cs`, `OperationalWorklists.cs`, `OperationalReports.cs`, `Accountability.cs`, `LaboratoryResults.cs`, `StaffDirectory.cs`, `StaffProfiles.cs`, `MedicalTeam.cs` and `PatientAppointments.cs`. `HospitalTime` handles explicit presentation/input conversion while storage stays UTC. Password recovery has a separate transactional `PasswordRecoveryService`. Synthetic demonstration setup is an explicit console-only tool, never an application-startup path.
- `AppPage`, `WorkspacePage`, `LegacyDirectoryPage`, and `PublicRequestPage` apply page authorization and request/submission protections. Doctor, Nurse and Pharmacist primary workspaces also retain dedicated page implementations.
- Forms Authentication, server sessions, database-backed account activation and ticket version validation. Shared `GuardianUi` navigation and local Bootstrap/CSS/JavaScript remain in place; this functional phase is not redesigning them.
- Dedicated pages under `Views/Admin`, `Doctors`, `Receptionist`, `Laboratorian`, `Nurse`, `Pharmacist`, `Patient`, and shared Records/Documents/Notifications/Search.
- The generic `Portal.aspx` redirects authenticated requests to maintained workspaces before its old mutation dispatcher executes. Several obsolete public/clinical routes also redirect to maintained pages.
- Doctor, Laboratorian and Receptionist directory writes now use the transactional StaffDirectory service; page-level SQL remains for their reads. Some older catalogs still use page-level mutation and best-effort audit calls.

## 3. Original-roadmap functional audit

| Requirement / bounded workflow | Status | Implemented evidence and verification | Remaining qualification or gap |
|---|---|---|---|
| Seven-role login, role routing, protected workspace access and logout | VERIFIED COMPLETE | AuthenticationService, Global route guards, AppPage; all seven real development accounts passed HTTP login/redirect tests and interactive browser sign-in | Production hosting, MFA/SSO and independent penetration testing are not verified |
| Account lockout, activation and legacy credential migration | VERIFIED COMPLETE | AuthenticationGuards verifies locked/disabled accounts, old credentials, renamed accounts, first migration, current login and lockout; legacy fallback cannot reactivate or overwrite an existing unified account | Multi-client race/load testing remains; legacy migration is compatibility behavior, not a long-term identity-provider integration |
| Password recovery service and HTTP lifecycle | VERIFIED COMPLETE | PasswordRecoveryService and migration 019; standalone positive/negative SQL tests and real concurrent requests passed; version-bound single-use tokens, expiry, disabled-account rejection, preserved lockouts, transactional audit/security notice and HTTP replay guards | SMTP delivery is separately unverified. Historical unversioned links are retained in the database but must be replaced by a new request; public development token previews have been removed |
| Password recovery mail delivery | IMPLEMENTED BUT NOT FULLY VERIFIED | Configured HTTPS origin; encoded mail; uniform public feedback if delivery fails | No SMTP service is configured or verified locally; recovery requires an actual mail transport for user delivery. No development account was reset |
| Doctor, Laboratorian and Receptionist directory/account maintenance | VERIFIED COMPLETE | Shared transactional StaffDirectory service; 58 rollback assertions; actual browser selection/invalid-update postbacks for all three directories; HTTP selection, validation and replay guards | Explicit credential/email confirmation; existing identity and hashes retained on profile-only edits. Disabled accounts cannot be reactivated by synchronization, and password/profile edits do not clear lockouts. Positive mutations were service-tested, not submitted against real browser records |
| Nurse/Pharmacist profile and access maintenance | VERIFIED COMPLETE | Admin editor, StaffProfiles service and rollback tests cover contact/department synchronization, explicit email confirmation, account identity/password preservation, disabled/lockout state, stale submissions and duplicate guards; access changes are separate | Browser invalid submissions are checked without saving real profiles. Positive changes are tested on isolated fixtures; broad concurrent HR administration and infrastructure fault injection are not comprehensively tested |
| Broader Admin/staff management | PARTIALLY IMPLEMENTED | All five clinical/reception staff role directories now have profile maintenance; nurse/pharmacist department assignment, creation and activation are implemented | Not a complete human-resources system; department/ward/room maintenance remains limited, and central staff assignment/leave/roster workflows are absent |
| Departments/system configuration | PARTIALLY IMPLEMENTED | Department creation/directory; allowlisted hospital display/contact settings; transactional old/new-value audit tested | Safe department edit/deactivation and complete operational configuration lifecycle are absent; public settings integration needs further review |
| Patient registration, MRN, linked portal account | VERIFIED COMPLETE | RegisterPatient and CreatePatientAccount; real rollback fixture verifies unique MRN/account and duplicate-account rejection; directory renders | Patient duplicate identity reconciliation/merge is not implemented |
| Patient demographic maintenance/deactivation | IMPLEMENTED BUT NOT FULLY VERIFIED | Service validation, soft deactivation and reception directory actions exist | Full positive browser update/deactivation and historical-data regression matrix not exercised in this pass |
| EMR / patient medical timeline | PARTIALLY IMPLEMENTED | Consultations, observations, diagnoses, orders, results, nursing, emergency, admissions, claims/payments, documents and historical prescriptions accessible with role/ownership restrictions | Diagnosis coding/correction workflow and a formal clinical publication/visibility policy remain; historical missing authors/timestamps are not reconstructed |
| Doctor consultation draft, revision, sign-off and addenda | VERIFIED COMPLETE | Owned draft revision history, stale-edit guard, required assessment/plan at sign-off, immutable signed text with append-only addenda; integration/guard tests | This is not a digital-signature certificate system; structured multi-diagnosis maintenance and clinical discharge sign-off are separate gaps |
| Diagnoses, treatment plans and follow-up | PARTIALLY IMPLEMENTED | Primary diagnosis, free-text assessment/plan, follow-up date and follow-up worklist | Multiple coded diagnoses, diagnosis correction history, structured care-plan goals and follow-up reminder delivery not implemented |
| Additional/cancellable prescriptions and laboratory orders | VERIFIED COMPLETE | Shared order creation, active catalogs, duplicate-active-order guards, owner authorization, cancellation only before processing; history and recipient notifications tested | Orders after a signed encounter currently require another clinical workflow; finalized lab corrections use the separate, revision-guarded amendment workflow |
| Published schedules, booking, rescheduling and appointment lifecycle | VERIFIED COMPLETE | Published-slot checks; clinician/patient overlap checks; confirmation; UTC-day attendance; queue progression; elapsed-start no-show; request approval/decline; closed-state guards; unique linked encounter | Same-day early check-in is allowed by the current explicit UTC-day policy. Schedule withdrawal/editing with resolution of already-booked visits remains partial |
| Patient Medical Team discovery and doctor-first booking | VERIFIED COMPLETE | MedicalTeam service, patient-owned Web Forms workflow and migration 020; name/specialty/department/date filters, stored profile, actual available slots, confirmation and appointment history; service/DB guards and independent simultaneous-booking test; positive browser journey through reception/doctor | Dates are UTC; Admin must publish future schedules. No invented biography, certification, ratings or availability; existing professional fields only |
| Reception check-in to doctor handoff | VERIFIED COMPLETE | Recorded CheckInAt and CheckedIn/InProgress required before linked consultation; actor, patient and doctor matches; patient/doctor notifications and actual prior-status history | Unlinked walk-in consultations remain intentionally available; attendance is not inferred for old appointments |
| Laboratory collection, processing, final results and doctor review | VERIFIED COMPLETE | Ordered → Collected → Processing → Completed; result flags/reference ranges; no overwrite; critical clinician notification; owner review and cleared outstanding work tested | Append-only result correction is now implemented and tested (see below); specimen rejection/accession and device integration are not implemented |
| Laboratory result correction and re-review | VERIFIED COMPLETE | LaboratoryAmendments service, dedicated existing-workspace actions, migration 018, rollback correction/ownership/notifications/worklist/timeline tests and populated WorkspaceRender cases | Positive browser correction submission is not exercised; no instrument integration or dual-authorizer amendment approval |
| Pharmacy dispensing and stock integrity | VERIFIED COMPLETE | Matching unexpired batch, stock and remaining-prescription checks, partial/full status, attributed ledger/history and notifications; quantity precision and over-dispense guards | This does not include medication administration/eMAR, returns, recalls or purchase orders |
| Supplier maintenance, receiving and reconciliation | PARTIALLY IMPLEMENTED | Supplier create/update/deactivate; initial receiving; counted-stock adjustments; new supplier-referenced repeat delivery lines with ledger/audit, migration 017 and SupplierReceipts tests | Repeat delivery requires the same supplier/expiry as the existing batch; old unreferenced receipts are not backfilled. Stale counts are now rejected using the observed ledger revision and quantity, including net-zero intervening movements; returns/wastage workflow and procurement remain gaps |
| Nursing observations, assessments and escalation | VERIFIED COMPLETE | Patient/admission context, recorded-by vitals, care status, notes/history and attending-doctor escalation tested in rollback inpatient flow | A nurse-assignment roster and structured nursing care-plan goals/interventions/review are not implemented |
| Emergency/triage lifecycle | VERIFIED COMPLETE | Acuity, Waiting → UnderCare → Resolved, role checks, active queue and history; invalid skips rejected | Not a clinical decision-support or automatic triage system |
| Emergency-to-admission handoff | VERIFIED COMPLETE | Explicit same-patient UnderCare assessment link; composite FK and unique episode link in migration 016; nursing/attending notifications; replay and cross-patient guards | Admission does not silently resolve emergency care; an authorized nurse/doctor completes that transition |
| Admissions, transfer, discharge and cleaning/readiness | VERIFIED COMPLETE | Active patient/clinician/ward/room and available bed checks; no double allocation; old bed enters Cleaning on transfer; discharge summary; cleaning confirmation before availability; cross-role integration tests | Clinical discharge authorization/checklists, out-of-service maintenance and safe ward/room deactivation workflows remain outside the verified core |
| Wards/rooms/beds management | PARTIALLY IMPLEMENTED | Directory creation and allocation lifecycle exist | General editing/deactivation and maintenance scheduling are not complete |
| Invoices, line items, payments and balances | VERIFIED COMPLETE | Recomputed item totals; decimal validation; no overpayment; reserved insurance allocation; cash/card/transfer receipt recording; reference replay guard; patient notifications and ledger checks | Manual charge entry only; no automatic clinical-charge posting, refunds/credit notes/void reversal or online payment gateway |
| Insurance provider/policy eligibility and claims/settlement | VERIFIED COMPLETE | Active provider/policy/date/coverage checks; submit → review → approval/rejection → settlement; rejection reason; settlement reference; unique claim-linked payment; reservation release on rejection; history/notifs tested | Eligibility and insurer decisions are manually recorded, not independently verified through an insurer API. No external remittance, appeal or claim-resubmission integration |
| Patient Portal | PARTIALLY IMPLEMENTED | Own appointments/booking/change requests, records, prescriptions, final labs, balances/payment history, claims, documents and inbox; HTTP IDOR check and browser navigation | No online payment processing or patient profile self-edit. Patient access to draft/free-text clinical content needs an explicit hospital publication policy |
| Documents | PARTIALLY IMPLEMENTED | Authorized upload/download, patient ownership, 5 MB cap, PDF/PNG/JPEG signature checks, DB binary storage, encoded attachment filename and nosniff; upload/history/download service tests | No antivirus/content disarm, signed document approval, retention/version/withdrawal lifecycle. Full browser multipart-upload success path not exercised |
| In-app notifications / seven role inboxes | VERIFIED COMPLETE | Recipient-scoped inbox/read actions; order/check-in/lab/pharmacy/nursing/admission/financial/document notifications; transactional creation and patient event history; HTTP all-role inboxes | Browser read-state mutation/IDOR matrix is not comprehensive; no SMS/push/scheduled reminder delivery |
| Global search | IMPLEMENTED BUT NOT FULLY VERIFIED | Parameterized role-specific patient/catalog/invoice search; patient cannot open staff search; HTTP/render checks | Search result relevance, all role/data combinations, pagination and clinical/document full-text search are not comprehensively implemented or tested |
| Operational reports and secure CSV download | VERIFIED COMPLETE | Thirteen real-data reports; Admin page/service authorization; allowlisted report keys; date bounds; protected CSV cells; HTTP attachment/no-store/nosniff and negative tests; browser CSV download | Not historical financial snapshots; no scheduled reports, external BI or large-data performance validation |
| Transactional workflow accountability and Admin audit review | VERIFIED COMPLETE | Actor/status/detail/time history for the tested workflows; Admin-only source/entity/actor/date/text filters and pagination; integration/guard/HTTP/browser checks | Legacy page audits are sometimes best-effort and separate from mutation; no tamper-evident external archive; concurrent new events can shift offset pages |
| Public support/contact requests | PARTIALLY IMPLEMENTED | Persistent request/ticket forms, session-bound replay prevention, encoded email content; public HTTP render/replay checks | No complete staff assignment/reply/resolution workbench found; actual email delivery not verified |

## 4. Distinctive enhancements

| Enhancement | Status | Verified behavior / boundary |
|---|---|---|
| Patient 360 event history | VERIFIED COMPLETE | Migration 014 `vwPatientEvents`, compatible timeline projection, recorded-event timestamps, explicit unknown actors, inclusive filters, patient ownership. Tests cover nursing/triage/addenda restored after 013, creation vs future scheduled time, and no invented dispensing history |
| Patient Journey / outstanding actions | VERIFIED COMPLETE | Seven summary stages and detailed tasks from eight actual worklists; authorized Doctor/Nurse/own Patient access; completed/cancelled/reviewed/paid/discharged work disappears in integration tests |
| Admin Operational Command Center | VERIFIED COMPLETE | Nine allowlisted database worklists: appointments, clinical, lab, pharmacy, stock, beds, billing, insurance, emergency; current counts and role-safe navigation. Patient and non-Admin service access rejected |
| Explainable operational alerts | VERIFIED COMPLETE | Nine database rules for critical unreviewed results, long waits, old lab orders, low/expired stock, overdue invoices, cleaning, urgent waiting triage and rescheduling. No AI claims, predicted outcomes or inferred completion |
| Clinical accountability | VERIFIED COMPLETE | Attributed vital observations, owned sign-off, prior draft revisions, signed addenda, lab review and workflow history |
| Claim/payment integrity | VERIFIED COMPLETE | Protected pending allocations, frozen claim-covered invoice lines, deterministic state changes and exactly one payment per settled claim |

Command Center data refreshes on page/request load. It is not a push/streaming real-time system. Worklists/timelines have bounded result caps and do not imply all historical records are loaded at once.

## 5. Role-by-role capability map

| Role | Primary and secondary capabilities | Authorization limits |
|---|---|---|
| Admin | Workspace, Command Center, clinical operations, reports/downloads, audit review, staff directories, departments/wards/rooms/beds/settings, schedule publishing, admission/transfer/discharge/readiness, insurer decisions/settlement, shared documents/search/inbox | Not given Doctor clinical note-signing authority or general Medical Records page access merely because it is Admin |
| Doctor | Clinical queue/patient context, consultations/draft revisions, diagnosis/plan/follow-up, prescribing/lab orders, signed notes/addenda, result review, Medical Records/Documents/Search/Inbox | Only own encounters/orders for protected clinical changes and result acknowledgement; linked consultation requires recorded reception attendance |
| Receptionist | Patient registration/search/update, portal-account provisioning, schedule booking, confirmation/check-in/queue/rescheduling/no-show, invoices/items/payments, providers/policies/claim submission, documents/search/inbox | Cannot decide/settle insurance claims or sign/review clinical work |
| Laboratorian | Order worklist, sample collection/processing, result values/flags, final history, catalog search/inbox | Cannot sign a consultation, dispense medication or download patient documents |
| Nurse | Admitted patient context, vitals, assessments/notes/care status, escalation, emergency/triage, Medical Records/Documents/Search/Inbox | No pharmacy dispensing or administrative bed/admission assignment; no physician note signing |
| Pharmacist | Dispensing queue, inventory/batches/expiry/minimum alerts, receipt ledger, suppliers/catalog, referenced delivery lines, reconciliation, search/inbox | No laboratory finalization or clinical note signing; no patient document downloads |
| Patient | Own dashboard, appointments/booking/change requests, Medical Records/Patient 360/Journey, medicines/final labs, invoices/payment records/claims, documents/messages | Patient identity comes from the authenticated account, not a supplied patient ID; no staff search, reports, command center or audit access |

Doctor/Nurse read access is currently institution-wide within their allowed clinical role, not an assigned-care-team-only model. That privacy boundary needs hospital approval before release.

## 6. Cross-department scenarios actually tested

### Outpatient care — rollback integration fixture

Reception registers a patient and provisions its account → publishes real clinician slots → patient books/requests a change → reception reschedules/confirms → valid same-day check-in and queue progression → assigned doctor consultation with attributed vitals → additional/cancellable orders and draft revision → lab collection/processing/critical final result → ordering doctor notification and review → clinical sign-off/addendum → pharmacy dispensing from received stock and ledger reconciliation → invoice and added line → policy and claim review/approval → patient card/transfer portions plus one insurer settlement → rejection branch followed by cash payment → patient events/notifications and cleared worklists.

The future booking fixture is explicitly moved to the current attendance day **inside its rolled-back test transaction** before attendance actions. The application clock/rules are not bypassed in production code. This scenario does not claim that a browser completed every mutation.

### Inpatient care — rollback integration fixture

Nurse triage → UnderCare → Admin links that assessment to admission/available bed → nursing/attending notification → nursing escalation and independently attributed vitals → nurse resolves emergency care → transfer releases old bed to Cleaning → readiness confirmation → discharge summary/releases destination bed → cleaning/readiness. Integrity and guard tests reject mismatched patients, reused episodes, closed triage, occupied beds and inactive rooms.

### Negative and duplicate scenarios

Tests cover unauthorized service actors, patient ID forgery, duplicate consultations, invalid appointment transitions, future/past check-in, premature consultation, closed rescheduling, missing/invalid slots, lab overwrite and review replay, clinical stale revisions, processed-order cancellation, stock over-dispensing/precision, inactive suppliers, duplicate delivery references, claim replay/premature settlement/over-allocation, excess payments and repeated payment references, unsupported audit/report filters, document ownership, CSRF and identical POST replay.

## 7. Migrations 001–021

All listed scripts exist and were successfully replayed against the existing development database in the completed verification run. This project uses idempotent scripts/object checks, **not** a dedicated migration-version ledger. Application of a version is verified through its objects/constraints plus replay, not a nonexistent migration-history table.

| Migration | Purpose / preservation behavior |
|---|---|
| 001 GuardianHealth Core | Adds `gh` schema, accounts/permissions/settings/audit, patient profiles, scheduling/clinical/lab/pharmacy/finance/admission/document/notification foundations; extends rather than replaces legacy records |
| 002 Authentication Compatibility | Legacy identity and auth-version links, laboratory account compatibility and password storage capacity; unique legacy role/account mapping |
| 003 Patient Registration Integrity | Allows missing legacy receptionist attribution where appropriate; retains historical links rather than substituting unified account IDs |
| 004 Doctor Availability | Published schedule/slot procedure and booking enforcement, availability index |
| 005 Nursing Workflow | Attributed patient/admission nursing notes and care status |
| 006 Pharmacy StockMovements | Attributed stock ledger and batch index |
| 007 Workspace History | Transactional workflow history and independent patient-linked vital observations |
| 008 DocumentContent | Database-backed document content, constrained to 5 MB |
| 009 Triage Lifecycle | Explicit Waiting/UnderCare/Resolved state |
| 010 Clinical Addenda | Append-only signed-note addenda and timeline inclusion |
| 011 Legacy Laboratory Catalog | Preserves compatible legacy test definitions without inventing clinical reference ranges |
| 012 Workflow Accountability | Claim decision/settlement fields, unique linked insurance payment, encounter revision history and indexes |
| 013 Patient Timeline | Earlier cross-department timeline extension; superseded by the more complete event representation in 014 |
| 014 Patient360 Events | Actual event history with attribution; preserves unknown historical values and includes nursing/triage/addenda; compatibility timeline view |
| 015 Appointment Encounter Integrity | Unique filtered appointment-linked encounter index; refuses existing duplicates rather than deleting records |
| 016 Emergency Admission Handoff | Nullable explicit triage link on admission; composite same-patient FK and unique episode link; historical episodes are not guessed/backfilled |
| 017 Supplier Stock Receipts | Append-only referenced delivery lines, unique supplier/reference and ledger linkage, cost and receiver history; no invented references for old receipts |
| 018 Laboratory Result Amendments | Original final values/author/time remain unchanged; append-only correction revisions with before/after values, reason and actor; current-result projection and separate patient timeline events; versioned clinician review |
| 019 Password Recovery Version | Adds nullable IssuedAuthVersion and positive-version check to reset tokens. Existing token rows and all account credentials remain unchanged. Tokens issued before this binding are not retroactively trusted; users must request a fresh link |
| 020 Medical Team Booking | Optional doctor-to-department publication profile, SQL-backed available-slot view, strengthened database booking role/ownership/schedule/overlap guards; no account or historical-record rewrite |
| 021 Synthetic Demo Provenance | Explicit run/anchor/status and exact original manifest keys/fingerprints; only the separately invoked development console tool populates demonstration records |

Integrity checks verify trusted/enabled constraints, MRNs, seven active development accounts, stock vs ledger, over-dispensing, invoice vs payments, insurance reservations/settlements, active admission/bed relationships, documents/content, completed lab items, signed-note completeness, unique linked encounters, emergency/patient linkage and receipt/ledger agreement.

The current integrity suite also checks migration 019's enabled lookup index and trusted version constraint, Nurse/Pharmacist profile-to-account identity/role/status synchronization, duplicate linked staff accounts, retained fixture markers, and Medical Team/provenance schema objects. Migrations 020–021 were added in the later demo/booking phase; see section 18 for its verification evidence.

## 8. Security mechanisms and confirmed fixes

- Parameterized SQL; report/worklist/source keys are allowlisted before selecting SQL. Identity/ownership checks are repeated inside business services, not only hidden navigation links.
- Active account/role validation, session-to-actor binding, clinician ownership, patient ownership, and versioned authentication-ticket invalidation.
- Salted PBKDF2 passwords; malformed PBKDF2 encodings fail closed with bounded lengths/work factors. PBKDF2 strings are never accepted as plaintext legacy passwords.
- **Confirmed and fixed:** legacy credential fallback previously could reactivate a locked/disabled unified account. AuthenticationGuards reproduced the locked-account failure before the fix. Existing/renamed unified accounts now remain authoritative, and successful login uses a conditional update to detect concurrent credential/state changes.
- **Confirmed and fixed:** Nurse/Pharmacist access changes no longer clear lockouts or failed-login counts, and repeated same-state changes are rejected. The linked staff role must match before either record changes; actual prior/new state is recorded transactionally.
- **Confirmed and fixed:** recovery links are bound to the current account AuthVersion. Password/email/access-version changes invalidate prior links. Issuance and redemption lock the account before tokens, preventing successful double redemption. Reset does not activate or unlock an account. Password mutation, token consumption, audit and security inbox notice share one transaction.
- Forgot-password requests now have session-bound one-use submission protection; recovery pages use no-store/no-referrer headers. Public feedback is uniform for known/unknown/wrong-role/disabled identities and mail failures. No reset link is returned in public HTML, including development mode; the old preview flag is no longer used by recovery.
- Session-bound CSRF and expiring one-use workspace submissions; ViewState/session binding and submission tokens for legacy/public forms. Standalone role pages retain their own rotating request tokens.
- Serializable business transactions, explicit locks, uniqueness/check/FK constraints and transactional history on the major clinical, stock, insurance and admission flows.
- CSV quoting for delimiters/newlines/quotes, prefix neutralization of formula-like text, culture-independent numeric output, allowlisted filenames, Admin service/page restrictions, no-store attachment responses and nosniff.
- Document size/type signature checks, database storage outside public filesystem paths, authorized owner download, encoded filenames and attachment responses. These checks are **not** malware scanning.
- Password-reset URLs use a configured HTTPS origin rather than the request Host header. Contact email no longer builds a link from an untrusted Host value. Contact/support/audit IP handling does not trust arbitrary forwarded headers.
- Development seeding requires Development plus its explicit enable flag and now bootstraps **only an empty unified account store**, checked under the same transaction as creation. It never repairs a populated account store on application startup. This closes a confirmed code path that could recreate seed access after a legitimate sign-in email change. Existing/renamed/disabled/locked identities belong to the user; partial setup requires explicit administrator intervention, not automatic recreation.
- Recovery error diagnostics now log only a fixed operation category and exception type. Exception messages, inner exceptions, submitted values and mail-server responses are excluded; a regression injects secret-like markers and checks they do not reach the diagnostic listener. Hosting/proxy log redaction remains a separate deployment requirement.
- Seeder tests exercise missing/disabled/production gates with an unusable test connection, then force the explicitly enabled branch to fail. The temporary connection override is restored in `finally`; application configuration values, key count and read-only state are verified unchanged. Tests no longer mutate the read-only configuration collection. Populated-database tests include a genuinely renamed, disabled and locked rollback fixture plus fingerprints of all existing accounts and linked directories.
- Release transform disables development seeding/reset links/debug, removes the development connection-string section and enables secure cookies. Actual deployed transform/TLS/environment setup is still unverified.

This is evidence of particular controls, not a comprehensive security audit or compliance certification. Generic data access helpers are server infrastructure, not public APIs; remaining page-direct catalog mutations need further consolidation. Staff directory writes use a single transaction for both profile and linked account, with attributed history; changing the sign-in email or password requires an explicit confirmation, and current account IDs, role relationships and lockouts are preserved.

## 9. Historical functional-MVP automated verification — 2026-10-13

These are retained historical results. The populated-demo verification and expanded suites are recorded separately in section 18.

The final completed aggregate run for this phase exited **0** after all current fixes, including the seeder gate/configuration-failure tests. Full Debug Rebuild reported **0 warnings and 0 errors**. All **14 executable suites / 687 PASS checks**, database integrity, migration replay and the preservation comparison passed against the existing development database. Counts below are from that successful run, not the earlier interrupted or failed runs.

| Check | Actual result | Coverage |
|---|---|---|
| Full Debug build / Rebuild | PASS | Complete solution; 0 warnings and 0 errors in the final aggregate build |
| WorkflowIntegration | PASS, 70 assertions | Seven credentials, registered rollback patient, outpatient/inpatient/claims/document flows, accountability, timeline and worklist clearing |
| WorkflowGuards | PASS, 62 scenarios | Role/ownership/state/replay/precision/financial/bed/order and payment-reference negative paths |
| AppointmentLifecycle | PASS, 99 assertions | 56 transition pairs, linked consultation states, attendance/schedule guards, attributed history and direct database uniqueness |
| AuthenticationGuards | PASS, 40 assertions | Legacy/current boundaries, activation/lockout, credential preservation, malformed hashes, first migration; explicit development-only seeder gates, forced configuration failure/restoration, renamed disabled/locked fixture and populated-database preservation |
| PasswordRecovery | PASS, 63 assertions | Expiry, version binding/invalidation, single use, multiple outstanding links, real concurrent redemption, disabled/locked preservation, cross-account rejection, generic/no-preview HTTP responses, real fixture reset/replay, no-store/no-referrer, redacted error diagnostics and cleanup |
| StaffDirectories | PASS, 58 assertions | Three-role create/update/credential/account identity/history; duplicate and role guards; explicit email/password confirmation; lockout/disabled-state preservation; failed-sync rollback; ViewState selection persistence |
| StaffProfiles | PASS, 46 assertions | Nurse/Pharmacist contact and department updates; same IDs/staff numbers/passwords; disabled/lockout preservation; explicit email confirmation; no duplicate accounts/profiles; all six non-Admin roles denied; stale profile/access/recovery, invalid departments and other validation guards; rollback preservation fingerprints |
| ErrorHandling | PASS, 3 assertions | Legacy error feedback hides internal Exception/InvalidOperationException/ArgumentException detail while retaining server diagnostics |
| StockReconciliation | PASS, 15 assertions | Observed quantity/ledger revision, stale receipt/adjustment, net-zero movement, replay, role, precision and history checks |
| LaboratoryAmendments | PASS, 38 assertions | Original-result preservation, correction history, re-review/version guards, critical alerts, doctor/patient notifications, patient timeline/ownership and rollback |
| SupplierReceipts | PASS, 19 assertions | Repeat delivery success, separate references/cost history, retained batch settings, ledger consistency, replay/role/supplier/expiry/value guards, rollback |
| Reporting | PASS, 50 assertions | All 13 queries and empty CSV headers, six non-Admin roles denied, key/date guards, formula/delimiter/null/culture handling |
| WorkspaceRender | PASS, 17 page bodies | Real DB rendering, unique HTML IDs in tested bodies, encoded draft/profile content, slot booking, patient action availability and timeline anchor; populated result correction/review, stock-count and staff-profile forms with bound revisions |
| HttpRegression | PASS, 107 assertions | All seven role routes/inboxes, forbidden routes, patient IDOR, CSRF/replay, CSV/security headers and public/retired routes; legacy directory selection; both Nurse/Pharmacist profile editors reject stale/replayed/invalid input and preserve record selection without changing existing data |
| DatabaseIntegrity | PASS | Trusted/enabled constraints, recovery index/version constraint, staff-account synchronization, operational ledgers/relationships and no identified fixture residue |
| Verify-Guardian | PASS, exit 0 | Rebuild, compile/run suites, integrity SQL and preservation comparison |
| Migration replay | PASS, 001–019 | All nineteen scripts replayed on the existing development schema; no discarded records |

Authoritative final output is `%TEMP%\guardian-verification-final.log`; the targeted seeder/authentication run is `%TEMP%\guardian-authentication-check.log`. **Do not stream logs into `bin`: ASP.NET monitors it, recycles the application and invalidates sessions, causing false replay/expired-form failures.** Three historical logs (`guardian-verification.log`, `http-directory-check.log`, `recovery-verification.log`) were moved intact from `bin` to `%TEMP%\guardian-log-archive-5da63dbbff994cecaf738dfa10e23932`. No diagnostics were deleted and the archive operation did not modify application binaries. No `.log` files remained in `bin` at the final check. Counts above were derived from actual PASS output; later code edits must trigger appropriate reruns.

Preservation checking now fingerprints all application `dbo`/`gh` table contents plus account identities/credentials/activation/lockout state, excluding only login/access telemetry and system diagrams. These order-independent SQL checksums are regression indicators, not cryptographic backups. Identity counters may advance during rolled-back inserts. Intentional login/access audit rows remain; clinical/financial/inventory fixture records do not. Recovery tests mainly roll back; true independent concurrent/HTTP requests require one temporarily committed, uniquely identified QA account. Its account/staff/token/notification/audit/history records are removed in a guarded `finally` block and cleanup is asserted. Existing accounts are never used for password mutations.

## 10. Historical browser verification — before populated demo

The table below records the earlier, mostly read-only pass. It does not describe the newer successful browser mutations in section 18.

All seven development roles signed in successfully through the actual local browser. No existing clinical record was changed to demonstrate a workflow.

| Role | Interactive observations | What was not proven by that browser check |
|---|---|---|
| Admin | Dashboard/navigation/operations/audit/report/download checks; all three legacy staff directories selected existing profiles and retained selection through invalid updates; password fields remained blank. Nurse and Pharmacist profiles loaded named departments and account context. Invalid profile submissions exposed a lost-selection redirect, now fixed and browser-retested for both roles | No live claim settlement, admission or positive staff-account modification was submitted; those changes use rollback service tests |
| Doctor | Queue shows awaiting reception check-in instead of consultation action for unattended visit; note/draft/order/sign/follow-up/history controls; revision-bound result-review selector | Successful consult/order/sign/re-review transactions were verified by rollback service tests, not a committed browser visit |
| Receptionist | Queue/check-in policy, rescheduling/decline controls, registration directory, finance navigation/forms | No new retained patient, appointment or payment was created in the browser |
| Laboratorian | Worklist, ordered/collected/processing/final progression, result controls/completed history; new Result corrections tab and correction-history empty state | No committed sample, final result or amendment entered through the browser |
| Nurse | Admitted-patient workspace and safety/assessment navigation; emergency active queue and progression controls | No live admission/assessment/triage mutation submitted |
| Pharmacist | Dispensing/inventory/movement navigation; suppliers/Deliveries tab; reference/cost/expiry fields and history; count-snapshot selection and stock-reconciliation instructions | Receiving and reconciliation mutations are tested with rolled-back DB fixtures, not retained browser stock |
| Patient | Own appointments, past-action contact guidance, records/journey/outstanding task, timeline Registration filter retaining the Timeline tab, Documents empty state and own notification inbox | No new booking, read-state mutation, online payment or download of a populated clinical attachment was submitted |

Browser checks are functional desktop checks, not a comprehensive new responsive/accessibility or visual-design certification. Final design work remains deferred. Empty-state observation is not evidence that a populated transaction workflow works; relevant positive coverage is listed under the automated suites.

The browser also submitted an unknown-account recovery request and confirmed generic feedback without creating a real-user reset token. Positive password changes were exercised by the automated HTTP suite against an isolated QA account, not by editing a development user's password in the browser. Seven-role sign-in/navigation was repeated during this verification phase; Doctor laboratory review, Reception finance, Laboratory corrections, Nurse emergency/triage, Pharmacist supplier workflows and Patient 360/Journey were opened through their actual navigation.

## 11. Known defects / material limitations

1. **Fixed and verified:** Doctor/Laboratorian/Receptionist directory changes, linked account synchronization and workflow history share one transaction. Selection persists across postbacks. Nurse/Pharmacist contact and department maintenance now has an Admin editor and transactional service, stale-edit guards, explicit sign-in email confirmation and scoped notifications. Browser invalid submissions retain the selected profile after the redirect fix. Tests preserve identity, passwords, role relationships, disabled state and lockout; no duplicate profile/account is created.
2. **Fixed and verified:** stock reconciliation requires an observed ledger version and quantity. Receipts, dispensing or another adjustment invalidate an old count, including net-zero changes. Invalid/replayed counts do not write an adjustment.
3. Clinical diagnosis maintenance remains limited. **Laboratory correction gap closed and verified:** originals are retained; amendments carry reason/author/previous and corrected values; clinician review is reopened and stale review submissions are rejected. Signed clinical notes retain protected addenda.
4. Public contact/support messages persist but have no complete Admin resolution workbench.
5. **Legacy error exposure fixed and tested:** the reviewed directory and retired clinical handlers use generic unexpected-error feedback and server tracing. Domain validation remains encoded and visible. Centralized error monitoring and delivery are not configured or verified.
6. Some catalog/settings/directory mutations are page-direct SQL and their audits are separate/best-effort. An audit UI does not make every mutation fully transactional/audited.
7. Historical unknown actors/timestamps, old appointment attendance and legacy prescriptions are displayed as recorded, not repaired by inference. Some Patient 360 creation/discharge entries project `Not recorded` even where an actor is available in the separate transactional audit; bed-transfer timeline detail currently uses internal bed references. Audit Review retains the actual actor and transition. Presentation enrichment is not claimed complete.
8. Reports show current statuses/balances for selected activity periods, not reconstructable historical financial snapshots. Worklists/search/timeline caps and no large-data/load testing limit scale claims.
9. Positive outpatient/inpatient browser mutation coverage was expanded in section 18, but remains short of complete hospital acceptance. Password-reset email delivery, general concurrent multi-user stress, backup restoration and production deployment are not verified. Simultaneous password-reset redemptions and competing appointment bookings were specifically tested; these are not general load tests. Recovery anti-abuse throttling and timing-side-channel assessment remain release work.
10. **Fixed:** failed Nurse/Pharmacist profile submissions no longer lose record selection on the post/redirect/get round trip. The redirect retains only a validated numeric identity on the same Admin page; it does not accept arbitrary return URLs. Both roles were browser-retested with invalid input, and HTTP tests cover stale, required-field and replay failures.
11. **Fixed:** development startup no longer identifies missing accounts by their original seed email and recreates access. Only an empty account store may be bootstrapped. Fresh-database bootstrap/deployment still needs a separate rehearsal; the preservation tests intentionally do not empty the real development database.
12. **Test harness fixes:** the staff selector parser now decodes HTML entities; configuration cleanup no longer calls a read-only configuration collection's removal API. These were test defects, distinct from the profile-selection and seeder defects. Historical logs were moved out of `bin` without modifying binaries or deleting diagnostics.

## 12. Explicitly missing or partial original functionality

- **MISSING:** structured nurse assignments and care plans with goals/interventions/review; multiple coded diagnosis/correction workflow; laboratory specimen rejection/accession; appointment schedule withdrawal with affected-patient resolution.
- **PARTIALLY IMPLEMENTED:** supplier receiving/reconciliation (receiving and concurrency-guarded counting work; complete returns/wastage/recall lifecycle is absent); staff/department/ward/room maintenance; clinical discharge approval.
- **PARTIALLY IMPLEMENTED:** billing and insurance beyond the verified manual lifecycle: automatic care-charge linkage, credit/refund/void correction and insurer remittance/appeal integration.
- **MISSING:** online payment gateway, external insurer eligibility verification, medication administration chart, scheduled email/SMS appointment reminders. These are not simulated or represented as working integrations.
- **PARTIALLY IMPLEMENTED:** documents retention/versioning/content scanning; comprehensive audit coverage; full staff support request handling; patient profile self-service; complete search pagination/secondary-entity discovery.

No extra speculative AI functionality is being added. More advanced procurement, device integrations, SSO or external BI should not be treated as silently completed requirements; their need must be explicitly scoped before implementation.

## 13. Remaining work before final UI redesign

1. Preserve the tested service/security boundaries and functional controls during the design phase. Legacy and Nurse/Pharmacist staff-profile maintenance no longer block that work.
2. Keep the still-partial original requirements explicitly in the backlog: clinical diagnosis maintenance, nursing assignment/care plans, schedule maintenance, clinical discharge authority, operational catalog lifecycle and support resolution. A decision to start design is not acceptance of these as complete.
3. Agree hospital privacy and publication rules before broad deployment. These decisions may affect the final design's patient/staff visibility and actions.
4. Exercise remaining positive UI mutations in an isolated acceptance-test database; preserve the development database and credentials. Final design must retain all currently tested workflows.
5. Rerun affected suites throughout redesign. No styling, homepage or visual-identity changes were made during this functional verification pass.

## 14. Remaining release blockers

- Provision production identities and least-privilege DB/app identities. Disabling a seeder does not remove already-provisioned development accounts; never publish the development database as production.
- Verify deployed Release transforms, HTTPS/HSTS/reverse-proxy trust and cookie behavior, canonical public origin, session settings and secret injection. No secrets belong in this file or source control.
- Configure and test SMTP delivery and operational error monitoring; reconcile the legacy `SmtpServer` vs reset `SmtpHost` configuration paths before relying on mail. Recovery no longer falls back to exposing a bearer link on the public page. Verify reset-token redaction in hosting/proxy/request logs (local IIS Express access logging is disabled), and add deployment-appropriate abuse throttling/timing assessment.
- Establish migration deployment/rollback runbooks and tested backup restoration. Scripts require the legacy schema; clean-machine bootstrap from an empty database has not been verified.
- Complete privacy/clinical policy decisions: care-team access boundaries, draft publication to patients, correction/discharge authority, retention and audit access.
- Add document malware scanning/content validation appropriate to deployment and test populated authorized/unauthorized download paths over HTTP.
- Perform a dependency/security review, independent penetration testing and performance/concurrency tests on representative data. Existing indexes/locks and sequential guards are not evidence of load capacity.
- Address remaining catalog transactional weaknesses and broader staff-maintenance coverage; verify audit/log retention, alerting and recovery after infrastructure failure.
- Complete clinical/user acceptance testing and the separately scheduled UI/accessibility/responsive phase. No regulatory or production certification is claimed.

## 15. Development setup and verification notes

1. Use Windows, Visual Studio 2022/MSBuild with ASP.NET Web development and the .NET Framework 4.7.2 targeting pack. Restore the solution's existing NuGet packages; retain its Web Forms structure.
2. Use the existing authorized development SQL Server database containing the legacy HospitalMS tables. Do not point verification scripts at production. The current test scripts contain local development-target configuration and must be reviewed before use on another machine.
3. Application database configuration is supplied through `HOSPITALMS_CONNECTIONSTRING` (or its configured environment-variable name) with a local configured fallback in development. This file deliberately omits all values.
4. Apply `Database/Migrations/*.sql` in filename order using a migration-capable identity. Migration replay is idempotent on the verified development database, not a substitute for backup or staged deployment rehearsal.
5. Run IIS Express using the existing solution configuration; the verified local site uses port 53677. Keep it running for HTTP tests. Compilation of files in `bin` recycles application sessions; sign in again afterwards if needed.
6. Supply any existing non-default test credentials via the `HOSPITALMS_TEST_<ROLE>_PASSWORD` environment variables expected by the test code. Obtain values from the authorized project owner; this document contains none. Do not re-seed or reset accounts to make a test pass.
7. Run `Tests/Verify-Guardian.ps1 -CheckMigrationIdempotence`. It performs full Debug Rebuild, compiles the console suites, runs each, invokes `Tests/DatabaseIntegrity.sql`, and compares preservation fingerprints. If capturing output, redirect it to a log outside the web application (such as the operating-system temporary directory), never to `bin`. SQL client tooling must be installed. Current roles include Admin, Doctor, Receptionist, Laboratorian, Nurse, Pharmacist and Patient.
8. Run tests while no other user is changing business data: concurrent legitimate edits may cause preservation snapshots to differ. Most fixture inserts are rolled back; the recovery concurrency/HTTP test uses one guarded, cleaned-up transient account. Login/access audit events and SQL identity increments are expected. If the test process is forcibly terminated during that specific fixture, inspect its QA-prefixed account before rerunning; never delete unrelated records.
9. Development account creation requires both development environment and seeding enabled, and occurs only when the unified account store is empty. Startup does not restore missing/renamed accounts or repair partially provisioned databases. Release disables seeding; recovery never emits development reset links in either mode. Existing credentials and activation/lockout state remain unchanged.
10. Keep generated `bin` logs/downloads and private deployment settings out of publication artifacts. Commit/release preparation must review the entire existing dirty working tree, not only the latest incremental changes.

## 16. Continuation checkpoint

The functional MVP verification phase is closed at the user's requested boundary on 2026-10-13. All final results are in section 9. The reviewed changes preserve existing records and all seven development credentials; fingerprints include account identities, passwords, activation, credential versions, failure counts and lockouts. Intentional login/access telemetry is excluded from business-data preservation checks; fixture identity-counter advances are expected. The final diff check found no whitespace errors. No reset/revert, destructive migration, unrelated module or visual redesign was performed.

## 17. Functional MVP readiness decision

**FUNCTIONAL MVP READY FOR FINAL VISUAL REDESIGN: YES.**

This is a decision about the tested core hospital workflows being stable enough for presentation-layer work. It is **not** a claim that every original roadmap extension is complete, that all browser transactions have been accepted by hospital staff, or that the application is production-ready. The statuses and gaps in sections 3, 12 and 14 remain authoritative.

### A. Functional MVP completeness

Verified core original modules include seven-role authentication/RBAC and staff profile maintenance; patient registration/MRN/account provisioning; scheduling, rescheduling, attendance and queue handoff; consultations, draft history, sign-off and addenda; laboratory collection/processing/finalization/amendment/re-review; pharmacy dispensing and stock/receipts/reconciliation; nursing observations/escalation and emergency triage; admission/transfer/discharge/bed cleaning; manual invoicing, payments and the internal insurance claim/settlement lifecycle; scoped in-app notifications; operational reports and audit review. Patient 360, Patient Journey, worklist completion and operational alerts are also verified within their stated boundaries.

The two requested cross-department journeys passed through real SQL services using controlled fixtures, including invalid-transition/role/ownership guards, financial and stock integrity, audit/history and notifications. All seven existing roles passed HTTP access checks and interactive browser sign-in/navigation. Documents, broader EMR/Patient Portal, search and broader administration remain qualified/partial as shown in the roadmap table; they are not silently promoted to complete.

### B. Remaining critical defects

No known blocking defect remains in the **tested functional MVP scope** after the final passing regression. This is an evidence-bounded finding, not proof that no undiscovered defects exist. Profile selection loss, lockout/version recovery issues, seeder identity recreation, sensitive recovery diagnostics and the identified test-harness failures have been addressed. No failing suite is being waived.

### C. High-priority gaps that do not block starting design

- Original scope still includes structured nurse assignment/care plans, multiple coded diagnoses/corrections, schedule withdrawal with affected-patient resolution, clinical discharge approval and fuller department/ward/room maintenance.
- Broader finance/pharmacy gaps include reversals/refunds/automatic charge linkage and returns/wastage/recall handling. Core manual financial and dispensing workflows are verified, not those extensions.
- Support request resolution, patient self-service demographic editing, document lifecycle/scanning and comprehensive legacy-catalog auditing remain partial.
- Patient publication/care-team privacy policies and operational acceptance need authorized hospital decisions. Keep these gaps visible during final design; starting design does not remove them from the roadmap.

### D. Optional enhancements

Advanced procurement, device integrations, external BI, MFA/SSO and push-based live dashboards are not required to validate the current MVP. External insurer and payment-gateway integrations must not be represented as working: current eligibility/claim decisions and recorded payments are manual. Any expanded integration scope needs explicit agreement.

### E. Security and deployment blockers

**NOT PRODUCTION READY.** Actual SMTP delivery, deployed HTTPS/cookies/secrets/least-privilege identities, removal of development identities from the deployment dataset, backup/restore and migration rehearsal, document malware controls, recovery abuse/log-redaction controls, privacy/clinical publication policy, operational logging/monitoring, dependency review, penetration testing and representative performance/concurrency testing remain unverified or unfinished. Existing production transform intentions are not evidence of a deployed secure configuration. See section 14 for the full release gate.

### F. Manual acceptance still required

Run positive browser-driven outpatient and inpatient journeys in an isolated acceptance-test database, with clinician/nurse/pharmacist/reception sign-off. Include staff profile/email edits, clinical notes and amended-result re-review, dispensing/count reconciliation, claims/settlement, populated document multipart uploads/download authorization, and actual reset email delivery. Existing browser checks establish access/navigation and selected validation behavior; service/HTTP fixture tests establish the successful mutations noted above. General multi-user stress, infrastructure failure injection and final responsive/accessibility testing remain separate acceptance work.

**Historical phase boundary (October 13).** Later user requests authorized the demo/booking verification below, not visual redesign. The final design phase still requires approval and must preserve the verified workflow/security contracts.

## 18. Populated synthetic demo and browser verification — 2026-10-16

### Installed data and preservation boundary

The existing `guardian-synthetic-v1` run is **Ready**, anchored to 2026-10-15, with **271 original manifest records**, **11 synthetic accounts**, **three synthetic doctors**, **four synthetic patients**, **three synthetic departments** and **22 published schedule records**. Setup was completed before this continuation; it was inspected rather than recreated. The seven original development accounts and pre-existing records were preserved. See `DEMO_SETUP.md` for non-secret access identifiers and the representative scenarios.

The three specialties are General Medicine, Cardiology and Pediatrics. The seven daily sessions per doctor run October 16–22, 09:00–12:00 UTC in 20-minute slots; an additional valid October 15 session supported same-day workflow setup. Time passing does not rewrite these sessions or old attendance. Further sessions must be explicitly published by Admin. No doctors, ratings, qualifications or hospital achievements are invented outside the clearly labelled synthetic development data.

The manifest is the **initial population baseline**, not a running registry of browser activity. Subsequent intentional synthetic appointments, clinical/lab/pharmacy and inpatient actions are retained with normal relationships/history/notifications. A separate patient appointment already present at the start of this browser pass was not altered. Used-demo cleanup deliberately refuses changed fingerprints or outside activity; there is no safe force-delete path and no broad cleanup was attempted.

### Browser transactions and cross-role results

| Role / scenario | Actually executed and observed | Verification boundary |
| --- | --- | --- |
| Patient discovery and booking | Synthetic Patient searched by name, specialty, department and date; opened a doctor; selected a published SQL slot; submitted confirmation; saw the new appointment and slot removal | This is an actual Web Forms POST, not only a service test; invalid/concurrent bookings remain automated-test coverage |
| Reception | Same booking appeared in today's list; confirmed, then checked in through queue controls | Existing UTC-day attendance rules retained; no date or database bypass in browser QA |
| Doctor | Opened the checked-in appointment, recorded consultation/vitals/diagnosis/plan and prescription/lab order; later acknowledged finalized result and signed note | One successful new outpatient journey, not all clinical correction branches |
| Laboratorian | New order appeared; Collected → Processing → final value with unit/reference/flag; completed queue updated | Browser amendment/re-review after correction remains manual acceptance work |
| Pharmacist | New prescription visible; valid batch dispensing succeeded; item left pending queue; stock history showed one-unit debit | Receiving/reconciliation guarded mutations are automated; supplier/inventory/movement pages were browser inspected |
| Nurse and Admin inpatient journey | New synthetic triage → UnderCare → linked admission into SYN-A → nursing assessment/vitals → emergency resolved → transfer to SYN-B → discharge → both old beds Cleaning → Admin readiness completion to Available | Existing SYN-C inpatient preserved; only synthetic patient/bed records were changed |
| Patient follow-through | Own appointment history, dispensed medicines, final labs, paid invoice, separate insurance/cash receipts, claim state and in-app notifications visible; discharged patient sees nursing/discharge records and no inpatient outstanding task | Document empty state verified; no populated browser upload/download or external payment performed |
| Admin | Command Center shows live pending clinical/lab/pharmacy/claim work and current beds; report date filters and admissions CSV download succeeded; audit filters show exactly one admission, transfer and discharge for new episode plus two readiness events | Finance/claim state mutations were not newly submitted through the browser; existing manual lifecycle is service/integration tested |

All seven roles signed in and their primary/secondary navigation was inspected using the running IIS Express site. Reception, Doctor, Laboratory, Nurse, Pharmacy, Admin and Patient pages loaded without an application error in the serial browser pass. The intentionally pending synthetic cases remain available; verification did not empty every worklist merely to produce zero cards.

The Discharge click's browser-control response timed out. Before any retry, SQL was inspected: the admission was already Discharged with a timestamp and a single transactional discharge event, and the bed was Cleaning. After reauthentication, that patient was absent from the active-admission selector. **No duplicate Discharge submission was made.** The readiness actions subsequently succeeded through the actual Admin controls. This was not treated as an uncommitted transaction or a reason to recreate data.

### Corrections and verification improvements in this phase

- Patient registration now validates a required valid birth date before SQL and normalizes an omitted/Unknown blood type to null rather than violating the existing database constraint. Invalid blood types produce domain validation.
- Medical Team booking reuses the existing booking transaction/business logic; direct SQL guards enforce active roles, patient ownership, published aligned slots and doctor/patient overlap. No UI-only security boundary was introduced.
- Populated `SyntheticData` verification now compares exact in-memory account identity, password-hash, role, activation, version, failure and lockout values. Its renamed/disabled/locked manifest-account fixture is transactionally rolled back; no credentials are printed. Cleanup-preview refusal is tested without deleting the live demo.
- Browser POSTs are checked for committed state before retrying after a timeout. Regression runs are kept separate from browser mutations to avoid serializable-fixture lock contention and preservation false positives. Diagnostics remain outside the application's `bin` directory.

### Latest regression run

`Tests/Verify-Guardian.ps1 -CheckMigrationIdempotence` completed on **2026-10-16 with exit 0** against the populated and browser-exercised development database. Full Debug Rebuild succeeded; no build warnings/errors were reported. All **16 executable suites / 745 PASS assertions**, integrity SQL, migration replay **001–021**, and the before/after preservation comparison passed. Output is retained outside the application at `%TEMP%\guardian-demo-final-regression.log`.

| Executed suite | Actual PASS assertions |
| --- | ---: |
| WorkflowIntegration | 70 |
| WorkflowGuards | 62 |
| AppointmentLifecycle | 99 |
| AuthenticationGuards | 40 |
| PasswordRecovery | 63 |
| StaffDirectories | 58 |
| StaffProfiles | 46 |
| SupplierReceipts | 19 |
| StockReconciliation | 15 |
| LaboratoryAmendments | 38 |
| Reporting | 50 |
| ErrorHandling | 3 |
| MedicalTeam | 32 |
| SyntheticData — populated branch | 10 |
| WorkspaceRender | 18 |
| HttpRegression | 122 |

DatabaseIntegrity and the aggregate preservation PASS are additional checks, not included in the 745 executable assertion count. MedicalTeam's true independent concurrent booking produced exactly one winner and removed only its isolated temporary fixtures. PasswordRecovery's independently committed concurrency/HTTP fixture was also cleaned. The original seven development accounts authenticated in the regression without password changes.

Preservation snapshots matched for all checked application tables and account identity/credential/security fields; only intentional login/access telemetry is excluded, and SQL identity counters can advance on rolled-back inserts. The enhanced synthetic test additionally compared exact account values in memory. No regression fixture account was retained. The post-run checks confirmed **18 accounts = seven original + 11 synthetic**, the run still **Ready**, all **271 original manifest rows**, and **22 synthetic schedules**. Synthetic batch quantities match the ledger exactly: 47 units in SYN-BATCH-01 and three in SYN-NEAR-EXPIRY. Three training invoices total 375, with 125 recorded payments and 250 outstanding; this is persisted demonstration activity, not real money. The browser admission has exactly one discharge history event.

A separate read-only `preview` confirmed the installed run; `cleanup-preview` safely refused the already-used demo without deleting anything. No population command was run to recreate it. Idempotence was exercised solely by the guarded no-op test branch. No new password/secret was added to repository documentation, and the provided development password was absent from the reviewed application/test/tool source paths. No verification logs were written into `bin`.

The earlier pre-population aggregate completed with 750 executable PASS assertions, migration replay 001–021, Debug build, integrity and preservation checks. Its `SyntheticData` branch exercised full seed, pristine selective cleanup, tombstone no-op, changed-account refusal and unrelated-activity refusal inside rolled-back fixtures. The populated run intentionally must not delete/reseed that live demonstration; its assertion count is therefore different.

### Remaining limitations / readiness

No frontend redesign or unrelated module was added. The original backlog in sections 12 and 14 remains: structured care plans/assignments, coded diagnosis correction, schedule withdrawal, clinical discharge approval, complete catalog lifecycle, finance reversals/automatic charges, pharmacy returns/recalls, support resolution and patient demographic self-service. Clinical publication/privacy policy is not resolved by the existence of a timeline.

Manual acceptance still includes browser supplier receiving/reconciliation, laboratory amendments and re-review, claims/settlement writes, populated document upload/download and hospital staff sign-off. SMTP, secure deployment, secrets/least privilege, synthetic-account exclusion from production, backup/restore, document malware controls, independent security review and representative load testing remain release gates. This database is a labelled **development demonstration**, not production data or evidence of production readiness.

**POPULATED DEMO READY FOR MANUAL REVIEW AND FINAL VISUAL REDESIGN: YES**, within the verified functional-MVP boundaries above. No failing test is waived. The new outpatient browser encounter was not automatically charged: billing is still manual, and the paid invoice/payment records inspected in the portal belong to the earlier seeded training scenario. Current patient timeline attribution/display limitations are stated in section 11, not disguised as invented history. Use the unchanged credentials and existing synthetic records; do not re-seed to refresh dates. The final visual redesign has not started.

## 19. Patient and staff task UX — 2026-10-18

This phase implements the prioritized findings in `HEALTHCARE_UX_AUDIT.md`, using official public Mayo, Cleveland Clinic, NHS, Epic/MyChart and Oracle Health documentation. It does not claim access to private EHRs or copy their clinical policies. Branding, colors, typography, homepage, framework and existing workflow rules remain unchanged.

### Completed scope

- Patient: single doctor-first booking path with explicit review/confirmation; dedicated owner-scoped appointment lists/details/history; contextual rescheduling/cancellation and stale start/end/status rejection; summary-first dashboard; strict UTC/local-input conversion with DST guards. Hospital display timezone defaults explicitly to UTC, not the user's location. No location/preparation instructions are invented.
- Doctor: pending rescheduling requests remain on the clinician queue while the original reservation is valid. Tests cover approval, decline, cancellation and completion through an attended consultation, without relaxing check-in or ownership.
- Reception: row → patient/clinician/time/status context → only eligible attendance actions. Snapshot guard prevents applying a state change to a changed appointment. Future check-in and direct clinical-start actions are not offered. Named/MRN selectors retain the selected appointment; existing scheduling, registration and finance services remain intact.
- Laboratory: unfinished-work default, stage/name/MRN filters, contextual specimen/test/reference information and valid next-stage/result actions. Completed items route to the existing revision-protected amendment workflow. Historical corrections and clinician re-review remain intact.
- Nurse: admitted-patient row → assessment tab with unique active admission, persistent safety summary, bed context and attributed last-recorded care state. Invalid POST retains patient/task context; stale assessment after discharge is rejected.
- Pharmacy: prescription row → patient/allergy/medication/remaining context → only matching positive, in-date batches ordered by expiry. Invalid POST retains selection; the original transactional stock/ledger/over-dispensing rules remain authoritative.
- Admin: role-safe command-center metric links, actual status filter and active-alert default. Snapshot limits are explicitly disclosed; no clinical authority is inferred from Admin access.
- Shared: rectangular worklist hit targets, record-specific tab fragments, accessible error/success roles and error focus following post/redirect/get. No visual redesign was introduced.

### Verification boundaries

The patient-phase aggregate passed 17 executable suites / 801 assertions plus integrity, migration replay and preservation checks before staff edits. The initial staff aggregate passed 18 executable suites / 835 assertions, including PatientExperience 47 and StaffTaskExperience 26. That result preceded the final mobile feedback correction; section 20 records the closure verification rather than treating the older pass as evidence for later changes.

Browser evidence spans all seven roles. Actual patient booking/request/reception decline/patient outcome/cancellation occurred in the earlier part of this continuation and is intentionally retained as one labelled synthetic appointment. Later checks verified specialty selection, real availability, no-availability recovery, upcoming/history separation, doctor queue and results/notes, reception context/registration/finance, lab active/completed/correction paths, nursing assessment/safety, pharmacy matching batches and supplier/reconciliation, Admin bed filtering/operations/scheduling/reports/audit. An over-dispense and invalid nursing temperature were submitted and rejected without changing stock or clinical records. These are negative browser transactions; positive new staff writes are rollback-fixture evidence rather than new browser treatment/finance events.

DOM checks on inspected routes found no duplicate IDs or unlabelled controls. Patient mobile rendering was reviewed at 390 pixels. This does not certify every screen-reader/device combination. The wrapped-link pointer defect was reproduced, fixed, and rechecked on staff worklist links. No passwords, reset tokens or patient-sensitive records are included in this document.

### Data and readiness

No new migration was needed; schema remains migrations 001–021. Existing seven development accounts, 11 synthetic accounts and original 271-row Ready manifest are not recreated. User-added appointments are preserved. Automated fixture inserts are rollback-only or use existing guarded cleanup; identity sequence gaps and expected authentication/access telemetry are not record loss.

Selected P1 task improvements are implemented. Remaining practical UX work includes doctor today/overdue/future grouping and pagination, direct selected-context links in older notes/result tables, more contextual reschedule decisions, wider server-side filtering/paging and clinical staff acceptance. The broader functional limitations in sections 12/14 are not erased by these UX improvements.

Starting final visual design is separate from production deployment. The final continuation decision is in section 20. SMTP, secure deployment/configuration, excluding demonstration identities/data, backup/restore rehearsal, document malware controls, clinical privacy/publication policy, independent security review and realistic load/concurrency acceptance remain release blockers.

## 20. Final functional UX verification — 2026-10-19

### Completed fixes and browser evidence

The remaining mobile issue was reproduced: focusing an error during page load did not prevent native fragment restoration from scrolling it out of view. Shared feedback now lives beside the selected task when a task fragment is present. It is an accessible alert with a programmatic focus target; after initial page display it receives focus and an immediate, non-animated scroll into view. The selected workflow tab remains open. Successful feedback is excluded from this handler, and normal navigation does not force error focus.

Related return-path defects found in this narrowly scoped audit were corrected: doctor validation preserves patient/appointment context; laboratory correction preserves the finalized item and correction tab; reception preserves reschedule/decline task sections; Admin setting feedback reopens Configuration; patient validation reopens the selected request/cancellation form without reopening actions after a successful save. Return identifiers are positive integers, never untrusted redirect URLs. Authorization, CSRF/replay checks and transactional services are unchanged. No stylesheet, branding, database schema or credential was changed.

All seven roles were checked in the actual browser at 1366 × 1000 and 390 × 844. Each submitted an intentionally rejected operation: over-dispensing, invalid nursing/consultation temperature, invalid laboratory correction unit, past reception reschedule, overlength Admin setting, and past patient preferred window. Errors were visible below the header and focused, relevant record/task context remained, and no duplicate IDs, unlabelled controls or page-width overflow were found in these inspected views. Sampled keyboard Tab and arrow-key tab navigation passed. Patient-to-Admin navigation was denied. The full per-role evidence and boundaries are in `HEALTHCARE_UX_AUDIT.md`.

These are **actual negative browser transactions**, not new successful treatment, dispensing, amendment or financial events. Positive workflow evidence remains the registered real-database test suites and the earlier browser journeys explicitly recorded above. A normal patient navigation did not force focus; success/error return behavior has an automated assertion and success-focus exclusion was inspected in the script. Assistive-technology speech output and every-device compatibility are not claimed verified.

### Preservation and scope

The same aggregate preservation fingerprint used by Verify-Guardian was taken before and after the seven-role negative browser pass. All business-table counts/checksums and account identity, credential, active/lockout and authentication-version fields matched. Expected login/access telemetry is excluded. No synthetic population or cleanup was run. Existing patient, clinical, financial and staff records were not edited by these rejection tests. IIS Express was restarted with its existing configuration after it was found stopped; no host configuration was rewritten.

Ten additional StaffTaskExperience assertions passed in the focused run (**36 total**) for task return routing, malformed identifiers, doctor selection and the difference between patient success/error destinations.

### Final executed regression results

The existing verification process continued beyond the PasswordRecovery checkpoint and finished with **exit code 0**. Inspection of the external log and the completed runner confirmed the entire aggregate; no interrupted checkpoint was treated as a pass. `Tests/Verify-Guardian.ps1 -CheckMigrationIdempotence` successfully performed the **full Debug rebuild**, replayed **001–021**, executed all **18 registered suites / 845 assertions**, then passed DatabaseIntegrity and the account/business-data preservation comparison. The latter two checks are additional to the executable assertion count (847 PASS lines altogether).

| Suite | Passed assertions |
|---|---:|
| WorkflowIntegration | 70 |
| WorkflowGuards | 62 |
| AppointmentLifecycle | 99 |
| Reporting | 50 |
| SupplierReceipts | 19 |
| StockReconciliation | 15 |
| LaboratoryAmendments | 38 |
| ErrorHandling | 3 |
| StaffDirectories | 58 |
| StaffProfiles | 46 |
| AuthenticationGuards | 40 |
| PasswordRecovery | 63 |
| MedicalTeam | 32 |
| PatientExperience | 47 |
| StaffTaskExperience | 36 |
| SyntheticData — existing populated branch | 10 |
| WorkspaceRender | 19 |
| HttpRegression | 138 |

Log: `%TEMP%\guardian-final-ux-regression-20261019.log`, outside the web application and `bin`. JavaScript syntax checking and `git diff --check` also passed; existing Git LF/CRLF normalization warnings are not build failures. No test failure was waived.

The final preservation fingerprint additionally matched the snapshot taken before the browser rejection tests. Existing clinical/financial records and account identities, credentials, active states, lockouts and authentication versions remain unchanged. Expected login/access telemetry is excluded. The synthetic run is still **Ready**, with **271 original tracked records** and **18 accounts (seven development + 11 synthetic)**. Stock quantities remain 47 and 3, exactly matching their movement ledgers. The populated SyntheticData test exercised its safe existing-run/no-op branch, not destructive cleanup/repopulation. No retained regression fixture was detected. No new migration was introduced.

### Readiness boundary

The completed **functional MVP** means the bounded workflows in section 3, not every possible hospital-system capability. The completed **functional UX increment** means the high-priority patient/staff task improvements and feedback/selection fixes above, not a finished visual redesign. Optional queue grouping, contextual shortcuts, broader paging/filtering and the original partial/missing roadmap items in sections 12/14 remain explicitly documented.

**FUNCTIONAL MVP READY: YES. FUNCTIONAL UX READY: YES. READY FOR VISUAL REDESIGN: YES**, within the documented MVP scope. No known P0/P1 functional UX blocker remains in this verification pass. All registered checks passed, targeted seven-role desktop/mobile validation passed, and records/accounts were preserved. This does not upgrade partial/missing original-roadmap items to complete.

**Functional development is paused pending the owner's chosen design references.** No additional feature or visual redesign is started at this boundary. Clinical user acceptance, screen-reader/device testing, SMTP delivery, secure deployment/secrets/least privilege, excluding development identities and demonstration data, backup/restore rehearsal, document malware handling, clinical publication/privacy policy and independent security/performance testing remain production gates. Readiness for the final design phase must not be read as authorization for production use.

## 21. Authorized cinematic homepage/sign-in — 2026-10-20

The subsequent user brief authorized only the public homepage and sign-in redesign. The existing functional MVP, clinical services, authentication code-behind and role workspaces were preserved. New `guardian-cinematic.css`, `guardian-theme.js` and `guardian-cinematic.js` opt in through a public-page root class; no authenticated page imports them. The original generated anatomical artwork is retained, with explicit illustrative disclosure. Department/contact integration still uses existing encoded database values, and synthetic department entries are explicitly identified as development demonstration content. Protected doctor discovery still requires patient authentication.

The public experience now includes complete light/dark tokens and a visible keyboard-operable switch; first-visit OS selection, pre-stylesheet initialization, persisted explicit choice, cross-tab updates and blocked-storage fallback. Motion includes entrances, atmosphere, pointer parallax, scroll entry, hover, native anchor scrolling, pause/resume and reduced-motion handling. All content/links and the original Web Forms login remain usable without the animation script. The login retains original server controls, validation, role redirects and account/security behavior; its password visibility, submit feedback and focused error presentation are client-only enhancements.

### Confirmed corrections

- New ASPX non-ASCII presentation literals initially rendered as encoding artifacts. HTML entities corrected titles, arrows and quotes without changing global configuration.
- Mobile menu opening now focuses the first revealed link; Escape restores focus to the trigger. Login input/label grouping, form-first document order and input-boundary contrast were refined.
- The initial aggregate completed eleven suites, then PasswordRecovery failed after 47 assertions on a real HTTP 500. Browser reproduction identified missing `bin/roslyn/csc.exe`. The existing CodeDom package copy target restored the runtime, and a subsequent Debug rebuild copied it successfully. The initiating reason for the earlier missing output was not independently reproduced. A project build guard now rejects missing compiler/dependency output; a deliberate empty-source/output negative test confirmed rejection. Verify-Guardian also checks runtime presence and homepage/login/recovery HTTP startup before lengthy fixtures. No failure was hidden or waived.

The failed recovery run's guarded `finally` cleanup was confirmed by SQL: zero retained recovery fixture accounts, with all 18 existing accounts present. No production/business data repair or synthetic population was performed.

### Actual final aggregate

`Tests/Verify-Guardian.ps1 -CheckMigrationIdempotence` completed with **exit 0** after the fixes. Full Debug Rebuild and runtime preflight passed. **18 C# executable suites / 856 assertions plus PublicTheme / 19 assertions = 875 passed assertions**. DatabaseIntegrity and the preservation comparison also passed, giving 877 PASS lines including those aggregate checks.

| Suite | Passed assertions |
|---|---:|
| PublicTheme — isolated JavaScript | 19 |
| WorkflowIntegration | 70 |
| WorkflowGuards | 62 |
| AppointmentLifecycle | 99 |
| Reporting | 50 |
| SupplierReceipts | 19 |
| StockReconciliation | 15 |
| LaboratoryAmendments | 38 |
| ErrorHandling | 3 |
| StaffDirectories | 58 |
| StaffProfiles | 46 |
| AuthenticationGuards | 40 |
| PasswordRecovery | 63 |
| MedicalTeam | 32 |
| PatientExperience | 47 |
| StaffTaskExperience | 36 |
| SyntheticData — populated branch | 10 |
| WorkspaceRender | 19 |
| HttpRegression | 149 |

Final log: `%TEMP%\guardian-public-redesign-final-20261020.log`. The earlier failed log is preserved separately. No verification log was written into `bin`. JavaScript syntax checks and `git diff --check` passed. PublicTheme is now part of the aggregate and requires Node.js in the development verification environment.

Migrations **001–021** replayed; no new migration or database semantics were introduced. Aggregate fingerprints matched for business/clinical/financial data and account identities, credentials, activation, lockouts and authentication versions, excluding expected login/access telemetry. Synthetic provenance remains **Ready / 271 original manifest records / 18 accounts**. Existing demonstration data was not recreated, and no retained regression fixture was found.

### Browser and accessibility evidence

Both homepage and sign-in were inspected in both themes at 1440, 1024, 768 and 390 CSS pixels; no horizontal page overflow was found. An additional 320-pixel homepage reflow check passed. Screenshots include full homepage, both desktop heroes and responsive sign-in layouts. Theme persistence was observed through navigation and reload. Actual patient login returned to protected Medical Team; actual Admin login returned to its unchanged workspace. All seven role routes/guards were reverified by the HTTP suite, not claimed as seven new manual workspace walkthroughs.

Mobile and desktop failed logins used a nonexistent identity, preserving existing accounts; generic errors received visible focus, role selection remained and passwords were cleared. Empty form validation focused the required email control without entering a submitting state. Password visibility, keyboard form order, mobile menu entry/Escape, native disclosures, help/recovery links, pause/resume and actual parallax offsets were checked. Observed pages had no duplicate IDs, unlabelled form controls, broken images or captured console errors. Core contrast pairs were calculated; full assistive-technology certification is not claimed.

Detailed evidence, source/asset inventory, contrast values and checkpoint/screenshot locations are in **PUBLIC_DESIGN_QA.md**. Image generation supplied the original decorative artwork; browser inspection drove encoding, focus, grouping and contrast corrections. No workspace redesign was started.

### Acceptance boundary and remaining limitations

**PUBLIC HOMEPAGE + LOGIN IMPLEMENTED AND TECHNICAL QA PASSED.** Owner visual approval is still required. This is not production readiness and does not close any partial original-roadmap requirement above.

Remaining manual acceptance: screen-reader speech output, real OS-level reduced-motion presentation, browser zoom/high-contrast and wider browser/device coverage, throttled-network/low-end-device performance, and the existing clinical/deployment gates. Reduced-motion logic/CSS and OS theme choices are covered by isolated tests; this browser tool did not expose OS preference emulation. Recovery/support pages intentionally retain their existing design. There is no known outstanding reproducible runtime failure in the checked homepage/sign-in/recovery paths after the compiler repair.

**Stopped at the requested public-design boundary.** No further feature or authenticated-workspace redesign is authorized by this phase.

## 22. Approved Care Constellation public-design verification — 2026-10-21

**Status: IMPLEMENTED AND TECHNICALLY VERIFIED; awaiting owner visual approval.** This supersedes the visual direction of section 21, not its historical test evidence or the outstanding clinical/release limitations elsewhere in this document.

### Scope and safety

The approved Homepage/Login implementation combines the structured Care Constellation anatomy/access composition with cinematic atmosphere, layered localized light and editorial spacing. Existing anatomical artwork bytes are preserved. A uniform-scale double heartbeat has a strong first beat, softer second beat, rest, synchronized glow, explicit pause and reduced-motion fallback. Light and Dark Mode share the existing persistent public-only theme infrastructure. Login retains its existing controls, handler, validation and authorization behavior. Authenticated Patient and staff workspaces were not redesigned.

No backend, account credential, permission, migration, demo dataset or business workflow was changed during this final verification continuation. The user had already performed a local Patient recovery before the final test baseline. The supplied current Patient/Admin/Doctor credentials passed rollback-only real-service authentication preflight; no further reset or unlock was necessary. Credentials were used through masked input and child-process environment variables, never committed or documented.

The interrupted user verification stopped at the first development-credential preflight before a Patient result. Its exact exception message was lost to Windows PowerShell native-stderr redirection. The runner now drains native stdout/stderr asynchronously, preserves failure details and checks the exit code; WorkflowIntegration prints the role under verification without secrets. The final complete run passed with current credentials. Earlier interrupted attempts are not counted as passes.

### Final executed verification

Full Debug Rebuild, runtime preflight, migration replay **001–021**, all **18 executable suites / 856 assertions**, **PublicTheme / 27 assertions**, DatabaseIntegrity and Verify-Guardian preservation checks passed. The helper/aggregate process completed with exit 0. HomepageRegression subsequently passed **54 checks** (55 PASS lines including its final completion message). DatabaseIntegrity was rerun successfully after manual browser sign-ins. No credential-dependent tests remain blocked.

| Suite | Passed assertions |
|---|---:|
| WorkflowIntegration | 70 |
| WorkflowGuards | 62 |
| AppointmentLifecycle | 99 |
| Reporting | 50 |
| SupplierReceipts | 19 |
| StockReconciliation | 15 |
| LaboratoryAmendments | 38 |
| ErrorHandling | 3 |
| StaffDirectories | 58 |
| StaffProfiles | 46 |
| AuthenticationGuards | 40 |
| PasswordRecovery | 63 |
| MedicalTeam | 32 |
| PatientExperience | 47 |
| StaffTaskExperience | 36 |
| SyntheticData | 10 |
| WorkspaceRender | 19 |
| HttpRegression | 149 |
| PublicTheme — JavaScript | 27 |

Registered assertion total: **883**, separate from homepage checks, SQL integrity and the final preservation assertion. Complete aggregate evidence is `%TEMP%\guardian-constellation-user-verification-20261021-230509.log`; homepage evidence is `%TEMP%\guardian-constellation-homepage-final-20261021.log`; post-browser integrity is `%TEMP%\guardian-constellation-integrity-post-browser-20261021.log`. Logs remain outside the web application, not in `bin`.

The aggregate's before/after fingerprints matched for clinical, financial, administrative and historical tables and account identities, credentials, activation, lockouts and authentication versions. Expected access/login telemetry is explicitly excluded. Synthetic provenance remains **Ready / 271 tracked records / 18 accounts**; all seven development accounts remained active/unlocked with zero failed attempts at the final read-only check. Fixture-residue and constraint checks passed. No demo population was repeated.

### Browser verification and limitations

Actual Homepage/Login rendering was checked at 1440, 1024, 768, 390 and 320 CSS-pixel widths, with both themes across desktop/mobile and no observed horizontal overflow. Verified: theme persistence; primary/help routes; responsive menu Enter/Escape; native disclosures; skip links; labels/IDs; required fields; password visibility; rejected desktop/mobile sign-in focus, role retention and cleared password; successful Admin sign-in to existing Workspace; successful Patient sign-in returning to protected Medical Team; normal sign-out; public-theme isolation; loaded artwork and captured console diagnostics. Only these two role sign-ins are new manual authentication evidence; all seven role guards/routing were covered by HttpRegression, not claimed as new manual walkthroughs of every workspace.

Live heartbeat sampling confirmed two contraction/light peaks and rest with stable surrounding text. Both page pause controls were checked. Reduced-motion and background-tab behavior passed isolated tests; OS-level reduced-motion simulation and screen-reader speech were not performed. No physical-anatomy or low-end performance claim is made.

Four requested approval screenshots and mobile/motion evidence are referenced in `PUBLIC_DESIGN_QA.md`. JavaScript syntax and diff whitespace checks passed. No additional creative rewrite was performed during QA because no blocking layout defect was reproduced.

**Ready for public visual approval: YES.** Remaining limits are owner visual acceptance, real assistive-technology/zoom/high-contrast checks, broader device/browser and throttled-network testing, plus the existing clinical, SMTP, privacy, backup/restore, deployment and independent-security gates. Functional MVP readiness does not equal production readiness. Work stops here; no internal workspace redesign or GitHub push is authorized by this completion.
