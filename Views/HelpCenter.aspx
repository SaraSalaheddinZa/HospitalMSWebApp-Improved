<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="HelpCenter.aspx.cs" Inherits="HospitalMSWebApp.Views.HelpCenter" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Guardian Health - Help Center</title>
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
            --text-dark: #1e293b;
            --text-muted: #64748b;
            --bg-light: #f8fafc;
            --gradient-bg: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }

        * { margin: 0; padding: 0; box-sizing: border-box; }

        body {
            font-family: 'Inter', sans-serif;
            background: var(--bg-light);
            min-height: 100vh;
        }

        .help-header {
            background: var(--gradient-bg);
            padding: 60px 20px;
            text-align: center;
            color: white;
        }

        .help-header h1 {
            font-size: 2.5rem;
            font-weight: 800;
            margin-bottom: 16px;
        }

        .help-header p {
            font-size: 1.125rem;
            opacity: 0.9;
            max-width: 600px;
            margin: 0 auto 32px;
        }

        .search-box {
            max-width: 600px;
            margin: 0 auto;
            position: relative;
        }

        .search-box input {
            width: 100%;
            padding: 18px 24px 18px 56px;
            border: none;
            border-radius: 16px;
            font-size: 1rem;
            box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
        }

        .search-box i {
            position: absolute;
            left: 24px;
            top: 50%;
            transform: translateY(-50%);
            color: var(--text-muted);
            font-size: 1.25rem;
        }

        .help-nav {
            background: white;
            border-bottom: 1px solid #e2e8f0;
            padding: 16px 0;
            position: sticky;
            top: 0;
            z-index: 100;
        }

        .help-nav .container {
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .help-nav a {
            color: var(--text-muted);
            text-decoration: none;
            font-weight: 500;
            display: flex;
            align-items: center;
            gap: 8px;
            transition: color 0.3s;
        }

        .help-nav a:hover {
            color: var(--primary-color);
        }

        .help-content {
            padding: 60px 20px;
        }

        .category-card {
            background: white;
            border-radius: 20px;
            padding: 32px;
            margin-bottom: 24px;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
            transition: all 0.3s;
        }

        .category-card:hover {
            box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
            transform: translateY(-4px);
        }

        .category-icon {
            width: 60px;
            height: 60px;
            border-radius: 16px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.5rem;
            margin-bottom: 20px;
        }

        .category-icon.blue { background: rgba(37, 99, 235, 0.1); color: var(--primary-color); }
        .category-icon.green { background: rgba(16, 185, 129, 0.1); color: var(--success-color); }
        .category-icon.orange { background: rgba(245, 158, 11, 0.1); color: var(--warning-color); }
        .category-icon.purple { background: rgba(124, 58, 237, 0.1); color: #7c3aed; }

        .category-card h3 {
            font-size: 1.25rem;
            font-weight: 700;
            margin-bottom: 12px;
            color: var(--text-dark);
        }

        .category-card p {
            color: var(--text-muted);
            margin-bottom: 20px;
        }

        .faq-list {
            list-style: none;
            padding: 0;
            margin: 0;
        }

        .faq-item {
            border-bottom: 1px solid #e2e8f0;
            padding: 16px 0;
        }

        .faq-item:last-child {
            border-bottom: none;
        }

        .faq-question {
            display: flex;
            justify-content: space-between;
            align-items: center;
            cursor: pointer;
            font-weight: 500;
            color: var(--text-dark);
            transition: color 0.3s;
        }

        .faq-question:hover {
            color: var(--primary-color);
        }

        .faq-answer {
            display: none;
            padding-top: 12px;
            color: var(--text-muted);
            font-size: 0.95rem;
            line-height: 1.7;
        }

        .faq-answer.show {
            display: block;
            animation: fadeIn 0.3s ease;
        }

        @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
        }

        /* Quick Links */
        .quick-links {
            background: white;
            border-radius: 20px;
            padding: 32px;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
        }

        .quick-links h3 {
            font-size: 1.125rem;
            font-weight: 700;
            margin-bottom: 20px;
        }

        .quick-link {
            display: flex;
            align-items: center;
            gap: 12px;
            padding: 14px;
            border-radius: 12px;
            color: var(--text-dark);
            text-decoration: none;
            transition: all 0.3s;
            margin-bottom: 8px;
        }

        .quick-link:hover {
            background: var(--bg-light);
            color: var(--primary-color);
        }

        .quick-link i {
            width: 40px;
            height: 40px;
            border-radius: 10px;
            background: rgba(37, 99, 235, 0.1);
            color: var(--primary-color);
            display: flex;
            align-items: center;
            justify-content: center;
        }

        .cta-section {
            background: linear-gradient(135deg, var(--primary-color), #7c3aed);
            border-radius: 20px;
            padding: 48px;
            text-align: center;
            color: white;
            margin-top: 40px;
        }

        .cta-section h2 {
            font-size: 1.75rem;
            font-weight: 700;
            margin-bottom: 12px;
        }

        .cta-section p {
            opacity: 0.9;
            margin-bottom: 24px;
        }

        .btn-cta {
            background: white;
            color: var(--primary-color);
            padding: 14px 32px;
            border-radius: 12px;
            font-weight: 600;
            text-decoration: none;
            display: inline-flex;
            align-items: center;
            gap: 10px;
            transition: all 0.3s;
        }

        .btn-cta:hover {
            transform: translateY(-2px);
            box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
        }

        /* Footer */
        .help-footer {
            background: var(--text-dark);
            color: white;
            padding: 40px 20px;
            text-align: center;
        }

        .help-footer p {
            opacity: 0.7;
            margin: 0;
        }

        @media (max-width: 768px) {
            .help-header h1 { font-size: 2rem; }
            .help-content { padding: 40px 20px; }
            .cta-section { padding: 32px 24px; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <header class="help-header">
            <h1> How Can We Help?</h1>
            <p>Find answers to common questions and learn how to use Guardian Health effectively</p>
            <div class="search-box">
                <i class="fas fa-search"></i>
                <input type="text" id="searchInput" placeholder="Search for answers..." onkeyup="searchFAQs()" />
            </div>
        </header>

        <nav class="help-nav">
            <div class="container">
                <a href="Home.aspx"><i class="fas fa-home"></i> Home</a>
                <a href="login.aspx"><i class="fas fa-sign-in-alt"></i> Login</a>
            </div>
        </nav>

        <main class="help-content">
            <div class="container">
                <div class="row">
                    <div class="col-lg-8">
                        <div class="category-card">
                            <div class="category-icon blue">
                                <i class="fas fa-rocket"></i>
                            </div>
                            <h3>Getting Started</h3>
                            <p>Learn the basics of using Guardian Health system</p>
                            <ul class="faq-list">
                                <li class="faq-item">
                                    <div class="faq-question" onclick="toggleFAQ(this)">
                                        <span>How do I log in to the system?</span>
                                        <i class="fas fa-chevron-down"></i>
                                    </div>
                                    <div class="faq-answer">
                                        Navigate to the login page, enter your registered email address, password, and select your role from the dropdown menu. Click "Sign In" to access your dashboard.
                                    </div>
                                </li>
                                <li class="faq-item">
                                    <div class="faq-question" onclick="toggleFAQ(this)">
                                        <span>What should I do if I forget my password?</span>
                                        <i class="fas fa-chevron-down"></i>
                                    </div>
                                    <div class="faq-answer">
                                        Click on "Forgot Password?" link on the login page. Enter your registered email and select your role. A password reset link will be sent to your email address.
                                    </div>
                                </li>
                                <li class="faq-item">
                                    <div class="faq-question" onclick="toggleFAQ(this)">
                                        <span>How do I request a new account?</span>
                                        <i class="fas fa-chevron-down"></i>
                                    </div>
                                    <div class="faq-answer">
                                        Contact your system administrator or use the "Contact Admin" page to request a new account. Provide your full name, email, and requested role.
                                    </div>
                                </li>
                            </ul>
                        </div>

                        <div class="category-card">
                            <div class="category-icon green">
                                <i class="fas fa-user-md"></i>
                            </div>
                            <h3>For Doctors</h3>
                            <p>Manage patients, prescriptions, and appointments</p>
                            <ul class="faq-list">
                                <li class="faq-item">
                                    <div class="faq-question" onclick="toggleFAQ(this)">
                                        <span>How do I create a prescription?</span>
                                        <i class="fas fa-chevron-down"></i>
                                    </div>
                                    <div class="faq-answer">
                                        Go to the Prescriptions section, click "Add New", select the patient, enter medication details, dosage, and instructions. Save the prescription to make it available to the patient and pharmacy.
                                    </div>
                                </li>
                                <li class="faq-item">
                                    <div class="faq-question" onclick="toggleFAQ(this)">
                                        <span>How do I view patient medical history?</span>
                                        <i class="fas fa-chevron-down"></i>
                                    </div>
                                    <div class="faq-answer">
                                        Navigate to the Patients section, search for the patient by name or ID, and click on their record to view complete medical history including past visits, prescriptions, and lab results.
                                    </div>
                                </li>
                                <li class="faq-item">
                                    <div class="faq-question" onclick="toggleFAQ(this)">
                                        <span>Can I order lab tests for patients?</span>
                                        <i class="fas fa-chevron-down"></i>
                                    </div>
                                    <div class="faq-answer">
                                        Yes, from the patient's profile or the Lab Tests section, you can order various tests. The laboratory staff will be notified and results will be available once completed.
                                    </div>
                                </li>
                            </ul>
                        </div>

                        <div class="category-card">
                            <div class="category-icon orange">
                                <i class="fas fa-calendar-check"></i>
                            </div>
                            <h3>For Receptionists</h3>
                            <p>Manage appointments, patient registration, and billing</p>
                            <ul class="faq-list">
                                <li class="faq-item">
                                    <div class="faq-question" onclick="toggleFAQ(this)">
                                        <span>How do I register a new patient?</span>
                                        <i class="fas fa-chevron-down"></i>
                                    </div>
                                    <div class="faq-answer">
                                        Go to the Patients section, click "Add New Patient", fill in all required information including personal details, contact information, and insurance details. Save to create the patient record.
                                    </div>
                                </li>
                                <li class="faq-item">
                                    <div class="faq-question" onclick="toggleFAQ(this)">
                                        <span>How do I schedule an appointment?</span>
                                        <i class="fas fa-chevron-down"></i>
                                    </div>
                                    <div class="faq-answer">
                                        Navigate to Appointments, click "New Appointment", select the patient, choose a doctor, pick an available time slot, and confirm the booking. The patient will receive a confirmation.
                                    </div>
                                </li>
                                <li class="faq-item">
                                    <div class="faq-question" onclick="toggleFAQ(this)">
                                        <span>How do I process patient billing?</span>
                                        <i class="fas fa-chevron-down"></i>
                                    </div>
                                    <div class="faq-answer">
                                        Access the Billing section, select the patient, add services provided, apply any insurance coverage, and generate the invoice. Payment can be recorded against the invoice.
                                    </div>
                                </li>
                            </ul>
                        </div>

                        <!-- For Laboratory Staff -->
                        <div class="category-card">
                            <div class="category-icon purple">
                                <i class="fas fa-flask"></i>
                            </div>
                            <h3>For Laboratory Staff</h3>
                            <p>Process tests, manage results, and generate reports</p>
                            <ul class="faq-list">
                                <li class="faq-item">
                                    <div class="faq-question" onclick="toggleFAQ(this)">
                                        <span>How do I view pending lab tests?</span>
                                        <i class="fas fa-chevron-down"></i>
                                    </div>
                                    <div class="faq-answer">
                                        Go to the Lab Tests section. Pending tests will be displayed with a "Pending" status. You can filter by date, patient, or test type to find specific orders.
                                    </div>
                                </li>
                                <li class="faq-item">
                                    <div class="faq-question" onclick="toggleFAQ(this)">
                                        <span>How do I enter test results?</span>
                                        <i class="fas fa-chevron-down"></i>
                                    </div>
                                    <div class="faq-answer">
                                        Select the pending test, click "Enter Results", input the values for each parameter, add any notes or observations, and submit. The results will be immediately available to the ordering doctor.
                                    </div>
                                </li>
                                <li class="faq-item">
                                    <div class="faq-question" onclick="toggleFAQ(this)">
                                        <span>Can I generate lab reports?</span>
                                        <i class="fas fa-chevron-down"></i>
                                    </div>
                                    <div class="faq-answer">
                                        Yes, after entering results, you can generate a PDF report by clicking "Generate Report". The report includes patient details, test parameters, results with reference ranges, and lab technician signature.
                                    </div>
                                </li>
                            </ul>
                        </div>
                    </div>

                    <div class="col-lg-4">
                        <div class="quick-links">
                            <h3>Quick Links</h3>
                            <a href="login.aspx" class="quick-link">
                                <i class="fas fa-sign-in-alt"></i>
                                <span>Staff Login</span>
                            </a>
                            <a href="ContactAdmin.aspx" class="quick-link">
                                <i class="fas fa-user-shield"></i>
                                <span>Contact Administrator</span>
                            </a>
                            <a href="Support.aspx" class="quick-link">
                                <i class="fas fa-headset"></i>
                                <span>Technical Support</span>
                            </a>
                            <a href="ForgotPassword.aspx" class="quick-link">
                                <i class="fas fa-key"></i>
                                <span>Reset Password</span>
                            </a>
                        </div>

                        <div class="cta-section">
                            <h2>Still Need Help?</h2>
                            <p>Our support team is available 24/7 to assist you with any issues.</p>
                            <a href="Support.aspx" class="btn-cta">
                                <i class="fas fa-envelope"></i> Contact Support
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </main>

        <footer class="help-footer">
            <div class="container">
                <p>&copy; <%= DateTime.Now.Year %> Guardian Health. All rights reserved.</p>
            </div>
        </footer>
    </form>

    <script>
        function toggleFAQ(element) {
            const answer = element.nextElementSibling;
            const icon = element.querySelector('i');

            answer.classList.toggle('show');
            icon.classList.toggle('fa-chevron-down');
            icon.classList.toggle('fa-chevron-up');
        }

        function searchFAQs() {
            const searchTerm = document.getElementById('searchInput').value.toLowerCase();
            const faqItems = document.querySelectorAll('.faq-item');
            const categoryCards = document.querySelectorAll('.category-card');

            faqItems.forEach(item => {
                const question = item.querySelector('.faq-question span').textContent.toLowerCase();
                const answer = item.querySelector('.faq-answer').textContent.toLowerCase();

                if (question.includes(searchTerm) || answer.includes(searchTerm)) {
                    item.style.display = 'block';
                } else {
                    item.style.display = searchTerm === '' ? 'block' : 'none';
                }
            });

            categoryCards.forEach(card => {
                const visibleFAQs = card.querySelectorAll('.faq-item[style="display: block;"], .faq-item:not([style])');
                card.style.display = visibleFAQs.length > 0 ? 'block' : 'none';
            });
        }
    </script>
</body>
</html>