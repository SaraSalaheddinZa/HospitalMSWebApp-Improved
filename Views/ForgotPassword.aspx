<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ForgotPassword.aspx.cs" Inherits="HospitalMSWebApp.Views.ForgotPassword" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Guardian Health - Forgot Password</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta charset="utf-8" />
    <meta name="description" content="Reset your Guardian Health account password" />
    <link rel="stylesheet" href="../Lib/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
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
            --bg-light: #f8fafc;
            --gradient-bg: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }

        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
            background: var(--gradient-bg);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 20px;
            position: relative;
            overflow-x: hidden;
        }

        .particles {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            overflow: hidden;
            z-index: 0;
            pointer-events: none;
        }

        .particle {
            position: absolute;
            width: 6px;
            height: 6px;
            background: rgba(255, 255, 255, 0.3);
            border-radius: 50%;
            animation: float 20s infinite linear;
        }

        @keyframes float {
            0% {
                transform: translateY(100vh) rotate(0deg);
                opacity: 0;
            }

            10% {
                opacity: 1;
            }

            90% {
                opacity: 1;
            }

            100% {
                transform: translateY(-100vh) rotate(720deg);
                opacity: 0;
            }
        }

        .forgot-container {
            background: white;
            border-radius: 24px;
            padding: 48px;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
            width: 100%;
            max-width: 460px;
            position: relative;
            z-index: 1;
            animation: fadeInUp 0.6s ease;
        }

        @keyframes fadeInUp {
            from {
                opacity: 0;
                transform: translateY(30px);
            }

            to {
                opacity: 1;
                transform: translateY(0);
            }
        }

        .back-link {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            color: var(--text-muted);
            text-decoration: none;
            font-size: 0.9rem;
            margin-bottom: 24px;
            transition: all 0.3s;
        }

            .back-link:hover {
                color: var(--primary-color);
            }

        .forgot-header {
            text-align: center;
            margin-bottom: 32px;
        }

        .icon-container {
            width: 80px;
            height: 80px;
            background: linear-gradient(135deg, rgba(37, 99, 235, 0.1), rgba(124, 58, 237, 0.1));
            border-radius: 20px;
            display: flex;
            align-items: center;
            justify-content: center;
            margin: 0 auto 20px;
        }

            .icon-container i {
                font-size: 2.5rem;
                color: var(--primary-color);
            }

        .forgot-title {
            font-size: 1.75rem;
            font-weight: 700;
            color: var(--text-dark);
            margin-bottom: 8px;
        }

        .forgot-subtitle {
            color: var(--text-muted);
            font-size: 0.95rem;
        }

        .form-group {
            margin-bottom: 24px;
        }

        .form-label {
            display: block;
            font-weight: 500;
            color: var(--text-dark);
            margin-bottom: 8px;
            font-size: 0.9rem;
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
            font-size: 1rem;
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
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 10px;
        }

            .btn-submit:hover {
                transform: translateY(-2px);
                box-shadow: 0 10px 25px rgba(37, 99, 235, 0.3);
            }

            .btn-submit:disabled {
                opacity: 0.6;
                cursor: not-allowed;
                transform: none;
            }

        .alert {
            padding: 14px 16px;
            border-radius: 12px;
            margin-bottom: 24px;
            display: none;
            align-items: center;
            gap: 12px;
            font-size: 0.9rem;
        }

            .alert.show {
                display: flex;
            }

        .alert-success {
            background: rgba(16, 185, 129, 0.1);
            color: var(--success-color);
            border: 1px solid rgba(16, 185, 129, 0.2);
        }

        .alert-error {
            background: rgba(239, 68, 68, 0.1);
            color: var(--danger-color);
            border: 1px solid rgba(239, 68, 68, 0.2);
        }

        .alert-info {
            background: rgba(37, 99, 235, 0.1);
            color: var(--primary-color);
            border: 1px solid rgba(37, 99, 235, 0.2);
        }

        .steps-container {
            margin-top: 32px;
            padding-top: 32px;
            border-top: 1px solid #e2e8f0;
        }

        .steps-title {
            font-size: 0.875rem;
            font-weight: 600;
            color: var(--text-dark);
            margin-bottom: 16px;
        }

        .step {
            display: flex;
            align-items: flex-start;
            gap: 12px;
            margin-bottom: 16px;
        }

        .step-number {
            width: 28px;
            height: 28px;
            min-width: 28px;
            background: var(--bg-light);
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 0.8rem;
            font-weight: 600;
            color: var(--primary-color);
        }

        .step-content h4 {
            font-size: 0.9rem;
            font-weight: 600;
            color: var(--text-dark);
            margin-bottom: 4px;
        }

        .step-content p {
            font-size: 0.85rem;
            color: var(--text-muted);
            margin: 0;
        }

        .help-section {
            text-align: center;
            margin-top: 24px;
            padding-top: 24px;
            border-top: 1px solid #e2e8f0;
        }

            .help-section p {
                color: var(--text-muted);
                font-size: 0.875rem;
                margin-bottom: 8px;
            }

            .help-section a {
                color: var(--primary-color);
                text-decoration: none;
                font-weight: 500;
            }

                .help-section a:hover {
                    text-decoration: underline;
                }

        @media (max-width: 480px) {
            .forgot-container {
                padding: 32px 24px;
            }

            .forgot-title {
                font-size: 1.5rem;
            }
        }
    </style>
</head>
<body>
    <div class="particles" id="particles"></div>

    <div class="forgot-container">
        <a href="login.aspx" class="back-link">
            <i class="fas fa-arrow-left"></i>Back to Login
        </a>

        <div class="forgot-header">
            <div class="icon-container">
                <i class="fas fa-key"></i>
            </div>
            <h1 class="forgot-title">Forgot Password?</h1>
            <p class="forgot-subtitle">Enter your email and we'll help you reset your password</p>
        </div>

        <form id="form1" runat="server">
            <div id="alertBox" runat="server" class="alert"></div>

            <div class="form-group">
                <label for="emailTb" class="form-label">Email Address</label>
                <div class="input-wrapper">
                    <i class="input-icon fas fa-envelope"></i>
                    <asp:TextBox ID="emailTb" runat="server" CssClass="form-control" TextMode="Email" placeholder="Enter your registered email" />
                </div>
            </div>

            <div class="form-group" id="roleSection" runat="server">
                <label for="roleCb" class="form-label">Your Role</label>
                <div class="input-wrapper">
                    <i class="input-icon fas fa-user-tag"></i>
                    <asp:DropDownList ID="roleCb" runat="server" CssClass="form-control">
                        <asp:ListItem Value="">--- Select Your Role ---</asp:ListItem>
                        <asp:ListItem Value="Admin">System Administrator</asp:ListItem>
                        <asp:ListItem Value="Doctor">Doctor</asp:ListItem>
                        <asp:ListItem Value="Receptionist">Receptionist</asp:ListItem>
                        <asp:ListItem Value="Laboratorian">Laboratory Technician</asp:ListItem>
                        <asp:ListItem Value="Nurse">Nurse</asp:ListItem>
                        <asp:ListItem Value="Pharmacist">Pharmacist</asp:ListItem>
                        <asp:ListItem Value="Patient">Patient Portal</asp:ListItem>
                    </asp:DropDownList>
                </div>
            </div>

            <asp:Button ID="submitBtn"
                runat="server"
                Text="Send Reset Link"
                CssClass="btn-submit"
                OnClick="submitBtn_Click"
                OnClientClick="return validateForm();" />

            <div class="steps-container">
                <h3 class="steps-title">How it works</h3>
                <div class="step">
                    <div class="step-number">1</div>
                    <div class="step-content">
                        <h4>Enter your email</h4>
                        <p>Provide the email address associated with your account</p>
                    </div>
                </div>
                <div class="step">
                    <div class="step-number">2</div>
                    <div class="step-content">
                        <h4>Check your inbox</h4>
                        <p>We'll send you a password reset link via email</p>
                    </div>
                </div>
                <div class="step">
                    <div class="step-number">3</div>
                    <div class="step-content">
                        <h4>Create new password</h4>
                        <p>Click the link and set your new secure password</p>
                    </div>
                </div>
            </div>

            <div class="help-section">
                <p>Still having trouble?</p>
                <a href="ContactAdmin.aspx"><i class="fas fa-headset"></i>Contact Administrator</a>
            </div>
        </form>
    </div>

    <script>
        // Create particles
        function createParticles() {
            const container = document.getElementById('particles');
            for (let i = 0; i < 25; i++) {
                const particle = document.createElement('div');
                particle.className = 'particle';
                particle.style.left = Math.random() * 100 + '%';
                particle.style.animationDelay = Math.random() * 20 + 's';
                particle.style.animationDuration = (Math.random() * 10 + 15) + 's';
                container.appendChild(particle);
            }
        }

        // Form validation
        function validateForm() {
            const email = document.getElementById('<%= emailTb.ClientID %>').value.trim();
            const role = document.getElementById('<%= roleCb.ClientID %>').value;

            if (!email) {
                showAlert('Please enter your email address.', 'error');
                return false;
            }

            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(email)) {
                showAlert('Please enter a valid email address.', 'error');
                return false;
            }

            if (!role) {
                showAlert('Please select your role.', 'error');
                return false;
            }

            const btn = document.getElementById('<%= submitBtn.ClientID %>');
            btn.disabled = true;
            btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Sending...';

            return true;
        }

        function showAlert(message, type) {
            const alertBox = document.getElementById('<%= alertBox.ClientID %>');
            alertBox.innerHTML = `<i class="fas fa-${type === 'error' ? 'exclamation-circle' : 'info-circle'}"></i> ${message}`;
            alertBox.className = `alert alert-${type} show`;
        }

        document.addEventListener('DOMContentLoaded', createParticles);
    </script>
</body>
</html>
