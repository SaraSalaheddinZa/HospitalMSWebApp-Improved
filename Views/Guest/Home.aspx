<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Home.aspx.cs" Inherits="HospitalMSWebApp.Views.Guest.Home" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Guardian Health Hospital</title>
    <link rel="stylesheet" href="../../Lib/css/bootstrap.min.css" />
    <link href="https://cdn.jsdelivr.net/npm/aos@2.3.4/dist/aos.css" rel="stylesheet" />
    <link href="https://fonts.googleapis.com/css2?family=Poppins:wght@400;600&display=swap" rel="stylesheet" />
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700;800&display=swap" rel="stylesheet" />

    <style>
        :root {
            --primary-color: #2563eb;
            --primary-dark: #1d4ed8;
            --primary-light: #3b82f6;
            --secondary-color: #1e293b;
            --accent-color: #06b6d4;
            --success-color: #10b981;
            --warning-color: #f59e0b;
            --danger-color: #ef4444;
            --text-dark: #1e293b;
            --text-light: #64748b;
            --bg-light: #f8fafc;
            --white: #ffffff;
            --shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
            --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
            --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
            --shadow-xl: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
            --gradient-primary: linear-gradient(135deg, #2563eb 0%, #7c3aed 100%);
            --gradient-hero: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }

        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        html {
            scroll-behavior: smooth;
        }

        body {
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
            color: var(--text-dark);
            line-height: 1.6;
            overflow-x: hidden;
        }

        /* Navigation */
        .navbar {
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            z-index: 1000;
            padding: 16px 0;
            transition: all 0.3s ease;
            background: transparent;
        }

            .navbar.scrolled {
                background: rgba(255, 255, 255, 0.95);
                backdrop-filter: blur(20px);
                box-shadow: var(--shadow-md);
                padding: 12px 0;
            }

        .navbar-brand {
            display: flex;
            align-items: center;
            gap: 12px;
            font-weight: 700;
            font-size: 1.5rem;
            color: var(--white);
            text-decoration: none;
        }

        .navbar.scrolled .navbar-brand {
            color: var(--primary-color);
        }

        .navbar-brand i {
            font-size: 1.8rem;
        }

        .navbar-brand span {
            color: var(--white);
        }

        .nav-link {
            color: rgba(255, 255, 255, 0.9);
            font-weight: 500;
            padding: 8px 16px;
            transition: all 0.3s;
            text-decoration: none;
        }

        .navbar.scrolled .nav-link {
            color: var(--text-dark);
        }

        .nav-link:hover {
            color: var(--white);
        }

        .navbar.scrolled .nav-link:hover {
            color: var(--primary-color);
        }

        .btn-nav {
            background: var(--white);
            color: var(--primary-color);
            padding: 10px 24px;
            border-radius: 50px;
            font-weight: 600;
            text-decoration: none;
            transition: all 0.3s;
            margin-left: 16px;
        }

        .navbar.scrolled .btn-nav {
            background: var(--gradient-primary);
            color: var(--white);
        }

        .btn-nav:hover {
            transform: translateY(-2px);
            box-shadow: var(--shadow-lg);
        }

        /* Hero Section */
        .hero {
            min-height: 100vh;
            background: var(--gradient-hero);
            display: flex;
            align-items: center;
            position: relative;
            overflow: hidden;
            padding: 120px 0 80px;
        }

            .hero::before {
                content: '';
                position: absolute;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='0.05'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E");
                opacity: 0.5;
            }

        .hero-particles {
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            overflow: hidden;
            pointer-events: none;
        }

        .hero-particle {
            position: absolute;
            width: 8px;
            height: 8px;
            background: rgba(255, 255, 255, 0.3);
            border-radius: 50%;
            animation: float-up 20s infinite linear;
        }

        @keyframes float-up {
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

        .hero-content {
            position: relative;
            z-index: 2;
        }

        .hero-badge {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            background: rgba(255, 255, 255, 0.15);
            backdrop-filter: blur(10px);
            padding: 8px 20px;
            border-radius: 50px;
            color: var(--white);
            font-size: 0.875rem;
            font-weight: 500;
            margin-bottom: 24px;
        }

            .hero-badge i {
                color: var(--success-color);
            }

        .hero-title {
            font-size: 4rem;
            font-weight: 800;
            color: var(--white);
            line-height: 1.1;
            margin-bottom: 24px;
        }

            .hero-title span {
                color: #fcd34d;
            }

        .hero-subtitle {
            font-size: 1.25rem;
            color: rgba(255, 255, 255, 0.85);
            max-width: 500px;
            margin-bottom: 40px;
        }

        .hero-buttons {
            display: flex;
            gap: 16px;
            flex-wrap: wrap;
        }

        .btn-primary-custom {
            background: var(--white);
            color: var(--primary-color);
            padding: 16px 32px;
            border-radius: 12px;
            font-weight: 600;
            font-size: 1rem;
            text-decoration: none;
            display: inline-flex;
            align-items: center;
            gap: 10px;
            transition: all 0.3s;
            border: none;
            cursor: pointer;
        }

            .btn-primary-custom:hover {
                transform: translateY(-3px);
                box-shadow: 0 20px 40px rgba(0, 0, 0, 0.2);
                color: var(--primary-dark);
            }

        .btn-secondary-custom {
            background: transparent;
            color: var(--white);
            padding: 16px 32px;
            border-radius: 12px;
            font-weight: 600;
            font-size: 1rem;
            text-decoration: none;
            display: inline-flex;
            align-items: center;
            gap: 10px;
            transition: all 0.3s;
            border: 2px solid rgba(255, 255, 255, 0.3);
        }

            .btn-secondary-custom:hover {
                background: rgba(255, 255, 255, 0.1);
                border-color: var(--white);
                color: var(--white);
            }

        .hero-stats {
            display: flex;
            gap: 48px;
            margin-top: 60px;
            padding-top: 40px;
            border-top: 1px solid rgba(255, 255, 255, 0.2);
        }

        .hero-stat {
            text-align: center;
        }

        .hero-stat-number {
            font-size: 2.5rem;
            font-weight: 800;
            color: var(--white);
        }

        .hero-stat-label {
            font-size: 0.875rem;
            color: rgba(255, 255, 255, 0.7);
        }

        .hero-image {
            position: relative;
            z-index: 2;
        }

        .hero-image-wrapper {
            position: relative;
            border-radius: 24px;
            overflow: hidden;
            box-shadow: 0 40px 80px rgba(0, 0, 0, 0.3);
        }

            .hero-image-wrapper img {
                width: 100%;
                height: auto;
                display: block;
            }

        .floating-card {
            position: absolute;
            background: var(--white);
            border-radius: 16px;
            padding: 20px;
            box-shadow: var(--shadow-xl);
            animation: float 3s ease-in-out infinite;
        }

        .floating-card-1 {
            top: 20%;
            left: -30px;
            animation-delay: 0s;
        }

        .floating-card-2 {
            bottom: 20%;
            right: -20px;
            animation-delay: 1.5s;
        }

        @keyframes float {
            0%, 100% {
                transform: translateY(0);
            }

            50% {
                transform: translateY(-15px);
            }
        }

        .floating-card-icon {
            width: 50px;
            height: 50px;
            border-radius: 12px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.5rem;
            margin-bottom: 12px;
        }

        .floating-card-1 .floating-card-icon {
            background: rgba(37, 99, 235, 0.1);
            color: var(--primary-color);
        }

        .floating-card-2 .floating-card-icon {
            background: rgba(16, 185, 129, 0.1);
            color: var(--success-color);
        }

        .floating-card-title {
            font-weight: 700;
            font-size: 1.1rem;
            margin-bottom: 4px;
        }

        .floating-card-text {
            font-size: 0.875rem;
            color: var(--text-light);
        }

        /* Features Section */
        .features {
            padding: 100px 0;
            background: var(--bg-light);
        }

        .section-header {
            text-align: center;
            margin-bottom: 60px;
        }

        .section-badge {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            background: rgba(37, 99, 235, 0.1);
            color: var(--primary-color);
            padding: 8px 20px;
            border-radius: 50px;
            font-size: 0.875rem;
            font-weight: 600;
            margin-bottom: 16px;
        }

        .section-title {
            font-size: 2.5rem;
            font-weight: 800;
            color: var(--text-dark);
            margin-bottom: 16px;
        }

        .section-subtitle {
            font-size: 1.125rem;
            color: var(--text-light);
            max-width: 600px;
            margin: 0 auto;
        }

        .feature-card {
            background: var(--white);
            border-radius: 20px;
            padding: 40px 30px;
            text-align: center;
            transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
            border: 1px solid transparent;
            height: 100%;
        }

            .feature-card:hover {
                transform: translateY(-10px);
                box-shadow: var(--shadow-xl);
                border-color: var(--border-color);
            }

        .feature-icon {
            width: 80px;
            height: 80px;
            border-radius: 20px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 2rem;
            margin: 0 auto 24px;
            transition: all 0.3s;
        }

        .feature-card:hover .feature-icon {
            transform: scale(1.1);
        }

        .feature-icon.primary {
            background: linear-gradient(135deg, rgba(37, 99, 235, 0.1), rgba(124, 58, 237, 0.1));
            color: var(--primary-color);
        }

        .feature-icon.success {
            background: linear-gradient(135deg, rgba(16, 185, 129, 0.1), rgba(6, 182, 212, 0.1));
            color: var(--success-color);
        }

        .feature-icon.warning {
            background: linear-gradient(135deg, rgba(245, 158, 11, 0.1), rgba(239, 68, 68, 0.1));
            color: var(--warning-color);
        }

        .feature-icon.info {
            background: linear-gradient(135deg, rgba(6, 182, 212, 0.1), rgba(37, 99, 235, 0.1));
            color: var(--accent-color);
        }

        .feature-title {
            font-size: 1.25rem;
            font-weight: 700;
            margin-bottom: 12px;
            color: var(--text-dark);
        }

        .feature-text {
            color: var(--text-light);
            font-size: 0.95rem;
            line-height: 1.7;
        }

        /* Services Section */
        .services {
            padding: 100px 0;
            background: var(--white);
        }

        .service-item {
            display: flex;
            align-items: flex-start;
            gap: 24px;
            padding: 32px;
            border-radius: 16px;
            transition: all 0.3s;
            margin-bottom: 24px;
        }

            .service-item:hover {
                background: var(--bg-light);
                transform: translateX(10px);
            }

        .service-icon {
            width: 60px;
            height: 60px;
            min-width: 60px;
            border-radius: 16px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.5rem;
            background: var(--gradient-primary);
            color: var(--white);
        }

        .service-content h4 {
            font-size: 1.2rem;
            font-weight: 700;
            margin-bottom: 8px;
            color: var(--text-dark);
        }

        .service-content p {
            color: var(--text-light);
            font-size: 0.95rem;
            margin-bottom: 0;
        }

        /* Stats Section */
        .stats-section {
            padding: 80px 0;
            background: var(--gradient-primary);
            position: relative;
            overflow: hidden;
        }

            .stats-section::before {
                content: '';
                position: absolute;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='0.05'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E");
            }

        .stat-box {
            text-align: center;
            color: var(--white);
            padding: 20px;
        }

            .stat-box i {
                font-size: 2.5rem;
                margin-bottom: 16px;
                opacity: 0.9;
            }

        .stat-number {
            font-size: 3rem;
            font-weight: 800;
            margin-bottom: 8px;
        }

        .stat-label {
            font-size: 1rem;
            opacity: 0.9;
        }

        /* Testimonials */
        .testimonials {
            padding: 100px 0;
            background: var(--bg-light);
        }

        .testimonial-card {
            background: var(--white);
            border-radius: 20px;
            padding: 40px;
            box-shadow: var(--shadow-md);
            transition: all 0.3s;
            height: 100%;
        }

            .testimonial-card:hover {
                box-shadow: var(--shadow-xl);
                transform: translateY(-5px);
            }

        .testimonial-quote {
            font-size: 3rem;
            color: var(--primary-color);
            opacity: 0.3;
            line-height: 1;
            margin-bottom: 16px;
        }

        .testimonial-text {
            font-size: 1rem;
            color: var(--text-dark);
            line-height: 1.8;
            margin-bottom: 24px;
            font-style: italic;
        }

        .testimonial-author {
            display: flex;
            align-items: center;
            gap: 16px;
        }

        .testimonial-avatar {
            width: 56px;
            height: 56px;
            border-radius: 50%;
            background: var(--gradient-primary);
            display: flex;
            align-items: center;
            justify-content: center;
            color: var(--white);
            font-size: 1.5rem;
            font-weight: 600;
        }

        .testimonial-info h5 {
            font-size: 1rem;
            font-weight: 700;
            margin-bottom: 4px;
            color: var(--text-dark);
        }

        .testimonial-info span {
            font-size: 0.875rem;
            color: var(--text-light);
        }

        .testimonial-rating {
            color: var(--warning-color);
            margin-top: 4px;
        }

        /* CTA Section */
        .cta-section {
            padding: 100px 0;
            background: var(--white);
        }

        .cta-box {
            background: var(--gradient-primary);
            border-radius: 24px;
            padding: 80px;
            text-align: center;
            position: relative;
            overflow: hidden;
        }

            .cta-box::before {
                content: '';
                position: absolute;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='0.05'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E");
            }

            .cta-box h2 {
                font-size: 2.5rem;
                font-weight: 800;
                color: var(--white);
                margin-bottom: 16px;
                position: relative;
            }

            .cta-box p {
                font-size: 1.125rem;
                color: rgba(255, 255, 255, 0.9);
                max-width: 600px;
                margin: 0 auto 32px;
                position: relative;
            }

        .btn-cta {
            background: var(--white);
            color: var(--primary-color);
            padding: 16px 40px;
            border-radius: 12px;
            font-weight: 600;
            font-size: 1rem;
            text-decoration: none;
            display: inline-flex;
            align-items: center;
            gap: 10px;
            transition: all 0.3s;
            position: relative;
        }

            .btn-cta:hover {
                transform: translateY(-3px);
                box-shadow: 0 20px 40px rgba(0, 0, 0, 0.2);
                color: var(--primary-dark);
            }


        .footer {
            background: var(--secondary-color);
            color: var(--white);
            padding: 80px 0 30px;
        }

        .footer-brand {
            display: flex;
            align-items: center;
            gap: 12px;
            font-weight: 700;
            font-size: 1.5rem;
            color: var(--white);
            text-decoration: none;
            margin-bottom: 20px;
        }

            .footer-brand i {
                font-size: 1.8rem;
                color: var(--primary-light);
            }

        .footer-about {
            color: rgba(255, 255, 255, 0.7);
            font-size: 0.95rem;
            line-height: 1.8;
            margin-bottom: 24px;
        }

        .footer-social {
            display: flex;
            gap: 12px;
        }

            .footer-social a {
                width: 44px;
                height: 44px;
                border-radius: 12px;
                background: rgba(255, 255, 255, 0.1);
                display: flex;
                align-items: center;
                justify-content: center;
                color: var(--white);
                text-decoration: none;
                transition: all 0.3s;
            }

                .footer-social a:hover {
                    background: var(--primary-color);
                    transform: translateY(-3px);
                }

        .footer-title {
            font-size: 1.1rem;
            font-weight: 700;
            margin-bottom: 24px;
            color: var(--white);
        }

        .footer-links {
            list-style: none;
            padding: 0;
            margin: 0;
        }

            .footer-links li {
                margin-bottom: 12px;
            }

            .footer-links a {
                color: rgba(255, 255, 255, 0.7);
                text-decoration: none;
                font-size: 0.95rem;
                transition: all 0.3s;
                display: flex;
                align-items: center;
                gap: 8px;
            }

                .footer-links a:hover {
                    color: var(--white);
                    padding-left: 5px;
                }

        .footer-contact li {
            display: flex;
            align-items: flex-start;
            gap: 12px;
            margin-bottom: 16px;
            color: rgba(255, 255, 255, 0.7);
            font-size: 0.95rem;
        }

        .footer-contact i {
            color: var(--primary-light);
            margin-top: 4px;
        }

        .footer-bottom {
            border-top: 1px solid rgba(255, 255, 255, 0.1);
            padding-top: 30px;
            margin-top: 50px;
            text-align: center;
            color: rgba(255, 255, 255, 0.6);
            font-size: 0.875rem;
        }


        @media (max-width: 991px) {
            .hero-title {
                font-size: 3rem;
            }

            .hero-stats {
                gap: 32px;
            }

            .cta-box {
                padding: 60px 40px;
            }

                .cta-box h2 {
                    font-size: 2rem;
                }
        }

        @media (max-width: 767px) {
            .hero {
                padding: 100px 0 60px;
            }

            .hero-title {
                font-size: 2.5rem;
            }

            .hero-stats {
                flex-wrap: wrap;
                gap: 24px;
            }

            .hero-stat {
                flex: 1;
                min-width: 100px;
            }

            .section-title {
                font-size: 2rem;
            }

            .floating-card {
                display: none;
            }

            .cta-box {
                padding: 40px 24px;
            }

                .cta-box h2 {
                    font-size: 1.75rem;
                }

            .footer {
                padding: 60px 0 20px;
            }
        }

        .fade-in {
            opacity: 0;
            transform: translateY(30px);
            transition: all 0.6s ease;
        }

            .fade-in.visible {
                opacity: 1;
                transform: translateY(0);
            }


        .loading-spinner {
            display: inline-block;
            width: 20px;
            height: 20px;
            border: 2px solid rgba(255, 255, 255, 0.3);
            border-top-color: white;
            border-radius: 50%;
            animation: spin 0.8s linear infinite;
        }

        @keyframes spin {
            to {
                transform: rotate(360deg);
            }
        }
    </style>

</head>
<body>
    <form id="form1" runat="server">

        <nav class="navbar" id="navbar">
            <div class="container">
                <div class="d-flex justify-content-between align-items-center w-100">
                    <a href="Home.aspx" class="navbar-brand">
                        <i class="fas fa-heartbeat"></i>
                        <span>Guardian Health</span>
                    </a>
                    <div class="d-none d-md-flex align-items-center">
                        <a href="#features" class="nav-link">Features</a>
                        <a href="#services" class="nav-link">Services</a>
                        <a href="#testimonials" class="nav-link">Testimonials</a>
                        <a href="../login.aspx" class="btn-nav">Staff Login
                        </a>
                    </div>
                    <button class="d-md-none btn btn-light" type="button" data-bs-toggle="collapse" data-bs-target="#mobileNav">
                    </button>
                </div>
                <div class="collapse d-md-none" id="mobileNav">
                    <div class="pt-3">
                        <a href="#features" class="nav-link d-block py-2">Features</a>
                        <a href="#services" class="nav-link d-block py-2">Services</a>
                        <a href="#testimonials" class="nav-link d-block py-2">Testimonials</a>
                        <a href="../login.aspx" class="nav-link d-block py-2">
                            <i class="fas fa-sign-in-alt"></i>Staff Login
                        </a>
                    </div>
                </div>
            </div>
        </nav>


        <section class="hero">
            <div class="hero-particles" id="heroParticles"></div>
            <div class="container">
                <div class="row align-items-center">
                    <div class="col-lg-6">
                        <div class="hero-content">
                            <div class="hero-badge">
                                <i class="fas fa-check-circle"></i>
                                Trusted by 500+ Healthcare Providers
                           
                            </div>
                            <h1 class="hero-title">Modern Healthcare <span>Management</span> System
                            </h1>
                            <p class="hero-subtitle">
                                Streamline your hospital operations with our comprehensive management solution. 
                                From patient records to laboratory tests, manage everything in one place.
                           
                            </p>
                            <div class="hero-buttons">
                                <a href="../login.aspx" class="btn-primary-custom">
                                    <i class="fas fa-sign-in-alt"></i>Staff Login
                                </a>
                                <a href="#features" class="btn-secondary-custom">
                                    <i class="fas fa-play-circle"></i>Learn More
                                </a>
                            </div>
                            <div class="hero-stats">
                                <div class="hero-stat">
                                    <div class="hero-stat-number" id="statPatients">0</div>
                                    <div class="hero-stat-label">Patients Managed</div>
                                </div>
                                <div class="hero-stat">
                                    <div class="hero-stat-number" id="statDoctors">0</div>
                                    <div class="hero-stat-label">Expert Doctors</div>
                                </div>
                                <div class="hero-stat">
                                    <div class="hero-stat-number" id="statAppointments">0</div>
                                    <div class="hero-stat-label">Appointments</div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-lg-6 d-none d-lg-block">
                        <div class="hero-image">
                            <div class="hero-image-wrapper">
                                <img src="https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=600&h=500&fit=crop"
                                    alt="Healthcare professionals"
                                    onerror="this.src='../Lib/images/hero-medical.jpg'" />
                            </div>
                            <div class="floating-card floating-card-1">
                                <div class="floating-card-icon">
                                    <i class="fas fa-user-md"></i>
                                </div>
                                <div class="floating-card-title">50+ Doctors</div>
                                <div class="floating-card-text">Available 24/7</div>
                            </div>
                            <div class="floating-card floating-card-2">
                                <div class="floating-card-icon">
                                    <i class="fas fa-calendar-check"></i>
                                </div>
                                <div class="floating-card-title">Easy Booking</div>
                                <div class="floating-card-text">Online appointments</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>


        <section class="features" id="features">
            <div class="container">
                <div class="section-header fade-in">
                    <div class="section-badge">
                        <i class="fas fa-star"></i>Key Features
                   
                    </div>
                    <h2 class="section-title">Everything You Need to Manage Your Hospital</h2>
                    <p class="section-subtitle">
                        Our comprehensive suite of tools helps you streamline operations, 
                        improve patient care, and enhance overall efficiency.
                   
                    </p>
                </div>
                <div class="row g-4">
                    <div class="col-md-6 col-lg-3 fade-in">
                        <div class="feature-card">
                            <div class="feature-icon primary">
                                <i class="fas fa-user-injured"></i>
                            </div>
                            <h3 class="feature-title">Patient Management</h3>
                            <p class="feature-text">
                                Complete patient records, medical history, and treatment tracking in one secure place.
                           
                            </p>
                        </div>
                    </div>
                    <div class="col-md-6 col-lg-3 fade-in">
                        <div class="feature-card">
                            <div class="feature-icon success">
                                <i class="fas fa-user-md"></i>
                            </div>
                            <h3 class="feature-title">Doctor Portal</h3>
                            <p class="feature-text">
                                Efficient appointment scheduling, prescription management, and patient consultations.
                           
                            </p>
                        </div>
                    </div>
                    <div class="col-md-6 col-lg-3 fade-in">
                        <div class="feature-card">
                            <div class="feature-icon warning">
                                <i class="fas fa-flask"></i>
                            </div>
                            <h3 class="feature-title">Laboratory Integration</h3>
                            <p class="feature-text">
                                Seamless lab test ordering, results tracking, and report generation.
                           
                            </p>
                        </div>
                    </div>
                    <div class="col-md-6 col-lg-3 fade-in">
                        <div class="feature-card">
                            <div class="feature-icon info">
                                <i class="fas fa-chart-line"></i>
                            </div>
                            <h3 class="feature-title">Analytics & Reports</h3>
                            <p class="feature-text">
                                Comprehensive dashboards and insights for data-driven decision making.
                           
                            </p>
                        </div>
                    </div>
                </div>
            </div>
        </section>


        <section class="services" id="services">
            <div class="container">
                <div class="row align-items-center">
                    <div class="col-lg-5 fade-in">
                        <div class="section-header text-start">
                            <div class="section-badge">
                                <i class="fas fa-cogs"></i>Our Services
                           
                            </div>
                            <h2 class="section-title">Comprehensive Healthcare Solutions</h2>
                            <p class="section-subtitle text-start ms-0">
                                From patient registration to discharge, our system covers every aspect of hospital management.
                           
                            </p>
                        </div>
                    </div>
                    <div class="col-lg-7">
                        <div class="service-item fade-in">
                            <div class="service-icon">
                                <i class="fas fa-clipboard-list"></i>
                            </div>
                            <div class="service-content">
                                <h4>Appointment Scheduling</h4>
                                <p>Efficient booking system with automated reminders and calendar integration for doctors and patients.</p>
                            </div>
                        </div>
                        <div class="service-item fade-in">
                            <div class="service-icon">
                                <i class="fas fa-prescription-bottle-alt"></i>
                            </div>
                            <div class="service-content">
                                <h4>Prescription Management</h4>
                                <p>Digital prescription creation, medication tracking, and pharmacy integration for seamless care.</p>
                            </div>
                        </div>
                        <div class="service-item fade-in">
                            <div class="service-icon">
                                <i class="fas fa-vial"></i>
                            </div>
                            <div class="service-content">
                                <h4>Lab Test Management</h4>
                                <p>Complete laboratory workflow from test ordering to result delivery with real-time tracking.</p>
                            </div>
                        </div>
                        <div class="service-item fade-in">
                            <div class="service-icon">
                                <i class="fas fa-file-invoice-dollar"></i>
                            </div>
                            <div class="service-content">
                                <h4>Billing & Invoicing</h4>
                                <p>Automated billing system with insurance integration and comprehensive financial reporting.</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>


        <section class="stats-section">
            <div class="container">
                <div class="row">
                    <div class="col-6 col-md-3">
                        <div class="stat-box">
                            <i class="fas fa-hospital"></i>
                            <div class="stat-number" id="counterHospitals">0</div>
                            <div class="stat-label">Partner Hospitals</div>
                        </div>
                    </div>
                    <div class="col-6 col-md-3">
                        <div class="stat-box">
                            <i class="fas fa-users"></i>
                            <div class="stat-number" id="counterPatients">0</div>
                            <div class="stat-label">Patients Served</div>
                        </div>
                    </div>
                    <div class="col-6 col-md-3">
                        <div class="stat-box">
                            <i class="fas fa-user-md"></i>
                            <div class="stat-number" id="counterDoctors">0</div>
                            <div class="stat-label">Medical Staff</div>
                        </div>
                    </div>
                    <div class="col-6 col-md-3">
                        <div class="stat-box">
                            <i class="fas fa-calendar-check"></i>
                            <div class="stat-number" id="counterAppointments">0</div>
                            <div class="stat-label">Appointments</div>
                        </div>
                    </div>
                </div>
            </div>
        </section>


        <section class="testimonials" id="testimonials">
            <div class="container">
                <div class="section-header fade-in">
                    <div class="section-badge">
                        <i class="fas fa-comments"></i>Testimonials
                   
                    </div>
                    <h2 class="section-title">What Our Users Say</h2>
                    <p class="section-subtitle">
                        Hear from healthcare professionals who have transformed their operations with Guardian Health.
                   
                    </p>
                </div>
                <div class="row g-4">
                    <div class="col-md-4 fade-in">
                        <div class="testimonial-card">
                            <div class="testimonial-quote">
                                <i class="fas fa-quote-left"></i>
                            </div>
                            <p class="testimonial-text">
                                "Guardian Health has completely transformed how we manage our hospital. 
                                The intuitive interface and comprehensive features have saved us countless hours."
                           
                            </p>
                            <div class="testimonial-author">
                                <div class="testimonial-avatar">DR</div>
                                <div class="testimonial-info">
                                    <h5>Dr. Sarah Johnson</h5>
                                    <span>Chief Medical Officer</span>
                                    <div class="testimonial-rating">
                                        <i class="fas fa-star"></i>
                                        <i class="fas fa-star"></i>
                                        <i class="fas fa-star"></i>
                                        <i class="fas fa-star"></i>
                                        <i class="fas fa-star"></i>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4 fade-in">
                        <div class="testimonial-card">
                            <div class="testimonial-quote">
                                <i class="fas fa-quote-left"></i>
                            </div>
                            <p class="testimonial-text">
                                "The laboratory integration is exceptional. Results are instantly available 
                                to doctors, and the reporting features are incredibly detailed."
                           
                            </p>
                            <div class="testimonial-author">
                                <div class="testimonial-avatar">MR</div>
                                <div class="testimonial-info">
                                    <h5>Michael Rodriguez</h5>
                                    <span>Lab Director</span>
                                    <div class="testimonial-rating">
                                        <i class="fas fa-star"></i>
                                        <i class="fas fa-star"></i>
                                        <i class="fas fa-star"></i>
                                        <i class="fas fa-star"></i>
                                        <i class="fas fa-star"></i>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4 fade-in">
                        <div class="testimonial-card">
                            <div class="testimonial-quote">
                                <i class="fas fa-quote-left"></i>
                            </div>
                            <p class="testimonial-text">
                                "Patient management has never been easier. The system is reliable, 
                                secure, and the support team is always responsive and helpful."
                           
                            </p>
                            <div class="testimonial-author">
                                <div class="testimonial-avatar">EP</div>
                                <div class="testimonial-info">
                                    <h5>Emily Parker</h5>
                                    <span>Hospital Administrator</span>
                                    <div class="testimonial-rating">
                                        <i class="fas fa-star"></i>
                                        <i class="fas fa-star"></i>
                                        <i class="fas fa-star"></i>
                                        <i class="fas fa-star"></i>
                                        <i class="fas fa-star"></i>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>


        <section class="cta-section">
            <div class="container">
                <div class="cta-box fade-in">
                    <h2>Ready to Transform Your Hospital?</h2>
                    <p>Join hundreds of healthcare providers who trust Guardian Health for their hospital management needs.</p>
                    <a href="login.aspx" class="btn-cta">
                        <i class="fas fa-rocket"></i>Get Started Now
                    </a>
                </div>
            </div>
        </section>


        <footer class="footer">
            <div class="container">
                <div class="row">
                    <div class="col-lg-4 mb-4 mb-lg-0">
                        <a href="Home.aspx" class="footer-brand">
                            <i class="fas fa-heartbeat"></i>
                            Guardian Health
                        </a>
                        <p class="footer-about">
                            Guardian Health is a comprehensive hospital management system designed to 
                            streamline healthcare operations and improve patient care delivery.
                       
                        </p>
                        <div class="footer-social">
                            <a href="#" aria-label="Facebook"><i class="fab fa-facebook-f"></i></a>
                            <a href="#" aria-label="Twitter"><i class="fab fa-twitter"></i></a>
                            <a href="#" aria-label="LinkedIn"><i class="fab fa-linkedin-in"></i></a>
                            <a href="#" aria-label="Instagram"><i class="fab fa-instagram"></i></a>
                        </div>
                    </div>
                    <div class="col-lg-2 col-md-4 mb-4 mb-md-0">
                        <h4 class="footer-title">Quick Links</h4>
                        <ul class="footer-links">
                            <li><a href="Home.aspx"><i class="fas fa-chevron-right"></i>Home</a></li>
                            <li><a href="#features"><i class="fas fa-chevron-right"></i>Features</a></li>
                            <li><a href="#services"><i class="fas fa-chevron-right"></i>Services</a></li>
                            <li><a href="login.aspx"><i class="fas fa-chevron-right"></i>Login</a></li>
                        </ul>
                    </div>
                    <div class="col-lg-2 col-md-4 mb-4 mb-md-0">
                        <h4 class="footer-title">Services</h4>
                        <ul class="footer-links">
                            <li><a href="#"><i class="fas fa-chevron-right"></i>Patient Care</a></li>
                            <li><a href="#"><i class="fas fa-chevron-right"></i>Laboratory</a></li>
                            <li><a href="#"><i class="fas fa-chevron-right"></i>Pharmacy</a></li>
                            <li><a href="#"><i class="fas fa-chevron-right"></i>Billing</a></li>
                        </ul>
                    </div>
                    <div class="col-lg-4 col-md-4">
                        <h4 class="footer-title">Contact Us</h4>
                        <ul class="footer-links footer-contact">
                            <li>
                                <i class="fas fa-map-marker-alt"></i>
                                <span>123 Healthcare Avenue, Medical District, NY 10001</span>
                            </li>
                            <li>
                                <i class="fas fa-phone"></i>
                                <span>+1 (555) 123-4567</span>
                            </li>
                            <li>
                                <i class="fas fa-envelope"></i>
                                <span>support@guardianhealth.com</span>
                            </li>
                            <li>
                                <i class="fas fa-clock"></i>
                                <span>24/7 Support Available</span>
                            </li>
                        </ul>
                    </div>
                </div>
                <div class="footer-bottom">
                    <p>&copy; <%= DateTime.Now.Year %> Guardian Health. All rights reserved. | Designed for Healthcare Excellence</p>
                </div>
            </div>
        </footer>
    </form>

    <script src="../Lib/js/bootstrap.bundle.min.js"></script>
    <script>

        window.addEventListener('scroll', () => {
            const navbar = document.getElementById('navbar');
            if (window.scrollY > 50) {
                navbar.classList.add('scrolled');
            } else {
                navbar.classList.remove('scrolled');
            }
        });


        function createHeroParticles() {
            const container = document.getElementById('heroParticles');
            const particleCount = 25;

            for (let i = 0; i < particleCount; i++) {
                const particle = document.createElement('div');
                particle.className = 'hero-particle';
                particle.style.left = Math.random() * 100 + '%';
                particle.style.animationDelay = Math.random() * 20 + 's';
                particle.style.animationDuration = (Math.random() * 10 + 15) + 's';
                particle.style.width = (Math.random() * 6 + 4) + 'px';
                particle.style.height = particle.style.width;
                container.appendChild(particle);
            }
        }


        function animateCounter(element, target, duration = 2000) {
            let start = 0;
            const increment = target / (duration / 16);

            function updateCounter() {
                start += increment;
                if (start < target) {
                    element.textContent = Math.floor(start).toLocaleString() + '+';
                    requestAnimationFrame(updateCounter);
                } else {
                    element.textContent = target.toLocaleString() + '+';
                }
            }

            updateCounter();
        }


        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };

        const fadeObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('visible');
                }
            });
        }, observerOptions);


        let countersAnimated = false;
        const counterObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting && !countersAnimated) {
                    countersAnimated = true;
                    animateCounter(document.getElementById('counterHospitals'), 500);
                    animateCounter(document.getElementById('counterPatients'), 50000);
                    animateCounter(document.getElementById('counterDoctors'), 1200);
                    animateCounter(document.getElementById('counterAppointments'), 150000);
                }
            });
        }, observerOptions);


        let heroStatsAnimated = false;
        const heroStatsObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting && !heroStatsAnimated) {
                    heroStatsAnimated = true;
                    animateCounter(document.getElementById('statPatients'), 25000, 1500);
                    animateCounter(document.getElementById('statDoctors'), 150, 1500);
                    animateCounter(document.getElementById('statAppointments'), 5000, 1500);
                }
            });
        }, observerOptions);


        document.addEventListener('DOMContentLoaded', () => {
            createHeroParticles();

            document.querySelectorAll('.fade-in').forEach(el => {
                fadeObserver.observe(el);
            });

            counterObserver.observe(document.querySelector('.stats-section'));
            heroStatsObserver.observe(document.querySelector('.hero-stats'));
        });

        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                e.preventDefault();
                const target = document.querySelector(this.getAttribute('href'));
                if (target) {
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });
    </script>
</body>

</html>
