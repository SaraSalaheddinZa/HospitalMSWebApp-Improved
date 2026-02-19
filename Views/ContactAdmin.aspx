<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ContactAdmin.aspx.cs" Inherits="HospitalMSWebApp.Views.ContactAdmin" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Guardian Health - Contact Administrator</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta charset="utf-8" />
    <link rel="stylesheet" href="../Lib/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet" />
    <style>
        :root {
            --primary-color: #2563eb;
            --primary-dark: #1d4ed8;
            --success-color: #10b981;
            --warning-color: #f59e0b;
            --danger-color: #ef4444;
            --text-dark: #1e293b;
            --text-muted: #64748b;
            --gradient-bg: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }

        * { margin: 0; padding: 0; box-sizing: border-box; }

        body {
            font-family: 'Inter', sans-serif;
            background: var(--gradient-bg);
            min-height: 100vh;
            padding: 40px 20px;
        }

        .contact-container {
            max-width: 800px;
            margin: 0 auto;
            background: white;
            border-radius: 24px;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
            overflow: hidden;
            animation: fadeInUp 0.6s ease;
        }

        @keyframes fadeInUp {
            from { opacity: 0; transform: translateY(30px); }
            to { opacity: 1; transform: translateY(0); }
        }

        .contact-header {
            background: linear-gradient(135deg, var(--primary-color), #7c3aed);
            padding: 40px;
            text-align: center;
            color: white;
        }

        .contact-header h1 {
            font-size: 2rem;
            font-weight: 700;
            margin-bottom: 8px;
        }

        .contact-header p {
            opacity: 0.9;
        }

        .contact-body {
            padding: 40px;
        }

        .back-link {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            color: var(--text-muted);
            text-decoration: none;
            margin-bottom: 24px;
            transition: color 0.3s;
        }

        .back-link:hover { color: var(--primary-color); }

        .form-group {
            margin-bottom: 24px;
        }

        .form-label {
            display: block;
            font-weight: 500;
            color: var(--text-dark);
            margin-bottom: 8px;
        }

        .input-wrapper {
            position: relative;
        }

        .input-icon {
            position: absolute;
            left: 16px;
            top: 50%;
            transform: translateY(-50%);
            color: var(--text-muted);
        }

        .form-control {
            width: 100%;
            padding: 14px 16px 14px 48px;
            border: 2px solid #e2e8f0;
            border-radius: 12px;
            font-size: 1rem;
            transition: all 0.3s;
            font-family: inherit;
        }

        .form-control:focus {
            outline: none;
            border-color: var(--primary-color);
            box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.1);
        }

        textarea.form-control {
            padding-left: 16px;
            min-height: 150px;
            resize: vertical;
        }

        .btn-submit {
            width: 100%;
            padding: 16px;
            background: linear-gradient(135deg, var(--primary-color), #7c3aed);
            border: none;
            border-radius: 12px;
            color: white;
            font-size: 1rem;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.3s;
        }

        .btn-submit:hover {
            transform: translateY(-2px);
            box-shadow: 0 10px 25px rgba(37, 99, 235, 0.3);
        }

        .alert {
            padding: 14px 16px;
            border-radius: 12px;
            margin-bottom: 24px;
            display: none;
            align-items: center;
            gap: 12px;
        }

        .alert.show { display: flex; }
        .alert-success { background: rgba(16, 185, 129, 0.1); color: var(--success-color); border: 1px solid rgba(16, 185, 129, 0.2); }
        .alert-error { background: rgba(239, 68, 68, 0.1); color: var(--danger-color); border: 1px solid rgba(239, 68, 68, 0.2); }

        .info-cards {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin-top: 32px;
            padding-top: 32px;
            border-top: 1px solid #e2e8f0;
        }

        .info-card {
            text-align: center;
            padding: 24px;
            background: #f8fafc;
            border-radius: 12px;
        }

        .info-card i {
            font-size: 2rem;
            color: var(--primary-color);
            margin-bottom: 12px;
        }

        .info-card h4 {
            font-size: 1rem;
            font-weight: 600;
            margin-bottom: 4px;
        }

        .info-card p {
            font-size: 0.875rem;
            color: var(--text-muted);
            margin: 0;
        }

        @media (max-width: 576px) {
            .contact-header { padding: 30px 20px; }
            .contact-body { padding: 24px 20px; }
        }
    </style>
</head>
<body>
    <div class="contact-container">
        <div class="contact-header">
            <h1> Contact Administrator</h1>
            <p>Need help with your account? Send us a message and we'll get back to you.</p>
        </div>
        
        <div class="contact-body">
            <form id="form1" runat="server">
                <a href="login.aspx" class="back-link">
                    <i class="fas fa-arrow-left"></i> Back to Login
                </a>

                <div id="alertBox" runat="server" class="alert"></div>

                <div class="row">
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label">Full Name</label>
                            <div class="input-wrapper">
                                <i class="input-icon fas fa-user"></i>
                                <input type="text" class="form-control" id="nameTb" runat="server" placeholder="Your full name" required />
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="form-group">
                            <label class="form-label">Email Address</label>
                            <div class="input-wrapper">
                                <i class="input-icon fas fa-envelope"></i>
                                <input type="email" class="form-control" id="emailTb" runat="server" placeholder="your@email.com" required />
                            </div>
                        </div>
                    </div>
                </div>

                <div class="form-group">
                    <label class="form-label">Request Type</label>
                    <div class="input-wrapper">
                        <i class="input-icon fas fa-tag" style="top: 28px;"></i>
                        <asp:DropDownList ID="requestTypeCb" runat="server" CssClass="form-control" style="padding-left: 48px;">
                            <asp:ListItem Value="">--- Select Request Type ---</asp:ListItem>
                            <asp:ListItem Value="New Account">Request New Account</asp:ListItem>
                            <asp:ListItem Value="Account Unlock">Unlock My Account</asp:ListItem>
                            <asp:ListItem Value="Password Reset">Password Reset Assistance</asp:ListItem>
                            <asp:ListItem Value="Technical Issue">Technical Issue</asp:ListItem>
                            <asp:ListItem Value="Other">Other</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                </div>

                <div class="form-group">
                    <label class="form-label">Message</label>
                    <textarea class="form-control" id="messageTb" runat="server" placeholder="Please describe your issue or request in detail..." required></textarea>
                </div>

                <asp:Button ID="submitBtn" runat="server" Text="Send Message" CssClass="btn-submit" OnClick="submitBtn_Click" />

                <div class="info-cards">
                    <div class="info-card">
                        <i class="fas fa-clock"></i>
                        <h4>Response Time</h4>
                        <p>Within 24 hours</p>
                    </div>
                    <div class="info-card">
                        <i class="fas fa-envelope"></i>
                        <h4>Email Us</h4>
                        <p>sarazaamout@gmail.com</p>
                    </div>
                    <div class="info-card">
                        <i class="fas fa-phone"></i>
                        <h4>Call Us</h4>
                        <p>+966 552840227</p>
                    </div>
                </div>
            </form>
        </div>
    </div>
</body>
</html>