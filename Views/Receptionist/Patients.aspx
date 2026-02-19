<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Receptionist/ReceptionistMaster.Master" AutoEventWireup="true" CodeBehind="Patients.aspx.cs" Inherits="HospitalMSWebApp.Views.Receptionist.Patients" EnableEventValidation="false" %>

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
            --accent: #0891b2;
            --accent-soft: rgba(8,145,178,0.08);
            --accent-glow: rgba(8,145,178,0.2);
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

        .pat-header {
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

        .pat-header::before {
                content: '';
                position: absolute;
                inset: 0;
                background: radial-gradient(ellipse 60% 80% at 85% 50%, rgba(8,145,178,0.25) 0%, transparent 65%), radial-gradient(ellipse 40% 60% at 10% 80%, rgba(45,110,247,0.12) 0%, transparent 60%);
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

        .header-badges {
            display: flex;
            gap: 12px;
            position: relative;
            z-index: 1;
        }

        .header-badge {
            background: rgba(255,255,255,0.06);
            border: 1px solid rgba(255,255,255,0.1);
            border-radius: 12px;
            padding: 14px 20px;
            text-align: center;
            min-width: 96px;
            backdrop-filter: blur(10px);
        }

        .header-badge .badge-num {
                font-size: 2rem;
                font-weight: 800;
                color: var(--accent);
                line-height: 1;
                font-family: var(--mono);
            }

        .header-badge .badge-num.green {
                    color: var(--green);
                }

        .header-badge .badge-label {
                font-size: 0.7rem;
                color: rgba(255,255,255,0.4);
                text-transform: uppercase;
                letter-spacing: 0.08em;
                margin-top: 4px;
            }

        .header-icon {
            position: absolute;
            right: 240px;
            top: 50%;
            transform: translateY(-50%);
            font-size: 7rem;
            color: rgba(255,255,255,0.03);
            line-height: 1;
        }

        .pat-layout {
            display: grid;
            grid-template-columns: 320px 1fr;
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

        .title-icon.cyan {
                background: var(--accent-soft);
                color: var(--accent);
            }

        .title-icon.blue {
                background: var(--blue-soft);
                color: var(--blue);
            }

        .field-group {
            margin-bottom: 15px;
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
        .field-wrap select {
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

        .field-wrap input:focus,
        .field-wrap select:focus {
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

        .alert-box {
            border-radius: var(--radius-sm);
            padding: 11px 14px;
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

        .divider {
            height: 1px;
            background: var(--border);
            margin: 18px 0;
        }

        .btn-row {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 10px;
        }

        .btn {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: 7px;
            padding: 10px 14px;
            border: none;
            border-radius: var(--radius-sm);
            font-size: 0.81rem;
            font-weight: 600;
            font-family: var(--font);
            cursor: pointer;
            transition: all 0.2s;
        }

        .btn-add {
            background: var(--accent);
            color: #fff;
            grid-column: 1 / -1;
        }

        .btn-add:hover {
                background: #0e7490;
                box-shadow: 0 4px 14px var(--accent-glow);
                transform: translateY(-1px);
            }

        .btn-edit {
            background: var(--blue-soft);
            color: var(--blue);
            border: 1px solid rgba(45,110,247,0.2);
        }

        .btn-edit:hover {
                background: var(--blue);
                color: #fff;
            }

        .btn-delete {
            background: var(--red-soft);
            color: var(--red);
            border: 1px solid rgba(242,53,87,0.2);
        }

        .btn-delete:hover {
                background: var(--red);
                color: #fff;
            }

        .btn-clear {
            background: var(--surface);
            color: var(--muted);
            border: 1.5px solid var(--border);
            grid-column: 1 / -1;
        }

        .btn-clear:hover {
                background: var(--border);
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
            max-width: 260px;
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
            }

        .table tbody tr:hover td {
                    background: var(--surface);
                }

        .table td:first-child {
                font-family: var(--mono);
                font-size: 0.78rem;
                color: var(--muted);
            }

        .gender-badge {
            display: inline-flex;
            align-items: center;
            gap: 4px;
            padding: 3px 10px;
            border-radius: 20px;
            font-size: 0.75rem;
            font-weight: 600;
        }

        .gender-m {
            background: var(--blue-soft);
            color: var(--blue);
        }

        .gender-f {
            background: rgba(236,72,153,0.08);
            color: #ec4899;
        }

        .gender-o {
            background: var(--amber-soft);
            color: var(--amber);
        }

        .allergy-chip {
            display: inline-flex;
            align-items: center;
            gap: 4px;
            background: var(--red-soft);
            color: var(--red);
            border-radius: 20px;
            padding: 3px 10px;
            font-size: 0.73rem;
            font-weight: 600;
        }

        .table .btn-select {
            background: var(--surface);
            color: var(--ink);
            border: 1.5px solid var(--border);
            padding: 6px 12px;
            font-size: 0.75rem;
            border-radius: 6px;
            font-weight: 600;
            cursor: pointer;
            font-family: var(--font);
            transition: all 0.2s;
        }

        .table .btn-select:hover {
                background: var(--accent);
                color: #fff;
                border-color: var(--accent);
            }

        .table tfoot td {
            padding: 14px;
        }

        .table tfoot a, .table tfoot span {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 30px;
            height: 30px;
            border-radius: 6px;
            font-size: 0.8rem;
            font-weight: 600;
            text-decoration: none;
            color: var(--muted);
            border: 1px solid var(--border);
            margin: 0 2px;
            transition: all 0.2s;
        }

        .table tfoot span {
            background: var(--accent);
            color: #fff;
            border-color: var(--accent);
        }

        .table tfoot a:hover {
            background: var(--surface);
            color: var(--ink);
        }

        .empty-state {
            text-align: center;
            padding: 48px 20px;
            color: var(--muted);
        }

        .empty-state i {
                font-size: 2.5rem;
                margin-bottom: 12px;
                opacity: 0.3;
                display: block;
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

        .pat-header {
            animation: fadeUp 0.4s ease both;
        }

        .form-card {
            animation: fadeUp 0.4s ease 0.1s both;
        }

        .table-card {
            animation: fadeUp 0.4s ease 0.1s both;
        }

        @media (max-width: 1100px) {
            .pat-layout {
                grid-template-columns: 1fr;
            }

            .form-card {
                position: static;
            }
        }

        @media (max-width: 640px) {
            .pat-header {
                flex-direction: column;
                align-items: flex-start;
            }

            .header-icon {
                display: none;
            }
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MyBody" runat="server">
    <div class="container-fluid">

        <!-- Header -->
        <div class="pat-header">
            <div class="header-text">
                <h2>Patient Management</h2>
                <p>Register, update and manage patient records</p>
            </div>
            <span class="header-icon"><i class="fas fa-user-injured"></i></span>
            <div class="header-badges">
                <div class="header-badge">
                    <div class="badge-num" id="totalPatients" runat="server">0</div>
                    <div class="badge-label">Total</div>
                </div>
                <div class="header-badge">
                    <div class="badge-num green" id="todayPatients" runat="server">0</div>
                    <div class="badge-label">Today</div>
                </div>
            </div>
        </div>

        <div class="pat-layout">

            <!-- FORM -->
            <div class="form-card">
                <div class="form-card-title">
                    <div class="title-icon cyan"><i class="fas fa-user-plus"></i></div>
                    Patient Details
               
                </div>

                <div id="alertBox" runat="server" class="alert-box"></div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-user"></i>Full Name</div>
                    <div class="field-wrap">
                        <input type="text" id="PatNameTb" runat="server" placeholder="Patient full name" />
                        <i class="fas fa-user field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-phone"></i>Phone</div>
                    <div class="field-wrap">
                        <input type="text" id="PhoneTb" runat="server" placeholder="+1 (555) 000-0000" />
                        <i class="fas fa-phone field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-venus-mars"></i>Gender</div>
                    <div class="field-wrap no-ico">
                        <asp:DropDownList ID="GenderCb" runat="server">
                            <asp:ListItem Value="">— Select —</asp:ListItem>
                            <asp:ListItem Value="Female">Female</asp:ListItem>
                            <asp:ListItem Value="Male">Male</asp:ListItem>
                            <asp:ListItem Value="Other">Other</asp:ListItem>
                        </asp:DropDownList>
                        <i class="fas fa-chevron-down select-arr"></i>
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-cake-candles"></i>Date of Birth</div>
                    <div class="field-wrap no-ico">
                        <input type="date" id="DOBTb" runat="server" />
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-location-dot"></i>Address</div>
                    <div class="field-wrap">
                        <input type="text" id="AddressTb" runat="server" placeholder="Full address" />
                        <i class="fas fa-location-dot field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-triangle-exclamation"></i>Allergies</div>
                    <div class="field-wrap">
                        <input type="text" id="AllergyTb" runat="server" placeholder="e.g. Penicillin, Peanuts" />
                        <i class="fas fa-triangle-exclamation field-ico"></i>
                    </div>
                </div>

                <div class="divider"></div>

                <div class="btn-row">
                    <asp:Button ID="addBtn" runat="server" Text="＋ Register Patient" CssClass="btn btn-add" OnClick="addBtn_Click" />
                    <asp:Button ID="editBtn" runat="server" Text="✎ Update" CssClass="btn btn-edit" OnClick="editBtn_Click" />
                    <asp:Button ID="deleteBtn" runat="server" Text="✕ Delete" CssClass="btn btn-delete" OnClick="deleteBtn_Click" />
                    <asp:Button ID="clearBtn" runat="server" Text="↺ Clear Form" CssClass="btn btn-clear" OnClick="clearBtn_Click" />
                </div>
            </div>

            <!-- TABLE -->
            <div class="table-card">
                <div class="table-header">
                    <h4 class="table-title">
                        <div class="title-icon blue"><i class="fas fa-table-list"></i></div>
                        Patient Directory
                    </h4>
                    <div class="search-wrap">
                        <i class="fas fa-search"></i>
                        <input type="text" id="searchInput" placeholder="Search patients…" onkeyup="filterTable()" />
                    </div>
                </div>

                <div class="table-responsive">
                    <asp:GridView ID="PatientListGV" runat="server" CssClass="table"
                        AutoGenerateColumns="false"
                        OnSelectedIndexChanged="PatientListGV_SelectedIndexChanged"
                        DataKeyNames="patientId"
                        AllowPaging="true"
                        PageSize="10"
                        OnPageIndexChanging="PatientListGV_PageIndexChanging"
                        GridLines="None">
                        <EmptyDataTemplate>
                            <div class="empty-state">
                                <i class="fas fa-user-injured"></i>
                                <p style="font-weight: 600; color: #1e2333; margin: 0 0 4px">No patients registered</p>
                                <p style="font-size: 0.82rem; margin: 0">Use the form to register the first patient.</p>
                            </div>
                        </EmptyDataTemplate>
                        <Columns>
                            <asp:BoundField DataField="patientId" HeaderText="ID" />
                            <asp:BoundField DataField="patientName" HeaderText="Name" />
                            <asp:BoundField DataField="patientPhone" HeaderText="Phone" />
                            <asp:TemplateField HeaderText="Gender">
                                <ItemTemplate>
                                    <span class='gender-badge <%# GetGenderClass(Eval("patientGender").ToString()) %>'>
                                        <%# Eval("patientGender") %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="patientDOB" HeaderText="DOB" DataFormatString="{0:yyyy-MM-dd}" />
                            <asp:BoundField DataField="patientAdd" HeaderText="Address" />
                            <asp:TemplateField HeaderText="Allergies">
                                <ItemTemplate>
                                    <%# string.IsNullOrEmpty(Eval("patientAllergies").ToString()) ? "<span style='color:#8892a4;font-size:0.8rem'>None</span>" : $"<span class='allergy-chip'><i class='fas fa-triangle-exclamation' style='font-size:0.65rem'></i>{Eval("patientAllergies")}</span>" %>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:CommandField ShowSelectButton="true" SelectText="Select" ControlStyle-CssClass="btn-select" />
                        </Columns>
                    </asp:GridView>
                </div>
            </div>

        </div>
    </div>

    <script>
        function filterTable() {
            const filter = document.getElementById('searchInput').value.toLowerCase();
            const table = document.getElementById('<%= PatientListGV.ClientID %>');
            if (!table) return;
            const rows = table.getElementsByTagName('tr');
            for (let i = 1; i < rows.length; i++) {
                const cells = rows[i].getElementsByTagName('td');
                let match = false;
                for (let j = 0; j < cells.length; j++) {
                    if (cells[j].textContent.toLowerCase().includes(filter)) { match = true; break; }
                }
                rows[i].style.display = match ? '' : 'none';
            }
        }
    </script>
</asp:Content>
