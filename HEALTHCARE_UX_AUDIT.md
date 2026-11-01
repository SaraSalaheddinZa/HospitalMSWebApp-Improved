# Guardian Health healthcare workflow UX audit

Date: 17 October 2026. Scope: functional UX, not visual redesign. Existing Web Forms architecture, styles, clinical rules, accounts and populated synthetic dataset are retained.

## Method and evidence

Official public documentation was researched, not private authenticated third-party portals. Guardian Health's seven role dashboards were signed into and inspected in the running browser before implementation. Patient booking/change forms, worklists, navigation and source/service contracts were inspected. Database checks confirmed 18 accounts and the existing Ready synthetic run; no population was run. Prior regression evidence is 16 suites / 745 assertions; it is historical evidence, not a result of this change.

## External comparison and proposed backlog (written before implementation)

External descriptions below are documented capabilities. Guardian recommendations are our own adaptation, not a claim of equivalent healthcare policy or feature parity.

| Official source | Documented practice | Confirmed Guardian behavior / gap | Recommendation | Priority; backend; success test |
|---|---|---|---|---|
| [Mayo online scheduling walkthrough](https://www.mayoclinic.org/scheduling-an-appointment-online/vid-20521112) | Provider/time selection precedes review and explicit confirmation; visit details remain accessible afterward. | Doctor directory works, but expandable slots mix selection with submission; a competing clinic-session picker and schedule table remain on My health. | One patient booking route with separate selection, review and persistent appointment details. | P1; reuse MedicalTeam booking, no storage migration; browser search → review → confirm → details, concurrency/stale-slot tests. |
| [Cleveland Clinic scheduling guidance](https://my.clevelandclinic.org/resources/mychart-appointment-and-scheduling-troubleshooting) | Upcoming/past visits and appointment details organize eligible change/cancel actions and explanations when unavailable. | Future appointments sort farthest-first, past Booked appointments mix with future ones; cancellation/rescheduling use separate selectors. | Upcoming sorted soonest first, past/history separately, actions bound to one appointment with a confirmation step. Do not copy Cleveland's cancellation cutoff. | P1; owner-scoped read service and stale-state checks; ownership, replay, changed-time and lifecycle tests. |
| [NHS hospital appointments](https://www.nhs.uk/nhs-app/help/appointments/hospital-and-other-appointments/) | Rescheduling requests can remain pending and later be accepted or rejected. | Reception already accepts/declines requests and retains original reservations, but patients must inspect generic timeline entries to learn outcomes. | Show pending explanation and attributed appointment status history; retain asynchronous requests. | P1; authorized history projection, existing transition service; request → reception decision → patient outcome. |
| [Epic MyChart features](https://www.mychart.org/Features) | Public feature documentation describes appointment discovery, records, medication information, results and financial access; availability depends on provider. | Guardian exposes these areas, but My health first shows the complete appointment list rather than next actions. | Next appointment, unread messages, recent final results, prescription summary, balance and record/document shortcuts. No fabricated medical interpretation or medication-currentness claim. | P1; scoped real queries; empty and populated dashboard/render tests. Source search retrieved the official features text; direct page fetch failed. |
| [Cleveland MyChart navigation](https://my.clevelandclinic.org/resources/mychart-features-and-navigation-troubleshooting) | Messages, letters and notification preferences have distinct purposes. | Guardian has a real inbox and documents; duplicated notification read controls clutter the homepage. | Summarize unread items and link to the existing inbox; keep documents separate. Notification preferences/two-way clinical messaging are not implied. | P1 summary / P2 preferences; no delivery changes; ownership and deep-link checks. |
| [NHS test results help](https://www.nhs.uk/nhs-app/help/test-results/) | Patients may see results not yet discussed with a clinician; missing records have explicit help guidance. | Guardian releases finalized results and separately tracks doctor review; corrections are retained. | Label results as final, avoid equating finalization with clinical review, link full results and advise asking the care team for explanation. Do not import NHS release policy. | P1 copy/context; preserve release logic; draft exclusion and correction tests. |
| [Mayo records access](https://www.mayoclinic.org/patient-visitor-guide/medical-records) | Portal records and records-request channels are distinguished. | Patient 360 and secure documents exist; no verified visit location/preparation source. | Link existing records/documents; explicitly say location/instructions are not recorded instead of inventing them. | P1 context; no new medical content; missing-value render tests. |
| [Cleveland MyChart overview](https://my.clevelandclinic.org/online-services/mychart) | App/web access combines visits, results, prescriptions and billing. | Current responsive tabs exist but booking creates many forms and long tables on small screens. | Fewer task-focused forms, native labeled date controls, keyboard navigation, explicit timezone labels and mobile check. | P1; lightweight server-rendered changes, no new framework; mobile overflow, labels and keyboard QA. |

## Prioritization

| Item | Impact / frequency | Clinical or financial risk | Complexity / regression risk | Decision |
|---|---|---|---|---|
| Appointment-centered actions with stale-state rejection | High / frequent | High if a previously rescheduled appointment is cancelled unknowingly | Medium / medium | P1 implement first; no confirmed P0 exploit found |
| Single doctor-first booking and review | High / frequent | Scheduling integrity | Medium / medium | P1 implement |
| Summary-first My health | High / every login | Avoid implying prescriptions are an active medication list | Low / medium | P1 implement |
| Explicit hospital time and strict input normalization | High / every appointment | Wrong-time attendance | Medium / high if storage changes | P1 preserve UTC storage; configured display zone only; test day boundaries and DST |
| Reception row-context actions | High / frequent | Wrong-patient update | Medium / medium | P2 follow-up; server lifecycle guards already enforce correctness |
| Lab pending-only worklist and contextual progression | High / frequent | Wrong-result context | Medium / medium | P2 follow-up; separate final/amendment workflows already exist |
| Nurse row-to-assessment handoff | High / frequent | Loss of safety context | Medium / medium | P2 follow-up; persistent safety panel exists after selection |
| Pharmacy dependent batch selection | High / frequent | Wrong batch choice (service rejects) | Medium / medium | P2 follow-up; do not weaken stock checks |
| Admin exception-to-filter links | Medium / daily | Operational delay | Low / low | P2 follow-up |
| Animation, branding, layout polish | Low | None | Separate design scope | P3 deferred |

## Staff task audit

All six staff workspaces were inspected in the browser, with service/page source cross-checks. This was a task/navigation audit, not a new successful transaction in every department.

- **Reception:** today's queue, registration, finance and rescheduling tabs exist. Broad appointment/status selectors require re-selecting context and include historical unresolved appointments. Pending requests are visible in the queue. Keep current rules; prioritize contextual row actions later.
- **Doctor:** queue has patient/MRN, check-in prerequisite and record links. Results review and unsigned consultations have direct links. The queue mixes overdue and future entries without explicit date groups. Preserve safety context, pending reviews and sign-off controls.
- **Laboratory:** stage counts and collection/processing/final/correction tabs are clear. Default order worklist includes completed orders and many columns; pending-first filter would reduce scanning. Amendments must continue to preserve originals and re-open clinician review.
- **Nurse:** admitted worklist includes ward/room/bed/allergies; selection is repeated in a separate patient picker. Assessment, history and triage are available; no assigned-shift handoff/task scheduling is inferred from notes.
- **Pharmacist:** pending queue and low-stock/expiry counts work. Dispensing asks for a prescription and a global batch independently. A prescription-constrained batch selector would improve error prevention; existing service checks remain mandatory. Receiving, supplier receipts and reconciliation are separate workflows.
- **Admin:** real operational alerts, Command Center, admissions/beds, reports, audit and staff management are linked. Zero-count alerts consume attention and some links are generic rather than prefiltered. No automatic refresh or clinical decision-making is claimed.

## Time contract and safety boundaries

Inspection: appointments use SQL datetime2 values interpreted as UTC; schedules use UTC dates and times, availability and check-in use SYSUTCDATETIME/DateTime.UtcNow. Browser datetime-local has no inherent timezone. No verified hospital timezone/location setting exists in the current database. User/computer timezone is not evidence of hospital location.

Implementation must preserve UTC scheduling/storage and staff check-in policy. Patient presentation can use a server-configured hospital timezone, defaulting explicitly to UTC. No browser-local conversion by accident; parse canonical ISO input, reject nonexistent/ambiguous local times rather than guessing a DST occurrence. Appointment notifications must identify the time basis. Deployment owners must confirm the hospital timezone before enabling a different display zone; staff scheduling/check-in still uses the documented UTC policy.

## Verification record

The initial research above preceded implementation. Executed results and their boundaries are recorded below. Existing synthetic accounts, manifest and user-added appointments are preserved; the one explicitly labelled synthetic browser booking retains its cancellation/request history.

## Staff extension: evidence and selected incremental changes

The following public sources extend the initial patient research. They describe workflow principles, not access to private EHRs or proof that Guardian meets another jurisdiction's standards.

| Reference | Current confirmed friction | Selected adaptation and operational value | Complexity / regression risk / priority |
|---|---|---|---|
| [Oracle Health: open an inbox item](https://docs.oracle.com/en/industries/health/oracle-health-ehr/ehrug/open-item-from-items-list.html) describes opening an item with its associated patient and relevant chart context. | Reception, lab and nursing lists require reselecting the same record in disconnected forms. | Links from rows open the selected task with patient/MRN and current state; submit through existing authorized services. Fewer opportunities to choose a different patient accidentally. | Medium / medium / P1 |
| [Oracle Health: results](https://docs.oracle.com/en/industries/health/oracle-health-ehr/ehrfg/results.html) groups results and supports inbox endorsement; it explicitly does not provide diagnostic recommendations. | Lab default table mixes completed results with uncollected work; doctor pending-reschedule reservation disappears. | Pending-first lab stages with contextual progression; preserve correction/review paths. Restore pending-reschedule reservations in doctor queue and explain their state. | Medium / medium / P1; doctor defect confirmed and fixed |
| [Epic hospital patient flow](https://www.epic.com/software/hospital-patient-flow/) documents shared capacity visibility and actionable bed-planning/cleaning work. | Admin Command Center has real worklists, but operational summaries often navigate to generic reports and zero-count alerts dilute attention. | Queue-specific summary links, real status filtering and active-exception focus. No predictive model or automatic staffing/cleaning assignment is added. | Low / low / P1 |
| [NHS England PIER](https://www.england.nhs.uk/patient-safety/managing-acute-physical-deterioration-through-the-prevention-identification-escalation-response-pier-approach/) emphasizes reliable escalation/handover communication; [patient safety board case study](https://www.england.nhs.uk/atlas_case_study/safety-on-board-reducing-risk/) describes easier access to recorded risks. | Nurse table has allergies and location, but selecting a patient and admission again can separate the task from its context. | Direct assessment link, selected active-admission context, latest recorded care status and author/time. No inferred acuity, shift assignment or invented observation schedule. | Medium / medium / P1 |
| [NHS SPS safe pharmacy processes](https://sps.nhs.uk/articles/managing-medicines-safely-in-pharmacy/) emphasizes unambiguous medicine identification and stock checks; [expiry guidance](https://sps.nhs.uk/articles/managing-expired-stock-and-decommissioned-products/) recommends stock rotation and expiry monitoring. | Pharmacist chooses a prescription and unrelated global batch list; the service rejects mismatches only after submit. | Open a specific prescription, show patient/allergy/medication/remaining context, then offer only matching in-date stock sorted by expiry with quantity shown. Service revalidates every dispense. | Medium / medium / P1 |

These are focused functional UX improvements. Staff assignments, automated triage, full medication administration, barcode integrations, predictive analytics and workflow-policy changes are not introduced.

### Patient work completed before the staff extension

- Single doctor-first selection → separate review → confirm → persistent appointment details; raw clinic-session chooser removed from the patient dashboard, reception's scheduling functionality retained.
- Upcoming (soonest first) versus past/cancelled lists, contextual request/cancellation confirmation, pending explanation and real event history. Stale time, duration and state snapshots reject obsolete submissions.
- Summary-first My health with next appointment, three unread messages, three recent final results, three recent prescriptions, balance and details/inbox/records/documents links. Prescription summaries explicitly do not assert an active medication list.
- UTC storage retained. Strict ISO inputs, configured display-zone support, actual offsets, local-day availability boundaries and rejection of ambiguous/nonexistent DST inputs. The live installation still explicitly uses UTC; hospital location/timezone remains an owner configuration decision.
- Browser confirmed a labelled synthetic appointment's booking, pending request, doctor/reception visibility, reception decline, patient outcome and cancellation. Original reservation remained unchanged until cancellation. Its audit/history remains intentionally retained, not deleted as a transient fixture.
- The patient-phase aggregate passed: 17 executable suites / 801 assertions, integrity SQL, migration replay 001–021 and preservation comparison. Staff changes below require their own final aggregate, not reliance on that earlier pass.

## Staff task refinement — 18 October 2026 continuation

This continues the same working tree. No visual identity, homepage, palette, typography, database migration or account configuration was changed. Browser checks use existing synthetic records; no population was run. Dates in the application remain explicitly UTC, independent of the client date.

### Implemented before/after behavior

| Role | Confirmed prior problem | Implemented behavior / guard |
|---|---|---|
| Reception | Rows lacked contextual actions; the global status form offered a clinical-start choice and unrelated patients had to be reselected. | Row → selected appointment with patient/MRN, clinician, reserved time and current status. Only eligible attendance actions are shown, with no premature check-in or direct clinical-start button. Observed start/end/status are checked transactionally. Active selectors preserve the selected record and show meaningful patient/doctor/time information instead of leading with an internal ID. Existing booking and rescheduling services remain authoritative. |
| Doctor | Pending rescheduling requests disappeared even though the original reservation remained valid. | Pending request remains in the clinician queue with an explicit original-reservation/check-in explanation. Approved, declined, cancelled and completed outcomes have queue/projection tests; clinical attendance and ownership rules are unchanged. |
| Laboratory | Completed tests crowded the default worklist; stage and result forms required unrelated selections. | Default unfinished-item queue, explicit stage and patient/MRN search filters, oldest-first order. Row → patient/test/specimen/reference context → the one supported collection/processing step, or result entry only while Processing. Final results route to correction with original/history/version safeguards retained. Whole-order stage effects are explained. |
| Nurse | Assessment required selecting patient and admission independently of the admitted worklist. | Row opens the assessment tab for the selected patient's unique active admission. Safety summary remains visible, with ward/room/bed and latest recorded care status, author and UTC time. Discharged patients have no active-admission form. Invalid submissions retain patient/task context; discharge-readiness notes explicitly do not discharge or free beds. |
| Pharmacist | Prescription and global batch selection were disconnected. | Row opens the dispensing task with patient/MRN, encoded allergies, medicine/dose/frequency and remaining quantity. Only matching positive in-date batches are offered, expiry-first with available quantity. Selection survives validation failure; service stock/ledger/concurrency checks remain unchanged. Completed prescriptions expose no dispensing form. |
| Admin | Some command-center cards linked to another role's workspace or missing anchors; zero-count rules diluted exceptions. | Command-center cards link to Admin worklists. Actual status filter and active-alert default, with a show-all-rules option. The 200-record snapshot/filter scope is disclosed; it is not represented as an unlimited filtered search. Bed/claim/schedule handoffs remain Admin-authorized. |
| Patient | Fragmented booking/change forms and weak outcome visibility. | Unified booking and contextual details/history described above. Upcoming and past/cancelled navigation uses rectangular inline-block hit targets after mobile pointer testing exposed a wrapped-link issue. Existing past Booked records are not falsely labelled Completed. |

Shared worklist links now have rectangular pointer targets and specific task fragments; internal numeric keys remain in URLs but need not be table columns. Nurse/pharmacy errors and success feedback have accessible roles. A validation error receives focus after the task tab opens so it is not hidden above the fragment scroll target. This is functional accessibility, not a new visual system.

### Browser evidence and limits

- **Patient:** actual earlier browser booking → reschedule request → reception decline → patient history/notification → cancellation retained. This continuation additionally verified specialty filtering, real clinician availability, unavailable-date recovery, upcoming/past separation, cancelled details and 390-pixel mobile rendering. No additional appointment was booked in this continuation. Appointment approval/completion branches are automated, not newly submitted in the browser.
- **Reception:** actual future appointment opened through the selector and row path, correct clinician/MRN/time shown, no future check-in action. Registration directory and finance/claims navigation reviewed. Earlier request-decline POST remains the positive browser mutation evidence; existing unrelated appointments were not changed.
- **Doctor:** signed in, reviewed live queue, result-review page and consultations/notes navigation. Pending-request visibility was verified during the earlier synthetic booking sequence; fresh accepted/declined/cancelled/completed cases are rolled-back automated fixtures.
- **Laboratory:** active queue → selected uncollected synthetic test → explicit collection action; Completed filter → finalized result → correction context and revision form. No seeded pending order was advanced merely to empty the queue. Collection/processing/finalization writes and corrected-result re-review are automated (and the prior demo journey provides positive browser finalization evidence).
- **Nurse:** pointer and keyboard row navigation, assessment tab, persistent safety summary, actual bed and attributed care state verified. An invalid temperature was submitted and rejected; no assessment/vitals were saved, and selected patient/task remained. Positive new assessment/discharge transitions are rolled-back test evidence, not an additional browser treatment event.
- **Pharmacist:** pointer row → selected prescription → matching expiry-ordered batches. Over-dispensing was submitted and rejected; remaining prescribed quantity and stock remained unchanged. Supplier/reconciliation navigation reviewed. Positive dispensing/stock balance verified in rollback fixtures; the prior demo journey also provides browser dispensing evidence.
- **Admin:** command-center card → Beds → Occupied filter yielded the actual single occupied bed. Admission/transfer, readiness, scheduling, report and audit navigation inspected; no operational or financial record changed. Existing clinical roles are not granted to Admin.
- DOM checks on the inspected views found no duplicate IDs or unlabelled controls. Mobile patient screenshots and desktop staff review were performed. This is not a complete screen-reader or every-device acceptance certification. The wrapped nursing row link initially failed pointer activation; keyboard activation succeeded, then rectangular hit targets and task fragments were implemented and pointer navigation succeeded.

### Automated verification checkpoint

Focused PatientExperience **47** and StaffTaskExperience **26** assertions passed after fixture corrections. Rejected service calls deliberately roll back their surrounding test transaction; negative checks must be isolated or last, rather than continuing with invalidated fixture records. No production transaction code was weakened to accommodate a test. StaffTaskExperience is registered in Verify-Guardian.

The initial staff aggregate passed **18 suites / 835 assertions**, integrity, migration replay and preservation checks. It preceded the final feedback-timing correction; the final closure evidence is recorded below. Logs remain outside the web application's bin directory.

### Remaining prioritized work (not silently claimed complete)

- P2: explicit today/overdue/future grouping and paging for the doctor's capped queue; selected consultation/result links on older notes/results tables; wider server-side paging/search for large histories.
- P2: contextual rescheduling approval/decline forms and fuller department-specific task filters; current services are guarded, but some selectors remain separate.
- Manual acceptance: positive browser amendments/re-review, supplier receipt/reconciliation, claims settlement and populated secure documents; screen-reader and all-device review with clinical staff. Automated lifecycle coverage is not a substitute for these acceptance checks.
- Production gates remain actual SMTP, deployment secrets/HTTPS/least privilege, excluding development identities/data, backup/restore rehearsal, document malware handling, clinical privacy/publication policy and independent security/performance review. No claim of production readiness.

## Final functional UX closure — 19 October 2026

### Validation feedback and task continuity

Browser verification reproduced the remaining defect: a validation alert could have keyboard focus while native fragment restoration left it above the viewport. Merely moving focus during `load` did not reliably resolve it. Shared `guardian.js` now places the existing encoded error beside the selected task, preserves the fragment-selected workflow tab, assigns `role="alert"` and `tabindex="-1"`, and focuses/reveals it after initial page display. Both the feedback and task scroll destinations therefore contain the message. Feedback scrolling is instant and does not depend on animation; success notices are not selected by the focus handler. No CSS, visual identity or business rule changed.

The return-path audit also fixed these concrete continuity defects:

- Doctor consultation rejection now retains the validated patient and optional appointment identifiers, safety context and consultation tab.
- Laboratory amendments return to the selected finalized result and corrections tab; the result picker retains that selection.
- Reception rescheduling and decline return to their respective task sections with the appointment retained.
- Admin setting validation returns to Configuration.
- A rejected patient appointment request/cancellation reopens its corresponding form; successful requests return to appointment details instead.

Identifiers used in these local return routes are parsed as positive integers. No arbitrary return URL, authorization bypass, request-token bypass or new clinical/financial write path was introduced. Ten additional StaffTaskExperience assertions cover these routes, malformed identifiers and success/error differences; its focused **36 assertions passed**.

### Actual browser outcomes

All seven roles signed in and submitted deliberately invalid inputs at **1366 × 1000 desktop** and **390 × 844 mobile** widths. These were actual POST → validation → redirect → rendered response checks, not fabricated notices. All were rejected without a business-data change.

| Role | Rejected operation | Verified response |
|---|---|---|
| Pharmacist | Quantity above the prescription's remaining amount | Visible focused alert; same prescription, matching batches and Dispensing tab |
| Nurse | Out-of-range temperature | Visible focused alert; same patient, active admission/bed and Nursing assessment tab |
| Doctor | Out-of-range temperature in a consultation | Visible focused alert; same patient/safety context and Record consultation tab |
| Laboratorian | Overlength corrected result unit | Visible focused alert; same finalized result, original value/revision and Result corrections tab |
| Receptionist | Reschedule into the past | Visible focused alert; same appointment and Rescheduling tab |
| Admin | Overlength system setting value | Visible focused alert and Configuration tab; no setting saved |
| Patient | Preferred appointment window in the past | Visible focused alert; same appointment, unchanged reservation and request form still available |

The final observations confirmed focused alert text, containment below the fixed header and within the viewport, no page-width overflow, no duplicate HTML IDs and no unlabelled controls on these views. Keyboard Tab from pharmacy feedback reached its prescription selector; Tab from patient feedback reached the following navigation link. Arrow-key workflow navigation was verified on reception/pharmacy tabs. Patient access to an Admin page was denied. A normal patient details navigation left focus on the document, not a forced feedback target. Success-focus exclusion was additionally inspected in the shared script and success return routing was tested; no new successful clinical or financial transaction was performed solely for focus testing.

Business-table and account/security fingerprints matched before/after this browser pass, excluding expected login/access telemetry. The original synthetic dataset was not regenerated and no rejected clinical note, amendment, stock movement or setting persisted. Existing developer changes remain intact. IIS Express had stopped at the start of this continuation; it was restarted using the existing local configuration, without changing site settings.

This is browser/DOM and keyboard evidence, **not** an NVDA/JAWS/VoiceOver acceptance test or certification of every browser/device. Positive clinical transactions remain covered by the established real-database regression suites and earlier explicitly recorded browser journeys.

### Phase boundary

The functional MVP and the selected high-priority functional UX improvements are separate from the broader roadmap and production requirements. Remaining queue grouping, large-history paging and richer contextual shortcuts are optional P2 usability work for this boundary. Clinical staff acceptance, assistive-technology testing and the production gates above remain required.

The existing aggregate continued beyond PasswordRecovery and completed with **exit code 0**; it was not restarted or assumed complete from an intermediate log. `Verify-Guardian.ps1 -CheckMigrationIdempotence` performed the full Debug rebuild and passed **all 18 executable suites / 845 assertions**, plus DatabaseIntegrity and the preservation comparison (847 PASS lines including those two aggregate checks). PasswordRecovery passed 63, AuthenticationGuards 40, PatientExperience 47, StaffTaskExperience 36, MedicalTeam 32, SyntheticData 10, WorkspaceRender 19 and HttpRegression 138. The complete count table is in PROJECT_STATUS.md section 20. Migrations **001–021** replayed successfully. The log is outside the application at `%TEMP%\guardian-final-ux-regression-20261019.log`.

The final fingerprint also matched the earlier pre-browser snapshot, covering both this browser pass and the aggregate regression. The Ready run, 271 tracked records, seven development accounts and 11 synthetic accounts remain intact; no retained regression fixture was detected. Authentication/access telemetry and rolled-back identity-sequence gaps are expected and excluded from preservation claims.

**FUNCTIONAL MVP READY: YES. FUNCTIONAL UX READY: YES. READY FOR FINAL VISUAL REDESIGN: YES**, within the documented MVP boundaries. No known P0/P1 functional UX defect remains from this audit. This is not production readiness and does not close the documented partial original-roadmap capabilities. Functional development is paused pending the owner's chosen design references. No further feature development or redesign is authorized at this boundary.
