<%@ Page Title="Manage Doctors - Guardian Health" Language="C#" MasterPageFile="~/Views/Admin/AdminMaster.Master" AutoEventWireup="true" CodeBehind="Doctors.aspx.cs" Inherits="HospitalMSWebApp.Views.Admin.Doctors" EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

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
                    <asp:Label CssClass="field-label" AssociatedControlID="DocNameTb" runat="server"><i class="fas fa-user"></i>Doctor Name</asp:Label>
                    <div class="field-wrap">
                        <input type="text" id="DocNameTb" runat="server" placeholder="Dr. Full Name" />
                        <i class="fas fa-user field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="emailTb" runat="server"><i class="fas fa-envelope"></i>Email Address</asp:Label>
                    <div class="field-wrap">
                        <asp:TextBox ID="emailTb" runat="server" placeholder="doctor@hospital.com" TextMode="Email"></asp:TextBox>
                        <i class="fas fa-envelope field-ico"></i>
                    </div>
                </div>
                <label><input type="checkbox" name="confirmLoginEmail" /> Confirm changing the existing sign-in email (ends existing sessions).</label>
                <label><input type="checkbox" name="confirmPasswordChange" /> Confirm replacing the selected account's password. Leave password blank to preserve it.</label>

                <div class="field-row">
                    <div class="field-group">
                        <asp:Label CssClass="field-label" AssociatedControlID="DocPhoneTb" runat="server"><i class="fas fa-phone"></i>Phone</asp:Label>
                        <div class="field-wrap">
                            <input type="text" id="DocPhoneTb" runat="server" placeholder="+1 (555) 000-0000" />
                            <i class="fas fa-phone field-ico"></i>
                        </div>
                    </div>
                    <div class="field-group">
                        <asp:Label CssClass="field-label" AssociatedControlID="DocExpTb" runat="server"><i class="fas fa-briefcase"></i>Experience (yrs)</asp:Label>
                        <div class="field-wrap">
                            <input type="number" id="DocExpTb" runat="server" placeholder="0" min="0" />
                            <i class="fas fa-briefcase field-ico"></i>
                        </div>
                    </div>
                </div>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="SpecialisationTb" runat="server"><i class="fas fa-stethoscope"></i>Specialization</asp:Label>
                    <div class="field-wrap">
                        <asp:TextBox ID="SpecialisationTb" runat="server" placeholder="e.g. Cardiology"></asp:TextBox>
                        <i class="fas fa-stethoscope field-ico"></i>
                    </div>
                </div>

                <div class="field-row">
                    <div class="field-group">
                        <asp:Label CssClass="field-label" AssociatedControlID="GenderCb" runat="server"><i class="fas fa-venus-mars"></i>Gender</asp:Label>
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
                        <asp:Label CssClass="field-label" AssociatedControlID="DOBTb" runat="server"><i class="fas fa-cake-candles"></i>Date of Birth</asp:Label>
                        <div class="field-wrap no-ico">
                            <input type="date" id="DOBTb" runat="server" />
                        </div>
                    </div>
                </div>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="AddressTb" runat="server"><i class="fas fa-location-dot"></i>Address</asp:Label>
                    <div class="field-wrap">
                        <asp:TextBox ID="AddressTb" runat="server" placeholder="Full address"></asp:TextBox>
                        <i class="fas fa-location-dot field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="PasswordTb" runat="server"><i class="fas fa-lock"></i>Password</asp:Label>
                    <div class="field-wrap">
                        <input type="password" id="PasswordTb" runat="server" autocomplete="new-password" placeholder="Leave blank on edit to preserve password" />
                        <i class="fas fa-lock field-ico"></i>
                    </div>
                </div>

                <div class="field-group">
                    <asp:Label CssClass="field-label" AssociatedControlID="StatusCb" runat="server"><i class="fas fa-toggle-on"></i>Status</asp:Label>
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
                    <asp:GridView ID="DoctorsGV" runat="server" CssClass="table gh-table"
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
