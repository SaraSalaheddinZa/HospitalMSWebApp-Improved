<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="login.aspx.cs" Inherits="HospitalMSWebApp.Views.login" EnableEventValidation="false" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Guardian Health - Secure Login</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta charset="utf-8" />
    <meta name="description" content="Hospital Management System - Secure Login Portal" />
    <meta name="robots" content="noindex, nofollow" />
    <link rel="stylesheet" href="../Lib/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet" />
    <style>
        :root {
            --primary-color: #2563eb;
            --primary-dark: #1d4ed8;
            --primary-light: #3b82f6;
            --secondary-color: #1e293b;
            --success-color: #10b981;
            --danger-color: #ef4444;
            --warning-color: #f59e0b;
            --info-color: #06b6d4;
            --light-color: #f1f5f9;
            --dark-bg: #0f172a;
            --card-bg: #ffffff;
            --text-color: #1e293b;
            --text-muted: #64748b;
            --border-color: #e2e8f0;
            --shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
            --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -2px rgba(0, 0, 0, 0.1);
            --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -4px rgba(0, 0, 0, 0.1);
            --shadow-xl: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1);
            --gradient-primary: linear-gradient(135deg, #2563eb 0%, #7c3aed 100%);
            --gradient-bg: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }
        
        [data-theme="dark"] {
            --card-bg: #1e293b;
            --text-color: #f1f5f9;
            --text-muted: #94a3b8;
            --border-color: #334155;
            --light-color: #334155;
        }

        [data-theme="dark"] body {
            background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
        }

        [data-theme="dark"] select.form-control {
            color: #f1f5f9;
            background-color: rgba(255, 255, 255, 0.05);
        }
        
        [data-theme="dark"] select.form-control option {
            background-color: #1e293b;
            color: #f1f5f9;
        }

        [data-theme="dark"] .form-control::placeholder {
            color: rgba(255, 255, 255, 0.4);
        }

        [data-theme="dark"] .input-icon {
            color: rgba(255, 255, 255, 0.6);
        }

        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: var(--gradient-bg);
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            margin: 0;
            position: relative;
            overflow-x: hidden;
            padding: 20px;
        }
        
        /* Animated Background */
        .bg-animation {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            overflow: hidden;
            z-index: 0;
            pointer-events: none;
        }
        
        .bg-blob {
            position: absolute;
            border-radius: 50%;
            filter: blur(80px);
            opacity: 0.4;
            animation: blob-float 20s infinite ease-in-out;
        }
        
        .bg-blob-1 {
            width: 400px;
            height: 400px;
            background: linear-gradient(135deg, #667eea, #764ba2);
            top: -100px;
            left: -100px;
            animation-delay: 0s;
        }
        
        .bg-blob-2 {
            width: 300px;
            height: 300px;
            background: linear-gradient(135deg, #f093fb, #f5576c);
            bottom: -50px;
            right: -50px;
            animation-delay: -5s;
        }
        
        .bg-blob-3 {
            width: 250px;
            height: 250px;
            background: linear-gradient(135deg, #4facfe, #00f2fe);
            top: 50%;
            left: 50%;
            animation-delay: -10s;
        }
        
        @keyframes blob-float {
            0%, 100% {
                transform: translate(0, 0) scale(1);
            }
            25% {
                transform: translate(50px, -50px) scale(1.1);
            }
            50% {
                transform: translate(-30px, 30px) scale(0.9);
            }
            75% {
                transform: translate(-50px, -30px) scale(1.05);
            }
        }
        
        /* Floating Particles */
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
            background: rgba(255, 255, 255, 0.4);
            border-radius: 50%;
            animation: particle-float 20s infinite linear;
        }
        
        @keyframes particle-float {
            0% {
                transform: translateY(100vh) translateX(0) rotate(0deg);
                opacity: 0;
            }
            10% {
                opacity: 1;
            }
            90% {
                opacity: 1;
            }
            100% {
                transform: translateY(-100vh) translateX(100px) rotate(720deg);
                opacity: 0;
            }
        }
        
        /* Login Container */
        .login-wrapper {
            position: relative;
            z-index: 1;
            width: 100%;
            max-width: 480px;
            animation: fadeInUp 0.6s cubic-bezier(0.4, 0, 0.2, 1);
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
        
        .login-container {
            background: var(--card-bg);
            border-radius: 24px;
            padding: 48px;
            box-shadow: var(--shadow-xl);
            border: 1px solid var(--border-color);
            transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
            position: relative;
            overflow: hidden;
        }
        
        .login-container::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            height: 4px;
            background: var(--gradient-primary);
        }
        
        .login-container:hover {
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
            transform: translateY(-4px);
        }
        
        /* Theme Toggle */
        .theme-toggle {
            position: absolute;
            top: 20px;
            right: 20px;
            background: var(--light-color);
            border: none;
            width: 44px;
            height: 44px;
            border-radius: 12px;
            cursor: pointer;
            display: flex;
            align-items: center;
            justify-content: center;
            transition: all 0.3s ease;
            box-shadow: var(--shadow-sm);
            z-index: 10;
        }
        
        .theme-toggle:hover {
            transform: rotate(180deg) scale(1.1);
            box-shadow: var(--shadow-md);
            background: var(--primary-color);
        }
        
        .theme-toggle:hover i {
            color: white;
        }
        
        .theme-toggle i {
            font-size: 1.1rem;
            color: var(--text-muted);
            transition: color 0.3s;
        }
        
        .login-header {
            text-align: center;
            margin-bottom: 40px;
        }
        
        .logo-container {
            width: 90px;
            height: 90px;
            margin: 0 auto 24px;
            background: var(--gradient-primary);
            border-radius: 24px;
            display: flex;
            align-items: center;
            justify-content: center;
            box-shadow: 0 10px 30px rgba(37, 99, 235, 0.3);
            animation: pulse 2s infinite;
            position: relative;
        }
        
        .logo-container::after {
            content: '';
            position: absolute;
            inset: -4px;
            background: var(--gradient-primary);
            border-radius: 28px;
            opacity: 0.3;
            animation: pulse-ring 2s infinite;
            z-index: -1;
        }
        
        @keyframes pulse {
            0%, 100% {
                transform: scale(1);
            }
            50% {
                transform: scale(1.05);
            }
        }
        
        @keyframes pulse-ring {
            0% {
                transform: scale(1);
                opacity: 0.3;
            }
            100% {
                transform: scale(1.3);
                opacity: 0;
            }
        }
        
        .logo-container i {
            font-size: 2.8rem;
            color: white;
        }
        
        .login-title {
            font-size: 2rem;
            color: var(--text-color);
            font-weight: 700;
            margin-bottom: 8px;
            letter-spacing: -0.5px;
        }
        
        .login-subtitle {
            color: var(--text-muted);
            font-size: 0.95rem;
            font-weight: 400;
        }
        
        /* Form Styles */
        .form-group {
            margin-bottom: 24px;
            position: relative;
        }
        
        .form-label {
            color: var(--text-color);
            font-weight: 500;
            margin-bottom: 8px;
            display: block;
            font-size: 0.875rem;
        }
        
        .input-wrapper {
            position: relative;
            display: flex;
            align-items: center;
        }
        
        .input-icon {
            position: absolute;
            left: 16px;
            color: var(--text-muted);
            font-size: 1rem;
            z-index: 2;
            transition: all 0.3s;
        }
        
        .form-control {
            width: 100%;
            padding: 14px 16px 14px 48px;
            border: 2px solid var(--border-color);
            border-radius: 12px;
            font-size: 0.95rem;
            transition: all 0.3s ease;
            background: var(--card-bg);
            color: var(--text-color);
            font-family: inherit;
        }
        
        .form-control:focus {
            outline: none;
            border-color: var(--primary-color);
            box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.1);
        }
        
        .form-control:focus + .input-icon,
        .input-wrapper:focus-within .input-icon {
            color: var(--primary-color);
        }
        
        .form-control::placeholder {
            color: var(--text-muted);
            opacity: 0.7;
        }
        
        
        .password-toggle {
            position: absolute;
            right: 12px;
            background: none;
            border: none;
            color: var(--text-muted);
            cursor: pointer;
            font-size: 1rem;
            z-index: 2;
            padding: 8px;
            border-radius: 8px;
            transition: all 0.3s;
        }
        
        .password-toggle:hover {
            color: var(--primary-color);
            background: rgba(37, 99, 235, 0.1);
        }
        
        
        .password-strength {
            height: 4px;
            background: var(--border-color);
            border-radius: 2px;
            margin-top: 8px;
            overflow: hidden;
            display: none;
        }
        
        .password-strength-bar {
            height: 100%;
            width: 0%;
            transition: all 0.3s ease;
            border-radius: 2px;
        }
        
        .strength-weak { width: 33%; background: var(--danger-color); }
        .strength-medium { width: 66%; background: var(--warning-color); }
        .strength-strong { width: 100%; background: var(--success-color); }
        
        
        select.form-control {
            appearance: none;
            background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='%2364748b' viewBox='0 0 16 16'%3E%3Cpath d='M7.247 11.14 2.451 5.658C1.885 5.013 2.345 4 3.204 4h9.592a1 1 0 0 1 .753 1.659l-4.796 5.48a1 1 0 0 1-1.506 0z'/%3E%3C/svg%3E");
            background-repeat: no-repeat;
            background-position: right 16px center;
            padding-right: 48px;
            cursor: pointer;
        }
        
        
        .remember-forgot {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin: 24px 0;
        }
        
        .remember-me {
            display: flex;
            align-items: center;
            gap: 10px;
            cursor: pointer;
        }
        
        .custom-checkbox {
            width: 20px;
            height: 20px;
            border: 2px solid var(--border-color);
            border-radius: 6px;
            display: flex;
            align-items: center;
            justify-content: center;
            transition: all 0.3s;
            cursor: pointer;
        }
        
        .custom-checkbox:hover {
            border-color: var(--primary-color);
        }
        
        .custom-checkbox.checked {
            background: var(--primary-color);
            border-color: var(--primary-color);
        }
        
        .custom-checkbox i {
            color: white;
            font-size: 0.7rem;
            opacity: 0;
            transition: opacity 0.3s;
        }
        
        .custom-checkbox.checked i {
            opacity: 1;
        }
        
        .remember-me span {
            color: var(--text-color);
            font-size: 0.875rem;
            user-select: none;
        }
        
        .forgot-password {
            color: var(--primary-color);
            text-decoration: none;
            font-size: 0.875rem;
            font-weight: 500;
            transition: all 0.3s;
        }
        
        .forgot-password:hover {
            color: var(--primary-dark);
            text-decoration: underline;
        }
        
        
        .btn-login {
            width: 100%;
            padding: 16px;
            background: var(--gradient-primary);
            border: none;
            border-radius: 12px;
            color: white;
            font-size: 1rem;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
            box-shadow: 0 4px 15px rgba(37, 99, 235, 0.3);
            position: relative;
            overflow: hidden;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 10px;
            font-family: inherit;
        }
        
        .btn-login::before {
            content: '';
            position: absolute;
            top: 50%;
            left: 50%;
            width: 0;
            height: 0;
            border-radius: 50%;
            background: rgba(255, 255, 255, 0.2);
            transform: translate(-50%, -50%);
            transition: width 0.6s, height 0.6s;
        }
        
        .btn-login:hover::before {
            width: 400px;
            height: 400px;
        }
        
        .btn-login:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 25px rgba(37, 99, 235, 0.4);
        }
        
        .btn-login:active {
            transform: translateY(0);
        }
        
        .btn-login:disabled {
            opacity: 0.6;
            cursor: not-allowed;
            transform: none;
        }
        
        .btn-login.loading .btn-text {
            opacity: 0.8;
        }
        
        .spinner {
            display: none;
            width: 18px;
            height: 18px;
            border: 2px solid rgba(255, 255, 255, 0.3);
            border-top-color: white;
            border-radius: 50%;
            animation: spin 0.8s linear infinite;
        }
        
        .btn-login.loading .spinner {
            display: inline-block;
        }
        
        @keyframes spin {
            to { transform: rotate(360deg); }
        }
        
        
        .alert {
            padding: 14px 16px;
            border-radius: 12px;
            margin-bottom: 24px;
            display: none;
            align-items: center;
            gap: 12px;
            font-size: 0.9rem;
            animation: slideDown 0.3s ease;
        }
        
        .alert.show {
            display: flex;
        }
        
        @keyframes slideDown {
            from {
                opacity: 0;
                transform: translateY(-10px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }
        
        .alert-error {
            background: rgba(239, 68, 68, 0.1);
            color: var(--danger-color);
            border: 1px solid rgba(239, 68, 68, 0.2);
        }
        
        .alert-success {
            background: rgba(16, 185, 129, 0.1);
            color: var(--success-color);
            border: 1px solid rgba(16, 185, 129, 0.2);
        }
        
        .alert-warning {
            background: rgba(245, 158, 11, 0.1);
            color: var(--warning-color);
            border: 1px solid rgba(245, 158, 11, 0.2);
        }
        
        .alert i {
            font-size: 1.1rem;
        }
        
        .lockout-warning {
            background: rgba(245, 158, 11, 0.1);
            border: 1px solid rgba(245, 158, 11, 0.2);
            color: var(--warning-color);
            padding: 14px;
            border-radius: 12px;
            margin-bottom: 20px;
            text-align: center;
            font-size: 0.9rem;
            display: none;
            animation: shake 0.5s;
        }
        
        .lockout-warning.show {
            display: block;
        }
        
        @keyframes shake {
            0%, 100% { transform: translateX(0); }
            25% { transform: translateX(-10px); }
            75% { transform: translateX(10px); }
        }
        
        .divider {
            text-align: center;
            margin: 28px 0;
            position: relative;
        }
        
        .divider::before {
            content: '';
            position: absolute;
            top: 50%;
            left: 0;
            right: 0;
            height: 1px;
            background: linear-gradient(to right, transparent, var(--border-color), transparent);
        }
        
        .divider span {
            background: var(--card-bg);
            padding: 0 16px;
            color: var(--text-muted);
            font-size: 0.85rem;
            position: relative;
            z-index: 1;
        }
        
        .footer-links {
            text-align: center;
        }
        
        .footer-links p {
            color: var(--text-muted);
            font-size: 0.875rem;
            margin-bottom: 12px;
        }
        
        .footer-links a {
            color: var(--primary-color);
            text-decoration: none;
            font-weight: 500;
            transition: all 0.3s;
        }
        
        .footer-links a:hover {
            color: var(--primary-dark);
            text-decoration: underline;
        }
        
        .help-links {
            display: flex;
            justify-content: center;
            gap: 24px;
            margin-top: 16px;
        }
        
        .help-links a {
            display: flex;
            align-items: center;
            gap: 6px;
            color: var(--text-muted);
            font-size: 0.85rem;
            transition: all 0.3s;
        }
        
        .help-links a:hover {
            color: var(--primary-color);
            text-decoration: none;
        }
        
        .security-badge {
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
            margin-top: 24px;
            padding-top: 24px;
            border-top: 1px solid var(--border-color);
            color: var(--text-muted);
            font-size: 0.8rem;
        }
        
        .security-badge i {
            color: var(--success-color);
        }
        
        
        .back-home {
            position: fixed;
            top: 24px;
            left: 24px;
            z-index: 100;
        }
        
        .back-home a {
            display: flex;
            align-items: center;
            gap: 8px;
            color: white;
            text-decoration: none;
            font-size: 0.9rem;
            font-weight: 500;
            padding: 10px 16px;
            background: rgba(255, 255, 255, 0.15);
            backdrop-filter: blur(10px);
            border-radius: 12px;
            transition: all 0.3s;
        }
        
        .back-home a:hover {
            background: rgba(255, 255, 255, 0.25);
            transform: translateX(-4px);
        }
        
        @media (max-width: 576px) {
            body {
                padding: 16px;
            }
            
            .login-container {
                padding: 32px 24px;
                border-radius: 20px;
            }
            
            .login-title {
                font-size: 1.6rem;
            }
            
            .logo-container {
                width: 70px;
                height: 70px;
            }
            
            .logo-container i {
                font-size: 2.2rem;
            }
            
            .remember-forgot {
                flex-direction: column;
                gap: 12px;
                align-items: flex-start;
            }
            
            .back-home {
                position: relative;
                top: auto;
                left: auto;
                margin-bottom: 16px;
                text-align: center;
            }
            
            .back-home a {
                display: inline-flex;
                color: var(--text-color);
                background: var(--card-bg);
                box-shadow: var(--shadow-md);
            }
        }
        
        
        *:focus-visible {
            outline: 2px solid var(--primary-color);
            outline-offset: 2px;
        }
        
        
        @media (prefers-reduced-motion: reduce) {
            *,
            *::before,
            *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
            }
        }
    </style>
</head>
<body>
    <div class="bg-animation">
        <div class="bg-blob bg-blob-1"></div>
        <div class="bg-blob bg-blob-2"></div>
        <div class="bg-blob bg-blob-3"></div>
    </div>
    
    <div class="particles" id="particles"></div>
    
    <div class="back-home">
        <a href="Guest/Home.aspx"><i class="fas fa-arrow-left"></i> Back to Home</a>
    </div>
    
    <div class="login-wrapper">
        <div class="login-container">
            <button type="button" class="theme-toggle" id="themeToggle" title="Toggle Dark Mode" aria-label="Toggle Dark Mode">
                <i class="fas fa-moon"></i>
            </button>
            
            <div class="login-header">
                <div class="logo-container">
                    <i class="fas fa-heartbeat"></i>
                </div>
                <h1 class="login-title">Welcome Back</h1>
                <p class="login-subtitle">Sign in to access your Guardian Health account</p>
            </div>
            
            <form id="form1" runat="server">
                <div class="lockout-warning" id="lockoutWarning">
                    <i class="fas fa-exclamation-triangle"></i>
                    <span id="lockoutText"></span>
                </div>
                
                <div id="ErrMsg" runat="server" class="alert"></div>
                
                <div class="form-group">
                    <label for="emailTb" class="form-label">Email Address</label>
                    <div class="input-wrapper">
                        <i class="input-icon fas fa-envelope"></i>
                        <input 
                            type="email" 
                            class="form-control" 
                            id="emailTb" 
                            placeholder="name@hospital.com" 
                            runat="server" 
                            required="required"
                            autocomplete="email"
                            aria-label="Email Address" />
                    </div>
                </div>
                
                
                <div class="form-group">
                    <label for="passwordTb" class="form-label">Password</label>
                    <div class="input-wrapper">
                        <i class="input-icon fas fa-lock"></i>
                        <input 
                            type="password" 
                            class="form-control" 
                            id="passwordTb" 
                            placeholder="Enter your password" 
                            runat="server" 
                            required="required"
                            autocomplete="current-password"
                            aria-label="Password" />
                        <button type="button" class="password-toggle" id="togglePassword" aria-label="Toggle password visibility">
                            <i class="fas fa-eye"></i>
                        </button>
                    </div>
                    <div class="password-strength" id="passwordStrength">
                        <div class="password-strength-bar" id="strengthBar"></div>
                    </div>
                </div>
                
                <div class="form-group">
                    <label for="roleCb" class="form-label">Select Role</label>
                    <div class="input-wrapper">
                        <i class="input-icon fas fa-user-tag"></i>
                        <asp:DropDownList ID="roleCb" runat="server" class="form-control" aria-label="Select Role">
                            <asp:ListItem Value="">--- Select Your Role ---</asp:ListItem>
                            <asp:ListItem Value="Admin">System Administrator</asp:ListItem>
                            <asp:ListItem Value="Doctor">Doctor</asp:ListItem>
                            <asp:ListItem Value="Receptionist">Receptionist</asp:ListItem>
                            <asp:ListItem Value="Laboratorian">Laboratory Technician</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                </div>
                
                <div class="remember-forgot">
                    <div class="remember-me" onclick="toggleRememberMe()">
                        <div class="custom-checkbox" id="customCheckbox">
                            <i class="fas fa-check"></i>
                        </div>
                        <asp:CheckBox ID="rememberMeCb" runat="server" style="display: none;" />
                        <span>Remember me</span>
                    </div>
                    <a href="ForgotPassword.aspx" class="forgot-password">Forgot Password?</a>
                </div>

                
                <asp:Button 
                    ID="loginBtn" 
                    runat="server" 
                    Text="Sign In" 
                    class="btn-login" 
                    OnClick="loginBtn_Click"
                    OnClientClick="return handleLoginClick();" />
                
                
                <div class="divider">
                    <span>Need Help?</span>
                </div>
                
                
                <div class="footer-links">
                    <p>Don't have an account? <a href="ContactAdmin.aspx">Contact Administrator</a></p>
                    <div class="help-links">
                        <a href="HelpCenter.aspx"><i class="fas fa-question-circle"></i> Help Center</a>
                        <a href="Support.aspx"><i class="fas fa-headset"></i> Support</a>
                    </div>
                </div>
                
                
                <div class="security-badge">
                    <i class="fas fa-shield-alt"></i>
                    <span>Secure 256-bit SSL Encryption</span>
                </div>
            </form>
        </div>
    </div>
    
    <script src="../Lib/js/bootstrap.bundle.min.js"></script>
    <script>
        
        function createParticles() {
            const particlesContainer = document.getElementById('particles');
            const particleCount = 30;

            for (let i = 0; i < particleCount; i++) {
                const particle = document.createElement('div');
                particle.className = 'particle';
                particle.style.left = Math.random() * 100 + '%';
                particle.style.animationDelay = Math.random() * 20 + 's';
                particle.style.animationDuration = (Math.random() * 15 + 15) + 's';
                particle.style.width = (Math.random() * 4 + 2) + 'px';
                particle.style.height = particle.style.width;
                particlesContainer.appendChild(particle);
            }
        }

        
        const themeToggle = document.getElementById('themeToggle');
        const html = document.documentElement;

        const savedTheme = localStorage.getItem('theme') || 'light';
        html.setAttribute('data-theme', savedTheme);
        updateThemeIcon(savedTheme);

        themeToggle.addEventListener('click', () => {
            const currentTheme = html.getAttribute('data-theme');
            const newTheme = currentTheme === 'light' ? 'dark' : 'light';
            html.setAttribute('data-theme', newTheme);
            localStorage.setItem('theme', newTheme);
            updateThemeIcon(newTheme);
        });

        function updateThemeIcon(theme) {
            const icon = themeToggle.querySelector('i');
            icon.className = theme === 'light' ? 'fas fa-moon' : 'fas fa-sun';
        }

        
        const togglePassword = document.getElementById('togglePassword');
        const passwordInput = document.getElementById('<%= passwordTb.ClientID %>');

        togglePassword.addEventListener('click', () => {
            const type = passwordInput.getAttribute('type');
            passwordInput.setAttribute('type', type === 'password' ? 'text' : 'password');
            togglePassword.querySelector('i').className = type === 'password' ? 'fas fa-eye-slash' : 'fas fa-eye';
        });

        
        const strengthIndicator = document.getElementById('passwordStrength');
        const strengthBar = document.getElementById('strengthBar');

        passwordInput.addEventListener('input', () => {
            const password = passwordInput.value;

            if (password.length === 0) {
                strengthIndicator.style.display = 'none';
                return;
            }

            strengthIndicator.style.display = 'block';

            let strength = 0;
            if (password.length >= 8) strength++;
            if (/[a-z]/.test(password) && /[A-Z]/.test(password)) strength++;
            if (/[0-9]/.test(password)) strength++;
            if (/[^a-zA-Z0-9]/.test(password)) strength++;

            strengthBar.className = 'password-strength-bar';
            if (strength <= 2) {
                strengthBar.classList.add('strength-weak');
            } else if (strength === 3) {
                strengthBar.classList.add('strength-medium');
            } else {
                strengthBar.classList.add('strength-strong');
            }
        });

        
        function toggleRememberMe() {
            const checkbox = document.getElementById('<%= rememberMeCb.ClientID %>');
            const customCheckbox = document.getElementById('customCheckbox');
            checkbox.checked = !checkbox.checked;
            customCheckbox.classList.toggle('checked', checkbox.checked);
        }

       
        let loginAttempts = parseInt(sessionStorage.getItem('loginAttempts') || '0');
        const maxAttempts = 5;

        function updateLoginAttempts() {
            const lockoutDiv = document.getElementById('lockoutWarning');
            const lockoutText = document.getElementById('lockoutText');

            if (loginAttempts > 0) {
                const remaining = maxAttempts - loginAttempts;
                lockoutText.textContent = `Warning: ${remaining} login attempts remaining`;
                lockoutDiv.classList.add('show');

                if (loginAttempts >= maxAttempts) {
                    const loginBtn = document.getElementById('<%= loginBtn.ClientID %>');
                    loginBtn.disabled = true;
                    loginBtn.innerHTML = '<span class="btn-text">Account Locked</span>';
                    lockoutText.textContent = 'Maximum attempts exceeded. Please try again in 15 minutes.';

                    setTimeout(() => {
                        sessionStorage.setItem('loginAttempts', '0');
                        loginAttempts = 0;
                        loginBtn.disabled = false;
                        loginBtn.innerHTML = '<span class="btn-text">Sign In</span>';
                        lockoutDiv.classList.remove('show');
                    }, 900000);
                }
            }
        }

        
        function handleLoginClick() {
            const email = document.getElementById('<%= emailTb.ClientID %>').value.trim();
            const password = document.getElementById('<%= passwordTb.ClientID %>').value;
            const role = document.getElementById('<%= roleCb.ClientID %>').value;
            const errMsg = document.getElementById('ErrMsg');

            
            errMsg.className = 'alert';
            errMsg.innerHTML = '';

            
            if (!email) {
                showMessage('Please enter your email address.', 'error');
                return false;
            }

            if (!password) {
                showMessage('Please enter your password.', 'error');
                return false;
            }

            if (!role) {
                showMessage('Please select your role.', 'error');
                return false;
            }

            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(email)) {
                showMessage('Please enter a valid email address.', 'error');
                return false;
            }
            const loginBtn = document.getElementById('<%= loginBtn.ClientID %>');
            loginBtn.classList.add('loading');
            loginBtn.innerHTML = '<span class="spinner"></span><span class="btn-text">Signing in...</span>';

            return true;
        }

        function showMessage(message, type) {
            const errMsg = document.getElementById('ErrMsg');
            const icon = type === 'error' ? 'fa-exclamation-circle' : type === 'success' ? 'fa-check-circle' : 'fa-exclamation-triangle';
            errMsg.innerHTML = `<i class="fas ${icon}"></i><span>${message}</span>`;
            errMsg.className = `alert alert-${type} show`;
        }

        
        document.addEventListener('DOMContentLoaded', () => {
            createParticles();
            updateLoginAttempts();

            
            const savedEmail = localStorage.getItem('rememberedEmail');
            if (savedEmail) {
                document.getElementById('<%= emailTb.ClientID %>').value = savedEmail;
                document.getElementById('<%= rememberMeCb.ClientID %>').checked = true;
                document.getElementById('customCheckbox').classList.add('checked');
            }

            
            document.getElementById('form1').addEventListener('submit', (e) => {
                const rememberMe = document.getElementById('<%= rememberMeCb.ClientID %>').checked;
                const email = document.getElementById('<%= emailTb.ClientID %>').value;

                if (rememberMe) {
                    localStorage.setItem('rememberedEmail', email);
                } else {
                    localStorage.removeItem('rememberedEmail');
                }
            });
        });

       
        document.querySelectorAll('.form-control').forEach(input => {
            input.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    handleLoginClick();
                }
            });
        });
    </script>
</body>
</html>
