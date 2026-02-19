<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Laboratorian/LaboratorianMaster.Master"
    AutoEventWireup="true" CodeBehind="LabTest.aspx.cs"
    Inherits="HospitalMSWebApp.Views.Laboratorian.LabTest"
    EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link href="https://fonts.googleapis.com/css2?family=Sora:wght@300;400;500;600;700;800&family=JetBrains+Mono:wght@400;500&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
    <style>
        :root {
            --ink: #0d1117;
            --surface: #f0f4fa;
            --card: #ffffff;
            --border: #e2e8f4;
            --muted: #8492a6;
            --accent: #06b6d4;
            --accent-dark: #0891b2;
            --accent-soft: rgba(6,182,212,0.09);
            --accent-glow: rgba(6,182,212,0.22);
            --blue: #3b6cf8;
            --blue-soft: rgba(59,108,248,0.09);
            --green: #10d9a0;
            --green-dark: #059669;
            --green-soft: rgba(16,217,160,0.09);
            --red: #f04060;
            --red-soft: rgba(240,64,96,0.09);
            --amber: #f5a623;
            --amber-soft: rgba(245,166,35,0.09);
            --purple: #8b5cf6;
            --purple-soft: rgba(139,92,246,0.09);
            --r-xl: 20px;
            --r-lg: 14px;
            --r-md: 10px;
            --r-sm: 7px;
            --shadow-sm: 0 1px 4px rgba(13,17,23,0.06), 0 2px 10px rgba(13,17,23,0.04);
            --shadow-md: 0 4px 20px rgba(13,17,23,0.08);
            --shadow-lg: 0 12px 40px rgba(13,17,23,0.12);
            --font: 'Sora', sans-serif;
            --mono: 'JetBrains Mono', monospace;
        }

        *, *::before, *::after {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }

        body {
            font-family: var(--font);
            background: var(--surface);
            color: var(--ink);
        }

        .hero {
            position: relative;
            overflow: hidden;
            background: var(--ink);
            border-radius: var(--r-xl);
            padding: 44px 48px;
            margin-bottom: 24px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 32px;
            min-height: 160px;
        }

            .hero::before {
                content: '';
                position: absolute;
                inset: 0;
                background: radial-gradient(ellipse 55% 90% at 92% 50%, rgba(6,182,212,0.32) 0%, transparent 60%), radial-gradient(ellipse 35% 70% at 5% 70%, rgba(59,108,248,0.18) 0%, transparent 55%), radial-gradient(ellipse 25% 50% at 50% 0%, rgba(16,217,160,0.07) 0%, transparent 50%);
                pointer-events: none;
            }

            /* dot-grid texture */
            .hero::after {
                content: '';
                position: absolute;
                inset: 0;
                background-image: radial-gradient(circle, rgba(255,255,255,0.055) 1px, transparent 1px);
                background-size: 24px 24px;
                pointer-events: none;
            }

        .hero-left {
            position: relative;
            z-index: 1;
        }

        .hero-eyebrow {
            display: inline-flex;
            align-items: center;
            gap: 7px;
            background: rgba(6,182,212,0.15);
            border: 1px solid rgba(6,182,212,0.3);
            color: var(--accent);
            border-radius: 20px;
            padding: 5px 14px;
            font-size: 0.72rem;
            font-weight: 700;
            letter-spacing: 0.08em;
            text-transform: uppercase;
            margin-bottom: 14px;
        }

        .hero h1 {
            font-size: 2rem;
            font-weight: 800;
            color: #fff;
            letter-spacing: -0.04em;
            line-height: 1.1;
            margin-bottom: 8px;
        }

            .hero h1 span {
                color: var(--accent);
            }

        .hero-sub {
            font-size: 0.875rem;
            color: rgba(255,255,255,0.45);
        }

        .hero-stats {
            display: flex;
            gap: 12px;
            position: relative;
            z-index: 1;
            flex-shrink: 0;
        }

        .hstat {
            background: rgba(255,255,255,0.07);
            border: 1px solid rgba(255,255,255,0.12);
            backdrop-filter: blur(12px);
            border-radius: var(--r-lg);
            padding: 18px 22px;
            text-align: center;
            min-width: 100px;
        }

        .hstat-num {
            font-family: var(--mono);
            font-size: 2rem;
            font-weight: 800;
            line-height: 1;
            color: var(--accent);
        }

            .hstat-num.g {
                color: var(--green);
            }

            .hstat-num.p {
                color: var(--purple);
            }

        .hstat-lbl {
            font-size: 0.68rem;
            font-weight: 600;
            letter-spacing: 0.08em;
            text-transform: uppercase;
            color: rgba(255,255,255,0.35);
            margin-top: 5px;
        }

        .hero-deco {
            position: absolute;
            right: 280px;
            top: 50%;
            transform: translateY(-50%);
            width: 180px;
            height: 180px;
            border-radius: 50%;
            border: 1.5px solid rgba(6,182,212,0.1);
            z-index: 0;
        }

            .hero-deco::before {
                content: '';
                position: absolute;
                inset: 20px;
                border-radius: 50%;
                border: 1px dashed rgba(6,182,212,0.1);
            }

        .hero-icon {
            position: absolute;
            right: 300px;
            top: 50%;
            transform: translateY(-50%);
            font-size: 5.5rem;
            color: rgba(6,182,212,0.07);
            z-index: 0;
        }

        .stats-row {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 14px;
            margin-bottom: 24px;
        }

        .scard {
            background: var(--card);
            border: 1px solid var(--border);
            border-radius: var(--r-lg);
            padding: 20px 22px;
            display: flex;
            align-items: center;
            gap: 16px;
            box-shadow: var(--shadow-sm);
            transition: box-shadow 0.22s, transform 0.22s;
        }

            .scard:hover {
                box-shadow: var(--shadow-md);
                transform: translateY(-2px);
            }

        .scard-ico {
            width: 46px;
            height: 46px;
            border-radius: 12px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.15rem;
            flex-shrink: 0;
        }

            .scard-ico.c {
                background: var(--accent-soft);
                color: var(--accent);
            }

            .scard-ico.g {
                background: var(--green-soft);
                color: var(--green-dark);
            }

            .scard-ico.p {
                background: var(--purple-soft);
                color: var(--purple);
            }

            .scard-ico.b {
                background: var(--blue-soft);
                color: var(--blue);
            }

        .scard-body .val {
            font-family: var(--mono);
            font-size: 1.65rem;
            font-weight: 800;
            color: var(--ink);
            line-height: 1;
        }

        .scard-body .key {
            font-size: 0.72rem;
            font-weight: 600;
            color: var(--muted);
            text-transform: uppercase;
            letter-spacing: 0.06em;
            margin-top: 3px;
        }

        .main-layout {
            display: grid;
            grid-template-columns: 360px 1fr;
            gap: 20px;
            align-items: start;
        }

        .form-panel {
            background: var(--card);
            border: 1px solid var(--border);
            border-radius: var(--r-xl);
            box-shadow: var(--shadow-sm);
            overflow: hidden;
            position: sticky;
            top: 20px;
        }

        .form-panel-bar {
            height: 4px;
            background: linear-gradient(90deg, var(--accent) 0%, var(--blue) 60%, var(--green) 100%);
        }

        .form-panel-body {
            padding: 28px;
        }

        .panel-title {
            display: flex;
            align-items: center;
            gap: 12px;
            margin-bottom: 24px;
        }

        .panel-title-icon {
            width: 36px;
            height: 36px;
            border-radius: 10px;
            background: var(--accent-soft);
            color: var(--accent);
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 0.9rem;
        }

        .panel-title h3 {
            font-size: 1rem;
            font-weight: 700;
            color: var(--ink);
        }

        .panel-title p {
            font-size: 0.72rem;
            color: var(--muted);
            margin-top: 2px;
        }

        .fsection {
            font-size: 0.68rem;
            font-weight: 700;
            color: var(--muted);
            text-transform: uppercase;
            letter-spacing: 0.1em;
            margin: 20px 0 12px;
            display: flex;
            align-items: center;
            gap: 8px;
        }

            .fsection::after {
                content: '';
                flex: 1;
                height: 1px;
                background: var(--border);
            }

        .fg {
            margin-bottom: 14px;
        }

        .flabel {
            font-size: 0.75rem;
            font-weight: 600;
            color: var(--muted);
            margin-bottom: 6px;
            display: flex;
            align-items: center;
            gap: 5px;
        }

            .flabel i {
                font-size: 0.68rem;
                opacity: 0.8;
            }

        .fw {
            position: relative;
        }

        .fw-ico {
            position: absolute;
            left: 13px;
            top: 50%;
            transform: translateY(-50%);
            font-size: 0.75rem;
            color: var(--muted);
            pointer-events: none;
            transition: color 0.2s;
        }

        .fw input, .fw select {
            width: 100%;
            padding: 11px 13px 11px 36px;
            border: 1.5px solid var(--border);
            border-radius: var(--r-md);
            font-size: 0.875rem;
            font-family: var(--font);
            color: var(--ink);
            background: #fafbfe;
            transition: border-color 0.2s, background 0.2s, box-shadow 0.2s;
        }

            .fw input:focus, .fw select:focus {
                outline: none;
                border-color: var(--accent);
                background: #fff;
                box-shadow: 0 0 0 3px var(--accent-glow);
            }

        .fw:focus-within .fw-ico {
            color: var(--accent);
        }

        .fw.cost-wrap::before {
            content: '$';
            position: absolute;
            left: 36px;
            top: 50%;
            transform: translateY(-50%);
            font-family: var(--mono);
            font-size: 0.85rem;
            color: var(--muted);
            pointer-events: none;
            z-index: 1;
        }

        .fw.cost-wrap input {
            padding-left: 50px;
            font-family: var(--mono);
            font-size: 0.9rem;
            font-weight: 600;
            color: var(--ink);
        }

        .cost-preview {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            margin-top: 7px;
            font-family: var(--mono);
            font-size: 0.78rem;
            font-weight: 700;
            color: var(--green-dark);
            background: var(--green-soft);
            border: 1px solid rgba(16,217,160,0.2);
            border-radius: 6px;
            padding: 4px 10px;
            transition: all 0.2s;
        }

        .alert-box {
            border-radius: var(--r-md);
            padding: 11px 14px;
            font-size: 0.82rem;
            font-weight: 500;
            display: none;
            align-items: center;
            gap: 9px;
            margin-bottom: 16px;
        }

            .alert-box.show {
                display: flex;
            }

            .alert-box.success {
                background: var(--green-soft);
                color: var(--green-dark);
                border: 1px solid rgba(16,217,160,0.25);
            }

            .alert-box.error {
                background: var(--red-soft);
                color: var(--red);
                border: 1px solid rgba(240,64,96,0.25);
            }

        .btn-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 10px;
            margin-top: 20px;
        }

        .xbtn {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: 7px;
            padding: 11px 16px;
            border: none;
            border-radius: var(--r-md);
            font-size: 0.81rem;
            font-weight: 700;
            font-family: var(--font);
            cursor: pointer;
            transition: all 0.2s;
        }

        .xbtn-primary {
            background: linear-gradient(135deg, var(--accent) 0%, var(--accent-dark) 100%);
            color: #fff;
            grid-column: 1 / -1;
            box-shadow: 0 2px 10px var(--accent-glow);
        }

            .xbtn-primary:hover {
                transform: translateY(-2px);
                box-shadow: 0 6px 20px var(--accent-glow);
            }

        .xbtn-blue {
            background: var(--blue-soft);
            color: var(--blue);
            border: 1.5px solid rgba(59,108,248,0.18);
        }

            .xbtn-blue:hover {
                background: var(--blue);
                color: #fff;
                border-color: var(--blue);
            }

        .xbtn-red {
            background: var(--red-soft);
            color: var(--red);
            border: 1.5px solid rgba(240,64,96,0.18);
        }

            .xbtn-red:hover {
                background: var(--red);
                color: #fff;
                border-color: var(--red);
            }

        .xbtn-ghost {
            background: transparent;
            color: var(--muted);
            border: 1.5px solid var(--border);
            grid-column: 1 / -1;
        }

            .xbtn-ghost:hover {
                background: var(--surface);
                color: var(--ink);
            }

        .table-panel {
            background: var(--card);
            border: 1px solid var(--border);
            border-radius: var(--r-xl);
            box-shadow: var(--shadow-sm);
            overflow: hidden;
        }

        .table-panel-bar {
            height: 4px;
            background: linear-gradient(90deg, var(--blue) 0%, var(--accent) 100%);
        }

        .table-panel-head {
            padding: 22px 28px 16px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 16px;
            flex-wrap: wrap;
            border-bottom: 1px solid var(--border);
        }

        .tph-left {
            display: flex;
            align-items: center;
            gap: 12px;
        }

        .tph-icon {
            width: 38px;
            height: 38px;
            border-radius: 10px;
            background: var(--blue-soft);
            color: var(--blue);
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 0.95rem;
        }

        .tph-title {
            font-size: 1rem;
            font-weight: 700;
            color: var(--ink);
        }

        .tph-sub {
            font-size: 0.72rem;
            color: var(--muted);
            margin-top: 1px;
        }

        .search-box {
            position: relative;
        }

            .search-box i {
                position: absolute;
                left: 12px;
                top: 50%;
                transform: translateY(-50%);
                color: var(--muted);
                font-size: 0.75rem;
            }

            .search-box input {
                padding: 9px 14px 9px 34px;
                border: 1.5px solid var(--border);
                border-radius: var(--r-md);
                font-size: 0.82rem;
                font-family: var(--font);
                background: var(--surface);
                color: var(--ink);
                width: 220px;
                transition: border-color 0.2s, box-shadow 0.2s, width 0.3s;
            }

                .search-box input:focus {
                    outline: none;
                    border-color: var(--accent);
                    box-shadow: 0 0 0 3px var(--accent-glow);
                    background: #fff;
                    width: 260px;
                }

        .tbl-wrap {
            overflow-x: auto;
            padding: 0 28px 24px;
        }

        table.grid {
            width: 100%;
            border-collapse: collapse;
        }

            table.grid thead th {
                padding: 12px 14px;
                font-size: 0.68rem;
                font-weight: 700;
                color: var(--muted);
                text-transform: uppercase;
                letter-spacing: 0.09em;
                text-align: left;
                background: var(--surface);
                border-bottom: 1px solid var(--border);
                white-space: nowrap;
            }

                table.grid thead th:first-child {
                    border-radius: 8px 0 0 0;
                }

                table.grid thead th:last-child {
                    border-radius: 0 8px 0 0;
                }

            table.grid tbody td {
                padding: 15px 14px;
                font-size: 0.865rem;
                border-bottom: 1px solid var(--border);
                color: var(--ink);
                vertical-align: middle;
            }

            table.grid tbody tr:last-child td {
                border-bottom: none;
            }

            table.grid tbody tr {
                transition: background 0.15s;
            }

                table.grid tbody tr:hover td {
                    background: #f6faff;
                }

        .id-cell {
            font-family: var(--mono);
            font-size: 0.75rem;
            color: var(--muted);
            font-weight: 500;
        }

        .test-name-cell {
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .test-ico {
            width: 32px;
            height: 32px;
            border-radius: 8px;
            background: var(--accent-soft);
            color: var(--accent);
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 0.75rem;
            flex-shrink: 0;
        }

        .test-name-val {
            font-weight: 600;
            font-size: 0.87rem;
        }

        .cost-chip {
            display: inline-flex;
            align-items: center;
            gap: 5px;
            font-family: var(--mono);
            font-size: 0.82rem;
            font-weight: 700;
            color: var(--green-dark);
            background: var(--green-soft);
            border: 1px solid rgba(16,217,160,0.2);
            padding: 4px 10px;
            border-radius: 6px;
        }

        .sel-btn {
            display: inline-flex;
            align-items: center;
            gap: 5px;
            padding: 6px 13px;
            font-size: 0.75rem;
            font-weight: 600;
            border: 1.5px solid var(--border);
            border-radius: var(--r-sm);
            background: var(--surface);
            color: var(--muted);
            cursor: pointer;
            font-family: var(--font);
            transition: all 0.18s;
        }

            .sel-btn:hover {
                background: var(--accent);
                color: #fff;
                border-color: var(--accent);
                box-shadow: 0 3px 10px var(--accent-glow);
            }

        /* pager */
        table.grid tfoot td {
            padding: 14px;
            border-top: 1px solid var(--border);
        }

        table.grid tfoot a,
        table.grid tfoot span {
            display: inline-flex;
            align-items: center;
            justify-content: center;
            width: 30px;
            height: 30px;
            border-radius: var(--r-sm);
            font-size: 0.78rem;
            font-weight: 600;
            text-decoration: none;
            color: var(--muted);
            border: 1px solid var(--border);
            margin: 0 2px;
            transition: all 0.18s;
        }

        table.grid tfoot span {
            background: var(--accent);
            color: #fff;
            border-color: var(--accent);
        }

        table.grid tfoot a:hover {
            background: var(--surface);
            color: var(--ink);
        }

        .empty-state {
            text-align: center;
            padding: 56px 20px;
            color: var(--muted);
        }

            .empty-state .emi {
                width: 64px;
                height: 64px;
                border-radius: 16px;
                background: var(--accent-soft);
                color: var(--accent);
                font-size: 1.6rem;
                display: flex;
                align-items: center;
                justify-content: center;
                margin: 0 auto 16px;
            }

            .empty-state strong {
                display: block;
                font-size: 0.95rem;
                color: var(--ink);
                margin-bottom: 6px;
            }

            .empty-state span {
                font-size: 0.82rem;
            }

        @keyframes up {
            from {
                opacity: 0;
                transform: translateY(14px);
            }

            to {
                opacity: 1;
                transform: translateY(0);
            }
        }

        .hero {
            animation: up 0.45s cubic-bezier(.22,.68,0,1.2) both;
        }

        .stats-row {
            animation: up 0.45s cubic-bezier(.22,.68,0,1.2) 0.07s both;
        }

        .form-panel {
            animation: up 0.45s cubic-bezier(.22,.68,0,1.2) 0.14s both;
        }

        .table-panel {
            animation: up 0.45s cubic-bezier(.22,.68,0,1.2) 0.14s both;
        }

        @media (max-width: 1100px) {
            .main-layout {
                grid-template-columns: 1fr;
            }

            .form-panel {
                position: static;
            }
        }

        @media (max-width: 700px) {
            .hero {
                flex-direction: column;
                align-items: flex-start;
                padding: 28px 24px;
            }

            .hero-stats {
                flex-wrap: wrap;
            }

            .hero-deco, .hero-icon {
                display: none;
            }

            .stats-row {
                grid-template-columns: 1fr 1fr;
            }

            .search-box input {
                width: 100%;
            }
        }
    </style>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MyBody" runat="server">
    <div class="container-fluid">

        <%-- ═══ HERO ═══ --%>
        <div class="hero">
            <div class="hero-deco"></div>
            <i class="fas fa-vial hero-icon"></i>

            <div class="hero-left">
                <div class="hero-eyebrow">
                    <i class="fas fa-flask"></i>Laboratorian Panel
           
                </div>
                <h1>Lab <span>Test</span> Catalog</h1>
                <p class="hero-sub">Manage available laboratory tests and their pricing</p>
            </div>

            <div class="hero-stats">
                <div class="hstat">
                    <div class="hstat-num" id="totalTests" runat="server">0</div>
                    <div class="hstat-lbl">Tests</div>
                </div>
                <div class="hstat">
                    <div class="hstat-num g" id="avgCostHero" runat="server">$0</div>
                    <div class="hstat-lbl">Avg Cost</div>
                </div>
                <div class="hstat">
                    <div class="hstat-num p" id="totalValHero" runat="server">$0</div>
                    <div class="hstat-lbl">Total Value</div>
                </div>
            </div>
        </div>

        <%-- ═══ STATS STRIP ═══ --%>
        <div class="stats-row">
            <div class="scard">
                <div class="scard-ico c"><i class="fas fa-vial"></i></div>
                <div class="scard-body">
                    <div class="val" id="statTotal" runat="server">0</div>
                    <div class="key">Total Tests</div>
                </div>
            </div>
            <div class="scard">
                <div class="scard-ico g"><i class="fas fa-dollar-sign"></i></div>
                <div class="scard-body">
                    <div class="val" id="avgCost" runat="server">$0</div>
                    <div class="key">Average Cost</div>
                </div>
            </div>
            <div class="scard">
                <div class="scard-ico p"><i class="fas fa-chart-line"></i></div>
                <div class="scard-body">
                    <div class="val" id="totalValue" runat="server">$0</div>
                    <div class="key">Total Value</div>
                </div>
            </div>
            <div class="scard">
                <div class="scard-ico b"><i class="fas fa-calendar-day"></i></div>
                <div class="scard-body">
                    <div class="val" id="todayDate" style="font-size: 1rem; padding-top: 4px"></div>
                    <div class="key">Today's Date</div>
                </div>
            </div>
        </div>

        <%-- ═══ MAIN LAYOUT ═══ --%>
        <div class="main-layout">

            <%-- ── FORM PANEL ── --%>
            <div class="form-panel">
                <div class="form-panel-bar"></div>
                <div class="form-panel-body">

                    <div class="panel-title">
                        <div class="panel-title-icon"><i class="fas fa-vial"></i></div>
                        <div>
                            <h3>Test Details</h3>
                            <p>Add a new test or edit an existing one</p>
                        </div>
                    </div>

                    <div id="alertBox" runat="server" class="alert-box"></div>

                    <div class="fsection">Test Information</div>

                    <div class="fg">
                        <div class="flabel"><i class="fas fa-vial"></i>Test Name</div>
                        <div class="fw">
                            <input type="text" id="LabNameTb" runat="server" placeholder="e.g. Complete Blood Count" />
                            <i class="fas fa-vial fw-ico"></i>
                        </div>
                    </div>

                    <div class="fg">
                        <div class="flabel"><i class="fas fa-dollar-sign"></i>Test Cost</div>
                        <div class="fw cost-wrap">
                            <i class="fas fa-dollar-sign fw-ico"></i>
                            <asp:TextBox ID="testCostTb" runat="server" TextMode="Number"
                                placeholder="0" onkeyup="updatePreview(this.value)" oninput="updatePreview(this.value)" />
                        </div>
                        <div class="cost-preview" id="costPreview">
                            <i class="fas fa-tag"></i>$0.00
                   
                        </div>
                    </div>

                    <div class="btn-grid">
                        <asp:Button ID="addBtn" runat="server" Text="＋  Add Test" CssClass="xbtn xbtn-primary" OnClick="addBtn_Click" />
                        <asp:Button ID="editBtn" runat="server" Text="✎  Update" CssClass="xbtn xbtn-blue" OnClick="editBtn_Click" />
                        <asp:Button ID="deleteBtn" runat="server" Text="✕  Delete" CssClass="xbtn xbtn-red" OnClick="deleteBtn_Click" />
                        <asp:Button ID="clearBtn" runat="server" Text="↺  Clear Form" CssClass="xbtn xbtn-ghost" OnClick="clearBtn_Click" />
                    </div>
                </div>
            </div>

            <%-- ── TABLE PANEL ── --%>
            <div class="table-panel">
                <div class="table-panel-bar"></div>
                <div class="table-panel-head">
                    <div class="tph-left">
                        <div class="tph-icon"><i class="fas fa-list-ul"></i></div>
                        <div>
                            <div class="tph-title">Test Catalog</div>
                            <div class="tph-sub">Select a row to load into the form</div>
                        </div>
                    </div>
                    <div class="search-box">
                        <i class="fas fa-search"></i>
                        <input type="text" id="searchInput" placeholder="Search tests…" onkeyup="filterTable()" />
                    </div>
                </div>

                <div class="tbl-wrap">
                    <asp:GridView ID="LabTestGV" runat="server" CssClass="grid"
                        AutoGenerateColumns="false"
                        DataKeyNames="testId"
                        OnSelectedIndexChanged="LabTestGV_SelectedIndexChanged"
                        AllowPaging="true"
                        PageSize="10"
                        OnPageIndexChanging="LabTestGV_PageIndexChanging"
                        GridLines="None">
                        <EmptyDataTemplate>
                            <div class="empty-state">
                                <div class="emi"><i class="fas fa-vial"></i></div>
                                <strong>No tests found</strong>
                                <span>Add your first lab test using the form on the left.</span>
                            </div>
                        </EmptyDataTemplate>
                        <Columns>
                            <asp:BoundField DataField="testId" HeaderText="ID" ItemStyle-CssClass="id-cell" />
                            <asp:TemplateField HeaderText="Test Name">
                                <ItemTemplate>
                                    <div class="test-name-cell">
                                        <div class="test-ico"><i class="fas fa-vial"></i></div>
                                        <div class="test-name-val"><%# Eval("testName") %></div>
                                    </div>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Cost">
                                <ItemTemplate>
                                    <span class="cost-chip">
                                        <i class="fas fa-dollar-sign" style="font-size: 0.65rem"></i>
                                        <%# string.Format("{0:N0}", Eval("testCost")) %>
                                </span>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:CommandField ShowSelectButton="true" SelectText="Select" ControlStyle-CssClass="sel-btn" />
                        </Columns>
                    </asp:GridView>
                </div>
            </div>

        </div>
        <%-- /main-layout --%>
    </div>
    <%-- /container-fluid --%>

    <script>
        document.getElementById('todayDate').textContent =
            new Date().toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

        function updatePreview(val) {
            const n = parseFloat(val);
            const el = document.getElementById('costPreview');
            el.textContent = isNaN(n) || n < 0 ? '$0.00' : '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
            el.innerHTML = '<i class="fas fa-tag"></i> ' + (isNaN(n) || n < 0 ? '$0.00' : '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
        }

        window.addEventListener('DOMContentLoaded', () => {
            const costInput = document.querySelector('#<%= testCostTb.ClientID %>');
        if (costInput && costInput.value) updatePreview(costInput.value);

        const a = document.getElementById('<%= alertBox.ClientID %>');
        if (a && a.classList.contains('show'))
            setTimeout(() => a.classList.remove('show'), 4000);
    });

        function filterTable() {
            const q = document.getElementById('searchInput').value.toLowerCase();
            const tbl = document.getElementById('<%= LabTestGV.ClientID %>');
            if (!tbl) return;
            Array.from(tbl.getElementsByTagName('tr')).slice(1).forEach(row => {
                const text = Array.from(row.getElementsByTagName('td')).map(c => c.textContent).join(' ').toLowerCase();
                row.style.display = text.includes(q) ? '' : 'none';
            });
        }
</script>
</asp:Content>
