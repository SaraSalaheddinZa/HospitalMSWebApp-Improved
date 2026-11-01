# Public cinematic design — implementation and QA

Latest verification: **2026-10-21, approved Care Constellation implementation**. See the final section for current results and screenshots. The 2026-10-20 checkpoint below is historical evidence, not the current visual direction.

Date: 2026-10-20. Scope: the public homepage and existing sign-in only.

## Boundaries and safety

The existing working tree was inspected and preserved. No service, authentication code-behind, password handling, clinical workflow, database semantics or account state was redesigned. The original anatomical artwork remains the centerpiece. No new external library, CDN dependency or backend feature was added.

Before editing, copies of `Home.aspx`, `login.aspx` and the project file were saved under `%TEMP%\guardian-public-before-20261020`. This is a scoped file checkpoint, not a Git commit or database backup. Recommended Git checkpoint: review/stage only the intended presentation and test changes after checking the already-dirty repository for private configuration; do not blindly stage the entire tree. No commit or push was made.

## Implemented experience

- Homepage: cinematic anatomical hero; separate Patient Portal and Staff Login; protected doctor discovery; six-stage care journey; existing database-backed department list; illustrative care/discovery composition; native expandable portal explanations; patient/staff access and working help links.
- No fabricated measurements, ratings, clinicians, outcomes or contact details. Existing synthetic department names are accompanied by an explicit development-demonstration explanation.
- Login: original server control IDs/types, submit handler, role selection, autofill names, remembered-identity control and routes retained. The form comes first in document order. Its desktop layout is paired with the artwork; on mobile the form precedes the supporting illustration.
- Password visibility is an explicitly labelled local control. It returns to hidden after page display. Submission state does not disable the named Web Forms submit input. Server errors retain `role=alert`, receive predictable focus and are scrolled into view.
- Shared public theme: OS preference on first visit, locally saved explicit choice, cross-tab synchronization, blocked-storage fallback, initialization before CSS. Only pages with the `gh-cinematic` root class opt in. Patient and Admin browser destinations confirmed no new theme stylesheet/root class.
- Motion: staged entrances, atmospheric breathing, requestAnimationFrame-throttled pointer parallax, entry reveals, image/button interactions and native smooth anchors. Pause/resume works; background tabs pause ambience. Reduced motion removes animations/transitions and parallax. Content is never hidden pending a scroll observer.

## Confirmed defects and corrections

1. Initial new ASPX literals displayed mis-decoded arrows/quotes/dashes. Non-ASCII presentation literals now use HTML entities; the actual browser title, links and text were rechecked. No global encoding or authentication configuration was changed.
2. Mobile menu links precede the toggle in DOM order. Opening now focuses the first revealed link; Escape closes it and returns focus to the toggle. Keyboard behavior was browser-tested.
3. Remember-me input and label initially separated across the options row. A presentational wrapper now groups them. Form fields have stronger contrasting boundaries, and the login heading/form precedes the decorative story in document order.
4. The first aggregate stopped in PasswordRecovery with HTTP 500. Browser reproduction identified missing `bin/roslyn/csc.exe`, not a bad password, database migration or login parser change. The existing NuGet copy target restored the compiler; a subsequent full rebuild also copied it correctly. The initiating reason for the earlier missing output was not independently reproduced. The project now fails a build if the required runtime compiler/dependency is missing, rather than trusting the package's permissive copy task. A deliberate missing-source/output test verified this fail-fast guard.
5. Verify-Guardian now checks runtime compiler presence and GETs homepage/login/recovery after all compilation but before long fixture suites. PublicTheme is also executed by the aggregate. Recovery fixture cleanup ran after the failed test; SQL confirmed zero retained recovery fixture accounts and 18 existing accounts.

No exceptions were suppressed and no failing assertion was removed. The tool's initial disclosure text/role locator mismatch was not an application failure: the native `summary` elements were subsequently opened with pointer and keyboard.

## Browser evidence

Actual IIS Express rendering was inspected in the in-app Chromium browser:

| Check | Observed result |
|---|---|
| Homepage, light/dark, 1440/1024/768/390 widths | Hero/artwork and navigation rendered; no page-width overflow |
| Login, light/dark, 1440/1024/768/390 widths | Form remained usable; no page-width overflow; responsive form-first layout |
| Theme toggle and reload/navigation | Choice persisted between homepage and sign-in and across reload |
| Failed login, mobile and desktop | Unknown identity rejected; generic error visible and focused; selected role retained; password cleared |
| Empty login | Native required-field validation focused email; no submitting state set |
| Password visibility | Show/Hide changed input type and accessible state; no credential printed |
| Patient successful sign-in | Returned from protected homepage discovery link to Medical Team |
| Admin successful sign-in | Routed to existing Admin Workspace; public styling not applied |
| Forgot Password | Real recovery form opened from sign-in and returned normally; no real-user reset requested |
| Menu keyboard | Enter reveals/focuses first link; Escape closes and restores focus |
| Portal explanations | Native details/summary opened by click and Enter |
| Motion | Breathing animation running; pause/resume toggled computed animation state; pointer movement updated parallax offset |
| Assets/DOM/console | Loaded images had natural dimensions; no duplicate IDs or unlabelled form controls in inspected pages; no captured console warnings/errors |

Screenshots are outside the web application at `%TEMP%\guardian-public-design-20261020`, including desktop dark/light hero, full homepage, mobile views and login at each width. They contain public content/empty sign-in controls, not credentials or patient records.

Core color-pair contrast was calculated from the implemented sRGB tokens: light body 15.09:1, muted text on secondary surface 5.21:1, accent 4.81:1, primary button 6.05:1, input boundary 3.37:1; dark equivalents 18.39:1, 7.76:1, 11.68:1, 11.45:1 and 3.85:1. These spot checks are not comprehensive accessibility certification.

## Automated evidence

The first aggregate log is `%TEMP%\guardian-public-redesign-20261020.log`: eleven suites completed, then PasswordRecovery stopped after 47 PASS assertions with a real HTTP failure. Later suites and final integrity/preservation were not reached in that attempt.

The corrected full aggregate completed with **exit 0** in `%TEMP%\guardian-public-redesign-final-20261020.log`: **18 executable suites / 856 assertions plus PublicTheme / 19 assertions = 875**, followed by both DatabaseIntegrity and preservation checks. Full Debug Rebuild, runtime preflight and migrations 001–021 replay passed. The actual per-suite table is in PROJECT_STATUS.md section 21. These results are from the new successful run, not inferred from the earlier failure.

`node Tests/PublicTheme.js` separately passed 19 isolated assertions: OS defaults/change, explicit precedence, storage/reload/cross-tab behavior, invalid/blocked storage, public-only opt-in, reduced-motion state changes, explicit pause, hidden-tab pause and the CSS reduced-motion fallback. Eleven new HTTP assertions cover theme load order/local assets, access links, real department explanation, password/autofill/error hooks, legacy-theme isolation and anonymous booking protection.

## Asset provenance and implementation files

`Assets/Images/guardian-heart.png` was generated using the built-in image-generation tool (transparent background), not downloaded from a third-party medical website. The original generated output is retained outside the repository. Final asset: a 1024 × 1536 PNG, approximately 2.54 MB. The existing illustrative care image is reused and labelled. Decorative artwork has empty alt text and is hidden from assistive technology.

Generation brief: one original premium 3D sculptural anatomical heart, non-gory three-quarter view, opaline pearl ceramic/translucent frosted glass, silver-lavender shadows, restrained luminous cyan branching surface vessels, violet accents, architectural studio lighting, clean transparent silhouette, no labels, logos, UI, measurements, blood or people; suitable for near-black and off-white backgrounds. The artwork is expressly not a diagnostic diagram.

Changed implementation: `Views/Guest/Home.aspx`, `Views/login.aspx`, new `Content/guardian-cinematic.css`, `Scripts/guardian-theme.js`, `Scripts/guardian-cinematic.js`, the new heart asset and project content/runtime-build entries. Verification changes: `Tests/PublicTheme.js`, `Tests/HttpRegression.cs`, `Tests/Verify-Guardian.ps1`. Existing internal CSS/scripts and page code-behind were not rewritten.

## Remaining review and limits

- Owner visual approval is still required before any staff/patient workspace design phase.
- Reduced-motion behavior is covered by isolated logic/CSS tests and code inspection; OS-level motion-preference emulation was not exposed by this browser tool. Real assistive-technology speech output, every browser/device, browser zoom and high-contrast modes still need manual acceptance.
- No throttled-network or low-end-device performance benchmark was run. Local rendering was checked; optional image delivery optimization should preserve the approved artwork.
- An additional 320-pixel homepage reflow check passed without horizontal overflow; its header and action buttons wrap rather than compressing below their usable sizes. Native browser zoom remains a separate manual check.
- Recovery/support pages retain their existing presentation, intentionally outside this redesign scope.
- Original clinical/privacy, SMTP, deployment, document scanning, backup/restore, independent security and load-testing gates remain in PROJECT_STATUS.md. Public design completion is not production readiness.

## Approved Care Constellation completion — 2026-10-21

### Implementation and scope

The approved Care Constellation structure is implemented with Luminous Continuum atmosphere: a large central anatomical heart, editorial headline, two open access links, distinct patient/staff entry points, spatial connection lines and layered architectural lighting. Login uses the same environment with a readable foreground form; tablet/mobile layouts put the form first. The supporting care journey, database department directory, illustrative care scene, native disclosures, help links and footer remain functional. No invented measurements or hospital claims were added.

The original `guardian-heart.png` is byte-for-byte unchanged (also enforced by PublicTheme). The new atmosphere image is a decorative background, not a replacement heart or clinical data. Public styling remains opt-in through `gh-cinematic`; actual Patient and Admin destinations were checked and do not load the public stylesheet. No internal workspace, authentication code-behind, service, credential, migration or clinical workflow was changed in this verification continuation.

### Credential issue and verification-runner correction

The earlier default-credential aggregate attempts were not passes. The user-run log ending `172544` reached migrations, build, runtime preflight and PublicTheme, then stopped at the first development-credential assertion before any Patient credential result. Admin is first in the observed account-query order. Windows PowerShell native-stderr handling prevented the useful assertion exception from reaching that log; the submitted input was intentionally not retained, so its exact value cannot be reconstructed.

The user completed Patient recovery locally before this final verification. No additional reset was performed or needed. A temporary helper outside the web application exercised the real AuthenticationService for Patient, Admin and Doctor inside rollback-only transactions. All three passed without persisting login counters, credential upgrades or security-state changes. Credentials were entered through masked console input and supplied only to the verification child process through the existing environment-variable mechanism. No passwords or reset tokens are stored in this document or repository.

`Verify-Guardian.ps1` now captures executable stdout and stderr directly, draining both asynchronously, preserving failure details and exit codes instead of losing them to PowerShell error formatting. WorkflowIntegration announces the role being checked without exposing submitted credentials. The complete updated runner subsequently succeeded.

### Actual automated results

- Full Debug Rebuild and ASP.NET runtime preflight: passed.
- `Verify-Guardian -CheckMigrationIdempotence`: passed, process exit 0; all **18 executable suites / 856 assertions** completed, including WorkflowIntegration, WorkflowGuards, AppointmentLifecycle, Reporting, SupplierReceipts, StockReconciliation, LaboratoryAmendments, ErrorHandling, StaffDirectories, StaffProfiles, AuthenticationGuards, PasswordRecovery, MedicalTeam, PatientExperience, StaffTaskExperience, SyntheticData, WorkspaceRender and HttpRegression.
- PublicTheme: **27 assertions passed**, included in the aggregate. Total registered executable/JavaScript assertions: **883**.
- HomepageRegression: **54 checks passed**, plus its completion message (55 PASS-prefixed log lines). Run separately again with complete output captured outside the application.
- DatabaseIntegrity: passed in the aggregate and again after browser checks.
- Migration scripts **001–021** replayed successfully; before/after business-data and account-security fingerprints matched, excluding expected audit/login access telemetry.
- Synthetic dataset: **Ready, 271 tracked records, 18 accounts**. Seven development accounts remain active and unlocked with zero failed-attempt counts at the final metadata check. No regression fixtures were retained according to the aggregate and integrity checks.
- JavaScript syntax checks and `git diff --check`: passed. Git's existing LF/CRLF advisory warnings are not build/test failures.

Evidence outside the web application:

- `%TEMP%\guardian-constellation-user-verification-20261021-230509.log` — completed aggregate.
- `%TEMP%\guardian-constellation-homepage-final-20261021.log` — completed homepage checks.
- `%TEMP%\guardian-constellation-integrity-post-browser-20261021.log` — post-browser integrity.
- `%TEMP%\guardian-private-credential-check-20261021.log` — non-secret rollback-only authentication outcomes.

No credential-dependent suite remains skipped or blocked in this final run. Preservation is relative to the post-recovery baseline: the user's earlier intentional Patient recovery is not misrepresented as an unchanged password across the entire history.

### Actual browser evidence

The implemented pages, not static studies, were inspected in the in-app Chromium browser at desktop 1440, tablet 1024/768, mobile 390 and narrow 320 CSS-pixel widths. Both themes were inspected across desktop and mobile; no horizontal page overflow was found at these checkpoints. The mobile header and buttons wrap at 320 rather than clipping. Login retains a readable form-first layout.

Observed checks: theme switching and persistence across public navigation/reload; menu opening with Enter and Escape returning focus; native disclosure keyboard activation; skip-link focus; associated form labels and no duplicate IDs; required email validation; Show/Hide password state; generic failed login at desktop/mobile with visible focused `role=alert`, retained role and cleared password; actual successful Admin login to Workspace; actual successful Patient login returning to protected Medical Team; normal sign-out; and no public-theme leakage into either authenticated destination. HomepageRegression additionally GET-tested all public CTA/help targets and exact database-backed department output. Inspected images loaded and the captured browser console had no warnings/errors.

The live heartbeat was sampled over more than two cycles: a stronger first contraction (~0.96 scale), smaller second contraction (~0.98), and a rest interval in a **1.68-second cycle**. Local glow peaks follow the two beats. The artwork's transform uses uniform scale plus a fraction-degree tilt, preserving its proportions; this is an achievable stylized bitmap effect, not a claim of physically accurate anatomical deformation. Sampled heading position stayed constant. Both pages' pause controls returned the heart/glow to a stable rest and resumed correctly. Isolated tests cover reduced-motion preference changes, steady fallback light, parallax reset and hidden-tab pausing. Real OS reduced-motion/screen-reader emulation was not available through this browser tool and is not claimed as executed.

Current token contrast spot checks (not certification): light body 12.54:1, muted text on secondary surface 5.35:1, primary button 6.99:1, input boundary 3.37:1; dark equivalents 17.04:1, 8.68:1, 11.49:1 and 3.82:1. Photography/gradient overlays still need human accessibility acceptance across more devices.

Final browser screenshots are outside the application at `%USERPROFILE%\.codex\visualizations\2026\09\27\01a0e22a-897b-7d33-94ca-8ae0a0569c93`:

- `implemented-home-dark.jpg`
- `implemented-home-light.jpg`
- `implemented-login-dark.jpg`
- `implemented-login-light.jpg`

Additional mobile/error captures and `implemented-heartbeat-evidence.json` are in the same folder. The four approval images contain public content and empty sign-in controls, not credentials or patient records.

### Approval boundary

**Public redesign is technically verified and ready for owner visual approval.** No known reproducible blocking defect remains in these checked public-page paths. This is not a promise of exhaustive accessibility, device-performance, clinical or production certification. Remaining manual checks: screen-reader speech output, real OS reduced-motion/high-contrast/zoom, broader browser/device coverage and throttled/low-end performance. Existing SMTP/release/security/clinical acceptance blockers remain unchanged. No further workspace redesign or GitHub push was performed.
