<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Support.aspx.cs" Inherits="HospitalMSWebApp.Views.Support" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Guardian Health — Support</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta charset="utf-8" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link href="https://fonts.googleapis.com/css2?family=Sora:wght@300;400;500;600;700;800&family=JetBrains+Mono:wght@400;500&display=swap" rel="stylesheet" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
    <style>
        :root {
            --ink: #0f1117;
            --surface: #f7f8fc;
            --card: #ffffff;
            --border: #e4e7f0;
            --muted: #8892a4;
            --accent: #0ea5e9;
            --accent-soft: rgba(14,165,233,0.08);
            --accent-glow: rgba(14,165,233,0.2);
            --green: #00c896;
            --green-soft: rgba(0,200,150,0.08);
            --amber: #f59e0b;
            --amber-soft: rgba(245,158,11,0.08);
            --red: #f23557;
            --red-soft: rgba(242,53,87,0.08);
            --radius: 16px;
            --radius-sm: 10px;
            --shadow: 0 2px 12px rgba(15,17,23,0.06), 0 1px 3px rgba(15,17,23,0.04);
            --shadow-lg: 0 12px 40px rgba(15,17,23,0.1);
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
            min-height: 100vh;
        }

        /* ── HERO ── */
        .hero {
            position: relative;
            overflow: hidden;
            background: var(--ink);
            padding: 90px 24px 120px;
            text-align: center;
        }

            .hero::before {
                content: '';
                position: absolute;
                inset: 0;
                background: radial-gradient(ellipse 70% 80% at 20% 50%, rgba(14,165,233,0.18) 0%, transparent 60%), radial-gradient(ellipse 50% 60% at 80% 30%, rgba(0,200,150,0.12) 0%, transparent 60%);
                pointer-events: none;
            }

            .hero::after {
                content: '';
                position: absolute;
                inset: 0;
                background-image: linear-gradient(rgba(255,255,255,0.025) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.025) 1px, transparent 1px);
                background-size: 48px 48px;
                pointer-events: none;
            }

        .hero-inner {
            position: relative;
            z-index: 1;
            max-width: 640px;
            margin: 0 auto;
        }

        .hero-eyebrow {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            background: rgba(14,165,233,0.12);
            border: 1px solid rgba(14,165,233,0.25);
            color: var(--accent);
            border-radius: 20px;
            padding: 6px 16px;
            font-size: 0.78rem;
            font-weight: 600;
            letter-spacing: 0.06em;
            text-transform: uppercase;
            margin-bottom: 20px;
        }

        .hero h1 {
            font-size: clamp(2rem, 5vw, 3rem);
            font-weight: 800;
            color: #fff;
            letter-spacing: -0.04em;
            line-height: 1.1;
            margin-bottom: 16px;
        }

            .hero h1 span {
                color: var(--accent);
            }

        .hero p {
            font-size: 1rem;
            color: rgba(255,255,255,0.55);
            line-height: 1.7;
        }

        /* ── CONTACT CARDS ── */
        .cards-section {
            max-width: 1000px;
            margin: -56px auto 0;
            padding: 0 24px;
            position: relative;
            z-index: 2;
        }

        .cards-grid {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 16px;
        }

        .contact-card {
            background: var(--card);
            border: 1px solid var(--border);
            border-radius: var(--radius);
            padding: 32px 24px;
            text-align: center;
            box-shadow: var(--shadow-lg);
            transition: transform 0.25s, box-shadow 0.25s;
        }

            .contact-card:hover {
                transform: translateY(-6px);
                box-shadow: 0 20px 56px rgba(15,17,23,0.13);
            }

        .c-icon {
            width: 60px;
            height: 60px;
            border-radius: 14px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.4rem;
            margin: 0 auto 18px;
        }

            .c-icon.sky {
                background: var(--accent-soft);
                color: var(--accent);
            }

            .c-icon.green {
                background: var(--green-soft);
                color: var(--green);
            }

            .c-icon.amber {
                background: var(--amber-soft);
                color: var(--amber);
            }

        .contact-card h3 {
            font-size: 1rem;
            font-weight: 700;
            margin-bottom: 8px;
            color: var(--ink);
        }

        .contact-card p {
            font-size: 0.82rem;
            color: var(--muted);
            margin-bottom: 16px;
            line-height: 1.6;
        }

        .c-link {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            font-size: 0.82rem;
            font-weight: 600;
            color: var(--accent);
            text-decoration: none;
            transition: gap 0.2s;
        }

            .c-link:hover {
                gap: 10px;
            }

        /* ── STATUS STRIP ── */
        .status-strip {
            max-width: 1000px;
            margin: 20px auto 0;
            padding: 0 24px;
            display: flex;
            gap: 12px;
        }

        .status-pill {
            flex: 1;
            background: var(--card);
            border: 1px solid var(--border);
            border-radius: var(--radius-sm);
            padding: 14px 18px;
            display: flex;
            align-items: center;
            gap: 10px;
            font-size: 0.82rem;
            font-weight: 500;
            box-shadow: var(--shadow);
        }

        .s-dot {
            width: 8px;
            height: 8px;
            border-radius: 50%;
            flex-shrink: 0;
        }

            .s-dot.online {
                background: var(--green);
                box-shadow: 0 0 0 3px rgba(0,200,150,0.2);
            }

            .s-dot.busy {
                background: var(--amber);
                box-shadow: 0 0 0 3px rgba(245,158,11,0.2);
            }

        .status-pill strong {
            color: var(--ink);
        }

        .status-pill span {
            color: var(--muted);
        }

        /* ── FORM SECTION ── */
        .form-section {
            max-width: 760px;
            margin: 48px auto;
            padding: 0 24px;
        }

        .form-card {
            background: var(--card);
            border: 1px solid var(--border);
            border-radius: 20px;
            padding: 48px;
            box-shadow: var(--shadow-lg);
        }

        .form-top {
            text-align: center;
            margin-bottom: 36px;
        }

            .form-top .ticket-tag {
                display: inline-flex;
                align-items: center;
                gap: 6px;
                background: var(--accent-soft);
                color: var(--accent);
                border-radius: 20px;
                padding: 5px 14px;
                font-size: 0.75rem;
                font-weight: 600;
                letter-spacing: 0.05em;
                text-transform: uppercase;
                margin-bottom: 14px;
            }

            .form-top h2 {
                font-size: 1.6rem;
                font-weight: 800;
                letter-spacing: -0.03em;
                margin-bottom: 6px;
                color: var(--ink);
            }

            .form-top p {
                font-size: 0.875rem;
                color: var(--muted);
            }

        /* ── ALERT ── */
        .alert-box {
            border-radius: var(--radius-sm);
            padding: 14px 18px;
            font-size: 0.875rem;
            font-weight: 500;
            display: none;
            align-items: center;
            gap: 10px;
            margin-bottom: 28px;
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

        /* ── FIELDS ── */
        .field-row-2 {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 16px;
        }

        .field-group {
            margin-bottom: 18px;
        }

        .field-label {
            font-size: 0.78rem;
            font-weight: 600;
            color: var(--muted);
            text-transform: uppercase;
            letter-spacing: 0.06em;
            display: flex;
            align-items: center;
            gap: 6px;
            margin-bottom: 8px;
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
            padding: 12px 14px 12px 40px;
            border: 1.5px solid var(--border);
            border-radius: var(--radius-sm);
            font-size: 0.875rem;
            font-family: var(--font);
            color: var(--ink);
            background: var(--surface);
            transition: border-color 0.2s, box-shadow 0.2s, background 0.2s;
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
        .field-wrap.no-ico select,
        .field-wrap.no-ico textarea {
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

        textarea {
            resize: vertical;
            min-height: 130px;
        }

        .field-wrap textarea {
            padding-left: 14px !important;
        }

        /* ── PRIORITY CHIPS ── */
        .priority-row {
            display: flex;
            gap: 8px;
            flex-wrap: wrap;
        }

        .p-chip input[type="radio"] {
            display: none;
        }

        .p-chip label {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 8px 16px;
            border-radius: 20px;
            border: 1.5px solid var(--border);
            font-size: 0.8rem;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s;
            background: var(--surface);
            color: var(--muted);
        }

        .p-chip input:checked + label {
            border-color: currentColor;
        }

        .p-chip.low label {
            color: var(--green);
        }

        .p-chip.low input:checked + label {
            background: var(--green-soft);
            border-color: var(--green);
        }

        .p-chip.med label {
            color: var(--accent);
        }

        .p-chip.med input:checked + label {
            background: var(--accent-soft);
            border-color: var(--accent);
        }

        .p-chip.high label {
            color: var(--amber);
        }

        .p-chip.high input:checked + label {
            background: var(--amber-soft);
            border-color: var(--amber);
        }

        .p-chip.crit label {
            color: var(--red);
        }

        .p-chip.crit input:checked + label {
            background: var(--red-soft);
            border-color: var(--red);
        }

        .char-info {
            display: flex;
            justify-content: flex-end;
            font-size: 0.72rem;
            color: var(--muted);
            margin-top: 5px;
            font-family: var(--mono);
        }

        /* ── SUBMIT ── */
        .btn-submit {
            width: 100%;
            padding: 15px;
            background: var(--accent);
            border: none;
            border-radius: var(--radius-sm);
            color: #fff;
            font-size: 0.95rem;
            font-weight: 700;
            font-family: var(--font);
            cursor: pointer;
            transition: all 0.25s;
            margin-top: 8px;
            letter-spacing: 0.01em;
        }

            .btn-submit:hover {
                background: #0284c7;
                box-shadow: 0 6px 20px var(--accent-glow);
                transform: translateY(-2px);
            }

        /* ── FOOTER ── */
        .support-footer {
            background: var(--ink);
            color: rgba(255,255,255,0.5);
            padding: 40px 24px;
            text-align: center;
            font-size: 0.82rem;
        }

        .footer-links {
            display: flex;
            justify-content: center;
            gap: 32px;
            margin-bottom: 20px;
        }

            .footer-links a {
                color: rgba(255,255,255,0.5);
                text-decoration: none;
                display: inline-flex;
                align-items: center;
                gap: 6px;
                transition: color 0.2s;
            }

                .footer-links a:hover {
                    color: #fff;
                }

        /* ── ANIMATIONS ── */
        @keyframes fadeUp {
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
            animation: fadeUp 0.4s ease both;
        }

        .cards-grid {
            animation: fadeUp 0.4s ease 0.1s both;
        }

        .status-strip {
            animation: fadeUp 0.4s ease 0.18s both;
        }

        .form-card {
            animation: fadeUp 0.4s ease 0.24s both;
        }

        /* ── RESPONSIVE ── */
        @media (max-width: 768px) {
            .cards-grid {
                grid-template-columns: 1fr;
            }

            .status-strip {
                flex-direction: column;
            }

            .form-card {
                padding: 28px 20px;
            }

            .field-row-2 {
                grid-template-columns: 1fr;
            }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <!-- Hero -->
        <header class="hero">
            <div class="hero-inner">
                <div class="hero-eyebrow"><i class="fas fa-headset"></i>24/7 Support Center</div>
                <h1>We're here to <span>help you</span></h1>
                <p>Reach out through any channel below — our team typically responds within 2 hours for high-priority issues.</p>
            </div>
        </header>

        <!-- Contact Cards -->
        <section class="cards-section">
            <div class="cards-grid">
                <div class="contact-card">
                    <div class="c-icon sky"><i class="fas fa-envelope"></i></div>
                    <h3>Email Support</h3>
                    <p>Send us an email and we'll respond within 24 hours on business days.</p>
                    <a href="mailto:support@guardianhealth.com" class="c-link">support@guardianhealth.com <i class="fas fa-arrow-right"></i></a>
                </div>
                <div class="contact-card">
                    <div class="c-icon green"><i class="fas fa-phone-alt"></i></div>
                    <h3>Phone Support</h3>
                    <p>Call us directly for urgent issues — available around the clock.</p>
                    <a href="tel:+15551234567" class="c-link">+1 (555) 123-4567 <i class="fas fa-arrow-right"></i></a>
                </div>
                <div class="contact-card">
                    <div class="c-icon amber"><i class="fas fa-comments"></i></div>
                    <h3>Live Chat</h3>
                    <p>Chat with our support team in real-time for quick resolutions.</p>
                    <a href="#" class="c-link" onclick="return false;">Start Chat <i class="fas fa-arrow-right"></i></a>
                </div>
            </div>
        </section>

        <!-- Status Strip -->
        <div class="status-strip">
            <div class="status-pill"><span class="s-dot online"></span><strong>System Status</strong><span>— All systems operational</span></div>
            <div class="status-pill"><span class="s-dot online"></span><strong>Support Hours</strong><span>— 24 / 7 Available</span></div>
            <div class="status-pill"><span class="s-dot online"></span><strong>Avg Response</strong><span>— Under 2 hours</span></div>
        </div>

        <!-- Ticket Form -->
        <section class="form-section">
            <div class="form-card">
                <div class="form-top">
                    <div class="ticket-tag"><i class="fas fa-ticket-alt"></i>Submit a Ticket</div>
                    <h2>How can we help?</h2>
                    <p>Fill out the form and we'll get back to you as soon as possible.</p>
                </div>

                <div id="alertBox" runat="server" class="alert-box"></div>

                <div class="field-row-2">
                    <div class="field-group">
                        <div class="field-label"><i class="fas fa-user"></i>Full Name</div>
                        <div class="field-wrap">
                            <asp:TextBox ID="nameTb" runat="server" placeholder="Your full name"></asp:TextBox>
                            <i class="fas fa-user field-ico"></i>
                        </div>
                    </div>
                    <div class="field-group">
                        <div class="field-label"><i class="fas fa-envelope"></i>Email</div>
                        <div class="field-wrap">
                            <asp:TextBox ID="emailTb" runat="server" placeholder="you@example.com" TextMode="Email"></asp:TextBox>
                            <i class="fas fa-envelope field-ico"></i>
                        </div>
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-tag"></i>Issue Category</div>
                    <div class="field-wrap no-ico">
                        <asp:DropDownList ID="categoryCb" runat="server">
                            <asp:ListItem Value="">— Select a category —</asp:ListItem>
                            <asp:ListItem Value="Login Issues">Login Issues</asp:ListItem>
                            <asp:ListItem Value="System Error">System Error</asp:ListItem>
                            <asp:ListItem Value="Feature Request">Feature Request</asp:ListItem>
                            <asp:ListItem Value="Data Issue">Data Issue</asp:ListItem>
                            <asp:ListItem Value="Performance">Performance Problem</asp:ListItem>
                            <asp:ListItem Value="Other">Other</asp:ListItem>
                        </asp:DropDownList>
                        <i class="fas fa-chevron-down select-arr"></i>
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-signal"></i>Priority</div>
                    <div class="priority-row">
                        <div class="p-chip low">
                            <input type="radio" name="priorityChip" id="pLow" value="Low" />
                            <label for="pLow"><i class="fas fa-circle-dot"></i>Low</label>
                        </div>
                        <div class="p-chip med">
                            <input type="radio" name="priorityChip" id="pMed" value="Medium" checked="checked" />
                            <label for="pMed"><i class="fas fa-circle-dot"></i>Medium</label>
                        </div>
                        <div class="p-chip high">
                            <input type="radio" name="priorityChip" id="pHigh" value="High" />
                            <label for="pHigh"><i class="fas fa-circle-dot"></i>High</label>
                        </div>
                        <div class="p-chip crit">
                            <input type="radio" name="priorityChip" id="pCrit" value="Critical" />
                            <label for="pCrit"><i class="fas fa-circle-dot"></i>Critical</label>
                        </div>
                    </div>
                    <%-- Hidden server DDL carries the value to codebehind --%>
                    <asp:DropDownList ID="priorityCb" runat="server" Style="display: none">
                        <asp:ListItem Value="Low">Low</asp:ListItem>
                        <asp:ListItem Value="Medium" Selected="True">Medium</asp:ListItem>
                        <asp:ListItem Value="High">High</asp:ListItem>
                        <asp:ListItem Value="Critical">Critical</asp:ListItem>
                    </asp:DropDownList>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-pen"></i>Subject</div>
                    <div class="field-wrap">
                        <input type="text" id="subjectTb" runat="server" placeholder="Brief summary of your issue" />
                        <i class="fas fa-pen field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <div class="field-label"><i class="fas fa-align-left"></i>Description</div>
                    <div class="field-wrap no-ico">
                        <asp:TextBox ID="descriptionTb" runat="server" TextMode="MultiLine"
                            placeholder="Describe your issue in detail — what happened, when it occurred, and any steps you've already tried…"
                            onkeyup="updateCount(this)"></asp:TextBox>
                    </div>
                    <div class="char-info"><span id="charCount">0</span> / 2000</div>
                </div>

                <asp:Button ID="submitBtn" runat="server" Text="Submit Support Ticket →" CssClass="btn-submit" OnClick="submitBtn_Click" />
            </div>
        </section>

        <!-- Footer -->
        <footer class="support-footer">
            <div class="footer-links">
                <a href="Home.aspx"><i class="fas fa-home"></i>Home</a>
                <a href="HelpCenter.aspx"><i class="fas fa-question-circle"></i>Help Center</a>
                <a href="login.aspx"><i class="fas fa-sign-in-alt"></i>Login</a>
            </div>
            <p>&copy; <%= DateTime.Now.Year %> Guardian Health. All rights reserved.</p>
        </footer>

    </form>
    <script>
        // Sync priority chips → hidden DropDownList
        document.querySelectorAll('input[name="priorityChip"]').forEach(function (r) {
            r.addEventListener('change', function () {
                var ddl = document.getElementById('<%= priorityCb.ClientID %>');
            for (var i = 0; i < ddl.options.length; i++) {
                if (ddl.options[i].value === this.value) { ddl.selectedIndex = i; break; }
            }
        });
    });

        function updateCount(el) {
            document.getElementById('charCount').textContent = el.value.length;
        }
</script>
</body>
</html>
