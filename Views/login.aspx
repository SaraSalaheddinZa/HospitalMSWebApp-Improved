<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="login.aspx.cs" Inherits="HospitalMSWebApp.Views.login" %>
<!doctype html>
<html lang="en" data-theme="light" data-mode="<%= EntryMode %>" data-motion="running">
<head>
<meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<meta name="color-scheme" content="light dark">
<meta name="description" content="Sign in to Guardian Health for your care or your authorized hospital workspace.">
<title>Guardian Health — Sign in</title>
<script src="../Assets/Public/LivingThread/public-preference.js"></script>
<link rel="preload" href="../Assets/Public/LivingThread/Manrope.ttf" as="font" type="font/ttf" crossorigin>
<link rel="stylesheet" href="../Assets/Public/LivingThread/living-sculpture.css?v=3"><link rel="stylesheet" href="../Assets/Public/LivingThread/living-sculpture-render.css?v=5"><link rel="stylesheet" href="../Assets/Public/LivingThread/living-sculpture-polish.css?v=1"><script defer src="../Assets/Public/LivingThread/living-sculpture.js?v=5"></script>
<link rel="stylesheet" href="../Assets/Public/LivingThread/application.css" />
</head>
<body class="living-auth">
<a class="skip-link" href="#sign-in">Skip to sign in</a>
<div class="render-environment" aria-hidden="true"><img class="render-light" src="../Assets/Public/LivingThread/sculpture-architecture-light.webp" alt="" fetchpriority="high"><img class="render-dark" src="../Assets/Public/LivingThread/sculpture-architecture-dark.webp" alt="" fetchpriority="high"></div>
<header class="sculpture-header">
 <a class="brand" href="Guest/Home.aspx" aria-label="Guardian Health homepage"><svg viewBox="0 0 44 48" aria-hidden="true"><path d="M5 40V22C5 10 12 3 22 3s17 7 17 19v18M13 40V22c0-7 3-11 9-11s9 4 9 11v18M2 45h40"/><path class="brand-thread" d="M22 45V31"/></svg><span>Guardian Health<small>CONNECTED CARE</small></span></a>
 <div class="mode-switch" role="group" aria-label="Choose Patient or Staff experience"><button type="button" data-mode-choice="patient" aria-pressed="true">Patient</button><button type="button" data-mode-choice="staff" aria-pressed="false">Staff</button></div>
 <div class="preferences"><button type="button" id="motion-toggle" aria-pressed="false"><svg viewBox="0 0 20 20" aria-hidden="true"><path d="M7 4v12M13 4v12"/></svg><span>Pause motion</span></button><button id="theme-toggle" type="button" aria-label="Switch to dark mode"><svg class="theme-sun" viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="4"/><path d="M12 1v3m0 16v3M1 12h3m16 0h3M4 4l2 2m12 12 2 2M4 20l2-2M18 6l2-2"/></svg><svg class="theme-moon" viewBox="0 0 24 24" aria-hidden="true"><path d="M20 15A9 9 0 0 1 9 3a9 9 0 1 0 11 12Z"/></svg><span data-theme-label>Light</span></button></div>
</header>
<main class="sculpture-main" id="main">
 <section class="editorial" aria-labelledby="sculpture-title">
  <p class="eyebrow" data-copy="eyebrow">A connected place<br>for every care journey</p>
  <h1 id="sculpture-title"><span data-copy="title1">Your care.</span><span data-copy="title2">A more open</span><em data-copy="title3">tomorrow.</em></h1>
  <p class="editorial-note" data-copy="intro">Appointments, records and your next step.<br>A familiar place to continue your care journey.</p>
  <a class="home-link" href="Guest/Home.aspx"><span aria-hidden="true">↖</span> Explore Guardian Health</a>
  <div class="editorial-thread" aria-hidden="true"><span></span><i></i><span></span></div>
 </section>
 <div class="sculpture-stage">
  <div class="sculpture-aura" aria-hidden="true"><span class="aura-halo"></span><span class="aura-core"></span><span class="aura-shimmer"></span><span class="aura-focus"></span></div>
  <div class="silk-image silk-back" aria-hidden="true"><img class="render-light" src="../Assets/Public/LivingThread/sculpture-silk-sheer-light.webp" alt=""><img class="render-dark" src="../Assets/Public/LivingThread/sculpture-silk-sheer-dark.webp" alt=""></div>
  <svg class="silk light-trail-back" viewBox="0 0 800 1000" preserveAspectRatio="none" aria-hidden="true"><defs><linearGradient id="thread-color"><stop offset="0" stop-color="#f7bdc6" stop-opacity="0"/><stop offset=".5" stop-color="#ffe6ce"/><stop offset="1" stop-color="#d8b3df" stop-opacity="0"/></linearGradient></defs><path class="thread-focus focus-back" pathLength="1" d="M197 799C163 655 100 551 128 361C155 191 303 91 444 116C583 142 688 252 700 402"/></svg>
  <section class="glass-panel" id="sign-in" aria-labelledby="form-title" tabindex="-1">
   <div class="glass-sheen" aria-hidden="true"></div>
   <div class="form-heading"><p class="form-kicker" data-copy="formKicker">YOUR PERSONAL ENTRANCE</p><h2 id="form-title" data-copy="formTitle">Welcome back.</h2><p data-copy="formIntro">Continue your care journey.</p></div>
   <form id="form1" runat="server" novalidate data-sign-in>
    <input type="hidden" name="entryMode" id="entry-mode" value="<%= EntryMode %>" />
    <div id="ErrMsg" runat="server" ClientIDMode="Static" class="feedback" role="alert" aria-atomic="true" tabindex="-1"></div>
    <div class="field"><label for="emailTb">Email address</label><div class="input-wrap"><svg viewBox="0 0 24 24" aria-hidden="true"><rect x="4" y="6" width="16" height="12" rx="2"/><path d="m4 7 8 6 8-6"/></svg><input id="emailTb" runat="server" ClientIDMode="Static" type="email" inputmode="email" autocomplete="username" placeholder="you@example.com" aria-describedby="email-error" required></div><span class="field-error" id="email-error" hidden></span></div>
    <div class="field"><label for="passwordTb">Password</label><div class="input-wrap password-wrap"><svg viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="10" width="14" height="11" rx="2"/><path d="M8 10V6a4 4 0 0 1 8 0v4M12 14v3"/></svg><input id="passwordTb" runat="server" ClientIDMode="Static" type="password" autocomplete="current-password" placeholder="Your password" aria-describedby="password-error" required><button type="button" id="password-toggle" aria-label="Show password" aria-controls="passwordTb" aria-pressed="false"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M2 12s4-6 10-6 10 6 10 6-4 6-10 6S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/><path class="eye-slash" d="m4 3 16 18"/></svg></button></div><span class="field-error" id="password-error" hidden></span></div>
    <div class="role-slot"><div class="field role-field"><label for="roleCb">Account role</label><asp:DropDownList ID="roleCb" runat="server" ClientIDMode="Static" aria-describedby="role-error">
<asp:ListItem Value="">Select your staff role</asp:ListItem><asp:ListItem Value="Admin">Admin</asp:ListItem><asp:ListItem Value="Doctor">Doctor</asp:ListItem><asp:ListItem Value="Receptionist">Receptionist</asp:ListItem><asp:ListItem Value="Laboratorian">Laboratorian</asp:ListItem><asp:ListItem Value="Nurse">Nurse</asp:ListItem><asp:ListItem Value="Pharmacist">Pharmacist</asp:ListItem><asp:ListItem Value="Patient">Patient</asp:ListItem></asp:DropDownList><span class="field-error" id="role-error" hidden></span></div></div>
    <div class="form-options"><span class="remember"><asp:CheckBox ID="rememberMeCb" runat="server" ClientIDMode="Static" Text="Remember me" /></span><a class="text-control" href="ForgotPassword.aspx">Forgot password?</a></div>
    <div class="sign-in-control"><asp:Button ID="loginBtn" runat="server" ClientIDMode="Static" Text="Sign In" CssClass="sign-in-button" OnClick="loginBtn_Click" /><span class="button-arrow" aria-hidden="true">&#8594;</span></div>
    <p class="privacy-note">Your account determines the records and actions you can access.</p>
   </form>
   <p class="access-help"><span data-copy="helpLabel">New to Guardian Health?</span> <a class="text-control" href="ContactAdmin.aspx"><span data-copy="helpAction">Registration guidance</span></a></p>
  </section>
  <svg class="silk silk-front" viewBox="0 0 800 1000" preserveAspectRatio="none" aria-hidden="true">
   <defs><linearGradient id="fragment-split" x1="0" y1="0" x2="0" y2="1"><stop id="front-fade-start" offset=".78" stop-color="black"/><stop id="front-fade-end" offset=".86" stop-color="white"/></linearGradient><mask id="safe-content"><rect width="800" height="1000" fill="url(#fragment-split)"/></mask></defs>
   <g mask="url(#safe-content)"><g class="raster-front">
    <image class="render-light" href="../Assets/Public/LivingThread/sculpture-silk-sheer-light.webp" width="800" height="1000" preserveAspectRatio="none"/>
    <image class="render-dark" href="../Assets/Public/LivingThread/sculpture-silk-sheer-dark.webp" width="800" height="1000" preserveAspectRatio="none"/>
   </g><path class="thread-focus focus-front" pathLength="1" d="M58 769C173 777 166 863 339 890C502 917 665 943 735 813C784 720 714 616 731 532"/></g>
  </svg>
  <div class="sculpture-shadow" aria-hidden="true"></div>
 </div>
</main>
<footer class="sculpture-footer"><span>PEOPLE <i>·</i> CARE <i>·</i> A MORE OPEN TOMORROW</span><a href="HelpCenter.aspx">Help centre ↗</a><a href="Guest/Home.aspx">Back to hospital</a></footer>
<noscript><p class="no-script">Sign-in works without JavaScript. Choose the role linked to your account.</p></noscript>
</body></html>
