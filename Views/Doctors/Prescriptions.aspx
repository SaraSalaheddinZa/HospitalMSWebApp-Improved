<%@ Page Title="Prescriptions - Guardian Health" Language="C#" MasterPageFile="~/Views/Doctors/DoctorMaster.Master" AutoEventWireup="true" CodeBehind="Prescriptions.aspx.cs" Inherits="HospitalMSWebApp.Views.Doctors.Prescriptions" EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link href="https://fonts.googleapis.com/css2?family=Sora:wght@300;400;500;600;700;800&family=JetBrains+Mono:wght@400;500&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
    <style>
        :root {
            --ink: #0f1117;
            --surface: #f7f8fc;
            --card: #ffffff;
            --border: #e4e7f0;
            --muted: #8892a4;
            --accent: #059669;
            --accent-soft: rgba(5,150,105,0.08);
            --accent-glow: rgba(5,150,105,0.2);
            --blue: #2d6ef7;
            --blue-soft: rgba(45,110,247,0.08);
            --green: #00c896;
            --green-soft: rgba(0,200,150,0.08);
            --red: #f23557;
            --red-soft: rgba(242,53,87,0.08);
            --amber: #f59e0b;
            --amber-soft: rgba(245,158,11,0.08);
            --radius: 14px;
            --radius-sm: 8px;
            --shadow: 0 2px 12px rgba(15,17,23,0.06), 0 1px 3px rgba(15,17,23,0.04);
            --shadow-hover: 0 8px 32px rgba(15,17,23,0.1);
            --font: 'Sora', sans-serif;
            --mono: 'JetBrains Mono', monospace;
        }

        *, *::before, *::after {
            box-sizing: border-box;
        }

        body {
            font-family: var(--font);
            background: var(--surface);
            color: var(--ink);
        }

        .rx-header {
            position: relative;
            overflow: hidden;
            background: var(--ink);
            border-radius: 20px;
            padding: 36px 40px;
            margin-bottom: 28px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 24px;
        }

        .rx-header::before {
                content: '';
                position: absolute;
                inset: 0;
                background: radial-gradient(ellipse 60% 80% at 85% 50%, rgba(5,150,105,0.25) 0%, transparent 65%), radial-gradient(ellipse 40% 60% at 10% 80%, rgba(45,110,247,0.12) 0%, transparent 60%);
                pointer-events: none;
            }

        .header-text {
            position: relative;
            z-index: 1;
        }

        .header-text h2 {
                font-size: 1.75rem;
                font-weight: 800;
                color: #fff;
                margin: 0 0 6px;
                letter-spacing: -0.03em;
            }

        .header-text p {
                margin: 0;
                font-size: 0.875rem;
                color: rgba(255,255,255,0.5);
            }

        .header-icon {
            position: absolute;
            right: 40px;
            top: 50%;
            transform: translateY(-50%);
            font-size: 7rem;
            color: rgba(255,255,255,0.03);
            line-height: 1;
        }

        .stats-bar {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 16px;
            margin-bottom: 28px;
        }

        .stat-item {
            background: var(--card);
            border: 1px solid var(--border);
            border-radius: var(--radius);
            padding: 20px 24px;
            display: flex;
            align-items: center;
            gap: 16px;
            transition: all 0.3s;
        }

        .stat-item:hover {
                box-shadow: var(--shadow-hover);
                transform: translateY(-2px);
            }

        .stat-icon {
            width: 48px;
            height: 48px;
            border-radius: 12px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.25rem;
        }

        .stat-icon.green {
                background: var(--green-soft);
                color: var(--green);
            }

        .stat-icon.blue {
                background: var(--blue-soft);
                color: var(--blue);
            }

        .stat-icon.amber {
                background: var(--amber-soft);
                color: var(--amber);
            }

        .stat-icon.red {
                background: var(--red-soft);
                color: var(--red);
            }

        .stat-info h4 {
            font-size: 0.75rem;
            font-weight: 600;
            color: var(--muted);
            text-transform: uppercase;
            letter-spacing: 0.08em;
            margin: 0 0 4px;
        }

        .stat-info .number {
            font-size: 1.5rem;
            font-weight: 700;
            color: var(--ink);
            font-family: var(--mono);
        }

        .rx-layout {
            display: grid;
            grid-template-columns: 360px 1fr;
            gap: 20px;
            align-items: start;
        }

        .form-card {
            background: var(--card);
            border: 1px solid var(--border);
            border-radius: 20px;
            padding: 28px;
            box-shadow: var(--shadow);
            position: sticky;
            top: 20px;
        }

        .form-card-title {
            font-size: 1rem;
            font-weight: 700;
            color: var(--ink);
            margin: 0 0 24px;
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .title-icon {
            width: 32px;
            height: 32px;
            border-radius: 8px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 0.85rem;
        }

        .title-icon.green {
                background: var(--accent-soft);
                color: var(--accent);
            }

        .title-icon.blue {
                background: var(--blue-soft);
                color: var(--blue);
            }

        .field-group {
            margin-bottom: 16px;
        }

        .field-label {
            display: flex;
            align-items: center;
            gap: 6px;
            font-size: 0.78rem;
            font-weight: 600;
            color: var(--muted);
            text-transform: uppercase;
            letter-spacing: 0.06em;
            margin-bottom: 7px;
        }

        .field-label i {
                font-size: 0.7rem;
            }

        .field-wrap {
            position: relative;
        }

        .field-ico {
            position: absolute;
            left: 14px;
            top: 50%;
            transform: translateY(-50%);
            color: var(--muted);
            font-size: 0.8rem;
            pointer-events: none;
            transition: color 0.2s;
        }

        .field-wrap input,
        .field-wrap select,
        .field-wrap textarea {
            width: 100%;
            padding: 10px 14px 10px 38px;
            border: 1.5px solid var(--border);
            border-radius: var(--radius-sm);
            font-size: 0.875rem;
            font-family: var(--font);
            color: var(--ink);
            background: var(--surface);
            transition: border-color 0.2s, background 0.2s, box-shadow 0.2s;
        }

        .field-wrap textarea {
            padding-left: 14px;
            min-height: 80px;
            resize: vertical;
        }

        .field-wrap input:focus,
        .field-wrap select:focus,
        .field-wrap textarea:focus {
                outline: none;
                border-color: var(--accent);
                background: #fff;
                box-shadow: 0 0 0 3px var(--accent-glow);
            }

        .field-wrap:focus-within .field-ico {
            color: var(--accent);
        }

        .field-wrap.no-ico input,
        .field-wrap.no-ico select {
            padding-left: 14px;
        }

        .field-wrap select {
            cursor: pointer;
            appearance: none;
        }

        .select-arr {
            position: absolute;
            right: 14px;
            top: 50%;
            transform: translateY(-50%);
            color: var(--muted);
            font-size: 0.7rem;
            pointer-events: none;
        }

        .cost-wrap {
            position: relative;
        }

        .cost-wrap .cost-prefix {
                position: absolute;
                left: 14px;
                top: 50%;
                transform: translateY(-50%);
                font-size: 0.85rem;
                font-weight: 700;
                color: var(--muted);
                font-family: var(--mono);
                pointer-events: none;
            }

        .cost-wrap input {
                padding-left: 30px;
            }

        .medicine-input {
            width: 100% !important;
            min-height: 80px !important;
            padding: 10px 14px !important;
            border: 1.5px solid var(--border) !important;
            border-radius: var(--radius-sm) !important;
            font-size: 0.875rem !important;
            font-family: var(--font) !important;
            color: var(--ink) !important;
            background: var(--surface) !important;
        }

        .medicine-input:focus {
                outline: none !important;
                border-color: var(--accent) !important;
                background: #fff !important;
                box-shadow: 0 0 0 3px var(--accent-glow) !important;
            }

        .alert-box {
            border-radius: var(--radius-sm);
            padding: 12px 14px;
            font-size: 0.82rem;
            font-weight: 500;
            display: none;
            align-items: center;
            gap: 8px;
            margin-bottom: 16px;
        }

        .alert-box.show {
                display: flex;
            }

        .alert-box.success {
                background: var(--green-soft);
                color: var(--green);
                border: 1px solid rgba(0,200,150,0.2);
            }

        .alert-box.error {
                background: var(--red-soft);
                color: var(--red);
                border: 1px solid rgba(242,53,87,0.2);
            }

        .alert-box.info {
                background: var(--blue-soft);
                color: var(--blue);
                border: 1px solid rgba(45,110,247,0.2);
            }

        .divider {
            height: 1px;
            background: var(--border);
            margin: 18px 0;
        }

        .btn-save {
            width: 100%;
            padding: 12px;
            background: var(--accent);
            color: #fff;
            border: none;
            border-radius: var(--radius-sm);
            font-size: 0.88rem;
            font-weight: 700;
            font-family: var(--font);
            cursor: pointer;
            transition: all 0.2s;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
        }

        .btn-save:hover {
                background: #047857;
                box-shadow: 0 4px 16px var(--accent-glow);
                transform: translateY(-1px);
            }

        .btn-save:disabled {
                opacity: 0.6;
                cursor: not-allowed;
                transform: none;
            }

        .btn-clear {
            width: 100%;
            padding: 10px;
            background: transparent;
            color: var(--muted);
            border: 1.5px solid var(--border);
            border-radius: var(--radius-sm);
            font-size: 0.85rem;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s;
            margin-top: 10px;
        }

        .btn-clear:hover {
                border-color: var(--muted);
                color: var(--ink);
            }

        .table-card {
            background: var(--card);
            border: 1px solid var(--border);
            border-radius: 20px;
            padding: 28px;
            box-shadow: var(--shadow);
        }

        .table-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 20px;
            gap: 16px;
            flex-wrap: wrap;
        }

        .table-title {
            font-size: 1rem;
            font-weight: 700;
            color: var(--ink);
            margin: 0;
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .search-wrap {
            position: relative;
            flex: 1;
            max-width: 280px;
            margin-left:auto;
        }

        .search-wrap i {
                position: absolute;
                left: 12px;
                top: 50%;
                transform: translateY(-50%);
                color: var(--muted);
                font-size: 0.78rem;
            }

        .search-wrap input {
                width: 100%;
                padding: 9px 14px 9px 34px;
                border: 1.5px solid var(--border);
                border-radius: var(--radius-sm);
                font-size: 0.82rem;
                font-family: var(--font);
                background: var(--surface);
                color: var(--ink);
                transition: border-color 0.2s, box-shadow 0.2s;
            }

        .search-wrap input:focus {
                    outline: none;
                    border-color: var(--accent);
                    box-shadow: 0 0 0 3px var(--accent-glow);
                    background: #fff;
                }

        .table-responsive {
            overflow-x: auto;
        }

        .table {
            width: 100%;
            border-collapse: collapse;
        }

        .table thead th {
                background: var(--surface);
                padding: 11px 14px;
                font-size: 0.68rem;
                font-weight: 700;
                color: var(--muted);
                text-transform: uppercase;
                letter-spacing: 0.08em;
                text-align: left;
                white-space: nowrap;
                border-bottom: 1px solid var(--border);
            }

        .table thead th:first-child {
                    border-radius: 8px 0 0 8px;
                }

        .table thead th:last-child {
                    border-radius: 0 8px 8px 0;
                }

        .table tbody td {
                padding: 13px 14px;
                font-size: 0.875rem;
                border-bottom: 1px solid var(--border);
                color: var(--ink);
                vertical-align: middle;
            }

        .table tbody tr:last-child td {
                border-bottom: none;
            }

        .table tbody tr {
                transition: background 0.15s;
                cursor: pointer;
            }

        .table tbody tr:hover td {
                    background: var(--surface);
                }

        .table tbody tr.selected td {
                    background: var(--accent-soft);
                }

        .table td:first-child {
                font-family: var(--mono);
                font-size: 0.78rem;
                color: var(--muted);
            }

        .cost-cell {
            font-family: var(--mono);
            font-weight: 600;
            color: var(--accent);
        }

        .med-chip {
            display: inline-flex;
            align-items: center;
            gap: 4px;
            background: var(--amber-soft);
            color: var(--amber);
            border-radius: 20px;
            padding: 3px 10px;
            font-size: 0.75rem;
            font-weight: 600;
            max-width: 200px;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .patient-cell {
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .patient-avatar {
            width: 32px;
            height: 32px;
            border-radius: 50%;
            background: var(--blue-soft);
            color: var(--blue);
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 0.75rem;
            font-weight: 700;
        }

        .empty-state {
            text-align: center;
            padding: 48px 20px;
            color: var(--muted);
        }

        .empty-state i {
                font-size: 3rem;
                margin-bottom: 16px;
                opacity: 0.3;
                display: block;
            }

        .empty-state h4 {
                font-size: 1rem;
                font-weight: 600;
                color: var(--ink);
                margin: 0 0 8px;
            }

        .empty-state p {
                font-size: 0.85rem;
                margin: 0;
            }

        @keyframes fadeUp {
            from {
                opacity: 0;
                transform: translateY(12px);
            }

            to {
                opacity: 1;
                transform: translateY(0);
            }
        }

        .rx-header {
            animation: fadeUp 0.4s ease both;
        }

        .form-card {
            animation: fadeUp 0.4s ease 0.1s both;
        }

        .table-card {
            animation: fadeUp 0.4s ease 0.15s both;
        }

        .stat-item {
            animation: fadeUp 0.4s ease both;
        }

        @media (max-width: 1200px) {
            .stats-bar {
                grid-template-columns: repeat(2, 1fr);
            }
        }

        @media (max-width: 1100px) {
            .rx-layout {
                grid-template-columns: 1fr;
            }

            .form-card {
                position: static;
            }
        }

        @media (max-width: 640px) {
            .rx-header {
                flex-direction: column;
                padding: 24px;
            }

            .header-icon {
                display: none;
            }

            .stats-bar {
                grid-template-columns: 1fr;
            }

            .table-header {
                flex-direction: column;
                align-items: stretch;
            }

            .search-wrap {
                max-width: none;
            }
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MyBody" runat="server">
    <div class="container-fluid">

        <div class="rx-header">
            <div class="header-text">
                <h2>Prescriptions</h2>
                <p>Create and manage patient prescriptions and lab test orders</p>
            </div>
            <span class="header-icon"><i class="fas fa-prescription-bottle-alt"></i></span>
        </div>

        <div class="stats-bar">
            <div class="stat-item">
                <div class="stat-icon green"><i class="fas fa-prescription"></i></div>
                <div class="stat-info">
                    <h4>Total Prescriptions</h4>
                    <div class="number" id="totalPrescriptions" runat="server">0</div>
                </div>
            </div>
            <div class="stat-item">
                <div class="stat-icon blue"><i class="fas fa-user-injured"></i></div>
                <div class="stat-info">
                    <h4>Patients</h4>
                    <div class="number" id="totalPatients" runat="server">0</div>
                </div>
            </div>
            <div class="stat-item">
                <div class="stat-icon amber"><i class="fas fa-flask"></i></div>
                <div class="stat-info">
                    <h4>Lab Tests</h4>
                    <div class="number" id="totalTests" runat="server">0</div>
                </div>
            </div>
            <div class="stat-item">
                <div class="stat-icon red"><i class="fas fa-dollar-sign"></i></div>
                <div class="stat-info">
                    <h4>Total Value</h4>
                    <div class="number" id="totalValue" runat="server">$0</div>
                </div>
            </div>
        </div>

        <div class="rx-layout">

            <div class="form-card">
                <div class="form-card-title">
                    <div class="title-icon green"><i class="fas fa-plus"></i></div>
                    New Prescription
               
                </div>

                <div id="alertBox" runat="server" class="alert-box"></div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-user-injured"></i>Patient</div>
                    <div class="field-wrap no-ico">
                        <asp:DropDownList ID="patientCb" runat="server" CssClass="form-control"></asp:DropDownList>
                        <i class="fas fa-chevron-down select-arr"></i>
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-pills"></i>Medicines</div>
                    <div class="field-wrap">
                        <input type="text" id="medicineTb" runat="server" placeholder="Enter medicines separated by commas..." class="medicine-input" />
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-flask"></i>Lab Test</div>
                    <div class="field-wrap no-ico">
                        <asp:DropDownList ID="testCb" runat="server" CssClass="form-control"></asp:DropDownList>
                        <i class="fas fa-chevron-down select-arr"></i>
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-dollar-sign"></i>Total Cost</div>
                    <div class="field-wrap cost-wrap">
                        <span class="cost-prefix">$</span>
                        <asp:TextBox ID="costTb" runat="server" TextMode="Number" step="0.01" min="0" placeholder="0.00"></asp:TextBox>
                    </div>
                </div>

                <div class="divider"></div>

                <asp:Button ID="addBtn" runat="server" Text="＋ Save Prescription" CssClass="btn-save" OnClick="addBtn_Click" />
                <asp:Button ID="clearBtn" runat="server" Text="Clear Form" CssClass="btn-clear" OnClick="clearBtn_Click" />
            </div>

            <div class="table-card">
                <div class="table-header">
                    <h4 class="table-title">
                        <div class="title-icon blue"><i class="fas fa-list-alt"></i></div>
                        Prescription Records
                    </h4>
                    <div class="search-wrap">
                        <i class="fas fa-search"></i>
                        <input type="text" id="searchInput" placeholder="Search prescriptions..." onkeyup="filterTable()" />
                    </div>
                </div>

                <div class="table-responsive">
                    <asp:GridView ID="prescriptionGV" runat="server" CssClass="table"
                        AutoGenerateColumns="false"
                        DataKeyNames="prescriptionId"
                        AllowPaging="true"
                        PageSize="10"
                        OnPageIndexChanging="prescriptionGV_PageIndexChanging"
                        OnRowDataBound="prescriptionGV_RowDataBound">
                        <EmptyDataTemplate>
                            <div class="empty-state">
                                <i class="fas fa-prescription-bottle"></i>
                                <h4>No prescriptions yet</h4>
                                <p>Use the form to create the first prescription.</p>
                            </div>
                        </EmptyDataTemplate>
                        <Columns>
                            <asp:BoundField DataField="prescriptionId" HeaderText="Rx #" />
                            <asp:TemplateField HeaderText="Patient">
                                <ItemTemplate>
                                    <div class="patient-cell">
                                        <div class="patient-avatar"><%# Eval("patientName").ToString().Substring(0,1).ToUpper() %></div>
                                        <span><%# Eval("patientName") %></span>
                                    </div>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="doctorName" HeaderText="Doctor" />
                            <asp:TemplateField HeaderText="Medicines">
                                <ItemTemplate>
                                    <span class="med-chip" title="<%# Eval("medicines") %>">
                                        <i class="fas fa-pills"></i><%# Eval("medicines") %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="testName" HeaderText="Lab Test" />
                            <asp:TemplateField HeaderText="Cost">
                                <ItemTemplate>
                                    <span class="cost-cell">$<%# Eval("cost", "{0:F2}") %></span>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>
        </div>
    </div>

    <script>
        function filterTable() {
            const filter = document.getElementById('searchInput').value.toLowerCase();
            const table = document.getElementById('<%= prescriptionGV.ClientID %>');
            if (!table) return;
            const rows = table.getElementsByTagName('tr');
            for (let i = 1; i < rows.length; i++) {
                if (rows[i].getElementsByTagName('td').length === 0) continue;
                const cells = rows[i].getElementsByTagName('td');
                let match = false;
                for (let j = 0; j < cells.length; j++) {
                    const txtValue = cells[j].textContent || cells[j].innerText;
                    if (txtValue.toLowerCase().indexOf(filter) > -1) { match = true; break; }
                }
                rows[i].style.display = match ? '' : 'none';
            }
        }

        document.addEventListener('DOMContentLoaded', function () {
            const table = document.getElementById('<%= prescriptionGV.ClientID %>');
            if (!table) return;
            const rows = table.getElementsByTagName('tbody')[0].getElementsByTagName('tr');
            for (let i = 0; i < rows.length; i++) {
                rows[i].addEventListener('click', function () {
                    for (let j = 0; j < rows.length; j++) rows[j].classList.remove('selected');
                    this.classList.add('selected');
                });
            }
        });
    </script>
</asp:Content>
