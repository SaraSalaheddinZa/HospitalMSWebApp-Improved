(function () {
    'use strict';
    var main=document.getElementById('main-content'), menu=document.querySelector('.gh-menu'), sidebar=document.querySelector('.gh-sidebar');
    function syncMenu() {
        if(!menu||!sidebar)return;
        var mobile=window.matchMedia('(max-width:800px)').matches, open=mobile?document.body.classList.contains('gh-mobile-open'):!document.body.classList.contains('gh-collapse');
        menu.setAttribute('aria-expanded',String(open));sidebar.inert=!open;
    }
    if(menu)menu.addEventListener('click',function(){document.body.classList.toggle(window.matchMedia('(max-width:800px)').matches?'gh-mobile-open':'gh-collapse');syncMenu();});
    window.addEventListener('resize',syncMenu);syncMenu();
    document.addEventListener('click',function(e){if(menu&&sidebar&&document.body.classList.contains('gh-mobile-open')&&!sidebar.contains(e.target)&&!menu.contains(e.target)){document.body.classList.remove('gh-mobile-open');syncMenu();}});
    document.addEventListener('keydown',function(e){
        if(e.key!=='Tab'||!menu||!sidebar||!document.body.classList.contains('gh-mobile-open'))return;
        var links=Array.from(sidebar.querySelectorAll('a[href],button:not([disabled])')).filter(function(el){return el.getClientRects().length;});
        var last=links[links.length-1];if(!last)return;
        if(e.shiftKey&&document.activeElement===menu){e.preventDefault();last.focus();}
        else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();menu.focus();}
    });
    document.addEventListener('keydown',function(e){if(e.key==='Escape'&&document.body.classList.contains('gh-mobile-open')){document.body.classList.remove('gh-mobile-open');syncMenu();menu.focus();}});
    document.querySelectorAll('.gh-sidebar a').forEach(function(a){a.addEventListener('click',function(){document.body.classList.remove('gh-mobile-open');syncMenu();});});
    document.querySelectorAll('input[required],select[required],textarea[required]').forEach(function(input){
        var label=input.closest('label')||(input.id&&document.querySelector('label[for="'+input.id+'"]'));
        if(label&&!label.querySelector('.gh-required')){var star=document.createElement('span');star.className='gh-required';star.textContent=' *';star.setAttribute('aria-hidden','true');label.insertBefore(star,label.firstChild?label.firstChild.nextSibling:null);}
    });
    document.querySelectorAll('.alert-box').forEach(function(a){if(!a.textContent.trim())a.hidden=true;else a.setAttribute('role','alert');});
    document.querySelectorAll('select[required]').forEach(function(select,i){
        if(Array.from(select.options).some(function(option){return option.value!=='';}))return;
        var note=document.createElement('small');note.className='gh-form-help gh-select-empty';note.id='empty-selection-help-'+i;note.setAttribute('aria-hidden','true');
        var role=document.body.dataset.role,name=select.name;
        note.textContent=name==='itemId'&&role==='Laboratorian'?'No tests are ready for results. Orders must be collected and processing first.':
            name==='patientId'&&role==='Nurse'?'No admitted patients are available. Admission and bed allocation are managed by hospital operations.':
            name==='batchId'?'No available, unexpired medication batches. Review inventory and receiving.':
            name==='medicationId'?(role==='Pharmacist'?'No medications are available in the catalog. Review Stock & suppliers.':'No medications are available in the catalog. Contact pharmacy for assistance.'):'No matching records are available for this step yet.';
        select.insertAdjacentElement('afterend',note);select.setAttribute('aria-describedby',((select.getAttribute('aria-describedby')||'')+' '+note.id).trim());
    });
    // Keep the named patient's safety context within reach on long clinical forms.
    var context=main&&main.querySelector('.gh-patient-context');
    if(context&&'IntersectionObserver' in window){
        context.id=context.id||'patient-safety-context';
        var safety=document.createElement('aside'),identity=document.createElement('strong'),allergy=document.createElement('span'),jump=document.createElement('a');
        var patientName=context.querySelector('h2').textContent,allergyValue=context.querySelector('.gh-allergy dd').textContent;
        safety.className='gh-safety-strip';safety.hidden=true;safety.setAttribute('aria-label','Current patient safety reminder');
        identity.textContent=patientName;allergy.textContent='Allergies: '+allergyValue;jump.href='#'+context.id;jump.textContent='Patient summary ↗';
        safety.append(identity,allergy,jump);main.appendChild(safety);
        var observer=new IntersectionObserver(function(entries){var e=entries[0];safety.hidden=e.isIntersecting||e.boundingClientRect.bottom>0;},{threshold:0});observer.observe(context);
        var currentPatient=new URLSearchParams(location.search).get('patientId');
        main.querySelectorAll('form[method=post] select[name=patientId]').forEach(function(select){
            var warning=document.createElement('p');warning.className='gh-context-warning';warning.hidden=true;warning.setAttribute('role','status');select.closest('label').appendChild(warning);
            select.addEventListener('change',function(){var changed=select.value&&currentPatient&&select.value!==currentPatient;
                warning.hidden=!changed;warning.textContent='The displayed safety summary belongs to '+patientName+'. Open the selected patient’s record to review their safety information.';
                safety.classList.toggle('is-mismatch',!!changed);identity.textContent=changed?'Patient selection changed':patientName;
                allergy.textContent=changed?'Review the selected patient’s safety information before documenting care.':'Allergies: '+allergyValue;
            });
        });
    }
    function slug(s){return s.toLowerCase().replace(/[^a-z0-9]+/g,'-').replace(/^-|-$/g,'');}
    var panels=main?Array.from(main.querySelectorAll('.gh-panel,.panel')):[], path=location.pathname.toLowerCase(), groups=[], tabs;
    if(path.endsWith('/patient/workspace.aspx')){
        var legacyPatientRoutes={'#my-appointments':'Appointments.aspx','#cancel-an-appointment':'Appointments.aspx','#request-rescheduling':'Appointments.aspx','#book-appointment':'MedicalTeam.aspx','#available-clinician-schedules':'MedicalTeam.aspx'};
        if(legacyPatientRoutes[location.hash]){location.replace(legacyPatientRoutes[location.hash]);return;}
    }
    panels.forEach(function(p,i){var h=p.querySelector('h2');if(!p.id&&h)p.id=slug(h.textContent);});
    var emptyCopy={
        'my-consultation-queue':['No appointments waiting','Your scheduled and checked-in patients will appear here.'],
        'admitted-patients':['No active admissions','Patients will appear here after admission and bed allocation.'],
        'prescription-dispensing-queue':['No prescriptions waiting','Prescriptions with medication still to dispense appear here.'],
        'inventory-and-alerts':['No stock batches recorded','Set up the medication catalog and suppliers, then receive a stock batch.'],
        'order-worklist':['No laboratory orders to display','New orders from clinical consultations appear in this worklist.'],
        'finalized-result-history':['No finalized results yet','Completed results retain their reference ranges, flags and recording history.'],
        'my-appointments':['No appointments to display','Use Book or change to see the published appointment availability.'],
        'today-s-appointments-and-patient-search':['No matching appointments','Today’s appointments appear here. Search by patient name or medical record number to find other dates.'],
        'patient-rescheduling-requests':['No requests waiting','Requests submitted through the patient portal will appear here.'],
        'emergency-queue':['No active triage assessments','New assessments will appear in priority order.'],
        'invoices-and-balances':['No invoices recorded','Create an invoice for a patient to add charges and record payments.'],
        'active-admissions':['No active admissions','Admitted patients and their current bed allocations will appear here.'],
        'collections-by-day-and-method':['No collections in this period','Choose another reporting period, or return after payments have been recorded.'],
        'invoices-created-in-period-current-balances':['No invoices in this period','This report summarizes invoices created within the selected dates.'],
        'current-medication-stock-exceptions':['No stock exceptions to display','This list uses current batch quantities, minimum levels and expiry dates.']
    };
    panels.forEach(function(p){var empty=p.querySelector('.gh-empty'),copy=emptyCopy[p.id];if(empty&&copy){empty.querySelector('strong').textContent=copy[0];empty.querySelector('p').textContent=copy[1];}});
    // Presentation-only groups. Existing forms, handlers, validation and request tokens remain unchanged.
    if(path.endsWith('/admin/workspace.aspx'))groups=[
        ['People',['staff-directory','nurse-and-pharmacist-access','nurse-and-pharmacist-profiles','create-nurse-or-pharmacist']],
        ['Departments',['departments']],['Wards & beds',['ward-room-and-bed-directory','create-ward','create-room','create-bed']],
        ['Configuration',['system-configuration']],['Audit review',['audit-review']]];
    else if(path.endsWith('/admin/operations.aspx'))groups=[
        ['Admissions',['active-admissions','admit-patient','discharge','transfer-to-an-available-bed']],
        ['Bed readiness',['confirm-bed-cleaned-and-ready','bed-utilization']],['Scheduling',['publish-clinician-availability']],
        ['Lab catalog',['laboratory-catalog']],['Insurance claims',['insurance-decisions-and-settlement']],
        ['History & summaries',['operational-history','monthly-collections','appointment-outcomes']]];
    else if(path.endsWith('/admin/reports.aspx'))groups=[
        ['Financial',['collections-by-day-and-method','invoices-created-in-period-current-balances','insurance-claims-submitted-in-period','insurance-decision-turnaround','current-receivables-aging']],
        ['Appointments',['appointments-scheduled-in-period']],
        ['Clinical & admissions',['laboratory-orders-placed-in-period','admissions-started-in-period','emergency-assessments-in-period','clinical-sign-off-accountability','recorded-patient-flow-transitions']],
        ['Stock exceptions',['current-medication-stock-exceptions','medication-movements-in-period']]];
    else if(path.endsWith('/receptionist/workspace.aspx'))groups=[
        ["Today & queue",['today-s-appointments-and-patient-search','queue-and-appointment-status','patient-rescheduling-requests']],
        ['Book appointment',['book-appointment','published-availability']],['Rescheduling',['reschedule-appointment','decline-rescheduling-request']],
        ['Patient access',['create-patient-portal-access']],['History',['recent-appointment-history']]];
    else if(path.endsWith('/receptionist/finance.aspx'))groups=[
        ['Invoices',['invoices-and-balances','create-invoice','add-invoice-line','invoice-item-history']],
        ['Payments',['record-payment']],['Providers & policies',['insurance-providers','policy-eligibility','add-policy','policy-availability']],['Claims',['record-claim','claims','insurance-record-history']]];
    else if(path.endsWith('/laboratorian/workspace.aspx'))groups=[
        ['Order worklist',['order-worklist']],['Collect & process',['sample-collection-and-processing']],
        ['Enter results',['finalize-result']],['Completed results',['finalized-result-history']],['Result corrections',['correct-a-finalized-result','result-correction-history']]];
    else if(path.endsWith('/pharmacist/workspace.aspx'))groups=[
        ['Dispensing',['prescription-dispensing-queue','dispense-prescription']],['Inventory & receiving',['inventory-and-alerts','receive-stock']],['Stock movements',['stock-movement-history']]];
    else if(path.endsWith('/pharmacist/suppliers.aspx'))groups=[
        ['Suppliers',['suppliers','add-or-update-supplier','supplier-availability','supplier-history']],['Deliveries',['receive-supplier-delivery','supplier-delivery-history']],['Medication catalog',['medication-catalog','add-medication']],['Reconciliation',['reconcile-physical-stock-count','reconciliation-history']]];
    else if(path.endsWith('/patient/workspace.aspx'))groups=[
        ['Overview',['next-appointment','unread-messages','recent-test-results','recent-prescriptions','your-health-information']],
        ['My care',['my-medical-timeline','prescriptions','laboratory-results']],['Billing',['invoices','payments','insurance-coverage-and-claims']],['Profile & messages',['my-profile','notifications']]];
    else if(path.endsWith('/doctors/clinicalworkspace.aspx'))groups=[
        ['Queue & patient context',['my-consultation-queue','patient-record','medical-timeline']],['Record consultation',['record-consultation']]];
    else if(path.endsWith('/nurse/workspace.aspx'))groups=[
        ['Admitted patients',['admitted-patients','patient-record']],['Nursing assessment',['nursing-assessment']],['History',['vital-signs-history','patient-timeline','nursing-history']]];
    else if(path.endsWith('/nurse/emergency.aspx'))groups=[
        ['Active queue',['emergency-queue','progress-care']],['New assessment',['record-assessment']],['Resolved',['recent-resolved-assessments']]];
    else if(path.endsWith('/doctors/notes.aspx'))groups=[
        ['Consultations',['my-consultations','edit-an-open-consultation']],['Clinical orders',['add-prescription-to-consultation','add-laboratory-test-to-consultation','cancel-an-unprocessed-prescription','cancel-an-uncollected-laboratory-order']],['Sign & add notes',['finalize-consultation','append-an-addendum']],['Follow-ups',['upcoming-follow-ups']],['Note history',['addendum-history','draft-revision-history']]];
    else if(path.endsWith('/records/index.aspx'))groups=[
        ['Care journey',['care-journey-outstanding-actions','current-care-tasks','appointments']],['Clinical summary',['consultations-and-care-plans','diagnoses','observations']],
        ['Medicines',['medicines-and-prescriptions','historical-prescriptions']],['Laboratory',['final-laboratory-results','laboratory-correction-history']],
        ['Admissions & notes',['admissions-and-discharge-summaries','signed-note-addenda','nursing-care','emergency-assessments']],['Billing',['billing-summary','insurance-claims']],['Timeline',['timeline']]];
    function activate(index,focus) {
        if(!tabs)return;
        var members=groups[index][1];
        panels.forEach(function(p){var controlled=groups.some(function(g){return g[1].includes(p.id);});if(controlled)p.hidden=!members.includes(p.id);});
        tabs.querySelectorAll('button').forEach(function(b,i){b.classList.toggle('is-active',i===index);b.setAttribute('aria-selected',String(i===index));b.tabIndex=i===index?0:-1;});
        if(focus)tabs.querySelectorAll('button')[index].focus();
    }
    if(groups.length&&panels.length) {
        groups=groups.filter(function(g){return g[1].some(function(id){return document.getElementById(id);});});
        tabs=document.createElement('div');tabs.className='gh-workflow-tabs';tabs.setAttribute('role','tablist');tabs.setAttribute('aria-label','Workspace sections');
        groups.forEach(function(g,i){var b=document.createElement('button');b.type='button';b.id='workflow-tab-'+i;b.setAttribute('role','tab');b.textContent=g[0];b.setAttribute('aria-controls',g[1].filter(function(id){return document.getElementById(id);}).join(' '));b.addEventListener('click',function(){activate(i,false);});b.addEventListener('keydown',function(e){if(['ArrowRight','ArrowLeft','Home','End'].includes(e.key)){e.preventDefault();var next=e.key==='Home'?0:e.key==='End'?groups.length-1:(i+(e.key==='ArrowRight'?1:groups.length-1))%groups.length;activate(next,true);}});tabs.appendChild(b);});
        var first=panels.find(function(p){return groups.some(function(g){return g[1].includes(p.id);});});
        if(first){var parent=first.parentElement; if(parent!==main&&parent.id==='audit')first=parent;first.insertAdjacentElement('beforebegin',tabs);}
        activate(0,false);
    }
    function openHash() {
        var id;try{id=decodeURIComponent(location.hash.slice(1));}catch(e){return;}
        var target=document.getElementById(id);
        if(!target)return;
        var index=groups.findIndex(function(g){return g[1].some(function(id){var p=document.getElementById(id);return p&&(p===target||p.contains(target));});});
        if(index>=0)activate(index,false);
        if(target.tagName==='DETAILS')target.open=true;
        target.scrollIntoView({block:'start'});
        document.querySelectorAll('.gh-nav-link').forEach(function(a){var same=a.pathname===location.pathname&&a.hash===location.hash;a.classList.toggle('is-active',same);if(same)a.setAttribute('aria-current','location');else a.removeAttribute('aria-current');});
    }
    window.addEventListener('hashchange',openHash);
    document.querySelectorAll('a[data-appointment]').forEach(function(link){link.addEventListener('click',function(){var panel=document.getElementById(link.hash.slice(1));var select=panel&&panel.querySelector('select[name=appointmentId]');if(select)select.value=link.dataset.appointment;});});
    document.querySelectorAll('a[href*="#"]').forEach(function(a){a.addEventListener('click',function(){if(a.pathname===location.pathname&&a.hash===location.hash)openHash();});});
    if(location.hash)openHash();
    // PRG keeps the selected task open, but validation feedback must not be left above the scroll target.
    document.querySelectorAll('.notice.error').forEach(function(notice){notice.setAttribute('role','alert');});
    document.querySelectorAll('.notice.success').forEach(function(notice){notice.setAttribute('role','status');});
    var taskError=main&&main.querySelector('.notice.error,.alert-danger');
    if(taskError){
        // Keep feedback beside the selected task. Browsers can restore a fragment after
        // pageshow (notably PRG to the same URL); both scroll destinations must contain it.
        var taskId;try{taskId=decodeURIComponent(location.hash.slice(1));}catch(e){taskId='';}
        var errorTask=taskId&&document.getElementById(taskId);
        if(errorTask&&main.contains(errorTask)&&errorTask!==main&&!errorTask.contains(taskError))errorTask.insertBefore(taskError,errorTask.firstChild);
        taskError.tabIndex=-1;
        // Native fragment scrolling can run after deferred scripts. Keep the task tab selected,
        // then announce and reveal its error after the page has completed that initial navigation.
        var revealTaskError=function(){requestAnimationFrame(function(){
            taskError.focus({preventScroll:true});
            taskError.scrollIntoView({block:'center',behavior:'instant'});
        });};
        // Run after initial display; contextual placement also handles later fragment restoration.
        // Do not refocus when returning from browser history, or on successful saves.
        if(document.readyState==='complete')revealTaskError();else window.addEventListener('pageshow',function(e){if(!e.persisted)revealTaskError();},{once:true});
    }
    if(!groups.length&&panels.length>3&&main){
        var nav=document.createElement('nav');nav.className='gh-section-nav';nav.setAttribute('aria-label','On this page');
        panels.forEach(function(p){var h=p.querySelector('h2');if(!h)return;var a=document.createElement('a');a.href='#'+p.id;a.textContent=h.textContent;nav.appendChild(a);});
        var header=main.querySelector('.gh-page-header');if(header)header.insertAdjacentElement('afterend',nav);
    }
    var destinations={
        'Active accounts':'#staff-directory','Occupied beds':'#ward-room-and-bed-directory','Pending laboratory orders':'/Views/Admin/Reports.aspx',
        'Outstanding balance':document.body.dataset.role==='Patient'?'#invoices':'/Views/Admin/Reports.aspx',
        "Today's appointments":document.body.dataset.role==='Doctor'?'#my-consultation-queue':'#today-s-appointments-and-patient-search',
        'Waiting':path.endsWith('/nurse/emergency.aspx')?'#emergency-queue':'#queue-and-appointment-status','Under care':'#emergency-queue','Acuity 1–2 active':'#emergency-queue','In consultation':'#queue-and-appointment-status','Active patients':'/Views/Receptionist/Patients.aspx',
        'Waiting patients':'#my-consultation-queue','Results to review':'/Views/Doctors/Results.aspx','Open consultations':'/Views/Doctors/Notes.aspx',
        'Admitted patients':'#admitted-patients','Triage waiting':'/Views/Nurse/Emergency.aspx','Assessments today':'#nursing-assessment',
        'Prescriptions waiting':'#prescription-dispensing-queue','Low-stock batches':'#inventory-and-alerts','Expiring in 30 days':'#inventory-and-alerts','Dispensations today':'#stock-movement-history',
        'Awaiting collection':'#order-worklist','Collected':'#sample-collection-and-processing','Processing':'#finalize-result','Completed':'#finalized-result-history',
        'Upcoming appointments':'/Views/Patient/Appointments.aspx','Unread notifications':'/Views/Notifications/Index.aspx'
    };
    // Admin indicators stay within Admin authorization; clinical work remains with its department.
    if(path.endsWith('/admin/commandcenter.aspx')){
        destinations={};
        [['Appointments today','Appointments'],['Waiting patients','Appointments'],['Open consultations','Clinical'],['Pending lab orders','Laboratory'],['Prescriptions waiting','Pharmacy'],['Occupied beds','Beds'],['Unsettled insurance claims','Insurance'],['Outstanding balance','Billing']].forEach(function(item){destinations[item[0]]='CommandCenter.aspx?queue='+item[1];});
    }
    document.querySelectorAll('.gh-stat').forEach(function(card){
        var label=card.querySelector('.gh-stat-label'),target=label&&destinations[label.textContent];if(!target)return;
        if(target.charAt(0)==='#'&&!document.getElementById(target.slice(1)))return;
        var a=document.createElement('a');a.href=target;a.setAttribute('aria-label','View '+label.textContent.toLowerCase());a.innerHTML='<span aria-hidden="true">↗</span>';card.appendChild(a);
        a.addEventListener('click',function(){if(a.hash===location.hash)openHash();});
    });
    document.querySelectorAll('.gh-table').forEach(function(table){
        var rows=Array.from(table.querySelectorAll('tbody tr')).filter(function(r){return r.querySelector('td');});
        if(rows.length<8||table.closest('.table-card'))return;
        var bar=document.createElement('div');bar.className='gh-table-toolbar';var count=document.createElement('span'),input=document.createElement('input');
        count.textContent=rows.length+' records';input.type='search';input.placeholder='Filter these records…';input.setAttribute('aria-label','Filter records in this table');bar.append(count,input);table.parentNode.insertAdjacentElement('beforebegin',bar);
        input.addEventListener('input',function(){var q=input.value.trim().toLocaleLowerCase(),n=0;rows.forEach(function(r){r.hidden=!r.textContent.toLocaleLowerCase().includes(q);if(!r.hidden)n++;});count.textContent=n+' of '+rows.length+' records';});
    });
    // Existing Web Forms directories keep the same server controls and postback handlers.
    // A selected row repopulates the editor, so return to that view after its postback.
    var directory=main&&main.querySelector('.table-card'),editor=main&&main.querySelector('.form-card');
    if(directory&&editor){
        var layout=directory.parentElement,bar=document.createElement('div');bar.className='gh-workflow-tabs';bar.setAttribute('role','tablist');bar.setAttribute('aria-label','Directory and editor');
        layout.classList.add('gh-directory-layout');directory.id='directory-records';editor.id='directory-editor';
        [directory,editor].forEach(function(panel,i){var button=document.createElement('button');button.type='button';button.setAttribute('role','tab');button.setAttribute('aria-controls',panel.id);button.textContent=i?'Register / edit details':'Directory';bar.appendChild(button);button.addEventListener('click',function(){showDirectory(i);});button.addEventListener('keydown',function(e){if(e.key==='ArrowLeft'||e.key==='ArrowRight'){e.preventDefault();showDirectory(1-i);bar.children[1-i].focus();}});});
        function showDirectory(i){directory.hidden=i===1;editor.hidden=i===0;Array.from(bar.children).forEach(function(button,n){button.setAttribute('aria-selected',String(n===i));button.classList.toggle('is-active',n===i);button.tabIndex=n===i?0:-1;});}
        layout.insertAdjacentElement('beforebegin',bar);
        var populated=Array.from(editor.querySelectorAll('input:not([type=submit]):not([type=hidden]):not([type=button]):not([type=checkbox]):not([type=radio])')).some(function(input){return input.value.trim()!=='';});
        var feedback=editor.querySelector('.alert-box');showDirectory(populated||(feedback&&feedback.textContent.trim())?1:0);
    }
    document.querySelectorAll('form').forEach(function(form){
        var action=form.querySelector('[name=action]'),status=form.querySelector('[name=status]');
        var destructive=action&&(action.value==='discharge'||(action.value==='status'&&document.body.dataset.role==='Patient'));
        if(destructive){var button=form.querySelector('button');if(button)button.classList.add('btn-danger');}
        form.addEventListener('submit',function(e){
            var danger=destructive||(action&&['status','context-status'].includes(action.value)&&status&&status.value==='Cancelled');
            if(danger&&!window.confirm('Confirm this change? It will be recorded in the patient history.'))e.preventDefault();
            // Preserve Web Forms submit values; show progress without disabling the submitting control.
            if(!e.defaultPrevented){form.setAttribute('aria-busy','true');var button=e.submitter;if(button&&button.tagName==='BUTTON')button.textContent='Working…';}
        });
    });
})();
