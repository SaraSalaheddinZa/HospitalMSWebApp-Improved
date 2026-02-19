<%@ Page Title="" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Logout.aspx.cs" Inherits="HospitalMSWebApp.Views.Logout" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <!DOCTYPE html>

    <html>
    <head>
        <title>Guardian Health - Logged Out</title>
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <meta charset="utf-8" />
        <meta http-equiv="refresh" content="3;url=login.aspx" />
        <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
        <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet" />
        <style>
            :root {
                --primary-color: #2563eb;
                --success-color: #10b981;
                --text-dark: #1e293b;
                --text-muted: #64748b;
                --gradient-bg: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            }

            * {
                margin: 0;
                padding: 0;
                box-sizing: border-box;
            }

            body {
                font-family: 'Inter', sans-serif;
                background: var(--gradient-bg);
                min-height: 100vh;
                display: flex;
                justify-content: center;
                align-items: center;
                padding: 20px;
            }

            .logout-container {
                background: white;
                border-radius: 24px;
                padding: 60px 48px;
                text-align: center;
                box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
                max-width: 450px;
                width: 100%;
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

            .success-icon {
                width: 100px;
                height: 100px;
                background: linear-gradient(135deg, rgba(16, 185, 129, 0.1), rgba(6, 182, 212, 0.1));
                border-radius: 50%;
                display: flex;
                align-items: center;
                justify-content: center;
                margin: 0 auto 32px;
                animation: scaleIn 0.5s ease 0.2s both;
            }

            @keyframes scaleIn {
                from {
                    transform: scale(0);
                }

                to {
                    transform: scale(1);
                }
            }

            .success-icon i {
                font-size: 3rem;
                color: var(--success-color);
            }

            .logout-title {
                font-size: 1.75rem;
                font-weight: 700;
                color: var(--text-dark);
                margin-bottom: 12px;
            }

            .logout-message {
                color: var(--text-muted);
                font-size: 1rem;
                margin-bottom: 32px;
                line-height: 1.6;
            }

            .redirect-info {
                background: #f8fafc;
                border-radius: 12px;
                padding: 16px;
                margin-bottom: 24px;
            }

                .redirect-info p {
                    color: var(--text-muted);
                    font-size: 0.9rem;
                    margin: 0;
                }

            .countdown {
                font-weight: 700;
                color: var(--primary-color);
            }

            .btn-login {
                display: inline-flex;
                align-items: center;
                gap: 10px;
                background: linear-gradient(135deg, var(--primary-color), #7c3aed);
                color: white;
                padding: 14px 32px;
                border-radius: 12px;
                text-decoration: none;
                font-weight: 600;
                transition: all 0.3s;
            }

                .btn-login:hover {
                    transform: translateY(-2px);
                    box-shadow: 0 10px 25px rgba(37, 99, 235, 0.3);
                }

            .security-note {
                margin-top: 32px;
                padding-top: 24px;
                border-top: 1px solid #e2e8f0;
                display: flex;
                align-items: center;
                justify-content: center;
                gap: 8px;
                color: var(--text-muted);
                font-size: 0.875rem;
            }

                .security-note i {
                    color: var(--success-color);
                }

            @media (max-width: 480px) {
                .logout-container {
                    padding: 40px 24px;
                }

                .logout-title {
                    font-size: 1.5rem;
                }
            }
        </style>
    </head>
    <body>
        <form id="form1" runat="server">
            <div class="logout-container">
                <div class="success-icon">
                    <i class="fas fa-check-circle"></i>
                </div>

                <h1 class="logout-title">Successfully Logged Out</h1>
                <p class="logout-message">
                    Thank you for using Guardian Health. You have been securely logged out of your account.
                </p>

                <div class="redirect-info">
                    <p>Redirecting to login page in <span class="countdown" id="countdown">3</span> seconds...</p>
                </div>

                <a href="login.aspx" class="btn-login">
                    <i class="fas fa-sign-in-alt"></i>Login Again
                </a>

                <div class="security-note">
                    <i class="fas fa-shield-alt"></i>
                    <span>Your session has been securely terminated</span>
                </div>
            </div>
        </form>

        <script>
            let seconds = 3;
            const countdownElement = document.getElementById('countdown');

            const timer = setInterval(() => {
                seconds--;
                countdownElement.textContent = seconds;

                if (seconds <= 0) {
                    clearInterval(timer);
                }
            }, 1000);
        </script>
    </body>
    </html>
</asp:Content>
