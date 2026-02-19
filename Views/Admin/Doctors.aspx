<%@ Page Title="Manage Doctors - Guardian Health" Language="C#" MasterPageFile="~/Views/Admin/AdminMaster.Master" AutoEventWireup="true" CodeBehind="Doctors.aspx.cs" Inherits="HospitalMSWebApp.Views.Admin.Doctors" EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link href="https://fonts.googleapis.com/css2?family=Sora:wght@300;400;500;600;700;800&family=JetBrains+Mono:wght@400;500&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
    <style>
        :root {
            --ink: #0f1117;
            --ink-soft: #1e2333;
            --surface: #f7f8fc;
            --card: #ffffff;
            --border: #e4e7f0;
            --muted: #8892a4;
            --accent: #7c3aed;
            --accent-soft: rgba(124, 58, 237, 0.08);
            --accent-glow: rgba(124, 58, 237, 0.2);
            --blue: #2d6ef7;
            --blue-soft: rgba(45, 110, 247, 0.08);
            --green: #00c896;
            --green-soft: rgba(0, 200, 150, 0.08);
            --red: #f23557;
            --red-soft: rgba(242, 53, 87, 0.08);
            --purple: #7c3aed;
            --purple-soft: rgba(124, 58, 237, 0.08);
            --radius: 14px;
            --radius-sm: 8px;
            --shadow: 0 2px 12px rgba(15,17,23,0.06), 0 1px 3px rgba(15,17,23,0.04);
            --shadow-hover: 0 8px 32px rgba(15,17,23,0.1), 0 2px 8px rgba(15,17,23,0.06);
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

        /* ── HEADER ── */
        .doc-header {
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

            .doc-header::before {
                content: '';
                position: absolute;
                inset: 0;
                background: radial-gradient(ellipse 60% 80% at 85% 50%, rgba(124,58,237,0.28) 0%, transparent 65%), radial-gradient(ellipse 40% 60% at 10% 80%, rgba(45,110,247,0.15) 0%, transparent 60%);
                pointer-events: none;
            }

            .doc-header::after {
                content: '';
                position: absolute;
                top: -60px;
                right: -40px;
                width: 280px;
                height: 280px;
                border-radius: 50%;
                border: 1px solid rgba(124,58,237,0.15);
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
                font-weight: 400;
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
            min-width: 100px;
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

                .header-badge .badge-num.blue {
                    color: #818cf8;
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
            right: 340px;
            top: 50%;
            transform: translateY(-50%);
            font-size: 7rem;
            color: rgba(255,255,255,0.03);
            line-height: 1;
        }

        /* ── STATS STRIP ── */
        .stats-strip {
            display: flex;
            gap: 16px;
            margin-bottom: 28px;
        }

        .stat-pill {
            background: var(--card);
            border: 1px solid var(--border);
            border-radius: var(--radius);
            padding: 18px 24px;
            display: flex;
            align-items: center;
            gap: 16px;
            flex: 1;
            box-shadow: var(--shadow);
            transition: box-shadow 0.2s;
        }

            .stat-pill:hover {
                box-shadow: var(--shadow-hover);
            }

        .stat-icon {
            width: 44px;
            height: 44px;
            border-radius: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.1rem;
        }

            .stat-icon.purple {
                background: var(--purple-soft);
                color: var(--purple);
            }

            .stat-icon.green {
                background: var(--green-soft);
                color: var(--green);
            }

            .stat-icon.blue {
                background: var(--blue-soft);
                color: var(--blue);
            }

            .stat-icon.indigo {
                background: rgba(99,102,241,0.08);
                color: #6366f1;
            }

        .stat-info .num {
            font-size: 1.6rem;
            font-weight: 800;
            color: var(--ink);
            line-height: 1;
            font-family: var(--mono);
        }

        .stat-info .lbl {
            font-size: 0.75rem;
            color: var(--muted);
            margin-top: 2px;
        }

        /* ── LAYOUT ── */
        .doc-layout {
            display: grid;
            grid-template-columns: 380px 1fr;
            gap: 20px;
            align-items: start;
        }

        /* ── FORM CARD ── */
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

            .form-card-title .title-icon {
                width: 32px;
                height: 32px;
                background: var(--accent-soft);
                border-radius: 8px;
                display: flex;
                align-items: center;
                justify-content: center;
                color: var(--accent);
                font-size: 0.85rem;
            }

        /* ── FORM FIELDS ── */
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

            .field-wrap .field-ico {
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

            .field-wrap select {
                cursor: pointer;
                appearance: none;
            }

            .field-wrap.no-ico input,
            .field-wrap.no-ico select {
                padding-left: 14px;
            }

        .select-arrow {
            position: absolute;
            right: 14px;
            top: 50%;
            transform: translateY(-50%);
            color: var(--muted);
            font-size: 0.7rem;
            pointer-events: none;
        }

        /* two-col grid inside form */
        .field-row {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 12px;
        }

        /* ── DIVIDER ── */
        .divider {
            height: 1px;
            background: var(--border);
            margin: 18px 0;
        }

        /* ── ALERT ── */
        .alert-box {
            border-radius: var(--radius-sm);
            padding: 12px 16px;
            font-size: 0.85rem;
            font-weight: 500;
            display: none;
            align-items: center;
            gap: 10px;
            margin-bottom: 18px;
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
                background: var(--purple-soft);
                color: var(--purple);
                border: 1px solid rgba(124,58,237,0.2);
            }

        /* ── BUTTONS ── */
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
            padding: 11px 18px;
            border: none;
            border-radius: var(--radius-sm);
            font-size: 0.82rem;
            font-weight: 600;
            font-family: var(--font);
            cursor: pointer;
            transition: all 0.2s;
            letter-spacing: 0.01em;
        }

        .btn-add {
            background: var(--accent);
            color: #fff;
            grid-column: 1 / -1;
        }

            .btn-add:hover {
                background: #6d28d9;
                box-shadow: 0 4px 16px var(--accent-glow);
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

        /* ── TABLE CARD ── */
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

            .table-title .title-icon {
                width: 32px;
                height: 32px;
                background: var(--purple-soft);
                border-radius: 8px;
                display: flex;
                align-items: center;
                justify-content: center;
                color: var(--purple);
                font-size: 0.85rem;
            }

        /* ── SEARCH ── */
        .search-wrap {
            position: relative;
            flex: 1;
            max-width: 280px;
        }

            .search-wrap i {
                position: absolute;
                left: 13px;
                top: 50%;
                transform: translateY(-50%);
                color: var(--muted);
                font-size: 0.8rem;
            }

            .search-wrap input {
                width: 100%;
                padding: 10px 14px 10px 36px;
                border: 1.5px solid var(--border);
                border-radius: var(--radius-sm);
                font-size: 0.85rem;
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

        /* ── TABLE ── */
        .table-responsive {
            overflow-x: auto;
        }

        .table {
            width: 100%;
            border-collapse: collapse;
        }

            .table thead th {
                background: var(--surface);
                padding: 11px 16px;
                font-size: 0.7rem;
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
                padding: 13px 16px;
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
                font-size: 0.8rem;
                color: var(--muted);
            }

        /* specialization chip */
        .spec-chip {
            display: inline-flex;
            align-items: center;
            gap: 5px;
            background: var(--purple-soft);
            color: var(--purple);
            border-radius: 20px;
            padding: 3px 10px;
            font-size: 0.75rem;
            font-weight: 600;
        }

        /* exp badge */
        .exp-badge {
            font-family: var(--mono);
            font-size: 0.8rem;
            color: var(--muted);
        }

        /* ── STATUS BADGE ── */
        .badge {
            display: inline-flex;
            align-items: center;
            gap: 5px;
            padding: 4px 10px;
            border-radius: 20px;
            font-size: 0.75rem;
            font-weight: 600;
        }

        .badge-active {
            background: var(--green-soft);
            color: var(--green);
        }

        .badge-inactive {
            background: var(--red-soft);
            color: var(--red);
        }

        .badge .dot {
            width: 5px;
            height: 5px;
            border-radius: 50%;
            background: currentColor;
        }

        /* ── SELECT BTN ── */
        .table .btn-select {
            background: var(--surface);
            color: var(--ink);
            border: 1.5px solid var(--border);
            padding: 7px 14px;
            font-size: 0.78rem;
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

        /* ── PAGER ── */
        .table tfoot td {
            padding: 14px 16px;
        }

        .table tfoot a, .table tfoot span {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 32px;
            height: 32px;
            border-radius: 6px;
            font-size: 0.82rem;
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

        /* ── EMPTY STATE ── */
        .empty-state {
            text-align: center;
            padding: 48px 20px;
            color: var(--muted);
        }

            .empty-state i {
                font-size: 2.5rem;
                margin-bottom: 12px;
                opacity: 0.3;
            }

        /* ── RESPONSIVE ── */
        @media (max-width: 1200px) {
            .doc-layout {
                grid-template-columns: 1fr;
            }

            .form-card {
                position: static;
            }
        }

        @media (max-width: 640px) {
            .doc-header {
                flex-direction: column;
                align-items: flex-start;
            }

            .stats-strip {
                flex-direction: column;
            }

            .header-icon {
                display: none;
            }

            .field-row {
                grid-template-columns: 1fr;
            }
        }

        /* ── ANIMATIONS ── */
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

        .doc-header {
            animation: fadeUp 0.4s ease both;
        }

        .stats-strip {
            animation: fadeUp 0.4s ease 0.08s both;
        }

        .form-card {
            animation: fadeUp 0.4s ease 0.16s both;
        }

        .table-card {
            animation: fadeUp 0.4s ease 0.16s both;
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="Mybody" runat="server">
    <div class="container-fluid">

        <!-- Header -->
        <div class="doc-header">
            <div class="header-text">
                <h2>Doctor Management</h2>
                <p>Add, edit and manage doctor profiles and system access</p>
            </div>
            <span class="header-icon"><i class="fas fa-user-md"></i></span>
            <div class="header-badges">
                <div class="header-badge">
                    <div class="badge-num" id="totalDoctors" runat="server">0</div>
                    <div class="badge-label">Total</div>
                </div>
                <div class="header-badge">
                    <div class="badge-num green" id="activeDoctors" runat="server">0</div>
                    <div class="badge-label">Active</div>
                </div>
                <div class="header-badge">
                    <div class="badge-num blue" id="specializations" runat="server">0</div>
                    <div class="badge-label">Specialties</div>
                </div>
            </div>
        </div>

        <!-- Stats Strip -->
        <div class="stats-strip">
            <div class="stat-pill">
                <div class="stat-icon purple"><i class="fas fa-user-md"></i></div>
                <div class="stat-info">
                    <div class="num" id="totalDoctors2" runat="server">0</div>
                    <div class="lbl">Total Doctors</div>
                </div>
            </div>
            <div class="stat-pill">
                <div class="stat-icon green"><i class="fas fa-circle-check"></i></div>
                <div class="stat-info">
                    <div class="num" id="activeDoctors2" runat="server">0</div>
                    <div class="lbl">Active Doctors</div>
                </div>
            </div>
            <div class="stat-pill">
                <div class="stat-icon indigo"><i class="fas fa-stethoscope"></i></div>
                <div class="stat-info">
                    <div class="num" id="specializations2" runat="server">0</div>
                    <div class="lbl">Specializations</div>
                </div>
            </div>
            <div class="stat-pill">
                <div class="stat-icon blue"><i class="fas fa-calendar-day"></i></div>
                <div class="stat-info">
                    <div class="num" id="todayDate" style="font-size: 1rem; padding-top: 2px"></div>
                    <div class="lbl">Today's Date</div>
                </div>
            </div>
        </div>

        <!-- Layout -->
        <div class="doc-layout">

            <!-- FORM -->
            <div class="form-card">
                <div class="form-card-title">
                    <div class="title-icon"><i class="fas fa-plus"></i></div>
                    Doctor Details
               
                </div>

                <div id="alertBox" runat="server" class="alert-box"></div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-user"></i>Doctor Name</div>
                    <div class="field-wrap">
                        <input type="text" id="DocNameTb" runat="server" placeholder="Dr. Full Name" />
                        <i class="fas fa-user field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-envelope"></i>Email Address</div>
                    <div class="field-wrap">
                        <asp:TextBox ID="emailTb" runat="server" placeholder="doctor@hospital.com" TextMode="Email"></asp:TextBox>
                        <i class="fas fa-envelope field-ico"></i>
                    </div>
                </div>

                <div class="field-row">
                    <div class="field-group">
                        <div class="field-label"><i class="fas fa-phone"></i>Phone</div>
                        <div class="field-wrap">
                            <input type="text" id="DocPhoneTb" runat="server" placeholder="+1 (555) 000-0000" />
                            <i class="fas fa-phone field-ico"></i>
                        </div>
                    </div>
                    <div class="field-group">
                        <div class="field-label"><i class="fas fa-briefcase"></i>Experience (yrs)</div>
                        <div class="field-wrap">
                            <input type="number" id="DocExpTb" runat="server" placeholder="0" min="0" />
                            <i class="fas fa-briefcase field-ico"></i>
                        </div>
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-stethoscope"></i>Specialization</div>
                    <div class="field-wrap">
                        <asp:TextBox ID="SpecialisationTb" runat="server" placeholder="e.g. Cardiology"></asp:TextBox>
                        <i class="fas fa-stethoscope field-ico"></i>
                    </div>
                </div>

                <div class="field-row">
                    <div class="field-group">
                        <div class="field-label"><i class="fas fa-venus-mars"></i>Gender</div>
                        <div class="field-wrap no-ico">
                            <asp:DropDownList ID="GenderCb" runat="server">
                                <asp:ListItem Value="">Select Gender</asp:ListItem>
                                <asp:ListItem Value="Male">Male</asp:ListItem>
                                <asp:ListItem Value="Female">Female</asp:ListItem>
                                <asp:ListItem Value="Other">Other</asp:ListItem>
                            </asp:DropDownList>
                            <i class="fas fa-chevron-down select-arrow"></i>
                        </div>
                    </div>
                    <div class="field-group">
                        <div class="field-label"><i class="fas fa-cake-candles"></i>Date of Birth</div>
                        <div class="field-wrap no-ico">
                            <input type="date" id="DOBTb" runat="server" />
                        </div>
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-location-dot"></i>Address</div>
                    <div class="field-wrap">
                        <asp:TextBox ID="AddressTb" runat="server" placeholder="Full address"></asp:TextBox>
                        <i class="fas fa-location-dot field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-lock"></i>Password</div>
                    <div class="field-wrap">
                        <input type="password" id="PasswordTb" runat="server" placeholder="Set a secure password" />
                        <i class="fas fa-lock field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-toggle-on"></i>Status</div>
                    <div class="field-wrap no-ico">
                        <asp:DropDownList ID="StatusCb" runat="server">
                            <asp:ListItem Value="Active">Active</asp:ListItem>
                            <asp:ListItem Value="Inactive">Inactive</asp:ListItem>
                        </asp:DropDownList>
                        <i class="fas fa-chevron-down select-arrow"></i>
                    </div>
                </div>

                <div class="divider"></div>

                <div class="btn-row">
                    <asp:Button ID="addBtn" runat="server" Text="＋ Add Doctor" CssClass="btn btn-add" OnClick="addBtn_Click" />
                    <asp:Button ID="editBtn" runat="server" Text="✎ Update" CssClass="btn btn-edit" OnClick="editBtn_Click" />
                    <asp:Button ID="deleteBtn" runat="server" Text="✕ Delete" CssClass="btn btn-delete" OnClick="deleteBtn_Click" />
                    <asp:Button ID="clearBtn" runat="server" Text="↺ Clear Form" CssClass="btn btn-clear" OnClick="clearBtn_Click" />
                </div>
            </div>

            <div class="table-card">
                <div class="table-header">
                    <h4 class="table-title">
                        <div class="title-icon"><i class="fas fa-user-md"></i></div>
                        Doctor Directory
                    </h4>
                    <div class="search-wrap">
                        <i class="fas fa-search"></i>
                        <input type="text" id="searchInput" placeholder="Search by name, specialty…" onkeyup="filterTable()" />
                    </div>
                </div>

                <div class="table-responsive">
                    <asp:GridView ID="DoctorsGV" runat="server" CssClass="table"
                        AutoGenerateColumns="false"
                        OnSelectedIndexChanged="DoctorsGV_SelectedIndexChanged"
                        DataKeyNames="doctorID"
                        AllowPaging="true"
                        PageSize="10"
                        OnPageIndexChanging="DoctorsGV_PageIndexChanging"
                        GridLines="None">
                        <EmptyDataTemplate>
                            <div class="empty-state">
                                <i class="fas fa-user-md"></i>
                                <p style="font-weight: 600; color: #1e2333; margin: 0 0 4px">No doctors found</p>
                                <p style="font-size: 0.82rem; margin: 0">Add your first doctor using the form.</p>
                            </div>
                        </EmptyDataTemplate>
                        <Columns>
                            <asp:BoundField DataField="doctorID" HeaderText="ID" />
                            <asp:BoundField DataField="doctorName" HeaderText="Name" />
                            <asp:BoundField DataField="doctorEmail" HeaderText="Email" />
                            <asp:BoundField DataField="doctorPhone" HeaderText="Phone" />
                            <asp:TemplateField HeaderText="Specialization">
                                <ItemTemplate>
                                    <span class="spec-chip">
                                        <i class="fas fa-stethoscope" style="font-size: 0.65rem"></i><%# Eval("doctorSpec") %>
                                    </span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Exp.">
                                <ItemTemplate>
                                    <span class="exp-badge"><%# Eval("doctorExp") %>yr</span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Status">
                                <ItemTemplate>
                                    <span class='badge <%# Eval("doctorStatus").ToString() == "Active" ? "badge-active" : "badge-inactive" %>'>
                                        <span class="dot"></span><%# Eval("doctorStatus") %>
                                    </span>
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
        // Live date
        const d = new Date();
        document.getElementById('todayDate').textContent = d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

        // Search filter
        function filterTable() {
            const filter = document.getElementById('searchInput').value.toLowerCase();
            const table = document.getElementById('<%= DoctorsGV.ClientID %>');
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

        // Auto-dismiss alerts after 4s
        window.addEventListener('DOMContentLoaded', () => {
            const alert = document.getElementById('<%= alertBox.ClientID %>');
            if (alert && alert.classList.contains('show')) {
                setTimeout(() => alert.classList.remove('show'), 4000);
            }
        });
    </script>
</asp:Content>
